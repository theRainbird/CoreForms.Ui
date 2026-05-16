using System.Runtime.InteropServices;
using System.Text;
using CoreForms.Ui.Core;
using static CoreForms.Ui.Platform.DBus.DBusNative;

namespace CoreForms.Ui.Platform.DBus;

/// <summary>
/// XDG Desktop Portal FileChooser integration via libdbus-1.
/// Uses org.freedesktop.portal.FileChooser on the session bus.
/// </summary>
public static class PortalFileChooser
{
    private const string PortalBus = "org.freedesktop.portal.Desktop";
    private const string PortalPath = "/org/freedesktop/portal/desktop";
    private const string FileChooserIface = "org.freedesktop.portal.FileChooser";
    private const string RequestIface = "org.freedesktop.portal.Request";

    /// <summary>
    /// Shows an Open File dialog.
    /// </summary>
    public static DialogResult ShowOpenFile(
        string? parentHandle, string title,
        string? initialDirectory, string fileName,
        string filter, int filterIndex, bool multiSelect,
        out string[] selectedFiles)
    {
        return ShowFileDialog(
            parentHandle, title, initialDirectory, fileName,
            filter, filterIndex, multiSelect, isSave: false,
            createPrompt: false, overwritePrompt: false,
            out selectedFiles);
    }

    /// <summary>
    /// Shows a Save File dialog.
    /// </summary>
    public static DialogResult ShowSaveFile(
        string? parentHandle, string title,
        string? initialDirectory, string fileName,
        string filter, int filterIndex,
        bool createPrompt, bool overwritePrompt,
        out string[] selectedFiles)
    {
        return ShowFileDialog(
            parentHandle, title, initialDirectory, fileName,
            filter, filterIndex, multiSelect: false, isSave: true,
            createPrompt, overwritePrompt, out selectedFiles);
    }

    private static DialogResult ShowFileDialog(
        string? parentHandle, string title,
        string? initialDirectory, string fileName,
        string filter, int filterIndex, bool multiSelect,
        bool isSave, bool createPrompt, bool overwritePrompt,
        out string[] selectedFiles)
    {
        selectedFiles = [];

        using var conn = DBusConnection.Connect();

        uint token = (uint)(Environment.TickCount & 0x7FFFFFFF);
        string handleToken = $"coreforms{token}";
        string requestPath = $"/org/freedesktop/portal/desktop/request/CoreForms/{token}";
        string matchRule = $"type='signal',interface='{RequestIface}',path='{requestPath}'";
        conn.AddMatch(matchRule);

        var msg = new DBusMessage(
            dbus_message_new_method_call(PortalBus, PortalPath, FileChooserIface,
                isSave ? "SaveFile" : "OpenFile"));
        if (msg.Handle == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create portal method call");

        try
        {
            msg.Destination = PortalBus;
            msg.BodyBuilder = (ref DBusMessageIter iter) =>
            {
                var parentStr = Marshal.StringToCoTaskMemUTF8(parentHandle ?? "");
                dbus_message_iter_append_basic(ref iter, DBUS_TYPE_STRING, ref parentStr);
                Marshal.FreeCoTaskMem(parentStr);

                var titleStr = Marshal.StringToCoTaskMemUTF8(title);
                dbus_message_iter_append_basic(ref iter, DBUS_TYPE_STRING, ref titleStr);
                Marshal.FreeCoTaskMem(titleStr);

                AppendOptionsDict(ref iter, parentHandle, filter, filterIndex,
                    initialDirectory, fileName, handleToken, multiSelect,
                    isSave, overwritePrompt);
            };

            var reply = conn.SendAndWaitForReply(msg, timeoutMs: 10000,
                pumpEvents: () => CoreForms.Ui.Platform.Platform.ProcessEvents(Application.Instance));

            string? responsePath = null;
            if (reply.BodyValues != null && reply.BodyValues.Length > 0)
                responsePath = reply.BodyValues[0] as string;

            Console.WriteLine($"[Portal] Response path: {responsePath}");

            if (responsePath == null)
                return DialogResult.Cancel;

            if (responsePath != requestPath)
            {
                string actualMatch = $"type='signal',interface='{RequestIface}',path='{responsePath}'";
                conn.AddMatch(actualMatch);
            }

            var signal = conn.WaitForSignal(RequestIface, "Response", timeoutMs: 180000,
                pumpEvents: () => CoreForms.Ui.Platform.Platform.ProcessEvents(Application.Instance));

            if (signal == null)
            {
                Console.WriteLine("[Portal] No response signal received");
                return DialogResult.Cancel;
            }

            Console.WriteLine($"[Portal] Response signal received: type={signal.Type}");

            if (signal.BodyValues != null && signal.BodyValues.Length >= 2)
            {
                uint responseCode = signal.BodyValues[0] is uint u ? u : 0;
                var results = signal.BodyValues[1] as Dictionary<string, object?>;

                Console.WriteLine($"[Portal] Response code: {responseCode}");

                if (responseCode == 0 && results != null)
                {
                    if (results.TryGetValue("uris", out var urisObj) && urisObj is string[] uris)
                    {
                        selectedFiles = ConvertUrisToPaths(uris);
                        return selectedFiles.Length > 0 ? DialogResult.OK : DialogResult.Cancel;
                    }

                    if (results.TryGetValue("uri", out var uriObj) && uriObj is string singleUri)
                    {
                        selectedFiles = ConvertUrisToPaths([singleUri]);
                        return DialogResult.OK;
                    }
                }

                return responseCode switch
                {
                    0 => DialogResult.OK,
                    _ => DialogResult.Cancel
                };
            }

            return DialogResult.Cancel;
        }
        finally
        {
            msg.Dispose();
        }
    }

    private static void AppendOptionsDict(
        ref DBusMessageIter iter,
        string? parentHandle,
        string filter, int filterIndex,
        string? initialDirectory, string fileName,
        string handleToken, bool multiSelect,
        bool isSave, bool overwritePrompt)
    {
        if (!dbus_message_iter_open_container(ref iter, DBUS_TYPE_ARRAY, "{sv}", out DBusMessageIter dictIter))
            return;

        AppendDictEntry(ref dictIter, "handle_token", handleToken);
        AppendFilters(ref dictIter, filter, filterIndex);

        if (!string.IsNullOrEmpty(initialDirectory))
            AppendDictEntry(ref dictIter, "current_folder", initialDirectory);

        if (!string.IsNullOrEmpty(fileName))
            AppendDictEntry(ref dictIter, "current_name", fileName);

        if (isSave)
            AppendDictEntry(ref dictIter, "overwrite", overwritePrompt);

        if (multiSelect)
            AppendDictEntry(ref dictIter, "multiple", true);

        if (!dbus_message_iter_close_container(ref iter, ref dictIter))
            Console.WriteLine("[Portal] Failed to close dict container");
    }

    private static void AppendDictEntry(ref DBusMessageIter dictIter, string key, string value)
    {
        if (!dbus_message_iter_open_container(ref dictIter, DBUS_TYPE_DICT_ENTRY, null, out DBusMessageIter entryIter))
            return;

        var keyStr = Marshal.StringToCoTaskMemUTF8(key);
        try { dbus_message_iter_append_basic(ref entryIter, DBUS_TYPE_STRING, ref keyStr); }
        finally { Marshal.FreeCoTaskMem(keyStr); }

        if (!dbus_message_iter_open_container(ref entryIter, DBUS_TYPE_VARIANT, "s", out DBusMessageIter varIter))
        {
            dbus_message_iter_close_container(ref dictIter, ref entryIter);
            return;
        }

        var valStr = Marshal.StringToCoTaskMemUTF8(value);
        try { dbus_message_iter_append_basic(ref varIter, DBUS_TYPE_STRING, ref valStr); }
        finally { Marshal.FreeCoTaskMem(valStr); }

        dbus_message_iter_close_container(ref entryIter, ref varIter);
        dbus_message_iter_close_container(ref dictIter, ref entryIter);
    }

    private static void AppendDictEntry(ref DBusMessageIter dictIter, string key, bool value)
    {
        if (!dbus_message_iter_open_container(ref dictIter, DBUS_TYPE_DICT_ENTRY, null, out DBusMessageIter entryIter))
            return;

        var keyStr = Marshal.StringToCoTaskMemUTF8(key);
        try { dbus_message_iter_append_basic(ref entryIter, DBUS_TYPE_STRING, ref keyStr); }
        finally { Marshal.FreeCoTaskMem(keyStr); }

        if (!dbus_message_iter_open_container(ref entryIter, DBUS_TYPE_VARIANT, "b", out DBusMessageIter varIter))
        {
            dbus_message_iter_close_container(ref dictIter, ref entryIter);
            return;
        }

        uint bVal = value ? 1u : 0u;
        dbus_message_iter_append_basic(ref varIter, DBUS_TYPE_BOOLEAN, ref bVal);

        dbus_message_iter_close_container(ref entryIter, ref varIter);
        dbus_message_iter_close_container(ref dictIter, ref entryIter);
    }

    private static void AppendDictEntry(ref DBusMessageIter dictIter, string key, uint value)
    {
        if (!dbus_message_iter_open_container(ref dictIter, DBUS_TYPE_DICT_ENTRY, null, out DBusMessageIter entryIter))
            return;

        var keyStr = Marshal.StringToCoTaskMemUTF8(key);
        try { dbus_message_iter_append_basic(ref entryIter, DBUS_TYPE_STRING, ref keyStr); }
        finally { Marshal.FreeCoTaskMem(keyStr); }

        if (!dbus_message_iter_open_container(ref entryIter, DBUS_TYPE_VARIANT, "u", out DBusMessageIter varIter))
        {
            dbus_message_iter_close_container(ref dictIter, ref entryIter);
            return;
        }

        dbus_message_iter_append_basic(ref varIter, DBUS_TYPE_UINT32, ref value);

        dbus_message_iter_close_container(ref entryIter, ref varIter);
        dbus_message_iter_close_container(ref dictIter, ref entryIter);
    }

    private static void AppendFilters(ref DBusMessageIter dictIter, string filter, int filterIndex)
    {
        if (string.IsNullOrEmpty(filter))
            return;

        var parsed = ParseFilter(filter);
        if (parsed.Count == 0)
            return;

        if (!dbus_message_iter_open_container(ref dictIter, DBUS_TYPE_DICT_ENTRY, null, out DBusMessageIter entryIter))
            return;

        var keyStr = Marshal.StringToCoTaskMemUTF8("filters");
        dbus_message_iter_append_basic(ref entryIter, DBUS_TYPE_STRING, ref keyStr);
        Marshal.FreeCoTaskMem(keyStr);

        if (!dbus_message_iter_open_container(ref entryIter, DBUS_TYPE_VARIANT, "a(sa(us))", out DBusMessageIter varIter))
            return;

        if (!dbus_message_iter_open_container(ref varIter, DBUS_TYPE_ARRAY, "(sa(us))", out DBusMessageIter arrayIter))
            return;

        foreach (var (name, patterns) in parsed)
        {
            if (!dbus_message_iter_open_container(ref arrayIter, DBUS_TYPE_STRUCT, null, out DBusMessageIter structIter))
                continue;

            var nameStr = Marshal.StringToCoTaskMemUTF8(name);
            dbus_message_iter_append_basic(ref structIter, DBUS_TYPE_STRING, ref nameStr);
            Marshal.FreeCoTaskMem(nameStr);

            if (!dbus_message_iter_open_container(ref structIter, DBUS_TYPE_ARRAY, "(us)", out DBusMessageIter rulesIter))
                continue;

            foreach (var pat in patterns)
            {
                if (!dbus_message_iter_open_container(ref rulesIter, DBUS_TYPE_STRUCT, null, out DBusMessageIter ruleStruct))
                    continue;

                uint type = 0;
                dbus_message_iter_append_basic(ref ruleStruct, DBUS_TYPE_UINT32, ref type);

                var patStr = Marshal.StringToCoTaskMemUTF8(pat);
                dbus_message_iter_append_basic(ref ruleStruct, DBUS_TYPE_STRING, ref patStr);
                Marshal.FreeCoTaskMem(patStr);

                dbus_message_iter_close_container(ref rulesIter, ref ruleStruct);
            }

            dbus_message_iter_close_container(ref structIter, ref rulesIter);
            dbus_message_iter_close_container(ref arrayIter, ref structIter);
        }

        dbus_message_iter_close_container(ref varIter, ref arrayIter);
        dbus_message_iter_close_container(ref entryIter, ref varIter);
        dbus_message_iter_close_container(ref dictIter, ref entryIter);
    }

    /// <summary>
    /// Returns the parent window handle string formatted for the portal.
    /// </summary>
    public static string GetParentHandleString(Form? form)
    {
        if (form == null)
            return string.Empty;

        nint nativeHandle = CoreForms.Ui.Platform.Platform.GetNativeWindowHandle(form);
        if (nativeHandle == nint.Zero)
            return string.Empty;

        string sessionType = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE") ?? "x11";

        return sessionType switch
        {
            "wayland" => $"wayland:wl_surface@{nativeHandle:x}",
            _ => $"x11:{(ulong)nativeHandle:x}"
        };
    }

    private static List<(string Name, List<string> Patterns)> ParseFilter(string filter)
    {
        var result = new List<(string Name, List<string> Patterns)>();
        if (string.IsNullOrEmpty(filter))
            return result;

        string[] parts = filter.Split('|');
        for (int i = 0; i + 1 < parts.Length; i += 2)
        {
            string name = parts[i].Trim();
            string patternStr = parts[i + 1].Trim();
            var patterns = new List<string>(
                patternStr.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            if (patterns.Count == 0)
                patterns.Add("*.*");
            result.Add((name, patterns));
        }

        if (parts.Length % 2 == 1)
        {
            result.Add((string.Empty, [parts[^1].Trim()]));
        }

        return result;
    }

    private static string[] ConvertUrisToPaths(string[] uris)
    {
        var paths = new List<string>();
        foreach (var uriStr in uris)
        {
            if (Uri.TryCreate(uriStr, UriKind.Absolute, out var uri) && uri.IsFile)
                paths.Add(uri.LocalPath);
            else
                paths.Add(uriStr);
        }
        return paths.ToArray();
    }
}
