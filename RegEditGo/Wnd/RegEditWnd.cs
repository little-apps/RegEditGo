using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegEditGo.Wnd
{
    class RegEditWnd : BaseWnd
    {
        public RegEditWnd(IntPtr wndHandle) : base(wndHandle)
        {
        }

        public bool SetForegroundWindow()
        {
            return Interop.SetForegroundWindow(WndHandle);
        }

        public bool BringWindowToTop()
        {
            return Interop.BringWindowToTop(WndHandle);
        }
    }
}
