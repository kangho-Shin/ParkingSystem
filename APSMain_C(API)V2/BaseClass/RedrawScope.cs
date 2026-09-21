using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace APSMain.BaseClass
{
    static class RedrawScope
    {
        private const int WM_SETREDRAW = 0x000B;

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        public static IDisposable Suspend(Control c) => new ScopeImpl(c);

        private sealed class ScopeImpl : IDisposable
        {
            private readonly Control _c;
            public ScopeImpl(Control c)
            {
                _c = c ?? throw new ArgumentNullException(nameof(c));
                if (_c.IsHandleCreated)
                    SendMessage(_c.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero); // redraw off
            }

            public void Dispose()
            {
                if (_c.IsHandleCreated) {
                    SendMessage(_c.Handle, WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);  // redraw on
                    _c.Invalidate(true);
                    _c.Update();
                }
            }
        }
    }
}
