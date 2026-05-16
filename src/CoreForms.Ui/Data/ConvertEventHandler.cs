namespace CoreForms.Ui.Data;

/// <summary>
/// Provides data for the Format and Parse events of a Binding.
/// </summary>
public class ConvertEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the value to be converted.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Gets the type to which the value should be converted.
    /// </summary>
    public Type DesiredType { get; }

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public ConvertEventArgs(object? value, Type desiredType)
    {
        Value = value;
        DesiredType = desiredType;
    }
}

/// <summary>
/// Represents the method that handles the Format and Parse events of a Binding.
/// </summary>
public delegate void ConvertEventHandler(object? sender, ConvertEventArgs e);
