using JPXLpr.BaseClass;
using JPXLpr.Edge;
using JPXLpr.HelpClass;
using JPXLpr.Novitec;
using System.ComponentModel;
using System.Configuration;
using System.Drawing.Imaging;
using System.Net;

namespace JPXLpr;

public partial class JPXLpr : Form
{
    public CameraView[]? _cameras;
    public NovaeyeWrapper _novaCar = null!;
    public static ConsoleManager? _MyConsole;
    public ContextMenuStrip _CTXMenu { get; set; } = new();
    public bool isManagerMode { get; set; }
    public int[] CarIndex = { 1001, 1001, 1001, 1001 };
    public ExpSchedule[] ExpConfig = Array.Empty<ExpSchedule>();
    private readonly Queue<LprWorkItem> _lprQueue = new();
    private readonly object _lprQueueLock = new();
    private readonly AutoResetEvent _lprQueueEvent = new(false);
    private Thread? _lprThread;
    private bool _keepLprThread;
    private CAMINFO[] camInfos = Array.Empty<CAMINFO>();
    private EdgeLprOutbox? _edgeOutbox;
    private UserControl? _expandControl;
    private int _oldRow;
    private int _oldCol;
    private bool _isClosing;
    private bool _consoleOpen;
    private CancellationTokenSource? _buttonLoopCts;

    public JPXLpr() => InitializeComponent();

    private async void JPXLpr_Load(object sender, EventArgs e)
    {
        if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
        JPXConfig.Sitenum = ReadInt("SITENUM", 9001);
        JPXConfig.Groupnum = ReadInt("GROUPNUM", 2);
        JPXConfig.EdgeServiceHost = ConfigurationManager.AppSettings["EDGESERVICEHOST"] ?? "127.0.0.1";
        JPXConfig.EdgeServicePort = ReadInt("EDGESERVICEPORT", 29200);
        JPXConfig.BASENUM = ReadInt("BASENUM", 401);
        JPXConfig.DummyTest = ReadBool("DUMMYTEST", false);
        JPXConfig.DebugMode = ReadBool("DEBUGMODE", false);

        if (JPXConfig.DebugMode )   ToggleConsole();
        ContextMenuInit();
        _cameras = new[] { camTV1, camTV2, camTV3, camTV4 };
        camInfos = CameraConfigFile.Load(Path.Combine(Application.StartupPath, "caminfo.json"));
        ExpConfig = ExpScheduleFile.Load(Path.Combine(Application.StartupPath, "expschedule.json"));
        if (camInfos.Length < 4 || ExpConfig.Length < 4)
            throw new InvalidOperationException("카메라와 노출 설정은 각각 4대 분량이 필요합니다.");
        EdgeLprOptions endpoint = new(JPXConfig.Sitenum, JPXConfig.Groupnum, 1, 1, "Entry", JPXConfig.EdgeServiceHost, JPXConfig.EdgeServicePort);
        _edgeOutbox = new EdgeLprOutbox(
            Path.Combine(Application.StartupPath, "EdgeLprOutbox.json"),
            new EdgeLprClient(endpoint),
            log: LogDisplay);
        _edgeOutbox.Start();

        await Task.Delay(100);
        _ = Task.Run(StartCameras);
    }

    private void StartCameras()
    {
        _novaCar = new NovaeyeWrapper();
        _novaCar.Initialize(null, null);
        for (int i = 0; i < _cameras!.Length; i++)
        {
            CAMINFO info = camInfos[i];
            CameraView camera = _cameras[i];
            camera.SetCameraInfo(this, info, ExpConfig[i]);
            camera.CameraNo = i + 1;
            if (!info.use || string.IsNullOrWhiteSpace(info.camip) || !IPAddress.TryParse(info.camip, out _)) continue;
            try
            {
                CameraEdgeOptions.Create(JPXConfig.Sitenum, JPXConfig.Groupnum, JPXConfig.EdgeServiceHost, JPXConfig.EdgeServicePort, info).Validate();
                camera.ImageReceived += Camera_ImageReceived;
                camera.ViewDoubleClicked += CameraView_DoubleClick;
                camera.StartCamera(info.camip, i + 1);
            }
            catch (Exception ex)
            {
                LogDisplay($"카메라 {i + 1} 설정 오류: {ex.Message}");
                Program.SaveLogString($"Camera {i + 1} startup error: {ex}", true);
            }
        }
        StartLpr();
    }

    private void JPXLpr_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (!_isClosing && e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; return; }
        _buttonLoopCts?.Cancel();
        StopLpr();
        if (_edgeOutbox is not null) _edgeOutbox.DisposeAsync().AsTask().GetAwaiter().GetResult();
        CameraConfigFile.Save(Path.Combine(Application.StartupPath, "caminfo.json"), camInfos);
        ExpScheduleFile.Save(Path.Combine(Application.StartupPath, "expschedule.json"), ExpConfig);
    }

    private void StartLpr()
    {
        _keepLprThread = true;
        _lprThread = new Thread(LprWorkLoop) { IsBackground = true, Name = "JPXLprRecognition" };
        _lprThread.Start();
    }

    private void StopLpr()
    {
        _keepLprThread = false;
        _lprQueueEvent.Set();
        _lprThread?.Join(1000);
        if (_cameras is null) return;
        foreach (CameraView camera in _cameras)
        {
            camera.ImageReceived -= Camera_ImageReceived;
            camera.ViewDoubleClicked -= CameraView_DoubleClick;
            camera.Disconnect();
        }
    }

    private void Camera_ImageReceived(object? sender, LprCameraImageEventArgs e)
    {
        if (sender is not CameraView camera || camera.isVideoMode) return;
        lock (_lprQueueLock) _lprQueue.Enqueue(new LprWorkItem { CameraNo = e.CameraNo, Bitmap = e.Bitmap });
        _lprQueueEvent.Set();
    }

    private void LprWorkLoop()
    {
        while (_keepLprThread)
        {
            _lprQueueEvent.WaitOne(100);
            LprWorkItem? item = null;
            lock (_lprQueueLock) if (_lprQueue.Count > 0) item = _lprQueue.Dequeue();
            if (item?.Bitmap is null) continue;
            using Bitmap bitmap = item.Bitmap;
            try { ProcessImage(item.CameraNo, bitmap); }
            catch (Exception ex) { Program.SaveLogString($"LprWorkLoop error: {ex}", true); }
        }
    }

    private void ProcessImage(int cameraNo, Bitmap bitmap)
    {
        CAMINFO info = camInfos[cameraNo - 1];
        string carNumber = JPXConfig.DummyTest ? DummyCarNumber(cameraNo) : Recognize(bitmap);
        EdgeLprEvent edgeEvent = EdgeLprEvent.Create(JPXConfig.Sitenum, JPXConfig.Groupnum,
            info.devicenum, info.laneid, info.direction, DateTime.Now, carNumber);
        SaveCaptureImage(bitmap, edgeEvent.FileName);
        _edgeOutbox?.Enqueue(edgeEvent);
        LogDisplay($"{edgeEvent.Direction} - {edgeEvent.FileName}");
    }

    private string Recognize(Bitmap bitmap)
    {
        byte[] imageData;
        using (MemoryStream stream = new()) { bitmap.Save(stream, ImageFormat.Jpeg); imageData = stream.ToArray(); }
        int count = 0;
        if (_novaCar.Recognize(imageData, imageData.Length, ref count) == 0)
        {
            NovaeyeWrapper.ALPRResult result = new();
            for (int i = 0; i < count; i++)
                if (_novaCar.RetrieveResult(i, ref result) == 0 && !string.IsNullOrWhiteSpace(result.text)) return result.text.Trim();
        }
        return "UNKNOWN";
    }

    private string DummyCarNumber(int cameraNo) => $"서울{cameraNo}가{CarIndex[cameraNo - 1]++:D4}";

    private static void SaveCaptureImage(Bitmap bitmap, string fileName)
    {
        string directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Capture");
        Directory.CreateDirectory(directory);
        using Bitmap saved = new(bitmap);
        saved.Save(Path.Combine(directory, fileName), ImageFormat.Jpeg);
    }

    private void ProcessFile(int cameraIndex)
    {
        using OpenFileDialog dialog = new() { Filter = "jpg files (*.jpg)|*.jpg|All files (*.*)|*.*" };
        if (dialog.ShowDialog() != DialogResult.OK) return;
        using Bitmap bitmap = new(dialog.FileName);
        ProcessImage(cameraIndex + 1, bitmap);
    }

    private void btnInFile_Click(object sender, EventArgs e) => ProcessFile(0);
    private void btnOutFile_Click(object sender, EventArgs e) => ProcessFile(2);

    private void StartButtonLoop()
    {
        _buttonLoopCts = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
            int index = 0;
            while (!_buttonLoopCts.IsCancellationRequested)
            {
                BeginInvoke(() => { _cameras?[index].PerformButtonClick(); index = (index + 1) % 4; });
                try { await Task.Delay(2500, _buttonLoopCts.Token); }
                catch (OperationCanceledException) { break; }
            }
        });
    }

    private void btnTest_Click(object sender, EventArgs e)
    {
        if (_buttonLoopCts is null) { StartButtonLoop(); btnTest.Text = "테스트중지"; }
        else { _buttonLoopCts.Cancel(); _buttonLoopCts = null; btnTest.Text = "테스트시작"; }
    }

    private void CameraView_DoubleClick(object? sender, EventArgs e)
    {
        if (sender is not CameraView camera) return;
        if (_expandControl == camera)
        {
            tlpMain.SetRowSpan(camera, 1); tlpMain.SetColumnSpan(camera, 1);
            tlpMain.SetRow(camera, _oldRow); tlpMain.SetColumn(camera, _oldCol);
            foreach (Control control in tlpMain.Controls) control.Visible = true;
            _expandControl = null; return;
        }
        _oldRow = tlpMain.GetRow(camera); _oldCol = tlpMain.GetColumn(camera);
        foreach (Control control in tlpMain.Controls) if (control != camera) control.Visible = false;
        tlpMain.SetRow(camera, 0); tlpMain.SetColumn(camera, 0);
        tlpMain.SetRowSpan(camera, tlpMain.RowCount); tlpMain.SetColumnSpan(camera, tlpMain.ColumnCount);
        camera.Visible = true; camera.Dock = DockStyle.Fill; _expandControl = camera;
    }

    private void ContextMenuInit()
    {
        _CTXMenu.Items.Add("콘솔", null, (_, _) => ToggleConsole());
        _CTXMenu.Items.Add("관리자", null, (_, _) => isManagerMode = !isManagerMode);
    }

    private void ToggleConsole()
    {
        _MyConsole ??= new ConsoleManager();
        if (_consoleOpen)   _MyConsole.Close(); 
        else                _MyConsole.Open();
        _consoleOpen = !_consoleOpen;
    }

    private void panMenu_MouseClick(object sender, MouseEventArgs e)
    { if (e.X < 50 && e.Y < 50) _CTXMenu.Show(panMenu, e.Location); }

    public void LogDisplay(string text)
    {
        if (InvokeRequired) { BeginInvoke(new Action<string>(LogDisplay), text); return; }

        string logData = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} => {text}";
        Console.WriteLine(logData);
        lbLog.Items.Insert(0, logData);
        while (lbLog.Items.Count > 500) lbLog.Items.RemoveAt(lbLog.Items.Count - 1);
    }

    private void btnExit_Click(object sender, EventArgs e)
    {
        //string? managerPassword = Environment.GetEnvironmentVariable("JPXLPR_MANAGER_PASSWORD");
        //if (string.IsNullOrWhiteSpace(managerPassword))
        //{
        //    MessageBox.Show(this, "JPXLPR_MANAGER_PASSWORD 환경변수를 설정하세요.");
        //    return;
        //}
        using ManagerPassForm form = new();
        if (form.ShowDialog() == DialogResult.OK && form.Password == "yes") { _isClosing = true; Application.Exit(); }
    }

    private static int ReadInt(string key, int fallback) => int.TryParse(ConfigurationManager.AppSettings[key], out int value) ? value : fallback;
    private static bool ReadBool(string key, bool fallback) => bool.TryParse(ConfigurationManager.AppSettings[key], out bool value) ? value : fallback;
}

public static class CameraEdgeOptions
{
    public static EdgeLprOptions Create(int site, int group, string host, int port, CAMINFO camera) =>
        new(site, group, camera.laneid, camera.devicenum, camera.direction, host, port);
}
