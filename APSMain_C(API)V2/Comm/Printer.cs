using APSMain.BaseClass;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace APSMain.Comm
{
    public class Printer
    {
        private readonly SerialDevice _device;
        public Action<byte[]>? PrintRecEvent;

        public Printer(string portName, int baudRate = 19200, Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One)
        {
            _device = new SerialDevice(portName, baudRate, parity, dataBits, stopBits);
            _device.MainDataReceived += Device_DataReceived;
        }

        private void Device_DataReceived(object? sender, byte[] e)
        {
            PrintRecEvent?.Invoke(e);
        }

        public void Initialize()
        {
            _device.WriteRaw(PrinterManager.SpaceWidth);
            _device.WriteRaw(PrinterManager.HeightLarge[..3]);
        }

        public void PrintCentered(string text, int width = 48)
        {
            string line = text.PadLeft((width + text.Length) / 2);
            PrintLine(line);
        }

        public void PrintLine(string text)
        {
            string formatted = text + "\r\n";
            byte[] bytes = Encoding.GetEncoding("ks_c_5601").GetBytes(formatted);
            _device.WriteByte(bytes, bytes.Length);
        }

        public void PrintRaw(string text)
        {
            _device.WriteString(text);
        }

        public void PrintNormal()
        {
            _device.WriteRaw(PrinterManager.WidthNormal[..3]);
        }

        public void PrintBold(string text)
        {
            _device.WriteRaw(PrinterManager.WidthLarge[..3]);
            PrintLine(text);
            _device.WriteRaw(PrinterManager.WidthNormal[..3]);
        }

        public void PrintBoldCenter(string text, int width = 48)
        {
            _device.WriteRaw(PrinterManager.WidthLarge[..3]);
            string line = text.PadLeft((width + text.Length)/4);
            PrintLine(line);
            _device.WriteRaw(PrinterManager.WidthNormal[..3]);
        }

        public void LineFeed(int count = 3)
        {
            byte[] cmd = { 0x1B, 0x64, (byte)count };
            _device.WriteRaw(cmd);
        }

        public void Cut(bool full = false)
        {
            if (full)
                _device.WriteRaw(PrinterManager.PaperFullCut);
            else
                _device.WriteRaw(PrinterManager.PaperHalfCut);
        }

        public void CheckStatus()
        {
            _device.WriteRaw(PrinterManager.StatusCheck);
        }

        public void Close()
        {
            _device.Dispose();
        }
    }
}
