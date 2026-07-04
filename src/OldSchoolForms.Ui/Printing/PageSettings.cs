using OldSchoolForms.Ui.Core;

namespace OldSchoolForms.Ui.Printing;

/// <summary>
/// Specifies the settings for a single printed page.
/// </summary>
public class PageSettings
{
    /// <summary>
    /// Gets or sets whether the page is printed in landscape orientation.
    /// </summary>
    public bool Landscape { get; set; }

    /// <summary>
    /// Gets or sets the margins of the page, in hundredths of an inch.
    /// </summary>
    public Margins Margins { get; set; } = new(100, 100, 100, 100);

    /// <summary>
    /// Gets or sets the paper size.
    /// </summary>
    public PaperSize PaperSize { get; set; } = PaperSize.A4;

    /// <summary>
    /// Gets or sets the paper source (tray).
    /// </summary>
    public PaperSource PaperSource { get; set; } = PaperSource.Default;

    /// <summary>
    /// Gets or sets the PrinterSettings associated with these page settings.
    /// </summary>
    public PrinterSettings? PrinterSettings { get; set; }

    /// <summary>
    /// Gets the bounds of the page in pixels at 100 DPI.
    /// Width/Height reflect orientation: landscape swaps width/height.
    /// </summary>
    public Core.Rectangle Bounds
    {
        get
        {
            int w = PaperSize.Width;
            int h = PaperSize.Height;
            if (Landscape)
                (w, h) = (h, w);
            return new Core.Rectangle(0, 0, w, h);
        }
    }

    /// <summary>
    /// Gets the printable area bounds (page minus margins), in hundredths of an inch.
    /// </summary>
    public Core.Rectangle PrintableBounds
    {
        get
        {
            var b = Bounds;
            return new Core.Rectangle(
                b.X + Margins.Left,
                b.Y + Margins.Top,
                b.Width - Margins.Left - Margins.Right,
                b.Height - Margins.Top - Margins.Bottom);
        }
    }

    /// <summary>
    /// Returns a clone of this instance.
    /// </summary>
    public PageSettings Clone() => new()
    {
        Landscape = Landscape,
        Margins = Margins.Clone(),
        PaperSize = new PaperSize(PaperSize.PaperName, PaperSize.Width, PaperSize.Height),
        PaperSource = new PaperSource(PaperSource.SourceName, PaperSource.Kind),
        PrinterSettings = PrinterSettings?.Clone()
    };
}
