namespace OldSchoolForms.Ui.Controls.Advanced;

/// <summary>
/// Defines the type of diagram to render.
/// </summary>
public enum DiagramType
{
    /// <summary>
    /// Vertical bar chart (columns).
    /// </summary>
    Bar,

    /// <summary>
    /// Stacked vertical bar chart.
    /// </summary>
    StackedBar,

    /// <summary>
    /// Pie chart (circular).
    /// </summary>
    Pie,

    /// <summary>
    /// Doughnut chart (pie with hole).
    /// </summary>
    Doughnut,

    /// <summary>
    /// Line chart with connected data points.
    /// </summary>
    Line,

    /// <summary>
    /// Area chart (line chart with filled area below).
    /// </summary>
    Area,

    /// <summary>
    /// Gauge/tachometer display (radial).
    /// </summary>
    Gauge
}

/// <summary>
/// Defines the axis scale type.
/// </summary>
public enum AxisType
{
    /// <summary>
    /// Linear arithmetic scale.
    /// </summary>
    Linear,

    /// <summary>
    /// Logarithmic scale.
    /// </summary>
    Logarithmic
}

/// <summary>
/// Defines the legend position relative to the chart area.
/// </summary>
public enum LegendPosition
{
    /// <summary>
    /// No legend displayed.
    /// </summary>
    None,

    /// <summary>
    /// Legend positioned above the chart.
    /// </summary>
    Top,

    /// <summary>
    /// Legend positioned below the chart.
    /// </summary>
    Bottom,

    /// <summary>
    /// Legend positioned to the left of the chart.
    /// </summary>
    Left,

    /// <summary>
    /// Legend positioned to the right of the chart.
    /// </summary>
    Right
}

/// <summary>
/// Defines how data point labels are displayed.
/// </summary>
public enum LabelStyle
{
    /// <summary>
    /// No data point labels.
    /// </summary>
    None,

    /// <summary>
    /// Show the numeric value of each data point.
    /// </summary>
    Value,

    /// <summary>
    /// Show the percentage of the total (for pie charts).
    /// </summary>
    Percentage,

    /// <summary>
    /// Show the category name of each data point.
    /// </summary>
    Category
}

/// <summary>
/// Defines the type of trend line to calculate and render.
/// </summary>
public enum TrendLineType
{
    /// <summary>
    /// No trend line.
    /// </summary>
    None,

    /// <summary>
    /// Linear regression trend line.
    /// </summary>
    LinearRegression,

    /// <summary>
    /// Simple moving average trend line.
    /// </summary>
    MovingAverage
}
