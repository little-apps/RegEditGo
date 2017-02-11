using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;
using RegEditGo.Wnd;

namespace RegEditGo
{
    public class RegEditGo : IDisposable
    {
        internal static int BufferSize { get; } = 512;
        
        //internal readonly IntPtr MainWnd;

        internal readonly IntPtr ProcHandle;
        internal readonly IntPtr RemoteBuffer;
        internal readonly IntPtr LocalBuffer;

        internal readonly RegEditWnd RegEdit;
        internal readonly TreeViewWnd TreeView;
        internal readonly ListViewWnd ListView;

        private RegEditGo()
        {
            // Checks if access is disabled to regedit, and adds access to it
            CheckAccess();

            var process = GetProcess();

            if (process == null)
                throw new NullReferenceException("Unable to get process");

            try
            {
                RegEdit = new RegEditWnd(process.MainWindowHandle);
            }
            catch (NullReferenceException)
            {
                throw new SystemException("no app handle");
            }
            
            var processId = (uint)process.Id;

            RegEdit.SetForegroundWindow();

            // allocate buffer in local process
            LocalBuffer = Marshal.AllocHGlobal(BufferSize);
            if (LocalBuffer == IntPtr.Zero)
                throw new SystemException("Failed to allocate memory in local process");

            ProcHandle = Interop.OpenProcess(Interop.PROCESS_ALL_ACCESS, false, processId);
            if (ProcHandle == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error());
            //ShowErrorMessage(new ApplicationException("Failed to access process"));

            // Allocate a buffer in the remote process
            RemoteBuffer = Interop.VirtualAllocEx(ProcHandle, IntPtr.Zero, BufferSize, Interop.MEM_COMMIT,
                Interop.PAGE_READWRITE);
            if (RemoteBuffer == IntPtr.Zero)
                throw new SystemException("Failed to allocate memory in remote process");

            try
            {
                TreeView = new TreeViewWnd(this);
            }
            catch (NullReferenceException)
            {
                throw new SystemException("Unable to locate treeview");
            }

            try
            {
                ListView = new ListViewWnd(this);
            }
            catch (NullReferenceException)
            {
                throw new SystemException("Unable to locate listview");
            }
        }
        
        ~RegEditGo()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        public void Close()
        {
            Dispose();
        }
        
        /// <summary>
        ///     Opens RegEdit.exe and navigates to given registry path and value
        /// </summary>
        /// <param name="keyPath">path of registry key</param>
        /// <param name="valueName">name of registry value (can be null)</param>
        public static void GoTo(string keyPath, string valueName)
        {
            using (var locator = new RegEditGo())
            {
                var hasValue = !string.IsNullOrEmpty(valueName);
                locator.OpenKey(keyPath, hasValue);

                if (!hasValue)
                    return;

                Thread.Sleep(200);
                locator.OpenValue(valueName);
            }
        }

        private void OpenKey(string path, bool select)
        {
            if (string.IsNullOrEmpty(path)) return;

            const int TVGN_CARET = 0x0009;

            TreeView.SetFocus();
                
            var tvItem = TreeView.GetRootItem();

            foreach (var key in path.Split('\\').Where(key => key.Length != 0))
            {
                tvItem = TreeView.FindKey(tvItem, key);
                if (tvItem == IntPtr.Zero)
                    return;

                TreeView.SendMessage(Interop.TVM_SELECTITEM, (IntPtr)TVGN_CARET, tvItem);

                // expand tree node
                const int VK_RIGHT = 0x27;
                TreeView.SendMessage(Interop.WM_KEYDOWN, (IntPtr)VK_RIGHT, IntPtr.Zero);
                TreeView.SendMessage(Interop.WM_KEYUP, (IntPtr)VK_RIGHT, IntPtr.Zero);
            }

            TreeView.SendMessage(Interop.TVM_SELECTITEM, (IntPtr)TVGN_CARET, tvItem);

            if (select)
                RegEdit.BringWindowToTop();
            else
                RegEdit.SendTabKey(false);
        }

        private void OpenValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return;

            ListView.SetFocus();

            if (value.Length == 0)
            {
                ListView.SetLvItemState(0);
                return;
            }

            var item = 0;
            for (;;)
            {
                var itemText = ListView.GetLvItemText(item);
                if (itemText == null)
                    return;

                if (string.Compare(itemText, value, StringComparison.OrdinalIgnoreCase) == 0)
                    break;

                item++;
            }

            ListView.SetLvItemState(item);

            const int LVM_FIRST = 0x1000;
            const int LVM_ENSUREVISIBLE = LVM_FIRST + 19;
            ListView.SendMessage(LVM_ENSUREVISIBLE, (IntPtr)item, IntPtr.Zero);

            RegEdit.BringWindowToTop();

            RegEdit.SendTabKey(false);
            RegEdit.SendTabKey(true);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
            }

            if (LocalBuffer != IntPtr.Zero)
                Marshal.FreeHGlobal(LocalBuffer);
            if (RemoteBuffer != IntPtr.Zero)
                Interop.VirtualFreeEx(ProcHandle, RemoteBuffer, 0, Interop.MEM_RELEASE);
            if (ProcHandle != IntPtr.Zero)
                Interop.CloseHandle(ProcHandle);
        }
        
        private static Process GetProcess()
        {
            Process proc;

            var processes = Process.GetProcessesByName("RegEdit");
            if (processes.Length == 0)
            {
                try
                {
                    proc = Process.Start("RegEdit.exe");

                    proc?.WaitForInputIdle();
                }
                catch (Exception)
                {
                    return null;
                }
            }
            else
            {
                proc = processes[0];
            }

            return proc;
        }

        private static void CheckAccess()
        {
            using (
                var regKey =
                    Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", true))
            {
                var n = regKey?.GetValue("DisableRegistryTools") as int?;

                // Value doesnt exists
                if (n == null)
                    return;

                // User has access
                if (n.Value == 0)
                    return;

                // Value is either 1 or 2 which means we cant access regedit.exe

                // So, lets enable access
                regKey.SetValue("DisableRegistryTools", 0, RegistryValueKind.DWord);
            }
        }
    }
}