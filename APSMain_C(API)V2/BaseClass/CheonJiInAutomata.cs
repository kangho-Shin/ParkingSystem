using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APSMain.BaseClass
{
    public class CheonJiInAutomata
    {
        private static readonly int[] ChoseongTable =
        {
        0, 1, 2, 3, 5, 6, 7, 9, 11, 12,
        14, 15, 16, 17, 18
    };

        private readonly StringBuilder _completedText = new StringBuilder();
        private bool _isDoubleDot = false;
        private int _chosung = -1;
        private int _jungsung = -1;
        private int _jongsung = -1;

        public string GetText()
        {
            return _completedText.ToString() + GetComposingText();
        }

        public void Clear()
        {
            _completedText.Clear();
            _chosung = -1;
            _jungsung = -1;
            _jongsung = -1;
            _isDoubleDot = false;
        }

        private int GetDisplayJungsung()
        {
            if (_jungsung == 19 && !_isDoubleDot)
                return 18;   // 천지인 입력의 ㅡ 를 실제 한글 ㅡ 로 변환

            return _jungsung;
        }

        public void ReplaceChoseong(string key)
        {
            int choseong = GetChoseongIndex(key);
            if (choseong < 0)
                return;

            _chosung = choseong;
        }

        public bool CanReplaceChoseong()
        {
            return _chosung >= 0 && _jungsung < 0 && _jongsung < 0;
        }

        public bool CanReplaceJongsung()
        {
            return _chosung >= 0 && _jungsung >= 0 && _jongsung >= 0;
        }

        public void ReplaceJongsung(string key)
        {
            int choseong = GetChoseongIndex(key);
            if (choseong < 0)
                return;

            int jongsung = GetJongsungFromChoseong(choseong);
            if (jongsung < 0)
                return;

            _jongsung = jongsung;
        }

        public void InputConsonant(string key, bool isSameCycle)
        {
            int choseong = GetChoseongIndex(key);
            if (choseong < 0)
                return;

            if (_chosung < 0) {
                _chosung = choseong;
                return;
            }

            if (_chosung >= 0 && _jungsung < 0) {
                if (isSameCycle) {
                    _chosung = choseong;
                }
                else {
                    CommitCurrent();
                    _chosung = choseong;
                }
                return;
            }

            if (_chosung >= 0 && _jungsung >= 0 && _jongsung < 0) {
                _jongsung = GetJongsungFromChoseong(choseong);
                if (_jongsung < 0) {
                    CommitCurrent();
                    _chosung = choseong;
                }
                return;
            }

            if (_chosung >= 0 && _jungsung >= 0 && _jongsung >= 0) {
                if (isSameCycle) {
                    int newJong = GetJongsungFromChoseong(choseong);
                    if (newJong >= 0)
                        _jongsung = newJong;
                }
                else {
                    CommitCurrent();
                    _chosung = choseong;
                }
                return;
            }
        }

        public void InputVowel(char vowelKey)
        {
            int newJung = GetJungsungFromCheonJiIn(vowelKey);

            if (newJung < 0)
                return;

            if (_chosung < 0 && _jungsung < 0) {
                _chosung = GetChoseongIndex("ㅇ");
                _jungsung = newJung;
                return;
            }

            if (_chosung >= 0 && _jungsung < 0) {
                _jungsung = newJung;
                return;
            }

            if (_chosung >= 0 && _jungsung >= 0 && _jongsung < 0) {
                // ㆍ + ㆍ 상태 기억
                if (_jungsung == 18 && newJung == 18) {
                    _isDoubleDot = true;
                    return;
                }

                // ㆍㆍ + ㅣ = ㅕ
                if (_isDoubleDot && _jungsung == 18 && newJung == 20) {
                    _jungsung = 6;   // ㅕ
                    _isDoubleDot = false;
                    return;
                }

                // 필요하면 같이 넣어도 됨: ㆍㆍ + ㅡ = ㅛ
                if (_isDoubleDot && _jungsung == 18 && newJung == 19) {
                    _jungsung = 12;  // ㅛ
                    _isDoubleDot = false;
                    return;
                }

                _isDoubleDot = false;

                int merged = MergeJungsung(_jungsung, newJung);
                if (merged >= 0) {
                    _jungsung = merged;
                }
                else {
                    CommitCurrent();
                    _chosung = GetChoseongIndex("ㅇ");
                    _jungsung = newJung;
                }
                return;
            }

            if (_chosung >= 0 && _jungsung >= 0 && _jongsung >= 0) {
                int nextChoseong = GetChoseongFromJongsung(_jongsung);
                CommitWithoutReset();

                _chosung = nextChoseong >= 0 ? nextChoseong : GetChoseongIndex("ㅇ");
                _jungsung = newJung;
                _jongsung = -1;
            }
        }

        public void Backspace()
        {
            if (_jongsung >= 0) {
                _jongsung = -1;
                return;
            }

            if (_jungsung >= 0) {
                if (_isDoubleDot) {
                    _isDoubleDot = false;
                    return;
                }

                int split = SplitJungsung(_jungsung);
                if (split >= 0)
                    _jungsung = split;
                else
                    _jungsung = -1;
                return;
            }

            if (_chosung >= 0) {
                _chosung = -1;
                return;
            }

            if (_completedText.Length > 0)
                _completedText.Length--;
        }

        public void Commit()
        {
            CommitCurrent();
        }

        private void CommitCurrent()
        {
            string text = GetComposingText();
            if (!string.IsNullOrEmpty(text))
                _completedText.Append(text);

            _chosung = -1;
            _jungsung = -1;
            _jongsung = -1;
            _isDoubleDot = false;
        }

        private void CommitWithoutReset()
        {
            int displayJung = GetDisplayJungsung();
            string text = BuildSyllable(_chosung, displayJung, -1);
            if (!string.IsNullOrEmpty(text))
                _completedText.Append(text);
        }

        private string GetComposingText()
        {
            if (_chosung < 0 && _jungsung < 0)
                return string.Empty;

            if (_chosung >= 0 && _jungsung < 0)
                return GetCompatibilityConsonant(_chosung).ToString();

            if (_chosung >= 0 && _jungsung == 18 && _jongsung < 0) {
                if (_isDoubleDot)
                    return GetCompatibilityConsonant(_chosung).ToString() + "..";

                return GetCompatibilityConsonant(_chosung).ToString() + ".";
            }

            int displayJung = GetDisplayJungsung();
            return BuildSyllable(_chosung, displayJung, _jongsung);
        }

        private static string BuildSyllable(int cho, int jung, int jong)
        {
            if (cho < 0 && jung < 0)
                return string.Empty;

            if (cho >= 0 && jung < 0)
                return GetCompatibilityConsonant(cho).ToString();

            if (cho < 0 || jung < 0)
                return string.Empty;

            int code = 0xAC00 + ((cho * 21) + jung) * 28 + Math.Max(jong, 0);
            return char.ConvertFromUtf32(code);
        }

        private static char GetCompatibilityConsonant(int cho)
        {
            return cho switch
            {
                0 => 'ㄱ',
                1 => 'ㄲ',
                2 => 'ㄴ',
                3 => 'ㄷ',
                5 => 'ㄹ',
                6 => 'ㅁ',
                7 => 'ㅂ',
                9 => 'ㅅ',
                11 => 'ㅇ',
                12 => 'ㅈ',
                14 => 'ㅊ',
                15 => 'ㅋ',
                16 => 'ㅌ',
                17 => 'ㅍ',
                18 => 'ㅎ',
                _ => ' '
            };
        }

        private static int GetChoseongIndex(string key)
        {
            return key switch
            {
                "ㄱ" => 0,
                "ㅋ" => 15,
                "ㄴ" => 2,
                "ㄷ" => 3,
                "ㅌ" => 16,
                "ㄹ" => 5,
                "ㅁ" => 6,
                "ㅂ" => 7,
                "ㅍ" => 17,
                "ㅅ" => 9,
                "ㅎ" => 18,
                "ㅇ" => 11,
                "ㅈ" => 12,
                "ㅊ" => 14,
                _ => -1
            };
        }

        private static int GetJongsungFromChoseong(int cho)
        {
            return cho switch
            {
                0 => 1,
                2 => 4,
                3 => 7,
                5 => 8,
                6 => 16,
                7 => 17,
                9 => 19,
                11 => 21,
                12 => 22,
                14 => 23,
                15 => 24,
                16 => 25,
                17 => 26,
                18 => 27,
                _ => -1
            };
        }

        private static int GetChoseongFromJongsung(int jong)
        {
            return jong switch
            {
                1 => 0,
                4 => 2,
                7 => 3,
                8 => 5,
                16 => 6,
                17 => 7,
                19 => 9,
                21 => 11,
                22 => 12,
                23 => 14,
                24 => 15,
                25 => 16,
                26 => 17,
                27 => 18,
                _ => -1
            };
        }

        private static int GetJungsungFromCheonJiIn(char key)
        {
            return key switch
            {
                'ㆍ' => 18,
                'ㅡ' => 19,
                'ㅣ' => 20,
                _ => -1
            };
        }

        private static int MergeJungsung(int oldJung, int newJung)
        {
            if (oldJung == 20 && newJung == 18)
                return 0;   // ㅣ + ㆍ = ㅏ
            if (oldJung == 18 && newJung == 20)
                return 4;   // ㆍ + ㅣ = ㅓ

            //if (oldJung == 18 && newJung == 18) return -1;  // ㆍ + ㆍ 는 여기서 처리 안함
            if (oldJung == 18 && newJung == 19)
                return 8;   // ㆍ + ㅡ = ㅗ
            if (oldJung == 19 && newJung == 18)
                return 13;  // ㅡ + ㆍ = ㅜ

            if (oldJung == 0 && newJung == 20)
                return 1;    // ㅏ + ㅣ = ㅐ
            if (oldJung == 4 && newJung == 20)
                return 5;    // ㅓ + ㅣ = ㅔ
            if (oldJung == 8 && newJung == 20)
                return 11;   // ㅗ + ㅣ = ㅚ
            if (oldJung == 13 && newJung == 20)
                return 16;  // ㅜ + ㅣ = ㅟ

            if (oldJung == 0 && newJung == 18)
                return 2;    // ㅏ + ㆍ = ㅑ
            if (oldJung == 4 && newJung == 18)
                return 6;    // ㅓ + ㆍ = ㅕ
            if (oldJung == 8 && newJung == 18)
                return 12;   // ㅗ + ㆍ = ㅛ
            if (oldJung == 13 && newJung == 18)
                return 17;  // ㅜ + ㆍ = ㅠ

            return -1;
        }

        private static int SplitJungsung(int jung)
        {
            return jung switch
            {
                1 => 0,   // ㅐ -> ㅏ
                5 => 4,   // ㅔ -> ㅓ
                11 => 8,  // ㅚ -> ㅗ
                12 => 8,  // ㅛ -> ㅗ
                0 => 20,  // ㅏ -> ㅣ
                4 => 18,  // ㅓ -> ㆍ
                8 => 18,  // ㅗ -> ㆍ
                19 => -1, // ㅡ
                20 => -1, // ㅣ
                18 => -1, // ㆍ
                _ => -1
            };
        }

        public void AppendText(string text)
        {
            CommitCurrent();

            if (!string.IsNullOrEmpty(text))
                _completedText.Append(text);
        }
    }
}

