using System;
using System.Runtime.InteropServices;

namespace RegEditGo.Wnd
{
    class TreeViewWnd : BaseWnd
    {
        private RegEditGo RegEditGo { get; }
        
        /// <summary>
        /// Constructor for TreeViewWnd
        /// </summary>
        /// <param name="regEditGo">RegEditGo instance with handle to main window</param>
        public TreeViewWnd(RegEditGo regEditGo) : base(regEditGo.RegEdit.DangerousGetHandle(), "SysTreeView32")
        {
            RegEditGo = regEditGo;
        }

        /// <summary>
        /// Gets text of treeview node at index
        /// </summary>
        /// <param name="item">Item index</param>
        /// <returns>Text or null if SendMessage failed</returns>
        /// <exception cref="SystemException">Thrown if unable to read/write from remote buffer</exception>
        public string GetTvItemTextEx(IntPtr item)
        {
            const int TVIF_TEXT = 0x0001;
            const int MAX_TVITEMTEXT = 512;

            // set address to remote buffer immediately following the tvItem
            var nRemoteBufferPtr = RegEditGo.RemoteBuffer.ToInt64() + Marshal.SizeOf(typeof(Interop.TVITEM));

            var tvi = new Interop.TVITEM
            {
                mask = TVIF_TEXT,
                hItem = item,
                cchTextMax = MAX_TVITEMTEXT,
                pszText = (IntPtr)nRemoteBufferPtr
            };

            // copy local tvItem to remote buffer
            var success = Interop.WriteProcessMemory(RegEditGo.ProcHandle, RegEditGo.RemoteBuffer, ref tvi,
                Marshal.SizeOf(typeof(Interop.TVITEM)), IntPtr.Zero);
            if (!success)
                throw new SystemException("Failed to write to process memory");

            if (SendMessage(Interop.TVM_GETITEMW, IntPtr.Zero, RegEditGo.RemoteBuffer) == IntPtr.Zero)
                // This can occur if the remote process is using an architecture thats incompatible with this process
                return null;

            // copy tvItem back into local buffer (copy whole buffer because we don't yet know how big the string is)
            success = Interop.ReadProcessMemory(RegEditGo.ProcHandle, RegEditGo.RemoteBuffer, RegEditGo.LocalBuffer,
                RegEditGo.BufferSize, IntPtr.Zero);
            if (!success)
                throw new SystemException("Failed to read from process memory");

            var nLocalBufferPtr = RegEditGo.LocalBuffer.ToInt64() + Marshal.SizeOf(typeof(Interop.TVITEM));

            return Marshal.PtrToStringUni((IntPtr)nLocalBufferPtr);
        }

        /// <summary>
        /// Gets root node in treeview
        /// </summary>
        /// <returns>Pointer to root node</returns>
        public IntPtr GetRootItem()
        {
            return SendMessage(Interop.TVM_GETNEXTITEM, (IntPtr) Interop.TVGN_ROOT, IntPtr.Zero);
        }

        /// <summary>
        /// Recurses through treeview to find node with text
        /// </summary>
        /// <param name="itemParent">Parent node to start search at</param>
        /// <param name="key">Text to look for</param>
        /// <returns>Pointer to treeview node</returns>
        /// <exception cref="SystemException">Thrown if unable to find node with text</exception>
        public IntPtr FindKey(IntPtr itemParent, string key)
        {
            var itemChild = SendMessage(Interop.TVM_GETNEXTITEM, (IntPtr) Interop.TVGN_CHILD, itemParent);

            while (itemChild != IntPtr.Zero)
            {
                var itemChildText = GetTvItemTextEx(itemChild);

                if (string.Compare(itemChildText, key, StringComparison.OrdinalIgnoreCase) == 0)
                    return itemChild;

                itemChild = SendMessage(Interop.TVM_GETNEXTITEM, (IntPtr) Interop.TVGN_NEXT, itemChild);
            }

            throw new SystemException($"TVM_GETNEXTITEM failed... key '{key}' not found!");
        }
    }
}
