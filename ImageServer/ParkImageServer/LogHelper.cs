namespace ParkImageServer
{
    public static class LogHelper
    {
        private static readonly object _lock = new object();
        private static string _logRoot = string.Empty;
        private static DateTime _lastCleanupDate = DateTime.MinValue;

        public static void Init(string logRoot)
        {
            if (string.IsNullOrEmpty(logRoot))
                _logRoot = Path.Combine(AppContext.BaseDirectory, "log");
            else
                _logRoot = logRoot;

            if (!Directory.Exists(_logRoot))
                Directory.CreateDirectory(_logRoot);
        }

        public static void Write(string msg)
        {
            try {
                var now = DateTime.Now;

                string yearPath = Path.Combine(_logRoot, now.ToString("yyyy"));
                string monthPath = Path.Combine(yearPath, now.ToString("MM"));

                if (!Directory.Exists(monthPath))
                    Directory.CreateDirectory(monthPath);

                string logFile = Path.Combine(monthPath, now.ToString("yyyy-MM-dd") + ".log");

                string line = now.ToString("HH:mm:ss") + " " + msg;

                lock (_lock) {
                    File.AppendAllText(logFile, line + Environment.NewLine);

                    if (_lastCleanupDate.Date != now.Date) {
                        _lastCleanupDate = now.Date;
                        CleanupOldLogMonths(3);
                    }
                }
            }
            catch {
            }
        }

        private static void CleanupOldLogMonths(int keepMonths)
        {
            try {
                if (string.IsNullOrEmpty(_logRoot))
                    return;

                DateTime limitMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1)
                    .AddMonths(-(keepMonths - 1));

                foreach (string yearDir in Directory.GetDirectories(_logRoot)) {
                    string yearName = Path.GetFileName(yearDir);

                    if (!int.TryParse(yearName, out int year))
                        continue;

                    foreach (string monthDir in Directory.GetDirectories(yearDir)) {
                        string monthName = Path.GetFileName(monthDir);

                        if (!int.TryParse(monthName, out int month))
                            continue;

                        DateTime logMonth = new DateTime(year, month, 1);

                        if (logMonth < limitMonth)
                            Directory.Delete(monthDir, true);
                    }

                    // 월 폴더 다 지워져서 비었으면 년 폴더도 삭제
                    if (Directory.GetDirectories(yearDir).Length == 0 &&
                        Directory.GetFiles(yearDir).Length == 0) {
                        Directory.Delete(yearDir, false);
                    }
                }
            }
            catch {
            }
        }
    }
}
