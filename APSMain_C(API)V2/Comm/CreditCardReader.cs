using APSMain.BaseClass;
using System.Diagnostics.Eventing.Reader;
using System.IO.Ports;
using System.Text;

namespace APSMain.Comm
{
    public class CreditCardReader
    {
        private readonly SerialDevice _device;

        private enum RxState { None, TermId, DateTime, JobCode, ResCode, LenLow, LenHigh, Body, ETX, Done }
        private RxState _state = RxState.None;

        private byte[] _bufArr = new byte[2048];   // 배킹 버퍼(필요 시 확장)
        private Memory<byte> _buffer;              // 뷰(주소+길이 역할)
        private int _bufCount = 0;                 // 현재 누적 길이

        private byte[] _rxBuff = new byte[1024];
        public  int _rxCount = 0;
        public int _bodyLength = 0;

        private void BufferClear() => _bufCount = 0;
        private int BufferCount => _bufCount;

        public ClsLog? XLogClass;

        // 원본 패킷 이벤트 (필요하면 사용)
        public event Action<byte,byte[]?,int>? OnRawPacketReceived;

        byte[] XTestData = { 
            0x02, 0x70, 0x63, 0x75, 0x63, 0x30, 0x30, 0x30, 0x31, 0x32, 0x6D, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x32, 0x30, 0x32, 0x35, 0x31, 
            0x31, 0x31, 0x30, 0x31, 0x36, 0x32, 0x38, 0x30, 0x33, 0x67, 0x00, 0xBB, 0x00, 0x31, 0x31, 0x30, 0x30, 0x30, 0x30, 0x34, 0x31, 0x30, 
            0x31, 0x32, 0x30, 0x32, 0x30, 0x2A, 0x2A, 0x2A, 0x2A, 0x2A, 0x2A, 0x2A, 0x2A, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x31, 0x30, 
            0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x36, 0x32, 0x32, 
            0x35, 0x36, 0x35, 0x35, 0x36, 0x20, 0x20, 0x20, 0x20, 0x32, 0x30, 0x32, 0x35, 0x31, 0x31, 0x31, 0x30, 0x31, 0x36, 0x32, 0x38, 0x30, 
            0x33, 0x32, 0x35, 0x31, 0x31, 0x31, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x32, 0x37, 0x32, 0x36, 0x39, 0x32, 0x31, 0x38, 0x30, 0x33, 
            0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x70, 0x63, 0x75, 0x63, 0x30, 0x30, 0x30, 0x31, 0x32, 0x6D, 0x30, 0x30, 0x30, 0x32, 0x30, 0x34, 
            0x30, 0x30, 0xBA, 0xF1, 0xBE, 0xBE, 0xC4, 0xAB, 0xB5, 0xE5, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x30, 0x34, 0x30, 0x30, 
            0x42, 0x43, 0xC4, 0xAB, 0xB5, 0xE5, 0xBB, 0xE7, 0x00, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x70, 0x63, 0x75, 0x63, 0x30, 0x30, 
            0x30, 0x31, 0x32, 0x6D, 0x30, 0x31, 0x30, 0x33, 0x32, 0x35, 0x31, 0x31, 0x31, 0x30, 0x31, 0x36, 0x32, 0x38, 0x30, 0x33, 0x38, 0x32, 
            0x35, 0x31, 0x03, 0x86
        };

        public void XDataCall(int inmoney)
        {
            string smoney = $"{inmoney:D10}";
            string sdate = DateTime.Now.ToString("yyyyMMdd");
            string stime = DateTime.Now.ToString("HHmmss");

            Buffer.BlockCopy(Encoding.GetEncoding("ks_c_5601-1987").GetBytes(smoney), 0, XTestData, 22+35, 10);
            Buffer.BlockCopy(Encoding.GetEncoding("ks_c_5601-1987").GetBytes(sdate),  0, XTestData, 62+35, 8);
            Buffer.BlockCopy(Encoding.GetEncoding("ks_c_5601-1987").GetBytes(stime),  0, XTestData, 70+35, 6);
            foreach (byte b in XTestData) {
                switch (_state) {
                    case RxState.None:
                        if (b == Constants.ASCII_STX) {
                            BufferClear();               // _buffer.Clear();
                            BufferAdd(b);                // BufferAdd(b);
                            _rxCount = 0;
                            _state = RxState.TermId;
                        }
                        else if (b == Constants.ASCII_ACK) {
                            _state = RxState.None;
                            BufferClear();
                            OnRawPacketReceived?.Invoke(Constants.ASCII_ACK, _rxBuff, 0);
                        }
                        else if (b == Constants.ASCII_NAK) {
                            _state = RxState.None;
                            BufferClear();
                            OnRawPacketReceived?.Invoke(Constants.ASCII_NAK, _rxBuff, 0);
                        }
                        break;
                    case RxState.TermId:
                        BufferAdd(b);
                        if (++_rxCount == 16) {
                            _rxCount = 0;
                            _state = RxState.DateTime;
                        }
                        break;

                    case RxState.DateTime:
                        BufferAdd(b);
                        if (++_rxCount == 14)
                            _state = RxState.JobCode;
                        break;

                    case RxState.JobCode:
                        BufferAdd(b);
                        _state = RxState.ResCode;
                        break;

                    case RxState.ResCode:
                        BufferAdd(b);
                        _state = RxState.LenLow;
                        break;

                    case RxState.LenLow:
                        BufferAdd(b);
                        _bodyLength = b;
                        _state = RxState.LenHigh;
                        break;

                    case RxState.LenHigh:
                        BufferAdd(b);
                        _bodyLength |= b << 8;
                        _rxCount = 0;

                        if (_bodyLength == 0)
                            _state = RxState.ETX;
                        else
                            _state = RxState.Body;

                        break;
                    case RxState.Body:
                        BufferAdd(b);
                        _rxCount++;
                        if (_rxCount >= _bodyLength)
                            _state = RxState.ETX;

                        break;

                    case RxState.ETX:
                        BufferAdd(b);
                        if (b != Constants.ASCII_ETX) {
                            _state = RxState.None;
                            _rxCount = 0;
                            _bodyLength = 0;
                            BufferClear();

                            OnRawPacketReceived?.Invoke(Constants.ASCII_NAK, null, 0);
                            break;
                        }
                        _state = RxState.Done;
                        break;

                    case RxState.Done:
                        BufferAdd(b);

                        byte[] packet = new byte[BufferCount];
                        Buffer.BlockCopy(_bufArr, 0, packet, 0, BufferCount);

                        string hex = BitConverter.ToString(packet).Replace("-", ",");

                        string text = Encoding.GetEncoding("ks_c_5601-1987")
                            .GetString(packet)
                            .Replace("\0", "")
                            .Replace("\x02", "[STX]")
                            .Replace("\x03", "[ETX]")
                            .Replace("\x06", "[ACK]")
                            .Replace("\x15", "[NACK]");

                        if (XLogClass != null) {
                            XLogClass.SaveLogString("SMT", "RECV TEXT : " + text);
                        }

                        OnRawPacketReceived?.Invoke(0x88, packet, BufferCount);
                        _state = RxState.None;
                        _rxCount = 0;
                        _bodyLength = 0;
                        BufferClear();
                        break;
                }
            }
        }

        public CreditCardReader(string portName, int baudRate = 115200, Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One)
        {
            _device = new SerialDevice(portName, baudRate, parity, dataBits, stopBits);
            _device.MainDataReceived += OnDataReceived;

            _buffer = _bufArr;   // Memory<byte> 초기화

            XLogClass = ClsLog.Instance;
        }

        public void Close()
        {
            _device.Dispose();
        }

        private void BufferAdd(byte value)
        {
            EnsureCapacity(_bufCount + 1);
            _buffer.Span[_bufCount++] = value;
        }

        private void EnsureCapacity(int need)
        {
            if (need <= _bufArr.Length)
                return;
            int newCap = Math.Max(need, _bufArr.Length * 2);
            var newArr = new byte[newCap];
            Buffer.BlockCopy(_bufArr, 0, newArr, 0, _bufCount);
            _bufArr = newArr;
            _buffer = _bufArr; // Memory<byte> 갱신
        }

        private void OnDataReceived(object? sender, byte[] data)
        {
            ///XLogClass?.SaveLogString("SM", "RAW RECV : " + BitConverter.ToString(data).Replace("-", ","));
            foreach (byte b in data)
            {
                switch (_state)
                {
                    case RxState.None:
                        if (b == Constants.ASCII_STX)
                        {
                            BufferClear();               // _buffer.Clear();
                            BufferAdd(b);                // BufferAdd(b);
                            _rxCount = 0;
                            _state = RxState.TermId;
                        }
                        else if (b == Constants.ASCII_ACK)
                        {
                            _state = RxState.None;
                            BufferClear();
                            OnRawPacketReceived?.Invoke(Constants.ASCII_ACK, _rxBuff,0);
                        }
                        else if(b == Constants.ASCII_NAK)
                        {
                            _state = RxState.None;
                            BufferClear();
                            OnRawPacketReceived?.Invoke(Constants.ASCII_NAK, _rxBuff,0);
                        }
                        break;
                    case RxState.TermId:
                        BufferAdd(b);
                        if (++_rxCount == 16)
                        {
                            _rxCount = 0;
                            _state = RxState.DateTime;
                        }
                        break;

                    case RxState.DateTime:
                        BufferAdd(b);
                        if (++_rxCount == 14)
                            _state = RxState.JobCode;
                        break;

                    case RxState.JobCode:
                        BufferAdd(b);
                        _state = RxState.ResCode;
                        break;

                    case RxState.ResCode:
                        BufferAdd(b);
                        _state = RxState.LenLow;
                        break;

                    case RxState.LenLow:
                        BufferAdd(b);
                        _bodyLength = b;
                        _state = RxState.LenHigh;
                        break;

                    case RxState.LenHigh:
                        BufferAdd(b);
                        _bodyLength |= b << 8;
                        _rxCount = 0;

                        if (_bodyLength == 0)
                            _state = RxState.ETX;
                        else
                            _state = RxState.Body;

                        break;
                    case RxState.Body:
                        BufferAdd(b);
                        _rxCount++;
                        if (_rxCount >= _bodyLength)
                            _state = RxState.ETX;

                        break;

                    case RxState.ETX:
                        BufferAdd(b);
                        if (b != Constants.ASCII_ETX) {
                            _state = RxState.None;
                            _rxCount = 0;
                            _bodyLength = 0;
                            BufferClear();

                            OnRawPacketReceived?.Invoke(Constants.ASCII_NAK, null, 0);
                            break;
                        }
                        _state = RxState.Done;
                        break;

                    case RxState.Done:
                        BufferAdd(b);

                        byte[] packet = new byte[BufferCount];
                        Buffer.BlockCopy(_bufArr, 0, packet, 0, BufferCount);

                        string hex = BitConverter.ToString(packet).Replace("-", ",");

                        string text = Encoding.GetEncoding("ks_c_5601-1987")
                            .GetString(packet)
                            .Replace("\0", "")
                            .Replace("\x02", "[STX]")
                            .Replace("\x03", "[ETX]")
                            .Replace("\x06", "[ACK]")
                            .Replace("\x15", "[NACK]");

                        if (XLogClass != null) {
                            XLogClass.SaveLogString("SMT", "RECV TEXT : " + text);
                        }

                        OnRawPacketReceived?.Invoke(0x88, packet, BufferCount);
                        _state = RxState.None;
                        _rxCount = 0;
                        _bodyLength = 0;
                        BufferClear();
                        break;
                }
            }
        }

        public void WriteByte(byte[] data, int nLength)
        {
            if (_device.IsOpen) {
                byte[] sendData = data.Take(nLength).ToArray();

                string hex = BitConverter.ToString(sendData).Replace("-", ",");

                string text = Encoding.GetEncoding("ks_c_5601-1987")
                    .GetString(sendData)
                    .Replace("\0", "")
                    .Replace("\x02", "[STX]")
                    .Replace("\x03", "[ETX]")
                    .Replace("\x06", "[ACK]")
                    .Replace("\x15", "[NACK]");

                if (XLogClass != null) {
                    XLogClass.SaveLogString("SMT", "SEND TEXT : " + text);
                }

                _device.WriteRaw(sendData);
            }
        }

        public void ResetRxState()
        {
            _state = RxState.None;
            _rxCount = 0;
            _bodyLength = 0;
            BufferClear();
        }

        public bool IsOpen => _device.IsOpen;
        public void SendRaw(byte[] data) => _device.WriteRaw(data);
    }
}
