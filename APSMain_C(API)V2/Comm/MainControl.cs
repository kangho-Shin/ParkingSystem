using APSMain.BaseClass;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APSMain.Comm
{
    public class MainControl
    {
        private readonly SerialDevice _device;
        private readonly List<byte> _recvBuffer = new();
        public byte _rxState = Constants.ASCII_NONE;
        private int _rxCount = 0;
        public event Action<byte, byte[]>? OnPacketReceived;

        private byte[] _recvBuf = new byte[512];

        public MainControl(string portName, int baudRate = 19200, Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One)
        {
            _device = new SerialDevice(portName, baudRate, parity, dataBits, stopBits);

            _device.MainDataReceived += Device_DataReceived;
        }

        public void Close()
        {
            _device.Dispose();
        }


        public void WriteByte(byte[] data, int nLength)
        {
            _device.WriteByte(data, nLength);
        }

        private void Device_DataReceived(object? sender, byte[] data)
        {
            foreach (byte b in data) {
                switch (_rxState) {
                    case Constants.ASCII_NONE:
                        if (b == 0x02) {
                            _recvBuffer.Clear();
                            _rxCount = 0;
                            _rxState = Constants.ASCII_STX;
                        }
                        break;
                    case Constants.ASCII_STX:
                        if (b == 0x03) {
                            _recvBuffer.Add(0x00); // Null termination if needed
                            _rxState = Constants.ASCII_NONE;
                            OnPacketReceived?.Invoke(_rxState, _recvBuffer.ToArray());
                        }
                        else {
                            _recvBuffer.Add(b);
                            _rxCount++;
                        }
                        break;
                    default:
                        _rxState = Constants.ASCII_NONE;
                        break;
                }
            }
        }

        public bool IsOpen => _device.IsOpen;
    }
}
