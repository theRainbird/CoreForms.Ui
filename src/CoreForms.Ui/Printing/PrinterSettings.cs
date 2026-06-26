namespace CoreForms.Ui.Printing;

/// <summary>
/// Specifies the configuration of a printer.
/// </summary>
public class PrinterSettings
{
    private string _printerName = string.Empty;

    /// <summary>
    /// Gets or sets the name of the printer to use.
    /// </summary>
    public string PrinterName
    {
        get => _printerName;
        set => _printerName = value ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets the number of copies to print.
    /// </summary>
    public int Copies { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page range to print.
    /// </summary>
    public PrintRange PrintRange { get; set; } = PrintRange.AllPages;

    /// <summary>
    /// Gets or sets the first page to print (when PrintRange is SomePages).
    /// </summary>
    public int FromPage { get; set; }

    /// <summary>
    /// Gets or sets the last page to print (when PrintRange is SomePages).
    /// </summary>
    public int ToPage { get; set; }

    /// <summary>
    /// Gets or sets the maximum page number allowed.
    /// </summary>
    public int MaxPage { get; set; } = 10000;

    /// <summary>
    /// Gets or sets the minimum page number allowed.
    /// </summary>
    public int MinPage { get; set; }

    /// <summary>
    /// Gets or sets whether to collate printed copies.
    /// </summary>
    public bool Collate { get; set; }

    /// <summary>
    /// Gets whether the specified printer is valid.
    /// </summary>
    public bool IsValid
    {
        get
        {
            if (string.IsNullOrEmpty(_printerName))
                return false;
            return InstalledPrinters.Contains(_printerName);
        }
    }

    /// <summary>
    /// Gets whether the default printer is being used.
    /// </summary>
    public bool IsDefaultPrinter => string.IsNullOrEmpty(_printerName);

    /// <summary>
    /// Gets whether the printer supports color printing.
    /// </summary>
    public bool SupportsColor => true; // determined by platform

    /// <summary>
    /// Gets all installed printer names.
    /// </summary>
    public static string[] InstalledPrinters
    {
        get
        {
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                return Platform.Windows.PrintDialogWindows.EnumeratePrinters();
            else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
                return CoreForms.Ui.Platform.CupsNative.EnumeratePrinters();
            return [];
        }
    }

    /// <summary>
    /// Returns a clone of this instance.
    /// </summary>
    public PrinterSettings Clone() => new()
    {
        _printerName = _printerName,
        Copies = Copies,
        PrintRange = PrintRange,
        FromPage = FromPage,
        ToPage = ToPage,
        MaxPage = MaxPage,
        MinPage = MinPage,
        Collate = Collate
    };
}
