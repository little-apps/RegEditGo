using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace RegEditGo.Wnd
{
    public abstract class BaseWnd : SafeHandleZeroOrMinusOneIsInvalid
    {

        protected BaseWnd(IntPtr parentWnd, string className) : base(true)
        {
            SetHandle(Interop.FindWindowEx(parentWnd, IntPtr.Zero, className, null));
            
            if (handle == IntPtr.Zero)
                throw new NullReferenceException("Unable to get hwnd");
        }

        protected BaseWnd(IntPtr wndHandle) : base(true)
        {
            SetHandle(wndHandle);
        }

        public IntPtr SendMessage(uint msg, IntPtr wParam, IntPtr lParam)
        {
            return Interop.SendMessage(handle, msg, wParam, lParam);
        }

        public int PostMessage(int msg, int wParam, int lParam)
        {
            return Interop.PostMessage(handle, msg, wParam, lParam);
        }

        public IntPtr SetFocus()
        {
            return SendMessage(Interop.WM_SETFOCUS, IntPtr.Zero, IntPtr.Zero);
        }

        protected override bool ReleaseHandle()
        {
            handle = (IntPtr) (-1);

            SetHandleAsInvalid();

            return true;
        }
    }
}
