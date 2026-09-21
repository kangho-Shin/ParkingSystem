using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace APSMain.BaseClass
{
    public class DbJobWorker : IDisposable
    {
        private const int MaxDbQueue = 1000;

        private readonly object _queueLock = new object();
        private readonly Queue<string> _jobQueue = new Queue<string>();

        private Thread? _workerThread;
        private AutoResetEvent _workEvent = new AutoResetEvent(false);
        private ManualResetEvent _stopEvent = new ManualResetEvent(false);

        private volatile bool _isStopping;
        private int _queueCount;

        private string _connectionString = string.Empty;
        private MySqlConnection? _connection;

        private readonly string _queueFilePath = "dbqueue.txt";

        public bool Start(string server, string user, string password, string database, int port)
        {
            if (_workerThread != null)
                return true;

            _connectionString =
                $"Server={server};Port={port};Database={database};Uid={user};Pwd={password};Charset=utf8;SslMode=none;";

            if (!Connect())
                return false;

            LoadRemainQueue(_queueFilePath);

            _isStopping = false;
            _stopEvent.Reset();

            _workerThread = new Thread(Run);
            _workerThread.IsBackground = true;
            _workerThread.Start();

            return true;
        }

        public void Stop()
        {
            _isStopping = true;

            SaveRemainQueue(_queueFilePath);

            _stopEvent.Set();
            _workEvent.Set();

            if (_workerThread != null) {
                if (!_workerThread.Join(5000)) {
                    try { _workerThread.Interrupt(); }
                    catch { }
                }
                _workerThread = null;
            }

            CloseConnection();
        }

        public bool PushQuery(string sqlText)
        {
            if (_isStopping)
                return false;

            if (string.IsNullOrWhiteSpace(sqlText))
                return false;

            lock (_queueLock) {
                if (_queueCount >= MaxDbQueue) {
                    Console.WriteLine("DB Queue FULL!!! drop query");
                    return false;
                }

                _jobQueue.Enqueue(sqlText);
                _queueCount++;
            }

            _workEvent.Set();
            return true;
        }

        private void Run()
        {
            WaitHandle[] waitHandles = { _stopEvent, _workEvent };

            while (true) {
                int waitRet = WaitHandle.WaitAny(waitHandles);

                if (waitRet == 0)
                    break;

                if (waitRet == 1) {
                    while (true) {
                        string? sqlText = PopQuery();
                        if (sqlText == null)
                            break;

                        ExecuteQuery(sqlText);
                    }
                }
            }
        }

        private string? PopQuery()
        {
            lock (_queueLock) {
                if (_jobQueue.Count == 0)
                    return null;

                string sqlText = _jobQueue.Dequeue();
                _queueCount--;
                return sqlText;
            }
        }

        private bool ExecuteQuery(string sqlText)
        {
            if (_connection == null)
                return false;

            try {
                using var cmd = new MySqlCommand(sqlText, _connection);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (MySqlException ex) {
                // duplicate key
                if (ex.Number == 1062) {
                    Console.WriteLine("Duplicate key skip");
                    return true;
                }

                // 데이터 자체 오류는 재시도 금지
                if (ex.Number == 1406 || ex.Number == 1366) {
                    Console.WriteLine($"DB DATA ERROR skip : {ex.Number} / {ex.Message}");
                    return true;
                }

                // 연결 끊김 계열
                if (IsConnectionError(ex)) {
                    Console.WriteLine("DB reconnect try...");

                    if (!Reconnect()) {
                        PushQuery(sqlText);
                        Thread.Sleep(100);
                        return false;
                    }

                    try {
                        using var retryCmd = new MySqlCommand(sqlText, _connection);
                        retryCmd.ExecuteNonQuery();
                        return true;
                    }
                    catch (MySqlException retryEx) {
                        if (retryEx.Number == 1062) {
                            Console.WriteLine("Duplicate key skip after reconnect");
                            return true;
                        }

                        if (retryEx.Number == 1406 || retryEx.Number == 1366) {
                            Console.WriteLine($"DB DATA ERROR skip : {retryEx.Number} / {retryEx.Message}");
                            return true;
                        }

                        Console.WriteLine($"Retry failed : {retryEx.Number} / {retryEx.Message}");
                        PushQuery(sqlText);
                        Thread.Sleep(100);
                        return false;
                    }
                }

                Console.WriteLine($"DB ERROR : {ex.Number} / {ex.Message}");
                PushQuery(sqlText);
                Thread.Sleep(50);
                return false;
            }
            catch (Exception ex) {
                Console.WriteLine($"DB UNKNOWN ERROR : {ex.Message}");
                PushQuery(sqlText);
                Thread.Sleep(50);
                return false;
            }
        }

        private bool Connect()
        {
            CloseConnection();

            try {
                _connection = new MySqlConnection(_connectionString);
                _connection.Open();

                using var cmd = new MySqlCommand("SET NAMES utf8", _connection);
                cmd.ExecuteNonQuery();

                return true;
            }
            catch (Exception ex) {
                Console.WriteLine($"mysql connect error: {ex.Message}");
                CloseConnection();
                return false;
            }
        }

        private bool Reconnect()
        {
            CloseConnection();
            Thread.Sleep(500);
            return Connect();
        }

        private void CloseConnection()
        {
            if (_connection != null) {
                try { _connection.Close(); }
                catch { }
                try { _connection.Dispose(); }
                catch { }
                _connection = null;
            }
        }

        private bool IsConnectionError(MySqlException ex)
        {
            return ex.Number == 0
                || ex.Number == 1042
                || ex.Number == 1047
                || ex.Number == 1152
                || ex.Number == 1153
                || ex.Number == 1158
                || ex.Number == 1159
                || ex.Number == 1160
                || ex.Number == 1161
                || ex.Number == 2002
                || ex.Number == 2003
                || ex.Number == 2006
                || ex.Number == 2013;
        }

        private bool SaveRemainQueue(string filePath)
        {
            try {
                StringBuilder sb = new StringBuilder();

                lock (_queueLock) {
                    foreach (string item in _jobQueue) {
                        string line = item.Replace("\r", " ").Replace("\n", " ");
                        sb.AppendLine(line);
                    }
                }

                if (sb.Length == 0) {
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                    return true;
                }

                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                return true;
            }
            catch {
                return false;
            }
        }

        private bool LoadRemainQueue(string filePath)
        {
            try {
                if (!File.Exists(filePath))
                    return true;

                string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);

                lock (_queueLock) {
                    foreach (string line in lines) {
                        string sqlText = line.Trim();
                        if (string.IsNullOrWhiteSpace(sqlText))
                            continue;

                        if (_queueCount >= MaxDbQueue)
                            break;

                        _jobQueue.Enqueue(sqlText);
                        _queueCount++;
                    }
                }

                File.Delete(filePath);
                return true;
            }
            catch {
                return false;
            }
        }

        public void Dispose()
        {
            Stop();
            _workEvent.Dispose();
            _stopEvent.Dispose();
        }
    }
}

/*
 private DbJobWorker _dbJobWorker = new DbJobWorker();

private void StartDbWorker()
{
    _dbJobWorker.Start("127.0.0.1", "uparkdb", "!@Uparkdb1004", "uparkdb", 3306);
}

private void StopDbWorker()
{
    _dbJobWorker.Stop();
}

private void InsertTest()
{
    _dbJobWorker.PushQuery("insert into testtable(carnum, intime) values('123가4567', now())");
}
 */