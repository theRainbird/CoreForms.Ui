using OldSchoolForms.Ui.Core;
using OldSchoolForms.Ui.Rendering;

namespace OldSchoolForms.Ui.Printing;

/// <summary>
/// Provides data for the PrintDocument.PrintPage event.
/// </summary>
public class PrintPageEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public PrintPageEventArgs(
        Graphics? graphics,
        PageSettings pageSettings,
        Rectangle pageBounds,
        Rectangle marginBounds)
    {
        Graphics = graphics;
        PageSettings = pageSettings;
        PageBounds = pageBounds;
        MarginBounds = marginBounds;
    }

    /// <summary>
    /// Gets the Graphics object used to paint the page.
    /// </summary>
    public Graphics? Graphics { get; }

    /// <summary>
    /// Gets the settings for the current page.
    /// </summary>
    public PageSettings PageSettings { get; }

    /// <summary>
    /// Gets the bounds of the page.
    /// </summary>
    public Rectangle PageBounds { get; }

    /// <summary>
    /// Gets the bounds of the printable area within the page.
    /// </summary>
    public Rectangle MarginBounds { get; }

    /// <summary>
    /// Gets or sets whether to print more pages.
    /// Set to true if there are additional pages to print.
    /// </summary>
    public bool HasMorePages { get; set; }

    /// <summary>
    /// Gets or sets whether the print job should be cancelled.
    /// </summary>
    public bool Cancel { get; set; }
}
