using System.Runtime.InteropServices;
using CoreForms.Ui.Core;
using CoreForms.Ui.Printing;

namespace CoreForms.Ui.Platform;

/// <summary>
/// Dispatches print dialog calls to the appropriate platform implementation.
/// </summary>
internal static class PrintDialogImpl
{
    /// <summary>
    /// Shows the native print dialog and returns the user's printer settings.
    /// </summary>
    public static DialogResult ShowDialog(
        Form? owner,
        PrinterSettings? settings,
        PageSettings? pageSettings,
        bool allowCurrentPage,
        bool allowSomePages,
        bool allowSelection,
        bool allowPrintToFile,
        bool showNetwork,
        out PrinterSettings resultSettings,
        out PageSettings? resultPageSettings)
    {
        resultSettings = new PrinterSettings();
        resultPageSettings = null;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Windows.PrintDialogWindows.ShowDialog(
                owner, settings, pageSettings,
                allowCurrentPage, allowSomePages, allowSelection,
                allowPrintToFile, showNetwork,
                out resultSettings, out resultPageSettings);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            string? parentHandle = DBus.PortalPrint.GetParentHandleString(owner);
            return DBus.PortalPrint.PreparePrint(
                parentHandle, "Print", settings, pageSettings,
                out resultSettings, out resultPageSettings);
        }
        else
        {
            throw new PlatformNotSupportedException(
                "Print dialogs are only supported on Windows and Linux.");
        }
    }

    /// <summary>
    /// Sends a PDF to the system printer.
    /// </summary>
    public static void PrintPdf(byte[] pdfData, PrinterSettings settings, string documentName)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Windows.PrintDialogWindows.PrintPdf(pdfData, settings, documentName);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            string? parentHandle = DBus.PortalPrint.GetParentHandleString(null);
            DBus.PortalPrint.PrintPdf(pdfData, settings.PrinterName, parentHandle);
        }
        else
        {
            throw new PlatformNotSupportedException(
                "Printing is only supported on Windows and Linux.");
        }
    }
}
