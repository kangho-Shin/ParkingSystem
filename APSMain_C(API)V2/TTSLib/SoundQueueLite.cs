using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.AxHost;

namespace APSMain.TTSLib
{
    public enum SoundKind { Text, File }

    public sealed class SoundItemLite
    {
        public SoundKind Kind { get; init; }
        public string SceneId { get; init; } = "Default";
        public string Payload { get; init; } = "";      // Text -> 문장 / File,Sfx -> 파일명(상대, SOUNDS/)
        public int Priority { get; init; } = 0;         // 높을수록 먼저
        public int DelayAfterMs { get; init; } = 0;     // 다음 항목 전 딜레이
    }

    public sealed class SoundQueueLite : IDisposable
    {
        private readonly object _lock = new();
        private readonly List<SoundItemLite> _queue = new(); // Scene별 정리 편의상 List
        private readonly OneCoreTts _tts;

        private string _activeScene = "Default";
        private Task? _worker;
        private CancellationTokenSource _runCts = new();
        private volatile bool _isPlaying;
        private readonly ManualResetEventSlim _completedEvt = new(false);

        public SoundQueueLite(OneCoreTts oneCoreTts)
        {
            _tts = oneCoreTts;
            //_tts.Init();
            //_tts.GetDefaultKoreanVoice();

            MediaPlayer.PlaybackCompleted += OnPlaybackCompleted;

            // 전용 스레드(LongRunning)로 동기 루프 실행
            _worker = Task.Factory.StartNew( WorkerLoopSync,
                                             CancellationToken.None,
                                             TaskCreationOptions.LongRunning,
                                             TaskScheduler.Default );
        }

        // ===== 외부 API =====
        public void SetScene(string sceneId)
        {
            if (string.IsNullOrWhiteSpace(sceneId))
                sceneId = "Default";
            lock (_lock) {
                _activeScene = sceneId;
                _queue.RemoveAll(x => !string.Equals(x.SceneId, _activeScene, StringComparison.Ordinal));
                MediaPlayer.Stop();    // ★ 실제 재생 컷
                _isPlaying = false;
                _completedEvt.Set();
                Monitor.PulseAll(_lock);
            }
        }

        public void EnqueueText(string sceneId, string text, int delayAfterMs = 0, int priority = 0, int ttlMs = 0)
        {
            Enqueue(new SoundItemLite
            {
                Kind = SoundKind.Text,
                SceneId = string.IsNullOrWhiteSpace(sceneId) ? "Default" : sceneId,
                Payload = text ?? "",
                DelayAfterMs = delayAfterMs,
                Priority = priority,
            });
        }

        //public void EnqueueSsml(string sceneId, string text, int delayAfterMs = 0, int priority = 0, int ttlMs = 0)
        //{
        //    Enqueue(new SoundItemLite
        //    {
        //        Kind = SoundKind.Ssml,
        //        SceneId = string.IsNullOrWhiteSpace(sceneId) ? "Default" : sceneId,
        //        Payload = text ?? "",
        //        DelayAfterMs = delayAfterMs,
        //        Priority = priority,
        //    });
        //}

        public void EnqueueFile(string sceneId, string fileName, int delayAfterMs = 0, int priority = 0)
        { 
            Enqueue(new SoundItemLite
            {
                Kind = SoundKind.File,
                SceneId = string.IsNullOrWhiteSpace(sceneId) ? "Default" : sceneId,
                Payload = fileName ?? "",
                DelayAfterMs = delayAfterMs,
                Priority = priority,
            });
        }

        public void FlushScene(string sceneId)
        {
            lock (_lock) {
                _queue.RemoveAll(x => x.SceneId == sceneId);
                MediaPlayer.Stop();    // ★
                _isPlaying = false;
                _completedEvt.Set();
                Monitor.PulseAll(_lock);
            }
        }

        public void StopAll()
        {
            lock (_lock) {
                _queue.Clear();
                MediaPlayer.Stop();    // ★
                _isPlaying = false;
                _completedEvt.Set();
                Monitor.PulseAll(_lock);
            }
        }

        private void Enqueue(SoundItemLite item)
        {
            lock (_lock) {
                _queue.RemoveAll(x => x.SceneId == item.SceneId && x.Priority < item.Priority);
                _queue.Add(item);

                if ( item.Priority > 0 ) {
                    MediaPlayer.Stop(); 
                    _isPlaying = false;
                    _completedEvt.Set();
                }
                Monitor.PulseAll(_lock);
            }
        }

        private void WorkerLoopSync()
        {
            var ct = _runCts.Token;

            while (!ct.IsCancellationRequested) {
                SoundItemLite? next = null;

                // 아이템 선정
                lock (_lock) {
                    if (_isPlaying == false) {
                        var cand = _queue.Where(x => x.SceneId == _activeScene).ToList();
                        if (cand.Count == 0) {
                            // 150ms 대기(취소 신호는 WaitHandle로 병행 감시)
                            Monitor.Wait(_lock, TimeSpan.FromMilliseconds(150));
                            _isPlaying = false;
                            continue;
                        }

                        // 우선순위 내림차순, 동일 우선은 삽입순(FIFO) 유지
                        cand.Sort((a, b) => b.Priority.CompareTo(a.Priority));
                        next = cand[0];
                        _queue.Remove(next);
                    }
                    else {
                        // 재생 중이면 잠깐 쉼
                        Monitor.Wait(_lock, TimeSpan.FromMilliseconds(50));
                        continue;
                    }
                }

                // 재생 시도
                bool started = true;
                _completedEvt.Reset();
                try {
                    switch (next!.Kind) {
                        case SoundKind.Text:
                            // 내부에서 MediaPlayer.Stop/PlayMemory 호출 → 자연 종료 시 PlaybackCompleted 발생
                            _tts.PlayTextSpeech(next.Payload, SoundKind.Text);
                            break;
                        case SoundKind.File:
                            started = MediaPlayer.Play(next.Payload); // 실패 시 false 가능
                            break;
                    }
                }
                catch {
                    started = false;
                }
                finally {
                    _isPlaying = true;
                }
                if (!started) {
                    _isPlaying = false;
                    continue;
                }

                _completedEvt.Wait();
                if (next.DelayAfterMs > 0) {
                    int remain = next.DelayAfterMs;
                    while (remain > 0 && !_isPlaying)
                    {
                        int chunk = Math.Min(50, remain);
                        if (_completedEvt.Wait(0))
                            break;
                        Thread.Sleep(chunk);
                        remain -= chunk;
                    }
                }
                _isPlaying = false;
                lock (_lock) { Monitor.PulseAll(_lock); }
            }
        }

        private async Task WorkerLoop()
        {
            var ct = _runCts.Token;
            while ( !ct.IsCancellationRequested ) {
                SoundItemLite? next = null;
                if ( _isPlaying == false ) {
                    lock (_lock) {
                        var cand = _queue.Where(x => x.SceneId == _activeScene).ToList();
                        if (cand.Count == 0) {
                            Monitor.Wait(_lock, TimeSpan.FromMilliseconds(150));
                            _isPlaying = false;
                            continue;
                        }
                        cand.Sort((a, b) => b.Priority.CompareTo(a.Priority));
                        next = cand[0];
                        _queue.Remove(next);
                    }
                    try {
                        _isPlaying = true;
                        switch (next.Kind) {
                            case SoundKind.Text:
                                _tts.PlayTextSpeech(next.Payload, SoundKind.Text);
                                break;
                            case SoundKind.File:
                                bool ret = MediaPlayer.Play(next.Payload);
                                if (!ret)
                                    _isPlaying = false;
                                break;
                        }
                    }
                    catch {
                    }
                    finally {
                        
                    }
                }
                await Task.Delay(150, ct);
            }
        }

        private void OnPlaybackCompleted(object? sender, EventArgs e)
        {
            _isPlaying = false;
            _completedEvt.Set(); 
            lock (_lock) { Monitor.PulseAll(_lock); }
        }

        public void Dispose()
        {
            try {
                _runCts.Cancel();
                _worker?.Wait(300);
            }
            catch { }
            finally {
                MediaPlayer.PlaybackCompleted -= OnPlaybackCompleted;
                _completedEvt.Dispose();
                _runCts.Dispose();
            }
        }
    }
}
