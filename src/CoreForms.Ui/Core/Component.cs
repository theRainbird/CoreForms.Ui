using System.ComponentModel;

namespace CoreForms.Ui.Core;

public class Component : IComponent
{
    private ISite? _site;
    private EventHandlerList? _events;

    public ISite? Site
    {
        get => _site;
        set => _site = value;
    }

    public event EventHandler? Disposed
    {
        add => Events.AddHandler(nameof(Disposed), value);
        remove => Events.RemoveHandler(nameof(Disposed), value);
    }

    protected EventHandlerList Events => _events ??= new EventHandlerList();

    public virtual void Dispose()
    {
        Events?.Dispose();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Dispose();
        }
    }
}