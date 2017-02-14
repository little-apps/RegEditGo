using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace RegEditGo.Wnd
{
    public abstract class BaseWnd : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// Constructor that uses FindWindowEx to locate window with class name inside parent window
        /// </summary>
        /// <param name="parentWnd">Parent window to search in</param>
        /// <param name="className">Name of window class to search for</param>
        /// <exception cref="NullReferenceException">Thrown if unable to find window</exception>
        protected BaseWnd(IntPtr parentWnd, string className) : base(true)
        {
            SetHandle(Interop.FindWindowEx(parentWnd, IntPtr.Zero, className, null));
            
            if (handle == IntPtr.Zero)
                throw new NullReferenceException("Unable to get hwnd");
        }

        /// <summary>
        /// Constructor that takes in window handle
        /// </summary>
        /// <param name="wndHandle">Window handle</param>
        protected BaseWnd(IntPtr wndHandle) : base(true)
        {
            SetHandle(wndHandle);
        }

        /// <summary>
        /// Sends message to window
        /// </summary>
        /// <param name="msg">Message</param>
        /// <param name="wParam">First parameter</param>
        /// <param name="lParam">Second parameter</param>
        /// <returns></returns>
        public IntPtr SendMessage(uint msg, IntPtr wParam, IntPtr lParam)
        {
            return Interop.SendMessage(this, msg, wParam, lParam);
        }

        /// <summary>
        /// Posts message to window
        /// </summary>
        /// <param name="msg">Message</param>
        /// <param name="wParam">First parameter</param>
        /// <param name="lParam">Second parameter</param>
        /// <returns></returns>
        public int PostMessage(int msg, int wParam, int lParam)
        {
            return Interop.PostMessage(this, msg, wParam, lParam);
        }

        /// <summary>
        /// Sends WM_SETFOCUS to window
        /// </summary>
        /// <returns><see cref="IntPtr.Zero"/> if message was processed</returns>
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
