using OldSchoolForms.Ui.Core;

namespace OldSchoolForms.Ui.Controls.Advanced;

/// <summary>
/// Configures a trend line overlay on a diagram.
/// </summary>
public class DiagramViewTrendLine
{
    /// <summary>
    /// Gets or sets the type of trend line calculation.
    /// </summary>
    public TrendLineType Type { get; set; } = TrendLineType.None;

    /// <summary>
    /// Gets or sets the color of the trend line.
    /// </summary>
    public Color Color { get; set; } = Color.Red;

    /// <summary>
    /// Gets or sets the width of the trend line in pixels.
    /// </summary>
    public float LineWidth { get; set; } = 2f;

    /// <summary>
    /// Gets or sets the period for moving average calculation.
    /// Only used when Type is MovingAverage.
    /// </summary>
    public int MovingAveragePeriod { get; set; } = 5;

    /// <summary>
    /// Gets or sets the dash style pattern for the trend line.
    /// </summary>
    public float[]? DashPattern { get; set; }
}
