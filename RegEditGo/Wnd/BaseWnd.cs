using System;

namespace RegEditGo.Wnd
{
    public abstract class BaseWnd
    {
        public IntPtr WndHandle { get; protected set; }

        protected BaseWnd(IntPtr parentWnd, string className)
        {
            WndHandle = Interop.FindWindowEx(parentWnd, IntPtr.Zero, className, null);;

            if (WndHandle == IntPtr.Zero)
                throw new NullReferenceException("Unable to get hwnd");
        }

        protected BaseWnd(IntPtr wndHandle)
        {
            WndHandle = wndHandle;
        }

        public IntPtr SendMessage(uint msg, IntPtr wParam, IntPtr lParam)
        {
            return Interop.SendMessage(WndHandle, msg, wParam, lParam);
        }
    }
}
