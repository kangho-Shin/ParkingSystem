using ImageUploadAgent.Models;
using System.Configuration;
using System.Text.Json;

namespace ImageUploadAgent
{
    public partial class MainForm : Form
    {
        private ContextMenuStrip _trayMenu;

        private CancellationTokenSource? _workCts;
        private Task? _workTask;

        private string _watchPath = @"D:\LPR\IMAGE";
        private string _serverUrl = "http://127.0.0.1:5000/api/image/upload";
        private int _scanIntervalMs = 2000;

        private bool _isExit = false;
        private bool _isRunning = false;
        public MainForm()
        {
            InitializeComponent();

            InitTray();
            InitFormData();
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            LoadSetting();
            await LoadEdgeSettingAsync();

            StartWork();
            btnStart.Enabled = false;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_isExit) {
                e.Cancel = true;
                Hide();
                return;
            }

            StopWork();

            if (_trayIcon != null) {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            }
        }


        private void InitFormData()
        {
            txtWatchPath.Text = _watchPath;
            txtServerUrl.Text = _serverUrl;
            txtScanInterval.Text = _scanIntervalMs.ToString();
            lblStatus.Text = "중지";
            lblCurrentFile.Text = "-";
        }

        private void InitTray()
        {
            _trayMenu = new ContextMenuStrip();
            _trayMenu.Items.Add("열기", null, OnTrayOpen);
            _trayMenu.Items.Add("시작", null, OnTrayStart);
            _trayMenu.Items.Add("중지", null, OnTrayStop);
            _trayMenu.Items.Add("종료", null, OnTrayExit);

            // _trayIcon = new NotifyIcon();
            _trayIcon.Text = "ImageUploadAgent";
            // _trayIcon.Icon = SystemIcons.Application;
            _trayIcon.ContextMenuStrip = _trayMenu;
            _trayIcon.Visible = true;
            _trayIcon.DoubleClick += OnTrayOpen;
        }

        private void OnTrayOpen(object? sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
            BringToFront();
        }

        private void OnTrayStart(object? sender, EventArgs e)
        {
            StartWork();
        }

        private void OnTrayStop(object? sender, EventArgs e)
        {
            StopWork();
        }

        private void OnTrayExit(object? sender, EventArgs e)
        {
            _isExit = true;
            Close();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            StartWork();
            btnStart.Enabled = false;
            btnStop.Enabled = true;
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            StopWork();
            btnStart.Enabled = true;
            btnStop.Enabled = false;
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.SelectedPath = txtWatchPath.Text;

            if (dlg.ShowDialog() == DialogResult.OK) {
                txtWatchPath.Text = dlg.SelectedPath;
            }
        }

        private void StartWork()
        {
            if (_isRunning)
                return;

            _watchPath = txtWatchPath.Text.Trim();
            _serverUrl = txtServerUrl.Text.Trim();

            if (!int.TryParse(txtScanInterval.Text.Trim(), out _scanIntervalMs))
                _scanIntervalMs = 2000;

            if (string.IsNullOrWhiteSpace(_watchPath) || !Directory.Exists(_watchPath)) {
                WriteLog("감시 폴더가 없거나 잘못됨");
                return;
            }

            if (string.IsNullOrWhiteSpace(_serverUrl)) {
                WriteLog("서버 URL이 비어 있음");
                return;
            }

            _workCts = new CancellationTokenSource();
            _workTask = Task.Run(() => WorkLoop(_workCts.Token));

            _isRunning = true;
            lblStatus.Text = "동작중";
            WriteLog("업로드 작업 시작");
        }

        private void StopWork()
        {
            if (!_isRunning)
                return;

            try {
                _workCts?.Cancel();
                _workTask?.Wait(3000);
            }
            catch {
            }

            _workCts?.Dispose();
            _workCts = null;
            _workTask = null;

            _isRunning = false;
            lblStatus.Text = "중지";
            lblCurrentFile.Text = "-";
            WriteLog("업로드 작업 중지");
        }

        private async Task WorkLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested) {
                try {
                    List<string> files = GetUploadFiles(_watchPath);
                    UpdatePendingCount(files.Count);

                    foreach (string filePath in files) {
                        if (token.IsCancellationRequested)
                            break;

                        FileInfo fi = new FileInfo(filePath);

                        if (DateTime.Now - fi.LastWriteTime < TimeSpan.FromSeconds(2))
                            continue;

                        UpdateCurrentFile(fi.Name);
                        UpdatePreview(filePath);

                        bool existsOnServer = await ExistsFile(fi.Name);

                        if (existsOnServer) {
                            try {
                                File.Delete(filePath);
                                WriteLog("서버에 이미 있어 로컬 삭제: " + fi.Name);
                            }
                            catch (Exception ex) {
                                WriteLog("중복파일 삭제 실패: " + fi.Name + " / " + ex.Message);
                            }
                            continue;
                        }

                        UpdateCurrentFile(fi.Name);
                        UpdatePreview(filePath);

                        bool uploadOk = await UploadFile(filePath, fi.Name);

                        if (uploadOk) {
                            try {
                                File.Delete(filePath);
                                WriteLog("업로드 성공 후 삭제: " + fi.Name);
                            }
                            catch (Exception ex) {
                                WriteLog("삭제 실패: " + fi.Name + " / " + ex.Message);
                            }
                        }
                        else {
                            WriteLog("업로드 실패: " + fi.Name);
                        }
                    }
                }
                catch (Exception ex) {
                    WriteLog("작업 오류: " + ex.Message);
                }

                try {
                    await Task.Delay(_scanIntervalMs, token);
                }
                catch {
                    break;
                }
            }
        }

        private List<string> GetUploadFiles(string watchPath)
        {
            var fileList = new List<string>();

            string extText = txtExtList.Text.Trim();
            if (string.IsNullOrWhiteSpace(extText))
                extText = "*.jpg;*.jpeg;*.png;*.bmp";

            string[] patterns = extText.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string pattern in patterns) {
                string extPattern = pattern.Trim();
                if (string.IsNullOrWhiteSpace(extPattern))
                    continue;

                try {
                    fileList.AddRange(Directory.GetFiles(watchPath, extPattern));
                }
                catch {
                }
            }

            return fileList
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();
        }

        private async Task<bool> UploadFile(string filePath, string fileName)
        {
            try {
                using var client = new System.Net.Http.HttpClient();
                using var form = new System.Net.Http.MultipartFormDataContent();
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var content = new System.Net.Http.StreamContent(fs);

                form.Add(content, "file", fileName);
                form.Add(new System.Net.Http.StringContent(fileName), "fileName");

                using var response = await client.PostAsync(_serverUrl, form);
                string resultText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode) {
                    WriteLog("업로드 실패(HTTP): " + fileName + " / " + (int)response.StatusCode);
                    return false;
                }

                var uploadResponse = JsonSerializer.Deserialize<UploadResponse>(resultText, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (uploadResponse == null) {
                    WriteLog("응답 없음: " + fileName);
                    return false;
                }

                if (uploadResponse.Result == 0) {
                    WriteLog("업로드 성공: " + fileName);
                    return true;
                }

                WriteLog("업로드 실패(API): " + fileName + " / " + uploadResponse.Message);
                return false;
            }
            catch (Exception ex) {
                WriteLog("업로드 예외: " + fileName + " / " + ex.Message);
                return false;
            }
        }

        private void UpdateCurrentFile(string fileName)
        {
            if (InvokeRequired) {
                BeginInvoke(new Action<string>(UpdateCurrentFile), fileName);
                return;
            }

            lblCurrentFile.Text = fileName;
        }

        private void UpdatePendingCount(int count)
        {
            if (InvokeRequired) {
                BeginInvoke(new Action<int>(UpdatePendingCount), count);
                return;
            }

            lblPendingCount.Text = count.ToString();
        }

        private void UpdatePreview(string filePath)
        {
            if (InvokeRequired) {
                Invoke(new Action<string>(UpdatePreview), filePath);
                return;
            }

            try {
                // 기존 이미지 해제
                if (pictureBoxPreview.Image != null) {
                    pictureBoxPreview.Image.Dispose();
                    pictureBoxPreview.Image = null;
                }

                // 파일 열기 (공유 모드)
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                    var img = Image.FromStream(fs);
                    pictureBoxPreview.Image = (Image)img.Clone(); // 중요: Clone
                }
            }
            catch {
            }
        }

        private void WriteLog(string msg)
        {
            if (InvokeRequired) {
                BeginInvoke(new Action<string>(WriteLog), msg);
                return;
            }

            string log = DateTime.Now.ToString("HH:mm:ss") + " " + msg;
            lstLog.Items.Insert(0, log);

            if (lstLog.Items.Count > 1000)
                lstLog.Items.RemoveAt(lstLog.Items.Count - 1);
        }

       


        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (WindowState == FormWindowState.Minimized) {
                Hide();
            }
        }

        private void LoadSetting()
        {
            txtWatchPath.Text = ConfigurationManager.AppSettings["WatchPath"] ?? @"D:\LPR\IMAGE";
            txtServerUrl.Text = ConfigurationManager.AppSettings["ServerUrl"] ?? "http://127.0.0.1:5000/api/image/upload";
            txtScanInterval.Text = ConfigurationManager.AppSettings["ScanInterval"] ?? "2000";
            // txtExtList.Text = ConfigurationManager.AppSettings["ExtList"] ?? "*.jpg;*.jpeg;*.png;*.bmp";
        }

        private async Task LoadEdgeSettingAsync()
        {
            try {
                string edgeUrl = ConfigurationManager.AppSettings["EdgeServiceBaseUrl"]
                    ?? "http://localhost:5200/";
                EdgeConfigurationClient client = new(edgeUrl);
                AgentConfiguration? value = await client.GetAsync(CancellationToken.None);
                if (value == null) {
                    WriteLog("EdgeService 영상 설정을 불러오지 못해 로컬 설정을 사용합니다.");
                    return;
                }
                txtWatchPath.Text = value.ImageWatchPath;
                txtServerUrl.Text = value.ImageServerUrl.TrimEnd('/') + "/api/image/upload";
                WriteLog("EdgeService 영상 설정을 적용했습니다.");
            }
            catch (Exception ex) {
                WriteLog("EdgeService 설정 오류: " + ex.Message);
            }
        }

        private void SaveSetting()
        {
            SaveAppSetting("WatchPath", txtWatchPath.Text.Trim());
            SaveAppSetting("ServerUrl", txtServerUrl.Text.Trim());
            SaveAppSetting("ScanInterval", txtScanInterval.Text.Trim());
            //SaveAppSetting("ExtList", txtExtList.Text.Trim());
        }

        private void SaveAppSetting(string key, string value)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            if (config.AppSettings.Settings[key] != null)
                config.AppSettings.Settings[key].Value = value;
            else
                config.AppSettings.Settings.Add(key, value);

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        private async Task<bool> ExistsFile(string fileName)
        {
            try {
                using var client = new System.Net.Http.HttpClient();

                string baseUrl = _serverUrl.Replace("/upload", "/exists");
                string url = baseUrl + "?fileName=" + Uri.EscapeDataString(fileName);

                var response = await client.GetAsync(url);
                string resultText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode) {
                    WriteLog("실패(HTTP): " + fileName + " / " + (int)response.StatusCode);
                    return false;
                }

                ExistsResponse? existsResponse = System.Text.Json.JsonSerializer.Deserialize<ExistsResponse>(
                    resultText,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (existsResponse == null) {
                    WriteLog("응답 없음: " + fileName);
                    return false;
                }

                if (existsResponse.Result != 0) {
                    WriteLog("실패(API): " + fileName + " / " + existsResponse.Message);
                    return false;
                }

                return existsResponse.Exists;
            }
            catch (Exception ex) {
                WriteLog("예외: " + fileName + " / " + ex.Message);
                return false;
            }
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            BeginInvoke(new Action(() =>
            {
                Hide();
            }));
        }
    }
}
