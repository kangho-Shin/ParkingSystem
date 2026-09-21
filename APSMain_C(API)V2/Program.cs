using APSMain.BaseClass;
using System.Configuration;
using System.Diagnostics;
using System.Text;

namespace APSMain
{
    internal static class Program
    {
        static string _strFileHdr = string.Empty;
        static int _restarting = 0;
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            if (IsExistProcess(Process.GetCurrentProcess().ProcessName)) {
                Console.WriteLine("APSMain 프로그램이 이미 실행되었습니다.");
                return;
            }
            else {
                _strFileHdr = Path.Combine(Application.StartupPath, "LOG");
                if (!Directory.Exists(_strFileHdr)) {
                    Directory.CreateDirectory(_strFileHdr);
                }

                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

                Application.ThreadException += (s, e) =>
                {
                    SaveLogString(e.Exception.ToString());
                };

                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    var ex = e.ExceptionObject as Exception;

                    SaveLogString(ex?.ToString() ?? "Unknown Exception");

                    if (ex is ObjectDisposedException)
                        RestartProgram();
                };

                // To customize application configuration such as set high DPI settings or default font,
                // see https://aka.ms/applicationconfiguration.
                // EUC-KR(51949) 코드페이지 등록
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);


                ApplicationConfiguration.Initialize();

                Application.EnableVisualStyles();
                //Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
                Application.SetCompatibleTextRenderingDefault(false);

                Form form;
                APSConfig.ScreenMode = Convert.ToInt32(ConfigurationManager.AppSettings["APSSCREENMODE"]);

                var screens = Screen.AllScreens;
                if (APSConfig.ScreenMode == 15)
                    form = new MainForm15();   // 15인치 가로
                else
                    form = new MainForm();    // 24인치 세로

                // 모니터가 2개 이상이면 2번 모니터 선택 (index 1)
                if (screens.Length >= 2) {
                    var s = APSConfig.ScreenMode == 15 ? screens[0] : screens[1]; // 0=주모니터, 1=세컨더리
                                                                                  //var s = screens[1];
                    form.StartPosition = FormStartPosition.Manual;
                    form.FormBorderStyle = FormBorderStyle.None; // 키오스크면
                    form.TopMost = true;
                    if (APSConfig.ScreenMode == 15) {
                        form.Bounds = new Rectangle(
                            s.Bounds.Left,
                            s.Bounds.Top,
                            1152,
                            864
                        );
                        form.WindowState = FormWindowState.Normal;
                    }
                    else {
                        // 24인치: 전체 화면
                        form.Bounds = s.Bounds;
                        form.WindowState = FormWindowState.Normal;
                    }
                }

                Application.Run(form);
            }
        }

        static void RestartProgram()
        {
            if (Interlocked.CompareExchange(ref _restarting, 1, 0) != 0)
                return;

            try {
                SaveLogString("ObjectDisposedException 발생으로 프로그램을 재시작합니다.");

                string executablePath = Application.ExecutablePath;

                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c timeout /t 2 /nobreak > nul & start \"\" \"{executablePath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
            }
            catch (Exception ex) {
                SaveLogString("프로그램 재시작 실패: " + ex);
            }
            finally {
                Environment.Exit(1);
            }
        }

        public static void SaveLogString(string strMsg)
        {
            try {
                DateTime now = DateTime.Now;
                string logPath = Path.Combine(_strFileHdr, now.Year.ToString(), now.Month.ToString());
                Directory.CreateDirectory(logPath);

                string fileName = $"{now:yyyyMMdd}_EXC.log";
                string strFile = Path.Combine(logPath, fileName);
                string strTxt = $"{now:HH:mm:ss} | {strMsg}";

                using (StreamWriter stream = File.AppendText(strFile)) {
                    stream.WriteLine(strTxt);
                }
            }
            catch (Exception ex) {
                Console.WriteLine("AppendFileLog: " + ex.Message);
            }
        }

        static bool IsExistProcess(string processName)
        {
            Process[] process = Process.GetProcesses();
            int cnt = 0;

            //프로세스명으로 확인해서 동일한 프로세스 개수가 2개이상인지 확인합니다. 
            //현재실행하는 프로세스도 포함되기때문에 1보다커야합니다.
            foreach (var p in process) {
                if (p.ProcessName == processName)
                    cnt++;
                if (cnt > 1)
                    return true;
            }
            return false;
        }
    }
}


/*
    Encoding.GetEncoding("euc-kr")     // OK
    Encoding.GetEncoding("ks_c_5601-1987") // OK
    Encoding.GetEncoding(949)         // OK (CP949 = EUC-KR + 확장)

    .Net Core에서는 EUC-KR 인코딩을 사용하려면 CodePagesEncodingProvider를 등록해야 합니다.
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    Encoding eucKr = Encoding.GetEncoding("euc-kr"); // 또는 "ks_c_5601-1987"도 O
    Encoding eucKr = Encoding.GetEncoding(949); // 가장 안전하고 호환성 좋음

 DataObjects 디렉토리 설정
 Nuget 설치
 Microsoft.EntityFrameworkCore.Tools
 
 Mysql.Data.EntityFrameworkCore 이건 상용하지 말라고 함
 Mysql.EntityFrameworkCore      이건 에러 발생해서
 
 
 Pomelo.EntityFrameworkCore.MySql 이걸로 하라고 함


Install-Package Pomelo.EntityFrameworkCore.MySql -Version 7.0.0
Install-Package Microsoft.EntityFrameworkCore.Tools -Version 7.0.0

Package Manager Console 에서 다음 실행

Scaffold-DbContext "Server=localhost;Database=ipims;User=ipims;Password=!@Uparkdb1004" Pomelo.EntityFrameworkCore.MySql -OutputDir DbModels -f

Add-Migration InitialCreate
Update-Database

 */