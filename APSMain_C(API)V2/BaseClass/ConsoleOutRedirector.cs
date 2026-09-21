using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace APSMain.BaseClass
{
    public class ConsoleManager : IDisposable
    {
        private nint? stdHandle = null;
        private SafeFileHandle? safeHandle = null;
        private FileStream? fileStream = null;
        private StreamWriter? writer = null;
        private bool isConsoleAllocated = false;

        internal const uint SWP_NOZORDER = 0x0004;
        internal const uint SWP_SHOWWINDOW = 0x0040;

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool SetWindowPos(
            nint hWnd,
            nint hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags);

        public void Open(int posX = 100, int posY = 100, int width = 900, int height = 600)
        {
            if (isConsoleAllocated)
                return;

            if (!NativeMethods.AllocConsole())
                throw new InvalidOperationException("Console allocation failed");

            stdHandle = NativeMethods.CreateFile(
                "CONOUT$",
                NativeMethods.GENERIC_WRITE,
                NativeMethods.FILE_SHARE_WRITE,
                0, NativeMethods.OPEN_EXISTING, 0, 0);

            safeHandle = new SafeFileHandle((nint)stdHandle, true);
            fileStream = new FileStream(safeHandle, FileAccess.Write);

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding encoding = Encoding.GetEncoding(949); // EUC-KR

            writer = new StreamWriter(fileStream, encoding) { AutoFlush = true };
            Console.SetOut(writer);

            isConsoleAllocated = true;

            MoveConsoleWindow(posX, posY, width, height);
        }

        private void MoveConsoleWindow(int posX, int posY, int width, int height)
        {
            nint hWnd = nint.Zero;

            for (int i = 0; i < 20; i++) {
                hWnd = NativeMethods.GetConsoleWindow();
                if (hWnd != nint.Zero)
                    break;

                Thread.Sleep(50);
            }

            if (hWnd != nint.Zero) {
                SetWindowPos(
                    hWnd,
                    nint.Zero,
                    posX,
                    posY,
                    width,
                    height,
                    SWP_NOZORDER | SWP_SHOWWINDOW);
            }
        }

        public void Close()
        {
            if (!isConsoleAllocated)
                return;

            Console.SetOut(TextWriter.Null);
            writer?.Close();
            fileStream?.Close();

            try
            {
                if (stdHandle is nint ptr && ptr != nint.Zero)
                    NativeMethods.CloseHandle(ptr);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
            }
            nint hWnd = NativeMethods.GetConsoleWindow();

            // 콘솔 분리
            NativeMethods.FreeConsole();
            isConsoleAllocated = false;

            // 창 강제 닫기 (윈도우 메시지 루프 없는 콘솔에 이게 확실히 먹히진 않음)
            if (hWnd != nint.Zero)
            {
                Thread.Sleep(100); // 타이밍 보장
                NativeMethods.ShowWindow(hWnd, 0); // SW_HIDE
            }
        }

        public void Dispose()
        {
            Close();
        }

        private static class NativeMethods
        {
            internal const uint GENERIC_WRITE = 0x40000000;
            internal const uint FILE_SHARE_WRITE = 0x2;
            internal const uint OPEN_EXISTING = 0x3;

            [DllImport("kernel32.dll", SetLastError = true)]
            internal static extern bool AllocConsole();

            [DllImport("kernel32.dll", SetLastError = true)]
            internal static extern bool FreeConsole();

            [DllImport("kernel32.dll", SetLastError = true)]
            internal static extern nint CreateFile(
                string lpFileName,
                uint dwDesiredAccess,
                uint dwShareMode,
                uint lpSecurityAttributes,
                uint dwCreationDisposition,
                uint dwFlagsAndAttributes,
                uint hTemplateFile);

            [DllImport("kernel32.dll")]
            internal static extern nint GetConsoleWindow();

            [DllImport("user32.dll")]
            internal static extern bool ShowWindow(nint hWnd, int nCmdShow); // 0 = SW_HIDE
            [DllImport("kernel32.dll")]
            internal static extern bool CloseHandle(nint hObject);
        }
    }
}
