using CoreForms.Ui.Core;

namespace CoreForms.Ui.Controls.Advanced;

/// <summary>
/// Represents a single data point in a diagram series.
/// </summary>
public class DiagramViewDataPoint
{
    /// <summary>
    /// Gets or sets the category label for this data point (e.g. "January", "2024").
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Gets or sets the primary numeric value of this data point.
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// Gets or sets an optional secondary value (used for stacked bars as the previous total).
    /// </summary>
    public double? SecondaryValue { get; set; }

    /// <summary>
    /// Gets or sets an optional per-point color. If null, the series color is used.
    /// </summary>
    public Color? Color { get; set; }

    /// <summary>
    /// Gets or sets an optional category key for grouping data points.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Initializes a new instance of DiagramViewDataPoint with default values.
    /// </summary>
    public DiagramViewDataPoint()
    {
    }

    /// <summary>
    /// Initializes a new instance of DiagramViewDataPoint with the specified label and value.
    /// </summary>
    /// <param name="label">The category label.</param>
    /// <param name="value">The numeric value.</param>
    public DiagramViewDataPoint(string? label, double value)
    {
        Label = label;
        Value = value;
    }
}
