using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RegEditGo
{
    class Program
    {
        public const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
        public const uint TOKEN_QUERY = 0x0008;
        public const int SE_PRIVILEGE_ENABLED = 0x00000002;
        public const int SE_PRIVILEGE_REMOVED = 0x00000004;

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out long lpLuid);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AdjustTokenPrivileges(IntPtr TokenHandle,
            [MarshalAs(UnmanagedType.Bool)] bool DisableAllPrivileges,
            ref TokenPrivileges NewState,
            UInt32 Zero,
            IntPtr Null1,
            IntPtr Null2);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hHandle);

        [StructLayout(LayoutKind.Sequential)]
        public struct TokenPrivileges
        {
            public int PrivilegeCount;
            public long Luid;
            public int Attributes;
        };

        
        private const string RegKey = "HKEY_LOCAL_MACHINE\\Software\\Microsoft";
        private const string ValueName = "";

        static void Main(string[] args)
        {
            if (!SetPrivilege("SeDebugPrivilege", true))
                throw new Exception("Unable to enable privilege");

            RegEditGo.GoTo(RegKey, ValueName);

            SetPrivilege("SeDebugPrivilege", false);

        }

        /// <summary>
        /// Enables or disables specified privilege
        /// </summary>
        /// <param name="privilege">Privilege name</param>
        /// <param name="enabled">If true, privilege is enabled. Otherwise, it is disabled.</param>
        /// <remarks>See https://msdn.microsoft.com/en-us/library/windows/desktop/bb530716(v=vs.85).aspx for a list of privilege strings</remarks>
        /// <returns>True if it was enabled or disabled</returns>
        private static bool SetPrivilege(string privilege, bool enabled)
        {
            try
            {
                var hproc = Process.GetCurrentProcess().Handle;
                var htok = IntPtr.Zero;

                if (!OpenProcessToken(hproc, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out htok))
                    return false;

                var tp = new TokenPrivileges
                {
                    PrivilegeCount = 1,
                    Luid = 0,
                    Attributes = enabled ? SE_PRIVILEGE_ENABLED : SE_PRIVILEGE_REMOVED
                };

                if (!LookupPrivilegeValue(null, privilege, out tp.Luid))
                    return false;

                var retVal = AdjustTokenPrivileges(htok, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);

                // Cleanup
                CloseHandle(htok);

                return retVal;
            }
            catch
            {
                return false;
            }
        }
    }
}
