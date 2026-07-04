using OldSchoolForms.Ui.Core;

namespace OldSchoolForms.Ui.Controls.Advanced;

/// <summary>
/// Represents a single card (item) displayed within a <see cref="KanbanBoardView"/> column.
/// Wraps a data item and caches the display values for efficient rendering.
/// </summary>
public class KanbanBoardCard
{
    /// <summary>
    /// Gets the underlying data item from the data source.
    /// </summary>
    public object DataItem { get; }

    /// <summary>
    /// Gets or sets the display text computed from <see cref="KanbanBoardView.DisplayMember"/>.
    /// </summary>
    public string DisplayText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description text computed from <see cref="KanbanBoardView.DescriptionMember"/>.
    /// </summary>
    public string DescriptionText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the category text computed from <see cref="KanbanBoardView.CategoryMember"/>.
    /// </summary>
    public string CategoryText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the assigned-to text computed from <see cref="KanbanBoardView.AssignedToMember"/>.
    /// </summary>
    public string AssignedToText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tags computed from <see cref="KanbanBoardView.TagMember"/>.
    /// </summary>
    public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the state value used to determine which column this card belongs to.
    /// </summary>
    public object? StateValue { get; set; }

    /// <summary>
    /// Gets or sets the value from <see cref="KanbanBoardView.ValueMember"/>.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="KanbanBoardCard"/>.
    /// </summary>
    /// <param name="dataItem">The data item this card wraps.</param>
    /// <exception cref="ArgumentNullException">Thrown when dataItem is null.</exception>
    public KanbanBoardCard(object dataItem)
    {
        DataItem = dataItem ?? throw new ArgumentNullException(nameof(dataItem));
    }
}
