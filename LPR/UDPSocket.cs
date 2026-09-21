using APSMain.BaseClass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace APSMain.Tcpip
{
    public class UDPSocket
    {
        #region Constants
        private const int ReceiveBufSize = 4096;
        private const ushort HeaderMagic = 0xC0C0;
        private const int HEADER_SIZE = 8;
        private const int STUDPDATA_SIZE = 1066;
        private const int XPACKET_MAX_SIZE = 1272;
        #endregion

        #region Fields
        private Socket? _socket;
        private SocketAsyncEventArgs? _recvArgs;
        private byte[]? _recvBuf;

        private volatile bool _connected = false;
        private volatile bool _closing = false;

        private byte _rxFlag = 0x00;
        private int _rxLength = 0;
        private STUDPPACKET _rxPacket;
        private STUDPPACKET _txPacket;

        public ClsLog? XLogClass = ClsLog.Instance;

        public int DeviceNum { get; set; } = 0;
        public int DeviceType { get; set; } = 0;
        public string DeviceName { get; set; } = string.Empty;
        public string MyIP { get; set; } = string.Empty;
        #endregion

        #region Events
        public event Action<UDPSocket, int>? OnConnected;
        public event Action<UDPSocket, STUDPPACKET>? OnReceived;
        public event Action<UDPSocket, int>? OnDisconnected;
        public event Action<UDPSocket, string>? OnError;
        #endregion

        public bool Connected => _connected;
        public Socket? Socket => _socket;

        public UDPSocket()
        {
            XLogClass = ClsLog.Instance;
        }

        public UDPSocket(Socket accepted)
        {
            XLogClass = ClsLog.Instance;
            AttachAcceptedSocket(accepted);
        }

        public void AttachAcceptedSocket(Socket accepted)
        {
            ResetState();
            InitializeConnectedSocket(accepted);
        }

        public void Connect(string svrip, int svrPort)
        {
            ResetState();

            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sock.NoDelay = true;

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = new IPEndPoint(IPAddress.Parse(svrip), svrPort);
            args.UserToken = sock;
            args.Completed += Connect_Completed;

            bool pending = sock.ConnectAsync(args);
            if (!pending)
                Connect_Completed(sock, args);
        }

        private void Connect_Completed(object? sender, SocketAsyncEventArgs e)
        {
            try {
                Socket? sock = e.UserToken as Socket ?? sender as Socket;
                if (sock == null) {
                    OnError?.Invoke(this, "Connect socket is null");
                    Close(1);
                    return;
                }

                if (e.SocketError != SocketError.Success) {
                    OnError?.Invoke(this, $"Connect failed : {e.SocketError}");
                    try { sock.Close(); }
                    catch { }
                    Close(1);
                    return;
                }

                InitializeConnectedSocket(sock);
                OnConnected?.Invoke(this, 0);
            }
            catch (Exception ex) {
                OnError?.Invoke(this, $"Connect_Completed error : {ex.Message}");
                Close(1);
            }
            finally {
                e.Dispose();
            }
        }

        private void InitializeConnectedSocket(Socket sock)
        {
            _socket = sock;
            _socket.NoDelay = true;
            _connected = true;
            _closing = false;

            try {
                if (_socket.RemoteEndPoint is IPEndPoint ep)
                    MyIP = ep.Address.ToString();
            }
            catch {
                MyIP = string.Empty;
            }

            PrepareReceiveArgs();
        }

        private void PrepareReceiveArgs()
        {
            _recvBuf = new byte[ReceiveBufSize];
            _recvArgs = new SocketAsyncEventArgs();
            _recvArgs.SetBuffer(_recvBuf, 0, _recvBuf.Length);
            _recvArgs.Completed += OnReceiveCompleted;
            _recvArgs.UserToken = this;
        }

        public void StartReceive()
        {
            if (!_connected || _socket == null || _recvArgs == null)
                return;

            try {
                if (!_socket.ReceiveAsync(_recvArgs))
                    OnReceiveCompleted(_socket, _recvArgs);
            }
            catch (Exception ex) {
                OnError?.Invoke(this, $"StartReceive error : {ex.Message}");
                Close(2);
            }
        }

        public void Close(int retval)
        {
            if (_closing)
                return;

            _closing = true;
            _connected = false;

            try { _socket?.Shutdown(SocketShutdown.Both); }
            catch { }
            try { _socket?.Close(); }
            catch { }
            try { _recvArgs?.Dispose(); }
            catch { }

            _socket = null;
            _recvArgs = null;
            _recvBuf = null;

            OnDisconnected?.Invoke(this, retval);
        }

        public void XSendPacketData<T>(req_cmd_code packetComm, in T xdata, string desip) where T : unmanaged
        {
            if (!_connected || _socket == null)
                return;

            try {
                STUDPPACKET packet = default;
                STUDPDATA udata = default;

                udata.devicenum = (ushort)APSConfig.APSNUM;
                StructHelper.WriteString(ref udata, nameof(STUDPDATA.srcip), 20, APSConfig.APSIP);
                StructHelper.WriteString(ref udata, nameof(STUDPDATA.desip), 20, desip);

                StructHelper.WriteUnmanaged(ref udata, nameof(STUDPDATA.xdata), 1024, in xdata);

                packet.btID = HeaderMagic;
                packet.wClientID = 0x9999;
                packet.wtCmd = (ushort)packetComm;
                packet.nSize = (ushort)Unsafe.SizeOf<STUDPDATA>();

                StructHelper.WriteUnmanaged(ref packet, nameof(STUDPPACKET.xPacket), Unsafe.SizeOf<STUDPDATA>(), in udata);

                byte[] sendData = StructHelper.StructToBytes(packet);
                int sent = _socket.Send(sendData, 0, HEADER_SIZE + STUDPDATA_SIZE, SocketFlags.None);

                if (sent != HEADER_SIZE + STUDPDATA_SIZE)
                    OnError?.Invoke(this, $"Send size mismatch : {sent}/{HEADER_SIZE + STUDPDATA_SIZE}");
            }
            catch (Exception ex) {
                SaveLogString(ex.Message);
                OnError?.Invoke(this, ex.Message);
            }
        }

        public int SendPacketData(req_cmd_code packetComm, byte[] packetData, int size)
        {
            if (!_connected || _socket == null)
                return -1;

            if (size != STUDPDATA_SIZE) {
                OnError?.Invoke(this, $"Invalid payload size: {size}, required {STUDPDATA_SIZE}");
                return -1;
            }

            if (packetData.Length < STUDPDATA_SIZE)
                return -1;

            _txPacket = default;
            _txPacket.btID = HeaderMagic;
            _txPacket.wClientID = 0x9999;
            _txPacket.wtCmd = (ushort)packetComm;
            _txPacket.nSize = (ushort)STUDPDATA_SIZE;

            StructHelper.WriteBytes(ref _txPacket, nameof(STUDPPACKET.xPacket), STUDPDATA_SIZE, packetData);

            byte[] buf = StructHelper.StructToBytes(_txPacket);

            try {
                return _socket.Send(buf, 0, HEADER_SIZE + STUDPDATA_SIZE, SocketFlags.None);
            }
            catch (Exception ex) {
                SaveLogString(ex.Message);
                OnError?.Invoke(this, ex.Message);
                return -1;
            }
        }

        public int SendData(byte[] raw, int length)
        {
            if (!_connected || _socket == null)
                return -1;

            if (length < 0)
                length = 0;
            if (length > raw.Length)
                length = raw.Length;

            try {
                return _socket.Send(raw, 0, length, SocketFlags.None);
            }
            catch (Exception ex) {
                SaveLogString(ex.Message);
                OnError?.Invoke(this, ex.Message);
                return -1;
            }
        }

        private void OnReceiveCompleted(object? sender, SocketAsyncEventArgs e)
        {
            if (_closing)
                return;

            if (e.SocketError != SocketError.Success || e.BytesTransferred <= 0) {
                Close(0);
                return;
            }

            try {
                int len = e.BytesTransferred;
                byte[] buf = e.Buffer!;

                for (int i = 0; i < len; i++) {
                    byte b = buf[i];

                    switch (_rxFlag) {
                        case 0x00:
                            if (b == 0xC0)
                                _rxFlag = 0x01;
                            _rxLength = 0;
                            break;

                        case 0x01:
                            _rxFlag = (b == 0xC0) ? (byte)0x02 : (byte)0x00;
                            break;

                        case 0x02:
                            _rxPacket = default;
                            _rxPacket.wClientID = b;
                            _rxFlag = 0x03;
                            break;

                        case 0x03:
                            _rxPacket.wClientID += (ushort)(b << 8);
                            _rxFlag = 0x04;
                            break;

                        case 0x04:
                            _rxPacket.wtCmd = b;
                            _rxFlag = 0x05;
                            break;

                        case 0x05:
                            _rxPacket.wtCmd += (ushort)(b << 8);
                            _rxFlag = 0x06;
                            break;

                        case 0x06:
                            _rxPacket.nSize = b;
                            _rxFlag = 0x07;
                            break;

                        case 0x07:
                            _rxPacket.nSize += (ushort)(b << 8);

                            if (_rxPacket.nSize != STUDPDATA_SIZE || _rxPacket.nSize > XPACKET_MAX_SIZE) {
                                _rxFlag = 0x00;
                                _rxLength = 0;
                            }
                            else {
                                _rxLength = 0;
                                _rxFlag = 0x08;
                            }
                            break;

                        case 0x08:
                            unsafe {
                                fixed (byte* p = _rxPacket.xPacket) {
                                    p[_rxLength] = b;
                                }
                            }

                            _rxLength++;

                            if (_rxLength >= STUDPDATA_SIZE) {
                                _rxPacket.btID = HeaderMagic;
                                STUDPPACKET packet = _rxPacket;
                                OnReceived?.Invoke(this, packet);
                                _rxFlag = 0x00;
                                _rxLength = 0;
                            }
                            break;

                        default:
                            _rxFlag = 0x00;
                            _rxLength = 0;
                            break;
                    }
                }

                if (_socket == null || _recvBuf == null) {
                    Close(2);
                    return;
                }

                e.SetBuffer(0, _recvBuf.Length);

                if (!_socket.ReceiveAsync(e))
                    OnReceiveCompleted(_socket, e);
            }
            catch (Exception ex) {
                OnError?.Invoke(this, $"Receive error : {ex.Message}");
                Close(3);
            }
        }

        private void ResetState()
        {
            _connected = false;
            _closing = false;
            _rxFlag = 0x00;
            _rxLength = 0;
            _rxPacket = default;
            _txPacket = default;

            try { _recvArgs?.Dispose(); }
            catch { }
            try { _socket?.Close(); }
            catch { }

            _socket = null;
            _recvArgs = null;
            _recvBuf = null;
        }

        private void SaveLogString(string message)
        {
            try {
                XLogClass?.SaveLogString("LOG", message);
            }
            catch { }
        }
    }
}
