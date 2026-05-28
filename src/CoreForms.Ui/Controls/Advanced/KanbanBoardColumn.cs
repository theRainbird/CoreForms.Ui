using CoreForms.Ui.Core;
using CoreForms.Ui.Theming;

namespace CoreForms.Ui.Controls.Advanced;

/// <summary>
/// Represents a single column in a <see cref="KanbanBoardView"/> control.
/// Each column is identified by a <see cref="StateValue"/> that maps to the StateMember property of data items.
/// </summary>
public class KanbanBoardColumn
{
    private string _name = string.Empty;
    private object? _stateValue;
    private Color _backColor;
    private Color _headerColor;
    private bool _backColorSet;
    private bool _headerColorSet;
    private int _width = 220;
    internal readonly List<KanbanBoardCard> Cards = new();

    /// <summary>
    /// Initializes a new instance of <see cref="KanbanBoardColumn"/>.
    /// </summary>
    public KanbanBoardColumn()
    {
        var theme = ThemeManager.CurrentTheme;
        _backColor = theme.ControlBackground;
        _headerColor = theme.Highlight;
    }

    /// <summary>
    /// Gets or sets the display name of the column shown in the header.
    /// </summary>
    public string Name
    {
        get => _name;
        set => _name = value ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets the state value that identifies this column.
    /// Cards whose StateMember value matches this value are displayed in this column.
    /// </summary>
    public object? StateValue
    {
        get => _stateValue;
        set => _stateValue = value;
    }

    /// <summary>
    /// Gets or sets the background color of the column body (card area).
    /// </summary>
    public Color BackColor
    {
        get => _backColor;
        set
        {
            _backColor = value;
            _backColorSet = true;
        }
    }

    /// <summary>
    /// Gets or sets the background color of the column header.
    /// </summary>
    public Color HeaderColor
    {
        get => _headerColor;
        set
        {
            _headerColor = value;
            _headerColorSet = true;
        }
    }

    /// <summary>
    /// Gets or sets the desired width of the column in pixels.
    /// The actual width may be scaled proportionally if total column widths exceed the control width.
    /// </summary>
    public int Width
    {
        get => _width;
        set => _width = Math.Max(50, value);
    }

    /// <summary>
    /// Gets the number of cards in this column.
    /// </summary>
    public int CardCount => Cards.Count;

    /// <summary>
    /// Applies theme colors when no explicit colors have been set.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    internal void ApplyTheme(Theme theme)
    {
        if (!_backColorSet)
            _backColor = theme.ControlBackground;
        if (!_headerColorSet)
            _headerColor = theme.Highlight;
    }
}

/// <summary>
/// Represents a collection of <see cref="KanbanBoardColumn"/> objects for a <see cref="KanbanBoardView"/>.
/// </summary>
public class KanbanBoardColumnCollection
{
    private readonly List<KanbanBoardColumn> _columns = new();
    private readonly Action _invalidate;

    /// <summary>
    /// Initializes a new instance of <see cref="KanbanBoardColumnCollection"/>.
    /// </summary>
    /// <param name="invalidate">Action to invalidate the owning KanbanBoardView.</param>
    internal KanbanBoardColumnCollection(Action invalidate)
    {
        _invalidate = invalidate;
    }

    /// <summary>
    /// Gets the number of columns.
    /// </summary>
    public int Count => _columns.Count;

    /// <summary>
    /// Gets the column at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index.</param>
    public KanbanBoardColumn this[int index] => _columns[index];

    /// <summary>
    /// Adds a column to the collection.
    /// </summary>
    /// <param name="column">The column to add.</param>
    public void Add(KanbanBoardColumn column)
    {
        _columns.Add(column);
        _invalidate();
    }

    /// <summary>
    /// Removes a column from the collection.
    /// </summary>
    /// <param name="column">The column to remove.</param>
    public void Remove(KanbanBoardColumn column)
    {
        _columns.Remove(column);
        _invalidate();
    }

    /// <summary>
    /// Removes all columns from the collection.
    /// </summary>
    public void Clear()
    {
        _columns.Clear();
        _invalidate();
    }

    /// <summary>
    /// Returns an enumerator that iterates through the columns.
    /// </summary>
    public IEnumerator<KanbanBoardColumn> GetEnumerator() => _columns.GetEnumerator();
}
