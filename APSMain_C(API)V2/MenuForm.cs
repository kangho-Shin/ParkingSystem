using APSMain.BaseClass;
using APSMain.Comm;
using APSMain.TTSLib;

namespace APSMain
{
    public partial class MenuForm : Form, IActiveForm
    {
        private ContrastToggler _contrast => APSConfig.Contrast;
        public Action<Form, Keys, int, int>? RouteArrow { get; set; }
        private ZoomWrapper? _zoomWrap;
        public FormResult Result = FormResult.FormNone;
        public int lastIndex = 0;
        private MainForm? _mainForm;

        private DateTime _lastDate = DateTime.MinValue;
        private System.Windows.Forms.Timer? _timer;

        public MenuForm()
        {
            InitializeComponent();

            this.Size = new Size(1152, 864);

            _mainForm = APSConfig.mainForm;
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
            if (!IsHandleCreated || !Visible)
                return "";

            if (InvokeRequired) // �� �ܺο��� �ҷ��� �����
                return (string)Invoke(new Func<string>(ZoomInOut));

            if (_zoomRequested)
                return "";
            _zoomRequested = true;

            try {
                _zoomWrap?.ToggleZoom();   // ���� ����
                Invalidate();
                Update();                  // ��� ����
                return _zoomWrap?.IsZoomed == true ? "���" : "Ȯ��";
            }
            finally { _zoomRequested = false; }
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
        //    const int MA_ACTIVATE = 1;               // Ȱ��ȭ + Ŭ�� ����
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

        private void MenuForm_Load(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(APSConfig.PrintPort)) {
                PrinterManager.Init(APSConfig.PrintPort, APSConfig.PrintSpeed);
            }

            if ( APSConfig.BARRIERFREE == true) {
                btnMain1.Visible = true;
                if (APSConfig.JungkiMode == true) {
                    btnMain3.Visible = true;
                }
            }
            btnMain1.Theme = ButtonRole.Receipt;
            btnMain2.Theme = ButtonRole.ParkingFee;
            btnMain3.Theme = ButtonRole.SeasonPass;

            btnMain1.Text = "������";
            btnMain2.Text = "�������";
            btnMain3.Text = "�����\n����";

            _zoomWrap = new ZoomWrapper(pZoomContent);

            _timer = new System.Windows.Forms.Timer();
            _timer.Interval = 60000; // 1��
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
                    btnMain2.Location = new Point(403, btnMain2.Location.Y);
                    btnMain3.Location = new Point(706, btnMain3.Location.Y);
                }
                else {
                    // 2�� ǥ��
                    btnMain1.Visible = true;
                    btnMain2.Visible = true;
                    btnMain3.Visible = false;

                    btnMain1.Location = new Point(200, btnMain1.Location.Y);
                    btnMain2.Location = new Point(607, btnMain2.Location.Y);
                }
            }
            else {
                btnMain1.Visible = true;
                btnMain2.Visible = true;
                btnMain3.Visible = false;

                btnMain1.Location = new Point(200, btnMain1.Location.Y);
                btnMain2.Location = new Point(607, btnMain2.Location.Y);
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
                       new CarInNumForm(false),
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
                       new CarInNumForm(true),
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
            // ������ ���� ������ ������ Zoom Ʈ����
            this.BeginInvoke(() =>
            {
                var result = ZoomInOut();
                ZoomRequested?.Invoke(this, EventArgs.Empty);
            });
        }

        private void MenuForm_Shown(object sender, EventArgs e)
        {
            _contrast.Register(this);   // ��Ʈ(��) ���
            _contrast.SaveBase(this);   // ���� ���� ������ ����

            _mainForm = APSConfig.mainForm;
            if (_mainForm != null) {
                RouteArrow = _mainForm.ProcessArrow;
            }

            APSConfig.ContrastChanged += OnContrastChanged;   // ���� ��ȣ ����

            if (APSConfig.isContrast)   // ���� ��尡 �̹� ON�̶�� ��� ����
                _contrast.ApplyHighContrast(this);

            btnMain2.Focus();
        }

        public void ZoomMovedQuard(int pos)
        {
            _zoomWrap?.SnapToQuadrant(pos);
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
    }
}


/*
 *  //_zoom.ApplyZoom(tlpMain,1.0f);
            CarInNum carInNumForm = new CarInNum();

            carInNumForm.ShowDialog();
 * Scaffold-DbContext "server=192.168.0.21;user=ipims;password=!@Uparkdb1004;database=ipims" Pomelo.EntityFrameworkCore.MySql -OutputDir DbModels -ContextDir DummyContext -NoOnConfiguring -DataAnnotations -Force
 */