using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APSMain.BaseClass
{
    public class SerialDevice : IDisposable
    {
        protected SerialPort _port;
        public event EventHandler<byte[]>? MainDataReceived;

        private bool _isDisposed;
        private readonly object _disposeLock = new();
        public bool IsOpen => !_isDisposed && _port.IsOpen;

        public SerialDevice(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            _port = new SerialPort(portName, baudRate, parity, dataBits, stopBits)
            {
                Encoding = Encoding.ASCII,
                ReadTimeout = 1000,
                WriteTimeout = 1000
            };

            _port.DataReceived += OnDataReceived;
            Open();
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try {
                int count = _port.BytesToRead;
                if (count <= 0)
                    return;

                byte[] buffer = new byte[count];
                _port.Read(buffer, 0, count);
                MainDataReceived?.Invoke(this, buffer);
            }
            catch (Exception ex) {
                Console.WriteLine($"수신 오류: {ex.Message}");
            }
        }

        public bool Open()
        {
            try {
                if (!IsOpen)
                    _port.Open();
                return _port.IsOpen;
            }
            catch (Exception ex) {
                Console.WriteLine($"포트 열기 실패: {ex.Message}");
                return false;
            }
        }

        public void Dispose()
        {
            lock (_disposeLock) {
                if (_isDisposed)
                    return;

                _isDisposed = true;
                _port.DataReceived -= OnDataReceived;
                MainDataReceived = null;

                try {
                    if (_port.IsOpen)
                        _port.Close();
                }
                finally {
                    _port.Dispose();
                }
            }

            GC.SuppressFinalize(this);
        }

        public void Close()
        {
            if (_port == null) {
                return;
            }

            if (_port.IsOpen) {
                _port.Close();
            }
        }

        public void WriteRaw(byte[] data)
        {
            if (IsOpen)
                _port.Write(data, 0, data.Length);
        }

        public void WriteByte(byte[] data, int nLength)
        {
            if (IsOpen)
                _port.Write(data, 0, nLength);
        }

        public void WriteString(string cmd)
        {
            _port.Write(cmd);
        }

        public string? ReadLine()
        {
            try { return _port.ReadLine(); }
            catch { return null; }
        }

        public byte[] ReadBytes(int count)
        {
            var buffer = new byte[count];
            _port.Read(buffer, 0, count);
            return buffer;
        }
    }
}
