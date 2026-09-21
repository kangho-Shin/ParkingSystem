using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace APSMain.TTSLib
{
    public enum PlayMode
    {
        SND_NONE,
        SND_NORMAL,
        SND_EXPLAIN,
        SND_SUBMENU,
        SND_USBSTART,
        SND_STOP
    }

    public static class MediaPlayer
    {
        public static event EventHandler? PlaybackCompleted; // 자연 종료 알림

        private static readonly int[] _volumeVal = { 0, 25, 50, 75, 100 };
        private static readonly object _lock = new();
        private static IWavePlayer? _outputDevice;
        private static AudioFileReader? _audioFile;
        private static string? _currentSong;
        private static int _volume = 2;

        private static readonly string filedir = Path.Combine(Application.StartupPath, "SOUNDS", APSConfig.ScreenMode.ToString());

        private static volatile bool _stopRequested = false;

        public static PlayMode _playMode { get; set; } = PlayMode.SND_NONE;

        private static VolumeSampleProvider? _volumeProvider;  // 메모리 경로 볼륨
        private static MMDevice? _activeDevice;                // 사용 중 출력 장치
        private static WaveStream? _reader;                    // 공통 리더
        private static IWaveProvider? _toDevice;               // 최종 장치 포맷 파이프
        private static MediaFoundationResampler? _resampler;   // 믹스 포맷 변환
        private static string? _mmDeviceId;                    // 지정 출력 장치 ID

        private const int DESIRED_LATENCY_MS = 120;
        private const int PREROLL_MS = 100;

        public static string CurrentSong => _currentSong ?? "";

        public static int Volume
        {
            get => _volume;
            set
            {
                lock (_lock) {
                    _volume = Math.Clamp(value, 0, 4);
                    float vol = _volumeVal[_volume] / 100f;
                    if (vol >= 1.0)
                        vol = 1.0F;
                    if (_audioFile != null)
                        _audioFile.Volume = vol;
                    if (_volumeProvider != null)
                        _volumeProvider.Volume = vol;
                }
            }
        }

        public static bool Play(string fileName)
        {
             lock (_lock) {
                try {
                    string fullPath = Path.Combine(filedir, fileName);
                    //Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} - {fullPath}");
                    if (!File.Exists(fullPath))
                        return false;

                    StopInternal();

                    _activeDevice = AcquireDeviceFromId(_mmDeviceId);
                    //NormalizeVolumes(_activeDevice);
                    _audioFile = new AudioFileReader(fullPath)
                    {
                        Volume = _volumeVal[_volume] / 100f
                    };
                    _reader = _audioFile;

                    // ISampleProvider 체인 구성(+ 프리롤)
                    ISampleProvider sample = _audioFile.ToSampleProvider();
                    var toDev = BuildToDevicePipe(sample, _activeDevice, withPreroll: PREROLL_MS > 0);

                    _outputDevice = CreateOutput(_activeDevice);
                    _outputDevice.PlaybackStopped += OutputDevice_PlaybackStopped;
                    _stopRequested = false;

                    _toDevice = toDev;
                    _outputDevice.Init(_toDevice);
                    _outputDevice.Play();

                    _currentSong = fullPath;
                    return true;
                }
                catch (Exception ex) {
                    Console.WriteLine($"[MediaPlayer] 파일 재생 에러: {fileName} {ex.Message}");
                    return false;
                }
            }
        }

        private static float _ttsPregainDb = 4f;   // ★ TTS(메모리) 전용 프리게인(dB) 기본 +9dB

        public static bool PlayMemory(byte[] wavData)
        {
            if (wavData == null || wavData.Length < 100)
                return false;

            lock (_lock) {
                try {
                    StopInternal();

                    _activeDevice = AcquireDeviceFromId(_mmDeviceId);
                    //NormalizeVolumes(_activeDevice); 

                    // WAV 리더
                    var ms = new MemoryStream(wavData, 0, wavData.Length, false, true);
                    var waveReader = new WaveFileReader(ms);
                    _reader = waveReader;

                    // float → (TTS 프리게인) → 전역 볼륨 → (프리롤) → PCM16 → (옵션)Resample
                    ISampleProvider sample = waveReader.ToSampleProvider();

                    // ★ TTS 전용 프리게인(dB → 선형)
                    float pregain = (float)Math.Pow(10.0, _ttsPregainDb / 20.0);
                    var ttsGainProvider = new VolumeSampleProvider(sample) { Volume = pregain };

                    // 전역 볼륨은 기존대로 동작
                    _volumeProvider = new VolumeSampleProvider(ttsGainProvider)
                    {
                        Volume = _volumeVal[_volume] / 100f
                    };

                    var toDev = BuildToDevicePipe(_volumeProvider, _activeDevice, withPreroll: PREROLL_MS > 0);

                    _outputDevice = CreateOutput(_activeDevice);
                    _outputDevice.PlaybackStopped += OutputDevice_PlaybackStopped;
                    _stopRequested = false;

                    _toDevice = toDev;
                    _outputDevice.Init(_toDevice);
                    _outputDevice.Play();

                    _currentSong = "[memory]";
                    _audioFile = null;
                    return true;
                }
                catch (Exception ex) {
                    Console.WriteLine($"[MediaPlayer] PlayMemory 에러: {ex.Message}");
                    return false;
                }
            }
        }

        /*
        public static bool PlayMemory(byte[] wavData)
        {
            if (wavData == null || wavData.Length < 100)
                return false;

            lock (_lock) {
                try {
                    StopInternal();

                    _activeDevice = AcquireDeviceFromId(_mmDeviceId);

                    // WAV 리더
                    var ms = new MemoryStream(wavData, 0, wavData.Length, false, true);
                    var waveReader = new WaveFileReader(ms);
                    _reader = waveReader;

                    // float → 볼륨 → (프리롤 포함) → PCM16 → (옵션)Resample
                    ISampleProvider sample = waveReader.ToSampleProvider();
                    _volumeProvider = new VolumeSampleProvider(sample) { Volume = _volumeVal[_volume] / 100f };
                    var toDev = BuildToDevicePipe(_volumeProvider, _activeDevice, withPreroll: PREROLL_MS > 0);

                    _outputDevice = CreateOutput(_activeDevice);
                    _outputDevice.PlaybackStopped += OutputDevice_PlaybackStopped;
                    _stopRequested = false;

                    _toDevice = toDev;
                    _outputDevice.Init(_toDevice);
                    _outputDevice.Play();

                    _currentSong = "[memory]";
                    _audioFile = null;
                    return true;
                }
                catch (Exception ex) {
                    Console.WriteLine($"[MediaPlayer] PlayMemory 에러: {ex.Message}");
                    return false;
                }
            }
        }
        */
        // ISampleProvider → (프리롤) → 16-bit PCM → (옵션)장치 MixFormat
        //private static IWaveProvider BuildToDevicePipe(ISampleProvider sample, MMDevice? dev, bool withPreroll)
        //{
        //    if (withPreroll) {
        //        sample = new OffsetSampleProvider(sample) { DelayBy = TimeSpan.FromMilliseconds(PREROLL_MS) };
        //    }

        //    // PCM16으로 고정(Wasapi/Resampler와 상성↑)
        //    var wave16 = new SampleToWaveProvider16(sample);
        //    IWaveProvider current = wave16;

        //    _resampler?.Dispose();
        //    _resampler = null;

        //    if (dev != null) {
        //        try {
        //            var mix = dev.AudioClient.MixFormat;
        //            var outFormat = new WaveFormat(mix.SampleRate, 16, mix.Channels);
        //            _resampler = new MediaFoundationResampler(current, dev.AudioClient.MixFormat)
        //            {
        //                ResamplerQuality = 60
        //            };
        //            current = _resampler;
        //        }
        //        catch (Exception rex) {
        //            Console.WriteLine("[MediaPlayer] Resampler 실패 -> 기본 디바이스로 폴백: " + rex.Message);
        //            _resampler?.Dispose();
        //            _resampler = null;
        //            //try { _activeDevice?.Dispose(); }
        //            //catch { }
        //            //_activeDevice = null; 
        //        }
        //    }

        //    return current;
        //}

        private static IWaveProvider BuildToDevicePipe(ISampleProvider sample, MMDevice? dev, bool withPreroll)
        {
            if (withPreroll) {
                sample = new OffsetSampleProvider(sample) { DelayBy = TimeSpan.FromMilliseconds(PREROLL_MS) };
            }

            var wave16 = new SampleToWaveProvider16(sample);
            IWaveProvider current = wave16;

            _resampler?.Dispose();
            _resampler = null;

            if (dev != null) {
                // ★ USB 장치면 Resampler 안 씀 — 그대로 WasapiOut에 물림
                var name = (dev.FriendlyName ?? "").ToLowerInvariant();
                bool isUsb = name.Contains("usb audio device") || name.Contains("usb");

                if (!isUsb) {
                    try {
                        // Realtek 등 일반 스피커만 Resampler 사용
                        var mix = dev.AudioClient.MixFormat;
                        var outFormat = new WaveFormat(mix.SampleRate, 16, mix.Channels);

                        _resampler = new MediaFoundationResampler(current, outFormat)
                        {
                            ResamplerQuality = 60
                        };
                        current = _resampler;
                    }
                    catch (Exception rex) {
                        Console.WriteLine("[MediaPlayer] Resampler 실패(일반 스피커): " + rex);
                        _resampler?.Dispose();
                        _resampler = null;
                        // ★ 더 이상 “기본 디바이스로 폴백” 이런 거 하지 말고, 그냥 현재 파이프 그대로 둔다.
                    }
                }
            }

            return current;
        }

        private static void OutputDevice_PlaybackStopped(object? sender, StoppedEventArgs e)
        {
            bool naturalEnd = false;
            bool shouldCleanup = false;

            IWavePlayer? dev = null;
            WaveStream? reader = null;
            AudioFileReader? file = null;
            IWaveProvider? toDevice = null;
            MediaFoundationResampler? resampler = null;

            lock (_lock) {
                // 자연 종료: 수동정지가 아니고 예외도 없으면 true (짧은 SFX 대응)
                if (!_stopRequested && e.Exception == null)
                    naturalEnd = true;

                if (sender is IWavePlayer devFromEvent && ReferenceEquals(devFromEvent, _outputDevice)) {
                    shouldCleanup = true;

                    try { _outputDevice!.PlaybackStopped -= OutputDevice_PlaybackStopped; }
                    catch { }

                    dev = _outputDevice;
                    reader = _reader;
                    file = _audioFile;
                    toDevice = _toDevice;
                    resampler = _resampler;

                    _outputDevice = null;
                    _reader = null;
                    _audioFile = null;
                    _toDevice = null;
                    _resampler = null;
                    _volumeProvider = null;
                    _currentSong = null;
                }
            }

            if (!shouldCleanup)
                return;

            try { dev?.Dispose(); }
            catch { }
            try { resampler?.Dispose(); }
            catch { }
            toDevice = null;
            try { reader?.Dispose(); }
            catch { }
            try { file?.Dispose(); }
            catch { }

            try { _activeDevice?.Dispose(); }
            catch { }
            _activeDevice = null;

            if (naturalEnd)
                PlaybackCompleted?.Invoke(null, EventArgs.Empty);
        }

        public static bool Pause()
        {
            lock (_lock) {
                if (_outputDevice is null)
                    return false;
                try { _outputDevice.Pause(); return true; }
                catch { return false; }
            }
        }

        public static bool Resume()
        {
            lock (_lock) {
                if (_outputDevice is null)
                    return false;
                try { _outputDevice.Play(); return true; }
                catch { return false; }
            }
        }

        public static bool Stop()
        {
            lock (_lock) { return StopInternal(); }
        }

        private static bool StopInternal()
        {
            IWavePlayer? dev = null;
            WaveStream? reader = null;
            AudioFileReader? file = null;
            IWaveProvider? toDevice = null;
            MediaFoundationResampler? resampler = null;
            MMDevice? activeDev = null;

            lock (_lock) {
                if (_outputDevice == null && _reader == null && _audioFile == null && _toDevice == null)
                    return true;

                _stopRequested = true;

                dev = _outputDevice;
                reader = _reader;
                file = _audioFile;
                toDevice = _toDevice;
                resampler = _resampler;
                activeDev = _activeDevice;

                _outputDevice = null;
                _reader = null;
                _audioFile = null;
                _toDevice = null;
                _resampler = null;
                _volumeProvider = null;
                _currentSong = null;
                _activeDevice = null;
            }

            try {
                if (dev != null) {
                    try { dev.PlaybackStopped -= OutputDevice_PlaybackStopped; }
                    catch { }
                    try { dev.Stop(); }
                    catch { }
                    dev.Dispose();
                }

                try { resampler?.Dispose(); }
                catch { }
                toDevice = null;
                try { reader?.Dispose(); }
                catch { }
                try { file?.Dispose(); }
                catch { }
                try { activeDev?.Dispose(); }
                catch { }
                return true;
            }
            catch {
                Console.WriteLine("StopInternal catch");
                return false;
            }
            finally {
                _stopRequested = false;
            }
        }

        private static MMDevice? AcquireDeviceFromId(string? id)
        {
            if (string.IsNullOrEmpty(id))
                return null;
            try {
                using var en = new MMDeviceEnumerator();
                return en.GetDevice(id); // 새 RCW 생성
            }
            catch {
                return null; // 제거/비활성 등
            }
        }

        public static void SetOutputDevice(MMDevice? dev, bool stopCurrent = true)
        {
            lock (_lock) {
                _mmDeviceId = dev?.ID;
                if (stopCurrent)
                    StopInternal();
            }
        }

        private static IWavePlayer CreateOutput(MMDevice? dev)
        {
            try {
                if (dev == null) {
                    using var en = new MMDeviceEnumerator();
                    dev = en.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
                }
                return new WasapiOut(dev, AudioClientShareMode.Shared, true, DESIRED_LATENCY_MS);
            }
            catch (Exception ex) {
                Console.WriteLine("[MediaPlayer] WasapiOut 실패 -> WaveOutEvent 폴백: " + ex.Message);
                return new WaveOutEvent { DesiredLatency = DESIRED_LATENCY_MS };
            }
        }

        private static void NormalizeVolumes(MMDevice? dev)
        {
            if (dev == null)
                return;

            try {
                var ep = dev.AudioEndpointVolume;
                if (ep.MasterVolumeLevelScalar < 0.99f)
                    ep.MasterVolumeLevelScalar = 1.0f;
                ep.Mute = false;
            }
            catch { }
        }
    }

    public enum PlayerStates
    {
        Playing,
        Paused,
        Stopped
    }

    public class PlayerStartEventArgs : EventArgs { }
    public class PlayerStopEventArgs : EventArgs { }
    public class PlayerPausedEventArgs : EventArgs { }

    public class ErrorEventArgs : EventArgs
    {
        public string Message { get; }
        public ErrorEventArgs(string msg) => Message = msg;
    }
}
