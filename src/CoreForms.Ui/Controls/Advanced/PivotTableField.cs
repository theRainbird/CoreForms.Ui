using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CoreForms.Ui.Controls.Advanced;

/// <summary>
/// Defines a field (dimension or measure) used in a PivotTable control.
/// </summary>
public class PivotTableField : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string _dataPropertyName = string.Empty;
    private PivotTableFieldUsage _usage;
    private PivotTableAggregation _aggregation = PivotTableAggregation.None;
    private string? _formatString;
    private bool _showAsPercentageOfRow;
    private bool _showAsPercentageOfColumn;
    private bool _showAsPercentageOfGrandTotal;
    private bool _isExpanded = true;
    private int _width = 120;
    private bool _visible = true;
    private int _order;

    /// <summary>
    /// Initializes a new instance of <see cref="PivotTableField"/>.
    /// </summary>
    public PivotTableField()
    {
    }

    /// <summary>
    /// Initializes a new instance with the specified name, data property, and usage.
    /// </summary>
    /// <param name="name">Display name of the field.</param>
    /// <param name="dataPropertyName">Property name on the data source objects.</param>
    /// <param name="usage">How the field is used in the pivot table.</param>
    public PivotTableField(string name, string dataPropertyName, PivotTableFieldUsage usage)
    {
        _name = name;
        _dataPropertyName = dataPropertyName;
        _usage = usage;
    }

    /// <summary>
    /// Initializes a new instance with the specified name and usage.
    /// The data property name defaults to the name.
    /// </summary>
    /// <param name="name">Display name and default property name.</param>
    /// <param name="usage">How the field is used in the pivot table.</param>
    public PivotTableField(string name, PivotTableFieldUsage usage)
    {
        _name = name;
        _dataPropertyName = name;
        _usage = usage;
    }

    /// <summary>
    /// Gets or sets the display name shown in headers.
    /// </summary>
    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the property name on the data source object.
    /// </summary>
    public string DataPropertyName
    {
        get => _dataPropertyName;
        set
        {
            if (_dataPropertyName != value)
            {
                _dataPropertyName = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets how this field is used in the pivot table.
    /// </summary>
    public PivotTableFieldUsage Usage
    {
        get => _usage;
        set
        {
            if (_usage != value)
            {
                _usage = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the aggregation function for value fields.
    /// Ignored for row, column, and filter fields.
    /// </summary>
    public PivotTableAggregation Aggregation
    {
        get => _aggregation;
        set
        {
            if (_aggregation != value)
            {
                _aggregation = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the format string (e.g., "C2", "N0") for value display.
    /// </summary>
    public string? FormatString
    {
        get => _formatString;
        set
        {
            if (_formatString != value)
            {
                _formatString = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether to show values as a percentage of the row total.
    /// </summary>
    public bool ShowAsPercentageOfRow
    {
        get => _showAsPercentageOfRow;
        set
        {
            if (_showAsPercentageOfRow != value)
            {
                _showAsPercentageOfRow = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether to show values as a percentage of the column total.
    /// </summary>
    public bool ShowAsPercentageOfColumn
    {
        get => _showAsPercentageOfColumn;
        set
        {
            if (_showAsPercentageOfColumn != value)
            {
                _showAsPercentageOfColumn = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether to show values as a percentage of the grand total.
    /// </summary>
    public bool ShowAsPercentageOfGrandTotal
    {
        get => _showAsPercentageOfGrandTotal;
        set
        {
            if (_showAsPercentageOfGrandTotal != value)
            {
                _showAsPercentageOfGrandTotal = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the default expanded state for drilldown at this field level.
    /// </summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded != value)
            {
                _isExpanded = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the preferred width of the field in pixels (for row header columns).
    /// </summary>
    public int Width
    {
        get => _width;
        set
        {
            if (_width != value)
            {
                _width = Math.Max(30, value);
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the field is visible in the pivot table.
    /// </summary>
    public bool Visible
    {
        get => _visible;
        set
        {
            if (_visible != value)
            {
                _visible = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the display order (lower values appear first).
    /// </summary>
    public int Order
    {
        get => _order;
        set
        {
            if (_order != value)
            {
                _order = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event.
    /// </summary>
    /// <param name="propertyName">The name of the changed property.</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
