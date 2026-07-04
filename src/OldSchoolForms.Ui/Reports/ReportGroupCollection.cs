using System.Collections.ObjectModel;

namespace OldSchoolForms.Ui.Reports;

/// <summary>
/// A collection of <see cref="ReportGroup"/> objects, providing
/// ordered access to grouping levels within a report.
/// </summary>
public class ReportGroupCollection : Collection<ReportGroup>
{
    /// <summary>
    /// Initializes a new empty group collection.
    /// </summary>
    public ReportGroupCollection()
    {
    }

    /// <summary>
    /// Initializes a new group collection with the specified groups.
    /// </summary>
    /// <param name="groups">The groups to add.</param>
    public ReportGroupCollection(IEnumerable<ReportGroup> groups)
    {
        foreach (var g in groups)
            Add(g);
    }
}
