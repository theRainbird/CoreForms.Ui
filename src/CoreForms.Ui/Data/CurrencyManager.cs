using System.ComponentModel;

namespace CoreForms.Ui.Data;

/// <summary>
/// Manages the position and binding of controls bound to an IBindingList data source.
/// </summary>
public class CurrencyManager : BindingManagerBase
{
    private IBindingList _list;
    private int _position;
    private bool _suspended;

    /// <summary>
    /// Initializes a new instance of CurrencyManager.
    /// </summary>
    /// <param name="list">The binding list to manage.</param>
    /// <exception cref="ArgumentNullException">Thrown when list is null.</exception>
    public CurrencyManager(IBindingList list)
    {
        _list = list ?? throw new ArgumentNullException(nameof(list));
        _position = list.Count > 0 ? 0 : -1;
        Subscribe();
    }

    /// <summary>
    /// Gets or sets the underlying list.
    /// </summary>
    public IBindingList List => _list;

    /// <inheritdoc/>
    public override int Position
    {
        get => _position;
        set
        {
            if (value < -1 || value >= _list.Count)
                return;

            if (_position != value)
            {
                _position = value;
                OnCurrentChanged(EventArgs.Empty);
                OnCurrentItemChanged(EventArgs.Empty);
            }
        }
    }

    /// <inheritdoc/>
    public override int Count => _list.Count;

    /// <inheritdoc/>
    public override object? Current
    {
        get
        {
            if (_position >= 0 && _position < _list.Count)
                return _list[_position];
            return null;
        }
    }

    /// <inheritdoc/>
    public override object DataSource => _list;

    /// <inheritdoc/>
    public override void ResumeBinding()
    {
        _suspended = false;
    }

    /// <inheritdoc/>
    public override void SuspendBinding()
    {
        _suspended = true;
    }

    /// <summary>
    /// Moves to the first item.
    /// </summary>
    public void MoveFirst() => Position = _list.Count > 0 ? 0 : -1;

    /// <summary>
    /// Moves to the previous item.
    /// </summary>
    public void MovePrevious() => Position = _position > 0 ? _position - 1 : 0;

    /// <summary>
    /// Moves to the next item.
    /// </summary>
    public void MoveNext() => Position = _position < _list.Count - 1 ? _position + 1 : _list.Count - 1;

    /// <summary>
    /// Moves to the last item.
    /// </summary>
    public void MoveLast() => Position = _list.Count > 0 ? _list.Count - 1 : -1;

    /// <summary>
    /// Adds a new item.
    /// </summary>
    public object? AddNew()
    {
        var item = _list.AddNew();
        Position = _list.Count - 1;
        return item;
    }

    /// <summary>
    /// Removes the current item.
    /// </summary>
    public void RemoveCurrent()
    {
        if (_position >= 0 && _position < _list.Count)
        {
            _list.RemoveAt(_position);
            if (_position >= _list.Count)
                _position = _list.Count - 1;
            OnCurrentChanged(EventArgs.Empty);
        }
    }

    /// <summary>
    /// Cancels the current edit.
    /// </summary>
    public void CancelCurrentEdit()
    {
        if (_list is ICancelAddNew cancel)
        {
            cancel.CancelNew(_position);
        }
    }

    /// <summary>
    /// Ends the current edit.
    /// </summary>
    public void EndCurrentEdit()
    {
        if (_list is ICancelAddNew cancel)
        {
            cancel.EndNew(_position);
        }
    }

    internal void SetList(IBindingList newList)
    {
        Unsubscribe();
        _list = newList;
        _position = _list.Count > 0 ? 0 : -1;
        Subscribe();
        OnCurrentChanged(EventArgs.Empty);
    }

    private void Subscribe()
    {
        if (_list is IBindingList bindingList)
        {
            bindingList.ListChanged += OnListChanged;
        }
    }

    private void Unsubscribe()
    {
        if (_list is IBindingList bindingList)
        {
            bindingList.ListChanged -= OnListChanged;
        }
    }

    private void OnListChanged(object? sender, ListChangedEventArgs e)
    {
        if (_suspended) return;

        switch (e.ListChangedType)
        {
            case ListChangedType.ItemAdded:
                if (_position == -1)
                    _position = 0;
                break;
            case ListChangedType.ItemDeleted:
                if (_position >= _list.Count)
                    _position = Math.Max(0, _list.Count - 1);
                break;
            case ListChangedType.Reset:
                _position = _list.Count > 0 ? Math.Min(_position, _list.Count - 1) : -1;
                break;
        }

        OnCurrentChanged(EventArgs.Empty);
    }
}
