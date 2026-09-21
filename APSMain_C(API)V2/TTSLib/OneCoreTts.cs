using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using Windows.Media.SpeechSynthesis;

namespace APSMain.TTSLib
{
    public sealed class OneCoreTts 
    {
        private readonly object _playLock = new();
        private CancellationTokenSource? _playCts;          // 현재 작업 취소 토큰
        private Task? _currentPlayTask;
        readonly string _lang;

        public OneCoreTts(string lang = "ko-KR") { _lang = lang; }

        public void Dispose()
        {
            try { _playCts?.Cancel(); }
            catch { }
            try { _playCts?.Dispose(); }
            catch { }
            _playCts = null;

            // 재생 정지
            try { MediaPlayer.Stop(); }
            catch { }

            try { _currentPlayTask?.Dispose(); }
            catch { }
            _currentPlayTask = null;
        }

        public async Task<byte[]> SpeakToBytesAsync( string text,
                                                     double ratePercent = 0,
                                                     int volume0to100 = 75,
                                                     string? voiceId = null,
                                                     CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(text))
                text = "안녕하세요. 테스트 문장입니다.";

            using var synth = new Windows.Media.SpeechSynthesis.SpeechSynthesizer();

            // ── 보이스 선택 ─────────────────────────
            if (!string.IsNullOrWhiteSpace(voiceId)) {
                var v = Windows.Media.SpeechSynthesis.SpeechSynthesizer.AllVoices.FirstOrDefault(x => x.Id == voiceId);
                if (v != null)
                    synth.Voice = v;
            }
            else {
                var ko = Windows.Media.SpeechSynthesis.SpeechSynthesizer.AllVoices.FirstOrDefault(v =>
                    (!string.IsNullOrEmpty(v.Language) && v.Language.StartsWith("ko", StringComparison.OrdinalIgnoreCase)));
                if (ko != null)
                    synth.Voice = ko;
            }

            // ── 속도 (-50~+50% → 0.5x~1.5x) ─────────
            double rate = 1.0 + (ratePercent / 100.0);
            if (rate < 0.5)
                rate = 0.5;
            if (rate > 1.5)
                rate = 1.5;
            synth.Options.SpeakingRate = rate;

            // ── 볼륨 (0~100 → 0.2~1.0) ─────────────
            double vol = volume0to100 / 100.0;
            if (vol <= 0.0)
                vol = 1.0;
            else if (vol < 0.2)
                vol = 0.2;
            synth.Options.AudioVolume = vol;

            // ── 합성 ───────────────────────────────
            using SpeechSynthesisStream stream =
                await synth.SynthesizeTextToStreamAsync(text).AsTask(ct);

            using var input = stream.AsStreamForRead();
            using var ms = new MemoryStream();
            await input.CopyToAsync(ms, 81920, ct);

            if (ms.Length < 100)
                throw new InvalidDataException("TTS 결과가 비정상적으로 짧음");

            return ms.ToArray();
        }

/*
        public async Task<byte[]> SpeakToBytesAsync(string text, double ratePercent = 0, int volume0to100 = 75,
                                                     string? voiceId = null, CancellationToken ct = default)
        {
            var key = Environment.GetEnvironmentVariable("AZURE_SPEECH_KEY") ?? string.Empty;
            var region = "koreacentral";
            var config = SpeechConfig.FromSubscription(key, region);

            config.SpeechSynthesisLanguage = "ko-KR";
            config.SpeechSynthesisVoiceName = "ko-KR-SunHiNeural";


            config.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Riff16Khz16BitMonoPcm);//.Riff24Khz16BitMonoPcm

            // 3) 합성 → WAV 메모리 → 재생
            using var synth = new Microsoft.CognitiveServices.Speech.SpeechSynthesizer(config, (AudioConfig?)null);

            Task<SpeechSynthesisResult> task = IsSsml(text) ? synth.SpeakSsmlAsync(text) : synth.SpeakTextAsync(text);

            using var result = await task.ConfigureAwait(false);

            if (result.Reason != ResultReason.SynthesizingAudioCompleted) {
                Console.WriteLine($"TTS 실패: {result.Reason}");
                throw new Exception($"TTS 실패: {result.Reason}");
            }
            //string abc = $"{DateTime.Now.ToString("HHmmddss")}.wav";
            //Console.WriteLine($"RESULT:{result.AudioData} - FILE:{abc}");
            //File.WriteAllBytes(abc, result.AudioData);
            return result.AudioData; // 내부 소유 바이트 반환
        }
*/
        static bool IsSsml(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return false;
            var t = s.AsSpan().TrimStart();
            return t.StartsWith("<speak", StringComparison.OrdinalIgnoreCase);
        }

        public void PlayTextSpeech(string text, SoundKind ttype, CancellationToken ct = default)
        {
            /// 공백 방지
            if (string.IsNullOrWhiteSpace(text))
                text = "안녕하세요. 테스트 문장입니다.";

            CancellationTokenSource localCts;

            lock (_playLock) {
                // ① 이전 작업 즉시 취소 후 정리
                try { _playCts?.Cancel(); }
                catch { /* ignore */ }
                try { _playCts?.Dispose(); }
                catch { /* ignore */ }

                // ② 새 토큰(외부 ct와 연결)
                _playCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                localCts = _playCts;

                // ③ MediaPlayer도 즉시 정지 (잔향/겹침 방지)
                try { MediaPlayer.Stop(); }
                catch { /* ignore */ }
            }

            // ④ 새 작업 즉시 시작 (이전 작업 대기/큐잉 없음)
            _currentPlayTask = Task.Run(async () =>
            {
                try {
                    byte[] bytes;
                    if ( ttype == SoundKind.Text ) { 
                        bytes = await SpeakToBytesAsync(
                            text,
                            ratePercent: 0.8,
                            volume0to100: 75,
                            voiceId: null,
                            ct: localCts.Token
                        );
                    }
                    else {
                        return;
                    }

                    localCts.Token.ThrowIfCancellationRequested();
                    // 메모리 재생 (NAudio)
                    MediaPlayer.PlayMemory(bytes);
                }
                catch (OperationCanceledException) {
                    // 이전 호출에 의해 취소된 것: 조용히 무시
                }
                catch (Exception ex) {
                    Console.WriteLine("[OneCoreTts] PlayTextSpeech 오류: " + ex.Message);
                }
            }, CancellationToken.None);
        }
    }
}
