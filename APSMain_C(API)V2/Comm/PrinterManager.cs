using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace APSMain.Comm
{
    public static class PrinterManager
    {
        private static Printer? _printer;
        public static Action<byte[]>? PrintRecEvent;
        // ESC/POS Commands

        public static readonly byte[] PaperCutMode = { 0x1D, 0x56, 0x01, 0x00 };
        public static readonly byte[] PaperFullCut = { 0x1D, 0x56, 0x00, 0x00 };
        public static readonly byte[] PaperHalfCut = { 0x1D, 0x56, 0x01, 0x00 };
        public static readonly byte[] WidthLarge   = { 0x1D, 0x21, 0x20, 0x00 };
        public static readonly byte[] HeightLarge  = { 0x1D, 0x21, 0x10, 0x00 };
        public static readonly byte[] WidthNormal  = { 0x1D, 0x21, 0x00, 0x00 };
        public static readonly byte[] StatusCheck  = { 0x1D, 0x72, 0x01, 0x00 };
        public static readonly byte[] SpaceWidth   = { 0x1D, 0x4C, 0x20, 0x00 };
        public static readonly byte[] LineFeed3    = { 0x1B, 0x64, 0x03 };


        public static void Init(string portName, int baudRate = 19200, Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One)
        {
            _printer = new Printer(portName, baudRate, parity, dataBits, stopBits);
            _printer.PrintRecEvent += OnPrintEventRecieved;    
            _printer.Initialize();
        }

        private static void OnPrintEventRecieved(byte[] obj)
        {
            PrintRecEvent?.Invoke(obj);
        }

        public static void PrintLine(string text) => _printer?.PrintLine(text);

        public static void PrintNormal() => _printer?.PrintNormal();

        public static void PrintBold(string text) => _printer?.PrintBold(text);

        public static void PrintCentered(string text,int width) => _printer?.PrintCentered(text,width);

        public static void PrintRaw(string text) => _printer?.PrintRaw(text);

        public static void LineFeed(int count = 3) => _printer?.LineFeed(count);

        public static void PrintBoldCenter(string text, int width = 48) => _printer?.PrintBoldCenter(text, width);
        public static void Cut(bool full = false) => _printer?.Cut(full);

        public static void CheckStatus() => _printer?.CheckStatus();

        public static void Close() => _printer?.Close();

    }
}
