using System.Collections;
using System.ComponentModel;
using System.Reflection;
using CoreForms.Ui.Core;

namespace CoreForms.Ui.Data;

/// <summary>
/// Associates a control property with a data source property,
/// enabling automatic synchronization between them.
/// </summary>
public class Binding
{
    private readonly Control _control;
    private readonly string _propertyName;
    private readonly object _dataSource;
    private readonly string _dataMember;
    private readonly DataSourceUpdateMode _updateMode;
    private bool _pushing;
    private bool _subscribed;
    private ReflectionPropertyDescriptor? _controlProperty;
    private ReflectionPropertyDescriptor? _sourceProperty;
    private object? _dataSourceInstance;
    private object? _subscribedSourceInstance;
    private bool _savedEnabledState = true;

    /// <summary>
    /// Occurs when the binding operation completes.
    /// </summary>
    public event EventHandler<BindingCompleteEventArgs>? BindingComplete;

    /// <summary>
    /// Occurs when data is formatted for display.
    /// </summary>
    public event ConvertEventHandler? Format;

    /// <summary>
    /// Occurs when data is parsed from display format back to the data source format.
    /// </summary>
    public event ConvertEventHandler? Parse;

    /// <summary>
    /// Gets the control bound to the data source.
    /// </summary>
    public Control Control => _control;

    /// <summary>
    /// Gets the name of the control property being bound.
    /// </summary>
    public string PropertyName => _propertyName;

    /// <summary>
    /// Gets the data source object.
    /// </summary>
    public object DataSource => _dataSource;

    /// <summary>
    /// Gets the property name on the data source to bind to.
    /// </summary>
    public string DataMember => _dataMember;

    /// <summary>
    /// Gets the update mode for the binding.
    /// </summary>
    public DataSourceUpdateMode DataSourceUpdateMode => _updateMode;

    /// <summary>
    /// Gets or sets the format string for display.
    /// </summary>
    public string? FormatString { get; set; }

    /// <summary>
    /// Gets or sets the format provider.
    /// </summary>
    public IFormatProvider? FormatProvider { get; set; }

    /// <summary>
    /// Gets or sets whether formatting is enabled.
    /// </summary>
    public bool FormattingEnabled { get; set; }

    /// <summary>
    /// Gets the default value used when the data source value is null or DBNull.
    /// </summary>
    public object? NullValue { get; set; }

    /// <summary>
    /// Initializes a new instance of the Binding class.
    /// </summary>
    /// <param name="propertyName">The name of the control property.</param>
    /// <param name="dataSource">The data source object.</param>
    /// <param name="dataMember">The name of the property on the data source.</param>
    /// <param name="formattingEnabled">Whether formatting is enabled.</param>
    /// <param name="updateMode">When to push changes back to the data source.</param>
    /// <param name="defaultValue">Default value for null data source values.</param>
    /// <param name="formatString">Format string for display.</param>
    /// <param name="formatProvider">Format provider.</param>
    /// <exception cref="ArgumentNullException">Thrown when propertyName, dataSource, or dataMember is null.</exception>
    public Binding(
        string propertyName,
        object dataSource,
        string dataMember,
        bool formattingEnabled = false,
        DataSourceUpdateMode updateMode = DataSourceUpdateMode.OnPropertyChanged,
        object? defaultValue = null,
        string? formatString = null,
        IFormatProvider? formatProvider = null)
    {
        _propertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        _dataMember = dataMember ?? throw new ArgumentNullException(nameof(dataMember));
        _control = null!;
        FormattingEnabled = formattingEnabled;
        _updateMode = updateMode;
        NullValue = defaultValue;
        FormatString = formatString;
        FormatProvider = formatProvider;
    }

    internal void SetControl(Control control)
    {
        var field = typeof(Binding).GetField("_control", BindingFlags.Instance | BindingFlags.NonPublic);
        field?.SetValue(this, control);
    }

    /// <summary>
    /// Pushes the initial value from the data source to the control.
    /// Subscribes to change notifications for subsequent updates.
    /// </summary>
    internal void PushInitialValue()
    {
        ResolveProperties();
        if (_sourceProperty == null || _controlProperty == null)
            return;

        Subscribe();
        ReadValue();
    }

    /// <summary>
    /// Reads the value from the data source and writes it to the control.
    /// When no current item exists, clears the control to its default value.
    /// </summary>
    internal void ReadValue()
    {
        if (_pushing || _controlProperty == null)
            return;

        _pushing = true;
        try
        {
            var sourceInstance = ResolveSourceInstance();
            if (sourceInstance == null)
            {
                // No current item — clear control and disable
                if (_control != null)
                {
                    var targetType = _controlProperty.PropertyType;
                    object? clearValue = targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
                    _controlProperty.SetValue(_control, clearValue);
                    _savedEnabledState = _control.Enabled;
                    _control.Enabled = false;
                }
                return;
            }

            // Restore control enabled state after empty-list disable
            if (_control != null && !_control.Enabled && _savedEnabledState)
            {
                _control.Enabled = true;
            }

            var value = _sourceProperty!.GetValue(sourceInstance);
            value = OnFormat(value);
            _controlProperty.SetValue(_control!, value);
            OnBindingComplete(BindingCompleteState.Success);
        }
        catch (Exception ex)
        {
            OnBindingComplete(BindingCompleteState.Exception, ex);
        }
        finally
        {
            _pushing = false;
        }
    }

    /// <summary>
    /// Writes the current control value to the data source.
    /// </summary>
    internal void WriteValue()
    {
        if (_pushing || _updateMode == DataSourceUpdateMode.Never || _sourceProperty == null || _controlProperty == null)
            return;

        _pushing = true;
        try
        {
            var sourceInstance = ResolveSourceInstance();
            if (sourceInstance == null)
                return;

            var value = _controlProperty.GetValue(_control);
            value = OnParse(value);
            _sourceProperty.SetValue(sourceInstance, value);
            OnBindingComplete(BindingCompleteState.Success);
        }
        catch (Exception ex)
        {
            OnBindingComplete(BindingCompleteState.Exception, ex);
        }
        finally
        {
            _pushing = false;
        }
    }

    internal void OnControlPropertyChanged()
    {
        if (_updateMode == DataSourceUpdateMode.OnPropertyChanged)
        {
            WriteValue();
        }
    }

    private void ResolveProperties()
    {
        _controlProperty ??= ReflectionPropertyDescriptor.GetProperty(_control.GetType(), _propertyName);
        ResolveSourceProperty();
    }

    private void ResolveSourceProperty()
    {
        var instance = ResolveSourceInstance();
        if (instance != null && _sourceProperty == null)
        {
            _sourceProperty = ReflectionPropertyDescriptor.GetProperty(instance.GetType(), _dataMember);
        }
    }

    private object? ResolveSourceInstance()
    {
        if (_dataSource is BindingSource bs)
        {
            return bs.Current;
        }

        if (_dataSource is IBindingList list)
        {
            if (_dataSourceInstance == null && list.Count > 0)
            {
                _dataSourceInstance = list[0];
            }
            return _dataSourceInstance;
        }

        return _dataSource;
    }

    private void Subscribe()
    {
        if (_subscribed)
            return;

        _subscribed = true;

        if (_control is INotifyPropertyChanged controlInpc)
        {
            controlInpc.PropertyChanged += OnControlPropertyChangedHandler;
        }

        if (_dataSource is BindingSource bs)
        {
            bs.CurrentChanged += OnBindingSourceCurrentChanged;
        }

        _subscribedSourceInstance = ResolveSourceInstance();
        if (_subscribedSourceInstance is INotifyPropertyChanged sourceInpc)
        {
            sourceInpc.PropertyChanged += OnSourcePropertyChangedHandler;
        }
    }

    internal void Unsubscribe()
    {
        if (!_subscribed)
            return;

        _subscribed = false;

        if (_control is INotifyPropertyChanged controlInpc)
        {
            controlInpc.PropertyChanged -= OnControlPropertyChangedHandler;
        }

        if (_dataSource is BindingSource bs)
        {
            bs.CurrentChanged -= OnBindingSourceCurrentChanged;
        }

        if (_subscribedSourceInstance is INotifyPropertyChanged sourceInpc)
        {
            sourceInpc.PropertyChanged -= OnSourcePropertyChangedHandler;
        }
        _subscribedSourceInstance = null;
    }

    private void OnBindingSourceCurrentChanged(object? sender, EventArgs e)
    {
        if (_dataSource is not BindingSource bs)
            return;

        // Re-subscribe PropertyChanged to the new current item
        if (_subscribedSourceInstance is INotifyPropertyChanged oldInpc)
        {
            oldInpc.PropertyChanged -= OnSourcePropertyChangedHandler;
        }

        _subscribedSourceInstance = bs.Current;
        if (_subscribedSourceInstance is INotifyPropertyChanged newInpc)
        {
            newInpc.PropertyChanged += OnSourcePropertyChangedHandler;
        }

        // Re-read the current item's property value
        ReadValue();
    }

    private void OnControlPropertyChangedHandler(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == _propertyName)
        {
            OnControlPropertyChanged();
        }
    }

    private void OnSourcePropertyChangedHandler(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == _dataMember)
        {
            ReadValue();
        }
    }

    private object? OnFormat(object? value)
    {
        if (value == null || value == DBNull.Value)
            return NullValue;

        var targetType = _controlProperty?.PropertyType ?? typeof(object);
        var args = new ConvertEventArgs(value, targetType);
        Format?.Invoke(this, args);

        if (FormattingEnabled && args.Value is IFormattable formattable && !string.IsNullOrEmpty(FormatString))
        {
            return formattable.ToString(FormatString, FormatProvider);
        }

        if (args.Value != null && targetType != typeof(object) && args.Value.GetType() != targetType)
        {
            try
            {
                return Convert.ChangeType(args.Value, targetType, FormatProvider);
            }
            catch
            {
                return args.Value;
            }
        }

        return args.Value;
    }

    private object? OnParse(object? value)
    {
        var targetType = _sourceProperty?.PropertyType ?? typeof(object);
        var args = new ConvertEventArgs(value, targetType);
        Parse?.Invoke(this, args);

        if (args.Value != null && targetType != typeof(object) && args.Value.GetType() != targetType)
        {
            try
            {
                return Convert.ChangeType(args.Value, targetType, FormatProvider);
            }
            catch
            {
                return args.Value;
            }
        }

        return args.Value;
    }

    private void OnBindingComplete(BindingCompleteState state, Exception? exception = null)
    {
        var args = new BindingCompleteEventArgs(this, state, exception);
        BindingComplete?.Invoke(this, args);
    }
}
