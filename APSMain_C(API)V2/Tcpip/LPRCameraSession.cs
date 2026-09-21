using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace APSMain.Tcpip
{
    public class LPRCameraSession
    {
        private readonly Socket _socket;
        private readonly SocketAsyncEventArgs _receiveEvent;
        private readonly byte[] _buffer = new byte[256];   // 버퍼 크기 조정 가능
        private readonly List<byte> _recvBuffer = new();    // 패킷 조립용
        private int _rxflag = 0;
        private int _rxcount = 0;
        public event Action<LPRCameraSession,byte[]>? PacketReceived;
        public event Action<LPRCameraSession>? CloseSocket;

        public LPRCameraSession(Socket socket)
        {
            _socket = socket;

            _receiveEvent = new SocketAsyncEventArgs();
            _receiveEvent.SetBuffer(_buffer, 0, _buffer.Length);
            _receiveEvent.Completed += ReceiveCompleted;
            _receiveEvent.UserToken = this;
        }

        public void StartReceive()
        {
            if (!_socket.ReceiveAsync(_receiveEvent))
                ProcessReceive(_receiveEvent);
        }

        private void ReceiveCompleted(object? sender, SocketAsyncEventArgs e)
        {
            ProcessReceive(e);
        }

        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                for (int i = 0; i < e.BytesTransferred ; i++)
                {
                    byte b = e.Buffer![i];
                    if(_rxflag == 0 && b == Constants.ASCII_STX ) 
                    {
                        _rxflag = 1;
                        _rxcount = 0;
                        _recvBuffer.Clear(); 
                    }
                    else if(_rxflag == 1 )
                    {
                        if( b == Constants.ASCII_ETX )
                        {
                            byte[] packet = _recvBuffer.Take(_rxcount).ToArray();
                            PacketReceived?.Invoke(this, packet);
                            _rxflag = 0x00;
                        }
                        else
                        {
                            _rxcount++;
                            _recvBuffer.Add(b);
                            if (_rxcount >= 150 )
                            {
                                _rxflag = 0;
                                _recvBuffer.Clear(); // 너무 긴 패킷 방어
                            }
                        }
                    }
                }
                StartReceive(); // 다음 수신 예약
            }
            else
            {
                Close();
            }
        }

        public void Close()
        {
            try { _socket.Shutdown(SocketShutdown.Both); } catch { }
            _socket.Close();

            CloseSocket?.Invoke(this);
        }
    }
}
