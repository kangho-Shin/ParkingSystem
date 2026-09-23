using APSMain.BaseClass;
using APSMain.Comm;
using APSMain.Models;
using APSMain.Tcpip;
using APSMain.TTSLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using NAudio.Wave;
using Windows.Media.MediaProperties;
using APSMain.Api.Request;
using System.Threading.Tasks;
using APSMain.Api.Response;
using APSMain.Smatro;
using APSMain.Integration.EdgeService;

namespace APSMain
{
    public partial class MainForm15 : Form, IMainForm
    {
//        public string? _connstr { get; set; }

        public Action<Form, Keys, int, int>? RouteArrow = null;

        public event Action<int, SmartroPacket, SmartroApprovalResponse?>? CardCalculateEvent = null;
        public event Action<int, SmartroPacket, SmartroApprovalResponse?>? CardCancleEvent = null;
        public event Action<int, SmartroPacket>? CardSetupEvent = null;

        public CreditCardReader? _creditSmatro = null;
        public KiccCredit? _creditKicc = null;

        public SmatroDeviceFrm? _smDevice = null;
        public SMPAYDATA _smPayData = new SMPAYDATA();

        public SMDEVICEINFO smDeviceInfo = new SMDEVICEINFO();
        public SMDEVICEINFO SMDeviceInfo { get { return smDeviceInfo; } set { smDeviceInfo = value; } }

        public MainControl? _mainControl;
        private ContrastToggler _contrast => APSConfig.Contrast;
        public ContextMenuStrip _CTXMenu { get; set; } = new ContextMenuStrip();
        public static ConsoleManager? _MyConsole = null;
        public ToolStripMenuItem? _consoleItem = null;

        public XLocalSocket? XLprSocket = null;
        private bool _isLprReconnect = false;
        private LPRCameraServer? _cameraServer;
        public LDMDisplayManager? _displays { get; set; }

        public Tparkinfo _xparkinfo { get; set; } = new Tparkinfo();
        public Tperiodmember _xperiodmember { get; set; } = new Tperiodmember();
        public CardTransInfo _xcdinfo { get; set; } = new CardTransInfo();

        public ClsLog? XLogClass;

        public string[] _LDMIP = new string[4];
        public int[] _LDMPORT = new int[4];
        public int[] _XLDMTimerTick = { 0, 0, 0, 0 };
        private int[] _oldMin = { -1, -1, -1, -1 };

        public int _ILDMNum { get; set; } = 0;
        public int _OLDMNum { get; set; } = 2;

        public int MainStartIndex = 0;
        public int MainEndIndex = 0;
        public string? _lastCardNumber;
        public string[]? lblMessage;

        public System.Threading.Timer? _voiceRepeatTimer = null;
        private string _repeatFile = "Mainloop.mp3";

        public FormResult Result = FormResult.FormNone;
        public string ResultData { get; private set; } = "";
        public ParkCalForm15? _calForm = null;
        public string _carNumber = string.Empty;
        private System.Threading.Timer? _clockTimer;
        public bool UsbPlayOn { get; set; } = false;
        private static SoundQueueLite? _sndQueue;

        public AudioRouteState CurrentState { get; set; } = AudioRouteState.Idle;

        private UDPSocket? _udpSocket;
        public long _pressStart { get; set; } = 0;

        public bool _kbArmed { get; set; } = false;

        //AudioDeviceChangeNotifier _notifier = new AudioDeviceChangeNotifier();

        [DllImport("user32.dll")]
        static extern bool SetProp(IntPtr hWnd, string lpString, IntPtr hData);
        [DllImport("user32.dll")]
        static extern int GetMessageTime();

        STTPARKINFO stparkinfo = new STTPARKINFO();
        private bool _reconnecting = false;

        private int _memberOverTime = 0;
        private int _memberOverDay = 0;
        private System.Windows.Forms.Timer? _configRetryTimer;
        private EdgeServiceFormBridge? _edgeBridge;
        internal EdgeServiceClient? EdgeClient
        {
            get
            {
                if (_edgeBridge is null && EdgeServiceEnabled) _ = StartEdgeServiceAsync();
                return _edgeBridge?.Client;
            }
        }
        private bool _edgeClosing;
        private bool EdgeServiceEnabled => true;

        public MainForm15()
        {
            InitializeComponent();

            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(1152, 864);
            this.StartPosition = FormStartPosition.Manual;
            this.TopMost = true;

            this.RouteArrow = ProcessArrow;
        }

        private async void MainForm15_FormClosing(object sender, FormClosingEventArgs e)
        {
            _edgeClosing = true;
            MediaPlayer.PlaybackCompleted -= MediaPlayer_PlaybackCompleted;

            if (XLprSocket != null) {
                XLprSocket.OnLocalConnectEvent -= OnLocalConnectEvent;
                XLprSocket.OnLocalDisconnectedEvent -= OnLocalDisconnectedEvent;
                XLprSocket.OnLocalNotConnectEvent -= OnLocalNotConnectEvent;
                XLprSocket.OnLocalReceiveEvent -= OnLocalReceiveEvent;
                XLprSocket.Dispose();
                XLprSocket = null;
            }

            _voiceRepeatTimer?.Dispose();
            _voiceRepeatTimer = null;
            _clockTimer?.Dispose();
            _clockTimer = null;
            if (_edgeBridge is not null) await _edgeBridge.DisposeAsync();
        }

        private async Task StartEdgeServiceAsync()
        {
            if (_edgeBridge is not null) return;
            try
            {
                _edgeBridge = new EdgeServiceFormBridge();
                _edgeBridge.Log += message => XLogClass?.SaveLogString("EDGE", message);
                _edgeBridge.SearchResolved += context => InvokeEdgeSearchAsync(context);
                await _edgeBridge.StartAsync();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { XLogClass?.SaveLogString("EDGE", $"EdgeService 설정/연결 오류: {ex.Message}"); }
        }

        private Task InvokeEdgeSearchAsync(KioskExitContext context)
        {
            if (_edgeClosing || IsDisposed || !IsHandleCreated) return Task.FromCanceled(new CancellationToken(true));
            TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            try { BeginInvoke(new Action(() => { try { ApplyEdgeSearch(context); completion.SetResult(); } catch (Exception ex) { completion.SetException(ex); } })); }
            catch (InvalidOperationException ex) { completion.SetException(ex); }
            return completion.Task;
        }

        private void ApplyEdgeSearch(KioskExitContext context)
        {
            if (IsDisposed) return;
            EdgeSettlementLauncher.Show(this, this, _edgeBridge!.Client, context);
        }

        private async void MainForm15_Load(object sender, EventArgs e)
        {
            //InitMainLayout();

            XLogClass = ClsLog.Instance;
            this.AutoScaleMode = AutoScaleMode.None;          // 임시: DPI 자동 스케일 끔
            this.FormBorderStyle = FormBorderStyle.None;      // 테두리/제목줄 제거
            this.StartPosition = FormStartPosition.Manual;
            this.TopMost = true;
            //this.Bounds = Screen.FromHandle(this.Handle).Bounds; // 작업표시줄 포함 전체 화면

            APSConfig.mainForm15 = this;
            APSConfig.mainForm = null;

            ReaderAPSConfigFile();
            ContextMenuInit();
            LoadMenuWithPanel();
            if (EdgeServiceEnabled) _ = StartEdgeServiceAsync();

            if (APSConfig.VANTYPE == (int)EnumVanType.SMATRO) {
                if (!string.IsNullOrEmpty(APSConfig.CDPort)) {
                    _creditSmatro = new CreditCardReader(APSConfig.CDPort, APSConfig.CDSpeed);
                    if (_creditSmatro != null) {
                        _creditSmatro.OnRawPacketReceived += OnRawPacketReceived;

                        MakePacketData(Constants.CMD_TX_WAITNG, _smPayData);

                        await Task.Delay(150);
                    }
                }
            }

            AudioDeviceRouter.OutputDeviceChanged += (name) =>
            {
                Console.WriteLine($"[APP] Output => {AudioDeviceRouter.CurrentState}-{name}");
                CurrentState = AudioDeviceRouter.CurrentState;
                if (AudioDeviceRouter.CurrentState == AudioRouteState.UsbActive) {
                    PlaySoundFile("HpStart.mp3", AudioRouteState.UsbActive, 1);
                    MediaPlayer._playMode = PlayMode.SND_USBSTART;
                    UsbPlayOn = true;
                }
                else {
                    if (UsbPlayOn == true) {
                        if (AudioDeviceRouter.CurrentState == AudioRouteState.Idle) {
                            PlaySoundFile("HpStop.mp3", AudioRouteState.Idle, 1);
                        }
                        MediaPlayer._playMode = PlayMode.SND_NORMAL;
                        UsbPlayOn = false;
                    }
                }
            };

            AudioDeviceRouter.RouteByConnectionNow();

            if (APSConfig.APSMODE == 0) {
                _voiceRepeatTimer = new System.Threading.Timer(_ =>
                {
                    _voiceRepeatTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                    if (APSConfig.activeForm == APSConfig.menuForm) {
                        if (CurrentState == AudioRouteState.Idle)
                            MediaPlayer.Play(_repeatFile);
                    }
                }, null, Timeout.Infinite, Timeout.Infinite);

                MediaPlayer.Play(_repeatFile);
            }
            _sndQueue = new SoundQueueLite(new OneCoreTts());
            _sndQueue.SetScene("mainform");

            MediaPlayer._playMode = PlayMode.SND_NORMAL;
            MediaPlayer.PlaybackCompleted += MediaPlayer_PlaybackCompleted;

            _clockTimer = new System.Threading.Timer(OnClockTimer, null, 0, 1000);

            //await InitConfigAsync();

            if (EdgeServiceEnabled)
                SetConfigReady(true);
            else
                await StartConfigLoadAsync();

            MainEndIndex = GetEndTabIndex(this);

            if (!EdgeServiceEnabled) {
                _displays = new LDMDisplayManager(_LDMIP, _LDMPORT);
                _displays.Disconnected += _displays_Disconnected;
                _displays.MessageReceived += _displays_MessageReceived;
            }
            if (APSConfig.HOSTUSE == true) {
                _udpSocket = new UDPSocket();
                _udpSocket.OnConnected += _udpSocket_OnConnected;
                _udpSocket.OnReceived += _udpSocket_OnReceived;
                _udpSocket.OnDisconnected += _udpSocket_OnDisconnected;
                _udpSocket.OnError += _udpSocket_OnError;

                _udpSocket.Connect(APSConfig.HOSTIP!, APSConfig.HOSTPORT);
            }
            if (!EdgeServiceEnabled && APSConfig.LprXUse == true) {
                if (!string.IsNullOrEmpty(APSConfig.LprXIp)) {
                    XLprSocket = new XLocalSocket(APSConfig.LprXIp, APSConfig.LprXPort);
                    XLprSocket.OnLocalConnectEvent += OnLocalConnectEvent;
                    XLprSocket.OnLocalNotConnectEvent += OnLocalNotConnectEvent;
                    XLprSocket.OnLocalDisconnectedEvent += OnLocalDisconnectedEvent;
                    XLprSocket.OnLocalReceiveEvent += OnLocalReceiveEvent;

                    XLprSocket.SocketConnect(APSConfig.LprXIp, APSConfig.LprXPort);
                }
            }


            //picLogo.BackgroundImage = Image.FromFile(@".\Image\logo.png");

            if (!string.IsNullOrEmpty(APSConfig.MainPort)) {
                _mainControl = new MainControl(APSConfig.MainPort, APSConfig.MainSpeed);
                if (_mainControl != null) {
                    _mainControl.OnPacketReceived += OnMainPacketReceived;
                }
            }

            string imgUrl = ConfigurationManager.AppSettings["IMGSVRIP"] ?? "http://192.168.0.190:5284";
            ImageDownloadHelper.Init(imgUrl);
        }

        private void _displays_MessageReceived(int idx, byte[] data)
        {
            int nullIndex = Array.IndexOf(data, (byte)0x00);
            int len = (nullIndex >= 0) ? nullIndex : data.Length;

            string txt = Encoding.GetEncoding("ks_c_5601").GetString(data, 0, len);

            Console.WriteLine($"LDM RX => {idx:D2}-{txt}");
        }

        private void _displays_Disconnected(int idx)
        {
            _XLDMTimerTick[idx] = 0;
            _oldMin[idx] = -1;
        }

        private void OnClockTimer(object? state)
        {
            if (IsDisposed || !IsHandleCreated)
                return;

            try {
                UpdateClockAndDisplay();
            }
            catch {
                // 폼 종료 중이면 무시
            }
        }

        private void UpdateClockAndDisplay()
        {
            if (IsDisposed || !IsHandleCreated)
                return;

            DateTime now = DateTime.Now;
            for (int i = 0; i < 4; i++) {
                ProcessDisplayTimer(i, now);
            }
        }

        private void ProcessDisplayTimer(int index, DateTime now)
        {
            if (_displays != null && _displays._displays[index] != null) {
                if (_XLDMTimerTick[index] == 0) {
                    if (_oldMin[index] != now.Minute) {
                        string strtime = MakeDisplayTimeText(now);

                        _displays?.LDMDisplayOneLineSend(index, strtime, 'I', 2, 1, 100);
                        _oldMin[index] = now.Minute;
                    }
                }
                else {
                    _XLDMTimerTick[index]--;
                    if (_XLDMTimerTick[index] == 0) {
                        _displays?.LDMReset(index);
                        _oldMin[index] = -1;
                    }
                }
            }
        }

        public void LDMReset(int ldmNum, int delay)
        {
            _XLDMTimerTick[ldmNum] = delay;
            _oldMin[ldmNum] = -1;
            if (delay == 0)
                _displays?.LDMReset(ldmNum);
        }

        private string MakeDisplayTimeText(DateTime now)
        {
            if (now.Hour >= 12)
                return $"^YPM     {now:HH:mm}";

            return $"^YAM     {now:HH:mm}";
        }

        public void voiceRepeatTimerChange(int duetime, int period)
        {
            if (_voiceRepeatTimer != null)
                _voiceRepeatTimer?.Change(duetime, period);
        }

        public void ProcessArrow(Form form, Keys key, int startIndex, int endIndex)
        {
            bool forward = (key == Keys.Right);
            if (form == null)
                return;

            Control? cur = GetDeepActiveControl(form);
            Button? btn;

            // 1) 포커스 없으면: 이 폼의 startIndex로
            if (cur == null) {
                var first = FindButtonByTabIndex(startIndex) ?? FindFirstButton(form);
                btn = first as Button;
                if (btn != null) {
                    MediaPlayer.Play($"{btn.Tag}");
                }
                first?.Focus();
                return;
            }
            int curIndex = cur.TabIndex;
            bool atEndForward = forward && curIndex == endIndex;
            bool atEndBackward = !forward && curIndex == startIndex;

            bool isMain = ReferenceEquals(form, this);

            // 2) 서브/메뉴 폼 끝 -> 메인으로 점프
            if (!isMain && (atEndForward || atEndBackward)) {
                int mainIdx = forward ? MainStartIndex : MainEndIndex; // 네가 쓰는 매핑 유지
                var target = FindButtonByTabIndex(mainIdx);
                btn = target as Button;
                if (btn != null) {
                    MediaPlayer.Play($"{btn.Tag}");
                }
                target?.Focus();
                return;
            }

            // 3) 메인 폼에서 시작/끝 -> activeForm으로 되돌리기 (MenuForm은 제외)
            if (isMain && (atEndForward || atEndBackward)) {
                var sub = APSConfig.activeForm as Form;
                if (sub != null && sub.Visible) {
                    int subStart = GetStartTabIndex(sub);
                    int subEnd = GetEndTabIndex(sub);
                    int idx = forward ? subStart : subEnd;
                    var target = FindButtonByFormTabIndex(sub, idx);
                    if (target != null) {
                        btn = target as Button;
                        if (btn != null) {
                            MediaPlayer.Play($"{btn.Tag}");
                        }
                        target.Focus();
                        return;
                    }
                }
                // activeForm 없거나 MenuForm이면 메인 내에서 래핑 그대로 진행
            }

            Form? active = APSConfig.activeForm as Form;
            Control scope;

            if (cur != null) {
                if (active != null && IsUnder(cur, active))
                    scope = active;
                else if (IsUnder(cur, this))
                    scope = this;
                else
                    scope = this;
            }
            else {
                scope = this;
            }

            if (scope == this) {
                if (forward) {
                    if (cur?.TabIndex < MainEndIndex) {
                        cur = FindButtonByTabIndex((int)(cur.TabIndex) + 1);
                        if (cur != null) {
                            cur.Select();
                            if (cur?.Tag is string msg)
                                MediaPlayer.Play(msg);

                            return;
                        }
                    }
                }
                else {
                    if (cur?.TabIndex > MainStartIndex) {
                        cur = FindButtonByTabIndex((int)(cur.TabIndex) - 1);
                        if (cur != null) {
                            cur.Select();
                            if (cur?.Tag is string msg)
                                MediaPlayer.Play(msg);

                            return;
                        }
                    }
                }
            }
            if (cur == null || !IsUnder(cur, scope))
                cur = (scope as ContainerControl)?.ActiveControl ?? scope;

            // 스코프 내부에서만 이동
            scope.SelectNextControl(cur, forward, tabStopOnly: true, nested: true, wrap: true);

            // 결과 포커스에서 음성 태그 처리
            Control? next = this.ActiveControl;                  // 폼 전체의 현재 포커스
            if (next is Form f)
                next = f.ActiveControl;

            if (next?.Tag is string tag)
                MediaPlayer.Play(tag);
        }

        private static Control? GetDeepActiveControl(Form form)
        {
            Control? c = form.ActiveControl;
            while (c is ContainerControl cc && cc.ActiveControl != null)
                c = cc.ActiveControl;
            return c;
        }

        private static int GetStartTabIndex(Control root)
        {
            int min = int.MaxValue;
            void Walk(Control p)
            {
                foreach (Control c in p.Controls) {
                    if (c is Button b && b.Visible && b.Enabled && b.TabStop)
                        min = Math.Min(min, b.TabIndex);
                    if (c.HasChildren && c is not Form)
                        Walk(c);
                }
            }
            Walk(root);
            return (min == int.MaxValue) ? 0 : min;
        }

        private static Control? FindFirstButton(Control root)
        {
            Button? best = null;
            int min = int.MaxValue;
            void Walk(Control p)
            {
                foreach (Control c in p.Controls) {
                    if (c is Button b && b.Visible && b.Enabled && b.TabStop) {
                        if (b.TabIndex < min) { min = b.TabIndex; best = b; }
                    }
                    if (c.HasChildren && c is not Form)
                        Walk(c);
                }
            }
            Walk(root);
            return best;
        }

        private static Control? FindButtonByFormTabIndex(Control root, int tabIndex)
        {
            foreach (Control c in root.Controls) {
                if (c is Button b && b.Visible && b.Enabled && b.TabStop && b.TabIndex == tabIndex) {
                    return b;
                }

                if (c.HasChildren && c is not Form) {
                    var hit = FindButtonByFormTabIndex(c, tabIndex);
                    if (hit != null) {
                        return hit;
                    }
                }
            }
            return null;
        }

        Control? FindButtonByTabIndex(int tabIndex)
        {
            var seq = Enumerable.Empty<Button>();

            //if (panelBottom != null)
            //    seq = seq.Concat(EnumButtons(panelBottom));

            //if (panelQuadNav != null && panelQuadNav.Visible)
            //    seq = seq.Concat(EnumButtons(panelQuadNav));

            // 동일 TabIndex가 둘 이상이면 Name 순으로 안정화
            return seq.Where(b => b.TabIndex == tabIndex)
                      .OrderBy(b => b.Name)
                      .Cast<Control>()
                      .FirstOrDefault();
        }

        static bool IsUnder(Control c, Control scope)
        {
            for (Control? p = c; p != null; p = p.Parent)
                if (ReferenceEquals(p, scope))
                    return true;
            return false;
        }

        private void LoadMenuWithPanel()
        {
            APSConfig.FormHost = panelFrame;

            APSConfig.menuForm15 = new MenuForm15();
            var menu = APSConfig.menuForm15!;
            menu.TopLevel = false;
            menu.FormBorderStyle = FormBorderStyle.None;
            menu.AutoScaleMode = AutoScaleMode.None;
            menu.Parent = APSConfig.FormHost!;
            menu.Dock = DockStyle.Fill;

            menu.Show();

            APSConfig.FormStack.Clear();
            APSConfig.FormStack.Add(menu);
            APSConfig.activeForm = menu as IActiveForm;

            panelFrame.BackgroundImage = Image.FromFile(".\\Image\\centerback.jpg");
        }

        public void XDataCall(int nMoney)
        {
            _creditSmatro?.XDataCall(nMoney);
        }

        //protected override void WndProc(ref Message m)
        //{
        //    const int WM_MOUSEACTIVATE = 0x0021;
        //    const int MA_ACTIVATE = 1;               // 활성화 + 클릭 전달
        //    if (m.Msg == WM_MOUSEACTIVATE) { m.Result = (IntPtr)MA_ACTIVATE; return; }
        //    base.WndProc(ref m);
        //}

        private void ReaderAPSConfigFile()
        {
            APSConfig.HOSTUSE = ConfigurationManager.AppSettings["HOSTUSE"] == "false" ? false : true;
            APSConfig.HOSTIP = ConfigurationManager.AppSettings["HOSTIP"];
            APSConfig.HOSTPORT = Convert.ToInt32(ConfigurationManager.AppSettings["HOSTPORT"]);

            APSConfig.LprXUse = ConfigurationManager.AppSettings["LPRXUSE"] == "false" ? false : true;
            APSConfig.LprXIp = ConfigurationManager.AppSettings["LPRXIP"];
            APSConfig.LprXPort = Convert.ToInt32(ConfigurationManager.AppSettings["LPRXPORT"]);

            APSConfig.LprUse = ConfigurationManager.AppSettings["LPRUSE"] == "false" ? false : true;
            APSConfig.LprPort = Convert.ToInt32(ConfigurationManager.AppSettings["LPRPORT"]);

            APSConfig.ParkingName = ConfigurationManager.AppSettings["ParkingName"];
            APSConfig.ParkingAddress = ConfigurationManager.AppSettings["ParkingAddress"];
            APSConfig.ParkingPhone = ConfigurationManager.AppSettings["ParkingPhone"];
            APSConfig.BusinessNumber = ConfigurationManager.AppSettings["BusinessNumber"];
            APSConfig.OwnerName = ConfigurationManager.AppSettings["OwnerName"];

            APSConfig.AutoCancleTime1 = Convert.ToInt32(ConfigurationManager.AppSettings["AUTOCANCLETIME1"]);
            APSConfig.AutoCancleTime2 = Convert.ToInt32(ConfigurationManager.AppSettings["AUTOCANCLETIME2"]);
            APSConfig.AutoCancleTime3 = Convert.ToInt32(ConfigurationManager.AppSettings["AUTOCANCLETIME3"]);
            APSConfig.AutoCancleTime4 = Convert.ToInt32(ConfigurationManager.AppSettings["AUTOCANCLETIME4"]);
            APSConfig.AutoCancleTime5 = Convert.ToInt32(ConfigurationManager.AppSettings["AUTOCANCLETIME5"]);

            APSConfig.Sitenum = Convert.ToInt32(ConfigurationManager.AppSettings["SITENUM"]);
            APSConfig.Groupnum = Convert.ToInt32(ConfigurationManager.AppSettings["GROUPNUM"]);

            APSConfig.APSMODE = Convert.ToInt32(ConfigurationManager.AppSettings["APSMODE"]);

            APSConfig.VANTYPE = Convert.ToInt32(ConfigurationManager.AppSettings["VANTYPE"]);
            APSConfig.KICCCANCELTIME = Convert.ToInt32(ConfigurationManager.AppSettings["KICCCANCELTIME"]);

            APSConfig.BARRIERFREE = ConfigurationManager.AppSettings["BARRIERFREE"] == "false" ? false : true;
            APSConfig.JungkiMode = ConfigurationManager.AppSettings["JUNGKIMODE"] == "false" ? false : true;

            // 출차시 입차내역없을때 차단기 열기
            APSConfig.NoCarOutGate = ConfigurationManager.AppSettings["NOCAROUTGTAE"] == "false" ? false : true;

            APSConfig.APSNUM = Convert.ToInt32(ConfigurationManager.AppSettings["APSNUM"]);
            APSConfig.APSNAME = ConfigurationManager.AppSettings["APSNAME"];
            APSConfig.APSIP = ConfigurationManager.AppSettings["APSIP"];
            APSConfig.LprBaseNum = Convert.ToInt32(ConfigurationManager.AppSettings["LPRBASENUM"]);

            APSConfig.PrintPort = ConfigurationManager.AppSettings["PRINTPORT"];
            APSConfig.PrintSpeed = Convert.ToInt32(ConfigurationManager.AppSettings["PRINTSPEED"]);
            APSConfig.PrintPaper = Convert.ToInt32(ConfigurationManager.AppSettings["PRINTPAPER"]);

            APSConfig.TDPort = ConfigurationManager.AppSettings["TDPORT"];
            APSConfig.TDSpeed = Convert.ToInt32(ConfigurationManager.AppSettings["TDSPEED"]);

            APSConfig.MainPort = ConfigurationManager.AppSettings["MAINPORT"];
            APSConfig.MainSpeed = Convert.ToInt32(ConfigurationManager.AppSettings["MAINSPEED"]);

            APSConfig.CDPort = ConfigurationManager.AppSettings["CDPORT"];
            APSConfig.CDSpeed = Convert.ToInt32(ConfigurationManager.AppSettings["CDSPEED"]);
            APSConfig.SMTERMID = ConfigurationManager.AppSettings["SMTERMID"];

            //APSConfig.FtpIp = ConfigurationManager.AppSettings["FTPIP"];
            //APSConfig.FtpId = ConfigurationManager.AppSettings["FTPID"];
            //APSConfig.FtpPass = ConfigurationManager.AppSettings["FTPPASS"];

            _LDMIP[0] = ConfigurationManager.AppSettings["LDMIP1"] ?? "";
            _LDMPORT[0] = Convert.ToInt32(ConfigurationManager.AppSettings["LDMPORT1"]);
            _LDMIP[1] = ConfigurationManager.AppSettings["LDMIP2"] ?? "";
            _LDMPORT[1] = Convert.ToInt32(ConfigurationManager.AppSettings["LDMPORT2"]);
            _LDMIP[2] = ConfigurationManager.AppSettings["LDMIP3"] ?? "";
            _LDMPORT[2] = Convert.ToInt32(ConfigurationManager.AppSettings["LDMPORT3"]);
            _LDMIP[3] = ConfigurationManager.AppSettings["LDMIP4"] ?? "";
            _LDMPORT[3] = Convert.ToInt32(ConfigurationManager.AppSettings["LDMPORT4"]);

            _ILDMNum = Convert.ToInt32(ConfigurationManager.AppSettings["ILDMNUM"]) - 1;
            _OLDMNum = Convert.ToInt32(ConfigurationManager.AppSettings["OLDMNUM"]) - 1;

            APSConfig.SoundRepeatTime = Convert.ToInt32(ConfigurationManager.AppSettings["SOUNDREPEATTIME"]);

            if (APSConfig.SoundRepeatTime == 0)
                APSConfig.SoundRepeatTime = 120000;
            else
                APSConfig.SoundRepeatTime *= 1000;

            APSConfig.debugmode = ConfigurationManager.AppSettings["DEBUGMODE"] == "false" ? false : true;
            string[] dpos = (ConfigurationManager.AppSettings["DEBUGPOS"] ?? "10,100").Split(",");
            if (dpos.Length >= 2) {
                APSConfig.DebugX = Convert.ToInt32(dpos[0]);
                APSConfig.DebugY = Convert.ToInt32(dpos[1]);
            }
            else {
                APSConfig.DebugX = 10;
                APSConfig.DebugY = 100;
            }

            if (APSConfig.debugmode) {
                _MyConsole = new ConsoleManager();
                _MyConsole.Open(APSConfig.DebugX, APSConfig.DebugY, 900, 600);
            }

            APSConfig.cutmode = ConfigurationManager.AppSettings["CUTMODE"] == "false" ? false : true;

            //SetProp(this.Handle, "TabletDisablePressAndHold", new IntPtr(1));
            //SetProp(this.Handle, "TabletDisableFlicks", new IntPtr(1));
        }

        private void ContextMenuInit()
        {
            _consoleItem = new ToolStripMenuItem("콘솔창열기");
            _consoleItem.Click += (s, e) =>
            {
                if (_MyConsole == null) {
                    _MyConsole = new ConsoleManager();
                    _consoleItem.Text = "콘솔창닫기";
                    _MyConsole.Open();
                }
                else {
                    _MyConsole.Close();
                    _MyConsole = null;
                    _consoleItem.Text = "콘솔창열기";
                }
            };

            _CTXMenu.Items.Add(_consoleItem);
            _CTXMenu.Items.Add("최소화", null, (s, e) => { this.WindowState = FormWindowState.Minimized; });
            _CTXMenu.Items.Add("전광판제어", null, (s, e) => { new LDMShow().ShowDialog(); });
            _CTXMenu.Items.Add("승인취소", null, (s, e) => { new CardCancelForm().ShowDialog(); });
            _CTXMenu.Items.Add("신용리더기설정", null, (s, e) =>
            {
                _smDevice = new SmatroDeviceFrm();
                _smDevice.ShowDialog();
                _smDevice = null;
            });
            _CTXMenu.Items.Add("요금설정", null, (s, e) =>
            {
                var feedlg = new FeeStepForm();
                feedlg.ShowDialog();
            });
            _CTXMenu.Items.Add("요금계산", null, (s, e) => this.ParkInTimeCall());

            _CTXMenu.Items.Add("환경설정", null, (s, e) => this.ConfigSystemCall());

            _CTXMenu.Items.Add("종료", null, (s, e) => this.Close());

            if (!EdgeServiceEnabled && APSConfig.LprUse) {
                _cameraServer = new LPRCameraServer(APSConfig.LprPort);
                _cameraServer.PacketReceived += OnPlateDataReceived;
                _cameraServer.Start();
            }
        }

        public void ConfigSystemCall()
        {
            ConfigForm frm = new ConfigForm();

            frm.ShowDialog();
        }

        public LprPacket? TryParse(string payload)
        {
            LprPacket pkt = new LprPacket();
            try {
                pkt.Recognized = payload[0] == '1';
                pkt.Direction = payload[1].ToString();
                pkt.DeviceNum = int.Parse(payload.Substring(2, 3));
                pkt.CarType = payload[5] - '0';
                pkt.IOdatetime = DateTime.ParseExact(payload.Substring(6, 14), "yyyyMMddHHmmss", null);

                pkt.Uniq = payload.Substring(20, 8);
                int dash = payload.IndexOf('_') + 1;
                int dot = payload.IndexOf('.');
                pkt.Carnum = payload.Substring(dash, dot - dash);
                pkt.image = payload;
                return pkt;
            }
            catch { }

            return null;
        }

        private bool ILPRParkInData(LprPacket pData)
        {
            //string ticketData = $"{APSConfig.MainTicketNum:D5}1{APSConfig.Groupnum:D3}{APSConfig.APSNUM:D3}";
            //CarIoSendData sendData = new CarIoSendData
            //{
            //    Sitenum = (short)APSConfig.Sitenum,
            //    Groupnum = (short)APSConfig.Groupnum,
            //    Ticketcartype = 1,
            //    Parkcartype = 1,
            //    Ticketdata = ticketData,
            //    Ticketnum = APSConfig.MainTicketNum++,
            //    Carnum = pData.Carnum!,
            //    Devicenum = (short)APSConfig.APSNUM,
            //    Iotime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            //    Inimage = pData?.image,
            //    Intick = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            //};

            //string? content = await RestHelper.Instance.PostAsync<CarIoSendData>("/api/CarIo/parkin", sendData);
            //if (string.IsNullOrWhiteSpace(content))
            //    return false;

            //var response = JsonConvert.DeserializeObject<CarInResponse>(content);

            return true;
        }

        public bool ILPRCheckMemberData(Tperiodmember member, LprPacket? pData)
        {
            Tperiodinout tio = new Tperiodinout();
            string ldmText1 = "";
            string ldmText2 = "";
            DateTime now = DateTime.Now;
            int carLastNum = 0x00;
            int jPassNAK;

            if (member.Useflag != null && member.Useflag == 0) {
                ldmText1 = $"^W{member.Carnum1}";
                ldmText2 = $"^R사용중지차량";
                LDMDisplayDataSend(_ILDMNum, ldmText1, ldmText2, 'I', 0, 11);
                return false;
            }

            string nowDate = now.ToString("yyyy-MM-dd");
            string startDate = member.Startdate.ToString("yyyy-MM-dd");
            string endDate = member.Enddate.ToString("yyyy-MM-dd");

            if (string.Compare(nowDate, startDate, StringComparison.Ordinal) < 0) {
                ldmText2 = $"^R시작일자미달";
                return true;
            }
            tio.Enddate = member.Enddate;
            switch (member.Parklevel) {
                case 0:
                    break;
                case 1: {
                        int xLen = pData!.Carnum!.Length;
                        carLastNum = (pData!.Carnum[xLen - 1] - '0') % 2;
                        if (now.DayOfWeek != DayOfWeek.Saturday && now.DayOfWeek != DayOfWeek.Sunday) {
                            if (carLastNum != now.Day % 2) {
                                ldmText2 = $"^R홀짝수 위반";
                                LDMDisplayDataSend(_ILDMNum, ldmText1, ldmText2, 'I', 0, 11);
                                jPassNAK = 0x11;
                            }
                        }
                        break;
                    }
                case 2: {
                        int xLen = pData!.Carnum!.Length;
                        carLastNum = (pData!.Carnum[xLen - 1] - '0') % 10;

                        if (now.DayOfWeek != DayOfWeek.Saturday && now.DayOfWeek != DayOfWeek.Sunday) {
                            if (carLastNum == now.Day % 10) {
                                ldmText2 = $"^R10부제 위반";
                                LDMDisplayDataSend(_ILDMNum, ldmText1, ldmText2, 'I', 0, 11);
                                jPassNAK = 0x11;
                            }
                        }
                        break;
                    }

                case 3: {
                        string validDay = member.Parkvalidday!;

                        int xCodeVal = (int)(now.DayOfWeek) == 1 ? 6 : (int)(now.DayOfWeek) - 2;

                        if (!string.IsNullOrEmpty(validDay) && validDay.Length > xCodeVal && validDay[xCodeVal] == '0') {
                            ldmText2 = $"^R요일제 위반";
                            LDMDisplayDataSend(_ILDMNum, ldmText1, ldmText2, 'I', 0, 11);
                            jPassNAK = 0x11;
                        }
                        break;
                    }
            }

            int nowMinute = now.Hour * 60 + now.Minute;
            int weekOfDay = (int)now.DayOfWeek;
            int startMinute = 8 * 60 + 0;
            int endMinute = 18 * 60 + 0;
            switch (member.Parktype) {
                case 0:
                    break;
                case 1:
                    if (nowMinute < startMinute || nowMinute >= endMinute) {
                        ldmText2 = $"^R주차시간위반";
                        LDMDisplayDataSend(_ILDMNum, ldmText1, ldmText2, 'I', 0, 11);
                        jPassNAK = 0x11;
                    }
                    break;

                case 2:
                    if (nowMinute >= startMinute && nowMinute < endMinute) {
                        ldmText2 = $"^R주차시간위반";
                        LDMDisplayDataSend(_ILDMNum, ldmText1, ldmText2, 'I', 0, 11);
                        jPassNAK = 0x11;
                    }
                    break;

                case 3: {
                        string parkTimeTime = member.Parktimetime!;
                        int customStart = ParseHHmmRangeToMinute(parkTimeTime, 0);
                        int customEnd = ParseHHmmRangeToMinute(parkTimeTime, 1);

                        startMinute = customStart;
                        endMinute = customEnd;

                        if (customStart < customEnd) {
                            if (nowMinute < customStart || nowMinute > customEnd) {
                                ldmText2 = $"^R주차시간위반";
                                LDMDisplayDataSend(_ILDMNum, ldmText1, ldmText2, 'I', 0, 11);
                                jPassNAK = 0x11;
                            }
                        }
                        else {
                            if (nowMinute < customStart && nowMinute > customEnd) {
                                ldmText2 = $"^R주차시간위반";
                                LDMDisplayDataSend(_ILDMNum, ldmText1, ldmText2, 'I', 0, 11);
                                jPassNAK = 0x11;
                            }
                        }
                        break;
                    }
            }

            tio.Sitenum = (short)APSConfig.Sitenum;
            tio.Groupnum = (short)APSConfig.Groupnum;
            tio.Carnum = member.Carnum1;
            tio.Indevicenum = (short)APSConfig.Sitenum;
            tio.Outdevicenum = (short)APSConfig.Groupnum;
            tio.Indevicenum = (short)APSConfig.APSNUM;
            tio.Indate = now;
            tio.Inhour = (short)now.Hour;
            tio.Inmin = (short)now.Minute;
            tio.Cartype = member.Cartype1;
            tio.Inimage = pData?.image;
            tio.Enddate = member.Enddate;
            tio.Outdate = now;
            tio.Outhour = (short)now.Hour;
            tio.Outmin = (short)now.Minute;
            tio.Outimage = "";
            tio.Parktime = 0;

            STTPERIODINOUT stio = new STTPERIODINOUT();
            StructHelper.CopyTperiodinoutToStruct(tio!, ref stio);
            XSendPacketData<STTPERIODINOUT>(req_cmd_code.APS_CMD_PERIODIN, stio, "0.0.0.0");

            ldmText1 = $"^W{tio.Carnum}";
            //ldmText2 = $"^R기간{tio.Enddate?.Month:D2}월{tio.Enddate?.Day:D2}일";
            ldmText2 = $"^R  등록차량  ";

            if (_displays != null && _displays._displays[_ILDMNum] != null) {
                _displays.GateCommand(_ILDMNum, GateCmd.GATEOPEN);
                Thread.Sleep(100);
                LDMDisplayDataSend(_ILDMNum, ldmText1, ldmText2, 'I', 0, 11);
            }
            return true;
        }

        public bool OLPRCheckMemberData(Tperiodmember member, LprPacket? pData)
        {
            Tperiodinout tio = new Tperiodinout();
            string ldmText1 = "";
            string ldmText2 = "";
            DateTime now = DateTime.Now;
            int carLastNum = 0x00;
            int jPassNAK = 0;

            jPassNAK = 0x00;
            if (member.Useflag != null && member.Useflag == 0) {
                ldmText1 = $"^W{member.Carnum1}";
                ldmText2 = $"^R사용중지차량";
                LDMDisplayDataSend(_ILDMNum, ldmText1, ldmText2, 'I', 0, 11);
                return false;
            }

            string nowDate = now.ToString("yyyy-MM-dd");
            string startDate = member.Startdate.ToString("yyyy-MM-dd");
            string endDate = member.Enddate.ToString("yyyy-MM-dd");

            if (string.Compare(nowDate, startDate, StringComparison.Ordinal) < 0) {
                ldmText2 = $"^R시작일자미달";
                return true;
            }
            tio.Enddate = member.Enddate;
            switch (member.Parklevel) {
                case 0:
                    break;
                case 1: {
                        int xLen = pData!.Carnum!.Length;
                        carLastNum = (pData!.Carnum[xLen - 1] - '0') % 2;
                        if (now.DayOfWeek != DayOfWeek.Saturday && now.DayOfWeek != DayOfWeek.Sunday) {
                            if (carLastNum != now.Day % 2) {
                                ldmText2 = $"^R홀짝수 위반";
                                LDMDisplayDataSend(_ILDMNum, ldmText1, ldmText2, 'I', 0, 11);
                                jPassNAK = 0x11;
                            }
                        }
                        break;
                    }
                case 2: {
                        int xLen = pData!.Carnum!.Length;
                        carLastNum = (pData!.Carnum[xLen - 1] - '0') % 10;

                        if (now.DayOfWeek != DayOfWeek.Saturday && now.DayOfWeek != DayOfWeek.Sunday) {
                            if (carLastNum == now.Day % 10) {
                                ldmText2 = $"^R10부제 위반";
                                LDMDisplayDataSend(_ILDMNum, ldmText1, ldmText2, 'I', 0, 11);
                                jPassNAK = 0x11;
                            }
                        }
                        break;
                    }

                case 3: {
                        string validDay = member.Parkvalidday!;

                        int xCodeVal = (int)(now.DayOfWeek) == 1 ? 6 : (int)(now.DayOfWeek) - 2;

                        if (!string.IsNullOrEmpty(validDay) && validDay.Length > xCodeVal && validDay[xCodeVal] == '0') {
                            ldmText2 = $"^R요일제 위반";
                            LDMDisplayDataSend(_ILDMNum, ldmText1, ldmText2, 'I', 0, 11);
                            jPassNAK = 0x11;
                        }
                        break;
                    }
            }

            int nowMinute = now.Hour * 60 + now.Minute;
            int weekOfDay = (int)now.DayOfWeek;
            int startMinute = 8 * 60 + 0;
            int endMinute = 18 * 60 + 0;
            switch (member.Parktype) {
                case 0:
                    break;
                case 1:
                    if (nowMinute < startMinute || nowMinute >= endMinute) {
                        ldmText2 = $"^R주차시간위반";
                        LDMDisplayDataSend(_ILDMNum, ldmText1, ldmText2, 'I', 0, 11);
                        jPassNAK = 0x11;
                    }
                    break;

                case 2:
                    if (nowMinute >= startMinute && nowMinute < endMinute) {
                        ldmText2 = $"^R주차시간위반";
                        LDMDisplayDataSend(_ILDMNum, ldmText1, ldmText2, 'I', 0, 11);
                        jPassNAK = 0x11;
                    }
                    break;

                case 3: {
                        string parkTimeTime = member.Parktimetime!;
                        int customStart = ParseHHmmRangeToMinute(parkTimeTime, 0);
                        int customEnd = ParseHHmmRangeToMinute(parkTimeTime, 1);

                        startMinute = customStart;
                        endMinute = customEnd;

                        if (customStart < customEnd) {
                            if (nowMinute < customStart || nowMinute > customEnd) {
                                ldmText2 = $"^R주차시간위반";
                                LDMDisplayDataSend(_ILDMNum, ldmText1, ldmText2, 'I', 0, 11);
                                jPassNAK = 0x11;
                            }
                        }
                        else {
                            if (nowMinute < customStart && nowMinute > customEnd) {
                                ldmText2 = $"^R주차시간위반";
                                LDMDisplayDataSend(_ILDMNum, ldmText1, ldmText2, 'I', 0, 11);
                                jPassNAK = 0x11;
                            }
                        }
                        break;
                    }
            }

            tio.Sitenum = (short)APSConfig.Sitenum;
            tio.Groupnum = (short)APSConfig.Groupnum;
            tio.Carnum = member.Carnum1;
            tio.Indevicenum = (short)APSConfig.Sitenum;
            tio.Outdevicenum = (short)APSConfig.Groupnum;
            tio.Indevicenum = (short)APSConfig.APSNUM;
            tio.Indate = now;
            tio.Inhour = (short)now.Hour;
            tio.Inmin = (short)now.Minute;
            tio.Cartype = member.Cartype1;
            tio.Inimage = pData?.image;
            tio.Enddate = member.Enddate;
            tio.Outdate = now;
            tio.Outhour = (short)now.Hour;
            tio.Outmin = (short)now.Minute;
            tio.Outimage = "";
            tio.Outflag = 79;
            tio.Parktime = 0;

            STTPERIODINOUT stio = new STTPERIODINOUT();
            StructHelper.CopyTperiodinoutToStruct(tio!, ref stio);
            XSendPacketData<STTPERIODINOUT>(req_cmd_code.APS_CMD_PERIODINOUT, stio, "0.0.0.0");

            ldmText1 = $"^W{tio.Carnum}";
            //ldmText2 = $"^R기간{tio.Enddate?.Month:D2}월{tio.Enddate?.Day:D2}일";
            ldmText2 = $"^R  등록차량  ";
            if (_displays != null && _displays._displays[_OLDMNum] != null) {
                _displays?.GateCommand(_OLDMNum, GateCmd.GATEOPEN);
                Thread.Sleep(100);
                LDMDisplayDataSend(_OLDMNum, ldmText1, ldmText2, 'I', 0, 11);

                XLogClass!.SaveLogString("CAL", $"종료일자 {tio.Enddate?.Month:D2}월{tio.Enddate?.Day:D2}일 등록차량출차");
            }
            return true;
        }

        private string MakeLikePatternForCarNum(string carNum)
        {
            if (string.IsNullOrEmpty(carNum))
                return string.Empty;

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < carNum.Length; i++) {
                if (carNum[i] > 127) {
                    sb.Append('_');
                }
                else {
                    sb.Append(carNum[i]);
                }
            }
            return sb.ToString();
        }

        private int ParseHHmmRangeToMinute(string text, int index)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length < 11)
                return 0;

            if (index == 0) {
                int hh = int.Parse(text.Substring(0, 2));
                int mm = int.Parse(text.Substring(3, 2));
                return hh * 60 + mm;
            }
            else {
                int hh = int.Parse(text.Substring(6, 2));
                int mm = int.Parse(text.Substring(9, 2));
                return hh * 60 + mm;
            }
        }

        public void LDMDisplayDataSend(int ldmnum, string ldmText1, string ldmText2, char memory, int rotate, int viewtime)
        {
            Console.WriteLine($"LDM[{ldmnum}] : {ldmText1} - {ldmText2}");
            if (_displays != null) {
                _displays.LDMDisplayDataSend(ldmnum, ldmText1, ldmText2, memory, rotate, viewtime);

                _XLDMTimerTick[ldmnum] = viewtime;
            }
        }

        public void LDMTextDisplay(int ldmNum, int type, int viewtime, string ltext1, string ltext2)
        {
            if (_displays != null) {
                _displays.LDMDisplayDataSend(ldmNum, ltext1, ltext2, 'I', 0, viewtime);

                _XLDMTimerTick[ldmNum] = viewtime;
            }
        }

        private async void OnPlateDataReceived(LPRCameraSession clsocket, byte[] packet)
        {
            bool memberchk = false;
            LprPacket? pData;
            int ldmNum = 0;
            bool chkok;

            UiHost.ResetToMenu();
            if (APSConfig.activeForm is ParkCalForm15) {
                SmatroWaiting();
            }
            Task.Delay(150).Wait();   // 100~200ms
            string payload = Encoding.GetEncoding("ks_c_5601").GetString(packet);
            Invoke(() => Console.WriteLine($"[인식기 수신] {payload}"));
            XLogClass!.SaveLogString("REG", payload);
            if (payload[0] == 'C' && payload[1] == 'D') {
                pData = TryParse(payload.Substring(4));
            }
            else {
                pData = TryParse(payload);
            }

            if (pData != null) {
                 bool isPassCar = pData.Carnum!.Contains("아") ||
                                  pData.Carnum!.Contains("바") ||
                                  pData.Carnum!.Contains("사") ||
                                  pData.Carnum!.Contains("자") ||
                                  pData.Carnum!.Contains("배");
                if (isPassCar) {
                    if (_displays != null && _displays._displays[_OLDMNum] != null) {
                        _displays?.GateCommand(_OLDMNum, GateCmd.GATEOPEN);
                        await Task.Delay(100);
                        LDMDisplayDataSend(_OLDMNum, $"^W{pData.Carnum}", $"^R서행하십시요", 'I', 0, 11);
                    }
                    return;
                }
                ldmNum = (int)(pData.DeviceNum - APSConfig.LprBaseNum);
                if (pData.Recognized) {
                    ParkCache.Clear(); // 캐시 초기화
                    _memberOverTime = 0;
                    _memberOverDay = 0;
                    chkok = RestrictionHelper.RestrictionDayOfWeek(pData.Carnum!, 4);
                    if (chkok == false) {
                        Console.WriteLine($"요일제 위반차량 : {pData.Carnum}");
                    }
                    bool ret = await CarParkSearch(pData);
                    if (ParkCache.Periodmembers.Count > 0) {
                        foreach (Tperiodmember member in ParkCache.Periodmembers) {
                            if (member.Carnum1.Equals(pData.Carnum)) {
                                DateTime enddate = member.Enddate;
                                if (DateOnly.FromDateTime(enddate) >= DateOnly.FromDateTime(DateTime.Now)) {
                                    if (pData.Direction == "I'") {
                                        Console.WriteLine($"등록차량 입차 종료일자 {enddate.ToString("yyyy-MM-dd")}");
                                        memberchk = ILPRCheckMemberData(member, pData);
                                    }
                                    else {
                                        Console.WriteLine($"등록차량 출차 종료일자 {enddate.ToString("yyyy-MM-dd")}");
                                        memberchk = OLPRCheckMemberData(member, pData);
                                        if (memberchk == true) {
                                            if (APSConfig.APSMODE == 1) {
                                                var request = new CarOutRequest
                                                {
                                                    Sitenum = (short)APSConfig.Sitenum,
                                                    Groupnum = (short)APSConfig.Groupnum,
                                                    Devicenum = (short)APSConfig.APSNUM,
                                                    CarGubun = 2,
                                                    Carnum = pData.Carnum ?? "",
                                                    Iotime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                                    Outimage = pData.image
                                                };
                                                await RestHelper.Instance.PostAsync<CarOutRequest>("/api/Carout", request);
                                            }
                                         }
                                    }
                                }
                                else {
                                    string ldmText1 = $"^R{pData.Carnum}";
                                    string ldmText2 = $"^R주차기간오류";
                                    LDMDisplayDataSend(_OLDMNum, ldmText1, ldmText2, 'I', 0, 11);
                                }
                            }
                        }
                    }
                    if (memberchk == false) {
                        if (pData.Direction == "I") {
                            Console.WriteLine("${pData.Carnum} 일반차량입차");
                            _ = ILPRParkInData(pData);
                        }
                        else {
                            if (ParkCache.Parkinfos.Count > 0) {
                                CarCountCheck(ParkCache.Parkinfos.Count);
                            }
                            else {
                                string ldmText1 = $"^R{pData.Carnum}";
                                string ldmText2 = $"^G입차내역없음";

                                if (_displays != null && _displays._displays[_OLDMNum] != null) {
                                    if (APSConfig.NoCarOutGate == true) {
                                        _displays?.GateCommand(_OLDMNum, GateCmd.GATEOPEN);
                                        await Task.Delay(100);
                                    }

                                    LDMDisplayDataSend(_OLDMNum, ldmText1, ldmText2, 'I', 0, 11);
                                }
                                PlaySoundFile($"error14.mp3", AudioRouteState.Idle, 1);
                                UiHelpers.ShowMessage(this, 0, true);
                            }
                        }
                    }
                }
                else {
                    if (pData.Direction == "I") {
                        Console.WriteLine("미인식차량입차");
                    }
                    else {
                        Console.WriteLine("미인식차량출차");
                    }
                }
            }
        }

        public void ParkCalFormDisplay()
        {
            string ldmtext;
            int ldmNum = _OLDMNum;

            if (ParkCache.Parkinfos != null) {
                if (ParkCache.Parkinfos.Count == 1) {
                    var Parkinfo = ParkCache.Parkinfos.FirstOrDefault();
                    if (Parkinfo != null) {
                        ldmtext = $"^W{Parkinfo.Carnum ?? ""}";
                        //LDMDisplayDataSend(ldmNum, ldmtext, "^G주차요금을 정산합니다.", 'I', 0, 10);
                        HandleExitCarDetected(Parkinfo, null, ldmNum);
                        //TTSWrapper.SpeakText($"차량 번호 {Parkinfo.Carnum} 를 선택하여 정산을 합니다.", 0);
                    }
                }
                else {
                    if (ParkCache.Parkinfos.Count > 1) {
                        UiHost.ShowActiveForm<string>(this, new CarSelectForm15(false), (result, carNum) =>
                        {
                            Result = result;
                            ResultData = carNum;
                            if (result == FormResult.FormOk) {
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
                            }
                        });
                    }

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
            var oldForm = _calForm;
            _calForm = null;

            if (oldForm != null && !oldForm.IsDisposed) {
                oldForm.Close();
            }

            if (carInfo != null) { _xparkinfo.CopyFrom(carInfo!); }
            if (tmember != null) { _xperiodmember.CopyFrom(tmember!); }

            var newForm = new ParkCalForm15(ldmIndex, false)
            {
                _parkinfo = _xparkinfo,
                _periodmember = _xperiodmember
            };
            _calForm = newForm;

            UiHost.ShowActiveForm<string>(this, newForm, (result, data) =>
            {
                Result = result;

                if (ReferenceEquals(_calForm, newForm))
                    _calForm = null;
            });
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
                ParkCalFormDisplay();
            }
            else {
                //TTSWrapper.SpeakText("조회된 차량이 없습니다.", 0);
                PlaySoundFile("main03.mp3", AudioRouteState.Idle);
            }
        }

        public async Task<bool> CarParkSearch(LprPacket pData)
        {
            string? content = string.Empty;
            StringBuilder sb = new StringBuilder();
            ParkCache.Clear(); // 캐시 초기화

            XLogClass!.SaveLogString("CAL", $"LPR-[{pData.Direction}] {pData.Carnum}");
            if (pData.Direction == "I") {
                var request = new CarInRequest
                {
                    Sitenum = (short)APSConfig.Sitenum,
                    Groupnum = (short)APSConfig.Groupnum,
                    Carnum = pData.Carnum ?? "",
                    Parkcartype = (short)pData.CarType,
                    Iotime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Inimage = pData.image
                };

                try {
                    content = await RestHelper.Instance.PostAsync<CarInRequest>("/api/Insert", request);
                    if (string.IsNullOrWhiteSpace(content))
                        return false;
                    string pretty = JToken.Parse(content).ToString(Formatting.Indented);
                    XLogClass!.SaveLogString("CNT", pretty);
                    //Console.WriteLine(content);
                }
                catch { return false; }

                var response = JsonConvert.DeserializeObject<CarInResponse>(content);
                if (response == null)
                    return false;

                ParkCache.Periodmembers.Clear();
                if (response.PeriodMember != null) { 
                    ParkCache.Periodmembers.Add(response.PeriodMember);
                    Console.WriteLine($"사용자: {response.PeriodMember.Name}  차량번호 : {response.PeriodMember.Carnum1} 종료일자:{response.PeriodMember.Enddate}");
                }
                return true;
            }
            else {
                var request = new CarCalcRequest
                {
                    Sitenum = (short)APSConfig.Sitenum,
                    Groupnum = (short)APSConfig.Groupnum,
                    Carnum = pData.Carnum ?? "",
                    CarGubun = APSConfig.APSMODE == 1 ? 3 : 1           //  // 1: 일반, 2: 등록 3:일반+등록(출구에서사용)
                };

                try {
                    content = await RestHelper.Instance.PostAsync<CarCalcRequest>("/api/Carcalc", request);
                    if (string.IsNullOrWhiteSpace(content))
                        return false;
                    string pretty = JToken.Parse(content).ToString(Formatting.Indented);
                    XLogClass!.SaveLogString("CNT", pretty);
                    //Console.WriteLine(content);
                }
                catch { return false; }

                var response = JsonConvert.DeserializeObject<CarCalcResponse>(content);
                if (response == null)
                    return false;

                ParkCache.Periodmembers.Clear();
                ParkCache.Parkinfos.Clear();
                ParkCache.Dispersions.Clear();
                ParkCache.Discountinfos.Clear();
                ParkCache.Bcardinfos.Clear();

                if (response.PeriodMembers != null && response.PeriodMembers.Count > 0) {   // 등록차량
                    ParkCache.Periodmembers.Clear();
                    ParkCache.Periodmembers.Add(response.PeriodMembers[0]);

                    Tperiodmember periodMember = response.PeriodMembers[0];
                    Console.WriteLine($"사용자: {periodMember.Name}  차량번호 : {periodMember.Carnum1} 종료일자:{periodMember.Enddate}");
                }
                if (response.Cars != null && response.Cars.Count > 0) {    // 일반차량
                    CarCalcItem carItem = response.Cars[0];
                    if (carItem.Tparkinfo != null) {
                        carItem.Tparkinfo.Outimage = pData?.image;
                        ParkCache.Parkinfos.Add(carItem.Tparkinfo);

                        Console.WriteLine($"장치번호 : {carItem.Tparkinfo.Indevicenum}  차량번호 : {carItem.Tparkinfo.Carnum}");
                    }

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
                if (ParkCache.GetCarMatchCount() > 0) {
                    return true;
                }
            }

            return false;
        }

        private void ParkInTimeCall()
        {
            ParkInTimeFrm parkInTimeForm = new ParkInTimeFrm();

            parkInTimeForm.ShowDialog();
        }

        private void OnRawPacketReceived(byte flag, byte[]? data, int length)
        {
            byte[] ackData = { 0x06 };

            if (flag == Constants.ASCII_ACK) {
                Console.WriteLine("SM ACK 수신");
                XLogClass?.SaveLogString("CDI", "SM ACK 수신");
                return;
            }

            if (flag == Constants.ASCII_NAK) {
                Console.WriteLine("SM NACK 수신");
                XLogClass?.SaveLogString("CDI", "SM NACK 수신");
                return;
            }

            if (flag != 0x88 || data == null)
                return;

            try {
                if (!SmartroPacketParser.ParsePacket(data, length, out SmartroPacket packet)) {
                    XLogClass?.SaveLogString("CDI", "SM 패킷 파싱 실패");
                    return;
                }

                Console.WriteLine("CRX       => {0}", length);
                Console.WriteLine("Terminal  => {0}", packet.TerminalId);
                Console.WriteLine("DateTime  => {0}", packet.DateTimeText);
                Console.WriteLine("JobCode   => {0}", (char)packet.JobCode);
                Console.WriteLine("ResCode   => {0:X2}", packet.ResponseCode);
                Console.WriteLine("BodyLen   => {0}", packet.BodyLength);

                if (packet.BodyLength > 0) {
                    Console.WriteLine("Body      => {0}",Encoding.GetEncoding("ks_c_5601").GetString(packet.Body));
                }
                if (packet.JobCode != (byte)'@') {
                    Console.WriteLine("SM ACK 송신");
                    _creditSmatro?.WriteByte(ackData, 1);
                }

                switch ((char)packet.JobCode) {
                    case 'a':
                        SmartroDeviceStatus status = SmartroPacketParser.ParseDeviceStatus(packet.Body);
                        XLogClass?.SaveLogString("CDI",$"장치상태 Card={status.CardModule}, RF={status.RfModule}, VAN={status.VanServer}, LINK={status.LinkServer}");
                        break;
                    case 'e':
                        XLogClass?.SaveLogString("CDI", "결제대기 응답 수신");
                        break;
                    case 'b':
                    case 'g':
                    case 'l':
                        SmartroApprovalResponse resa = SmartroPacketParser.ParseApproval(packet.Body);
                        if (resa.IsSuccess) {
                            try {
                                CardCalculateEvent?.Invoke(0, packet, resa);
                                Console.WriteLine("승인 완료");
                            }
                            catch (Exception ex) {
                                XLogClass?.SaveLogString("CDI", $"CardCalculateEvent 오류: {ex}");
                            }
                        }
                        else {
                            try {
                                CardCalculateEvent?.Invoke(1, packet, resa);
                                Console.WriteLine("승인 실패");
                            }
                            catch (Exception ex) {
                                XLogClass?.SaveLogString("CDI", $"CardCalculateEvent 오류: {ex}");
                            }
                          }
                        break;
                    case 'c':
                        SmartroApprovalResponse resc = SmartroPacketParser.ParseApproval(packet.Body);
                        if (resc.IsSuccess) {
                            try {
                                CardCancleEvent?.Invoke(0, packet, resc);
                                Console.WriteLine("취소 완료");
                            }
                            catch (Exception ex) {
                                XLogClass?.SaveLogString("CDI", $"CardCancleEvent 오류: {ex}");
                            }
                         }
                        else {
                            try {
                                CardCancleEvent?.Invoke(1, packet, resc);
                                Console.WriteLine("취소 실패");
                            }
                            catch (Exception ex) {
                                XLogClass?.SaveLogString("CDI", $"CardCancleEvent 오류: {ex}");
                            }
                        }
                        break;
                    case '@':
                        SmartroEventResponse ev = SmartroPacketParser.ParseEvent(packet.Body);
                        if (ev.IsFallback) {
                            Console.WriteLine("신용카드 삽입방향을 확인해 주세요");
                            BeginInvoke(() =>
                            {
                                try {
                                    CardCalculateEvent?.Invoke(5, packet, null);
                                }
                                catch (Exception ex) {
                                    XLogClass?.SaveLogString("CDI", $"CardCancleEvent 오류: {ex}");
                                }
                                
                            });
                            PlaySoundFile("credit05.mp3", AudioRouteState.Idle, 1);
                        }
                        else if (ev.IsIcInsert) {
                            Console.WriteLine("카드 삽입됨");
                            BeginInvoke(() =>
                            {
                                try {
                                    CardCalculateEvent?.Invoke(2, packet, null);
                                }
                                catch (Exception ex) {
                                    XLogClass?.SaveLogString("CDI", $"CardCancleEvent 오류: {ex}");
                                }
                            });
                            PlaySoundFile("cardinsert.mp3", AudioRouteState.Idle, 1);
                        }
                        break;
                    case 'f':
                        SmartroUidResponse uid = SmartroPacketParser.ParseUid(packet.Body);
                        XLogClass?.SaveLogString("CDI", "카드 UID 수신: " + uid.CardUid);
                        break;
                    case 'y':
                        BeginInvoke(() =>
                        {
                            try {
                                CardSetupEvent?.Invoke(0, packet);
                            }
                            catch (Exception ex) {
                                XLogClass?.SaveLogString("CDI", $"CardCancleEvent 오류: {ex}");
                            }
                        });
                        break;
                    default:
                        Console.WriteLine("알 수 없는 응답: " + (char)packet.JobCode);
                        break;
                }
            }
            catch (Exception ex) {
                XLogClass?.SaveLogString("CDI", $"SM 패킷 오류: {ex.Message}");
            }
        }

        public void PlayTextSpeech(string msg, AudioRouteState playType, int Priority = 0)
        {
            if (playType == AudioRouteState.Dual) {
                _sndQueue?.EnqueueText("mainform", msg, delayAfterMs: 500, priority: Priority);
            }
            else {
                if (playType == AudioRouteState.UsbActive) {
                    if (AudioDeviceRouter.CurrentState == AudioRouteState.UsbActive) {
                        _sndQueue?.EnqueueText("mainform", msg, delayAfterMs: 500, priority: Priority);
                    }
                }
                else {
                    _sndQueue?.EnqueueText("mainform", msg, delayAfterMs: 500, priority: Priority);
                }
            }

            // USB TEST
            //if (playType == AudioRouteState.UsbActive || playType == AudioRouteState.Dual) {
            //    _sndQueue?.EnqueueText("mainform", msg, delayAfterMs: 500, priority: Priority);
            //}
        }

        public void PlaySsmlSpeech(string msg, AudioRouteState playType, int Priority = 0)
        {
            if (playType == AudioRouteState.UsbActive) {
                if (AudioDeviceRouter.CurrentState == AudioRouteState.UsbActive) {
                    _sndQueue?.EnqueueText("mainform", msg, delayAfterMs: 500, priority: Priority);
                }
            }
            else {
                _sndQueue?.EnqueueText("mainform", msg, delayAfterMs: 500, priority: Priority);
            }
        }


        public void PlaySoundFile(string mpname, AudioRouteState state, int Priority = 0)
        {
            if (state == AudioRouteState.Dual) {
                _sndQueue?.EnqueueFile("mainform", mpname, priority: Priority);
            }
            else {
                if (state == AudioRouteState.UsbActive) {
                    if (AudioDeviceRouter.CurrentState == AudioRouteState.UsbActive) {
                        _sndQueue?.EnqueueFile("mainform", mpname, priority: Priority);
                    }
                }
                else {
                    _sndQueue?.EnqueueFile("mainform", mpname, priority: Priority);
                }
            }

            // USB TEST
            //if (state == AudioRouteState.UsbActive || state == AudioRouteState.Dual) {
            //    _sndQueue?.EnqueueFile("mainform", mpname, priority: Priority);
            //}
        }

        private void MediaPlayer_PlaybackCompleted(object? sender, EventArgs e)
        {
            //            Console.WriteLine("MediaPlayer_PlaybackCompleted");
            if (UsbPlayOn) {    // 이어폰 ON
                if (MediaPlayer._playMode == PlayMode.SND_USBSTART) {
                    MediaPlayer._playMode = PlayMode.SND_NORMAL;
                    PlaySoundFile("Main00.mp3", AudioRouteState.UsbActive);
                }
            }
            else {
                if (MediaPlayer._playMode == PlayMode.SND_NORMAL || MediaPlayer._playMode == PlayMode.SND_EXPLAIN) {
                    if (APSConfig.activeForm == APSConfig.menuForm) {
                        MediaPlayer._playMode = PlayMode.SND_NORMAL;
                        voiceRepeatTimerChange(APSConfig.SoundRepeatTime, Timeout.Infinite);
                    }
                }
            }
        }

        public async Task<bool> InitConfigAsync()
        {
            try {
                var req = new EnvConfigRequest
                {
                    Sitenum = APSConfig.Sitenum,
                    Groupnum = APSConfig.Groupnum
                };
                Console.WriteLine("InitConfigAsync");
                string? json = await RestHelper.Instance.PostAsync("/api/env/all", req);
                //Console.WriteLine(json);
                if (string.IsNullOrWhiteSpace(json))
                    return false;

                EnvConfigResponse? envData = Newtonsoft.Json.JsonConvert.DeserializeObject<EnvConfigResponse>(json);
                if (envData == null)
                    return false;

                APSConfig.Holidays = envData.Tholiday ?? new List<Tholiday>();
                APSConfig.FeeRules = envData.Tparkfee ?? new List<Tparkfee>();
                APSConfig.Discounts = (envData.Tdiscount ?? new List<Tdiscounttable>())
                                      .Where(t => t.Salecode >= (APSConfig.Groupnum * 100 + 1) &&
                                                  t.Salecode <= (APSConfig.Groupnum * 100 + 99)).ToList();
                APSConfig.VariableOptions = envData.Tparkvariable ?? new List<Tparkvariable>();

                foreach (var item in APSConfig.FeeRules) {
                    Console.WriteLine($"{item.Cartype} {item.Dayshift,-3} => S:{item.Feestep,-3}  P:{item.Parktime,-8} F:{item.Parkfee,-12}");
                }
                foreach (var item in APSConfig.Discounts) {
                    item.Salecode = item.Salecode % 100;

                    Console.WriteLine($"{item.Salecode:D2}-[{item.Saletype:D2}] => {item.Saletitle}");
                }
                ApplyVariableOptions();
                ApplyDayTimeRanges();

                return true;
            }
            catch (Exception ex) {
                Console.WriteLine("Error loading config from API: " + ex.Message);
                return false;
            }
        }

        public void ApplyVariableOptions()
        {
            Console.WriteLine("===============  tparkvariable  ===============");

            foreach (var item in APSConfig.VariableOptions) {
                Console.WriteLine($"{item.Cmd_Type,-20} = {item.Val,-20} - {item.Opt,-20} : {item.Msg}");

                switch (item.Cmd_Type) {
                    case "CMD_MAXDAILYFEE":
                        if (int.TryParse(item.Val, out int maxFee))
                            APSConfig.MaxDailyFee = maxFee;
                        break;

                    case "CMD_HOLIDAYUSE":
                        APSConfig.ExcludeHoliday = item.Opt == "1" ? 1 : 0;
                        break;

                    case "CMD_WEEKENDUSE":
                        APSConfig.ExcludeWeekend = item.Opt == "1" ? 1 : 0;
                        break;

                    case "CMD_GRACE_TIME":
                        APSConfig.GraceTime = item.Opt == "1" ? int.Parse(item.Val ?? "0") : 0;
                        break;

                    case "CMD_PREPAY_GRACE":
                        APSConfig.PrepayGraceTime = item.Opt == "1" ? int.Parse(item.Val ?? "0") : 0;
                        break;

                    case "CMD_SERVICE_TIME":
                        APSConfig.ServiceTime = item.Opt == "1" ? int.Parse(item.Val ?? "0") : 0;
                        break;

                    case "CMD_IRESTRICTION":
                        APSConfig.IRestriction = item.Opt == "1" ? int.Parse(item.Val ?? "0") : 0;
                        break;

                    case "CMD_JRESTRICTION":
                        APSConfig.JRestriction = item.Opt == "1" ? int.Parse(item.Val ?? "0") : 0;
                        break;
                }
            }

            Console.WriteLine("==============================================");
        }

        public static void ApplyDayTimeRanges()
        {
            DayOfWeek day;

            foreach (Tparkvariable? v in APSConfig.VariableOptions) {
                switch (v.Cmd_Type) {
                    case "CMD_SUNDAY":
                        day = DayOfWeek.Sunday;
                        break;
                    case "CMD_MONDAY":
                        day = DayOfWeek.Monday;
                        break;
                    case "CMD_TUESDAY":
                        day = DayOfWeek.Tuesday;
                        break;
                    case "CMD_WEDNESDAY":
                        day = DayOfWeek.Wednesday;
                        break;
                    case "CMD_THURSDAY":
                        day = DayOfWeek.Thursday;
                        break;
                    case "CMD_FRIDAY":
                        day = DayOfWeek.Friday;
                        break;
                    case "CMD_SATURDAY":
                        day = DayOfWeek.Saturday;
                        break;
                    default:
                        continue;
                }

                if (string.IsNullOrEmpty(v.Val))
                    continue;

                var sp = v.Val.Split('-');
                if (sp.Length != 2)
                    continue;

                TimeSpan start = TimeSpan.Parse(sp[0]);
                TimeSpan end;

                if (sp[1] == "24:00")
                    end = TimeSpan.FromHours(24);
                else
                    end = TimeSpan.Parse(sp[1]);

                APSConfig.DayTimeRanges[day] = (start, end);
            }

            foreach (var kv in APSConfig.DayTimeRanges) {
                Console.WriteLine($"{kv.Key,-12}: {kv.Value.Start:hh\\:mm} ~ {(kv.Value.End == TimeSpan.FromHours(24) ? "24:00" : kv.Value.End.ToString(@"hh\:mm"))}");
            }
        }

        private static int GetEndTabIndex(Control root)
        {
            int max = -1;
            void Walk(Control p)
            {
                foreach (Control c in p.Controls) {
                    if (c is Button b && b.Visible && b.Enabled && b.TabStop)
                        max = Math.Max(max, b.TabIndex);
                    if (c.HasChildren && c is not Form)
                        Walk(c);
                }
            }
            Walk(root);
            return (max < 0) ? 0 : max;
        }

        private void OnRemoteReceipt(int receiptIndex)
        {
            throw new NotImplementedException();
        }

        private void OnRemoteCdCancel(int cancelIndex)
        {
            throw new NotImplementedException();
        }

        public void RemoteGateControl(ref STUDPDATA udpData)
        {
            int relayBit;
            unsafe {
                fixed (byte* p = udpData.xdata) {
                    if (p[3] == 'R' && p[4] == 'L') {
                        relayBit = p[5] - 0x31;
                        if (relayBit > 0 && relayBit < 5) {
                            if (p[6] == '9') {
                                if (_displays != null && _displays._displays[relayBit] != null) {
                                    _displays?.GateCommand(relayBit, GateCmd.DETECTORRESET);
                                    Thread.Sleep(100);
                                }
                            }
                            else if (p[6] <= '3') {
                                GateCmd gateCmd = (GateCmd)(p[6] - 0x30);
                                if (_displays != null && _displays._displays[relayBit] != null) {
                                    _displays?.GateCommand(relayBit, gateCmd);
                                }
                            }
                        }
                    }

                }
            }
        }

        public async Task ReadDiscountTable()
        {
            try {
                EnvItemRequest request = new EnvItemRequest
                {
                    Sitenum = (short)APSConfig.Sitenum,
                    Groupnum = (short)APSConfig.Groupnum,
                    CmdType = "discount"
                };
                var content = await RestHelper.Instance.PostAsync<EnvItemRequest>("/api/env/item", request);
                if (content != null) {
                    var response = JsonConvert.DeserializeObject<EnvItemResponse>(content);

                    int startCode = request.Groupnum * 100;
                    int endCode = startCode + 99;

                    var discounts = (response?.Tdiscount ?? new List<Tdiscounttable>())
                        .Where(t => t.Salecode >= startCode + 1 && t.Salecode <= endCode)
                        .ToList();

                    foreach (var item in discounts) {
                        item.Salecode = item.Salecode % 100;
                    }

                    APSConfig.Discounts.Clear();
                    APSConfig.Discounts.AddRange(discounts);

                    foreach (var item in APSConfig.Discounts) {
                        Console.WriteLine($"{item.Salecode:D2}-[{item.Saletype:D2}] => {item.Saletitle}");
                    }
                }
            }
            catch (Exception ex) {
                XLogClass?.SaveLogString("CAL", $"ReadDiscountTable Error : {ex.Message}");
            }
        }

        private void ReconnectUdpSocket()
        {
            if (_reconnecting)
                return;

            _reconnecting = true;

            if (APSConfig.HOSTUSE == true) {
                Task.Run(async () =>
                {
                    await Task.Delay(2500);

                    try {
                        if (_udpSocket != null) {
                            _udpSocket.OnConnected -= _udpSocket_OnConnected;
                            _udpSocket.OnReceived -= _udpSocket_OnReceived;
                            _udpSocket.OnDisconnected -= _udpSocket_OnDisconnected;
                            _udpSocket.OnError -= _udpSocket_OnError;
                            _udpSocket.Close(0);
                        }

                        _udpSocket = new UDPSocket();
                        _udpSocket.OnConnected += _udpSocket_OnConnected;
                        _udpSocket.OnReceived += _udpSocket_OnReceived;
                        _udpSocket.OnDisconnected += _udpSocket_OnDisconnected;
                        _udpSocket.OnError += _udpSocket_OnError;
                        _udpSocket.Connect(APSConfig.HOSTIP!, APSConfig.HOSTPORT);
                    }
                    finally {
                        _reconnecting = false;
                    }
                });
            }
        }

        private void _udpSocket_OnReceived(UDPSocket sock, STUDPPACKET rxPacket)
        {
            int key;

            STUDPDATA udpData = StructHelper.FromBytes<STUDPDATA>(
                                StructHelper.FieldSpan(ref rxPacket, nameof(STUDPPACKET.xPacket), 1272)
                                            .Slice(0, StructHelper.GetSize<STUDPDATA>()));

            string srcIp = StructHelper.ReadString(ref udpData, nameof(STUDPDATA.srcip), 20, Encoding.ASCII);
            string desIp = StructHelper.ReadString(ref udpData, nameof(STUDPDATA.desip), 20, Encoding.ASCII);
            string xData = StructHelper.ReadString(ref udpData, nameof(STUDPDATA.xdata), 1024, Encoding.GetEncoding("ks_c_5601-1987"));

            if (rxPacket.wtCmd <= (ushort)req_cmd_code.APS_CMD_MAX) {
                Console.WriteLine("[srcip {0,-16} desip {1,-16} ({2:000})] => wtCmd[{3}]:{4,-20} nSize:{5}",
                    srcIp,
                    desIp,
                    udpData.devicenum,
                    rxPacket.wtCmd,
                    (req_cmd_code)rxPacket.wtCmd,
                    rxPacket.nSize);
            }

            Console.WriteLine("UDATA : " + xData);

            switch ((req_cmd_code)rxPacket.wtCmd) {
                case req_cmd_code.APS_CMD_CONFIG:
                    break;
                case req_cmd_code.APS_CMD_LOGON:
                    break;
                case req_cmd_code.APS_CMD_WEBREQ:
                    if (_calForm != null && !_calForm.IsDisposed) {
                        BeginInvoke(new Action(() =>
                        {
                            if (_calForm == null || _calForm.IsDisposed)
                                return;

                            StructHelper.CopyTparkinfoToStruct(_xparkinfo!, ref stparkinfo);
                            XSendPacketData<STTPARKINFO>(req_cmd_code.APS_CMD_WEBCARNUM, stparkinfo, "0.0.0.0");
                        }));
                    }
                    break;
                case req_cmd_code.APS_CMD_WEBSALEKEY:
                    if (_calForm != null && !_calForm.IsDisposed) {
                        Console.WriteLine("WEBMSG => " + xData);

                        string sendBuf = xData.Length >= 3 ? xData.Substring(0, 3) : xData;
                        string logdata = $"원격할인 : {sendBuf}";
                        XLogClass?.SaveLogString("CAL", logdata);

                        key = int.TryParse(sendBuf, out int tmpKey) ? tmpKey : 0;
                        if (key > 0 && key < 100) {
                            BeginInvoke(new Action(() =>
                            {
                                if (_calForm == null || _calForm.IsDisposed)
                                    return;

                                _calForm.SaleKeyParkFuction(key, 2, 1);
                                StructHelper.CopyTparkinfoToStruct(_xparkinfo!, ref stparkinfo);
                                XSendPacketData<STTPARKINFO>(req_cmd_code.APS_CMD_WEBCARNUM, stparkinfo, "0.0.0.0");
                            }));
                        }
                    }
                    break;
                case req_cmd_code.APS_CMD_WEBCANCEL:
                    if (_calForm != null && !_calForm.IsDisposed) {
                        BeginInvoke(new Action(() =>
                        {
                            if (_calForm == null || _calForm.IsDisposed)
                                return;

                            _calForm.RemoveDisKey(2);
                            StructHelper.CopyTparkinfoToStruct(_xparkinfo!, ref stparkinfo);
                            XSendPacketData<STTPARKINFO>(req_cmd_code.APS_CMD_WEBCARNUM, stparkinfo, "0.0.0.0");
                        }));
                    }
                    break;
                case req_cmd_code.APS_CMD_SALECARDISSUE:
                    _ = ReadDiscountTable();
                    break;
                case req_cmd_code.APS_CMD_PARKNUM:
                    break;
                case req_cmd_code.APS_CMD_GATEINFO:
                    BeginInvoke(new Action(() =>
                    {
                        RemoteGateControl(ref udpData);
                    }));
                    break;
                case req_cmd_code.APS_CMD_RERECEIPT: {
                        int receiptIndex = int.TryParse(xData, out int tmpIndex) ? tmpIndex : 0;
                        if (receiptIndex > 0) {
                            BeginInvoke(new Action(() =>
                            {
                                OnRemoteReceipt(receiptIndex);
                            }));
                        }
                        break;
                    }
                case req_cmd_code.APS_CMD_CDCANCEL: {
                        int cancelIndex = int.TryParse(xData, out int tmpIndex) ? tmpIndex : 0;
                        if (cancelIndex > 0) {
                            BeginInvoke(new Action(() =>
                            {
                                OnRemoteCdCancel(cancelIndex);
                            }));
                        }
                        break;
                    }
                default:
                    break;
            }
        }

        private void _udpSocket_OnDisconnected(UDPSocket sock, int arg)
        {
            Console.WriteLine("UDP Socket Disconnected {0}", arg);

            ReconnectUdpSocket();
        }

        private void _udpSocket_OnError(UDPSocket socket, string arg)
        {
            Console.WriteLine("UDP Socket Disconnected {0}", arg);

            ReconnectUdpSocket();
        }

        private void _udpSocket_OnConnected(UDPSocket sock, int arg)
        {
            STDEVINFO dev = default;

            sock.StartReceive();

            dev.devtype = (int)device_type.DEVICE_TYPE_APS;
            dev.devnum = (int)APSConfig.APSNUM;
            StructHelper.WriteString<STDEVINFO>(ref dev, nameof(STDEVINFO.devname), 40, APSConfig.APSNAME);

            sock.XSendPacketData<STDEVINFO>(req_cmd_code.APS_CMD_DEVICETYPE, dev, "0.0.0.0");
        }

        public void XSendPacketData<T>(req_cmd_code PacketComm, in T xdata, string desip) where T : unmanaged
        {
            _udpSocket?.XSendPacketData<T>(PacketComm, xdata, "0.0.0.0");
        }

        private void OnMainPacketReceived(byte state, byte[] Message)
        {
            if (Message != null) {
                string msg = Encoding.ASCII.GetString(Message).TrimEnd('\0');
                Console.WriteLine($"{msg}");
                if (msg.Equals("PT01")) {

                    PlaySoundFile("comfirm.mp3", AudioRouteState.Dual, 1);
                    PlaySoundFile("maincall.mp3", AudioRouteState.Dual, 0);
                    if (APSConfig.activeForm != null) {
                        Form? subForm = APSConfig.activeForm as Form;
                        if (subForm != null) {
                            UiHelpers.ShowMessage(subForm, 12, true);
                        }
                        else {
                            UiHelpers.ShowMessage(this, 12, true);
                        }
                    }
                    else {
                        UiHelpers.ShowMessage(this, 12, true);
                    }
                }
            }
        }

        public void MakePacketData(byte CMDMSG, SMPAYDATA payData, SMDEVICEINFO? infoset=null)
        {
            if (_creditSmatro == null)
                return;

            byte[] sendData = SmartroSendHelper.MakePacket(CMDMSG, payData, infoset);

            if (sendData.Length <= 0)
                return;

            _creditSmatro.ResetRxState();
            _creditSmatro.WriteByte(sendData, sendData.Length);

            Task.Delay(100).Wait();
        }

        public void AutoSoundPlay()
        {
            MediaPlayer._playMode = PlayMode.SND_NORMAL;
            //Console.WriteLine("AutoSoundPlay");
            voiceRepeatTimerChange(APSConfig.SoundRepeatTime, Timeout.Infinite);

            MainEndIndex = GetEndTabIndex(this);
        }

        public void ResetEndTabIndex()
        {
            MainEndIndex = GetEndTabIndex(this);
        }

        public void MainFormRecover()
        {
            if (APSConfig.menuForm15 != null) {
                APSConfig.menuForm15.MenuFormRecover();
            }
        }

        public void ParentFormRecover(bool zoomMode)
        {

        }

        public void UDPSendPacket(req_cmd_code PacketComm, byte[] MSG, int nLength, string desip)
        {
            STUDPDATA UPDData = default;

            UPDData.devicenum = (ushort)APSConfig.APSNUM;

            StructHelper.WriteString<STUDPDATA>(ref UPDData, nameof(STUDPDATA.srcip), 20, APSConfig.APSIP);
            StructHelper.WriteString<STUDPDATA>(ref UPDData, nameof(STUDPDATA.desip), 20, desip);

            StructHelper.WriteBytes(ref UPDData, nameof(STUDPDATA.xdata), 1024, MSG.AsSpan(0, Math.Min(nLength, 1024)));

            SendPacketData(PacketComm, UPDData, Unsafe.SizeOf<STUDPDATA>());
        }

        public void UDPXSendPacket<T>(req_cmd_code PacketComm, in T xdata, string desip) where T : unmanaged
        {
            STUDPPACKET packet = default;
            STUDPDATA udata;
            int dataSize = 0;

            udata.devicenum = (ushort)APSConfig.APSNUM;

            StructHelper.WriteString<STUDPDATA>(ref udata, nameof(STUDPDATA.srcip), 20, APSConfig.APSIP);
            StructHelper.WriteString<STUDPDATA>(ref udata, nameof(STUDPDATA.desip), 20, desip);

            dataSize = Unsafe.SizeOf<T>();
            StructHelper.WriteUnmanaged(ref udata, nameof(STUDPDATA.xdata), dataSize, in xdata);

            dataSize = Unsafe.SizeOf<STUDPDATA>();
            packet.btID = 0XC0C0;
            packet.wtCmd = (ushort)PacketComm;
            packet.wClientID = 0x9999;
            packet.nSize = (ushort)dataSize;

            var dbgIp = StructHelper.FieldSpan<STUDPDATA>(ref udata, nameof(STUDPDATA.xdata), 1024).ToArray();

            StructHelper.WriteUnmanaged(ref packet, nameof(STUDPPACKET.xPacket), dataSize, in udata);

            byte[] sendData = StructHelper.StructToBytes<STUDPPACKET>(packet);
        }

        public void SendPacketData(req_cmd_code PacketComm, STUDPDATA PacketData, int Size)
        {
            STUDPPACKET packet = default;

            packet.btID = 0XC0C0;
            packet.wtCmd = (ushort)PacketComm;
            packet.wClientID = 0x9999;
            packet.nSize = (ushort)Size;

            int dataSize = Unsafe.SizeOf<STUDPDATA>();

            StructHelper.WriteUnmanaged(ref packet, nameof(STUDPPACKET.xPacket), dataSize, in PacketData);

            byte[] sendData = StructHelper.StructToBytes<STUDPPACKET>(packet);
        }

        public void InvokeUI(Action action)
        {
            if (IsDisposed)
                return;

            if (InvokeRequired)
                Invoke((Action)action);
            else
                action();
        }

        public void Accept()
        {
            STTACCEPT accept = default;

            accept.sitenum = (byte)APSConfig.Sitenum;
            accept.groupnum = (byte)APSConfig.Groupnum;
            accept.devicenum = (ushort)APSConfig.APSNUM;
            accept.devicetype = 2;
            accept.managercode = 0;
            StructHelper.WriteString<STTACCEPT>(ref accept, nameof(STTACCEPT.accepttime), 20, DateTime.Now.ToString("yyyyMMddHHmmss"));
            StructHelper.WriteString<STTACCEPT>(ref accept, nameof(STTACCEPT.ip), 20, "192.168.0.12");

            UDPXSendPacket<STTACCEPT>((ushort)req_cmd_code.APS_CMD_ACCEPT, in accept, "0.0.0.0");

        }

        public void lelMessageChange(int xindex)
        {
        }

        public static int GetMemberOverMinute(DateTime inDateTime, DateTime outDateTime,
                                                DateTime validStartDateTime, DateTime validEndDateTime,
                                                int startTime, int endTime)
        {
            if (outDateTime <= inDateTime)
                return 0;

            int overMinute = 0;

            // 1. 기간 시작 전
            if (inDateTime < validStartDateTime) {
                DateTime preEnd = outDateTime < validStartDateTime ? outDateTime : validStartDateTime;
                if (preEnd > inDateTime)
                    overMinute += (int)(preEnd - inDateTime).TotalMinutes;
            }

            // 2. 기간 종료 후
            if (outDateTime > validEndDateTime) {
                DateTime postStart = inDateTime > validEndDateTime ? inDateTime : validEndDateTime;
                if (outDateTime > postStart)
                    overMinute += (int)(outDateTime - postStart).TotalMinutes;
            }

            // 3. 기간 내
            DateTime innerStart = inDateTime > validStartDateTime ? inDateTime : validStartDateTime;
            DateTime innerEnd = outDateTime < validEndDateTime ? outDateTime : validEndDateTime;

            if (innerEnd > innerStart)
                overMinute += GetTimeRuleOverMinute(innerStart, innerEnd, startTime, endTime);

            return overMinute;
        }

        private static int GetTimeRuleOverMinute(DateTime inDateTime, DateTime outDateTime, int startTime, int endTime)
        {
            if (outDateTime <= inDateTime)
                return 0;

            if (startTime < 0 || startTime > 1440 || endTime < 0 || endTime > 1440)
                throw new ArgumentOutOfRangeException(nameof(startTime), "startTime/endTime must be 0~1440.");

            // 전일 허용
            if (startTime == endTime)
                return 0;

            long totalMinute = 0;
            DateTime dayCursor = inDateTime.Date;
            DateTime lastDay = outDateTime.Date;

            while (dayCursor <= lastDay) {
                DateTime dayStart = dayCursor;
                DateTime dayEnd = dayCursor.AddDays(1);

                DateTime sectionStart = inDateTime > dayStart ? inDateTime : dayStart;
                DateTime sectionEnd = outDateTime < dayEnd ? outDateTime : dayEnd;

                if (sectionEnd > sectionStart) {
                    long dayTotalMinute = (long)(sectionEnd - sectionStart).TotalMinutes;
                    long allowMinute = GetAllowedMinuteInDay(sectionStart, sectionEnd, startTime, endTime);
                    long overMinute = dayTotalMinute - allowMinute;

                    if (overMinute > 0)
                        totalMinute += overMinute;
                }

                dayCursor = dayCursor.AddDays(1);
            }

            return (int)totalMinute;
        }

        private static long GetAllowedMinuteInDay(DateTime sectionStart, DateTime sectionEnd, int startTime, int endTime)
        {
            DateTime day = sectionStart.Date;

            if (startTime < endTime) {
                DateTime allowStart = day.AddMinutes(startTime);
                DateTime allowEnd = day.AddMinutes(endTime);
                return GetOverlapMinute(sectionStart, sectionEnd, allowStart, allowEnd);
            }
            else {
                // 예: 18:00 ~ 06:30
                DateTime allowStart1 = day;
                DateTime allowEnd1 = day.AddMinutes(endTime);

                DateTime allowStart2 = day.AddMinutes(startTime);
                DateTime allowEnd2 = day.AddDays(1);

                return GetOverlapMinute(sectionStart, sectionEnd, allowStart1, allowEnd1)
                     + GetOverlapMinute(sectionStart, sectionEnd, allowStart2, allowEnd2);
            }
        }

        private static long GetOverlapMinute(DateTime aStart, DateTime aEnd, DateTime bStart, DateTime bEnd)
        {
            DateTime start = aStart > bStart ? aStart : bStart;
            DateTime end = aEnd < bEnd ? aEnd : bEnd;

            if (end <= start)
                return 0;

            return (long)(end - start).TotalMinutes;
        }

        public void ReceiptPrintStart()
        {
            if (APSConfig.PrintPaper == 60) { ReceiptPrintStart60(); }
            else { ReceiptPrintStart80(); }
        }

        public void ReceiptPrintStart80()
        {
            if (_xparkinfo == null)
                return;

            if (string.IsNullOrEmpty(_xparkinfo.Carnum) ||
                 string.IsNullOrEmpty(_xparkinfo.Acceptno) ||
                 string.IsNullOrEmpty(_xparkinfo.Creditcardno))
                return;

            PrinterManager.PrintBoldCenter("** 영 수 증 **", 20);
            PrinterManager.LineFeed(2);
            PrinterManager.PrintLine($" 가맹점 명칭 : {APSConfig.ParkingName}");
            PrinterManager.PrintLine($" 가맹점 주소 : {APSConfig.ParkingAddress}");
            PrinterManager.PrintLine($" 전 화 번 호 : {APSConfig.ParkingPhone}");
            PrinterManager.PrintLine($" 사업자 번호 : {APSConfig.BusinessNumber}");
            PrinterManager.PrintLine($" 대  표  자  : {APSConfig.OwnerName}");
            PrinterManager.PrintLine($" 발 행 일 자 : {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
            PrinterManager.PrintLine($" 영수증 번호 : {APSConfig.APSNUM:D3}-{APSConfig.ReceiptCount++:d5}");
            PrinterManager.LineFeed(1);
            PrinterManager.PrintLine($"----------------------------------------------");
            PrinterManager.PrintLine($" 차 량 번 호 : {_xparkinfo!.Carnum}");
            PrinterManager.PrintLine($" 품       명 : 주차요금");
            PrinterManager.PrintLine($" 지 불 방 법 : 신용카드");
            PrinterManager.PrintLine($" 카 드 번 호 : {_xparkinfo!.Creditcardno}");
            //PrinterManager.PrintLine($" 회 원 번 호 : 등록차량");
            PrinterManager.PrintLine($" 승 인 번 호 : {_xparkinfo!.Acceptno}");
            PrinterManager.PrintLine($" 카 드 사 명 : {_xcdinfo!.CardName}");
            PrinterManager.PrintLine($"----------------------------------------------");
            PrinterManager.LineFeed(1);
            PrinterManager.PrintLine($" 입 차 일 시 : {_xparkinfo!.Indate.ToString("yyyy-MM-dd HH:MM:ss")}");
            PrinterManager.PrintLine($" 출 차 일 시 : {_xparkinfo!.Outdate.ToString("yyyy-MM-dd HH:MM:ss")}");
            PrinterManager.PrintLine($" 주 차 시 간 : {_xparkinfo!.Parktime} 분");
            PrinterManager.PrintLine($" 총주차 금액 : {(_xparkinfo!.Parkmoney + _xparkinfo!.Salemoney):N0} 원");

            DisInfo? dis = ParkCache.DisKeys.FirstOrDefault();
            if (dis != null) {
                PrinterManager.PrintLine($" 할 인 내 역 : {dis.title}");
            }
            else {
                PrinterManager.PrintLine($" 할 인 내 역 : 일반");
            }

            PrinterManager.PrintLine($" 할 인 금 액 : {_xparkinfo!.Salemoney:N0} 원");
            PrinterManager.PrintLine($" 정 산 요 금 : {_xparkinfo!.Parkmoney:N0} 원");
            PrinterManager.LineFeed(2);
            PrinterManager.PrintCentered($"* 이용해 주셔서 감사합니다 *", 40);
            PrinterManager.LineFeed(3);
            PrinterManager.Cut(APSConfig.cutmode);
            PrinterManager.CheckStatus();

            if (APSConfig.ReceiptCount > 99999) { APSConfig.ReceiptCount = 1; }
        }

        public void ReceiptPrintStart60()
        {
            if (_xparkinfo == null)
                return;

            if (string.IsNullOrEmpty(_xparkinfo.Carnum) ||
                 string.IsNullOrEmpty(_xparkinfo.Acceptno) ||
                 string.IsNullOrEmpty(_xparkinfo.Creditcardno))
                return;

            PrinterManager.PrintNormal();
            PrinterManager.PrintCentered("** 영 수 증 **", 30);
            PrinterManager.LineFeed(2);
            PrinterManager.PrintLine($"가맹점명칭 : {APSConfig.ParkingName}");
            PrinterManager.PrintLine($"가맹점주소 : {APSConfig.ParkingAddress}");
            PrinterManager.PrintLine($"전화번호   : {APSConfig.ParkingPhone}");
            PrinterManager.PrintLine($"사업자번호 : {APSConfig.BusinessNumber}");
            PrinterManager.PrintLine($"대표자     : {APSConfig.OwnerName}");
            PrinterManager.PrintLine($"발행일자   : {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
            PrinterManager.PrintLine($"영수증번호 : {APSConfig.APSNUM:D3}-{APSConfig.ReceiptCount++:d5}");
            PrinterManager.LineFeed(1);
            PrinterManager.PrintLine($"---------------------------------");
            _xparkinfo!.Carnum = _xparkinfo!.Carnum ?? "테트스";
            PrinterManager.PrintLine($"차량번호   : {_xparkinfo!.Carnum}");
            PrinterManager.PrintLine($"품  명     : 주차요금");
            PrinterManager.PrintLine($"지불방법   : 신용카드");

            _xparkinfo!.Creditcardno = _xparkinfo!.Creditcardno ?? "1234-5678-****-****";
            PrinterManager.PrintLine($"카드번호   :  {_xparkinfo!.Creditcardno}");

            _xparkinfo!.Acceptno = _xparkinfo!.Acceptno ?? "12345678";
            PrinterManager.PrintLine($"승인번호   : {_xparkinfo!.Acceptno}");

            if (_xcdinfo != null) {
                _xcdinfo!.CardName = _xcdinfo!.CardName ?? "테스트";
                PrinterManager.PrintLine($"카드사명   : {_xcdinfo!.CardName}");
            }
            else {
                PrinterManager.PrintLine($"카드사명   : 없음");
            }
            PrinterManager.PrintLine($"---------------------------------");
            PrinterManager.LineFeed(1);

            PrinterManager.PrintLine($"입차일시   : {_xparkinfo!.Indate.ToString("yyyy-MM-dd HH:MM:ss")}");
            PrinterManager.PrintLine($"출차일시   : {_xparkinfo!.Outdate.ToString("yyyy-MM-dd HH:MM:ss")}");
            PrinterManager.PrintLine($"주차시간   : {_xparkinfo!.Parktime} 분");
            PrinterManager.PrintLine($"총주차금액 : {(_xparkinfo!.Parkmoney + _xparkinfo!.Salemoney):N0} 원");

            DisInfo? dis = ParkCache.DisKeys.FirstOrDefault();
            if (dis != null) {
                PrinterManager.PrintLine($"할인내역   : {dis.title}");
            }
            else {
                PrinterManager.PrintLine($"할인내역   : 일반");
            }
            PrinterManager.PrintLine($"할인금액   : {_xparkinfo!.Salemoney:N0} 원");
            PrinterManager.PrintLine($"정산요금   : {_xparkinfo!.Parkmoney:N0} 원");
            PrinterManager.LineFeed(2);
            PrinterManager.PrintCentered($"* 이용해 주셔서 감사합니다 *", 30);
            PrinterManager.LineFeed(3);
            PrinterManager.Cut(APSConfig.cutmode);
            PrinterManager.CheckStatus();

            if (APSConfig.ReceiptCount > 99999) { APSConfig.ReceiptCount = 1; }
        }

        private async Task StartConfigLoadAsync()
        {
            _configRetryTimer ??= new System.Windows.Forms.Timer();

            _configRetryTimer.Interval = 10000;
            _configRetryTimer!.Tick -= ConfigRetryTimer_Tick;
            _configRetryTimer!.Tick += ConfigRetryTimer_Tick;

            bool ret = await InitConfigAsync();

            if (ret) {
                SetConfigReady(true);
                return;
            }

            SetConfigReady(false);
            _configRetryTimer.Start();
        }

        private void StopConfigRetryTimer()
        {
            if (_configRetryTimer!.Enabled)
                _configRetryTimer.Stop();
        }

        private async void ConfigRetryTimer_Tick(object? sender, EventArgs e)
        {
            _configRetryTimer!.Stop();

            bool ret = await InitConfigAsync();

            if (ret) {
                SetConfigReady(true);
                return;
            }

            _configRetryTimer.Start();
        }

        private void SetConfigReady(bool ready)
        {
            if (ready)
                StopConfigRetryTimer();

            if (APSConfig.menuForm15 != null) {
                if (APSConfig.menuForm15.InvokeRequired) {
                    APSConfig.menuForm15.Invoke(new Action(() =>
                    {
                        APSConfig.menuForm15.Enabled = ready;
                    }));
                }
                else {
                    APSConfig.menuForm15.Enabled = ready;
                }
            }
            BlockMenu(ready);
        }

        public void BlockMenu(bool ready)
        {
            if ( !ready ) {
                lblBlock.AutoSize = false;
                lblBlock.Parent = this;
                lblBlock.Left = 0;
                lblBlock.Top = 0;
                lblBlock.Width = this.ClientSize.Width;
                lblBlock.Height = this.ClientSize.Height;

                lblBlock.TextAlign = ContentAlignment.MiddleCenter;
                lblBlock.Text = "환경설정 수신 실패" + Environment.NewLine + "대기중...";

                lblBlock.Visible = true;
                lblBlock.BringToFront();
            }
            else {
                lblBlock.Left = 200;
                lblBlock.Top = 10;
                lblBlock.Width = 10;
                lblBlock.Height = 10;
                lblBlock.Visible = false;
            }
        }

        private DateTime _downTime;

        private void lblBlock_MouseDown(object sender, MouseEventArgs e)
        {
            Point pt = this.PointToClient(Cursor.Position);

            if (pt.X > 200 || pt.Y > 200)
                return;

            _downTime = DateTime.Now;
        }

        private void lblBlock_MouseUp(object sender, MouseEventArgs e)
        {
            Point pt = this.PointToClient(Cursor.Position);

            if (pt.X > 200 || pt.Y > 200)
                return;

            if ((DateTime.Now - _downTime).TotalSeconds >= 3) {
                SetConfigReady(true);
            }
        }

        public void SmatroWaiting()
        {
            _smPayData.Init();
            MakePacketData(Constants.CMD_TX_WAITNG, _smPayData);
            Task.Delay(100).Wait();
        }

        private async void LocalReconnect()
        {
            if (_isLprReconnect)
                return;

            _isLprReconnect = true;

            await Task.Delay(2500);

            if (XLprSocket != null) {
                XLprSocket.OnLocalConnectEvent -= OnLocalConnectEvent;
                XLprSocket.OnLocalDisconnectedEvent -= OnLocalDisconnectedEvent;
                XLprSocket.OnLocalNotConnectEvent -= OnLocalNotConnectEvent;
                XLprSocket.OnLocalReceiveEvent -= OnLocalReceiveEvent;

                XLprSocket.Dispose();
                XLprSocket = null;
            }

            if (!string.IsNullOrEmpty(APSConfig.LprXIp)) {
                XLprSocket = new XLocalSocket(APSConfig.LprXIp, APSConfig.LprXPort);

                XLprSocket.OnLocalConnectEvent += OnLocalConnectEvent;
                XLprSocket.OnLocalDisconnectedEvent += OnLocalDisconnectedEvent;
                XLprSocket.OnLocalNotConnectEvent += OnLocalNotConnectEvent;
                XLprSocket.OnLocalReceiveEvent += OnLocalReceiveEvent;

                XLprSocket.SocketConnect(APSConfig.LprXIp, APSConfig.LprXPort);
            }
            _isLprReconnect = false;
        }

        private void OnLocalConnectEvent(object? sender, LocalArgs e)
        {
            XLocalSocket? sock = sender as XLocalSocket;
            if (sock != null) {
                Console.WriteLine($"{sock._myIP} Connect Success");
            }
        }

        private void OnLocalNotConnectEvent(object? sender, LocalArgs e)
        {
            XLocalSocket? sock = sender as XLocalSocket;

            if (sock != null) {
                Console.WriteLine($"{sock._myIP} Not Connect");
            }

            LocalReconnect();
        }

        private void OnLocalDisconnectedEvent(object? sender, LocalArgs e)
        {
            XLocalSocket? sock = sender as XLocalSocket;

            if (sock != null) {
                Console.WriteLine($"{sock._myIP} DisConnect");
            }

            LocalReconnect();
        }

        private void OnLocalReceiveEvent(object? sender, LocalArgs e)
        {
            if (e.rxData == null || e.Length <= 0)
                return;
            XLocalSocket? sock = sender as XLocalSocket;
            string msg = Encoding.GetEncoding("ks_c_5601").GetString(e.rxData, 0, e.Length);
            if (sock != null) {
                Console.WriteLine($"{sock._myIP}-{msg}");
            }

            if (msg == null)
                return;
            if (msg[3] == 'R' && msg[4] == 'L') {
                if (_displays != null) {
                    _displays.GateCommand((int)(msg[4] - 0x31), GateCmd.GATEOPEN);
                }
            }
            else if (msg[0] == 'C' && msg[1] == 'D') {
                if (msg[2] == '1' && msg[3] == '1') {
                    UiHost.ResetToMenu();
                }
            }
            else if (msg[0] == 'C' && msg[1] == 'K') {
                int salecode = int.TryParse(msg.Substring(2, 2), out int tmpCode) ? tmpCode : 0;
                if (salecode > 0 && salecode < 100) {
                    string logdata = $"유인할인 : {msg}";
                    XLogClass?.SaveLogString("CAL", logdata);
                    BeginInvoke(new Action(() =>
                    {
                        if (_calForm == null || _calForm.IsDisposed)
                            return;

                        _calForm.SaleKeyParkFuction(salecode, 2, 1);
                        StructHelper.CopyTparkinfoToStruct(_xparkinfo!, ref stparkinfo);
                        XSendPacketData<STTPARKINFO>(req_cmd_code.APS_CMD_WEBCARNUM, stparkinfo, "0.0.0.0");
                    }));
                }
            }
        }

        public void XLPRCancelDataSend()
        {
            if (XLprSocket != null) {
                XLprSocket.SendStringPacket("CD11");
            }
        }
    }
}
