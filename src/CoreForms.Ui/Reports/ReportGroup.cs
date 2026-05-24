namespace CoreForms.Ui.Reports;

/// <summary>
/// Defines a grouping level within a report. Each group can have an optional
/// header and footer band, and can contain nested child groups.
/// </summary>
public class ReportGroup
{
    /// <summary>
    /// Gets or sets the name of this group.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data field on which to group records.
    /// </summary>
    public string GroupField { get; set; } = string.Empty;

    /// <summary>
    /// Gets the header band rendered before each group.
    /// </summary>
    public ReportBand Header { get; }

    /// <summary>
    /// Gets the footer band rendered after each group.
    /// </summary>
    public ReportBand Footer { get; }

    /// <summary>
    /// Gets or sets whether the entire group (header + detail + footer)
    /// should be kept together on one page if possible.
    /// </summary>
    public bool KeepTogether { get; set; }

    /// <summary>
    /// Gets the nested child groups.
    /// </summary>
    public ReportGroupCollection Groups { get; } = new();

    /// <summary>
    /// Initializes a new group for the specified data field.
    /// </summary>
    /// <param name="groupField">The data field to group by.</param>
    /// <param name="name">Optional name for this group.</param>
    public ReportGroup(string groupField, string? name = null)
    {
        GroupField = groupField;
        Name = name ?? $"Group on {groupField}";
        Header = new ReportBand(ReportBandType.GroupHeader) { GroupField = groupField };
        Footer = new ReportBand(ReportBandType.GroupFooter) { GroupField = groupField };
    }
}
