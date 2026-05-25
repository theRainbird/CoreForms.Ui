#pragma warning disable CA1416 // Windows-only
using System.Runtime.InteropServices;
using CoreForms.Ui.Core;

namespace CoreForms.Ui.Platform.Windows;

/// <summary>
/// Windows print dialog implementation using PrintDlg from comdlg32.dll.
/// </summary>
internal static class PrintDialogWindows
{
    [Flags]
    private enum PD_FLAGS : uint
    {
        PD_ALLPAGES = 0x00000000,
        PD_SELECTION = 0x00000001,
        PD_PAGENUMS = 0x00000002,
        PD_NOSELECTION = 0x00000004,
        PD_NOPAGENUMS = 0x00000008,
        PD_COLLATE = 0x00000010,
        PD_PRINTTOFILE = 0x00000020,
        PD_PRINTSETUP = 0x00000040,
        PD_NOWARNING = 0x00000080,
        PD_RETURNDC = 0x00000100,
        PD_RETURNIC = 0x00000200,
        PD_USEDEVMODECOPIESANDCOLLATE = 0x00040000,
        PD_HIDEPRINTTOFILE = 0x00100000,
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PRINTDLG
    {
        public uint lStructSize;
        public IntPtr hwndOwner;
        public IntPtr hDevMode;
        public IntPtr hDevNames;
        public IntPtr hDC;
        public PD_FLAGS Flags;
        public ushort nFromPage;
        public ushort nToPage;
        public ushort nMinPage;
        public ushort nMaxPage;
        public ushort nCopies;
        public IntPtr hInstance;
        public IntPtr lCustData;
        public IntPtr lpfnPrintHook;
        public IntPtr lpfnSetupHook;
        public IntPtr lpPrintTemplateName;
        public IntPtr lpSetupTemplateName;
        public IntPtr hPrintTemplate;
        public IntPtr hSetupTemplate;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DEVNAMES
    {
        public ushort wDriverOffset;
        public ushort wDeviceOffset;
        public ushort wOutputOffset;
        public ushort wDefault;
    }

    [DllImport("comdlg32.dll", SetLastError = true)]
    private static extern bool PrintDlg(ref PRINTDLG ppd);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalFree(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(IntPtr hMem);

    private const ushort PD_ERROR = 0xFFFF;

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

        try
        {
            var pd = new PRINTDLG();
            pd.lStructSize = (uint)Marshal.SizeOf<PRINTDLG>();
            pd.hwndOwner = owner != null ? Platform.GetNativeWindowHandle(owner) : IntPtr.Zero;

            pd.Flags = PD_FLAGS.PD_ALLPAGES | PD_FLAGS.PD_RETURNDC | PD_FLAGS.PD_USEDEVMODECOPIESANDCOLLATE | PD_FLAGS.PD_NOWARNING;

            if (!allowSelection)
                pd.Flags |= PD_FLAGS.PD_NOSELECTION;
            if (!allowSomePages)
                pd.Flags |= PD_FLAGS.PD_NOPAGENUMS;
            if (!allowPrintToFile)
                pd.Flags |= PD_FLAGS.PD_HIDEPRINTTOFILE;

            if (settings != null)
            {
                pd.nCopies = (ushort)Math.Max(1, Math.Min(settings.Copies, ushort.MaxValue));
                pd.nFromPage = (ushort)settings.FromPage;
                pd.nToPage = (ushort)settings.ToPage;
                pd.nMinPage = (ushort)settings.MinPage;
                pd.nMaxPage = (ushort)Math.Max(1, Math.Min(settings.MaxPage, ushort.MaxValue));

                if (settings.Collate)
                    pd.Flags |= PD_FLAGS.PD_COLLATE;

                pd.Flags = (pd.Flags & ~(PD_FLAGS.PD_ALLPAGES | PD_FLAGS.PD_SELECTION | PD_FLAGS.PD_PAGENUMS));
                pd.Flags |= settings.PrintRange switch
                {
                    PrintRange.Selection => PD_FLAGS.PD_SELECTION,
                    PrintRange.SomePages => PD_FLAGS.PD_PAGENUMS,
                    _ => PD_FLAGS.PD_ALLPAGES
                };
            }

            if (!PrintDlg(ref pd))
            {
                ushort err = CommDlgExtendedError();
                if (err != PD_ERROR)
                    Console.WriteLine($"[PrintDialogWindows] PrintDlg failed, extended error: {err}");
                return DialogResult.Cancel;
            }

            // Parse DEVNAMES to get printer name
            string? printerName = null;
            if (pd.hDevNames != IntPtr.Zero)
            {
                IntPtr devNamesPtr = GlobalLock(pd.hDevNames);
                if (devNamesPtr != IntPtr.Zero)
                {
                    var devNames = Marshal.PtrToStructure<DEVNAMES>(devNamesPtr);
                    printerName = Marshal.PtrToStringUni(
                        devNamesPtr + devNames.wDeviceOffset);
                    GlobalUnlock(pd.hDevNames);
                }
                GlobalFree(pd.hDevNames);
            }

            // Parse DEVMODE for copies, orientation, color, duplex
            if (pd.hDevMode != IntPtr.Zero)
                GlobalFree(pd.hDevMode);

            // Free DC if we got one
            if (pd.hDC != IntPtr.Zero)
            {
                // We don't have a direct way to delete GDI DC without P/Invoke
                // but for the dialog-only case we just store the settings
            }

            resultSettings = new PrinterSettings
            {
                PrinterName = printerName ?? "Default",
                Copies = pd.nCopies,
                Collate = pd.Flags.HasFlag(PD_FLAGS.PD_COLLATE),
                PrintRange = pd.Flags switch
                {
                    var f when f.HasFlag(PD_FLAGS.PD_SELECTION) => PrintRange.Selection,
                    var f when f.HasFlag(PD_FLAGS.PD_PAGENUMS) => PrintRange.SomePages,
                    _ => PrintRange.AllPages
                },
                FromPage = pd.nFromPage,
                ToPage = pd.nToPage,
            };

            return DialogResult.OK;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PrintDialogWindows] ShowDialog error: {ex.Message}");
            return DialogResult.Cancel;
        }
    }

    [DllImport("comdlg32.dll")]
    private static extern ushort CommDlgExtendedError();

    [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool EnumPrinters(
        uint flags,
        string? name,
        uint level,
        IntPtr pPrinterEnum,
        uint cbBuf,
        out uint pcbNeeded,
        out uint pcReturned);

    private const uint PRINTER_ENUM_LOCAL = 0x00000002;
    private const uint PRINTER_ENUM_CONNECTIONS = 0x00000004;

    [StructLayout(LayoutKind.Explicit)]
    private struct PRINTER_INFO_5
    {
        [FieldOffset(0)]
        public uint Attributes;
        [FieldOffset(8)]
        public IntPtr pPrinterName;
        [FieldOffset(16)]
        public IntPtr pPortName;
        [FieldOffset(24)]
        public uint Attributes2;
    }

    public static string[] EnumeratePrinters()
    {
        const uint flags = PRINTER_ENUM_LOCAL | PRINTER_ENUM_CONNECTIONS;

        uint needed = 0, returned = 0;
        if (!EnumPrinters(flags, null, 5, IntPtr.Zero, 0, out needed, out returned))
        {
            if (needed == 0)
                return [];
        }

        IntPtr buf = Marshal.AllocHGlobal((int)needed);
        try
        {
            if (!EnumPrinters(flags, null, 5, buf, needed, out needed, out returned))
                return [];

            var printers = new List<string>();
            IntPtr ptr = buf;
            int structSize = Marshal.SizeOf<PRINTER_INFO_5>();
            for (uint i = 0; i < returned; i++)
            {
                var info = Marshal.PtrToStructure<PRINTER_INFO_5>(ptr);
                string? name = info.pPrinterName != IntPtr.Zero
                    ? Marshal.PtrToStringUni(info.pPrinterName)
                    : null;
                if (!string.IsNullOrEmpty(name))
                    printers.Add(name);
                ptr += structSize;
            }

            return printers.ToArray();
        }
        finally
        {
            Marshal.FreeHGlobal(buf);
        }
    }

    /// <summary>
    /// Prints a PDF by sending it directly to the printer via the Windows print spooler.
    /// Uses a simple file-copy approach to the printer port.
    /// </summary>
    public static void PrintPdf(byte[] pdfData, PrinterSettings settings, string documentName)
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"{documentName}_{Guid.NewGuid():N}.pdf");
        try
        {
            File.WriteAllBytes(tempFile, pdfData);

            // Use raw printing via cmd.exe copy to printer
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c copy /b \"{tempFile}\" \"{settings.PrinterName}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var proc = System.Diagnostics.Process.Start(psi);
            if (proc != null)
            {
                proc.WaitForExit(30000);
            }
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }
}
