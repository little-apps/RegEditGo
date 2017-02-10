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
            return Interop.SetForegroundWindow(this);
        }

        public bool BringWindowToTop()
        {
            return Interop.BringWindowToTop(this);
        }

        public void SendTabKey(bool shiftPressed)
        {
            const int VK_TAB = 0x09;
            const int VK_SHIFT = 0x10;
            if (!shiftPressed)
            {
                PostMessage(Interop.WM_KEYDOWN, VK_TAB, 0x1f01);
                PostMessage(Interop.WM_KEYUP, VK_TAB, 0x1f01);
            }
            else
            {
                PostMessage(Interop.WM_KEYDOWN, VK_SHIFT, 0x1f01);
                PostMessage(Interop.WM_KEYDOWN, VK_TAB, 0x1f01);
                PostMessage(Interop.WM_KEYUP, VK_TAB, 0x1f01);
                PostMessage(Interop.WM_KEYUP, VK_SHIFT, 0x1f01);
            }
        }
    }
}
