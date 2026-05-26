namespace CoreForms.Ui.Controls.Advanced;

/// <summary>
/// Configures the appearance and scale of a diagram axis.
/// </summary>
public class DiagramViewAxisConfig
{
    /// <summary>
    /// Gets or sets whether the axis is visible.
    /// </summary>
    public bool Visible { get; set; } = true;

    /// <summary>
    /// Gets or sets the scale type (linear or logarithmic).
    /// </summary>
    public AxisType Type { get; set; } = AxisType.Linear;

    /// <summary>
    /// Gets or sets the axis title text displayed next to the axis.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the minimum value of the axis scale.
    /// If null, the minimum data value is used.
    /// </summary>
    public double? MinValue { get; set; }

    /// <summary>
    /// Gets or sets the maximum value of the axis scale.
    /// If null, the maximum data value is used.
    /// </summary>
    public double? MaxValue { get; set; }

    /// <summary>
    /// Gets or sets the interval between tick marks on the axis.
    /// If 0, the interval is calculated automatically.
    /// </summary>
    public double TickInterval { get; set; }

    /// <summary>
    /// Gets or sets whether grid lines are drawn from this axis.
    /// </summary>
    public bool ShowGridLines { get; set; } = true;

    /// <summary>
    /// Gets or sets whether tick mark labels are shown.
    /// </summary>
    public bool ShowLabels { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the axis line itself is drawn.
    /// </summary>
    public bool ShowAxisLine { get; set; } = true;
}
