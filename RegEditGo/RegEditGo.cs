using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;

namespace RegEditGo
{
    public class RegEditGo : IDisposable
    {
        private static int BufferSize { get; } = 512;

        private readonly IntPtr _wndApp;
        private readonly IntPtr _wndTreeView;
        private readonly IntPtr _wndListView;

        private readonly IntPtr _hProcess;
        private IntPtr _lpRemoteBuffer;
        private IntPtr _lpLocalBuffer;

        private RegEditGo()
        {
            // Checks if access is disabled to regedit, and adds access to it
            CheckAccess();

            var process = GetProcess();

            if (process == null)
                throw new NullReferenceException("Unable to get process");

            _wndApp = process.MainWindowHandle;
            var processId = (uint)process.Id;

            if (_wndApp == IntPtr.Zero)
            {
                ShowErrorMessage(new SystemException("no app handle"));
            }

            Interop.SetForegroundWindow(_wndApp);

            // get handle to treeview
            _wndTreeView = Interop.FindWindowEx(_wndApp, IntPtr.Zero, "SysTreeView32", null);
            if (_wndTreeView == IntPtr.Zero)
            {
                ShowErrorMessage(new SystemException("no treeview"));
            }

            // get handle to listview
            _wndListView = Interop.FindWindowEx(_wndApp, IntPtr.Zero, "SysListView32", null);
            if (_wndListView == IntPtr.Zero)
            {
                ShowErrorMessage(new SystemException("no listview"));
            }

            // allocate buffer in local process
            _lpLocalBuffer = Marshal.AllocHGlobal(BufferSize);
            if (_lpLocalBuffer == IntPtr.Zero)
                ShowErrorMessage(new SystemException("Failed to allocate memory in local process"));

            _hProcess = Interop.OpenProcess(Interop.PROCESS_ALL_ACCESS, false, processId);
            if (_hProcess == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error());
            //ShowErrorMessage(new ApplicationException("Failed to access process"));

            // Allocate a buffer in the remote process
            _lpRemoteBuffer = Interop.VirtualAllocEx(_hProcess, IntPtr.Zero, BufferSize, Interop.MEM_COMMIT,
                Interop.PAGE_READWRITE);
            if (_lpRemoteBuffer == IntPtr.Zero)
                ShowErrorMessage(new SystemException("Failed to allocate memory in remote process"));
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

            Interop.SendMessage(_wndTreeView, Interop.WM_SETFOCUS, IntPtr.Zero, IntPtr.Zero);

            var tvItem = Interop.SendMessage(_wndTreeView, Interop.TVM_GETNEXTITEM, (IntPtr)Interop.TVGN_ROOT,
                IntPtr.Zero);

            foreach (var key in path.Split('\\').Where(key => key.Length != 0))
            {
                tvItem = FindKey(tvItem, key);
                if (tvItem == IntPtr.Zero)
                    return;

                Interop.SendMessage(_wndTreeView, Interop.TVM_SELECTITEM, (IntPtr)TVGN_CARET, tvItem);

                // expand tree node
                const int VK_RIGHT = 0x27;
                Interop.SendMessage(_wndTreeView, Interop.WM_KEYDOWN, (IntPtr)VK_RIGHT, IntPtr.Zero);
                Interop.SendMessage(_wndTreeView, Interop.WM_KEYUP, (IntPtr)VK_RIGHT, IntPtr.Zero);
            }

            Interop.SendMessage(_wndTreeView, Interop.TVM_SELECTITEM, (IntPtr)TVGN_CARET, tvItem);

            if (select)
                Interop.BringWindowToTop(_wndApp);
            else
                SendTabKey(false);
        }

        private void OpenValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return;

            Interop.SendMessage(_wndListView, Interop.WM_SETFOCUS, IntPtr.Zero, IntPtr.Zero);

            if (value.Length == 0)
            {
                SetLvItemState(0);
                return;
            }

            var item = 0;
            for (;;)
            {
                var itemText = GetLvItemText(item);
                if (itemText == null)
                    return;

                if (string.Compare(itemText, value, StringComparison.OrdinalIgnoreCase) == 0)
                    break;

                item++;
            }

            SetLvItemState(item);

            const int LVM_FIRST = 0x1000;
            const int LVM_ENSUREVISIBLE = LVM_FIRST + 19;
            Interop.SendMessage(_wndListView, LVM_ENSUREVISIBLE, (IntPtr)item, IntPtr.Zero);

            Interop.BringWindowToTop(_wndApp);

            SendTabKey(false);
            SendTabKey(true);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
            }

            if (_lpLocalBuffer != IntPtr.Zero)
                Marshal.FreeHGlobal(_lpLocalBuffer);
            if (_lpRemoteBuffer != IntPtr.Zero)
                Interop.VirtualFreeEx(_hProcess, _lpRemoteBuffer, 0, Interop.MEM_RELEASE);
            if (_hProcess != IntPtr.Zero)
                Interop.CloseHandle(_hProcess);
        }

        

        private void SendTabKey(bool shiftPressed)
        {
            const int VK_TAB = 0x09;
            const int VK_SHIFT = 0x10;
            if (!shiftPressed)
            {
                Interop.PostMessage(_wndApp, Interop.WM_KEYDOWN, VK_TAB, 0x1f01);
                Interop.PostMessage(_wndApp, Interop.WM_KEYUP, VK_TAB, 0x1f01);
            }
            else
            {
                Interop.PostMessage(_wndApp, Interop.WM_KEYDOWN, VK_SHIFT, 0x1f01);
                Interop.PostMessage(_wndApp, Interop.WM_KEYDOWN, VK_TAB, 0x1f01);
                Interop.PostMessage(_wndApp, Interop.WM_KEYUP, VK_TAB, 0x1f01);
                Interop.PostMessage(_wndApp, Interop.WM_KEYUP, VK_SHIFT, 0x1f01);
            }
        }

        private string GetTvItemTextEx(IntPtr wndTreeView, IntPtr item)
        {
            const int TVIF_TEXT = 0x0001;
            const int MAX_TVITEMTEXT = 512;

            // set address to remote buffer immediately following the tvItem
            var nRemoteBufferPtr = _lpRemoteBuffer.ToInt64() + Marshal.SizeOf(typeof(Interop.TVITEM));

            var tvi = new Interop.TVITEM
            {
                mask = TVIF_TEXT,
                hItem = item,
                cchTextMax = MAX_TVITEMTEXT,
                pszText = (IntPtr)nRemoteBufferPtr
            };

            // copy local tvItem to remote buffer
            var success = Interop.WriteProcessMemory(_hProcess, _lpRemoteBuffer, ref tvi,
                Marshal.SizeOf(typeof(Interop.TVITEM)), IntPtr.Zero);
            if (!success)
                ShowErrorMessage(new SystemException("Failed to write to process memory"));

            Interop.SendMessage(wndTreeView, Interop.TVM_GETITEMW, IntPtr.Zero, _lpRemoteBuffer);

            // copy tvItem back into local buffer (copy whole buffer because we don't yet know how big the string is)
            success = Interop.ReadProcessMemory(_hProcess, _lpRemoteBuffer, _lpLocalBuffer, BufferSize, IntPtr.Zero);
            if (!success)
                ShowErrorMessage(new SystemException("Failed to read from process memory"));

            var nLocalBufferPtr = _lpLocalBuffer.ToInt64() + Marshal.SizeOf(typeof(Interop.TVITEM));

            return Marshal.PtrToStringUni((IntPtr)nLocalBufferPtr);
        }

        private IntPtr FindKey(IntPtr itemParent, string key)
        {
            var itemChild = Interop.SendMessage(_wndTreeView, Interop.TVM_GETNEXTITEM, (IntPtr) Interop.TVGN_CHILD,
                itemParent);

            while (itemChild != IntPtr.Zero)
            {
                var itemChildText = GetTvItemTextEx(_wndTreeView, itemChild);

                if (string.Compare(itemChildText, key, StringComparison.OrdinalIgnoreCase) == 0)
                    return itemChild;

                itemChild = Interop.SendMessage(_wndTreeView, Interop.TVM_GETNEXTITEM, (IntPtr)Interop.TVGN_NEXT,
                    itemChild);
            }
            ShowErrorMessage(new SystemException($"TVM_GETNEXTITEM failed... key '{key}' not found!"));
            return IntPtr.Zero;
        }

        private void SetLvItemState(int item)
        {
            const int LVM_FIRST = 0x1000;
            const int LVM_SETITEMSTATE = LVM_FIRST + 43;
            const int LVIF_STATE = 0x0008;

            const int LVIS_FOCUSED = 0x0001;
            const int LVIS_SELECTED = 0x0002;

            var lvItem = new Interop.LVITEM
            {
                mask = LVIF_STATE,
                iItem = item,
                iSubItem = 0,
                state = LVIS_FOCUSED | LVIS_SELECTED,
                stateMask = LVIS_FOCUSED | LVIS_SELECTED
            };

            // copy local lvItem to remote buffer
            var success = Interop.WriteProcessMemory(_hProcess, _lpRemoteBuffer, ref lvItem,
                Marshal.SizeOf(typeof(Interop.LVITEM)), IntPtr.Zero);
            if (!success)
                ShowErrorMessage(new SystemException("Failed to write to process memory"));

            // Send the message to the remote window with the address of the remote buffer
            if (Interop.SendMessage(_wndListView, LVM_SETITEMSTATE, (IntPtr)item, _lpRemoteBuffer) == IntPtr.Zero)
                ShowErrorMessage(new SystemException("LVM_GETITEM Failed "));
        }

        private string GetLvItemText(int item)
        {
            const int LVM_GETITEM = 0x1005;
            const int LVIF_TEXT = 0x0001;

            // set address to remote buffer immediately following the lvItem
            var nRemoteBufferPtr = _lpRemoteBuffer.ToInt64() + Marshal.SizeOf(typeof(Interop.TVITEM));

            var lvItem = new Interop.LVITEM
            {
                mask = LVIF_TEXT,
                iItem = item,
                iSubItem = 0,
                pszText = (IntPtr)nRemoteBufferPtr,
                cchTextMax = 50
            };

            // copy local lvItem to remote buffer
            var success = Interop.WriteProcessMemory(_hProcess, _lpRemoteBuffer, ref lvItem,
                Marshal.SizeOf(typeof(Interop.LVITEM)), IntPtr.Zero);
            if (!success)
                ShowErrorMessage(new SystemException("Failed to write to process memory"));

            // Send the message to the remote window with the address of the remote buffer
            if (Interop.SendMessage(_wndListView, LVM_GETITEM, IntPtr.Zero, _lpRemoteBuffer) == IntPtr.Zero)
                return null;

            // copy lvItem back into local buffer (copy whole buffer because we don't yet know how big the string is)
            success = Interop.ReadProcessMemory(_hProcess, _lpRemoteBuffer, _lpLocalBuffer, BufferSize, IntPtr.Zero);
            if (!success)
                ShowErrorMessage(new SystemException("Failed to read from process memory"));

            var localBufferPtr = _lpLocalBuffer.ToInt64() + Marshal.SizeOf(typeof(Interop.TVITEM));
            return Marshal.PtrToStringAnsi((IntPtr)localBufferPtr);
        }

        private Process GetProcess()
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

        private static void ShowErrorMessage(Exception ex)
        {
#if (DEBUG)
            throw ex;
#endif
        }
    }
}