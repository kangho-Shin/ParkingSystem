using APSMain.Api.Request;
using APSMain.Api.Response;
using APSMain.BaseClass;
using APSMain.Models;
using APSMain.Tcpip;
using APSMain.TTSLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Text;
using System.Configuration;
using APSMain.Integration.EdgeService;

namespace APSMain
{
    public partial class CarInNumForm15 : Form, ISubFormResult<string>, IActiveForm
    {
        private MainForm15? _mainForm;
        
        string _carNumber = string.Empty;   // 차량 번호 저장

        public ZoomWrapper? _zoomWrap;
        private ContrastToggler _contrast => APSConfig.Contrast;

        public FormResult Result { get; private set; } = FormResult.FormCancel;
        public string ResultData { get; private set; } = "";

        public Action<Form, Keys, int, int>? RouteArrow { get; set; }

        public ClsLog? XLogClass;
        private int lastIndex = 0;

        const int MaxLen = 4;
        const string Slot = "-";

        private string _inCarNum = string.Empty;
        private bool _parentZoom = false;
        private bool _periodtype = false;

        private System.Threading.Timer? _autoCloseTimer;
        private volatile bool _isClosing;
        private int _formGeneration;
        private int _AutoCancleTime = 0;
        private bool EdgeServiceEnabled => true;

        public CarInNumForm15(bool periodtype)
        {
            InitializeComponent();

            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            this.UpdateStyles();

            _mainForm = APSConfig.mainForm15;
            if (_mainForm != null) {
                RouteArrow = _mainForm.ProcessArrow;
            }
            _periodtype = periodtype;
            _parentZoom = APSConfig.isZoomed;
        }

        private void CarInNumForm15_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isClosing) { return; }

            var old = picCarImage.Image;
            picCarImage.Image = null;
            old?.Dispose();

            APSConfig.ContrastChanged -= OnContrastChanged;
            if (_zoomWrap != null) {
                _zoomWrap.DetachDragEvents(pZoomContent);
            }

            _mainForm?.lelMessageChange(0);

            StopAutoCloseTimer();

            _isClosing = true;
        }

        private void CarInNum15_Load(object sender, EventArgs e)
        {
            XLogClass = ClsLog.Instance;
            // _mainForm?.NormalZoomMode();
            lblCarNum.Text = "";  // 차량 번호 레이블 초기화

            picCarImage.Image = new Bitmap(Properties.Resources.carImage);

            _zoomWrap = new ZoomWrapper(pZoomContent);

            panCarNum.Text = "";
            ParkCache.Clear(); // 캐시 초기화

            if (_mainForm != null) {
                _mainForm.lelMessageChange(2);
                if (_mainForm!.CurrentState == AudioRouteState.UsbActive) {
                    _mainForm.PlaySoundFile($"main11.mp3", AudioRouteState.UsbActive, 1);
                }
                else {
                    _mainForm.PlaySoundFile($"main01.mp3", AudioRouteState.Dual, 1);
                }
            }
            btnOk.Enabled = false;
            if (APSConfig.isContrast) {
                btnOk.Image = Properties.Resources.confirm_gray;
                btnOk.BackColor = SystemColors.Control;
                btnOk.ForeColor = SystemColors.ControlText;
            }
            lastIndex = GetEndTabIndex();

            InitPlateLabel();
            btnNum1.Focus();

            StartAutoCloseTimer(APSConfig.AutoCancleTime1);
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
            BeginInvoke((Action)(() => this.Close()));  // 폼 닫기
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
            else if (keyData is Keys.A) {           // * 초기화
                ClearAll();
                lblCarNum.Invalidate();
                if (_mainForm!.CurrentState == AudioRouteState.UsbActive) {
                    _mainForm?.PlaySoundFile("DEL1.mp3", AudioRouteState.UsbActive, 1);
                }
                else {
                    _mainForm?.PlaySoundFile("DEL0.mp3", AudioRouteState.Idle, 1);
                }
                panCarNum.Text = "";
                ParkCache.Clear();

                btnOk.Enabled = false;
                if (APSConfig.isContrast) {
                    btnOk.Image = Properties.Resources.confirm_gray;
                    btnOk.BackColor = SystemColors.Control;
                    btnOk.ForeColor = SystemColors.ControlText;
                }
                return true;
            }
            else if (keyData is Keys.S) {           // # 정정
                if (_carNumber.Length > 0) {
                    if (_mainForm!.CurrentState == AudioRouteState.UsbActive) {
                        char ab = _carNumber[_carNumber.Length - 1];
                        string numname = $"BS1{ab}.mp3";
                        _mainForm?.PlaySoundFile(numname, AudioRouteState.UsbActive, 1);
                    }
                    else {
                        _mainForm?.PlaySoundFile("BS0.mp3", AudioRouteState.Idle, 1);
                    }
                    Backspace();
                    panCarNum.Text = "";
                    btnOk.Enabled = false;
                    if (APSConfig.isContrast) {
                        btnOk.Image = Properties.Resources.confirm_gray;
                        btnOk.BackColor = SystemColors.Control;
                        btnOk.ForeColor = SystemColors.ControlText;
                    }
                    ParkCache.Clear();
                }
                else {
                    _mainForm?.PlaySoundFile("BS20.mp3", AudioRouteState.Dual, 1);
                }

                return true;
            }
            else if (keyData is Keys.Enter) { // 🔘⏺️
                if (this.ActiveControl is IButtonControl btn) {

                    btn.PerformClick();
                    return true;
                }
            }
            else if (char.IsDigit((char)keyData)) {
                if (_carNumber.Length < 4) {
                    if (_mainForm!.CurrentState == AudioRouteState.UsbActive) {
                        _mainForm?.PlaySoundFile($"{(char)keyData}1.mp3", AudioRouteState.UsbActive, 1);
                    }
                    else {
                        _mainForm?.PlaySoundFile($"{(char)keyData}0.mp3", AudioRouteState.Idle, 1);
                    }
                    NumClick((char)keyData);
                    //lblCarNum.Invalidate();
                    if (!EdgeServiceEnabled && _carNumber.Length >= 4) {
                        _ = SearchCarAsync(1);
                    }
                }
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
            //b.UseVisualStyleBackColor = true;        // 테마 배경 유지 -> 텍스트영역 안정
            b.AutoSize = false;                        // 포커스 시 크기변동 방지
            b.AutoEllipsis = false;                    // "..." 잘림 방지
            b.UseCompatibleTextRendering = true;       // 한글 잘림/측정 이슈 완화
            b.TabStop = true;
            b.TextAlign = ContentAlignment.MiddleCenter;
            b.RightToLeft = RightToLeft.No;
            b.FlatAppearance.BorderSize = thick;
            b.FlatAppearance.BorderColor = Color.White; //.FromArgb(0x33, 0x33, 0x33); 

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

        private async Task SearchCarAsync(int type)
        {
            if (type == 1) {
                await Task.Delay(1500);
                _mainForm?.PlaySoundFile("comfirm.mp3", AudioRouteState.Dual, 1);
                await Task.Delay(1000);
                bool ret = await CarParkSearch();
                if (ret) {
                    btnOk.Enabled = true;
                    if (APSConfig.isContrast) {
                        btnOk.Image = Properties.Resources.confirm_gray;
                        btnOk.BackColor = Color.FromArgb(17, 17, 17);
                        btnOk.ForeColor = Color.White;
                    }
                    btnOk.Focus();
                }
            }
            else {
                await Task.Delay(250);
                _mainForm?.PlaySoundFile("comfirm.mp3", AudioRouteState.Dual, 0);
                await Task.Delay(1000);
                bool ret = await CarParkSearch();
                if (ret) {
                    btnOk.Enabled = true;
                    if (APSConfig.isContrast) {
                        btnOk.Image = Properties.Resources.confirm_white;
                        btnOk.BackColor = Color.FromArgb(17, 17, 17);
                        btnOk.ForeColor = Color.White;
                    }
                    lastIndex = GetEndTabIndex();
                    btnOk.Focus();
                }
            }
        }

        public void ParkCalFormDisplay()
        {
            int ldmNum = _mainForm?._OLDMNum ?? 0;

            if (ParkCache.GetCarMatchCount() == 1) {
                if (ParkCache.Parkins != null && ParkCache.Parkins.Count > 0) {
                    var Parkin = ParkCache.Parkins.FirstOrDefault();
                    if (Parkin != null) {
                        var Parkinfo = new Tparkinfo
                        {
                            Sitenum = Parkin.Sitenum,
                            Groupnum = Parkin.Groupnum,
                            Carnum = Parkin.Carnum ?? "",
                            Indate = Parkin.Indate,
                            Inhour = Parkin.Inhour,
                            Inmin = Parkin.Inmin,
                            Inimage = Parkin.Inimage,
                            Indevicenum = Parkin.Indevicenum,
                            Outflag = 73, // 입차 상태
                                          // 나머지도 채울 수 있으면 채움
                        };
                        HandleExitCarDetected(Parkinfo, null, ldmNum);
                        //TTSWrapper.SpeakText($"차량 번호 {Parkin.Carnum} 를 선택하여 정산을 합니다.", 0);

                    }
                }
                if (ParkCache.Parkinfos != null && ParkCache.Parkinfos.Count > 0) {
                    var carinfo = ParkCache.Parkinfos.FirstOrDefault();
                    if (carinfo != null) {
                        HandleExitCarDetected(carinfo, null, ldmNum);
                        //TTSWrapper.SpeakText($"차량 번호 {Parkinfo.Carnum} 를 선택하여 정산을 합니다.", 0);
                    }
                }
                if (ParkCache.Periodmembers != null && ParkCache.Periodmembers.Count > 0) {
                    var Periodmember = ParkCache.Periodmembers.FirstOrDefault();
                    if (Periodmember != null) {
                        //TTSWrapper.SpeakText($"차량 번호 {Periodmember.Carnum} 를 선택하여 정산을 합니다.", 0);
                    }
                    HandleExitCarDetected(null, Periodmember, ldmNum);
                }
            }
            else {
                if (ParkCache.GetCarMatchCount() > 1) {
                    PauseAutoCloseTimer();
                    UiHost.ShowActiveForm<string>(this, new CarSelectForm15(_periodtype), (result, carNum) =>
                    {
                        Result = result;
                        ResultData = carNum;
                        if (result == FormResult.FormOk) {
                            //TTSWrapper.SpeakText($"차량 번호 {carNum} 를 선택하셨습니다.", 0);
                            //Console.WriteLine($"선택된 차량번호 : {carNum} 요금정산 시작");

                            if (ParkCache.Parkins != null && ParkCache.Parkins.Count > 0) {
                                Tparkin? carin = ParkCache.Parkins.Where(t => string.Equals(t.Carnum, carNum)).FirstOrDefault();
                                if (carin != null) {
                                    Tparkinfo carinfo = new Tparkinfo
                                    {
                                        Sitenum = carin.Sitenum,
                                        Groupnum = carin.Groupnum,
                                        Carnum = carin.Carnum ?? "",
                                        Indate = carin.Indate,
                                        Inhour = carin.Inhour,
                                        Inmin = carin.Inmin,
                                        Inimage = carin.Inimage,
                                        Indevicenum = carin.Indevicenum,
                                        Outflag = 73,

                                    };
                                    //TTSWrapper.SpeakText($"차량 번호 {carinfo.Carnum} 를 선택하여 정산을 합니다.", 0);
                                    HandleExitCarDetected(carinfo, null, ldmNum);
                                }
                            }
                            if (ParkCache.Parkinfos != null && ParkCache.Parkinfos.Count > 0) {
                                Tparkinfo? carinfo = ParkCache.Parkinfos.Where(t => t.Carnum.Equals(carNum)).FirstOrDefault();
                                if (carinfo != null) {
                                    //TTSWrapper.SpeakText($"차량 번호 {carinfo.Carnum} 를 선택하여 정산을 합니다.", 0);
                                    HandleExitCarDetected(carinfo, null, ldmNum);
                                }
                            }
                            if (ParkCache.Periodmembers != null && ParkCache.Periodmembers.Count > 0) {
                                Tperiodmember? member = ParkCache.Periodmembers.Where(t => t.Carnum1.Equals(carNum)).FirstOrDefault();
                            }
                        }
                        else if (result == FormResult.FormHome) {
                            //BeginInvoke((Action)(() => OnGoHome()));
                            //OnGoHome();
                            if (Result == FormResult.FormHome) {
                                var host = APSConfig.FormHost;
                                if (host != null && host.IsHandleCreated && !host.IsDisposed) {
                                    host.BeginInvoke((Action)(() => OnGoHome()));
                                }
                                else {
                                    OnGoHome();
                                }
                                return;
                            }
                        }
                    });
                }
            }
        }

        private void HandleExitCarDetected(Tparkinfo? carInfo, Tperiodmember? tmember, int ldmIndex)
        {
            if (this.InvokeRequired) {
                this.Invoke(new Action(() => ShowOrResetCalForm(carInfo, tmember, ldmIndex)));
            }
            else {
                ShowOrResetCalForm(carInfo, tmember, ldmIndex);
            }
        }

        private void ShowOrResetCalForm(Tparkinfo? carInfo, Tperiodmember? tmember, int ldmIndex)
        {
            if (carInfo == null && tmember == null)
                return;

            ParkCalForm15 calForm = new ParkCalForm15(ldmIndex, _periodtype);

            if (carInfo != null) {
                _mainForm!._xparkinfo.CopyFrom(carInfo!);

                string xdata = $"CX00{carInfo.Xindex}";
                if (_mainForm!.XLprSocket != null) {
                    _mainForm!.XLprSocket.SendStringPacket(xdata);
                }
            }
            if (tmember != null) { _mainForm!._xperiodmember.CopyFrom(tmember); }

            calForm._parkinfo = _mainForm!._xparkinfo;
            calForm._periodmember = _mainForm!._xperiodmember;
            PauseAutoCloseTimer();
            UiHost.ShowActiveForm<string>(
                       this,
                       calForm,
                       (result, data) =>
                       {
                           Result = result;
                           if (Result == FormResult.FormHome) {
                               //BeginInvoke((Action)(() => OnGoHome()));
                               // OnGoHome();
                               if (Result == FormResult.FormHome) {
                                   var host = APSConfig.FormHost;
                                   if (host != null && host.IsHandleCreated && !host.IsDisposed) {
                                       host.BeginInvoke((Action)(() => OnGoHome()));
                                   }
                                   else {
                                       OnGoHome();
                                   }
                                   return;
                               }
                           }
                       }
            );
        }

        private async void getImageFromFtp(string imgname)
        {

            var old = picCarImage.Image;
            picCarImage.Image = null;
            old?.Dispose();

            await ImageDownloadHelper.ShowImageAsync(picCarImage, imgname);

            /*
            if (APSConfig.FtpIp != null && APSConfig.FtpId != null && APSConfig.FtpPass != null) {
                if (imgname != null) {
                    FTPUtil ftp = new FTPUtil(APSConfig.FtpIp, APSConfig.FtpId, APSConfig.FtpPass);

                    string imgName = @$"{Application.StartupPath}Image\carImage_{DateTime.Now.Second:D2}.jpg";
                    await ftp.ftpImageDownload(imgname, imgName);

                    try {
                        var old = picCarImage.Image;
                        picCarImage.Image = null;
                        old?.Dispose();
                        picCarImage.Image = Image.FromFile(imgName);
                    }
                    catch (Exception ex) {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
            */
        }

        private void btnNum_Click(object sender, EventArgs e)
        {
            Button? btn = sender as Button;
            string number = string.Empty;

            if (_carNumber.Length >= 4)
                return;
            if (btn != null) {
                switch (btn.Name) {
                    case "btnNum1":
                        number = "1";
                        break;
                    case "btnNum2":
                        number = "2";
                        break;
                    case "btnNum3":
                        number = "3";
                        break;
                    case "btnNum4":
                        number = "4";
                        break;
                    case "btnNum5":
                        number = "5";
                        break;
                    case "btnNum6":
                        number = "6";
                        break;
                    case "btnNum7":
                        number = "7";
                        break;
                    case "btnNum8":
                        number = "8";
                        break;
                    case "btnNum9":
                        number = "9";
                        break;
                    case "btnNum0":
                        number = "0";
                        break;
                }
                if (_mainForm!.CurrentState == AudioRouteState.UsbActive) {
                    _mainForm?.PlaySoundFile($"{number}1.mp3", AudioRouteState.UsbActive, 1);
                }
                else {
                    _mainForm?.PlaySoundFile($"{number}0.mp3", AudioRouteState.Idle, 1);
                }
                NumClick(number[0]);
                if (!EdgeServiceEnabled && _carNumber.Length >= 4) {
                    _ = SearchCarAsync(2);
                }
            }
        }

        private void CarCountCheck(int nCount)
        {
            if (!IsHandleCreated || IsDisposed)
                return;

            if (InvokeRequired) {        // ★ UI 스레드로 보냄
                BeginInvoke(new Action(() => CarCountCheck(nCount)));
                return;
            }
            if (nCount > 0) {
                if (ParkCache.GetCarMatchCount() == 1) {
                    if (ParkCache.Parkins != null && ParkCache.Parkins.Count > 0) {
                        var Parkin = ParkCache.Parkins.FirstOrDefault();
                        if (Parkin != null) {
                            panCarNum.Text = Parkin.Carnum!;
                            getImageFromFtp(Parkin.Inimage!);
                            btnOk.Focus();
                            if (_mainForm!.CurrentState == AudioRouteState.UsbActive) {
                                _mainForm?.PlaySoundFile("main12.mp3", AudioRouteState.UsbActive, 0);
                            }
                            else {
                                _mainForm?.PlaySoundFile("main02.mp3", AudioRouteState.Idle, 0);
                            }
                        }
                    }
                    if (ParkCache.Parkinfos != null && ParkCache.Parkinfos.Count > 0) {
                        var Parkinfo = ParkCache.Parkinfos.FirstOrDefault();
                        if (Parkinfo != null) {
                            panCarNum.Text = Parkinfo.Carnum;
                            //DateTime baseDate = Parkinfo.Indate;
                            ////DateTime newDateTime = baseDate
                            ////    .AddHours((int)Parkinfo.Inhour!)
                            ////    .AddMinutes((int)Parkinfo.Inmin!);

                            //Parkinfo.Indate = newDateTime;

                            getImageFromFtp(Parkinfo.Inimage!);
                            btnOk.Focus();
                            if (_mainForm!.CurrentState == AudioRouteState.UsbActive) {
                                //string bfText = "<speak version=\"1.0\" xml:lang=\"ko-KR\" " +
                                //                "xmlns=\"http://www.w3.org/2001/10/synthesis\" " +
                                //                "xmlns:mstts=\"http://www.w3.org/2001/mstts\">" +
                                //                "<voice name=\"ko-KR-SunHiNeural\">" +
                                //                "<prosody rate=\"-10%\">" +
                                //                "차량번호: <say-as interpret-as=\"characters\">" + Parkinfo.Carnum + "</say-as> 입니다. " +
                                //                "맞으시면 확인 버튼을 눌러주세요" +
                                //                "</prosody>" +
                                //                "</voice>" +
                                //                "</speak>";

                                string bfText = "차량번호 " + Parkinfo.Carnum + "입니다." +
                                                "맞으시면 확인 버튼을 눌러주세요";

                                _mainForm?.PlayTextSpeech(bfText, AudioRouteState.UsbActive, 0);
                            }
                            else {
                                _mainForm?.PlaySoundFile("main02.mp3", AudioRouteState.Idle, 0);
                            }
                        }
                    }
                }
                else {
                    ParkCalFormDisplay();
                }
            }
            else {
                _mainForm?.PlaySoundFile("error.mp3", AudioRouteState.Dual, 1);
                UiHelpers.ShowMessage(this, 0, true);
                picCarImage.Image = new Bitmap(Properties.Resources.carImage);
                Task.Delay(1000).Wait();
                _mainForm?.PlaySoundFile("incar01.mp3", AudioRouteState.Dual);
            }
        }

        public async Task<bool> CarParkSearch()
        {
            var request = new CarCalcRequest
            {
                Sitenum = (short)APSConfig.Sitenum,
                Groupnum = (short)APSConfig.Groupnum,
                Devicenum = (short)APSConfig.APSNUM,
                Carnum = _carNumber,
                CarGubun = 1           //  // 1: 일반, 2: 등록 3:일반+등록(출구에서사용)
            };

            string? content = string.Empty;
            try {
                content = await RestHelper.Instance.PostAsync<CarCalcRequest>("/api/Carcalc", request);
                if (string.IsNullOrWhiteSpace(content))
                    return false;
                string pretty = JToken.Parse(content).ToString(Formatting.Indented);
                XLogClass!.SaveLogString("CNT", pretty);
            }
            catch { return false; }

            var response = JsonConvert.DeserializeObject<CarCalcResponse>(content);
            if (response == null)
                return false;

            if (response.Cars != null && response.Cars.Count > 0) {    // 일반차량
                ParkCache.Clear();
                if (response.Cars.Count == 1) {
                    CarCalcItem carItem = response.Cars[0];
                    if (carItem.Tparkinfo != null) {
                        carItem.Tparkinfo.Outimage = "";
                        ParkCache.Parkinfos.Add(carItem.Tparkinfo);

                        Console.WriteLine($"장치번호 : {carItem.Tparkinfo.Indevicenum}  차량번호 : {carItem.Tparkinfo.Carnum}");

                        if (carItem.Tdisperson != null) {
                            ParkCache.Dispersions.Add(carItem.Tdisperson);
                        }

                        if (carItem.Tdiscountinfo != null && carItem.Tdiscountinfo.Count > 0) {
                            ParkCache.Discountinfos.AddRange(carItem.Tdiscountinfo);
                        }

                        if (carItem.Tbcardinfo != null && carItem.Tbcardinfo.Count > 0) {
                            ParkCache.Bcardinfos.AddRange(carItem.Tbcardinfo);
                        }

                        Console.WriteLine($"사전할인 개수 : {ParkCache.Discountinfos.Count}");
                        Console.WriteLine($"결제정보 개수 : {ParkCache.Bcardinfos.Count}");
                    }
                }
                else {
                    foreach (var item in response.Cars) {
                        if (item.Tparkinfo != null) {
                            ParkCache.Parkinfos.Add(item.Tparkinfo);
                            ParkCache.Cars.Add(item);
                        }
                    }
                }
                CarCountCheck(ParkCache.GetCarMatchCount());
                return true;
            }
            else {
                _mainForm?.PlaySoundFile("error.mp3", AudioRouteState.Dual, 1);
                UiHelpers.ShowMessage(this, 0, true);
                picCarImage.Image = new Bitmap(Properties.Resources.carImage);
                Task.Delay(1000).Wait();
                _mainForm?.PlaySoundFile("incar01.mp3", AudioRouteState.Dual);
                return false;
            }
        }

        private void btnCancelNum_Click(object sender, EventArgs e)
        {
            //_carNumber = string.Empty;  // 차량 번호 초기화
            //lblCarNum.Text = _carNumber;  // 레이블에 차량 번호 표시  
            ClearAll();
            lblCarNum.Invalidate();
            if (_mainForm!.CurrentState == AudioRouteState.UsbActive) {
                _mainForm?.PlaySoundFile("DEL1.mp3", AudioRouteState.UsbActive, 1);
            }
            else {
                _mainForm?.PlaySoundFile("DEL0.mp3", AudioRouteState.Idle, 1);
            }
            panCarNum.Text = "";
            btnOk.Enabled = false;
            if (APSConfig.isContrast) {
                btnOk.Image = Properties.Resources.confirm_gray;
                btnOk.BackColor = SystemColors.Control;
                btnOk.ForeColor = SystemColors.ControlText;
            }
            ParkCache.Clear();
        }

        private void btnDelChar_Click(object sender, EventArgs e)
        {
            if (_carNumber.Length > 0) {
                if (_mainForm!.CurrentState == AudioRouteState.UsbActive) {
                    char ab = _carNumber[_carNumber.Length - 1];
                    string numname = $"BS1{ab}.mp3";
                    _mainForm?.PlaySoundFile(numname, AudioRouteState.UsbActive, 1);
                }
                else {
                    _mainForm?.PlaySoundFile("BS0.mp3", AudioRouteState.Idle, 1);
                }
                //_carNumber = _carNumber.Substring(0, _carNumber.Length - 1);
                //lblCarNum.Text = _carNumber;  // 레이블에 차량 번호 표시  
                Backspace();
                panCarNum.Text = "";
                btnOk.Enabled = false;
                if (APSConfig.isContrast) {
                    btnOk.Image = Properties.Resources.confirm_gray;
                    btnOk.BackColor = SystemColors.Control;
                    btnOk.ForeColor = SystemColors.ControlText;
                }
                ParkCache.Clear();
            }
            else {
                _mainForm?.PlaySoundFile("BS20.mp3", AudioRouteState.Dual, 1);
            }
        }

        private async void btnOk_Click(object sender, EventArgs e)
        {
            if (EdgeServiceEnabled) {
                await SearchEdgeServiceAsync();
                return;
            }
            if (ParkCache.GetCarMatchCount() > 0) {
                ParkCalFormDisplay();
            }
            else {
                if (_carNumber.Length == 4)
                    _mainForm?.PlaySoundFile("incar01.mp3", AudioRouteState.Dual, 1);
                else
                    _mainForm?.PlaySoundFile("incar02.mp3", AudioRouteState.Dual, 1);
            }
        }

        private void CarInNumForm15_Shown(object sender, EventArgs e)
        {
            ApplyFocusStyleToAllButtons(this, thick: 5);

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
            btnNum1.Focus();
        }

        private void CarInNumForm15_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) {
                if (ActiveControl is Button btn)
                    btn.PerformClick();
            }
        }

        private void btnHome_Click(object sender, EventArgs e)
        {
            if (_isClosing) { return; }

            StopAutoCloseTimer();

            Result = FormResult.FormHome;
            _mainForm!.PlaySoundFile("btnHome1.mp3", AudioRouteState.Dual, 1);
            BeginInvoke((Action)(() => this.Close()));
        }

        private void btnPre_Click(object sender, EventArgs e)
        {
            if (_isClosing) { return; }

            StopAutoCloseTimer();

            Result = FormResult.FormPre;
            APSConfig.isZoomed = _parentZoom;
            _mainForm!.PlaySoundFile("btnPre1.mp3", AudioRouteState.Dual, 1);
            BeginInvoke((Action)(() => this.Close()));
        }

        public void ZoomMovedQuard(int pos)
        {
            _zoomWrap?.SnapToQuadrant(pos);
        }

        private void lblCarNum_Paint(object sender, PaintEventArgs e)
        {
            var lb = (Label)sender;
            string s = _inCarNum ?? "";            // ★ null 방지
            int tracking = 3;

            var g = e.Graphics;
            var r = lb.ClientRectangle;
            var flags = TextFormatFlags.NoPadding | TextFormatFlags.NoClipping | TextFormatFlags.SingleLine;

            int totalW = -tracking;
            int[] w = new int[s.Length];
            for (int i = 0; i < s.Length; i++) {
                w[i] = TextRenderer.MeasureText(g, s[i].ToString(), lb.Font, Size.Empty, flags).Width;
                totalW += w[i] + tracking;
            }
            int h = TextRenderer.MeasureText(g, "0", lb.Font, Size.Empty, flags).Height;
            int x = r.X + (r.Width - totalW) / 2;
            int y = r.Y + (r.Height - h) / 2 - 3;

            var rc = Rectangle.Inflate(r, -2, -2); // ★ 테두리·채움은 안쪽 사각형 기준
            Color rectColor;

            if (_carNumber.Length == 4) {                   // ★ _carNumber 말고 현재 입력 길이로 판정
                using var slot = new SolidBrush(Color.FromArgb(180, 0x90, 0xA4, 0xAE));
                g.FillRectangle(slot, rc);         // ★ r 대신 rc 채움
                rectColor = Color.FromArgb(0x1B, 0x5E, 0x20);
            }
            else {
                rectColor = Color.FromArgb(0x33, 0x33, 0x33);
            }

            for (int i = 0; i < s.Length; i++) {
                if (s[i] == '-') {
                    if (APSConfig.isContrast)
                        TextRenderer.DrawText(g, s[i].ToString(), lb.Font, new Point(x, y), Color.White, flags);
                    else
                        TextRenderer.DrawText(g, s[i].ToString(), lb.Font, new Point(x, y), Color.Red, flags);
                }
                else {
                    TextRenderer.DrawText(g, s[i].ToString(), lb.Font, new Point(x, y), lb.ForeColor, flags);
                }
                x += w[i] + tracking;
            }

            //using (var pen = new Pen(rectColor, s.Length == 4 ? 4f : 2f)) {
            //    pen.Alignment = System.Drawing.Drawing2D.PenAlignment.Inset;
            //    g.DrawRectangle(pen, rc);          // ★ rc에 단일 테두리
            //}
        }

        void InitPlateLabel()
        {
            //lblCarNum.TextAlign = ContentAlignment.MiddleCenter;
            lblCarNum.Font = new Font("Consolas", 60, FontStyle.Bold); // 모노스페이스 권장
            UpdatePlate();
        }

        void UpdatePlate()
        {
            var s = new string[] { Slot, Slot, Slot, Slot };
            for (int i = 0; i < _carNumber.Length && i < MaxLen; i++)
                s[i] = _carNumber[i].ToString();
            _inCarNum = string.Join(" ", s);
            btnOk.Enabled = (_carNumber.Length == MaxLen);
            lblCarNum.Invalidate();
        }

        private async Task SearchEdgeServiceAsync()
        {
            try
            {
                EdgeServiceOptions options = EdgeServiceOptions.Load(ConfigurationManager.AppSettings);
                EdgeServiceClient? client = _mainForm?.EdgeClient;
                if (client is null) { MessageBox.Show("EdgeService 연결이 준비되지 않았습니다."); return; }
                btnOk.Enabled = false;
                EdgeCallResult<ParkingSearchResult> result = await client.SearchParkingAsync(_carNumber, DateTimeOffset.Now);
                btnOk.Enabled = true;
                if (!result.IsSuccess || result.Value is null) { MessageBox.Show(result.Error ?? "차량검색에 실패했습니다."); return; }
                KioskExitContext context = new()
                {
                    Notification = new KioskExitNotification(Guid.NewGuid(), options.Sitenum, options.Groupnum, 0, 0, _carNumber, DateTimeOffset.Now, null),
                    Quote = result.Value.Quote,
                    Candidates = result.Value.Candidates,
                    ParkingSessionId = result.Value.Quote?.ParkingSessionId
                };
                if (_mainForm is null) { MessageBox.Show("MainForm15가 초기화되지 않았습니다."); return; }
                EdgeSettlementLauncher.Show(this, _mainForm, client, context, _periodtype);
            }
            catch (Exception ex) { btnOk.Enabled = true; MessageBox.Show($"EdgeService 차량검색 오류: {ex.Message}"); }
        }

        void NumClick(char inNum)
        {
            if (_carNumber.Length >= MaxLen)
                return;
            _carNumber += $"{inNum}";
            UpdatePlate();
        }

        void Backspace()
        {
            if (_carNumber.Length == 0)
                return;
            _carNumber = _carNumber.Substring(0, _carNumber.Length - 1);
            UpdatePlate();
        }

        void ClearAll()
        {
            _carNumber = "";
            UpdatePlate();
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
                if (btnOk.CanFocus)
                    btnOk.Focus();
            }));
        }
    }
}
