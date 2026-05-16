using System.Collections;
using CoreForms.Ui.Core;

namespace CoreForms.Ui.Data;

/// <summary>
/// Represents the collection of data bindings for a control.
/// </summary>
public class ControlBindingsCollection : ICollection<Binding>
{
    private readonly Control _owner;
    private readonly List<Binding> _bindings = new();

    /// <summary>
    /// Occurs when a binding is added to the collection.
    /// </summary>
    public event EventHandler<Binding>? BindingAdded;

    /// <summary>
    /// Occurs when a binding is removed from the collection.
    /// </summary>
    public event EventHandler<Binding>? BindingRemoved;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="owner">The control that owns this collection.</param>
    public ControlBindingsCollection(Control owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// Gets the owner control.
    /// </summary>
    public Control Owner => _owner;

    /// <inheritdoc/>
    public int Count => _bindings.Count;

    /// <inheritdoc/>
    public bool IsReadOnly => false;

    /// <summary>
    /// Gets the binding for the specified property name.
    /// </summary>
    public Binding? this[string propertyName]
    {
        get { return _bindings.Find(b => b.PropertyName == propertyName); }
    }

    /// <summary>
    /// Creates and adds a binding to the collection.
    /// </summary>
    /// <param name="propertyName">The control property to bind.</param>
    /// <param name="dataSource">The data source.</param>
    /// <param name="dataMember">The property on the data source.</param>
    /// <param name="formattingEnabled">Whether formatting is enabled.</param>
    /// <param name="updateMode">The data source update mode.</param>
    /// <returns>The created Binding.</returns>
    public Binding Add(
        string propertyName,
        object dataSource,
        string dataMember,
        bool formattingEnabled = false,
        DataSourceUpdateMode updateMode = DataSourceUpdateMode.OnPropertyChanged)
    {
        var binding = new Binding(propertyName, dataSource, dataMember, formattingEnabled, updateMode);
        Add(binding);
        return binding;
    }

    /// <summary>
    /// Adds a binding to the collection.
    /// </summary>
    /// <param name="binding">The binding to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when binding is null.</exception>
    /// <exception cref="ArgumentException">Thrown when a binding for the same property already exists.</exception>
    public void Add(Binding binding)
    {
        if (binding == null)
            throw new ArgumentNullException(nameof(binding));

        if (_bindings.Any(b => b.PropertyName == binding.PropertyName))
            throw new ArgumentException($"A binding for property '{binding.PropertyName}' already exists.");

        binding.SetControl(_owner);
        _bindings.Add(binding);
        binding.PushInitialValue();
        BindingAdded?.Invoke(this, binding);
    }

    /// <summary>
    /// Removes a binding from the collection.
    /// </summary>
    /// <param name="binding">The binding to remove.</param>
    /// <returns>True if the binding was removed; otherwise, false.</returns>
    public bool Remove(Binding binding)
    {
        if (_bindings.Remove(binding))
        {
            binding.Unsubscribe();
            BindingRemoved?.Invoke(this, binding);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Removes the binding for the specified property.
    /// </summary>
    /// <param name="propertyName">The property name.</param>
    /// <returns>True if a binding was removed; otherwise, false.</returns>
    public bool Remove(string propertyName)
    {
        var binding = this[propertyName];
        if (binding != null)
            return Remove(binding);
        return false;
    }

    /// <summary>
    /// Removes all bindings.
    /// </summary>
    public void Clear()
    {
        foreach (var binding in _bindings)
            binding.Unsubscribe();
        _bindings.Clear();
    }

    /// <summary>
    /// Determines whether the collection contains the specified binding.
    /// </summary>
    public bool Contains(Binding binding) => _bindings.Contains(binding);

    /// <summary>
    /// Copies the bindings to an array.
    /// </summary>
    public void CopyTo(Binding[] array, int arrayIndex) => _bindings.CopyTo(array, arrayIndex);

    /// <summary>
    /// Returns an enumerator.
    /// </summary>
    public IEnumerator<Binding> GetEnumerator() => _bindings.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _bindings.GetEnumerator();
}
