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
        /// <summary>
        /// Use to get select the default value name for registry key
        /// </summary>
        public const string DefaultValueName = "(Default)";

        /// <summary>
        /// Buffer size for local and remote buffers
        /// </summary>
        internal static int BufferSize { get; } = 512;

        /// <summary>
        /// Handle created with OpenProcess on regedit
        /// </summary>
        internal IntPtr ProcHandle { get; private set; }
        /// <summary>
        /// Pointer to remote buffer in regedit
        /// </summary>
        internal IntPtr RemoteBuffer { get; private set; }
        /// <summary>
        /// Pointer to local buffer in this process
        /// </summary>
        internal IntPtr LocalBuffer { get; private set; }

        /// <summary>
        /// Instance for manipulating regedit window
        /// </summary>
        internal RegEditWnd RegEdit { get; private set; }
        /// <summary>
        /// Instance for manipulating treeview inside regedit window
        /// </summary>
        internal TreeViewWnd TreeView { get; private set; }
        /// <summary>
        /// Instance for manipulating listview inside regedit window
        /// </summary>
        internal ListViewWnd ListView { get; private set; }

        private readonly string _keyPath;
        private readonly string _valueName;

        /// <summary>
        /// Constructor for RegEditGo
        /// </summary>
        /// <param name="keyPath">Key to open</param>
        /// <param name="valueName">Value name to select. Use <see cref="DefaultValueName"/> to select default value name. If null/empty, no value name is selected.</param>
        /// <exception cref="ArgumentException">Thrown if keyPath is null, empty, or whitespace</exception>
        /// <exception cref="NullReferenceException">Thrown if unable to get <see cref="Process"/> for regedit.exe</exception>
        public RegEditGo(string keyPath, string valueName)
        {
            if (string.IsNullOrWhiteSpace(keyPath))
                throw new ArgumentException("Key path cannot be null, empty or whitespace", nameof(keyPath));

            _keyPath = keyPath;
            _valueName = valueName;

            // Checks if access is disabled to regedit, and adds access to it
            CheckAccess();

            var process = GetProcess();

            if (process == null)
                throw new NullReferenceException("Unable to get process");
            
            AllocateBuffers((uint)process.Id);
            GetWndInstances(process.MainWindowHandle);

            RegEdit.SetForegroundWindow();
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

        /// <summary>
        /// Navigates to given registry path and value
        /// </summary>
        public void GoTo()
        {
            OpenKey();

            Thread.Sleep(200);
            OpenValue();
        }

        /// <summary>
        /// Sends messages to regedit.exe to go to key in tree view
        /// </summary>
        private void OpenKey()
        {
            const int TVGN_CARET = 0x0009;

            TreeView.SetFocus();

            var tvItem = TryGetRootItem();

            foreach (var key in _keyPath.Split('\\').Where(key => key.Length != 0))
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

            if (!string.IsNullOrEmpty(_valueName))
                RegEdit.BringWindowToTop();
            else
                RegEdit.SendTabKey(false);
        }

        /// <summary>
        /// Attempts to get root item. If unable to, tries to restart regedit.exe
        /// </summary>
        /// <returns>Pointer to root node information</returns>
        /// <exception cref="SystemException">Thrown if unable to access regedit.exe</exception>
        private IntPtr TryGetRootItem()
        {
            var tvItem = TreeView.GetRootItem();

            var rootItemText = TreeView.GetTvItemTextEx(tvItem);

            if (!string.IsNullOrEmpty(rootItemText))
                return tvItem;

            // Process may be running in incompatible architecture
            FreeBuffers();

            // Close process
            foreach (var proc in Process.GetProcessesByName("regedit"))
            {
                proc.Kill();
                proc.WaitForExit();
            }

            // Spawn process
            var newProc = GetProcess();

            // Allocate local + remote buffer
            AllocateBuffers((uint)newProc.Id);

            // Get new instances of TreeViewWnd and ListViewWnd
            GetWndInstances(newProc.MainWindowHandle);

            tvItem = TreeView.GetRootItem();

            // Test root item text
            rootItemText = TreeView.GetTvItemTextEx(tvItem);

            // If still fails -> throw exception
            if (string.IsNullOrEmpty(rootItemText))
                throw new SystemException("Unable to access regedit.exe");

            return tvItem;
        }

        /// <summary>
        /// Sends messages to regedit.exe to go to value name in listview
        /// </summary>
        private void OpenValue()
        {
            ListView.SetFocus();

            if (string.IsNullOrEmpty(_valueName))
            {
                ListView.SetLvItemState(0);
                return;
            }

            var item = 0;
            while (true)
            {
                if (item == int.MaxValue)
                    return;

                var itemText = ListView.GetLvItemText(item);
                if (itemText == null)
                    return;

                if (string.Compare(itemText, _valueName, StringComparison.OrdinalIgnoreCase) == 0)
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

        /// <summary>
        /// Releases handles and frees buffers
        /// </summary>
        /// <param name="disposing">If true, free managed resources</param>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Free managed resources
                RegEdit.SetHandleAsInvalid();
                TreeView.SetHandleAsInvalid();
                ListView.SetHandleAsInvalid();
            }

            FreeBuffers();
        }
        
        /// <summary>
        /// Gets regedit process that's already running (or starts it if not)
        /// </summary>
        /// <returns><see cref="Process"/> instance for regedit.exe or null if unable to start it</returns>
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

        /// <summary>
        /// Gets <see cref="BaseWnd"/> instances for main regedit window, treeview, and listview
        /// </summary>
        /// <param name="mainWindowHandle">Main window handle</param>
        /// <exception cref="SystemException">Thrown if unable to get main window, treeview, or listview handle</exception>
        private void GetWndInstances(IntPtr mainWindowHandle)
        {
            try
            {
                RegEdit = new RegEditWnd(mainWindowHandle);
            }
            catch (NullReferenceException)
            {
                throw new SystemException("no app handle");
            }

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

        /// <summary>
        /// Allocates local buffer for this program (using Marshal.AllocHGlobals) and remote buffer for regedit.exe
        /// </summary>
        /// <param name="procId">Regedit.exe process ID</param>
        /// <exception cref="SystemException">Throw if unable to allocate local or remote buffer</exception>
        /// <exception cref="Win32Exception">Thrown if unable to open handle for regedit.exe</exception>
        private void AllocateBuffers(uint procId)
        {
            LocalBuffer = Marshal.AllocHGlobal(BufferSize);
            if (LocalBuffer == IntPtr.Zero)
                throw new SystemException("Failed to allocate memory in local process");

            ProcHandle = Interop.OpenProcess(Interop.PROCESS_ALL_ACCESS, false, procId);
            if (ProcHandle == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            // Allocate a buffer in the remote process
            RemoteBuffer = Interop.VirtualAllocEx(ProcHandle, IntPtr.Zero, BufferSize, Interop.MEM_COMMIT,
                Interop.PAGE_READWRITE);
            if (RemoteBuffer == IntPtr.Zero)
                throw new SystemException("Failed to allocate memory in remote process");
        }

        /// <summary>
        /// Frees local and remote buffers as well as process handle
        /// </summary>
        private void FreeBuffers()
        {
            if (LocalBuffer != IntPtr.Zero)
                Marshal.FreeHGlobal(LocalBuffer);

            LocalBuffer = IntPtr.Zero;

            // Clear remote buffer
            if (RemoteBuffer != IntPtr.Zero)
                Interop.VirtualFreeEx(ProcHandle, RemoteBuffer, BufferSize, Interop.MEM_RELEASE);

            RemoteBuffer = IntPtr.Zero;

            // Close process handle
            if (ProcHandle != IntPtr.Zero)
                Interop.CloseHandle(ProcHandle);

            ProcHandle = IntPtr.Zero;
        }

        /// <summary>
        /// Checks if regedit.exe access is disabled. If so, re-enables it.
        /// </summary>
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