using System.Collections;
using System.ComponentModel;

namespace OldSchoolForms.Ui.Data;

/// <summary>
/// Manages the collection of BindingManagerBase objects for a container control.
/// </summary>
public class BindingContext
{
    private readonly Dictionary<object, BindingManagerBase> _managers = new();

    /// <summary>
    /// Gets the BindingManagerBase for the specified data source.
    /// </summary>
    /// <param name="dataSource">The data source.</param>
    /// <returns>The binding manager for the data source.</returns>
    /// <exception cref="ArgumentNullException">Thrown when dataSource is null.</exception>
    public BindingManagerBase this[object dataSource]
    {
        get
        {
            if (dataSource == null)
                throw new ArgumentNullException(nameof(dataSource));

            if (!_managers.TryGetValue(dataSource, out var manager))
            {
                manager = CreateManager(dataSource);
                _managers[dataSource] = manager;
            }
            return manager;
        }
    }

    /// <summary>
    /// Gets the BindingManagerBase for the specified data source and data member.
    /// </summary>
    /// <param name="dataSource">The data source.</param>
    /// <param name="dataMember">The data member (property path for nested lists).</param>
    /// <returns>The binding manager for the data source and member.</returns>
    public BindingManagerBase this[object dataSource, string dataMember]
    {
        get
        {
            var key = Tuple.Create(dataSource, dataMember);
            if (!_managers.ContainsKey(key))
            {
                var resolved = ResolveDataMember(dataSource, dataMember);
                _managers[key] = resolved;
                return resolved;
            }
            return _managers[key];
        }
    }

    private BindingManagerBase CreateManager(object dataSource)
    {
        if (dataSource is IBindingList bindingList)
        {
            return new CurrencyManager(bindingList);
        }

        if (dataSource is IList list)
        {
            var wrapper = new ListWrapper(list);
            return new CurrencyManager(wrapper);
        }

        if (dataSource is IEnumerable enumerable && dataSource is not string)
        {
            var items = new List<object>();
            foreach (var item in enumerable)
                items.Add(item);
            var wrapper = new ListWrapper(items);
            return new CurrencyManager(wrapper);
        }

        return new PropertyManager(dataSource);
    }

    private BindingManagerBase ResolveDataMember(object dataSource, string dataMember)
    {
        var manager = this[dataSource];
        if (manager is CurrencyManager cm && cm.Current != null)
        {
            var prop = cm.Current.GetType().GetProperty(dataMember);
            if (prop != null)
            {
                var nestedValue = prop.GetValue(cm.Current);
                if (nestedValue != null)
                {
                    return this[nestedValue];
                }
            }
        }
        return manager;
    }

    /// <summary>
    /// Clears all binding managers.
    /// </summary>
    public void Clear()
    {
        _managers.Clear();
    }

    private sealed class ListWrapper : IBindingList
    {
        private readonly IList _list;

        public ListWrapper(IList list) => _list = list;

        public event ListChangedEventHandler? ListChanged;

        public int Count => _list.Count;
        public bool IsReadOnly => _list.IsReadOnly;
        public bool IsFixedSize => _list.IsFixedSize;
        public bool IsSynchronized => _list.IsSynchronized;
        public object SyncRoot => _list.SyncRoot;
        public bool AllowNew => false;
        public bool AllowEdit => false;
        public bool AllowRemove => false;
        public bool SupportsChangeNotification => false;
        public bool SupportsSearching => false;
        public bool SupportsSorting => false;
        public bool IsSorted => false;
        public PropertyDescriptor? SortProperty => null;
        public ListSortDirection SortDirection => ListSortDirection.Ascending;

        public object? this[int index]
        {
            get => _list[index];
            set => _list[index] = value;
        }

        public int Add(object? value) => _list.Add(value);
        public void Clear() => _list.Clear();
        public bool Contains(object? value) => _list.Contains(value);
        public int IndexOf(object? value) => _list.IndexOf(value);
        public void Insert(int index, object? value) => _list.Insert(index, value);
        public void Remove(object? value) => _list.Remove(value);
        public void RemoveAt(int index) => _list.RemoveAt(index);

        public void CopyTo(Array array, int index) => _list.CopyTo(array, index);
        public IEnumerator GetEnumerator() => _list.GetEnumerator();

        public object? AddNew() => throw new NotSupportedException();
        public void AddIndex(PropertyDescriptor property) { }
        public void ApplySort(PropertyDescriptor property, ListSortDirection direction) { }
        public int Find(PropertyDescriptor property, object key) => -1;
        public void RemoveIndex(PropertyDescriptor property) { }
        public void RemoveSort() { }
    }
}
