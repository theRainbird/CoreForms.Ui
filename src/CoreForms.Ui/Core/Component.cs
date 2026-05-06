using System.ComponentModel;

namespace CoreForms.Ui.Core;

/// <summary>
/// Provides a base implementation for components in the UI framework.
/// </summary>
public class Component : IComponent
{
    private ISite? _site;
    private EventHandlerList? _events;

    /// <summary>
    /// Gets or sets the site associated with the component.
    /// </summary>
    public ISite? Site
    {
        get => _site;
        set => _site = value;
    }

    /// <summary>
    /// Occurs when the component is disposed.
    /// </summary>
    public event EventHandler? Disposed
    {
        add => Events.AddHandler(nameof(Disposed), value);
        remove => Events.RemoveHandler(nameof(Disposed), value);
    }

    /// <summary>
    /// Gets the event handler list for this component.
    /// </summary>
    protected EventHandlerList Events => _events ??= new EventHandlerList();

    /// <summary>
    /// Releases all resources used by the component.
    /// </summary>
    public virtual void Dispose()
    {
        Events?.Dispose();
    }

    /// <summary>
    /// Releases the resources used by the component.
    /// </summary>
    /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Dispose();
        }
    }
}