using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace APSMain.BaseClass
{
    public static class MagnifierHost
    {
        // EXE 파일명/경로 지정
        private static readonly string ExePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "JPMagnifier.exe"); 

        private static Process? _proc;

        public static bool Start(string? args = null)
        {
            // 이미 떠 있으면 그냥 OK
            if (_proc != null && !_proc.HasExited)
                return true;

            // 같은 exe가 떠 있으면 그 프로세스를 잡아둠(다중 실행 방지)
            var alive = Process.GetProcesses().FirstOrDefault(p =>
            {
                try { return string.Equals(p.MainModule?.FileName, ExePath, StringComparison.OrdinalIgnoreCase); }
                catch { return false; }
            });
            if (alive != null) { _proc = alive; return true; }

            if (!File.Exists(ExePath))
                return false;

            var psi = new ProcessStartInfo(ExePath, args ?? "")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };
            _proc = Process.Start(psi);
            return _proc != null;
        }

        public static void Stop(int gracefulWaitMs = 800)
        {
            if (_proc == null || _proc.HasExited) {
                // 혹시 다른 인스턴스가 있으면 모두 종료 시도
                TryCloseByPath(ExePath, gracefulWaitMs);
                _proc?.Dispose();
                _proc = null;
                return;
            }

            // 1) 우아한 종료: 해당 PID의 최상위 창에 WM_CLOSE
            if (!PostCloseToAnyTopLevelWindow(_proc)) {
                // 창 핸들 못 찾으면 Kill 전에 경로 기반으로 다른 창도 시도
                TryCloseByPath(ExePath, gracefulWaitMs);
            }

            // 2) 기다렸다가 안 내려가면 Kill
            if (!_proc.WaitForExit(gracefulWaitMs))
                _proc.Kill(true);

            _proc.Dispose();
            _proc = null;
        }

        // ===== Win32 보조 =====
        private const uint WM_CLOSE = 0x0010;

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private static bool PostCloseToAnyTopLevelWindow(Process p)
        {
            IntPtr target = IntPtr.Zero;
            EnumWindows((h, _) =>
            {
                GetWindowThreadProcessId(h, out uint pid);
                if (pid == (uint)p.Id) { target = h; return false; } // 첫 번째 최상위 창
                return true;
            }, IntPtr.Zero);

            if (target == IntPtr.Zero)
                return false;
            PostMessage(target, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            return true;
        }

        private static void TryCloseByPath(string exePath, int waitMs)
        {
            var procs = Process.GetProcesses().Where(p =>
            {
                try { return string.Equals(p.MainModule?.FileName, exePath, StringComparison.OrdinalIgnoreCase); }
                catch { return false; }
            }).ToList();

            foreach (var p in procs) {
                try {
                    PostCloseToAnyTopLevelWindow(p);
                    if (!p.WaitForExit(waitMs))
                        p.Kill(true);
                }
                catch { /* ignore */ }
                finally { try { p.Dispose(); } catch { } }
            }
        }
    }
}
