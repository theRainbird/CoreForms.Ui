#pragma warning disable CA1416 // libdbus is Linux-only, guarded by RuntimeInformation check
using System.Runtime.InteropServices;

namespace OldSchoolForms.Ui.Platform.DBus;

/// <summary>
/// Minimal P/Invoke wrapper around libdbus-1 for session bus communication.
/// </summary>
internal static class DBusNative
{
    private const string Lib = "libdbus-1.so.3";

    #region Types

    internal enum DBusMessageType : int
    {
        MethodCall = 1,
        MethodReturn = 2,
        Error = 3,
        Signal = 4
    }

    internal enum DBusBusType : int
    {
        Session = 0,
        System = 1
    }

    internal enum DBusHandlerResult : int
    {
        Handled = 0,
        NotYetHandled = 1,
        NeedMemory = 2
    }

    #endregion

    #region Error handling

    [StructLayout(LayoutKind.Sequential)]
    internal struct DBusError
    {
        public IntPtr name;
        public IntPtr message;
        private uint dummy;
        private IntPtr padding1;
    }

    [DllImport(Lib)]
    internal static extern void dbus_error_init(out DBusError error);

    [DllImport(Lib)]
    internal static extern void dbus_error_free(ref DBusError error);

    [DllImport(Lib)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool dbus_error_is_set(ref DBusError error);

    #endregion

    #region Connection

    [DllImport(Lib)]
    internal static extern IntPtr dbus_bus_get(DBusBusType busType, ref DBusError error);

    [DllImport(Lib)]
    internal static extern IntPtr dbus_connection_open(string address, ref DBusError error);

    [DllImport(Lib)]
    internal static extern void dbus_connection_ref(IntPtr connection);

    [DllImport(Lib)]
    internal static extern void dbus_connection_unref(IntPtr connection);

    [DllImport(Lib)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool dbus_connection_read_write_dispatch(IntPtr connection, int timeoutMs);

    [DllImport(Lib)]
    internal static extern IntPtr dbus_connection_pop_message(IntPtr connection);

    [DllImport(Lib)]
    internal static extern int dbus_bus_add_match(IntPtr connection, string rule, ref DBusError error);

    [DllImport(Lib)]
    internal static extern IntPtr dbus_connection_send_with_reply_and_block(IntPtr connection, IntPtr message, int timeoutMs, ref DBusError error);

    [DllImport(Lib)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool dbus_connection_send(IntPtr connection, IntPtr message, ref uint serial);

    [DllImport(Lib)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool dbus_connection_send_with_reply(IntPtr connection, IntPtr message, out IntPtr pendingReturn, int timeoutMs);

    [DllImport(Lib)]
    internal static extern void dbus_connection_flush(IntPtr connection);

    [DllImport(Lib)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool dbus_pending_call_get_completed(IntPtr pending);

    [DllImport(Lib)]
    internal static extern IntPtr dbus_pending_call_steal_reply(IntPtr pending);

    [DllImport(Lib)]
    internal static extern void dbus_pending_call_unref(IntPtr pending);

    #endregion

    #region Messages

    [DllImport(Lib)]
    internal static extern IntPtr dbus_message_new_method_call(string? busName, string? path, string? iface, string? method);

    [DllImport(Lib)]
    internal static extern IntPtr dbus_message_ref(IntPtr message);

    [DllImport(Lib)]
    internal static extern void dbus_message_unref(IntPtr message);

    [DllImport(Lib)]
    internal static extern DBusMessageType dbus_message_get_type(IntPtr message);

    [DllImport(Lib)]
    internal static extern IntPtr dbus_message_get_path(IntPtr message);

    [DllImport(Lib)]
    internal static extern IntPtr dbus_message_get_interface(IntPtr message);

    [DllImport(Lib)]
    internal static extern IntPtr dbus_message_get_member(IntPtr message);

    [DllImport(Lib)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool dbus_message_set_destination(IntPtr message, string destination);

    #endregion

    #region Message iterators

    [StructLayout(LayoutKind.Sequential)]
    internal struct DBusMessageIter
    {
        public IntPtr dummy1;
        public IntPtr dummy2;
        public uint dummy3;
        public int dummy4;
        public int dummy5;
        public int dummy6;
        public int dummy7;
        public int dummy8;
        public int dummy9;
        public int dummy10;
        public int dummy11;
        public int pad1;
        public int pad2;
        public IntPtr dummy12;
        public IntPtr dummy13;
    }

    [DllImport(Lib)]
    internal static extern void dbus_message_iter_init(IntPtr message, ref DBusMessageIter iter);

    [DllImport(Lib)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool dbus_message_iter_next(ref DBusMessageIter iter);

    [DllImport(Lib)]
    internal static extern int dbus_message_iter_get_arg_type(ref DBusMessageIter iter);

    [DllImport(Lib)]
    internal static extern void dbus_message_iter_get_basic(ref DBusMessageIter iter, out IntPtr value);

    [DllImport(Lib)]
    internal static extern void dbus_message_iter_get_basic(ref DBusMessageIter iter, out uint value);

    [DllImport(Lib)]
    internal static extern void dbus_message_iter_recurse(ref DBusMessageIter iter, out DBusMessageIter subIter);

    [DllImport(Lib)]
    internal static extern int dbus_message_iter_get_element_type(ref DBusMessageIter iter);

    [DllImport(Lib)]
    internal static extern void dbus_message_iter_init_append(IntPtr message, ref DBusMessageIter iter);

    [DllImport(Lib)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool dbus_message_iter_append_basic(ref DBusMessageIter iter, int type, ref IntPtr value);

    [DllImport(Lib)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool dbus_message_iter_append_basic(ref DBusMessageIter iter, int type, ref uint value);

    [DllImport(Lib)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool dbus_message_iter_append_basic(ref DBusMessageIter iter, int type, ref byte value);

    [DllImport(Lib)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool dbus_message_iter_open_container(ref DBusMessageIter iter, int type, string? containedSig, out DBusMessageIter subIter);

    [DllImport(Lib)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool dbus_message_iter_close_container(ref DBusMessageIter iter, ref DBusMessageIter subIter);

    #endregion

    #region Type codes

    internal const int DBUS_TYPE_INVALID = (int)'\0';
    internal const int DBUS_TYPE_STRING = (int)'s';
    internal const int DBUS_TYPE_OBJECT_PATH = (int)'o';
    internal const int DBUS_TYPE_SIGNATURE = (int)'g';
    internal const int DBUS_TYPE_BOOLEAN = (int)'b';
    internal const int DBUS_TYPE_UINT32 = (int)'u';
    internal const int DBUS_TYPE_ARRAY = (int)'a';
    internal const int DBUS_TYPE_VARIANT = (int)'v';
    internal const int DBUS_TYPE_STRUCT = (int)'r';
    internal const int DBUS_TYPE_DICT_ENTRY = (int)'e';
    internal const int DBUS_TYPE_BYTE = (int)'y';

    #endregion

    #region String helpers

    /// <summary>
    /// Converts an IntPtr returned by libdbus into a C# string.
    /// </summary>
    internal static string? PtrToString(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero) return null;
        return Marshal.PtrToStringUTF8(ptr);
    }

    #endregion
}
