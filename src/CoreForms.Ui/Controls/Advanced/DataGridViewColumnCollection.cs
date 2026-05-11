namespace CoreForms.Ui.Controls.Advanced;

/// <summary>
/// Provides a collection for DataGridView columns.
/// </summary>
public class DataGridViewColumnCollection
{
    private readonly List<DataGridViewColumn> _columns = new();

    /// <summary>
    /// Gets the number of columns.
    /// </summary>
    public int Count => _columns.Count;

    /// <summary>
    /// Gets the column at the specified index.
    /// </summary>
    /// <param name="index">The index.</param>
    public DataGridViewColumn this[int index] => _columns[index];

    /// <summary>
    /// Adds a column to the collection.
    /// </summary>
    /// <param name="column">The column to add.</param>
    public void Add(DataGridViewColumn column)
    {
        _columns.Add(column);
    }

    /// <summary>
    /// Removes a column from the collection.
    /// </summary>
    /// <param name="column">The column to remove.</param>
    public void Remove(DataGridViewColumn column)
    {
        _columns.Remove(column);
    }

    /// <summary>
    /// Removes all columns from the collection.
    /// </summary>
    public void Clear()
    {
        _columns.Clear();
    }
}