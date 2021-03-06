﻿using System;
using System.Runtime.InteropServices;

namespace RegEditGo.Wnd
{
    class ListViewWnd : BaseWnd
    {
        private RegEditGo RegEditGo { get; }

        /// <summary>
        /// Constructor for ListViewWnd
        /// </summary>
        /// <param name="regEditGo">RegEditGo instance with handle to main window</param>
        public ListViewWnd(RegEditGo regEditGo) : base(regEditGo.RegEdit.DangerousGetHandle(), "SysListView32")
        {
            RegEditGo = regEditGo;
        }

        /// <summary>
        /// Focuses and selects item in listview
        /// </summary>
        /// <param name="item">Item index</param>
        /// <exception cref="SystemException">Thrown if there was an error communicating with regedit.exe</exception>
        public void SetLvItemState(int item)
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
            var success = Interop.WriteProcessMemory(RegEditGo.ProcHandle, RegEditGo.RemoteBuffer, ref lvItem,
                Marshal.SizeOf(typeof(Interop.LVITEM)), IntPtr.Zero);
            if (!success)
                throw new SystemException("Failed to write to process memory");

            // Send the message to the remote window with the address of the remote buffer
            if (SendMessage(LVM_SETITEMSTATE, (IntPtr) item, RegEditGo.RemoteBuffer) == IntPtr.Zero)
                throw new SystemException("LVM_GETITEM Failed ");
        }

        /// <summary>
        /// Gets text in first column at specifed index in listview
        /// </summary>
        /// <param name="item">Index in listview</param>
        /// <param name="column">Column index to get text from</param>
        /// <returns>Text or null if SendMessage failed</returns>
        /// <exception cref="SystemException">Thrown if unable to read/write from remote buffer</exception>
        public string GetLvItemText(int item, int column = 0)
        {
            const int LVM_GETITEM = 0x1005;
            const int LVIF_TEXT = 0x0001;

            // set address to remote buffer immediately following the lvItem
            var nRemoteBufferPtr = RegEditGo.RemoteBuffer.ToInt64() + Marshal.SizeOf(typeof(Interop.TVITEM));

            var lvItem = new Interop.LVITEM
            {
                mask = LVIF_TEXT,
                iItem = item,
                iSubItem = column,
                pszText = (IntPtr)nRemoteBufferPtr,
                cchTextMax = 50
            };

            // copy local lvItem to remote buffer
            var success = Interop.WriteProcessMemory(RegEditGo.ProcHandle, RegEditGo.RemoteBuffer, ref lvItem,
                Marshal.SizeOf(typeof(Interop.LVITEM)), IntPtr.Zero);
            if (!success)
                throw new SystemException("Failed to write to process memory");

            // Send the message to the remote window with the address of the remote buffer
            if (SendMessage(LVM_GETITEM, IntPtr.Zero, RegEditGo.RemoteBuffer) == IntPtr.Zero)
                return null;

            // copy lvItem back into local buffer (copy whole buffer because we don't yet know how big the string is)
            success = Interop.ReadProcessMemory(RegEditGo.ProcHandle, RegEditGo.RemoteBuffer, RegEditGo.LocalBuffer,
                RegEditGo.BufferSize, IntPtr.Zero);
            if (!success)
                throw new SystemException("Failed to read from process memory");

            var localBufferPtr = RegEditGo.LocalBuffer.ToInt64() + Marshal.SizeOf(typeof(Interop.TVITEM));
            return Marshal.PtrToStringAnsi((IntPtr)localBufferPtr);
        }
    }
}
