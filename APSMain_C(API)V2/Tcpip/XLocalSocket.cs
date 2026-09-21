using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace APSMain.Tcpip
{
    public class LocalArgs : EventArgs
    {
        public string? myip;
        public byte[]? rxData;
        public string? message;
        public int Length;

        public LocalArgs(string? ip, byte[]? indata, string? msg, int length)
        {
            myip = ip;
            if (length > 0 && indata != null) {
                rxData = new byte[length];
                Array.Copy(indata, rxData, length);
            }
            message = msg;
            Length = length;
        }
    }
    public class XLocalSocket
    {
        public event EventHandler<LocalArgs>? OnLocalSendEvent;
        public event EventHandler<LocalArgs>? OnLocalReceiveEvent;
        public event EventHandler<LocalArgs>? OnLocalConnectEvent;
        public event EventHandler<LocalArgs>? OnLocalDisconnectedEvent;
        public event EventHandler<LocalArgs>? OnLocalNotConnectEvent;

        private Socket? _socket = null;
        public SocketAsyncEventArgs? rArgs;
        public int RxPtr = 0;
        public int RxFlag = 0;
        public bool isConnected;
        public byte[]? RxData;
        public byte[]? TxPackBuff;
        public byte[]? RxPackBuff;
        public string? _myIP;
        public int _myPort;

        public const byte STX = 0x02;
        public const byte ETX = 0x03;

        public XLocalSocket(string ip, int port)
        {
            _socket = null;
            _myIP = ip;
            _myPort = port;

            RxPtr = 0;
            RxFlag = 0;
            isConnected = false;

            RxData = new byte[2048];
            RxPackBuff = new byte[2048];
            TxPackBuff = new byte[512];
        }

        ~XLocalSocket()
        {
            try {
                ((IDisposable)this).Dispose();
            }
            catch { }
            GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            if (_socket != null) {
                SocketClose();
            }
        }

        public bool SocketConnect(string svrip, int svrPort)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(svrip), svrPort);

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = ipep;
            args.Completed += new EventHandler<SocketAsyncEventArgs>(Connect_Completed!);

            bool pending = _socket.ConnectAsync(args);
            if (!pending)
                Connect_Completed(_socket, args);

            return true;
        }

        public void SocketClose()
        {
            isConnected = false;

            try {
                if (rArgs != null) {
                    rArgs.Completed -= Receive_Completed!;
                    rArgs.SetBuffer(null, 0, 0);
                    rArgs.UserToken = null;
                    rArgs.Dispose();
                    rArgs = null;
                }

                if (_socket != null) {
                    try { _socket.Shutdown(SocketShutdown.Both); }
                    catch { }

                    _socket.Close();
                    _socket.Dispose();
                    _socket = null;
                }
            }
            catch { }
        }

        private void Connect_Completed(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success && _socket != null && _socket.Connected) {
                rArgs = new SocketAsyncEventArgs();

                RxData = new byte[2048];
                RxPackBuff = new byte[2048];

                rArgs.SetBuffer(RxData, 0, RxData.Length);
                rArgs.Completed += Receive_Completed!;

                isConnected = true;

                OnLocalConnectEvent?.Invoke(this,
                    new LocalArgs(_myIP, null, "Connect Success", 0));

                bool pending = _socket.ReceiveAsync(rArgs);
                if (!pending)
                    Receive_Completed(_socket, rArgs);
            }
            else {
                isConnected = false;

                e.Dispose();

                _socket?.Dispose();
                _socket = null;

                OnLocalNotConnectEvent?.Invoke(this,
                    new LocalArgs(_myIP, null, "Not Connect", 0));
            }
        }


        private void Receive_Completed(object sender, SocketAsyncEventArgs e)
        {
            if (!isConnected)
                return;

            if (e.SocketError != SocketError.Success || e.BytesTransferred <= 0) {
                isConnected = false;

                OnLocalDisconnectedEvent?.Invoke(this,
                    new LocalArgs(_myIP, null, "Disconnected", 0));

                SocketClose();
                return;
            }

            byte[]? tempBuf = e.Buffer;
            if (tempBuf == null || RxPackBuff == null)
                return;

            int nReadSize = e.BytesTransferred;

            for (int i = 0; i < nReadSize; i++) {
                byte data = tempBuf[i];

                if (RxFlag == 0x00) {
                    if (data == STX) {
                        RxFlag = 0x01;
                        RxPtr = 0;
                    }
                }
                else {
                    if (data == ETX) {
                        LocalArgs ea = new LocalArgs(_myIP, RxPackBuff, "Receive Data", RxPtr);
                        OnLocalReceiveEvent?.Invoke(this, ea);

                        RxFlag = 0x00;
                        RxPtr = 0;
                    }
                    else {
                        if (RxPtr >= RxPackBuff.Length) {
                            RxFlag = 0x00;
                            RxPtr = 0;
                            continue;
                        }

                        RxPackBuff[RxPtr++] = data;
                    }
                }
            }

            if (_socket == null || rArgs == null)
                return;

            bool pending = _socket.ReceiveAsync(rArgs);
            if (!pending)
                Receive_Completed(_socket, rArgs);
        }

        public void SendPacket(byte[] TxData, string cdmsg, int Length)
        {
            if (_socket == null || !isConnected)
                return;

            List<byte> TPacket = new List<byte>();

            TPacket.Add(0x02); // STX
            //TPacket.AddRange(Encoding.GetEncoding("ks_c_5601").GetBytes(cdmsg));
            if (Length > 0) {
                TPacket.AddRange(TxData.Take(Length));
            }
            TPacket.Add(0x03); // ETX

            byte[] sendData = TPacket.ToArray();

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.Completed += OnSendCompleted!;
            args.SetBuffer(sendData, 0, sendData.Length);

            try {
                bool pending = _socket.SendAsync(args);
                if (!pending)
                    OnSendCompleted(_socket, args);
            }
            catch {
                args.Dispose();
                OnLocalSendEvent?.Invoke(this, new LocalArgs(_myIP, null, "Send Fail", 0));
            }
        }

        public void SendStringPacket(string xdata)
        {
            if (_socket == null || !isConnected)
                return;

            List<byte> TPacket = new List<byte>();

            TPacket.Add(0x02); // STX
            TPacket.AddRange(Encoding.GetEncoding("ks_c_5601").GetBytes(xdata));
            TPacket.Add(0x03); // ETX

            byte[] sendData = TPacket.ToArray();

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.Completed += OnSendCompleted!;
            args.SetBuffer(sendData, 0, sendData.Length);

            try {
                bool pending = _socket.SendAsync(args);
                if (!pending)
                    OnSendCompleted(_socket, args);
            }
            catch {
                args.Dispose();
                OnLocalSendEvent?.Invoke(this, new LocalArgs(_myIP, null, "Send Fail", 0));
            }
        }

        private void OnSendCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success) {
                OnLocalSendEvent?.Invoke(this,
                    new LocalArgs(_myIP, null, "Send Success", 0));
            }
            else {
                OnLocalSendEvent?.Invoke(this,
                    new LocalArgs(_myIP, null, "Send Fail", 0));
            }

            e.Completed -= OnSendCompleted!;
            e.SetBuffer(null, 0, 0);
            e.UserToken = null;
            e.Dispose();
        }
    }
}
