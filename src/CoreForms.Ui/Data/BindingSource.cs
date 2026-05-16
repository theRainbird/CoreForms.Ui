using System.Collections;
using System.ComponentModel;
using System.Reflection;

namespace CoreForms.Ui.Data;

/// <summary>
/// Wraps a data source and provides binding management, currency, and change notification.
/// Analogous to System.Windows.Forms.BindingSource.
/// </summary>
public class BindingSource : Component
{
    private object? _dataSource;
    private string _dataMember = string.Empty;
    private IBindingList? _list;
    private PropertyManager? _propertyManager;
    private int _position;
    private bool _rebuilding;

    /// <summary>
    /// Occurs when the list changes.
    /// </summary>
    public event ListChangedEventHandler? ListChanged;

    /// <summary>
    /// Occurs when the current item changes.
    /// </summary>
    public event EventHandler? CurrentChanged;

    /// <summary>
    /// Occurs when the current item is about to change.
    /// </summary>
    public event EventHandler? CurrentItemChanged;

    /// <summary>
    /// Occurs when the position in the list changes.
    /// </summary>
    public event EventHandler? PositionChanged;

    /// <summary>
    /// Occurs before a new item is added.
    /// </summary>
    public event AddingNewEventHandler? AddingNew;

    /// <summary>
    /// Gets or sets the data source.
    /// </summary>
    public object? DataSource
    {
        get => _dataSource;
        set
        {
            if (_dataSource != value)
            {
                _dataSource = value;
                ResetList();
            }
        }
    }

    /// <summary>
    /// Gets or sets the data member (property path for nested lists).
    /// </summary>
    public string DataMember
    {
        get => _dataMember;
        set
        {
            if (_dataMember != value)
            {
                _dataMember = value ?? string.Empty;
                ResetList();
            }
        }
    }

    /// <summary>
    /// Gets the underlying list.
    /// </summary>
    public IBindingList? List => _list;

    /// <summary>
    /// Gets the current item.
    /// </summary>
    public object? Current
    {
        get
        {
            if (_list != null && _position >= 0 && _position < _list.Count)
                return _list[_position];
            return _propertyManager?.Current;
        }
    }

    /// <summary>
    /// Gets or sets the current position in the list.
    /// </summary>
    public int Position
    {
        get => _position;
        set
        {
            if (_list == null)
                return;

            if (value < 0) value = 0;
            if (value >= _list.Count) value = Math.Max(0, _list.Count - 1);

            if (_position != value)
            {
                _position = value;
                OnPositionChanged(EventArgs.Empty);
                OnCurrentChanged(EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Gets the number of items in the list.
    /// </summary>
    public int Count => _list?.Count ?? (_propertyManager != null ? 1 : 0);

    /// <summary>
    /// Gets whether the list allows adding new items.
    /// </summary>
    public bool AllowNew => _list?.AllowNew ?? false;

    /// <summary>
    /// Gets whether the list allows editing items.
    /// </summary>
    public bool AllowEdit => _list?.AllowEdit ?? false;

    /// <summary>
    /// Gets whether the list allows removing items.
    /// </summary>
    public bool AllowRemove => _list?.AllowRemove ?? false;

    /// <summary>
    /// Gets whether the data source supports change notification.
    /// </summary>
    public bool SupportsChangeNotification => _list?.SupportsChangeNotification ?? false;

    /// <summary>
    /// Gets a value indicating whether the list is empty.
    /// </summary>
    public bool IsEmpty => Count == 0;

    /// <summary>
    /// Gets the sort property description.
    /// </summary>
    public string? Sort { get; private set; }

    /// <summary>
    /// Gets the sort direction.
    /// </summary>
    public ListSortDirection SortDirection { get; private set; }

    /// <summary>
    /// Initializes a new instance of BindingSource.
    /// </summary>
    public BindingSource()
    {
    }

    /// <summary>
    /// Initializes a new instance with a data source.
    /// </summary>
    /// <param name="dataSource">The data source.</param>
    /// <param name="dataMember">The data member.</param>
    public BindingSource(object dataSource, string dataMember = "")
    {
        _dataSource = dataSource;
        _dataMember = dataMember ?? string.Empty;
        ResetList();
    }

    /// <summary>
    /// Gets the list as an IList.
    /// </summary>
    public IList? GetList()
    {
        return _list as IList;
    }

    /// <summary>
    /// Moves to the first item.
    /// </summary>
    public void MoveFirst() => Position = 0;

    /// <summary>
    /// Moves to the previous item.
    /// </summary>
    public void MovePrevious() => Position = _position - 1;

    /// <summary>
    /// Moves to the next item.
    /// </summary>
    public void MoveNext() => Position = _position + 1;

    /// <summary>
    /// Moves to the last item.
    /// </summary>
    public void MoveLast() => Position = _list != null ? Math.Max(0, _list.Count - 1) : 0;

    /// <summary>
    /// Adds a new item to the list.
    /// </summary>
    public object? AddNew()
    {
        var args = new AddingNewEventArgs(null);
        OnAddingNew(args);

        object? newItem;
        if (args.NewObject != null)
        {
            newItem = args.NewObject;
        }
        else if (_list != null)
        {
            newItem = _list.AddNew();
        }
        else
        {
            return null;
        }

        Position = _list?.Count - 1 ?? 0;
        return newItem;
    }

    /// <summary>
    /// Adds an existing item to the list.
    /// </summary>
    public int Add(object? value)
    {
        if (_list != null && value != null)
        {
            var index = _list.Add(value);
            return index;
        }
        return -1;
    }

    /// <summary>
    /// Removes the specified item from the list.
    /// </summary>
    public void Remove(object? value)
    {
        if (_list != null && value != null)
        {
            _list.Remove(value);
        }
    }

    /// <summary>
    /// Removes the item at the specified index.
    /// </summary>
    public void RemoveAt(int index)
    {
        if (_list != null && index >= 0 && index < _list.Count)
        {
            _list.RemoveAt(index);
        }
    }

    /// <summary>
    /// Clears the list.
    /// </summary>
    public void Clear()
    {
        _list?.Clear();
    }

    /// <summary>
    /// Finds the index of the item with the specified property value.
    /// </summary>
    public int Find(string propertyName, object key)
    {
        if (_list == null)
            return -1;

        var prop = ReflectionPropertyDescriptor.GetProperty(typeof(BindingSource), propertyName);
        if (prop == null)
            return -1;

        for (int i = 0; i < _list.Count; i++)
        {
            var item = _list[i];
            if (item != null)
            {
                var value = prop.GetValue(item);
                if (Equals(value, key))
                    return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Gets the value of the specified property on the current item.
    /// </summary>
    public object? GetPropertyValue(string propertyName)
    {
        var current = Current;
        if (current == null)
            return null;

        var prop = current.GetType().GetProperty(propertyName);
        return prop?.GetValue(current);
    }

    /// <summary>
    /// Resets the list based on the current DataSource and DataMember.
    /// </summary>
    protected virtual void ResetList()
    {
        if (_rebuilding)
            return;

        _rebuilding = true;
        try
        {
            UnsubscribeFromList();
            _list = CreateBindingList();
            _position = _list != null && _list.Count > 0 ? 0 : -1;
            SubscribeToList();
            OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
            OnCurrentChanged(EventArgs.Empty);
            OnPositionChanged(EventArgs.Empty);
        }
        finally
        {
            _rebuilding = false;
        }
    }

    private IBindingList? CreateBindingList()
    {
        if (_dataSource == null)
            return null;

        if (_dataSource is IBindingList bl)
        {
            if (!string.IsNullOrEmpty(_dataMember))
                return ResolveNestedList(bl, _dataMember);
            return bl;
        }

        if (_dataSource is IList list && _dataSource.GetType().IsGenericType)
        {
            var wrapper = new BindingList<object>();
            foreach (var item in list)
                wrapper.Add(item);
            if (list is IBindingList ibl)
                return ibl;
            return wrapper;
        }

        if (_dataSource is IEnumerable enumerable && _dataSource is not string)
        {
            var wrapper = new BindingList<object>();
            foreach (var item in enumerable)
                wrapper.Add(item);
            return wrapper;
        }

        _propertyManager = new PropertyManager(_dataSource);
        return null;
    }

    private static IBindingList? ResolveNestedList(IBindingList parent, string dataMember)
    {
        if (parent.Count == 0 || string.IsNullOrEmpty(dataMember))
            return parent;

        var first = parent[0];
        if (first == null)
            return parent;

        var prop = first.GetType().GetProperty(dataMember);
        if (prop == null)
            return parent;

        var value = prop.GetValue(first);
        if (value is IBindingList nestedList)
            return nestedList;
        if (value is IList nestedIList)
        {
            var wrapper = new BindingList<object>();
            foreach (var item in nestedIList)
                wrapper.Add(item);
            return wrapper;
        }

        return parent;
    }

    private void SubscribeToList()
    {
        if (_list != null)
        {
            _list.ListChanged += OnListChangedInternal;
        }
    }

    private void UnsubscribeFromList()
    {
        if (_list != null)
        {
            _list.ListChanged -= OnListChangedInternal;
        }
    }

    private void OnListChangedInternal(object? sender, ListChangedEventArgs e)
    {
        if (e.ListChangedType == ListChangedType.ItemDeleted && _position >= _list!.Count)
        {
            _position = Math.Max(0, _list.Count - 1);
        }

        OnListChanged(e);
    }

    /// <summary>
    /// Raises the ListChanged event.
    /// </summary>
    protected virtual void OnListChanged(ListChangedEventArgs e)
    {
        ListChanged?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the CurrentChanged event.
    /// </summary>
    protected virtual void OnCurrentChanged(EventArgs e)
    {
        CurrentChanged?.Invoke(this, e);
        CurrentItemChanged?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the PositionChanged event.
    /// </summary>
    protected virtual void OnPositionChanged(EventArgs e)
    {
        PositionChanged?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the AddingNew event.
    /// </summary>
    protected virtual void OnAddingNew(AddingNewEventArgs e)
    {
        AddingNew?.Invoke(this, e);
    }

    /// <summary>
    /// Disposes the binding source.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            UnsubscribeFromList();
        }
        base.Dispose(disposing);
    }
}
