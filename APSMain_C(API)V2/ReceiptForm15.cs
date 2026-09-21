using APSMain.BaseClass;
using APSMain.TTSLib;

namespace APSMain
{
    public partial class ReceiptForm15 : Form, ISubFormResult<string>, IActiveForm
    {
        public ZoomWrapper? _zoomWrap;
        private ContrastToggler _contrast => APSConfig.Contrast;
        public FormResult Result { get; private set; } = FormResult.FormCancel;
        public string ResultData { get; private set; } = "";
        public Action<Form, Keys, int, int>? RouteArrow { get; set; }
        private MainForm15? _mainForm;

        private System.Threading.Timer? _autoCloseTimer;
        private volatile bool _isClosing;
        private int _formGeneration;
        private int _AutoCancleTime = 0;
        private int lastIndex = 0;
        private bool _parentZoom = false;

        public ReceiptForm15()
        {
            InitializeComponent();
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            this.UpdateStyles();

            _mainForm = APSConfig.mainForm15;
            if (_mainForm != null) {
                RouteArrow = _mainForm.ProcessArrow;
            }

            _parentZoom = APSConfig.isZoomed;
        }

        private void ReceiptForm15_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isClosing) { return; }

            APSConfig.ContrastChanged -= OnContrastChanged;
            if (_zoomWrap != null) {
                _zoomWrap.DetachDragEvents(pZoomContent);
            }
            
            StopAutoCloseTimer();

            _isClosing = true;
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            StopAutoCloseTimer();
            base.OnHandleDestroyed(e);
        }

        private void PauseAutoCloseTimer()
        {
            var timer = _autoCloseTimer;
            if (timer != null) {
                try {
                    timer.Change(Timeout.Infinite, Timeout.Infinite);
                }
                catch { }
            }
        }

        private void StopAutoCloseTimer()
        {
            var timer = Interlocked.Exchange(ref _autoCloseTimer, null);
            if (timer != null) {
                try { timer.Dispose(); }
                catch { }
            }
        }

        private void StartAutoCloseTimer(int seconds)
        {
            StopAutoCloseTimer();

            _isClosing = false;
            _AutoCancleTime = seconds;
            int currentGeneration = ++_formGeneration;

            var host = APSConfig.FormHost;
            if (host == null || host.IsDisposed || !host.IsHandleCreated)
                return;

            _autoCloseTimer = new System.Threading.Timer(_ =>
            {
                if (_isClosing)
                    return;

                if (currentGeneration != _formGeneration)
                    return;

                int remain = Interlocked.Decrement(ref _AutoCancleTime);
                if (remain < 0)
                    remain = 0;

                try {
                    if (host.IsDisposed || !host.IsHandleCreated)
                        return;

                    host.BeginInvoke((Action)(() =>
                    {
                        if (_isClosing || IsDisposed || Disposing || !IsHandleCreated)
                            return;

                        if (currentGeneration != _formGeneration)
                            return;

                        lbCloseTime.Text = $"{remain / 60:D2}:{remain % 60:D2}";

                        if (remain <= 0) {
                            StopAutoCloseTimer();

                            if (!_isClosing && !IsDisposed && IsHandleCreated)
                                btnHome_Click(this, EventArgs.Empty);
                        }
                    }));
                }
                catch {
                }
            }, null, 0, 1000);
        }

        private void ReceiptForm15_Load(object sender, EventArgs e)
        {
            //_mainForm?.NormalZoomMode();
            _zoomWrap = new ZoomWrapper(pZoomContent);

            _mainForm?.lelMessageChange(5);
            if (APSConfig.APSMODE == 0 ) {
                _mainForm?.PlaySoundFile($"credit03_10.mp3", AudioRouteState.Dual, 1);
            }
            _mainForm?.PlaySoundFile($"Comfirm.mp3", AudioRouteState.Dual, 0);
            if (_mainForm!.CurrentState == AudioRouteState.UsbActive) {
                _mainForm?.PlaySoundFile($"main15.mp3", AudioRouteState.UsbActive, 0);
            }
            else {
                _mainForm?.PlaySoundFile($"main05.mp3", AudioRouteState.Idle, 0);
            }

            lastIndex = GetEndTabIndex();

            StartAutoCloseTimer(APSConfig.AutoCancleTime5);
        }

        #region IActionForm
        public void MoveTo(Point screenLocation)
        {
            this.Location = screenLocation;
        }

        private bool _zoomRequested = false;
        public string ZoomInOut()
        {
            if (!IsHandleCreated || !Visible)
                return "";

            if (InvokeRequired) // ★ 외부에서 불려도 동기로
                return (string)Invoke(new Func<string>(ZoomInOut));

            if (_zoomRequested)
                return "";
            _zoomRequested = true;

            try {
                _zoomWrap?.ToggleZoom();   // 동기 실행
                Invalidate();
                Update();                  // 즉시 갱신
                return _zoomWrap?.IsZoomed == true ? "축소" : "확대";
            }
            finally { _zoomRequested = false; }
        }

        public void ContrastCall()
        {
            _contrast.UpdateSnapshotValues(this);
        }

        public void OnContrastChanged(bool on)
        {
            if (IsDisposed)
                return;
            if (on)
                _contrast.ApplyHighContrast(this);
            else
                _contrast.RestoreBase(this);
        }

        public void ZoomMovedQuard(int pos)
        {
            _zoomWrap?.SnapToQuadrant(pos);
        }

        public void OnConfirm()
        {
            if (this.ActiveControl is IButtonControl btn) {
                btn.PerformClick();
            }
        }

        public void OnCancel()
        {
            if (_isClosing) { return; }

            StopAutoCloseTimer();

            Result = FormResult.FormPre;
            APSConfig.isZoomed = _parentZoom;
            _mainForm!.PlaySoundFile("btnPre1.mp3", AudioRouteState.Dual, 1);
            BeginInvoke((Action)(() => this.Close()));
        }

        public void GiveFocus()
        {
            this.Focus();

            btnPrint.Enabled = true;
            if (APSConfig.isContrast) {
                btnPrint.Image = Properties.Resources.confirm_white;
                btnPrint.BackColor = Color.FromArgb(17, 17, 17);
                btnPrint.ForeColor = Color.White;
            }
            lastIndex = GetEndTabIndex();
            btnPrint.Focus();
        }
        #endregion

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            var key = keyData & Keys.KeyCode;
            if (key is Keys.Left or Keys.Right) {
                if (this.ActiveControl is TextBoxBase or ComboBox)
                    return base.ProcessCmdKey(ref msg, keyData);

                RouteArrow?.Invoke(this, key, 0, lastIndex);
                return true;
            }
            else if (keyData is Keys.F) {           // ⏹️ 홈
                BeginInvoke((Action)(() => OnGoHome()));
                return true;
            }
            else if (keyData is Keys.Escape) {      // ✖️ 이전
                BeginInvoke((Action)(() => OnCancel()));
                return true;
            }
            else if (keyData is Keys.Enter) { // 🔘⏺️
                if (this.ActiveControl is IButtonControl btn) {
                    btn.PerformClick();
                    return true;
                }
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private int GetEndTabIndex(bool buttonsOnly = true)
        {
            int max = -1;
            void Walk(Control p)
            {
                foreach (Control c in p.Controls) {
                    if (!c.Visible || !c.Enabled) { if (c.HasChildren) Walk(c); continue; }

                    bool isTarget = c.TabStop && (!buttonsOnly || c is Button);
                    if (isTarget)
                        max = Math.Max(max, c.TabIndex);

                    if (c.HasChildren)
                        Walk(c);
                }
            }
            Walk(this);
            return max;
        }

        void ApplyFocusStyleToAllButtons(Control root, int thick = 6)
        {
            foreach (Control c in root.Controls) {
                if (c is Button b)
                    EnhanceFocus(b, thick);
                if (c.HasChildren)
                    ApplyFocusStyleToAllButtons(c, thick);
            }
        }

        private void EnhanceFocus(Button b, int thick = 6)
        {
            b.FlatStyle = FlatStyle.Standard;          // Flat의 내부 여백 이슈 피함
            //b.UseVisualStyleBackColor = true;          // 테마 배경 유지 -> 텍스트영역 안정
            b.AutoSize = false;                        // 포커스 시 크기변동 방지
            b.AutoEllipsis = false;                    // "..." 잘림 방지
            b.UseCompatibleTextRendering = true;       // 한글 잘림/측정 이슈 완화
            b.TabStop = true;
            b.TextAlign = ContentAlignment.MiddleCenter;
            b.RightToLeft = RightToLeft.No;
            b.FlatAppearance.BorderSize = thick;
            b.FlatAppearance.BorderColor = Color.White; //.FromArgb(0x33, 0x33, 0x33); 

            // 포커스 변화 → 다시 그리기만
            b.GotFocus += (s, e) =>
            {
                b.FlatAppearance.BorderSize = thick;
                if (APSConfig.isContrast)
                    b.FlatAppearance.BorderColor = Color.Yellow; // Color.FromArgb(0xFF, 0xEB, 0x3B);
                else
                    b.FlatAppearance.BorderColor = Color.Red; //Color.FromArgb(0xFF, 0xEB, 0x3B);
                b.Invalidate();
            };
            b.LostFocus += (s, e) =>
            {
                b.FlatAppearance.BorderSize = thick;
                b.FlatAppearance.BorderColor = Color.White; //.FromArgb(0x33, 0x33, 0x33); 
                b.Invalidate();
            };

            // Paint에서만 선명한 테두리 오버레이
            b.Paint += (_, e) =>
            {
                thick = b.FlatAppearance.BorderSize;
                using var p = new System.Drawing.Pen(b.FlatAppearance.BorderColor, thick);

                p.Alignment = System.Drawing.Drawing2D.PenAlignment.Inset;

                var r = b.ClientRectangle;

                e.Graphics.DrawRectangle(p, r);
            };
        }

        private void ReceiptForm15_Shown(object sender, EventArgs e)
        {
            ApplyFocusStyleToAllButtons(this, thick: 6);
            _contrast.Register(this);   // 루트(폼) 등록
            _contrast.SaveBase(this);   // 현재 상태 스냅샷 저장

            if (APSConfig.isContrast)   // 전역 모드가 이미 ON이라면 즉시 적용
                _contrast.ApplyHighContrast(this);

            APSConfig.ContrastChanged += OnContrastChanged;   // 전역 신호 구독

            if (APSConfig.isZoomed) {
                APSConfig.isZoomed = false;
                ZoomInOut();
            }
            lbCloseTime.Parent?.Controls.SetChildIndex(lbCloseTime, 0);
            btnPrint.Focus();
        }

        private void btnHome_Click(object sender, EventArgs e)
        {
            if (_isClosing) { return; }

            StopAutoCloseTimer();

            Result = FormResult.FormHome;
            ResultData = "FormHome";
            _mainForm!.PlaySoundFile("btnHome1.mp3", AudioRouteState.Dual, 1);
            BeginInvoke((Action)(() => this.Close()));
        }

        private void btnPre_Click(object sender, EventArgs e)
        {
            if (_isClosing) { return; }

            StopAutoCloseTimer();

            Result = FormResult.FormPre;
            ResultData = "FormPre";
            APSConfig.isZoomed = _parentZoom;
            _mainForm!.PlaySoundFile("btnPre1.mp3", AudioRouteState.Dual, 1);
            BeginInvoke((Action)(() => this.Close()));
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            if (_isClosing) { return; }

            StopAutoCloseTimer();

            Result = FormResult.FormHome;

            _mainForm!.ReceiptPrintStart();

            ResultData = "Print OK";
            Result = FormResult.FormHome;
            _mainForm!.PlaySoundFile("btnHome1.mp3", AudioRouteState.Dual, 1);
            BeginInvoke((Action)(() => this.Close()));
        }

        private void btn_MouseDown(object sender, MouseEventArgs e)
        {
            Button? btn = sender as Button;

            if (btn != null) {
                if (btn.Name.Equals("btnHome")) {
                    btn.Image = Properties.Resources.homepress;
                }
                else if (btn.Name.Equals("btnPre")) {
                    btn.Image = Properties.Resources.prevpress;
                }
                else if (btn.Name.Equals("btnOk")) {
                    btn.Image = Properties.Resources.confirmpress;
                }
            }
        }

        private void btn_MouseUp(object sender, MouseEventArgs e)
        {
            Button? btn = sender as Button;

            if (btn != null) {
                if (btn.Name.Equals("btnHome")) {
                    btn.Image = Properties.Resources.home;
                }
                else if (btn.Name.Equals("btnPre")) {
                    btn.Image = Properties.Resources.prev;
                }
                else if (btn.Name.Equals("btnOk")) {
                    btn.Image = Properties.Resources.confirm;
                }
            }
        }

        public void OnGoHome()
        {
            if (_isClosing) { return; }

            StopAutoCloseTimer();

            Result = FormResult.FormHome;
            _mainForm!.PlaySoundFile("btnHome1.mp3", AudioRouteState.Dual, 1);
            BeginInvoke((Action)(() => this.Close()));
        }

        public void DoActiveButton()
        {
            this.BeginInvoke((Action)(() =>
            {
                if (btnPrint.CanFocus)
                    btnPrint.Focus();
            }));
        }
    }
}
