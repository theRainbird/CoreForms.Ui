using OldSchoolForms.Ui.Rendering;

namespace OldSchoolForms.Ui.Reports;

/// <summary>
/// Represents a single rendered page of a report.
/// Contains the pre-rendered Graphics commands and metadata about the page.
/// </summary>
public class ReportPage
{
    /// <summary>
    /// Gets the 1-based page number.
    /// </summary>
    public int PageNumber { get; }

    /// <summary>
    /// Gets the total width of this page in cm.
    /// </summary>
    public Cm Width { get; }

    /// <summary>
    /// Gets the total height of this page in cm.
    /// </summary>
    public Cm Height { get; }

    /// <summary>
    /// Gets the pre-rendered graphics commands for this page.
    /// </summary>
    public Graphics Graphics { get; }

    /// <summary>
    /// Gets the list of bands rendered on this page.
    /// </summary>
    public List<ReportBand> Bands { get; } = new();

    /// <summary>
    /// Initializes a new page with the specified dimensions and graphics.
    /// </summary>
    /// <param name="pageNumber">The 1-based page number.</param>
    /// <param name="width">The page width in cm.</param>
    /// <param name="height">The page height in cm.</param>
    /// <param name="graphics">The Graphics to render to.</param>
    public ReportPage(int pageNumber, Cm width, Cm height, Graphics graphics)
    {
        PageNumber = pageNumber;
        Width = width;
        Height = height;
        Graphics = graphics;
    }
}
