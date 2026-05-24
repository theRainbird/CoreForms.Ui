using CoreForms.Ui.Core;

namespace CoreForms.Ui.Reports;

/// <summary>
/// Defines a printable banded report with sections (ReportHeader, PageHeader,
/// Detail, PageFooter, ReportFooter), optional grouping, and a data source.
/// All measurements are in centimeters (<see cref="Cm"/>).
/// </summary>
public class Report
{
    /// <summary>
    /// Gets or sets the name of this report.
    /// </summary>
    public string Name { get; set; } = "Report";

    /// <summary>
    /// Gets or sets the printable width of the page in cm.
    /// For A4 portrait: 21.0 cm. For A4 landscape: 29.7 cm.
    /// The render engine subtracts margins to determine the available content width.
    /// </summary>
    public Cm PageWidth { get; set; } = 21.0;

    /// <summary>
    /// Gets or sets the total height of the page in cm.
    /// For A4 portrait: 29.7 cm.
    /// </summary>
    public Cm PageHeight { get; set; } = 29.7;

    /// <summary>
    /// Gets or sets the left margin in cm.
    /// </summary>
    public Cm LeftMargin { get; set; } = 2.0;

    /// <summary>
    /// Gets or sets the right margin in cm.
    /// </summary>
    public Cm RightMargin { get; set; } = 2.0;

    /// <summary>
    /// Gets or sets the top margin in cm.
    /// </summary>
    public Cm TopMargin { get; set; } = 1.5;

    /// <summary>
    /// Gets or sets the bottom margin in cm.
    /// </summary>
    public Cm BottomMargin { get; set; } = 1.5;

    /// <summary>
    /// Gets the printable width (page width minus left and right margins).
    /// </summary>
    public Cm PrintableWidth => PageWidth - LeftMargin - RightMargin;

    /// <summary>
    /// Gets the printable height (page height minus top and bottom margins).
    /// </summary>
    public Cm PrintableHeight => PageHeight - TopMargin - BottomMargin;

    /// <summary>
    /// Gets or sets the PageSettings for this report (paper size, orientation, printer margins).
    /// Used when printing; the margins from PageSettings override Left/Right/Top/BottomMargin
    /// if they have been explicitly set.
    /// </summary>
    public PageSettings? PageSettings { get; set; }

    /// <summary>
    /// Gets or sets the data source for this report.
    /// Must be an IEnumerable (list, array, or any enumerable collection of records).
    /// </summary>
    public object? DataSource { get; set; }

    /// <summary>
    /// Gets the report header band (rendered once at the beginning of the report).
    /// </summary>
    public ReportBand ReportHeader { get; } = new(ReportBandType.ReportHeader);

    /// <summary>
    /// Gets the page header band (rendered at the top of every page).
    /// </summary>
    public ReportBand PageHeader { get; } = new(ReportBandType.PageHeader);

    /// <summary>
    /// Gets the detail band (rendered once for each record in the data source).
    /// </summary>
    public ReportBand Detail { get; } = new(ReportBandType.Detail);

    /// <summary>
    /// Gets the page footer band (rendered at the bottom of every page).
    /// </summary>
    public ReportBand PageFooter { get; } = new(ReportBandType.PageFooter);

    /// <summary>
    /// Gets the report footer band (rendered once at the end of the report, after the last page footer).
    /// </summary>
    public ReportBand ReportFooter { get; } = new(ReportBandType.ReportFooter);

    /// <summary>
    /// Gets the groups defined for this report.
    /// Groups are rendered in order and can be nested via <see cref="ReportGroup.Groups"/>.
    /// </summary>
    public ReportGroupCollection Groups { get; } = new();

    /// <summary>
    /// Initializes a new Report with default A4 page settings.
    /// </summary>
    public Report()
    {
    }

    /// <summary>
    /// Initializes a new Report with the specified name.
    /// </summary>
    /// <param name="name">The report name.</param>
    public Report(string name)
    {
        Name = name;
    }
}
