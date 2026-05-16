using System.ComponentModel;

namespace CoreForms.Ui.Data;

/// <summary>
/// Manages binding for a single object (non-list) data source.
/// </summary>
public class PropertyManager : BindingManagerBase
{
    private object _dataSource;
    private bool _suspended;

    /// <summary>
    /// Initializes a new instance of PropertyManager.
    /// </summary>
    /// <param name="dataSource">The data source object.</param>
    /// <exception cref="ArgumentNullException">Thrown when dataSource is null.</exception>
    public PropertyManager(object dataSource)
    {
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        Subscribe();
    }

    /// <inheritdoc/>
    public override int Position
    {
        get => 0;
        set { }
    }

    /// <inheritdoc/>
    public override int Count => 1;

    /// <inheritdoc/>
    public override object? Current => _dataSource;

    /// <inheritdoc/>
    public override object DataSource => _dataSource;

    /// <inheritdoc/>
    public override void ResumeBinding() => _suspended = false;

    /// <inheritdoc/>
    public override void SuspendBinding() => _suspended = true;

    internal void SetDataSource(object dataSource)
    {
        Unsubscribe();
        _dataSource = dataSource;
        Subscribe();
        OnCurrentChanged(EventArgs.Empty);
    }

    private void Subscribe()
    {
        if (_dataSource is INotifyPropertyChanged inpc)
        {
            inpc.PropertyChanged += OnPropertyChanged;
        }
    }

    private void Unsubscribe()
    {
        if (_dataSource is INotifyPropertyChanged inpc)
        {
            inpc.PropertyChanged -= OnPropertyChanged;
        }
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!_suspended)
        {
            OnCurrentChanged(EventArgs.Empty);
        }
    }
}
