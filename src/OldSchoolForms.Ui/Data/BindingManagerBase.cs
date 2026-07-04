using System.ComponentModel;

namespace OldSchoolForms.Ui.Data;

/// <summary>
/// Abstract base class for managing bindings between a data source and bound controls.
/// </summary>
public abstract class BindingManagerBase
{
    private readonly EventHandlerList _events = new();

    /// <summary>
    /// Occurs when the binding completes.
    /// </summary>
    public event EventHandler<BindingCompleteEventArgs>? BindingComplete;

    /// <summary>
    /// Occurs when the current item changes.
    /// </summary>
    public event EventHandler? CurrentChanged;

    /// <summary>
    /// Occurs when the current item is about to change.
    /// </summary>
    public event EventHandler? CurrentItemChanged;

    /// <summary>
    /// Gets or sets the position in the data source.
    /// </summary>
    public abstract int Position { get; set; }

    /// <summary>
    /// Gets the number of items in the data source.
    /// </summary>
    public abstract int Count { get; }

    /// <summary>
    /// Gets the current item.
    /// </summary>
    public abstract object? Current { get; }

    /// <summary>
    /// Gets the data source object.
    /// </summary>
    public abstract object DataSource { get; }

    /// <summary>
    /// Resumes data binding.
    /// </summary>
    public abstract void ResumeBinding();

    /// <summary>
    /// Suspends data binding.
    /// </summary>
    public abstract void SuspendBinding();

    /// <summary>
    /// Raises the BindingComplete event.
    /// </summary>
    protected virtual void OnBindingComplete(BindingCompleteEventArgs e)
    {
        BindingComplete?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the CurrentChanged event.
    /// </summary>
    protected virtual void OnCurrentChanged(EventArgs e)
    {
        CurrentChanged?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the CurrentItemChanged event.
    /// </summary>
    protected virtual void OnCurrentItemChanged(EventArgs e)
    {
        CurrentItemChanged?.Invoke(this, e);
    }
}
