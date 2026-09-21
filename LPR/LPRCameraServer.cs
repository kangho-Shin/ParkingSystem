using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace APSMain.Tcpip
{
    public class LprPacket
    {
        public bool Recognized { get; set; }
        public string? Direction { get; set; }
        public int DeviceNum { get; set; }
        public int CarType { get; set; }
        public DateTime IOdatetime { get; set; }
        public string? Carnum { get; set; }
        public string? Raw { get; set; }
        public int ViewNum { get; set; }
        public string? Title { get; set; }
        public string? Uniq { get; set; }
        public string? image { get; set; }
    }

    public class LPRCameraServer
    {
        private Socket? _listener=null;
        private readonly int _port;
        private readonly int _maxConnections;
        private readonly ConcurrentDictionary<LPRCameraSession, byte> _sessions = new();
        private SocketAsyncEventArgs? _acceptEventArg;

        public event Action<LPRCameraSession, byte[]>? PacketReceived;

        public LPRCameraServer(int port, int maxConnections = 4)
        {
            _port = port;
            _maxConnections = maxConnections;
        }

        private void OnSessionPacket(LPRCameraSession s, byte[] packet)
        => PacketReceived?.Invoke(s, packet);

        private void OnSessionClosedInternal(LPRCameraSession s)
            => OnSessionClosed(s);

        public void Start()
        {
            if (_listener == null) {
                _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _listener.Bind(new IPEndPoint(IPAddress.Any, _port));
                _listener.Listen(_maxConnections);
            }
            _acceptEventArg = new SocketAsyncEventArgs();
            _acceptEventArg.Completed += AcceptCompleted;

            BeginAccept();
        }

        public void Stop()
        {
            _listener?.Close();
            if (_acceptEventArg != null) {
                _acceptEventArg.Completed -= AcceptCompleted;
                _acceptEventArg.Dispose();
            }

            foreach (var session in _sessions.Keys)
            {
                session.Close();
            }
            _sessions.Clear();
            _listener = null;
        }

        private void BeginAccept()
        {
            try {
                _acceptEventArg!.AcceptSocket = null;
                if (!_listener!.AcceptAsync(_acceptEventArg))
                    ProcessAccept(_acceptEventArg);
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
                BeginAccept();
            }
        }

        private void AcceptCompleted(object? sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            try {
                if (e.SocketError == SocketError.Success && e.AcceptSocket != null) {
                    if (_sessions.Count < _maxConnections) {
                        var session = new LPRCameraSession(e.AcceptSocket);
                        Console.WriteLine($"LPR {e.AcceptSocket.RemoteEndPoint} Connecting...");
                        // 람다 대신 메서드 그룹으로 구독
                        session.PacketReceived += OnSessionPacket;
                        session.CloseSocket += OnSessionClosedInternal;

                        if (_sessions.TryAdd(session, 0))
                            session.StartReceive();
                        else {
                            // 실패 시 구독 해제
                            session.PacketReceived -= OnSessionPacket;
                            session.CloseSocket -= OnSessionClosedInternal;
                            session.Close();
                        }
                    }
                    else {
                        e.AcceptSocket.Close();
                    }
                }
            }
            finally {
                BeginAccept();
            }
        }

        private void OnSessionClosed(LPRCameraSession session)
        {
            if (_sessions.TryRemove(session, out _)) {
                // 같은 메서드로 해제하면 딕셔너리 불필요
                session.PacketReceived -= OnSessionPacket;
                session.CloseSocket -= OnSessionClosedInternal;
                session.Close();
            }
        }

        public void Dispose() => Stop();
    }
}
