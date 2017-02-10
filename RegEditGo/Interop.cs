using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RegEditGo
{
    internal static class Interop
    {
        internal const uint PROCESS_ALL_ACCESS = (uint)(0x000F0000L | 0x00100000L | 0xFFF);
        internal const uint MEM_COMMIT = 0x1000;
        internal const uint MEM_RELEASE = 0x8000;
        internal const uint PAGE_READWRITE = 0x04;

        internal const int WM_SETFOCUS = 0x0007;
        internal const int WM_KEYDOWN = 0x0100;
        internal const int WM_KEYUP = 0x0101;

        internal const int TVM_GETNEXTITEM = 0x1100 + 10;
        internal const int TVM_SELECTITEM = 0x1100 + 11;
        internal const int TVM_GETITEMW = 0x1100 + 62;

        internal const int TVGN_ROOT = 0x0000;
        internal const int TVGN_NEXT = 0x0001;
        internal const int TVGN_CHILD = 0x0004;
        internal const int TVGN_FIRSTVISIBLE = 0x0005;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        internal static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass,
            string lpszWindow);

        [DllImport("kernel32", SetLastError = true)]
        internal static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32")]
        internal static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, int dwSize,
            uint flAllocationType, uint flProtect);

        [DllImport("kernel32")]
        internal static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, uint dwFreeType);

        [DllImport("kernel32")]
        internal static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, ref LVITEM buffer,
            int dwSize, IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32")]
        internal static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, ref TVITEM buffer,
            int dwSize, IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32")]
        internal static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer,
            int dwSize, IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32")]
        internal static extern bool CloseHandle(IntPtr hObject);

        [DllImport("user32.dll")]
        internal static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        internal static extern int PostMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        internal static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

        #region structs

        /// <summary>
        ///     from 'http://dotnetjunkies.com/WebLog/chris.taylor/'
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct LVITEM
        {
            public uint mask;
            public int iItem;
            public int iSubItem;
            public uint state;
            public uint stateMask;
            public IntPtr pszText;
            public int cchTextMax;
            public readonly int iImage;
        }

        /// <summary>
        ///     from '.\PlatformSDK\Include\commctrl.h'
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct TVITEM
        {
            public uint mask;
            public IntPtr hItem;
            public uint state;
            public uint stateMask;
            public IntPtr pszText;
            public int cchTextMax;
            public uint iImage;
            public uint iSelectedImage;
            public uint cChildren;
            public IntPtr lParam;
        }

        #endregion structs
    }
}
