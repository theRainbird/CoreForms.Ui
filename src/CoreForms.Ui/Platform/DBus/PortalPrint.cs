using System.Runtime.InteropServices;
using CoreForms.Ui.Core;
using static CoreForms.Ui.Platform.DBus.DBusNative;

namespace CoreForms.Ui.Platform.DBus;

/// <summary>
/// XDG Desktop Portal Print integration via libdbus-1.
/// Uses org.freedesktop.portal.Print on the session bus.
/// </summary>
internal static class PortalPrint
{
    private const string PortalBus = "org.freedesktop.portal.Desktop";
    private const string PortalPath = "/org/freedesktop/portal/desktop";
    private const string PrintIface = "org.freedesktop.portal.Print";
    private const string RequestIface = "org.freedesktop.portal.Request";

    /// <summary>
    /// Shows the native print dialog via PreparePrint and returns user settings.
    /// </summary>
    public static DialogResult PreparePrint(
        string? parentHandle, string title,
        PrinterSettings? settings, PageSettings? pageSettings,
        out PrinterSettings resultSettings,
        out PageSettings? resultPageSettings)
    {
        resultSettings = new PrinterSettings();
        resultPageSettings = null;

        using var conn = DBusConnection.Connect();

        uint token = (uint)(Environment.TickCount & 0x7FFFFFFF);
        string handleToken = $"coreforms{token}";
        string requestPath = $"/org/freedesktop/portal/desktop/request/CoreForms/{token}";
        string matchRule = $"type='signal',interface='{RequestIface}',path='{requestPath}'";
        conn.AddMatch(matchRule);

        var msg = new DBusMessage(
            dbus_message_new_method_call(PortalBus, PortalPath, PrintIface, "PreparePrint"));
        if (msg.Handle == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create portal method call");

        try
        {
            msg.Destination = PortalBus;
            msg.BodyBuilder = (ref DBusMessageIter iter) =>
            {
                // 1. parent_window (s)
                var parentStr = Marshal.StringToCoTaskMemUTF8(parentHandle ?? "");
                dbus_message_iter_append_basic(ref iter, DBUS_TYPE_STRING, ref parentStr);
                Marshal.FreeCoTaskMem(parentStr);

                // 2. title (s)
                var titleStr = Marshal.StringToCoTaskMemUTF8(title);
                dbus_message_iter_append_basic(ref iter, DBUS_TYPE_STRING, ref titleStr);
                Marshal.FreeCoTaskMem(titleStr);

                // 3. settings (a{sv})
                AppendEmptyDict(ref iter);

                // 4. page_setup (a{sv})
                AppendEmptyDict(ref iter);

                // 5. options (a{sv}) with handle_token
                if (!dbus_message_iter_open_container(ref iter, DBUS_TYPE_ARRAY, "{sv}", out DBusMessageIter optIter))
                    return;
                AppendDictEntryString(ref optIter, "handle_token", handleToken);
                dbus_message_iter_close_container(ref iter, ref optIter);
            };

            var reply = conn.SendAndWaitForReply(msg, timeoutMs: 10000,
                pumpEvents: () => CoreForms.Ui.Platform.Platform.ProcessEvents(Application.Instance));

            string? responsePath = null;
            if (reply.BodyValues != null && reply.BodyValues.Length > 0)
                responsePath = reply.BodyValues[0] as string;

            Console.WriteLine($"[PortalPrint] Response path: {responsePath}");
            if (responsePath == null)
                return DialogResult.Cancel;

            if (responsePath != requestPath)
            {
                string actualMatch = $"type='signal',interface='{RequestIface}',path='{responsePath}'";
                conn.AddMatch(actualMatch);
            }

            var signal = conn.WaitForSignal(RequestIface, "Response", timeoutMs: 180000,
                pumpEvents: () => CoreForms.Ui.Platform.Platform.ProcessEvents(Application.Instance));

            if (signal?.BodyValues == null || signal.BodyValues.Length < 2)
                return DialogResult.Cancel;

            uint responseCode = signal.BodyValues[0] is uint u ? u : 0;
            var results = signal.BodyValues[1] as Dictionary<string, object?>;

            if (responseCode != 0 || results == null)
                return responseCode switch { 0 => DialogResult.OK, _ => DialogResult.Cancel };

            // Parse portal settings into PrinterSettings + PageSettings
            var printerSettings = new PrinterSettings();
            var pgSettings = new PageSettings();

            if (results.TryGetValue("settings", out var sObj) && sObj is Dictionary<string, object?> sDict)
            {
                if (sDict.TryGetValue("n-copies", out var copies) && copies is string copiesStr)
                {
                    if (int.TryParse(copiesStr, out int copiesVal))
                        printerSettings.Copies = copiesVal;
                }

                if (sDict.TryGetValue("collate", out var coll) && coll is string collStr)
                    printerSettings.Collate = collStr == "true";

                if (sDict.TryGetValue("print-pages", out var pages) && pages is string pagesStr)
                {
                    printerSettings.PrintRange = pagesStr switch
                    {
                        "ranges" => PrintRange.SomePages,
                        "selection" => PrintRange.Selection,
                        "current" => PrintRange.CurrentPage,
                        _ => PrintRange.AllPages
                    };
                }

                if (sDict.TryGetValue("page-ranges", out var ranges) && ranges is string rangesStr)
                {
                    var parts = rangesStr.Split(',');
                    if (parts.Length > 0)
                    {
                        var range = parts[0].Split('-');
                        int fromVal = 0, toVal = 0;
                        if (range.Length > 0)
                            int.TryParse(range[0], out fromVal);
                        if (range.Length > 1)
                            int.TryParse(range[1], out toVal);
                        printerSettings.FromPage = fromVal + 1;
                        printerSettings.ToPage = toVal + 1;
                    }
                }

                if (sDict.TryGetValue("use-color", out var color) && color is string colorStr)
                    printerSettings.Collate = colorStr == "true";

                if (sDict.TryGetValue("orientation", out var orient) && orient is string orientStr)
                    pgSettings.Landscape = orientStr == "landscape" || orientStr == "reverse_landscape";
            }

            if (results.TryGetValue("page-setup", out var psObj) && psObj is Dictionary<string, object?> psDict)
            {
                // Paper size in mm → hundredths of inch
                if (psDict.TryGetValue("Width", out var w) && w is double wMm)
                {
                    pgSettings.PaperSize = new PaperSize("Custom",
                        (int)(wMm / 25.4 * 100),
                        pgSettings.PaperSize.Height);
                }
                if (psDict.TryGetValue("Height", out var h) && h is double hMm)
                {
                    pgSettings.PaperSize = new PaperSize("Custom",
                        pgSettings.PaperSize.Width,
                        (int)(hMm / 25.4 * 100));
                }

                if (psDict.TryGetValue("Orientation", out var psOrient) && psOrient is string psOrientStr)
                    pgSettings.Landscape = psOrientStr == "landscape" || psOrientStr == "reverse-landscape";
            }

            resultSettings = printerSettings;
            resultPageSettings = pgSettings;
            return DialogResult.OK;
        }
        finally
        {
            msg.Dispose();
        }
    }

    /// <summary>
    /// Sends a PDF to the portal for printing.
    /// </summary>
    public static void PrintPdf(byte[] pdfData, string printerName, string? parentHandle)
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"print_{Guid.NewGuid():N}.pdf");
        try
        {
            File.WriteAllBytes(tempFile, pdfData);

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "lp",
                Arguments = $"-d \"{printerName}\" \"{tempFile}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var proc = System.Diagnostics.Process.Start(psi);
            if (proc != null)
                proc.WaitForExit(30000);
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
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

    private static void AppendEmptyDict(ref DBusMessageIter iter)
    {
        if (!dbus_message_iter_open_container(ref iter, DBUS_TYPE_ARRAY, "{sv}", out DBusMessageIter dictIter))
            return;
        dbus_message_iter_close_container(ref iter, ref dictIter);
    }

    private static void AppendDictEntryString(ref DBusMessageIter dictIter, string key, string value)
    {
        if (!dbus_message_iter_open_container(ref dictIter, DBUS_TYPE_DICT_ENTRY, null, out DBusMessageIter entryIter))
            return;

        var keyStr = Marshal.StringToCoTaskMemUTF8(key);
        dbus_message_iter_append_basic(ref entryIter, DBUS_TYPE_STRING, ref keyStr);
        Marshal.FreeCoTaskMem(keyStr);

        if (!dbus_message_iter_open_container(ref entryIter, DBUS_TYPE_VARIANT, "s", out DBusMessageIter varIter))
            return;

        var valStr = Marshal.StringToCoTaskMemUTF8(value);
        dbus_message_iter_append_basic(ref varIter, DBUS_TYPE_STRING, ref valStr);
        Marshal.FreeCoTaskMem(valStr);

        dbus_message_iter_close_container(ref entryIter, ref varIter);
        dbus_message_iter_close_container(ref dictIter, ref entryIter);
    }
}
