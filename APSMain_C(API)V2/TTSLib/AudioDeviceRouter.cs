using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;

namespace APSMain.TTSLib
{
    public enum AudioRouteState
    {
        Idle,            // 대기 중
        Dual,
        WaitingForUsb,   // USB Active 대기
        UsbActive,       // USB로 전환 완료
        Unplugged,       // 해제됨(또는 대기 중 취소)
        TimedOut,        // 대기 타임아웃
        Error            // 예외
    }

    public sealed class AudioRouteStateChangedEventArgs : EventArgs
    {
        public AudioRouteStateChangedEventArgs(AudioRouteState state, string? info = null)
        { State = state; Info = info; }
        public AudioRouteState State { get; }
        public string? Info { get; }
    }

    public static class AudioDeviceRouter
    {
        // ===== Events (메인에서 Program 시작 시 등록) =====
        public static event EventHandler<AudioRouteStateChangedEventArgs>? StateChanged;
        public static event Action<string>? OutputDeviceChanged;  // 선택: 장치명 알림

        // ===== State =====
        private static readonly object _lock = new();
        private static CancellationTokenSource? _usbWaitCts;
        private static string? _currentDeviceId;
        public static AudioRouteState CurrentState { get; private set; } = AudioRouteState.Idle;

        // ===== 외부 알림 진입점 =====

        /// <summary>이어폰(USB) "연결" 신호: USB가 Active 되면 자동 라우팅. 실패/취소 시 아무 것도 바꾸지 않음.</summary>
        //public static void NotifyHeadphonePlugged(int timeoutMs = 5000, int pollMs = 250)
        //{
        //    CancellationToken token;
        //    lock (_lock) {
        //        TryCancelAndDispose(ref _usbWaitCts);
        //        _usbWaitCts = new CancellationTokenSource();
        //        token = _usbWaitCts.Token;
        //        SetState(AudioRouteState.WaitingForUsb, $"timeout={timeoutMs}, poll={pollMs}");
        //    }

        //    _ = Task.Run(async () =>
        //    {
        //        try {
        //            var usb = await WaitForActiveUsbAsync(timeoutMs, pollMs, token).ConfigureAwait(false);
        //            if (token.IsCancellationRequested) {
        //                SetState(AudioRouteState.Idle, "canceled");
        //                return;
        //            }
        //            if (usb != null) {
        //                SetState(AudioRouteState.UsbActive, usb.FriendlyName);
        //                Apply(usb);
        //            }
        //            else {
        //                SetState(AudioRouteState.TimedOut, "no ACTIVE USB");
        //            }
        //        }
        //        catch (Exception ex) {
        //            SetState(AudioRouteState.Error, ex.Message);
        //        }
        //    }, token);
        //}

        /// <summary>이어폰 "해제" 신호: 대기 즉시 취소 + (옵션) 스피커로 복귀.</summary>
        //public static void NotifyHeadphoneUnplugged(bool switchToSpeaker = true)
        //{
        //    lock (_lock) {
        //        TryCancelAndDispose(ref _usbWaitCts);
        //        SetState(AudioRouteState.Unplugged, "notify");
        //    }
        //    if (switchToSpeaker) {
        //        var spk = GetDefaultSpeaker();
        //        if (spk != null)
        //            Apply(spk);
        //    }
        //}

        public static void NotifyHeadphonePlugged(int timeoutMs = 15000, int pollMs = 250)
        {
            lock (_lock)
            {
                TryCancelAndDispose(ref _usbWaitCts);
                _usbWaitCts = new CancellationTokenSource();
                SetState(AudioRouteState.WaitingForUsb, "plugged");
            }

            var token = _usbWaitCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    // USB 장치가 OS에 올라올 시간 조금 주기
                    await Task.Delay(500, token);

                    if (token.IsCancellationRequested)
                        return;

                    if (!SwitchToUsb())
                    {
                        SetState(AudioRouteState.Error, "USB not found or inactive");
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    SetState(AudioRouteState.Error, ex.Message);
                }
            }, token);
        }


        public static void NotifyHeadphoneUnplugged(bool switchToSpeaker = true)
        {
            lock (_lock) {
                TryCancelAndDispose(ref _usbWaitCts);
                SetState(AudioRouteState.Unplugged, "notify");
            }

            if (switchToSpeaker) {
                var spk = GetDefaultSpeaker();
                if (spk != null)
                    Apply(spk);
            }
        }
        

        /// <summary>즉시 라우팅: USB가 Active면 USB로, 아니면 일반 스피커로.</summary>
        public static void RouteByConnectionNow()
        {
            using var en = new MMDeviceEnumerator();

            var usb = en.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.All)
                        .FirstOrDefault(dev => dev.State == DeviceState.Active && IsUsbDevice(dev));
            if (usb != null) { SetState(AudioRouteState.UsbActive, usb.FriendlyName); Apply(usb); return; }

            var spk = en.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.All)
                        .FirstOrDefault(d => d.State == DeviceState.Active &&
                                             !IsUsbDevice(d) && !LooksLikeDisplaySink(d));
            if (spk != null) { SetState(AudioRouteState.Idle, spk.FriendlyName); Apply(spk); }
        }

        // ===== 내부 로직 =====

        // USB가 ACTIVE 되는 순간까지 기다림. 중간에 빠지면 즉시 종료.
        private static async Task<MMDevice?> WaitForActiveUsbAsync(int timeoutMs, int pollMs, CancellationToken token)
        {
            var sw = Stopwatch.StartNew();

            while (sw.ElapsedMilliseconds < timeoutMs) {
                if (token.IsCancellationRequested)
                    return null;

                try {
                    using var en = new MMDeviceEnumerator();

                    Console.WriteLine($"AA:{en.ToString()}");
                    // 1) 지금 당장 ACTIVE인 USB가 있으면 즉시 반환
                    var activeUsb = en.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.All)
                                      .FirstOrDefault(dev => dev.State == DeviceState.Active && IsUsbDevice(dev));
                    if (activeUsb != null)
                        return activeUsb;

                    // 2) 목록에 USB 자체가 안 보이면(= 꼽고 바로 빼거나 물리 분리) 즉시 종료
                    bool anyUsbListed = en.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.All)
                                          .Any(IsUsbDevice);
                    if (!anyUsbListed)
                        return null;
                }
                catch {
                    // 무시하고 재시도
                }

                try { await Task.Delay(pollMs, token).ConfigureAwait(false); }
                catch (OperationCanceledException) { return null; }
            }
            return null; // 타임아웃
        }

        // USB: 이름에 'usb' 포함 & 디스플레이 오디오는 제외 — 딱 두 조건만
        private static bool IsUsbDevice(MMDevice d)
        {
            var n = (d.FriendlyName ?? "").ToLowerInvariant();

            if (LooksLikeDisplaySink(d))
                return false;   // HDMI/DP/TV 등만 제외

            // “스피커 USB Audio Device …” 같은 이름까지 포함해서 잡도록
            return n.Contains("usb audio device") || n.Contains("usb");
        }

        // 기본 스피커: USB/디스플레이가 아닌 Console 우선, 없으면 ACTIVE 중 임의 스피커
        private static MMDevice? GetDefaultSpeaker()
        {
            try {
                using var en = new MMDeviceEnumerator();

                var console = en.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
                if (console != null) {
                    var name = (console.FriendlyName ?? "").ToLowerInvariant();
                    if (!LooksLikeDisplaySink(console) && !name.Contains("usb"))
                        return console;
                }

                return en.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                         .FirstOrDefault(d =>
                         {
                             var n = (d.FriendlyName ?? "").ToLowerInvariant();
                             return !LooksLikeDisplaySink(d) && !n.Contains("usb");
                         });
            }
            catch { return null; }
        }

        // 실제 장치 적용(자동 라우팅)x
        private static void Apply(MMDevice dev)
        {
            lock (_lock) { _currentDeviceId = dev.ID; }
            //Console.WriteLine($"[Router] Apply => {dev.FriendlyName}");

            if (CurrentState == AudioRouteState.Unplugged)
                CurrentState = AudioRouteState.Idle;
            MediaPlayer.SetOutputDevice(dev, stopCurrent: true);
            try { OutputDeviceChanged?.Invoke(dev.FriendlyName ?? ""); }
            catch { }
        }

        // HDMI/DP/TV 등 디스플레이 오디오 제외
        private static bool LooksLikeDisplaySink(MMDevice d)
        {
            var n = (d.FriendlyName ?? "").ToLowerInvariant();
            return  n.Contains("hdmi") ||
                    n.Contains("display audio") ||
                    n.Contains("dp") ||
                    n.Contains("tv");
        }

        // State helper
        public static void SetState(AudioRouteState s, string? info = null)
        {
            CurrentState = s;
            Console.WriteLine($"[Router] State => {s} {(string.IsNullOrEmpty(info) ? "" : $"| {info}")}");
            try { StateChanged?.Invoke(null, new AudioRouteStateChangedEventArgs(s, info)); }
            catch { }
        }

        private static void TryCancelAndDispose(ref CancellationTokenSource? cts)
        {
            try { cts?.Cancel(); }
            catch { }
            try { cts?.Dispose(); }
            catch { }
            cts = null;
        }

        /// <summary>강제 전환: 현재 활성 USB 장치로 전환(둘 다 연결된 상태 가정).</summary>
        //public static bool SwitchToUsb()
        //{
        //    try {
        //        using var en = new MMDeviceEnumerator();
        //        var usb = en.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
        //                    .FirstOrDefault(IsUsbDevice);
        //        if (usb == null)
        //            return false;

        //        SetState(AudioRouteState.UsbActive, usb.FriendlyName);
        //        Apply(usb);
        //        MediaPlayer.Volume = 2;
        //        return true;
        //    }
        //    catch (Exception ex) {
        //        SetState(AudioRouteState.Error, ex.Message);
        //        return false;
        //    }
        //}

        public static bool SwitchToUsb()
        {
            try {
                using var en = new MMDeviceEnumerator();

                // 모든 렌더링 장치 + 상태 로그
                var list = en.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.All).ToList();
                foreach (var d in list)
                    Console.WriteLine($"[USB-CAND] {d.FriendlyName} | {d.State}");

                // 이름만 USB 필터로 잡기
                var usb = list.FirstOrDefault(IsUsbDevice);
                if (usb == null)
                    return false;   // 여기서만 false 리턴

                SetState(AudioRouteState.UsbActive, usb.FriendlyName);
                Apply(usb);
                MediaPlayer.Volume = 2;
                return true;
            }
            catch (Exception ex) {
                SetState(AudioRouteState.Error, ex.Message);
                return false;
            }
        }

        /// <summary>강제 전환: 스피커(콘솔)로 전환(둘 다 연결된 상태 가정).</summary>
        public static bool SwitchToSpeaker()
        {
            try {
                var spk = GetDefaultSpeaker();
                if (spk == null)
                    return false;

                SetState(AudioRouteState.Idle, spk.FriendlyName);
                Apply(spk);
                MediaPlayer.Volume = 2;
                return true;
            }
            catch (Exception ex) {
                SetState(AudioRouteState.Error, ex.Message);
                return false;
            }
        }

        /// <summary>토글 전환: 현재 출력이 USB면 스피커로, 아니면 USB로.</summary>
        public static bool ToggleRoute()
        {
            try {
                string? id;
                lock (_lock) { id = _currentDeviceId; }

                using var en = new MMDeviceEnumerator();
                MMDevice? current = null;
                if (!string.IsNullOrEmpty(id)) {
                    try { current = en.GetDevice(id!); }
                    catch { current = null; }
                }

                if (current != null && IsUsbDevice(current))
                    return SwitchToSpeaker();
                else
                    return SwitchToUsb();
            }
            catch (Exception ex) {
                SetState(AudioRouteState.Error, ex.Message);
                return false;
            }
        }
    }
}

