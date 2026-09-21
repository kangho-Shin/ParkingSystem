using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timer = System.Threading.Timer;

namespace APSMain.BaseClass
{
    public class MarqueeLabel : Label
    {
        private readonly List<string> _pages = new();
        private int _index = 0;

        // Threading.Timer (폼 타이머 아님)
        private Timer? _timer;
        private bool _running = false;

        // 표시/타이머 옵션
        private int _dwellMs = 2500; // 문장당 2.5초
        public int DwellMilliseconds
        {
            get => _dwellMs;
            set
            {
                _dwellMs = Math.Max(500, value);
                if (_running && _timer != null)
                    _timer.Change(_dwellMs, _dwellMs);
            }
        }

        public bool AutoStart { get; set; } = true;
        public bool Repeat { get; set; } = true;     // 마지막 → 처음으로 반복
        public bool AutoWrapWhenOverflow { get; set; } = true; // 폭 넘으면 다음 페이지로 분할

        public MarqueeLabel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
            AutoSize = false;

            // Threading.Timer: 즉시 시작하지 않음
            _timer = new Timer(OnTick, null, Timeout.Infinite, Timeout.Infinite);
        }

        protected override void OnCreateControl() { base.OnCreateControl(); Reflow(); if (AutoStart) Start(); }
        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Reflow(); }
        protected override void OnSizeChanged(EventArgs e) { base.OnSizeChanged(e); Reflow(); }
        protected override void OnFontChanged(EventArgs e) { base.OnFontChanged(e); Reflow(); }

        protected override void Dispose(bool disposing)
        {
            if (disposing) {
                _timer?.Dispose();
                _timer = null;
            }
            base.Dispose(disposing);
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            using (var bg = new SolidBrush(BackColor))
                e.Graphics.FillRectangle(bg, ClientRectangle);
            if (_pages.Count == 0)
                return;

            var flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
            string line = _pages[_index];
            var sz = TextRenderer.MeasureText(line, Font, Size.Empty, flags);

            int x = Math.Max(Padding.Left, (ClientSize.Width - sz.Width) / 2);
            int y = Math.Max(Padding.Top, (ClientSize.Height - sz.Height) / 2);

            // ① 그림자(선택)
            TextRenderer.DrawText(e.Graphics, line, Font, new Point(x + 2, y + 2),
                                  Color.FromArgb(160, 0, 0, 0), flags);

            // ② 외곽선(선택: 8방향 1~2px)
            int s = 2;
            var oc = Color.Black;
            for (int dx = -s; dx <= s; dx++)
                for (int dy = -s; dy <= s; dy++)
                    if (dx != 0 || dy != 0)
                        TextRenderer.DrawText(e.Graphics, line, Font, new Point(x + dx, y + dy), oc, flags);

            // ③ 본문
            TextRenderer.DrawText(e.Graphics, line, Font, new Point(x, y), ForeColor, flags);
        }

        /*
        protected override void OnPaint(PaintEventArgs e)
        {
            using (var bg = new SolidBrush(BackColor))
                e.Graphics.FillRectangle(bg, ClientRectangle);

            if (_pages.Count == 0)
                return;

            var flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
            string line = _pages[_index];
            var sz = TextRenderer.MeasureText(line, Font, new Size(int.MaxValue, int.MaxValue), flags);

            int x = Math.Max(Padding.Left, (ClientSize.Width - sz.Width) / 2);
            int y = Math.Max(Padding.Top, (ClientSize.Height - sz.Height) / 2);

            TextRenderer.DrawText(e.Graphics, line, Font, new Point(x, y), ForeColor, flags);
        }
        */
        // ================= 내부 로직 =================

        private void OnTick(object? _)
        {
            // 타이머 콜백은 스레드풀 → UI로 마샬링
            if (IsDisposed || !IsHandleCreated)
                return;
            try {
                BeginInvoke((Action)NextPageUI);
            }
            catch { /* 폼 종료 중이면 무시 */ }
        }

        private void NextPageUI()
        {
            if (_pages.Count == 0) {
                Stop();
                return;
            }

            if (_pages.Count == 1) {
                if (!Repeat)
                    Stop(); // 한 페이지만 있고 반복 안 하면 정지
                Invalidate();
                return;
            }

            _index = (_index + 1) % _pages.Count; // 반복
            Invalidate();
        }

        private void Reflow()
        {
            Stop();               // 일단 멈추고 재계산
            _pages.Clear();
            _index = 0;

            string src = (Text ?? string.Empty).Replace("\\n", "\n").Trim();
            if (string.IsNullOrEmpty(src)) { Invalidate(); return; }

            // \n 기준 문장 분할
            var parts = src.Split('\n');
            int availW = Math.Max(0, ClientSize.Width - Padding.Left - Padding.Right);
            var flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;

            foreach (var raw in parts) {
                var s = raw.Trim();
                if (s.Length == 0)
                    continue;

                if (!AutoWrapWhenOverflow || availW <= 0) {
                    _pages.Add(s);
                    continue;
                }

                // 한 줄에 들어가면 그대로
                if (TextRenderer.MeasureText(s, Font, new Size(int.MaxValue, int.MaxValue), flags).Width <= availW) {
                    _pages.Add(s);
                    continue;
                }

                // 폭에 맞춰 여러 "페이지(문장)"로 분할
                WrapToPages(s, availW, _pages);
            }

            Invalidate();

            // 자동 시작 규칙
            if (AutoStart && (_pages.Count > 1 || Repeat))
                Start();
        }

        private void WrapToPages(string text, int maxWidth, List<string> outPages)
        {
            var flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;

            int start = 0;
            int last = text.Length;
            int lastSpace = -1;

            for (int i = 1; i <= last; i++) {
                if (char.IsWhiteSpace(text[i - 1]))
                    lastSpace = i - 1;

                string sub = text.Substring(start, i - start);
                var sz = TextRenderer.MeasureText(sub, Font, new Size(int.MaxValue, int.MaxValue), flags);
                if (sz.Width > maxWidth) {
                    int cut = (lastSpace >= start) ? lastSpace : (i - 1);
                    if (cut <= start)
                        cut = Math.Min(i - 1, last); // 안전장치

                    outPages.Add(text.Substring(start, cut - start).TrimEnd());
                    start = cut + 1;

                    // 다음 단어 시작(연속 공백 스킵)
                    while (start < last && char.IsWhiteSpace(text[start]))
                        start++;
                    i = start + 1;
                    lastSpace = -1;
                }
            }

            if (start < last)
                outPages.Add(text.Substring(start, last - start).Trim());
        }

        // ================= 제어 메서드 =================

        public void NextStep()
        {
            // 타이머 콜백은 스레드풀 → UI로 마샬링
            if (IsDisposed || !IsHandleCreated)
                return;
            try {
                BeginInvoke((Action)NextPageUI);
            }
            catch { /* 폼 종료 중이면 무시 */ }
        }

        public void Start()
        {
            if (_timer == null)
                return;
            _running = true;
            _timer.Change(_dwellMs, _dwellMs); // 주기적
        }

        public bool Stop()
        {
            if (_timer == null)
                return false;
            _running = false;
            _timer.Change(Timeout.Infinite, Timeout.Infinite);

            if (_index == _pages.Count-1)
                return true;
            return false;
        }

        public void ResetAndStart()
        {
            Reflow();
            Start();
        }
    }
}
