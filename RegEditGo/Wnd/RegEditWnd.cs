using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegEditGo.Wnd
{
    class RegEditWnd : BaseWnd
    {
        /// <summary>
        /// Constructor for RegEditWnd
        /// </summary>
        /// <param name="wndHandle">Main window handle for regedit</param>
        public RegEditWnd(IntPtr wndHandle) : base(wndHandle)
        {
        }

        /// <summary>
        /// Sets window as foreground
        /// </summary>
        /// <returns>True if it was brought to foreground</returns>
        public bool SetForegroundWindow()
        {
            return Interop.SetForegroundWindow(this);
        }

        /// <summary>
        /// Brings window to top
        /// </summary>
        /// <returns>True if function succeeded</returns>
        public bool BringWindowToTop()
        {
            return Interop.BringWindowToTop(this);
        }

        /// <summary>
        /// Sends tab key press (along with shift, if specified)
        /// </summary>
        /// <param name="shiftPressed">Imitates shift held down while tab is pressed</param>
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
