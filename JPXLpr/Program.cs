using System.Diagnostics;

namespace JPXLpr
{
    internal static class Program
    {
        static string _strFileHdr = string.Empty;
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            if (IsExistProcess(Process.GetCurrentProcess().ProcessName)) {
                MessageBox.Show("JPXLPR 프로그램이 이미 실행되었습니다.");
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
                };
                // To customize application configuration such as set high DPI settings or default font,
                // see https://aka.ms/applicationconfiguration.
                ApplicationConfiguration.Initialize();
                Application.Run(new JPXLpr());
            }
        }

        public static void SaveLogString(string strMsg,bool ctype=false)
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
                if(ctype == true)
                    Console.WriteLine(strTxt);
            }
            catch (Exception ex) {
                Console.WriteLine("AppendFileLog: " + ex.Message);
            }
        }

        static bool IsExistProcess(string processName)
        {
            Process[] process = Process.GetProcesses();
            int cnt = 0;

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
