using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace APSMain.Tcpip
{
    public class PaymentSocket
    {
        private Socket? _socket;
        private SocketAsyncEventArgs? _recvArgs;
        private SocketAsyncEventArgs? _sendArgs;

        private byte[] _recvBuf = new byte[4096];
        private int _recvLen = 0;

        private const byte STX = 0x02;
        private const byte ETX = 0x03;

        public event Action<string>? OnMessage;
        public event Action<string>? OnError;
        public event Action? OnDisconnected;

        public bool IsConnected
        {
            get { return _socket != null && _socket.Connected; }
        }

        public void Connect(string ip, int port)
        {
            try {
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                var connectArgs = new SocketAsyncEventArgs();
                connectArgs.RemoteEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
                connectArgs.Completed += ConnectCompleted;

                if (!_socket.ConnectAsync(connectArgs))
                    ConnectCompleted(this, connectArgs);
            }
            catch (Exception ex) {
                RaiseError(ex.Message);
            }
        }

        private void ConnectCompleted(object? sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success) {
                RaiseError("Connect Fail: " + e.SocketError);
                return;
            }

            InitRecv();
        }

        private void InitRecv()
        {
            _recvArgs = new SocketAsyncEventArgs();
            _recvArgs.SetBuffer(new byte[1024], 0, 1024);
            _recvArgs.Completed += IOCompleted;

            StartRecv();
        }

        private void StartRecv()
        {
            if ( _socket !=null && !_socket.ReceiveAsync(_recvArgs))
                ProcessRecv(_recvArgs);
        }

        private void IOCompleted(object? sender, SocketAsyncEventArgs e)
        {
            if (e.LastOperation == SocketAsyncOperation.Receive)
                ProcessRecv(e);
            else if (e.LastOperation == SocketAsyncOperation.Send) {
                // send complete
            }
        }

        private void ProcessRecv(SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred <= 0 || e.SocketError != SocketError.Success) {
                Close();
                RaiseDisconnected();
                return;
            }

            BufferAdd(e.Buffer, e.BytesTransferred);
            ParseBuffer();

            StartRecv();
        }

        private void BufferAdd(byte[] data, int len)
        {
            if (_recvLen + len > _recvBuf.Length)
                _recvLen = 0;

            Buffer.BlockCopy(data, 0, _recvBuf, _recvLen, len);
            _recvLen += len;
        }

        private void ParseBuffer()
        {
            int pos = 0;

            while (true) {
                int stx = FindByte(STX, pos);
                if (stx < 0)
                    break;

                int etx = FindByte(ETX, stx + 1);
                if (etx < 0)
                    break;

                int len = etx - stx - 1;
                if (len <= 0) {
                    pos = etx + 1;
                    continue;
                }

                string msg = Encoding.UTF8.GetString(_recvBuf, stx + 1, len);

                OnMessage?.Invoke(msg);

                pos = etx + 1;
            }

            if (pos > 0) {
                int remain = _recvLen - pos;
                if (remain > 0)
                    Buffer.BlockCopy(_recvBuf, pos, _recvBuf, 0, remain);

                _recvLen = remain;
            }
        }

        private int FindByte(byte val, int start)
        {
            for (int i = start; i < _recvLen; i++) {
                if (_recvBuf[i] == val)
                    return i;
            }
            return -1;
        }

        public void Send(string msg)
        {
            if (!IsConnected)
                return;

            byte[] body = Encoding.UTF8.GetBytes(msg);
            byte[] packet = new byte[body.Length + 2];

            packet[0] = STX;
            Buffer.BlockCopy(body, 0, packet, 1, body.Length);
            packet[packet.Length - 1] = ETX;

            _sendArgs = new SocketAsyncEventArgs();
            _sendArgs.SetBuffer(packet, 0, packet.Length);
            _sendArgs.Completed += IOCompleted;

            if ( _socket != null && !_socket.SendAsync(_sendArgs))
                IOCompleted(this, _sendArgs);
        }

        public void SendApprove(string paySeq, int amount, int vat)
        {
            Send("APPROVE|" + paySeq + "|" + amount + "|" + vat);
        }

        public void SendChange(string paySeq, int amount, int vat)
        {
            Send("CHANGE|" + paySeq + "|" + amount + "|" + vat);
        }

        public void SendCancel(string paySeq, int amount, int vat, string orgDate, string orgNo)
        {
            Send("CANCEL|" + paySeq + "|" + amount + "|" + vat + "|" + orgDate + "|" + orgNo);
        }

        public void SendReset(string paySeq)
        {
            Send("RESET|" + paySeq);
        }

        public void SendPing()
        {
            Send("PING");
        }

        public void Close()
        {
            try { _socket?.Close(); }
            catch { }
            _socket = null;
            _recvLen = 0;
        }

        private void RaiseError(string msg)
        {
            OnError?.Invoke(msg);
        }

        private void RaiseDisconnected()
        {
            OnDisconnected?.Invoke();
        }
    }
}
