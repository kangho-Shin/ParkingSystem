using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace APSMain.BaseClass
{
    public static class MagnifierOverlay
    {
        private static LensForm? _lens;
        public static bool IsRunning => _lens != null && !_lens.IsDisposed;

        public static void Start()
        {
            if (IsRunning)
                return;
            _lens = new LensForm();
            _lens.Show();
        }

        public static void Stop()
        {
            if (!IsRunning)
                return;
            try { _lens!.Close(); }
            catch { }
            _lens = null;
        }

        // ==================== 내부 폼 ====================
        private sealed class LensForm : Form
        {
            // 사용자 설정
            private int _diameter = 300;
            private float _zoom = 2.0f;

            // 내부
            private readonly System.Windows.Forms.Timer _timer = new();
            private bool _freeze = false;
            private volatile bool _injecting = false;
            private bool _armedDown = false;
            private IntPtr _hwndMag = IntPtr.Zero;
            private volatile int _lensL, _lensT, _lensW, _lensH;

            // Win32/Mag
            private const string WC_MAGNIFIER = "Magnifier";
            private const int WS_CHILD = 0x40000000;
            private const int WS_VISIBLE = 0x10000000;
            private const int GWL_EXSTYLE = -20;
            private const int WS_EX_TRANSPARENT = 0x00000020;
            private const uint SWP_NOSIZE = 0x0001, SWP_NOMOVE = 0x0002, SWP_NOACTIVATE = 0x0010, SWP_SHOWWINDOW = 0x0040, SWP_HIDEWINDOW = 0x0080, SWP_NOZORDER = 0x0004, SWP_FRAMECHANGED = 0x0020;
            private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

            // 메시지 상수
            private const int WM_NCHITTEST = 0x0084;
            private const int HTTRANSPARENT = -1;
            private const int WM_LBUTTONDOWN = 0x0201, WM_LBUTTONUP = 0x0202;
            private const int WM_RBUTTONDOWN = 0x0204, WM_MBUTTONDOWN = 0x0207;

            [StructLayout(LayoutKind.Sequential)] private struct RECT { public int Left, Top, Right, Bottom; }
            [StructLayout(LayoutKind.Sequential)]
            private struct MAGTRANSFORM
            {
                public float m00, m01, m02;
                public float m10, m11, m12;
                public float m20, m21, m22;
            }
            [StructLayout(LayoutKind.Sequential)] private struct POINT { public int X, Y; }
            [StructLayout(LayoutKind.Sequential)] private struct MSLLHOOKSTRUCT { public POINT pt; public uint mouseData, flags, time; public IntPtr dwExtraInfo; }

            [DllImport("Magnification.dll", ExactSpelling = true)] static extern bool MagInitialize();
            [DllImport("Magnification.dll", ExactSpelling = true)] static extern bool MagUninitialize();
            [DllImport("Magnification.dll", ExactSpelling = true)] static extern bool MagSetWindowSource(IntPtr hwnd, RECT rect);
            [DllImport("Magnification.dll", ExactSpelling = true)] static extern bool MagSetWindowTransform(IntPtr hwnd, ref MAGTRANSFORM pTransform);
            [DllImport("Magnification.dll", ExactSpelling = true)] static extern bool MagSetWindowFilterList(IntPtr hwnd, int dwFilterMode, int count, IntPtr[] pHWND);

            private const int MW_FILTERMODE_EXCLUDE = 0;

            [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            static extern IntPtr CreateWindowExW(int dwExStyle, string lpszClassName, string lpszWindowName, int style,
                                                 int x, int y, int width, int height,
                                                 IntPtr hWndParent, IntPtr hMenu, IntPtr hInst, IntPtr pvParam);
            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            static extern IntPtr GetModuleHandle(string? lpModuleName);
            [DllImport("user32.dll", SetLastError = true)]
            static extern bool DestroyWindow(IntPtr hWnd);
            [DllImport("user32.dll", SetLastError = true)]
            static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
            [DllImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
            static extern int GetWindowLong32(IntPtr hWnd, int nIndex);
            [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
            static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);
            [DllImport("user32.dll")] static extern int GetCursorPos(out POINT pt);
            [DllImport("user32.dll")] static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);
            [DllImport("user32.dll")] static extern bool UpdateWindow(IntPtr hWnd);

            // mouse_event 전용
            private static class NativeMouse
            {
                public const uint MOUSEEVENTF_LEFTDOWN = 0x0002, MOUSEEVENTF_LEFTUP = 0x0004;
                [DllImport("user32.dll", SetLastError = false)]
                public static extern void mouse_event(uint dwFlags, int dx, int dy, int dwData, UIntPtr dwExtraInfo);
            }

            // LL Mouse Hook
            private static IntPtr _hHook = IntPtr.Zero;
            private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
            private static LowLevelMouseProc? _hookProc; // GC 보호
            [DllImport("user32.dll")] static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);
            [DllImport("user32.dll")] static extern bool UnhookWindowsHookEx(IntPtr hhk);
            [DllImport("user32.dll")] static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
            [DllImport("kernel32.dll")] static extern IntPtr GetModuleHandle(IntPtr lpModuleName);
            private const int WH_MOUSE_LL = 14;

            public LensForm()
            {
                ShowInTaskbar = false;
                FormBorderStyle = FormBorderStyle.None;
                StartPosition = FormStartPosition.Manual;
                TopMost = true;
                Width = _diameter;
                Height = _diameter;

                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
                UpdateRoundRegion();

                _timer.Interval = 16;
                _timer.Tick += (_, __) =>
                {
                    if (!_freeze) {
                        var p = Cursor.Position;
                        Location = new Point(p.X - Width / 2, p.Y - Height / 2);
                    }
                    _lensL = Left;
                    _lensT = Top;
                    _lensW = Width;
                    _lensH = Height;
                    UpdateMagnifierSource();
                };
                _timer.Start();

                Shown += (_, __) =>
                {
                    SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
                    if (_hHook == IntPtr.Zero) {
                        _hookProc = HookCallback;
                        _hHook = SetWindowsHookEx(WH_MOUSE_LL, _hookProc, GetModuleHandle(IntPtr.Zero), 0);
                    }
                };
            }

            protected override void OnHandleCreated(EventArgs e)
            {
                base.OnHandleCreated(e);
                if (!MagInitialize()) { Close(); return; }

                _hwndMag = CreateWindowExW(0, WC_MAGNIFIER, "Mag", WS_CHILD | WS_VISIBLE, 0, 0, Width, Height,
                                           Handle, IntPtr.Zero, GetModuleHandle(null), IntPtr.Zero);
                if (_hwndMag == IntPtr.Zero) { MagUninitialize(); Close(); return; }

                MagSetWindowFilterList(_hwndMag, MW_FILTERMODE_EXCLUDE, 1, new[] { this.Handle });
                ApplyZoomTransform();
                UpdateMagnifierSource();

                // 자식 Magnifier도 히트 패스스루
                int exMag = GetWindowLong32(_hwndMag, GWL_EXSTYLE);
                SetWindowLong32(_hwndMag, GWL_EXSTYLE, exMag | WS_EX_TRANSPARENT);
                SetWindowPos(_hwndMag, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
            }

            protected override void OnFormClosed(FormClosedEventArgs e)
            {
                try { if (_hHook != IntPtr.Zero) { UnhookWindowsHookEx(_hHook); _hHook = IntPtr.Zero; } }
                catch { }
                try { if (_hwndMag != IntPtr.Zero) { DestroyWindow(_hwndMag); _hwndMag = IntPtr.Zero; } }
                catch { }
                try { MagUninitialize(); }
                catch { }
                base.OnFormClosed(e);
            }

            protected override CreateParams CreateParams
            {
                get
                {
                    var cp = base.CreateParams;
                    const int WS_EX_TOOLWINDOW = 0x00000080;
                    const int WS_EX_NOACTIVATE = 0x08000000;
                    cp.ExStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
                    return cp;
                }
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_NCHITTEST) { m.Result = (IntPtr)HTTRANSPARENT; return; }
                base.WndProc(ref m);
            }

            protected override void OnResize(EventArgs e)
            {
                base.OnResize(e);
                UpdateRoundRegion();
                ApplyZoomTransform();
            }

            private void UpdateRoundRegion()
            {
                using var gp = new GraphicsPath();
                gp.AddEllipse(new Rectangle(0, 0, Width, Height));
                Region = new Region(gp);
            }

            private void ApplyZoomTransform()
            {
                if (_hwndMag == IntPtr.Zero)
                    return;
                var m = new MAGTRANSFORM { m00 = _zoom, m11 = _zoom, m22 = 1 };
                MagSetWindowTransform(_hwndMag, ref m);
                SetWindowPos(_hwndMag, IntPtr.Zero, 0, 0, Width, Height, SWP_NOACTIVATE);
            }

            private void UpdateMagnifierSource()
            {
                if (_hwndMag == IntPtr.Zero)
                    return;
                GetCursorPos(out var center);
                float w = _diameter / _zoom, h = _diameter / _zoom;
                int left = (int)Math.Floor(center.X - w / 2f);
                int top = (int)Math.Floor(center.Y - h / 2f);
                int right = left + (int)Math.Ceiling(w);
                int bottom = top + (int)Math.Ceiling(h);

                var vb = SystemInformation.VirtualScreen;
                if (left < vb.Left) { int d = vb.Left - left; left += d; right += d; }
                if (top < vb.Top) { int d = vb.Top - top; top += d; bottom += d; }
                if (right > vb.Right) { int d = right - vb.Right; right -= d; left -= d; }
                if (bottom > vb.Bottom) { int d = bottom - vb.Bottom; bottom -= d; top -= d; }

                var src = new RECT { Left = left, Top = top, Right = right, Bottom = bottom };
                MagSetWindowSource(_hwndMag, src);
            }

            // === Hook ===
            private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
            {
                if (nCode < 0)
                    return CallNextHookEx(_hHook, nCode, wParam, lParam);

                int msg = wParam.ToInt32();
                if (msg != WM_LBUTTONDOWN && msg != WM_LBUTTONUP && msg != WM_RBUTTONDOWN && msg != WM_MBUTTONDOWN)
                    return CallNextHookEx(_hHook, nCode, wParam, lParam);

                var info = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

                // 주입 중이면 패스
                if (_injecting)
                    return CallNextHookEx(_hHook, nCode, wParam, lParam);

                // 우클릭/미들클릭은 freeze와 무관하게 항상 처리
                if (msg == WM_RBUTTONDOWN) { _freeze = !_freeze; return (IntPtr)1; }
                if (msg == WM_MBUTTONDOWN) { BeginInvoke((Action)(() => Close())); return (IntPtr)1; }

                // freeze면 패스
                if (_freeze)
                    return CallNextHookEx(_hHook, nCode, wParam, lParam);

                // 렌즈 원 내부만 좌클릭 개입
                int l = _lensL, t = _lensT, w = _lensW, h = _lensH;
                if (!PointInCircle(info.pt.X, info.pt.Y, l, t, w, h))
                    return CallNextHookEx(_hHook, nCode, wParam, lParam);

                if (msg == WM_LBUTTONDOWN) { _armedDown = true; return (IntPtr)1; }
                if (msg == WM_LBUTTONUP && _armedDown) {
                    _armedDown = false;
                    BeginInvoke((Action)HideAndInjectClick);
                    return (IntPtr)1;
                }
                return CallNextHookEx(_hHook, nCode, wParam, lParam);
            }

            private static bool PointInCircle(int x, int y, int l, int t, int w, int h)
            {
                if (w <= 0 || h <= 0)
                    return false;
                if (x < l || x > l + w || y < t || y > t + h)
                    return false;
                double cx = l + w / 2.0, cy = t + h / 2.0;
                double dx = x - cx, dy = y - cy;
                double r = Math.Min(w, h) / 2.0;
                return (dx * dx + dy * dy) <= (r * r);
            }

            // mouse_event만 사용 (홀드 ↑, 복귀 지연)
            private void HideAndInjectClick()
            {
                if (_injecting)
                    return;
                _injecting = true;
                bool prevFreeze = _freeze;

                try {
                    _freeze = true;

                    // 숨김
                    SetWindowPos(_hwndMag, IntPtr.Zero, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE | SWP_HIDEWINDOW);
                    SetWindowPos(Handle, IntPtr.Zero, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE | SWP_HIDEWINDOW);
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(12);

                    // 클릭 주입 (mouse_event)
                    NativeMouse.mouse_event(NativeMouse.MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                    System.Threading.Thread.Sleep(90); // 80~120ms 권장
                    NativeMouse.mouse_event(NativeMouse.MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);

                    // 복귀 지연
                    var restoreTimer = new System.Windows.Forms.Timer { Interval = 30 };
                    restoreTimer.Tick += (s, e) =>
                    {
                        restoreTimer.Stop();
                        restoreTimer.Dispose();

                        SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
                        SetWindowPos(_hwndMag, IntPtr.Zero, 0, 0, Width, Height, SWP_NOACTIVATE | SWP_SHOWWINDOW);

                        UpdateMagnifierSource();
                        InvalidateRect(_hwndMag, IntPtr.Zero, false);
                        UpdateWindow(_hwndMag);

                        _freeze = prevFreeze;
                        _injecting = false;
                    };
                    restoreTimer.Start();
                }
                catch {
                    _freeze = prevFreeze;
                    _injecting = false;
                    throw;
                }
            }
        }
    }
}
