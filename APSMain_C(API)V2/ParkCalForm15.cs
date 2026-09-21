using APSMain.Api.Request;
using APSMain.BaseClass;
using APSMain.Comm;
using APSMain.DbModels;
using APSMain.Smatro;
using APSMain.Tcpip;
using APSMain.TTSLib;
using APSMain.Integration.EdgeService;
using System.Data;
using System.Text;

namespace APSMain
{
    public partial class ParkCalForm15 : Form, ISubFormResult<string>, IActiveForm
    {
        private MainForm15? _mainForm;
        public ParkFeeCalculator _parkcal = new ParkFeeCalculator();

        public Tparkinfo? _parkinfo;
        public Tperiodmember? _periodmember;
        private CardTransInfo? _cdinfo = null;
        private STTPARKINFO stparkinfo = new STTPARKINFO();

        public ClsLog? XLogClass;

        public Action<Form, Keys, int, int>? RouteArrow { get; set; } = null;

        private ContrastToggler _contrast => APSConfig.Contrast;
        private ZoomWrapper? _zoomWrap;

        public SMPAYDATA _smPayData = new SMPAYDATA();

        private PaymentSocket? _paySocket;
        private KiccCredit? _kiccCredit;
        public TicketReader? _ticketReader;
        private bool _closeCleanupCompleted;

        private int _ldmNum = 0;

        public int _carType = 1;        // 1: 소형  2: 중형 3: 대형 4: 무료주차장위반차량 계산용
        public string _lastCardNumber = string.Empty;

        private DateTime _calinTime;
        private DateTime _caloutTime;

        public FormResult Result { get; private set; } = FormResult.FormNone;
        public string ResultData { get; private set; } = "";

        private System.Threading.Timer? _autoCloseTimer;

        public int lastIndex = 0;

        public int _parkRemainFee = 0;
        private int _prePay = 0;
        private bool _feeFree = false;

        public bool calOkcheck = false;
        public string intimestr = "";
        private bool _parentZoom = false;

        private sbyte _outflag = 0;
        private bool _periodtype = false;
        private EdgeParkCalSession? _edgeSession;

        private volatile bool _isClosing = false;
        private int _formGeneration;

        private int _AutoCancleTime = 0;

        List<Tbcardinfo>? _tbcards = null;
        List<Tdiscountinfo>? _disinfo = null;

        private bool ticketClose = false;

        private System.Windows.Forms.Timer? _closeTimer = null;

        public ParkCalForm15(int ldmIndex, bool periodtype)
        {
            InitializeComponent();

            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            this.UpdateStyles();

            _ldmNum = ldmIndex;
            _periodtype = periodtype;
            _mainForm = APSConfig.mainForm15
                ?? throw new InvalidOperationException("MainForm15가 초기화되기 전에 ParkCalForm15가 생성되었습니다.");
            RouteArrow = _mainForm.ProcessArrow;
            _mainForm._calForm = this;

            _parentZoom = APSConfig.isZoomed;
            _mainForm._xcdinfo.Clear();
        }

        public void UseEdgeSettlement(EdgeServiceClient client, KioskExitContext context, FeeQuote quote)
        {
            _edgeSession = new EdgeParkCalSession(client, context, quote);
        }

        private async void ParkCalForm15_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_closeCleanupCompleted)
                return;

            if (_isClosing) {
                e.Cancel = true;
                return;
            }

            ticketClose = true;
            _isClosing = true;
            e.Cancel = true;

            await Task.Delay(20);

            try {
                StopAutoCloseTimer();

                CloseTicketReader();

                APSConfig.ContrastChanged -= OnContrastChanged;

                if (_zoomWrap != null) {
                    _zoomWrap.DetachDragEvents(pZoomContent);
                }

                var oldImage = picCarImage.Image;
                picCarImage.Image = null;
                oldImage?.Dispose();

                if (_mainForm != null) {
                    _mainForm.LDMReset(_ldmNum, _feeFree ? 2 : 0);

                    if (APSConfig.VANTYPE == (int)EnumVanType.SMATRO) {
                        _mainForm.CardCalculateEvent -= CardCalculateEvent;
                    }
                    else if (APSConfig.VANTYPE == (int)EnumVanType.KICC) {
                        var kiccCredit = _kiccCredit;
                        _kiccCredit = null;

                        if (kiccCredit != null) {
                            try {
                                kiccCredit.PaymentCompleted -= OnKiccPaymentCompleted;
                                await kiccCredit.ResetAndWaitRemoveAsync();
                            }
                            catch (Exception ex) {
                                XLogClass?.SaveLogString("CAL", $"KICC Close Error: {ex}");
                            }
                            finally {
                                kiccCredit.Dispose();
                            }
                        }
                    }

                    _mainForm.lelMessageChange(0);
                    _mainForm.XLPRCancelDataSend();
                }
            }
            catch (Exception ex) {
                XLogClass?.SaveLogString("CAL", $"ParkCalForm15 Closing Error: {ex}");
            }
            finally {
                _closeCleanupCompleted = true;

                if (!IsDisposed && IsHandleCreated)
                    BeginInvoke(new Action(Close));

                XLogClass?.SaveLogString("CAL", "ParkCalForm15 Closing OK");
                XLogClass?.SaveLogString("CAL", "==============================================================");
            }
        }

        #region AUTO_CLOSE
        protected override void OnHandleDestroyed(EventArgs e)
        {
            StopAutoCloseTimer();
            base.OnHandleDestroyed(e);
        }

        private void ParkCalForm15_FormClosed(object sender, FormClosedEventArgs e)
        {
            CloseTicketReader();
        }

        private void CloseTicketReader()
        {
            var ticketReader = Interlocked.Exchange(ref _ticketReader, null);
            if (ticketReader == null)
                return;

            try {
                XLogClass?.SaveLogString("CAL", $"TicketReader Close OK");
                ticketReader.OnPacketReceived -= OnTciketPacketReceived;
                ticketReader.Dispose();
            }
            catch (Exception ex) {
                XLogClass?.SaveLogString("CAL", $"TicketReader Close Error: {ex}");
            }
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
        #endregion


        private async void ParkCalForm15_Load(object sender, EventArgs e)
        {
            int disPersonKey = 0;
            string carNum = string.Empty;

            _caloutTime = DateTime.Now; // 현재 시간
            XLogClass = ClsLog.Instance;

            _zoomWrap = new ZoomWrapper(pZoomContent);
            _outflag = (sbyte)(APSConfig.APSMODE == 0 ? 88 : 79);
            _prePay = 0;

            if (APSConfig.VANTYPE == (int)EnumVanType.SMATRO) {
                _mainForm!.CardCalculateEvent += CardCalculateEvent;
                await Task.Delay(20);
            }

            if (_parkinfo != null) {
                _tbcards = new List<Tbcardinfo>();
                _disinfo = new List<Tdiscountinfo>();

                carNum = _parkinfo.Carnum;
                XLogClass?.SaveLogString("CAL", $"==============================================================");
                XLogClass?.SaveLogString("CAL", $"{carNum} 정산시작");
                _carType = _parkinfo.Ticketcartype ?? 1;
                if (_carType < 1 || _carType > 4) { _carType = 1; }

                if (ParkCache.Bcardinfos != null && ParkCache.Bcardinfos.Count > 0) {
                    _tbcards = ParkCache.Bcardinfos.ToList();
                    _prePay = _tbcards.Sum(x => x.Money ?? 0);
                    Console.WriteLine($"사전정산금액 : {_prePay:N0} 원");
                }

                if (ParkCache.Discountinfos != null && ParkCache.Discountinfos.Count > 0) {
                    _disinfo = ParkCache.Discountinfos.ToList();
                }

                if (ParkCache.Dispersions != null && ParkCache.Dispersions.Count > 0) {
                    Tdisperson disp = ParkCache.Dispersions[0];

                    Console.WriteLine($"사전등록할인 : {disp.Salekey:D2}-{disp.Salemsg}");
                    disPersonKey = (int)(disp.Salekey ?? 0);

                    var title = APSConfig.Discounts.Where(d => d.Salecode == disPersonKey).Select(d => d.Saletitle).FirstOrDefault();
                    lblDisTitle.Text = string.IsNullOrWhiteSpace(title) ? "일반차량" : title;
                }

                _calinTime = new DateTime(_parkinfo.Indate.Year,
                                            _parkinfo.Indate.Month,
                                            _parkinfo.Indate.Day,
                                            _parkinfo.Inhour, // Hour (시간)
                                            _parkinfo.Inmin,  // Minute (분)
                                            0);                      // Second (초)는 0으로 고정

                _parkinfo.Sitenum = (short)APSConfig.Sitenum;
                _parkinfo.Groupnum = (short)APSConfig.Groupnum;
                _parkinfo.Outdevicenum = (short)APSConfig.APSNUM;
                _parkinfo.Managercode = (short)APSConfig.APSNUM;

                _parkinfo.Indate = _calinTime;
                _parkinfo.Outdate = _caloutTime;
                _parkinfo.Outhour = (short)_caloutTime.Hour;
                _parkinfo.Outmin = (short)_caloutTime.Minute;
                _parkinfo.Outflag = _outflag;
                _parkinfo.Managername = $"무인정산-#{_parkinfo.Outdevicenum:D3}";

                if (_edgeSession == null) {
                    _parkcal.InitCalculator(_calinTime, _caloutTime, _carType, _prePay);
                    XLogClass?.SaveLogString("CAL", $"차량번호:{_parkinfo.Carnum} 주차요금:{_parkcal.totalFee}");
                    if (_disinfo.Count > 0) {
                        foreach (var item in _disinfo) {
                            if (item.Salecode > 0) {
                                _parkcal.AddDiskey((int)item.Salecode, 0, 1);
                                XLogClass?.SaveLogString("CAL", $"사전정산할인 : {item.Salecode:D2}-{item.Salevalue,-5}");
                                Console.WriteLine($"사전정산할인 : {item.Salecode:D2}-{item.Salevalue,-5}");
                            }
                        }
                    }
                    if (disPersonKey > 0) {
                        _parkcal.AddDiskey(disPersonKey, 1, 1);
                        XLogClass?.SaveLogString("CAL", $"사전등록할인 : {disPersonKey:D2}");
                        Console.WriteLine($"사전등록할인 : {disPersonKey:D2}");
                    }
                    _parkRemainFee = _parkcal.CalculateTotalFee(_calinTime, _caloutTime);
                }
                else {
                    _calinTime = _edgeSession.Quote.EntryAt.LocalDateTime;
                    _caloutTime = _edgeSession.Quote.ExitAt.LocalDateTime;
                    _parkcal.DisKeys = ParkCache.DisKeys;
                    _parkcal.DisKeys.Clear();
                    _parkRemainFee = _edgeSession.ApplyQuote(_parkcal, _parkinfo);
                }

                intimestr = _calinTime.ToString("yyyy-MM-dd HH:mm");
                lblInTime.Text = _calinTime.ToString("MM-dd HH:mm");
                lblOutTime.Text = DateTime.Now.ToString("MM-dd HH:mm");
                lblParkTime.Text = StringParkingTime(_parkcal.totalTimeMinute);
                panCarNum.Text = _parkinfo.Carnum;
                lblTotalFee.Text = $"{_parkcal.totalFee:N0} 원 ";
                lblPrePay.Text = $"{_parkcal.prePay:N0} 원 ";

                lblDisFee.Text = $"{_parkcal.totalDiscountFee:N0} 원 ";
                _parkinfo.Parkmoney = _parkcal.totalFee;
                _parkinfo.Parktime = _parkcal.totalTimeMinute;
                _parkinfo.Saletime = _parkcal.disTimeMinute;
                _parkinfo.Salemoney = _parkcal.totalDiscountFee;
                _parkinfo.Denddate = new DateTime(1970, 1, 1);

                //_parkRemainFee = _parkcal.totalFee - (_parkcal.totalDiscountFee + _parkcal.prePay);
                XLogClass?.SaveLogString("CAL", $"주차요금 : {_parkcal.totalFee}   할인요금 : {_parkcal.totalDiscountFee}   결재금액 : {_parkRemainFee}-{_parkcal.totalFee}");

                if (_parkRemainFee <= 0 || _parkcal.totalFee == 0) {
                    _mainForm!.PlaySoundFile($"feefree.mp3", AudioRouteState.Idle, 1);
                    bool saved = UpdateParkData();              // 초기 결제금액이 발생되지 않을때
                    if (_edgeSession != null && !saved) {
                        MessageBox.Show(this, _edgeSession.LastError ?? "결제완료 처리에 실패했습니다.");
                        return;
                    }
                    if (_edgeSession != null && !await _edgeSession.CompleteExitAsync()) {
                        MessageBox.Show(this, _edgeSession.LastError ?? "출차사건 완료 처리에 실패했습니다.");
                        return;
                    }
                    Result = FormResult.FormHome;

                    if (APSConfig.VANTYPE == (int)EnumVanType.SMATRO) {
                        _mainForm!.SmatroWaiting();
                    }
                    else if (APSConfig.VANTYPE == (int)EnumVanType.KICC) {
                        await _kiccCredit!.ResetAndWaitRemoveAsync().ConfigureAwait(true);
                    }

                    _mainForm!._displays?.GateCommand(_ldmNum, GateCmd.GATEOPEN);
                    XLogClass?.SaveLogString("CAL", $"차단기열림신호 0");
                    if (_mainForm != null) {
                        await Task.Delay(100);
                        _mainForm.LDMTextDisplay(_ldmNum, 0, 7, $"^W  주차요금 ", $"^G{_parkRemainFee,9} 원");
                    }
                    _feeFree = true;
                    await Task.Delay(25);
                    if (_parkinfo != null) {
                        if (_edgeSession == null && APSConfig.APSMODE == 1) {
                            var request = new CarOutRequest
                            {
                                Sitenum = (short)APSConfig.Sitenum,
                                Groupnum = (short)APSConfig.Groupnum,
                                Devicenum = (short)APSConfig.APSNUM,
                                CarGubun = 1,
                                Carnum = _parkinfo.Carnum ?? "",
                                Iotime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                Outimage = _parkinfo.Outimage
                            };
                            await RestHelper.Instance.PostAsync<CarOutRequest>("/api/Carout", request);
                        }
                        StructHelper.CopyTparkinfoToStruct(_parkinfo, ref stparkinfo);
                        _mainForm?.XSendPacketData<STTPARKINFO>(req_cmd_code.APS_CMD_PARKINFO, stparkinfo, "0.0.0.0");
                    }

                    _closeTimer = new System.Windows.Forms.Timer();
                    _closeTimer.Interval = 10;
                    _closeTimer.Tick += (s, ev) =>
                    {
                        if (_isClosing) { return; }
                        Result = FormResult.FormHome;
                        XLogClass?.SaveLogString("CAL", $"FREE FEE CLOSETIMER");
                        _closeTimer.Stop();
                        _closeTimer.Dispose();
                        BeginInvoke((Action)(() => this.Close()));  // 폼 닫기
                    };
                    _closeTimer.Start();
                    return;
                }
                lblParkFee.Text = $"{_parkRemainFee:N0} 원 ";

                if (_parkinfo.Inimage != null) {
                    await ImageDownloadHelper.ShowImageAsync(picCarImage, _parkinfo.Inimage);
                }

                if (_mainForm != null) {
                    _mainForm.LDMTextDisplay(_ldmNum, 0, 100, $"^W  주차요금 ", $"^G{_parkRemainFee,9} 원");
                }
            }
            else if (_carType == 1 && _periodmember != null) {
            }

            if (!string.IsNullOrEmpty(APSConfig.TDPort)) {
                _ticketReader = new TicketReader(APSConfig.TDPort, APSConfig.TDSpeed);
                if (_ticketReader != null) {
                    _ticketReader.OnPacketReceived += OnTciketPacketReceived;

                    _ticketReader.SendCommand("C0032403011010");
                }
            }
            if (_parkRemainFee > 0) {
                btnOk.Enabled = false;
                if (APSConfig.isContrast) {
                    btnOk.Image = Properties.Resources.confirm_gray;
                    btnOk.BackColor = SystemColors.Control;
                    btnOk.ForeColor = SystemColors.ControlText;
                }

                if (_parkinfo != null) {
                    StructHelper.CopyTparkinfoToStruct(_parkinfo, ref stparkinfo);
                    _mainForm?.XSendPacketData<STTPARKINFO>(req_cmd_code.APS_CMD_PARKCALS, stparkinfo, "0.0.0.0");
                }

                if (APSConfig.VANTYPE == (int)EnumVanType.SMATRO) {
                    _smPayData.carNum = _parkinfo?.Carnum ?? "";
                    _smPayData.prodMsg = "일반차량";
                    _smPayData.nMoney = _parkRemainFee;
                    if (_mainForm != null && _smPayData.nMoney > 0) {
                        _mainForm.MakePacketData(Constants.CMD_TX_ADD_INFO, _smPayData);
                    }
                }
                else if (APSConfig.VANTYPE == (int)EnumVanType.KICC) {
                    InitKicc(_parkcal.totalFee, APSConfig.KICCCANCELTIME);
                }
            }

            _mainForm?.lelMessageChange(4);

            if (_mainForm!.CurrentState == AudioRouteState.UsbActive) {
                string bfText = "<speak version=\"1.0\" xml:lang=\"ko-KR\" " +
                            "xmlns=\"http://www.w3.org/2001/10/synthesis\" " +
                            "xmlns:mstts=\"http://www.w3.org/2001/mstts\">" +
                            "<voice name=\"ko-KR-SunHiNeural\">" +
                            "<prosody rate=\"-10%\">" +
                            "차량번호: <say-as interpret-as=\"characters\">" + _parkinfo?.Carnum + "</say-as>. " +
                            "입차시간: <say-as interpret-as=\"date\" format=\"ymd\">" + intimestr + "</say-as>. " +
                            "주차시간: <say-as interpret-as=\"date\" format=\"ymd\">" + lblParkTime.Text + "</say-as>, " +
                            "할인요금: <say-as interpret-as=\"cardinal\">" + lblDisFee.Text + "</say-as>, " +
                            "주차요금: <say-as interpret-as=\"cardinal\">" + lblTotalFee.Text + "</say-as> 입니다." +
                            "</prosody>" +
                            "</voice>" +
                            "</speak>";
                _mainForm?.PlayTextSpeech(bfText, AudioRouteState.UsbActive, 1);

                _mainForm?.PlaySoundFile($"main14.mp3", AudioRouteState.UsbActive);
            }
            else {
                _mainForm?.PlaySoundFile($"main04.mp3", AudioRouteState.Idle, 1);
            }

#if DEBUG
            btnTest.Visible = true;
#else
            btnTest.Visible = false;
#endif

            lastIndex = GetEndTabIndex();

            StartAutoCloseTimer(APSConfig.AutoCancleTime3);
        }

        private async void getImageFromFtp(string imgname)
        {
            var old = picCarImage.Image;
            picCarImage.Image = null;
            old?.Dispose();

            await ImageDownloadHelper.ShowImageAsync(picCarImage, imgname);
        }

        private void InitKicc(int amount, int vanTimeoutMs)
        {
            _kiccCredit?.Dispose();
            _kiccCredit = null;

            _kiccCredit = new KiccCredit(0);

            _kiccCredit.CancelHide = () =>
            {
            };

            _kiccCredit.PaymentCompleted += OnKiccPaymentCompleted;

            _kiccCredit!.BeginPayment(_parkcal.totalFee, vanTimeoutMs);
        }

        private void OnKiccPaymentCompleted(KiccPaymentResult result)
        {
            if (result.IsSuccess) {
                if (result.ResCode == "0000") {
                    _cdinfo = _mainForm!._xcdinfo;
                    DateTime dt = DateTime.ParseExact(result.TradeDateTime, "yyyyMMddHHmmss", null);

                    _cdinfo.Sitenum = (short)APSConfig.Sitenum;
                    _cdinfo.Groupnum = (short)APSConfig.Groupnum;
                    _cdinfo.TicketData = _parkinfo!.Ticketdata!;
                    _cdinfo.OutDeviceNum = (short)APSConfig.APSNUM;
                    _cdinfo.ResCode = result.ResCode;
                    _cdinfo.TermId = result.TermId;
                    _cdinfo.PosId = result.PosId;
                    _cdinfo.CardId = result.CardId;
                    _cdinfo.CardName = result.CardName;
                    _cdinfo.AcceptNum = result.ApprovalNo;
                    _cdinfo.DealDate = dt.ToString("yyyy-MM-dd");
                    _cdinfo.DealTime = dt.ToString("HHmmss");
                    _cdinfo.ReceiptNum = _parkinfo.Receiptnum.ToString();
                    _cdinfo.BranchNum = result.BranchNo;
                    _cdinfo.AcceptType = 0;
                    _cdinfo.Money = result.Price;
                    _cdinfo.ParkTime = _parkinfo!.Parktime;
                    _cdinfo.DendFlag = 0;
                    _cdinfo.TendFlag = 0;
                    _cdinfo.EndDate = "1970-01-01";
                    _cdinfo.DealNum = _parkinfo!.Carnum;

                    _parkinfo!.Credittype = 0x11;
                    _parkinfo!.Creditcardno = _cdinfo.CardId;
                    _parkinfo!.Acceptno = _cdinfo.AcceptNum;
                    _parkinfo!.Accepttime = DateTime.Now;
                    _parkinfo!.Creditmoney = _cdinfo.Money;

                    XLogClass?.SaveLogString("CAL", $"승인성공 : {result.Amount}-{result.Price}    승인번호={result.ApprovalNo}");
                    Console.WriteLine($"승인성공 : {result.Amount}-{result.Price}    승인번호={result.ApprovalNo}");
                    _mainForm?.PlaySoundFile($"Comfirm.mp3", AudioRouteState.Idle, 1);
                    UiHelpers.ShowPayMsg(this, 0, true);
                    _parkRemainFee = 0;
                    calOkcheck = true;

                    _mainForm?.PlaySoundFile($"main16.mp3", AudioRouteState.Idle, 0);

                    btnPre.Enabled = false;
                    btnHome.Enabled = false;

                    UpdateParkData();      // Kicc 결제 완료 처리
                    BeginInvoke((Action)(async () =>
                    {
                        btnOk.Enabled = true;
                        if (APSConfig.isContrast) {
                            btnOk.Image = Properties.Resources.confirm_white;
                            btnOk.BackColor = Color.FromArgb(17, 17, 17);
                            btnOk.ForeColor = Color.White;
                        }
                        lastIndex = GetEndTabIndex();
                        btnOk.Focus();
                        await ProcessOk();
                    }));
                }
                else {
                    _kiccCredit!.BeginPayment(_parkcal.totalFee, APSConfig.KICCCANCELTIME);
                    XLogClass?.SaveLogString("CAL", $"승인실패0: {result.ResCode}-{result.Message}");
                    Console.WriteLine($"승인실패0: {result.ResCode}-{result.Message}");
                }
            }
            else {
                XLogClass?.SaveLogString("CAL", $"승인실패1: {result.ResCode}-{result.Message}");
                Console.WriteLine($"승인실패1: {result.Message}");
            }
        }

        private void ResetParkCalculate(int ctype)
        {
            if (_parkinfo != null && _periodtype == false) {

                if (_edgeSession != null) {
                    int[] discountKeys = _parkcal.DisKeys?.Select(x => x.diskey).ToArray() ?? Array.Empty<int>();
                    if (!_edgeSession.RefreshQuote(discountKeys, _parkcal, _parkinfo)) return;
                    _parkRemainFee = _parkcal.totalRemainFee;
                }
                else {
                    _parkcal.ResetCalculator(_carType);
                    _parkRemainFee = _parkcal.CalculateTotalFee(_calinTime, _caloutTime);
                }

                _parkinfo.Parkmoney = _parkcal.totalFee;
                _parkinfo.Parktime = _parkcal.totalTimeMinute;
                _parkinfo.Saletime = _parkcal.disTimeMinute;
                _parkinfo.Salemoney = _parkcal.totalDiscountFee;
                _parkinfo.Denddate = new DateTime(1970, 1, 1);

                //_parkRemainFee = _parkcal.totalFee - (_parkcal.totalDiscountFee + _parkcal.prePay);
                if (_parkRemainFee <= 0) {
                    _parkRemainFee = 0;
                    calOkcheck = true;
                    btnOk.Enabled = true;
                    UpdateParkData();
                    _mainForm?.PlaySoundFile("Comfirm.mp3", AudioRouteState.Idle, 1);
                    _mainForm?.PlaySoundFile("main16.mp3", AudioRouteState.Idle, 0);
                }

                BeginInvoke((Action)(async () =>
                {
                    lblTotalFee.Text = $"{_parkcal.totalFee:N0} 원 ";
                    lblPrePay.Text = $"{_parkcal.prePay:N0} 원 ";
                    lblDisFee.Text = $"{_parkcal.totalDiscountFee:N0} 원 ";
                    lblParkFee.Text = $"{_parkRemainFee:N0} 원 ";

                    if (_parkRemainFee == 0) {
                        btnOk.Enabled = true;
                        if (APSConfig.isContrast) {
                            btnOk.Image = Properties.Resources.confirm_white;
                            btnOk.BackColor = Color.FromArgb(17, 17, 17);
                            btnOk.ForeColor = Color.White;
                        }

                        lastIndex = GetEndTabIndex();
                        btnOk.Focus();
                        await ProcessOk();
                    }
                    else {
                        lastIndex = GetEndTabIndex();
                    }
                }));

                if (_mainForm != null) {
                    _mainForm.LDMTextDisplay(_ldmNum, 0, 100, $"^W 주차요금 ", $"^G{_parkRemainFee,9} 원");
                }

                if (APSConfig.VANTYPE == (int)EnumVanType.SMATRO) {
                    _smPayData.carNum = _parkinfo?.Carnum ?? "";
                    _smPayData.prodMsg = "일반차량";
                    _smPayData.nMoney = _parkRemainFee;

                    if (_mainForm != null && _smPayData.nMoney > 0) {
                        _mainForm.MakePacketData(Constants.CMD_TX_ADD_INFO, _smPayData);
                    }
                }
                else if (APSConfig.VANTYPE == (int)EnumVanType.KICC) {
                    _kiccCredit?.ResetPayment(_parkRemainFee, APSConfig.KICCCANCELTIME);
                }

                if (_parkRemainFee != 0) {
                    _mainForm?.PlaySoundFile("magnetic00.mp3", AudioRouteState.Idle, 1);
                }
            }
            else if (_periodmember != null && _periodtype == true) {
            }
        }

        private string SpeechText(string a, string b)
        {
            string bfText = "<speak version=\"1.0\" xml:lang=\"ko-KR\" " +
                            "xmlns=\"http://www.w3.org/2001/10/synthesis\" " +
                            "xmlns:mstts=\"http://www.w3.org/2001/mstts\">" +
                            "<voice name=\"ko-KR-SunHiNeural\">" +
                            "<prosody rate=\"-10%\">" +
                            "<break time=\"150ms\"/> " +
                            "할인요금: <say-as interpret-as=\"cardinal\">" + lblDisFee.Text + "</say-as>, " +
                            "주차요금: <say-as interpret-as=\"cardinal\">" + lblTotalFee.Text + "</say-as> 입니다." +
                            "</prosody>" +
                            "</voice>" +
                            "</speak>";

            return bfText;
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
            Result = FormResult.FormOk;
            BeginInvoke((Action)(() => this.Close()));  // 폼 닫기
        }

        public void OnCancel()
        {
            if (_isClosing) { return; }

            StopAutoCloseTimer();
            if (this.ActiveControl is IButtonControl btn) {
                btn.PerformClick();
            }
        }

        public void GiveFocus()
        {
            this.Focus();
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
        #endregion

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            var key = keyData & Keys.KeyCode;
            if (key is Keys.Left or Keys.Right or Keys.Up or Keys.Down) {
                // 텍스트 입력 중에는 화살표를 건드리지 않으려면 다음 줄 해제:
                if (this.ActiveControl is TextBoxBase or ComboBox)
                    return base.ProcessCmdKey(ref msg, keyData);

                RouteArrow?.Invoke(this, key, 0, lastIndex);   // ← 항상 MainForm로 위임
                return true;               // 내가 처리했음
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

        public void CardCalculateEvent(int msgType, SmartroPacket packet, SmartroApprovalResponse? res)
        {
            if (_isClosing || IsDisposed || Disposing)
                return;

            _cdinfo = _mainForm!._xcdinfo;

            if (msgType == 0 && res != null) {
                XLogClass?.SaveLogString("CAL", $"BODY : {Encoding.GetEncoding("ks_c_5601").GetString(packet.Body)}");

                _cdinfo.AcceptType = ToInt(res.TradeType);
                _cdinfo.DealType = ToInt(res.MediaType);
                _cdinfo.ResCode = "00";
                _cdinfo.CardId = res.CardNo;

                _cdinfo.Money = res.Amount;
                _cdinfo.Tax = res.Tax;
                _cdinfo.Service = res.ServiceCharge;
                _cdinfo.Installment = ToInt(res.Installment);

                _cdinfo.AcceptNum = res.ApprovalNo;
                _cdinfo.DealDate = res.SaleDate;
                _cdinfo.DealTime = res.SaleTime;
                _cdinfo.DealNum = res.TradeNo;
                _cdinfo.PosId = res.MerchantNo;
                _cdinfo.SamId = res.TerminalNo;
                _cdinfo.CardName = res.IssuerInfo;
                _cdinfo.BranchNum = res.AcquirerInfo;

                _parkinfo!.Credittype = 0x11;
                _parkinfo!.Creditcardno = _cdinfo.CardId;
                _parkinfo!.Acceptno = _cdinfo.AcceptNum;
                _parkinfo!.Accepttime = DateTime.Now;
                _parkinfo!.Creditmoney = _cdinfo.Money;
                _parkcal.totalRemainFee = 0;

                Console.WriteLine("{0,-18} : {1}", "거래구분코드", _cdinfo.AcceptType);
                Console.WriteLine("{0,-18} : {1}", "거래매체", _cdinfo.DealType);
                Console.WriteLine("{0,-18} : {1}", "카드번호", _cdinfo.CardId);
                Console.WriteLine("{0,-18} : {1}", "승인금액", _cdinfo.Money);
                Console.WriteLine("{0,-18} : {1}", "세금/잔여횟수", _cdinfo.Tax);
                Console.WriteLine("{0,-18} : {1}", "봉사료/사용횟수", _cdinfo.Service);
                Console.WriteLine("{0,-18} : {1}", "할부개월", _cdinfo.Installment);
                Console.WriteLine("{0,-18} : {1}", "승인번호", _cdinfo.AcceptNum);
                Console.WriteLine("{0,-18} : {1}", "매출일자", _cdinfo.DealDate);
                Console.WriteLine("{0,-18} : {1}", "매출시간", _cdinfo.DealTime);
                Console.WriteLine("{0,-18} : {1}", "거래고유번호", _cdinfo.DealNum);
                Console.WriteLine("{0,-18} : {1}", "가맹점번호", _cdinfo.PosId);
                Console.WriteLine("{0,-18} : {1}", "단말기번호", _cdinfo.SamId);
                Console.WriteLine("{0,-18} : {1}", "발급사", _cdinfo.CardName);
                Console.WriteLine("{0,-18} : {1}", "매입사", _cdinfo.BranchNum);

                XLogClass?.SaveLogString("CAL", $"승인성공 : {_cdinfo.Money}-{_cdinfo.Money}    승인번호={_cdinfo.AcceptNum}");
                _mainForm?.PlaySoundFile($"Comfirm.mp3", AudioRouteState.Idle, 1);
                UiHelpers.ShowPayMsg(this, 0, true);
                _parkRemainFee = 0;
                calOkcheck = true;
                _mainForm?.PlaySoundFile($"main16.mp3", AudioRouteState.Idle, 0);

                UpdateParkData();      // 스마트로 결제완료
                BeginInvoke((Action)(async () =>
                {
                    btnPre.Enabled = false;
                    btnHome.Enabled = false;
                    btnOk.Enabled = true;

                    if (APSConfig.isContrast) {
                        btnOk.Image = Properties.Resources.confirm_white;
                        btnOk.BackColor = Color.FromArgb(17, 17, 17);
                        btnOk.ForeColor = Color.White;
                    }

                    lastIndex = GetEndTabIndex();
                    btnOk.Focus();

                    await ProcessOk();
                }));

                _mainForm?.PlaySoundFile($"main16.mp3", AudioRouteState.Idle, 0);
            }
            else if (msgType == 1) {
                string errMsg = "";
                if (res != null) {
                    errMsg = res.IssuerInfo + " " + res.AcquirerInfo;
                }

                XLogClass?.SaveLogString("CDI", $"승인 실패: {errMsg}");
                UiHelpers.ShowPayMsg(this, 4, true);
            }
            else if (msgType == 2) {
                UiHelpers.ShowPayMsg(this, 1, false);
            }
            else if (msgType == 5) {
                UiHelpers.ShowPayMsg(this, 5, true);
            }
        }

        private int ToInt(string value)
        {
            return int.TryParse(value, out int ret) ? ret : 0;
        }

        public Tbcardinfo TbCopy(CardTransInfo src)
        {
            DateTime xdate = DateTime.ParseExact(src.DealDate, new[] { "yyyy-MM-dd", "yyyyMMdd" }, null);

            return new Tbcardinfo
            {
                Sitenum = (short?)src.Sitenum,
                Groupnum = (short?)src.Groupnum,
                Ticketdata = src.TicketData,
                Outdevicenum = (short?)src.OutDeviceNum,
                Rescode = src.ResCode,
                Termid = src.TermId,
                Posid = src.PosId,
                Cardid = src.CardId,
                Cardname = src.CardName,
                Acceptnum = src.AcceptNum,
                Dealdate = xdate,
                Dealtime = src.DealTime,
                Enddate = DateTime.Now,
                Receiptnum = src.ReceiptNum,
                Branchnum = src.BranchNum,
                Accepttype = (short?)src.AcceptType,
                Money = src.Money,
                Parktime = src.ParkTime,
                Dealnum = src.DealNum,
                Pindex = src.Pindex
            };
        }

        public bool UpdateParkData()
        {
            int disCount = 0;

            try {
                if (_edgeSession != null)
                    return _edgeSession.CompletePayment(
                        _parkinfo!, calOkcheck || _parkRemainFee == 0);
                _parkinfo!.Denddate = DateTime.Now;
                _parkinfo.Parkmoney = _parkinfo.Creditmoney;
                _parkinfo.Outdevicenum = (short)APSConfig.APSNUM;
                _parkinfo.Outdate = DateTime.Now;
                _parkinfo.Outhour = (short)DateTime.Now.Hour;
                _parkinfo.Outmin = (short)DateTime.Now.Minute;
                _parkinfo.Outflag = 88;
                if (_parkRemainFee > 0) {
                    if (_parkcal.DisKeys == null || _parkcal.DisKeys.Count == 0)
                        return true;
                }

                var request = new CarPayRequest
                {
                    Sitenum = (short)APSConfig.Sitenum,
                    Groupnum = (short)APSConfig.Groupnum,
                    Devicenum = (short)APSConfig.APSNUM,
                    CarGubun = 1,
                    Tparkinfo = _parkinfo,
                    Tperiodmember = null
                };

                request.Tdiscountinfo.Clear();
                if (_parkcal.DisKeys != null && _parkcal.DisKeys.Count > 0) {
                    disCount = _parkcal.DisKeys.Count;
                    foreach (var k in _parkcal.DisKeys) {
                        if (k.save != 0) {
                            Tdiscounttable? td = APSConfig.Discounts.FirstOrDefault(d => d.Salecode == k.diskey);
                            if (td != null) {
                                Tdiscountinfo dinfo = new Tdiscountinfo
                                {
                                    Logid = $"APS-{APSConfig.APSNUM:D3}",
                                    Devicenum = APSConfig.APSNUM,
                                    Carnum = _parkinfo.Carnum,
                                    Deptcode = 0,
                                    Salecode = k.diskey,
                                    Saletype = td.Saletype ?? 0,
                                    Salevalue = td.Salevalue ?? 0,
                                    Indate = _calinTime,
                                    Sdate = _caloutTime,
                                    Tparkindex = _parkinfo.Xindex
                                };
                                request.Tdiscountinfo.Add(dinfo);
                            }
                        }
                    }
                }

                request.Tbcardinfo.Clear();
                if (_cdinfo != null) {
                    _cdinfo.Sitenum = APSConfig.Sitenum;
                    _cdinfo.Groupnum = APSConfig.Groupnum;
                    _cdinfo.OutDeviceNum = APSConfig.APSNUM;
                    _cdinfo.TicketData = _parkinfo.Ticketdata! ?? "";
                    _cdinfo.ReceiptNum = _parkinfo.Receiptnum!.ToString() ?? "";
                    _cdinfo.ParkTime = _parkinfo.Parktime;
                    _cdinfo.Pindex = _parkinfo.Xindex;
                    _cdinfo.DealNum = _parkinfo.Carnum!;

                    request.Tbcardinfo.Add(TbCopy(_cdinfo));
                }

                if (_parkRemainFee == 0 || disCount > 0) {
                    string? content = RestHelper.Instance.PostAsync<CarPayRequest>("/api/CarPay", request).GetAwaiter().GetResult();
                    return !string.IsNullOrWhiteSpace(content);
                }
                else {
                    return false;
                }
            }
            catch (Exception ex) {
                Console.WriteLine($"CarPay API error: {ex.Message}");
                return false;
            }
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

        private string StringParkingTime(int totalMinutes)
        {
            if (totalMinutes < 60)
                return $"{totalMinutes:D2}분 ";

            int hours = totalMinutes / 60;
            int mins = totalMinutes % 60;

            if (mins == 0)
                return $"{hours}시간 00분 ";
            else
                return $"{hours}시간 {mins:D2}분 ";
        }

        private string oldCmd = string.Empty;
        private int tdCheck = 0x00;
        private async void OnTciketPacketReceived(byte flag, byte[] obj)
        {
            if (_isClosing || IsDisposed || Disposing)
                return;

            if (ticketClose == true) { return; }

            byte[] TDACK = { 0x06, 0x0d, 0x00 };
            byte[] TDENQ = { 0x05, 0x0d, 0x00 };
            byte[] TDNACK = { 0x15, 0x0d, 0x00 };

            if (flag == 0x06) {
                _ticketReader?.WriteByte(TDENQ, 2);
            }
            else if (flag == 0x15) {
            }
            else if (flag == Constants.ASCII_DONE) {
                string xdata = Encoding.ASCII.GetString(obj).TrimEnd('\0');
                if (!oldCmd.Equals(xdata)) {
                    // Console.WriteLine("TD RX : " + xdata);
                    oldCmd = xdata;
                }
                await Task.Delay(25);
                if (xdata.Length > 0) {
                    if (xdata.StartsWith("P00")) {
                        _ticketReader?.SendCommand("C11");
                    }
                    else if (xdata.StartsWith("P11")) {
                        if (xdata[3] == '0' && xdata[4] == '0') {
                            tdCheck = 0x00;
                            _ticketReader?.SendCommand("C11");
                        }
                        else if (xdata[3] == '0' && xdata[4] != '0') {
                            if (tdCheck == 0x00)
                                _ticketReader?.SendCommand("C20");
                            else
                                _ticketReader?.SendCommand("C11");
                        }
                        else {
                            tdCheck = 0x11;
                            _ticketReader?.SendCommand("C30");
                        }
                    }
                    else if (xdata.StartsWith("P20")) {
                        _ticketReader?.SendCommand("C62");
                    }
                    else if (xdata.StartsWith("P:0")) {
                        _ticketReader?.SendCommand("C11");
                    }
                    else if (xdata.StartsWith("P30"))  // Eject Front
                    {
                        if (xdata[3] == '0' && (xdata[4] == '0' || xdata[4] == '1')) {
                            _ticketReader?.SendCommand("C11");
                        }
                    }
                    else if (xdata.StartsWith("P30"))  // Eject Front
                    {
                        if (xdata[3] == '0' && (xdata[4] == '0' || xdata[4] == '1')) {
                            _ticketReader?.SendCommand("C11");
                        }
                        else {
                            _ticketReader?.SendCommand("C30");
                        }
                    }
                    else if (xdata.StartsWith("P31"))  // Eject Back
                    {
                        if (xdata[3] == '0' && xdata[4] == '0') {
                            _ticketReader?.SendCommand("C11");
                        }
                    }
                    else if (xdata.StartsWith("P62"))  // Magnetic Data Read 2 Track
                    {
                        string sitenum = xdata.Substring(13, 3);
                        string groupnum = xdata.Substring(16, 3);
                        Console.WriteLine("====================할인권삽입===========================");
                        Console.WriteLine($"sitenum  : {xdata.Substring(13, 3):D3}");
                        Console.WriteLine($"devicenum : {xdata.Substring(16, 3):D3}");
                        Console.WriteLine($"diskey   : {xdata.Substring(5, 2):D2}");
                        Console.WriteLine($"disval   : {xdata.Substring(19, 6):D6}");
                        Console.WriteLine("========================================================");

                        Int32.TryParse($"{xdata.Substring(5, 2):D2}", out int diskey);

                        if (sitenum.Equals("001") && groupnum.Equals("401")) {
                            XLogClass?.SaveLogString("CAL", $"할인권 삽입 : {diskey:D2}");
                            _parkcal.AddDiskey(diskey, 1, 1);
                            ResetParkCalculate(1);
                            _ticketReader?.SendCommand("C31");
                        }
                        else {
                            tdCheck = 0x11;
                            UiHelpers.ShowMessage(this, 13, true);
                            _mainForm?.PlaySoundFile($"magnetic01.mp3", AudioRouteState.Idle, 1);
                            _ticketReader?.SendCommand("C30");
                        }
                    }
                    else if (xdata.StartsWith("N62"))  // Magnetic Data Read 2 Error
                    {
                        tdCheck = 0x11;
                        UiHelpers.ShowMessage(this, 13, true);
                        _mainForm?.PlaySoundFile($"magnetic01.mp3", AudioRouteState.Idle, 1);
                        _ticketReader?.SendCommand("C30");
                    }
                }
            }
        }

        private void pZoomContent_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            _zoomWrap?.ToggleZoom();
        }

        private void ParkCalForm15_Shown(object sender, EventArgs e)
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
            btnOk.Focus();
        }

        public void ZoomMovedQuard(int pos)
        {
            _zoomWrap?.SnapToQuadrant(pos);
        }

        private void btnPre_Click(object sender, EventArgs e)
        {
            if (_isClosing) { return; }

            StopAutoCloseTimer();

            if (_parkRemainFee > 0 && btnOk.Enabled == false) {
                UpdateParkData();      // 결제가 완료되지 않은 경우 이전으로 가기
            }

            Result = FormResult.FormPre;
            APSConfig.isZoomed = _parentZoom;
            _mainForm!.SmatroWaiting();

            _mainForm!.PlaySoundFile("btnPre1.mp3", AudioRouteState.Dual, 1);
            BeginInvoke((Action)(() => this.Close()));
        }

        private void btnHome_Click(object sender, EventArgs e)
        {
            if (_isClosing) { return; }

            StopAutoCloseTimer();

            if (_parkRemainFee > 0 && btnOk.Enabled == false) {
                UpdateParkData();      // 결제가 완료되지 않은 경우 홈으로 가기
            }

            Result = FormResult.FormHome;

            _mainForm!.SmatroWaiting();

            _mainForm!.PlaySoundFile("btnHome1.mp3", AudioRouteState.Dual, 1);

            BeginInvoke((Action)(() => this.Close()));
        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            using (var frm = new DiscountSelectForm()) {
                if (frm.ShowDialog() == DialogResult.OK) {
                    int disKey = frm.DisKey;
                    if (disKey > 0 && disKey < 100) {
                        SaleKeyParkFuction(disKey, 1, 1);
                    }
                    else if (disKey == 101) {
                        if (APSConfig.VANTYPE == 1) {
                            _mainForm?.XDataCall(_parkRemainFee);
                        }
                        else {
                            _kiccCredit?.DummyKiccParse();
                        }
                        btnTest.Visible = false;
                    }
                }
            }
            //UiHelpers.ShowMessage(this, 13, true);
        }

        static unsafe void CopyString(string? src, byte* dst, int size)
        {
            if (string.IsNullOrEmpty(src))
                return;

            var bytes = System.Text.Encoding.GetEncoding("ks_c_5601-1987").GetBytes(src);
            int len = Math.Min(bytes.Length, size - 1);

            for (int i = 0; i < len; i++)
                dst[i] = bytes[i];

            dst[len] = 0;
        }

        static unsafe void CopyDate(DateTime dt, byte* dst)
        {
            CopyString(dt.ToString("yyyyMMddHHmmss"), dst, 14);
        }

        private async Task ProcessOk()
        {
            if (calOkcheck == true || _parkRemainFee == 0) {

                if (_edgeSession != null) {
                    if (!_edgeSession.PaymentCompleted &&
                        !_edgeSession.CompletePayment(_parkinfo!, true)) {
                        MessageBox.Show(this, _edgeSession.LastError ?? "EdgeService 결제완료가 확인되지 않았습니다.");
                        return;
                    }
                    if (!await _edgeSession.CompleteExitAsync()) {
                        MessageBox.Show(this, _edgeSession.LastError ?? "출차사건 완료 처리에 실패했습니다.");
                        return;
                    }
                }

                if (APSConfig.VANTYPE == (int)EnumVanType.SMATRO) {
                    _mainForm!.SmatroWaiting();
                }
                else if (APSConfig.VANTYPE == (int)EnumVanType.KICC) {
                    await _kiccCredit!.ResetAndWaitRemoveAsync().ConfigureAwait(true);
                }

                _mainForm?._displays?.GateCommand(_ldmNum, GateCmd.GATEOPEN);
                XLogClass?.SaveLogString("CAL", $"차단기열림신호 1");
                await Task.Delay(100);
                if (_parkinfo != null) {
                    if (_edgeSession == null && APSConfig.APSMODE == 1) {
                        var request = new CarOutRequest
                        {
                            Sitenum = (short)APSConfig.Sitenum,
                            Groupnum = (short)APSConfig.Groupnum,
                            Devicenum = (short)APSConfig.APSNUM,
                            CarGubun = 1,
                            Carnum = _parkinfo.Carnum ?? "",
                            Iotime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            Outimage = _parkinfo.Outimage
                        };
                        await RestHelper.Instance.PostAsync<CarOutRequest>("/api/Carout", request);
                    }

                    StructHelper.CopyTparkinfoToStruct(_parkinfo, ref stparkinfo);
                    _mainForm?.XSendPacketData<STTPARKINFO>(req_cmd_code.APS_CMD_PARKINFO, stparkinfo, "0.0.0.0");
                }
                if (_parkinfo!.Creditmoney > 0) {
                    PauseAutoCloseTimer();
                    UiHost.ShowActiveForm<string>(
                       this,
                       new ReceiptForm15(),
                       (result, data) =>
                       {
                           Result = result;
                           //BeginInvoke((Action)(() => this.Close()));
                           //this.Close();
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
                    );
                }
                else {
                    this.Close();
                }
            }
        }

        private async void btnOk_Click(object sender, EventArgs? e)
        {
            await ProcessOk();
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

        public void SpeechParkCalForm15()
        {
            if (_parkinfo != null) {
                string bfText = $"차량번호는 {_parkinfo?.Carnum}입니다. 입차 시간:{lblInTime.Text}. 주차 요금:{lblParkTime.Text}. 할인 요금: {lblDisFee.Text}. 최종 결제 금액: {lblTotalFee.Text}입니다.";

                _mainForm?.PlayTextSpeech(bfText, AudioRouteState.Idle, 1);
            }

            // _mainForm?.PlaySoundFile("main14.mp3", AudioRouteState.UsbActive, 0);
        }

        public void OnGoHome()
        {
            if (_isClosing) { return; }

            StopAutoCloseTimer();

            if (_parkRemainFee > 0 && btnOk.Enabled == false) {
                UpdateParkData();      // 결제가 완료되지 않은 경우 이전으로 가기(OnGoHome)
            }

            Result = FormResult.FormHome;
            _mainForm!.PlaySoundFile("btnHome1.mp3", AudioRouteState.Dual, 1);
            BeginInvoke((Action)(() => this.Close()));
        }

        public void DoActiveButton()
        {
            BeginInvoke((Action)(() =>
            {
                if (btnOk.CanFocus)
                    btnOk.Focus();
            }));
        }

        public void SaleKeyParkFuction(int key, int save, int limit)
        {
            var dis = APSConfig.Discounts.Where(d => d.Salecode == key).FirstOrDefault();
            if (dis != null) {
                lblDisTitle.Text = dis.Saletitle;

                XLogClass?.SaveLogString("CAL", $"원격할인 : {key:D2}-{dis.Saletitle}");

                _parkcal.AddDiskey(key, save, limit);

                ResetParkCalculate(1);

                lblDisFee.Text = $"{_parkcal.totalDiscountFee:N0} 원 ";

                lblParkFee.Text = $"{_parkcal.totalRemainFee:N0} 원 ";
            }
        }

        public void RemoveDisKey(int keytype)
        {
            _parkcal.RemoveDisKey(keytype);

            ResetParkCalculate(1);
        }

        private void ReplaceAllLabels(Control parent)
        {
            for (int i = parent.Controls.Count - 1; i >= 0; i--) {
                var ctrl = parent.Controls[i];

                if (ctrl is Label lbl && !(ctrl is CenterLabel)) {

                    if (!ctrl.Name.Equals("lblTitle") && !ctrl.Name.Equals("lblDisTitle")) {
                        var newLbl = new CenterLabel
                        {
                            Text = lbl.Text,
                            Font = lbl.Font,
                            ForeColor = lbl.ForeColor,
                            BackColor = lbl.BackColor,
                            AutoSize = false,
                            Size = lbl.Size,
                            Location = lbl.Location,
                            Anchor = lbl.Anchor,
                            Dock = lbl.Dock,
                            Name = lbl.Name,
                            TextAlign = lbl.TextAlign
                        };

                        parent.Controls.RemoveAt(i);
                        parent.Controls.Add(newLbl);
                    }
                }
                else if (ctrl.HasChildren) {
                    ReplaceAllLabels(ctrl);
                }
            }
        }
    }
}
