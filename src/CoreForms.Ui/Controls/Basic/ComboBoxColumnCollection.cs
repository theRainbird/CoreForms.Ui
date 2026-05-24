namespace CoreForms.Ui.Controls.Basic;

/// <summary>
/// Provides a collection of <see cref="ComboBoxColumn"/> for the multi-column ComboBox dropdown.
/// </summary>
public class ComboBoxColumnCollection
{
    private readonly List<ComboBoxColumn> _columns = new();
    private readonly Action _invalidate;

    internal ComboBoxColumnCollection(Action invalidate)
    {
        _invalidate = invalidate;
    }

    /// <summary>
    /// Gets the number of columns in the collection.
    /// </summary>
    public int Count => _columns.Count;

    /// <summary>
    /// Gets the column at the specified index.
    /// </summary>
    public ComboBoxColumn this[int index] => _columns[index];

    /// <summary>
    /// Adds a column to the collection.
    /// </summary>
    public void Add(ComboBoxColumn column)
    {
        _columns.Add(column);
        _invalidate();
    }

    /// <summary>
    /// Removes a column from the collection.
    /// </summary>
    public void Remove(ComboBoxColumn column)
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
}
