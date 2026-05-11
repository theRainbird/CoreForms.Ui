namespace CoreForms.Ui.Controls.Advanced;

/// <summary>
/// Provides a collection for DataGridView rows.
/// </summary>
public class DataGridViewRowCollection
{
    private readonly List<DataGridViewRow> _rows = new();

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
    }

    /// <summary>
    /// Removes a row from the collection.
    /// </summary>
    /// <param name="row">The row to remove.</param>
    public void Remove(DataGridViewRow row)
    {
        _rows.Remove(row);
    }

    /// <summary>
    /// Removes all rows from the collection.
    /// </summary>
    public void Clear()
    {
        _rows.Clear();
    }
}