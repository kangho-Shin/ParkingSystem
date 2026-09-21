using APSMain.TTSLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Diagnostics;

namespace APSMain.BaseClass
{
    public interface IHasContrastToggler
    {
        ContrastToggler Contrast { get; }
    }

    public interface ISubFormResult<T>
    {
        FormResult Result { get; }
        T ResultData { get; }
    }

    public static class UiHost
    {
        private static IMainForm? _mainForm = null;

        private sealed class ActiveFormCloseContext<T>
        {
            public Form ShowForm { get; set; } = null!;
            public Control Host { get; set; } = null!;
            public Action<FormResult, T>? Callback { get; set; }
        }

        private static void OnActiveFormClosed<T>(object? sender, FormClosedEventArgs e)
        {
            if (sender is not Form showForm)
                return;

            if (showForm.Tag is not ActiveFormCloseContext<T> ctx)
                return;

            try {
                showForm.Tag = null;

                if (showForm is ISubFormResult<T> resultForm)
                    ctx.Callback?.Invoke(resultForm.Result, resultForm.ResultData);
                else
                    ctx.Callback?.Invoke(FormResult.FormNone, default!);

                if (APSConfig.FormStack.Contains(showForm))
                    APSConfig.FormStack.Remove(showForm);

                if (ctx.Host.Controls.Contains(showForm))
                    ctx.Host.Controls.Remove(showForm);

                showForm.Parent = null;

                //                Console.WriteLine($"[Closed] {showForm.Name} Hash={showForm.GetHashCode()} Stack={APSConfig.FormStack.Count} HostControls={ctx.Host.Controls.Count}");
            }
            finally {
                var nextTop = APSConfig.FormStack.Count > 0
                    ? APSConfig.FormStack[^1]
                    : APSConfig.ScreenMode == 15 ? APSConfig.menuForm15! : APSConfig.menuForm;

                if (nextTop != null) {
                    if (nextTop.Parent != ctx.Host) {
                        nextTop.Parent = ctx.Host;

                        if (!ctx.Host.Controls.Contains(nextTop))
                            ctx.Host.Controls.Add(nextTop);

                        nextTop.Bounds = ctx.Host.ClientRectangle;
                        nextTop.Dock = DockStyle.Fill;
                    }

                    nextTop.Show();
                    nextTop.BringToFront();
                    ctx.Host.Controls.SetChildIndex(nextTop, 0);

                    APSConfig.activeForm = nextTop as IActiveForm;

                    ctx.Host.BeginInvoke((Action)(() =>
                    {
                        try {
                            nextTop.BringToFront();
                            ctx.Host.Controls.SetChildIndex(nextTop, 0);
                            APSConfig.activeForm?.GiveFocus();
                        }
                        catch { }
                    }));
                }

                if (_mainForm != null) {
                    if (APSConfig.activeForm == APSConfig.menuForm as IActiveForm)
                        _mainForm.MainFormRecover();
                    if (APSConfig.activeForm == APSConfig.menuForm15 as IActiveForm)
                        _mainForm.MainFormRecover();
                }

                ctx.Host.ResumeLayout(true);
                ctx.Host.PerformLayout();
                ctx.Host.Refresh();

                showForm.FormClosed -= OnActiveFormClosed<T>;

                try {
                    showForm.Dispose();
                }
                catch { }
            }
        }
        
        public static void ShowActiveForm<T>(Form referForm, Form showForm, Action<FormResult, T>? callback)
        {
            _mainForm = APSConfig.ScreenMode == 15 ? APSConfig.mainForm15 : APSConfig.mainForm;

            if (showForm == null)
                return;

            var host = APSConfig.FormHost ?? throw new InvalidOperationException("FormHost 없음");

            showForm.AutoScaleMode = AutoScaleMode.None;
            showForm.TopLevel = false;
            showForm.FormBorderStyle = FormBorderStyle.None;
            showForm.StartPosition = FormStartPosition.Manual;

            host.SuspendLayout();

            showForm.Parent = host;

            if (!host.Controls.Contains(showForm))
                host.Controls.Add(showForm);

            showForm.Bounds = host.ClientRectangle;
            showForm.Dock = DockStyle.Fill;

            var closeContext = new ActiveFormCloseContext<T>
            {
                ShowForm = showForm,
                Host = host,
                Callback = callback
            };

            showForm.Tag = closeContext;
            showForm.FormClosed += OnActiveFormClosed<T>;

            APSConfig.FormStack.Add(showForm);

            //           Console.WriteLine($"[Show] {showForm.Name} Hash={showForm.GetHashCode()} Stack={APSConfig.FormStack.Count} HostControls={host.Controls.Count}");

            showForm.Show();
            showForm.Visible = true;
            showForm.BringToFront();
            host.Controls.SetChildIndex(showForm, 0);

            APSConfig.activeForm = showForm as IActiveForm;

            if (APSConfig.isContrast && APSConfig.activeForm != null)
                APSConfig.activeForm.ContrastCall();

            host.ResumeLayout(true);
            host.PerformLayout();
            host.Refresh();

            host.BeginInvoke((Action)(() =>
            {
                try {
                    showForm.Show();
                    showForm.BringToFront();
                    host.Controls.SetChildIndex(showForm, 0);
                    showForm.Refresh();
                    APSConfig.activeForm?.GiveFocus();
                }
                catch { }
            }));

            if (APSConfig.activeForm != APSConfig.menuForm as IActiveForm)
                MediaPlayer._playMode = PlayMode.SND_SUBMENU;
            if (APSConfig.activeForm != APSConfig.menuForm15 as IActiveForm)
                MediaPlayer._playMode = PlayMode.SND_SUBMENU;
        }

        public static void ResetToMenu()
        {
            _mainForm = APSConfig.ScreenMode == 15 ? APSConfig.mainForm15 : APSConfig.mainForm;

            var host = APSConfig.FormHost;
            if (host == null)
                return;

            if (host.InvokeRequired) {
                host.BeginInvoke(new Action(ResetToMenu));
                return;
            }

            if (APSConfig.FormStack == null || APSConfig.FormStack.Count <= 1)
                return;

            var menu = APSConfig.FormStack[0];
            var closeList = APSConfig.FormStack.Skip(1).Reverse().ToList();

            APSConfig.FormStack.Clear();
            APSConfig.FormStack.Add(menu);

            foreach (var form in closeList) {
                try {
                    if (form != null && !form.IsDisposed)
                        form.Close();
                }
                catch (Exception ex) {
                    Console.WriteLine(ex.Message);
                }
            }

            if (menu.Parent != host) {
                menu.Parent = host;

                if (!host.Controls.Contains(menu))
                    host.Controls.Add(menu);

                menu.Bounds = host.ClientRectangle;
                menu.Dock = DockStyle.Fill;
            }

            menu.Show();
            menu.BringToFront();
            host.Controls.SetChildIndex(menu, 0);

            APSConfig.activeForm = menu as IActiveForm;

            if (menu.IsHandleCreated)
                menu.BeginInvoke(new Action(() => APSConfig.activeForm?.GiveFocus()));

            _mainForm?.MainFormRecover();

            host.ResumeLayout(true);
            host.PerformLayout();
            host.Refresh();
        }

        public static void GoHome()
        {
            var host = APSConfig.FormHost;
            var form = APSConfig.activeForm;

            if (host == null || form == null)
                return;

            if (form == APSConfig.menuForm as IActiveForm)
                return;
            if (form == APSConfig.menuForm15 as IActiveForm)
                return;

            if (host.IsHandleCreated && !host.IsDisposed) {
                host.BeginInvoke((Action)(() => form.OnGoHome()));
            }
            else {
                form.OnGoHome();
            }
        }
    }

    static class UiPlace
    {
        // Center 패널의 "화면좌표" 사각형 (안전)
        public static Rectangle HostInScreenSafe(Panel host)
        {
            if (!host.IsHandleCreated)
                host.CreateControl();
            var rc = host.RectangleToScreen(host.ClientRectangle);
            if (rc.Width <= 0 || rc.Height <= 0)
                rc = Screen.FromControl(host).WorkingArea;
            return rc;
        }

        // 패널 영역 안으로 (X,Y) 강제 클램프
        public static Point ClampInto(Rectangle hostRc, Size formSize, Point target)
        {
            int x = Math.Max(hostRc.Left, Math.Min(target.X, hostRc.Right - formSize.Width));
            int y = Math.Max(hostRc.Top, Math.Min(target.Y, hostRc.Bottom - formSize.Height));
            return new Point(x, y);
        }

        // Center 패널 기준 절대 좌표 계산(상단/하단)
        public static Point CalcXY(Panel centerPanel, Size formSize, bool wheelchair, int topOffset, int bottomGap)
        {
            var rc = HostInScreenSafe(centerPanel);
            int x = rc.Left + (rc.Width - formSize.Width) / 2;
            int y = wheelchair ? (rc.Bottom - formSize.Height - bottomGap)
                               : (rc.Top + topOffset);
            return ClampInto(rc, formSize, new Point(x, y));
        }

        // 폼 배치(스냅). 애니 쓰려면 Top = p.Y 대신 AnimateY(form, p.Y) 호출
        public static void PlaceOverCenter(Panel centerPanel, Form form, bool wheelchair, int topOffset, int bottomGap)
        {
            var p = CalcXY(centerPanel, form.Size, wheelchair, topOffset, bottomGap);
            form.StartPosition = FormStartPosition.Manual;
            form.Left = p.X;   // X 즉시 고정
            form.Top = p.Y;   // Y 스냅
        }

        // 선택: Y 애니메이션
        public static void AnimateY(Control c, int targetY, int durationMs = 180)
        {
            int startY = c.Top;
            if (startY == targetY) { c.Top = targetY; return; }
            int elapsed = 0;
            var t = new System.Windows.Forms.Timer { Interval = 15 };
            t.Tick += (_, __) =>
            {
                elapsed += t.Interval;
                double p = Math.Min(1.0, (double)elapsed / durationMs);
                double e = 1 - (1 - p) * (1 - p); // ease-out
                c.Top = startY + (int)((targetY - startY) * e);
                if (p >= 1.0) { t.Stop(); c.Top = targetY; }
            };
            t.Start();
        }
    }
}
