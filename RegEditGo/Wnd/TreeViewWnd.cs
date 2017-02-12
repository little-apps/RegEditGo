using System;
using System.Runtime.InteropServices;

namespace RegEditGo.Wnd
{
    class TreeViewWnd : BaseWnd
    {
        private RegEditGo RegEditGo { get; }
        
        public TreeViewWnd(RegEditGo regEditGo) : base(regEditGo.RegEdit.DangerousGetHandle(), "SysTreeView32")
        {
            RegEditGo = regEditGo;
        }

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

        public IntPtr GetRootItem()
        {
            return SendMessage(Interop.TVM_GETNEXTITEM, (IntPtr) Interop.TVGN_ROOT, IntPtr.Zero);
        }

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
