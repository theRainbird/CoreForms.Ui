using System.Collections;

namespace CoreForms.Ui.Controls.Containers;

/// <summary>
/// A typed collection for managing ToolStripItem objects in a ToolStrip.
/// </summary>
public class ToolStripItemCollection : IList<ToolStripItem>
{
    private readonly ToolStrip _owner;
    private readonly List<ToolStripItem> _items = new();

    /// <summary>
    /// Initializes a new instance of ToolStripItemCollection for the specified owner.
    /// </summary>
    /// <param name="owner">The ToolStrip that owns this collection.</param>
    public ToolStripItemCollection(ToolStrip owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// Gets the number of items in the collection.
    /// </summary>
    public int Count => _items.Count;

    /// <summary>
    /// Gets a value indicating whether the collection is read-only.
    /// </summary>
    public bool IsReadOnly => false;

    /// <summary>
    /// Gets or sets the item at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The ToolStripItem at the specified index.</returns>
    public ToolStripItem this[int index]
    {
        get => _items[index];
        set
        {
            var oldItem = _items[index];
            _items[index] = value;
            _owner.OnItemRemoved(oldItem);
            _owner.OnItemAdded(value);
        }
    }

    /// <summary>
    /// Adds an item to the collection.
    /// </summary>
    /// <param name="item">The ToolStripItem to add.</param>
    public void Add(ToolStripItem item)
    {
        _items.Add(item);
        _owner.OnItemAdded(item);
    }

    /// <summary>
    /// Adds multiple items to the collection.
    /// </summary>
    /// <param name="items">The items to add.</param>
    public void AddRange(IEnumerable<ToolStripItem> items)
    {
        foreach (var item in items)
        {
            _items.Add(item);
            _owner.OnItemAdded(item);
        }
    }

    /// <summary>
    /// Removes an item from the collection.
    /// </summary>
    /// <param name="item">The ToolStripItem to remove.</param>
    /// <returns>True if the item was removed; otherwise, false.</returns>
    public bool Remove(ToolStripItem item)
    {
        if (_items.Remove(item))
        {
            _owner.OnItemRemoved(item);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Removes the item at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the item to remove.</param>
    public void RemoveAt(int index)
    {
        var item = _items[index];
        _items.RemoveAt(index);
        _owner.OnItemRemoved(item);
    }

    /// <summary>
    /// Inserts an item at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index at which to insert the item.</param>
    /// <param name="item">The ToolStripItem to insert.</param>
    public void Insert(int index, ToolStripItem item)
    {
        _items.Insert(index, item);
        _owner.OnItemAdded(item);
    }

    /// <summary>
    /// Removes all items from the collection.
    /// </summary>
    public void Clear()
    {
        var itemsToRemove = _items.ToList();
        _items.Clear();
        foreach (var item in itemsToRemove)
        {
            _owner.OnItemRemoved(item);
        }
    }

    /// <summary>
    /// Determines whether the collection contains a specific item.
    /// </summary>
    /// <param name="item">The item to locate.</param>
    /// <returns>True if the item is found; otherwise, false.</returns>
    public bool Contains(ToolStripItem item)
    {
        return _items.Contains(item);
    }

    /// <summary>
    /// Copies the items to an array, starting at the specified index.
    /// </summary>
    /// <param name="array">The destination array.</param>
    /// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
    public void CopyTo(ToolStripItem[] array, int arrayIndex)
    {
        _items.CopyTo(array, arrayIndex);
    }

    /// <summary>
    /// Searches for the specified item and returns the zero-based index of the first occurrence.
    /// </summary>
    /// <param name="item">The item to locate.</param>
    /// <returns>The zero-based index of the item, or -1 if not found.</returns>
    public int IndexOf(ToolStripItem item)
    {
        return _items.IndexOf(item);
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator for the collection.</returns>
    public IEnumerator<ToolStripItem> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
