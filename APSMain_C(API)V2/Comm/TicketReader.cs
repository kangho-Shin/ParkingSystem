using APSMain.BaseClass;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APSMain.Comm
{
    public sealed class TicketReader : IDisposable
    {
        private readonly SerialDevice _device;
        private int _disposed;
        private readonly List<byte> _recvBuffer = new();
        public byte _rxState = Constants.ASCII_NONE;
        private int _rxCount = 0;
        public event Action<byte, byte[]>? OnPacketReceived;

        private byte[] _recvBuf = new byte[512];

        public TicketReader(string portName, int baudRate = 38400, Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One)
        {
            _device = new SerialDevice(portName, baudRate, parity, dataBits, stopBits);

            if (!_device.IsOpen) {
                _device.Dispose();
                throw new IOException($"RS-232 포트를 열 수 없습니다: {portName}");
            }

            _device.MainDataReceived += Device_DataReceived;
        }

        public void Close()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            _device.MainDataReceived -= Device_DataReceived;
            OnPacketReceived = null;

            _device.Dispose();
            GC.SuppressFinalize(this);
        }

        private string oldCmd = string.Empty;
        public void SendCommand(string payload)
        {
            if (!_device.IsOpen)
                return;

            int length = payload.Length;
            byte[] data = new byte[length + 4];

            data[0] = 0x02;
            Buffer.BlockCopy(Encoding.ASCII.GetBytes(payload), 0, data, 1, length);
            data[length + 1] = 0x03;
            Encoding.ASCII.GetBytes(payload);
            data[length + 2] = CalculateChecksum(data);
            data[length + 3] = 0x0d;

            _rxState = Constants.ASCII_NONE;
            _device.WriteRaw(data);
            if (!oldCmd.Equals(payload)) {
                //Console.WriteLine("TD TX : "+Encoding.ASCII.GetString(data));
                oldCmd = payload;
            }
        }

        private byte CalculateChecksum(byte[] buffer)
        {
            byte checksum = 0x00;
            for (int i = 1; i < buffer.Length - 2; i++) // 1부터 시작!
                checksum ^= buffer[i];
            return checksum;
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
                        else if (b == 0x06 || b == 0x15 || b == 0x10) {
                            _rxState = b;
                            OnPacketReceived?.Invoke(_rxState, _recvBuffer.ToArray()); // ACK/NAK/DLE 이벤트 전달
                        }
                        break;

                    case Constants.ASCII_STX:
                        if (b == 0x03) {
                            _recvBuffer.Add(0x00); // Null termination if needed
                            _rxState = Constants.ASCII_ETX;
                        }
                        else {
                            _recvBuffer.Add(b);
                            _rxCount++;
                        }
                        break;

                    case Constants.ASCII_ETX:
                        _rxState = Constants.ASCII_BCC; // BCC 단계로 이동
                        break;

                    case Constants.ASCII_BCC:
                        if (b == 0x0D) // CR 수신
                        {
                            _rxState = Constants.ASCII_DONE;

                            OnPacketReceived?.Invoke(_rxState, _recvBuffer.ToArray());
                        }
                        else {
                            _rxState = Constants.ASCII_NONE; // 실패로 리셋
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
