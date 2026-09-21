using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace APSMain.BaseClass
{
    public class ClsLog
    {
        private readonly string _strFileHdr;
        private static ClsLog? _instance;
        private static readonly object flock = new object();

        public static ClsLog Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (flock)
                    {
                        if (_instance == null)
                            _instance = new ClsLog();
                    }
                }
                return _instance;
            }
        }

        private ClsLog()
        {
            _strFileHdr = Path.Combine(Application.StartupPath, "LOG");
        }

        public void SaveLogString(string logType, string strMsg)
        {
            try
            {
                DateTime now = DateTime.Now;
                string logPath = Path.Combine(_strFileHdr, now.Year.ToString(), now.Month.ToString());
                Directory.CreateDirectory(logPath); // 자동으로 상위 폴더까지 생성

                string fileName = $"{now:yyyyMMdd}_{APSConfig.APSNUM:D3}_{logType}.log";
                string strFile = Path.Combine(logPath, fileName);
                string strTxt = $"{now:HH:mm:ss} | {strMsg}";

                lock (flock)
                {
                    using (StreamWriter stream = File.AppendText(strFile))
                    {
                        stream.WriteLine(strTxt);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("AppendFileLog: " + ex.Message);
            }
        }
    }
}
