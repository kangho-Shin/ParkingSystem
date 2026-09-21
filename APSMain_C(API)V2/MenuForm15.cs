using APSMain.BaseClass;
using APSMain.Comm;
using APSMain.TTSLib;

namespace APSMain
{
    public partial class MenuForm15 : Form, IActiveForm
    {
        private ContrastToggler _contrast => APSConfig.Contrast;
        public Action<Form, Keys, int, int>? RouteArrow { get; set; }
        // private ZoomWrapper? _zoomWrap;
        public FormResult Result = FormResult.FormNone;
        public int lastIndex = 0;
        private MainForm15? _mainForm = null;
        private DateTime _lastDate = DateTime.MinValue;
        private System.Windows.Forms.Timer? _timer;

        public MenuForm15()
        {
            InitializeComponent();

            _mainForm = APSConfig.mainForm15;

            this.Size = new Size(1152, 864);
        }

        private void MenuForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _timer?.Stop();
            _timer = null;
            APSConfig.ContrastChanged -= OnContrastChanged;
        }

        #region IActiveForm
        public void MoveTo(Point screenLocation)
        {
            this.Location = screenLocation;
        }

        private bool _zoomRequested = false;

        public string ZoomInOut()
        {
            //if (!IsHandleCreated || !Visible)
            //    return "";

            //if (InvokeRequired) // ★ 외부에서 불려도 동기로
            //    return (string)Invoke(new Func<string>(ZoomInOut));

            //if (_zoomRequested)
            //    return "";
            //_zoomRequested = true;

            //try {
            //    _zoomWrap?.ToggleZoom();   // 동기 실행
            //    Invalidate();
            //    Update();                  // 즉시 갱신
            //    return _zoomWrap?.IsZoomed == true ? "축소" : "확대";
            //}
            //finally { _zoomRequested = false; }

            _zoomRequested = !_zoomRequested;
            return _zoomRequested == true ? "축소" : "확대";
        }

        public void OnConfirm()
        {

        }

        public void OnCancel()
        {

        }

        public void GiveFocus()
        {
            this.Focus();
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

        public void ContrastCall()
        {
            _contrast.UpdateSnapshotValues(this);
        }
        #endregion


        //protected override void WndProc(ref Message m)
        //{
        //    const int WM_MOUSEACTIVATE = 0x0021;
        //    const int MA_ACTIVATE = 1;               // 활성화 + 클릭 전달
        //    if (m.Msg == WM_MOUSEACTIVATE) { m.Result = (IntPtr)MA_ACTIVATE; return; }
        //    base.WndProc(ref m);
        //}

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            var key = keyData & Keys.KeyCode;
            if (key is Keys.Left or Keys.Right) {
                if (this.ActiveControl is TextBoxBase or ComboBox)
                    return base.ProcessCmdKey(ref msg, keyData);

                RouteArrow?.Invoke(this, key, 0, lastIndex);
                return true;
            }
            else if (keyData is Keys.Back) {    // 🔼
                MediaPlayer.Stop();
                Console.WriteLine("세모버튼 : 음성중지");
                return true;
            }
            else if (keyData is Keys.C) {       // ℹ️
                Console.WriteLine("I 버튼 : 상세설명");
                _mainForm?.voiceRepeatTimerChange(Timeout.Infinite, Timeout.Infinite);
                if (_mainForm!.UsbPlayOn) {
                    MediaPlayer._playMode = PlayMode.SND_EXPLAIN;
                    if (APSConfig.activeForm != null) {
                        if (APSConfig.activeForm.GetType() == typeof(MenuForm) ||
                            APSConfig.activeForm.GetType() == typeof(MainForm))
                            _mainForm?.PlaySoundFile("main10.mp3", AudioRouteState.UsbActive, 1);
                        else if (APSConfig.activeForm.GetType() == typeof(CarInNumForm))
                            _mainForm?.PlaySoundFile("main11.mp3", AudioRouteState.UsbActive, 1);
                        else if (APSConfig.activeForm.GetType() == typeof(CarSelectForm))
                            _mainForm?.PlaySoundFile("main13.mp3", AudioRouteState.UsbActive, 1);
                        else if (APSConfig.activeForm.GetType() == typeof(ParkCalForm))
                            ((ParkCalForm)APSConfig.activeForm).SpeechParkCalForm();
                        else if (APSConfig.activeForm.GetType() == typeof(ReceiptForm))
                            _mainForm?.PlaySoundFile("main15.mp3", AudioRouteState.UsbActive, 1);
                    }
                    return true;
                }
            }
            else if (keyData is Keys.Enter) {   // 🔘⏺️
                if (this.ActiveControl is IButtonControl btn) {
                    _mainForm!._kbArmed = true;
                    try { btn.PerformClick(); }
                    finally { _mainForm!._kbArmed = false; }
                    return true;
                }
            }
            else if (keyData is Keys.VolumeDown) {       // -
                Console.WriteLine("- 볼륨 다운 버튼");
                VolumeControl(1);
                return true;
            }
            else if (keyData is Keys.VolumeUp) {       // +
                Console.WriteLine("+ 볼륨 업 버튼");
                VolumeControl(0);
                return true;
            }
            else if (keyData is Keys.I) {       // x⬇️
                Console.WriteLine("I USB Audio In");
                AudioDeviceRouter.NotifyHeadphonePlugged(timeoutMs: 15000, pollMs: 250);
                //NotifyHeadphonePlugged();
                return true;
            }
            else if (keyData is Keys.X) {       // ⬇️
                Console.WriteLine("X USB Audio Out");
                AudioDeviceRouter.NotifyHeadphoneUnplugged(switchToSpeaker: true);
                if (APSConfig.activeForm != null) {
                    if (APSConfig.activeForm.GetType() != typeof(MenuForm)) {
                        APSConfig.activeForm.OnGoHome();
                    }
                }
                //NotifyHeadphoneUnplugged();
                return true;
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
                    if (isTarget) {
                        Button? btn = c as Button;
                        if (btn != null) {
                            _mainForm?.PlaySoundFile($"{btn.Tag}", AudioRouteState.Idle);
                        }
                        max = Math.Max(max, c.TabIndex);
                    }
                    if (c.HasChildren)
                        Walk(c);
                }
            }
            Walk(this);
            return max;
        }

        private int GetStartTabIndex(bool buttonsOnly = true)
        {
            int min = int.MaxValue;
            void Walk(Control p)
            {
                foreach (Control c in p.Controls) {
                    if (!c.Visible || !c.Enabled) { if (c.HasChildren) Walk(c); continue; }

                    bool isTarget = c.TabStop && (!buttonsOnly || c is Button);
                    if (isTarget)
                        min = Math.Min(min, c.TabIndex);

                    if (c.HasChildren)
                        Walk(c);
                }
            }
            Walk(this);
            return (min == int.MaxValue) ? -1 : min;
        }

        private void MenuForm15_Load(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(APSConfig.PrintPort)) {
                PrinterManager.Init(APSConfig.PrintPort, APSConfig.PrintSpeed);
            }

            if (APSConfig.BARRIERFREE == true) {
                btnMain1.Visible = true;
                if (APSConfig.JungkiMode == true) {
                    btnMain3.Visible = true;
                }
            }
            btnMain1.Theme = ButtonRole.Receipt;
            btnMain2.Theme = ButtonRole.ParkingFee;
            btnMain3.Theme = ButtonRole.SeasonPass;

            btnMain1.Text = "영수증";
            btnMain2.Text = "주차요금";
            btnMain3.Text = "정기권\n연장";

            _timer = new System.Windows.Forms.Timer();
            _timer.Interval = 60000; // 1분
            _timer.Tick += Timer_Tick;
            _timer.Start();

            _lastDate = DateTime.Today;

            UpdateButtons();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (APSConfig.JungkiMode == true) {
                if (DateTime.Today != _lastDate) {
                    _lastDate = DateTime.Today;
                    UpdateButtons();
                }
            }
        }

        private void UpdateButtons()
        {
            int day = DateTime.Today.Day;
            if (APSConfig.JungkiMode == true) {
                if (day > 15) {
                    btnMain1.Visible = true;
                    btnMain2.Visible = true;
                    btnMain3.Visible = true;

                    btnMain1.Location = new Point(100, btnMain1.Location.Y);
                    btnMain2.Location = new Point(450, btnMain2.Location.Y);
                    btnMain3.Location = new Point(800, btnMain3.Location.Y);
                }
                else {
                    // 2개 표시
                    btnMain1.Visible = true;
                    btnMain2.Visible = true;
                    btnMain3.Visible = false;

                    btnMain1.Location = new Point(200, btnMain1.Location.Y);
                    btnMain2.Location = new Point(699, btnMain2.Location.Y);
                }
            }
            else {
                btnMain1.Visible = true;
                btnMain2.Visible = true;
                btnMain3.Visible = false;

                btnMain1.Location = new Point(200, btnMain1.Location.Y);
                btnMain2.Location = new Point(699, btnMain2.Location.Y);
            }
            lastIndex = GetEndTabIndex();
        }

        private void btnMain1_Click(object sender, EventArgs e)
        {
            _mainForm!.ReceiptPrintStart();
        }

        private void btnMain2_Click(object sender, EventArgs e)
        {
            var mainForm = this.TopLevelControl as Form;
            if (mainForm == null)
                return;
            try {
                if (_mainForm != null) {
                    MediaPlayer._playMode = PlayMode.SND_SUBMENU;
                }

                UiHost.ShowActiveForm<string>(
                       this,
                       new CarInNumForm15(false),
                       (result, data) =>
                       {
                           Result = result;
                       });
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }

        static string? speechKey = Environment.GetEnvironmentVariable("AZURE_SPEECH_KEY") ?? string.Empty;
        static string? endpoint = Environment.GetEnvironmentVariable("https://koreacentral.api.cognitive.microsoft.com/");

        private void btnMain3_Click(object sender, EventArgs e)
        {
            var mainForm = this.TopLevelControl as Form;
            if (mainForm == null)
                return;
            try {
                if (_mainForm != null) {
                    MediaPlayer._playMode = PlayMode.SND_SUBMENU;
                }

                UiHost.ShowActiveForm<string>(
                       this,
                       new PeriodRenew15(),
                       (result, data) =>
                       {
                           Result = result;
                       });
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }

        public event EventHandler? ZoomRequested;

        public void RequestZoom()
        {
            // 렌더링 이후 안전한 시점에 Zoom 트리거
            this.BeginInvoke(() =>
            {
                var result = ZoomInOut();
                ZoomRequested?.Invoke(this, EventArgs.Empty);
            });
        }

        private void MenuForm15_Shown(object sender, EventArgs e)
        {
            _contrast.Register(this);   // 루트(폼) 등록
            _contrast.SaveBase(this);   // 현재 상태 스냅샷 저장

            _mainForm = APSConfig.mainForm15;
            if (_mainForm != null) {
                RouteArrow = _mainForm.ProcessArrow;
            }

            APSConfig.ContrastChanged += OnContrastChanged;   // 전역 신호 구독

            if (APSConfig.isContrast)   // 전역 모드가 이미 ON이라면 즉시 적용
                _contrast.ApplyHighContrast(this);

            btnMain2.Focus();
        }

        public void ZoomMovedQuard(int pos)
        {
            //_zoomWrap?.SnapToQuadrant(pos);
        }

        private void pZoomContent_MouseDown(object sender, MouseEventArgs e)
        {
            if (sender is not Button) {
                _mainForm?.PlaySoundFile("Main00.mp3", AudioRouteState.UsbActive);
            }
        }

        public void OnGoHome()
        {
        }

        public void DoActiveButton()
        {
        }

        private void btnContrast_Click(object sender, EventArgs e)
        {
            APSConfig.ToggleContrast();

            if (APSConfig.isContrast) {
                btnContrast.Text = "일반화면";
                btnContrast.Tag = "lowcontrast.mp3";
                btnContrast.IconImage = Properties.Resources.icon20;
                _mainForm?.PlaySoundFile("consthigh.mp3", AudioRouteState.Dual, 1);
            }
            else {
                btnContrast.Text = "고대비";
                btnContrast.Tag = "highcontrast.mp3";
                btnContrast.IconImage = Properties.Resources.icon21;
                _mainForm?.PlaySoundFile("constlow.mp3", AudioRouteState.Dual, 1);
            }
            GiveFocus();
        }

        private void btnZoom_Click(object sender, EventArgs e)
        {
            btnZoom.Text = ZoomInOut() ?? btnZoom.Text;
            if (btnZoom.Text.Equals("축소")) {
                btnZoom.Tag = "Zoomout.mp3";
                btnZoom.IconImage = Properties.Resources.icon31;
                _mainForm?.PlaySoundFile("zoominmsg.mp3", AudioRouteState.Dual, 1);

                MagnifierHost.Start();
            }
            else {
                btnZoom.Tag = "Zoomin.mp3";
                btnZoom.IconImage = Properties.Resources.icon30;
                _mainForm?.PlaySoundFile("zoomoutmsg.mp3", AudioRouteState.Dual, 1);
                _mainForm?.ResetEndTabIndex();
                MagnifierHost.Stop();
            }
            GiveFocus();
        }

        private void btnExplain_Click(object sender, EventArgs e)
        {
            if (_mainForm!._kbArmed == true) {
                Console.WriteLine($"{((Button)sender).Text} Click");

                int val = MediaPlayer.Volume;
                if (val < 4)
                    val = val + 1;
                else
                    val = 0;
                MediaPlayer.Volume = val;
                VolumeDisplay(val);
                _mainForm!._pressStart = 0;
            }
        }

        private void VolumeDisplay(int val)
        {
            if (val == 0) {
                btnExplain.IconImage = Properties.Resources.volume;
                btnExplain.Text = "무 음";
                _mainForm?.PlaySoundFile("Volumemin.mp3", AudioRouteState.Idle, 1);
            }
            else if (val == 1) {
                btnExplain.IconImage = Properties.Resources.volume1;
                btnExplain.Text = "볼륨 1단";
                _mainForm?.PlaySoundFile("Volume1.mp3", AudioRouteState.Idle, 1);
            }
            else if (val == 2) {
                btnExplain.IconImage = Properties.Resources.volume2;
                btnExplain.Text = "볼륨 2단";
                _mainForm?.PlaySoundFile("Volume2.mp3", AudioRouteState.Idle, 1);
            }
            else if (val == 3) {
                btnExplain.IconImage = Properties.Resources.volume3;
                btnExplain.Text = "볼륨 3단";
                _mainForm?.PlaySoundFile("Volume3.mp3", AudioRouteState.Idle, 1);
            }
            else if (val == 4) {
                btnExplain.IconImage = Properties.Resources.volume4;
                btnExplain.Text = "볼륨 4단";
                _mainForm?.PlaySoundFile("Volumemax.mp3", AudioRouteState.Idle, 1);
            }
        }

        private void VolumeControl(int upDown)
        {
            //if ( UsbPlayOn ) {
            int val = MediaPlayer.Volume;
            if (upDown == 0) {// UP
                if (val < 4)
                    val = val + 1;
                else
                    val = 0;
            }
            else {
                if (val > 0)
                    val = val - 1;
                else
                    val = 4;
            }
            VolumeDisplay(val);
            MediaPlayer.Volume = val;
            //}
        }

        private void pZoomContent_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.X > 0 && e.Y > 0) {
                if (e.X < 40 && e.Y < 40) {
                    _mainForm!._CTXMenu.Show(this, e.Location);
                }
            }
        }

        public void MenuFormRecover()
        {
            if (btnZoom.Text.Equals("축소")) {
                btnZoom.Tag = "Zoomin.mp3";
                btnZoom.IconImage = Properties.Resources.icon30;
                _mainForm?.PlaySoundFile("zoomoutmsg.mp3", AudioRouteState.Dual, 1);
                _mainForm?.ResetEndTabIndex();
                MagnifierHost.Stop();
            }

            if (APSConfig.isContrast) {
                APSConfig.ToggleContrast();
                btnContrast.Text = "고대비";
                btnContrast.Tag = "highcontrast.mp3";
                btnContrast.IconImage = Properties.Resources.icon21;
            }

            btnZoom.Tag = "Zoomin.mp3";
            btnZoom.IconImage = Properties.Resources.icon30;
        }
    }
}

