namespace CoreForms.Ui.Controls.Advanced;

/// <summary>
/// Represents a cell in a DataGridView.
/// </summary>
public class DataGridViewCell
{
    private object? _value;

    /// <summary>
    /// Called when the cell value changes. Set by the owning DataGridView.
    /// </summary>
    internal Action? OnValueChanged { get; set; }

    /// <summary>
    /// Gets or sets the value of the cell.
    /// </summary>
    public object? Value
    {
        get => _value;
        set
        {
            if (!Equals(_value, value))
            {
                _value = value;
                OnValueChanged?.Invoke();
            }
        }
    }

    /// <summary>
    /// Gets or sets the style of the cell.
    /// </summary>
    public string? Style { get; set; }
}