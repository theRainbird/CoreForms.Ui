namespace CoreForms.Ui.Controls.Advanced;

/// <summary>
/// Provides a collection for DataGridView rows.
/// </summary>
public class DataGridViewRowCollection
{
    private readonly List<DataGridViewRow> _rows = new();
    private readonly Action _invalidate;

    /// <summary>
    /// Initializes a new instance of DataGridViewRowCollection.
    /// </summary>
    /// <param name="invalidate">Action to invalidate the owning DataGridView.</param>
    internal DataGridViewRowCollection(Action invalidate)
    {
        _invalidate = invalidate;
    }

    /// <summary>
    /// Gets the number of rows.
    /// </summary>
    public int Count => _rows.Count;

    /// <summary>
    /// Gets the row at the specified index.
    /// </summary>
    /// <param name="index">The index.</param>
    public DataGridViewRow this[int index] => _rows[index];

    /// <summary>
    /// Adds a row to the collection.
    /// </summary>
    /// <param name="row">The row to add.</param>
    public void Add(DataGridViewRow row)
    {
        _rows.Add(row);
        _invalidate();
    }

    /// <summary>
    /// Removes a row from the collection.
    /// </summary>
    /// <param name="row">The row to remove.</param>
    public void Remove(DataGridViewRow row)
    {
        _rows.Remove(row);
        _invalidate();
    }

    /// <summary>
    /// Removes all rows from the collection.
    /// </summary>
    public void Clear()
    {
        _rows.Clear();
        _invalidate();
    }

    /// <summary>
    /// Sorts the rows using the specified comparison.
    /// </summary>
    /// <param name="comparison">The comparison to use for sorting.</param>
    internal void Sort(Comparison<DataGridViewRow> comparison)
    {
        _rows.Sort(comparison);
        _invalidate();
    }
}