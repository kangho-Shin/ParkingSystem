using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace APSMain.Tcpip
{
    public class LDMDisplayClient
    {
        public readonly string _ip;
        private readonly int _port;
        private Socket? _socket;

        public bool IsConnected => _socket != null && _socket.Connected;

        private readonly byte[] _recvBuffer = new byte[512];
        private readonly byte[] _rxPackBuff = new byte[512];

        private int _rxFlag = 0;
        private int _rxPtr = 0;
        private bool _disconnectNotified = false;
        private bool _connecting = false;

        public event Action<byte[]>? MessageReceived;
        public event Action<string>? Disconnected;
        public event Action<string>? Onconnected;

        public LDMDisplayClient(string ip, int port)
        {
            _ip = ip;
            _port = port;
        }

        public bool SocketConnect()
        {
            if (_connecting)
                return true;

            if (_socket != null && _socket.Connected)
                return true;

            CloseSocketOnly();

            _disconnectNotified = false;
            _connecting = true;

            try {
                CloseSocketOnly();

                _disconnectNotified = false;

                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _socket.NoDelay = true;

                IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(_ip), _port);

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.RemoteEndPoint = ipep;
                args.Completed += OnConnect;

                bool pending = _socket.ConnectAsync(args);
                //Console.WriteLine($"CONNECT PENDING {_ip} : {pending}");
                if (!pending)
                    OnConnect(_socket, args);

                return true;
            }
            catch {
                _connecting = false;
                Disconnect();
                return false;
            }
        }

        private void OnConnect(object? sender, SocketAsyncEventArgs e)
        {
            _connecting = false;

            Console.WriteLine($"CONNECT RESULT {_ip}-{_port}: {e.SocketError}");
            if (e.SocketError == SocketError.OperationAborted) {
                e.Dispose();
                return;
            }

            if (e.SocketError == SocketError.Success && _socket != null && _socket.Connected) {
                Onconnected?.Invoke(_ip);
                StartReceive();
            }
            else {
                Disconnect();
            }

            e.Dispose();
        }

        private void StartReceive()
        {
            if (_socket == null)
                return;

            try {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.SetBuffer(_recvBuffer, 0, _recvBuffer.Length);
                args.Completed += ReceiveCompleted;

                bool pending = _socket.ReceiveAsync(args);

                if (!pending)
                    ReceiveCompleted(_socket, args);
            }
            catch {
                Disconnect();
            }
        }

        private void ReceiveCompleted(object? sender, SocketAsyncEventArgs e)
        {
            try {
                if (_socket == null || e.SocketError != SocketError.Success || e.BytesTransferred <= 0) {
                    e.Dispose();
                    Disconnect();
                    return;
                }

                byte[]? tempBuf = e.Buffer;

                if (tempBuf != null) {
                    for (int i = 0; i < e.BytesTransferred; i++) {
                        if (_rxFlag == 0x00) {
                            if (tempBuf[i] == 0x02) {
                                _rxFlag = 0x01;
                                _rxPtr = 0;
                            }
                        }
                        else {
                            if (tempBuf[i] == 0x03) {
                                byte[] packet = new byte[_rxPtr];
                                Array.Copy(_rxPackBuff, packet, _rxPtr);

                                MessageReceived?.Invoke(packet);

                                _rxFlag = 0x00;
                                _rxPtr = 0;
                            }
                            else {
                                if (_rxPtr >= _rxPackBuff.Length) {
                                    _rxFlag = 0x00;
                                    _rxPtr = 0;
                                }
                                else {
                                    _rxPackBuff[_rxPtr++] = tempBuf[i];
                                }
                            }
                        }
                    }
                }

                e.SetBuffer(_recvBuffer, 0, _recvBuffer.Length);

                bool pending = _socket.ReceiveAsync(e);

                if (!pending)
                    ReceiveCompleted(_socket, e);
            }
            catch {
                e.Dispose();
                Disconnect();
            }
        }

        public bool SendPacket(byte[] data)
        {
            if (_connecting || _socket == null || !_socket.Connected) {
                return false;
            }

            try {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.SetBuffer(data, 0, data.Length);
                args.Completed += SendCompleted;

                bool pending = _socket.SendAsync(args);

                if (!pending)
                    SendCompleted(_socket, args);
            }
            catch {
                Disconnect();
            }

            return true;
        }

        private void SendCompleted(object? sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success || e.BytesTransferred <= 0)
                Disconnect();

            e.Dispose();
        }

        public void Close()
        {
            Disconnect();
        }

        private void Disconnect()
        {
            if (_disconnectNotified)
                return;

            _disconnectNotified = true;

            CloseSocketOnly();

            _rxFlag = 0;
            _rxPtr = 0;

            Disconnected?.Invoke(_ip);
        }

        private void CloseSocketOnly()
        {
            try { _socket?.Shutdown(SocketShutdown.Both); }
            catch { }

            try { _socket?.Close(); }
            catch { }

            try { _socket?.Dispose(); }
            catch { }

            _socket = null;
        }
    }
}
