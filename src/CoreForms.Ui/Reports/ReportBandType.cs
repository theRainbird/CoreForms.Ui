namespace CoreForms.Ui.Reports;

/// <summary>
/// Defines the type of a report band section.
/// Bands are rendered in a specific order to form a printable report page.
/// </summary>
public enum ReportBandType
{
    /// <summary>
    /// Rendered once at the beginning of the report, before the first page header.
    /// </summary>
    ReportHeader = 0,

    /// <summary>
    /// Rendered at the top of every page (including the first page).
    /// </summary>
    PageHeader = 1,

    /// <summary>
    /// Rendered before each group of records (if grouping is defined).
    /// </summary>
    GroupHeader = 2,

    /// <summary>
    /// Rendered once for each record in the data source.
    /// </summary>
    Detail = 3,

    /// <summary>
    /// Rendered after each group of records (if grouping is defined).
    /// </summary>
    GroupFooter = 4,

    /// <summary>
    /// Rendered at the bottom of every page.
    /// </summary>
    PageFooter = 5,

    /// <summary>
    /// Rendered once at the end of the report, after the last page footer.
    /// </summary>
    ReportFooter = 6
}
