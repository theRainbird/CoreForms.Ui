using System.Collections;
using System.ComponentModel;
using CoreForms.Ui.Core;
using CoreForms.Ui.Data;
using CoreForms.Ui.Theming;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Advanced;

/// <summary>
/// A control for rendering business diagrams such as bar charts, pie charts,
/// line charts, area charts, and gauge displays. Supports multiple series,
/// axes with scales, trend lines, legends, data binding, and zoom/pan.
/// </summary>
public class DiagramView : ContainerControl
{
    private DiagramType _chartType = DiagramType.Bar;
    private readonly DiagramViewSeriesCollection _series = new();
    private readonly DiagramViewAxisConfig _xAxis = new();
    private readonly DiagramViewAxisConfig _yAxis = new();
    private readonly DiagramViewTrendLine _trendLine = new();
    private LegendPosition _legendPosition = LegendPosition.Right;
    private bool _showLegend = true;
    private LabelStyle _dataLabelStyle = LabelStyle.None;

    private float _zoomLevel = 1.0f;
    private float _panX;
    private float _panY;
    private bool _isPanning;
    private Point _lastPanPoint;
    private bool _themeColorsLoaded;

    private object? _dataSource;
    private string _displayMember = string.Empty;
    private string _valueMember = string.Empty;
    private string _categoryMember = string.Empty;
    private bool _dataSourceUpdating;
    private BindingSource? _boundBindingSource;

    private readonly ScrollBarEngine _hScrollBar = new();
    private readonly ScrollBarEngine _vScrollBar = new();

    private Rectangle _chartAreaCached;
    private Rectangle _legendAreaCached;

    /// <summary>
    /// Initializes a new instance of DiagramView.
    /// </summary>
    public DiagramView()
    {
        Size = new Size(400, 300);
        TabStop = true;
        LoadThemeColors();

        _series.CollectionChanged += (s, e) => Invalidate();

        _hScrollBar.Scroll += (s, e) => { _panX = -_hScrollBar.Value; Invalidate(); };
        _vScrollBar.Scroll += (s, e) => { _panY = -_vScrollBar.Value; Invalidate(); };
    }

    private void LoadThemeColors()
    {
        var theme = ThemeManager.CurrentTheme;
        _diagramBackColor = theme.DiagramBackground;
        _diagramGridLineColor = theme.DiagramGridLine;
        _diagramAxisLineColor = theme.DiagramAxisLine;
        _diagramAxisLabelColor = theme.DiagramAxisLabel;
        _diagramLegendBackColor = theme.DiagramLegendBackground;
        _palette = theme.DiagramPalette;
        _themeColorsLoaded = true;
    }

    private Color _diagramBackColor;
    private Color _diagramGridLineColor;
    private Color _diagramAxisLineColor;
    private Color _diagramAxisLabelColor;
    private Color _diagramLegendBackColor;
    private Color[] _palette = Array.Empty<Color>();

    /// <summary>
    /// Gets or sets the type of diagram to render.
    /// </summary>
    public DiagramType ChartType
    {
        get => _chartType;
        set
        {
            if (_chartType != value)
            {
                _chartType = value;
                OnPropertyChanged(nameof(ChartType));
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets the collection of data series in this diagram.
    /// </summary>
    public DiagramViewSeriesCollection Series => _series;

    /// <summary>
    /// Gets the X-axis configuration.
    /// </summary>
    public DiagramViewAxisConfig XAxis => _xAxis;

    /// <summary>
    /// Gets the Y-axis configuration.
    /// </summary>
    public DiagramViewAxisConfig YAxis => _yAxis;

    /// <summary>
    /// Gets or sets the legend position.
    /// </summary>
    public LegendPosition LegendPosition
    {
        get => _legendPosition;
        set
        {
            if (_legendPosition != value)
            {
                _legendPosition = value;
                OnPropertyChanged(nameof(LegendPosition));
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the legend is displayed.
    /// </summary>
    public bool ShowLegend
    {
        get => _showLegend;
        set
        {
            if (_showLegend != value)
            {
                _showLegend = value;
                OnPropertyChanged(nameof(ShowLegend));
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets the data label display style.
    /// </summary>
    public LabelStyle DataLabelStyle
    {
        get => _dataLabelStyle;
        set
        {
            if (_dataLabelStyle != value)
            {
                _dataLabelStyle = value;
                OnPropertyChanged(nameof(DataLabelStyle));
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets the trend line configuration.
    /// </summary>
    public DiagramViewTrendLine TrendLine => _trendLine;

    /// <summary>
    /// Gets or sets the current zoom level. 1.0 = 100%.
    /// </summary>
    public float ZoomLevel
    {
        get => _zoomLevel;
        set
        {
            float clamped = Math.Clamp(value, 0.1f, 10f);
            if (Math.Abs(_zoomLevel - clamped) > 0.01f)
            {
                _zoomLevel = clamped;
                OnPropertyChanged(nameof(ZoomLevel));
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets the horizontal pan offset in pixels.
    /// </summary>
    public float PanX
    {
        get => _panX;
        set { _panX = value; Invalidate(); }
    }

    /// <summary>
    /// Gets or sets the vertical pan offset in pixels.
    /// </summary>
    public float PanY
    {
        get => _panY;
        set { _panY = value; Invalidate(); }
    }

    /// <summary>
    /// Gets or sets the data source for populating series automatically.
    /// </summary>
    public object? DataSource
    {
        get => _dataSource;
        set
        {
            if (_dataSource != value)
            {
                _dataSource = value;
                OnDataSourceChanged();
                OnPropertyChanged(nameof(DataSource));
            }
        }
    }

    /// <summary>
    /// Gets or sets the property name used for the data point label (category).
    /// </summary>
    public string DisplayMember
    {
        get => _displayMember;
        set
        {
            if (_displayMember != value)
            {
                _displayMember = value;
                OnPropertyChanged(nameof(DisplayMember));
                PopulateFromDataSource();
            }
        }
    }

    /// <summary>
    /// Gets or sets the property name used for the numeric value.
    /// </summary>
    public string ValueMember
    {
        get => _valueMember;
        set
        {
            if (_valueMember != value)
            {
                _valueMember = value;
                OnPropertyChanged(nameof(ValueMember));
                PopulateFromDataSource();
            }
        }
    }

    /// <summary>
    /// Gets or sets the property name used for the category/grouping key.
    /// </summary>
    public string CategoryMember
    {
        get => _categoryMember;
        set
        {
            if (_categoryMember != value)
            {
                _categoryMember = value;
                OnPropertyChanged(nameof(CategoryMember));
                PopulateFromDataSource();
            }
        }
    }

    /// <summary>
    /// Gets the cached chart area rectangle.
    /// </summary>
    protected Rectangle ChartArea => _chartAreaCached;

    /// <inheritdoc />
    public override void OnThemeChanged(Theme newTheme)
    {
        LoadThemeColors();
        Invalidate();
    }

    /// <inheritdoc />
    public override void Render(Graphics g)
    {
        if (!Visible || Width <= 0 || Height <= 0) return;

        if (!_themeColorsLoaded)
            LoadThemeColors();

        var theme = ThemeManager.CurrentTheme;

        g.FillRectangle(BackColor, 0, 0, Width, Height);

        g.Save();
        g.TranslateTransform(_panX, _panY);
        g.Zoom = _zoomLevel;

        ComputeChartLayout(g, theme);

        if (_chartAreaCached.Width <= 0 || _chartAreaCached.Height <= 0)
        {
            g.Restore();
            base.Render(g);
            return;
        }

        g.FillRectangle(_diagramBackColor, _chartAreaCached.X, _chartAreaCached.Y,
            _chartAreaCached.Width, _chartAreaCached.Height);

        RenderGridLines(g);
        RenderData(g);
        RenderAxes(g);
        RenderLegend(g);

        g.Restore();
        base.Render(g);
    }

    private void ComputeChartLayout(Graphics g, Theme theme)
    {
        int pad = 10;
        int left = pad;
        int top = pad;
        int right = Width - pad;
        int bottom = Height - pad;

        if (_showLegend && _series.Count > 0)
        {
            var legendSize = MeasureLegend(g);
            switch (_legendPosition)
            {
                case LegendPosition.Right:
                    right -= legendSize.Width + pad;
                    _legendAreaCached = new Rectangle(right + pad, top, legendSize.Width, bottom - top);
                    break;
                case LegendPosition.Left:
                    left += legendSize.Width + pad;
                    _legendAreaCached = new Rectangle(pad, top, legendSize.Width, bottom - top);
                    break;
                case LegendPosition.Top:
                    top += legendSize.Height + pad;
                    _legendAreaCached = new Rectangle(left, pad, right - left, legendSize.Height);
                    break;
                case LegendPosition.Bottom:
                    bottom -= legendSize.Height + pad;
                    _legendAreaCached = new Rectangle(left, bottom + pad, right - left, legendSize.Height);
                    break;
            }
        }

        bool isGauge = _chartType == DiagramType.Gauge;
        bool isPie = _chartType == DiagramType.Pie || _chartType == DiagramType.Doughnut;

        int yAxisWidth = 0;
        if (!isGauge && !isPie && _yAxis.Visible)
        {
            var dataBounds = GetDataBounds();
            int maxLabelWidth = 0;
            var font = theme.SmallFont;
            int tickCount = EstimateTickCount(dataBounds.MaxValue - dataBounds.MinValue, _yAxis.TickInterval);
            for (int i = 0; i <= tickCount; i++)
            {
                double val = dataBounds.MinValue + i * (dataBounds.MaxValue - dataBounds.MinValue) / Math.Max(tickCount, 1);
                var label = FormatValue(val);
                var (w, _) = CoordinateTransform.MeasureText(label, font, g.Zoom);
                if (w > maxLabelWidth) maxLabelWidth = w;
            }
            yAxisWidth = maxLabelWidth + 15;

            if (!string.IsNullOrEmpty(_yAxis.Title))
            {
                var (tw, _) = CoordinateTransform.MeasureText(_yAxis.Title, theme.DefaultFont, g.Zoom);
                yAxisWidth += tw + 5;
            }
        }

        int xAxisHeight = 0;
        if (!isGauge && !isPie && _xAxis.Visible)
        {
            var font = theme.SmallFont;
            int maxLabelHeight = 0;
            foreach (var series in _series)
            {
                foreach (var pt in series.Points)
                {
                    if (pt.Label != null)
                    {
                        var (_, h) = CoordinateTransform.MeasureText(pt.Label, font, g.Zoom);
                        if (h > maxLabelHeight) maxLabelHeight = h;
                    }
                }
            }
            xAxisHeight = maxLabelHeight + 15;

            if (!string.IsNullOrEmpty(_xAxis.Title))
            {
                var (_, th) = CoordinateTransform.MeasureText(_xAxis.Title, theme.DefaultFont, g.Zoom);
                xAxisHeight += th + 5;
            }
        }

        left += yAxisWidth;
        bottom -= xAxisHeight;

        _chartAreaCached = new Rectangle(
            (int)(left / g.Zoom), (int)(top / g.Zoom),
            (int)((right - left) / g.Zoom),
            (int)((bottom - top) / g.Zoom));
    }

    private (double MinValue, double MaxValue) GetDataBounds()
    {
        bool hasData = false;
        double min = double.MaxValue;
        double max = double.MinValue;

        foreach (var series in _series)
        {
            foreach (var pt in series.Points)
            {
                hasData = true;
                if (pt.Value < min) min = pt.Value;
                if (pt.Value > max) max = pt.Value;
            }
        }

        if (!hasData) return (0, 100);

        if (_yAxis.MinValue.HasValue) min = _yAxis.MinValue.Value;
        if (_yAxis.MaxValue.HasValue) max = _yAxis.MaxValue.Value;

        if (Math.Abs(max - min) < 0.001) max = min + 100;

        double margin = (max - min) * 0.1;
        if (min >= 0) min = 0;
        else min -= margin;
        max += margin;

        return (min, max);
    }

    private static int EstimateTickCount(double range, double interval)
    {
        if (interval > 0) return Math.Max(1, (int)(range / interval));
        double rough = range / 5;
        double magnitude = Math.Pow(10, Math.Floor(Math.Log10(rough)));
        double normalized = rough / magnitude;
        double nice = normalized <= 1.5 ? 1 : normalized <= 3.5 ? 2 : normalized <= 7.5 ? 5 : 10;
        return Math.Max(2, (int)(range / (nice * magnitude)));
    }

    private static double ComputeNiceInterval(double range)
    {
        if (range <= 0) return 1;
        double magnitude = Math.Pow(10, Math.Floor(Math.Log10(range)));
        double normalized = range / magnitude;
        double nice = normalized <= 1.5 ? 1 : normalized <= 3.5 ? 2 : normalized <= 7.5 ? 5 : 10;
        return nice * magnitude;
    }

    private static string FormatValue(double value)
    {
        if (Math.Abs(value) >= 1_000_000) return $"{value / 1_000_000:F1}M";
        if (Math.Abs(value) >= 1_000) return $"{value / 1_000:F1}K";
        if (value == (long)value) return value.ToString("F0");
        return value.ToString("F1");
    }

    private double MapValueToPixel(double value, double minValue, double maxValue, int pixelMin, int pixelMax, AxisType axisType)
    {
        if (axisType == AxisType.Logarithmic)
        {
            double logMin = Math.Log10(Math.Max(minValue, 1));
            double logMax = Math.Log10(Math.Max(maxValue, 1));
            double logVal = Math.Log10(Math.Max(value, 1));
            if (Math.Abs(logMax - logMin) < 0.001) return pixelMin;
            return pixelMin + (logVal - logMin) / (logMax - logMin) * (pixelMax - pixelMin);
        }
        else
        {
            if (Math.Abs(maxValue - minValue) < 0.001) return pixelMin;
            return pixelMin + (value - minValue) / (maxValue - minValue) * (pixelMax - pixelMin);
        }
    }

    private void RenderGridLines(Graphics g)
    {
        var r = _chartAreaCached;
        var bounds = GetDataBounds();
        double interval = _yAxis.TickInterval > 0 ? _yAxis.TickInterval : ComputeNiceInterval(bounds.MaxValue - bounds.MinValue);
        double start = Math.Ceiling(bounds.MinValue / interval) * interval;

        for (double v = start; v <= bounds.MaxValue; v += interval)
        {
            int y = (int)MapValueToPixel(v, bounds.MinValue, bounds.MaxValue,
                r.Y + r.Height, r.Y, _yAxis.Type);
            if (y >= r.Y && y <= r.Y + r.Height)
            {
                g.DrawLine(_diagramGridLineColor, r.X, y, r.X + r.Width, y, 0.5f);
            }
        }
    }

    private void RenderAxes(Graphics g)
    {
        var r = _chartAreaCached;
        var theme = ThemeManager.CurrentTheme;
        var bounds = GetDataBounds();
        var smallFont = theme.SmallFont;

        // Y-axis
        if (_yAxis.Visible)
        {
            if (_yAxis.ShowAxisLine)
            {
                g.DrawLine(_diagramAxisLineColor, r.X, r.Y, r.X, r.Y + r.Height, 1f);
            }

            double interval = _yAxis.TickInterval > 0 ? _yAxis.TickInterval : ComputeNiceInterval(bounds.MaxValue - bounds.MinValue);
            double start = Math.Ceiling(bounds.MinValue / interval) * interval;

            float maxLabelWidth = 0;
            for (double v = start; v <= bounds.MaxValue; v += interval)
            {
                int y = (int)MapValueToPixel(v, bounds.MinValue, bounds.MaxValue,
                    r.Y + r.Height, r.Y, _yAxis.Type);
                if (y < r.Y || y > r.Y + r.Height) continue;

                var label = FormatValue(v);
                var (lw, lh) = CoordinateTransform.MeasureText(label, smallFont, g.Zoom);
                if (lw > maxLabelWidth) maxLabelWidth = lw;

                if (_yAxis.ShowLabels)
                {
                    float lx = r.X - lw - 5;
                    float ly = y - lh / 2f;
                    if (_yAxis.Type == AxisType.Logarithmic && v <= 0) { }
                    else
                    {
                        g.DrawString(label, smallFont, _diagramAxisLabelColor, lx, ly);
                    }
                }

                if (_xAxis.ShowGridLines)
                {
                    g.DrawLine(_diagramGridLineColor, r.X, y, r.X + r.Width, y, 0.5f);
                }
            }

            // Y-axis title
            if (!string.IsNullOrEmpty(_yAxis.Title))
            {
                var (_, th) = CoordinateTransform.MeasureText(_yAxis.Title, theme.DefaultFont, g.Zoom);
                float ty = r.Y + r.Height / 2f - th;
                float tx = r.X - maxLabelWidth - th - 10;
                g.DrawString(_yAxis.Title, theme.DefaultFont, _diagramAxisLabelColor, tx, ty);
            }
        }

        // X-axis
        if (_xAxis.Visible)
        {
            int maxPoints = 0;
            foreach (var s in _series) maxPoints = Math.Max(maxPoints, s.Points.Count);

            if (_xAxis.ShowAxisLine)
            {
                g.DrawLine(_diagramAxisLineColor, r.X, r.Y + r.Height, r.X + r.Width, r.Y + r.Height, 1f);
            }

            if (maxPoints > 0 && _xAxis.ShowLabels)
            {
                float slotWidth = r.Width / (float)maxPoints;

                for (int i = 0; i < _series.Count; i++)
                {
                    var s = _series[i];
                    for (int j = 0; j < s.Points.Count; j++)
                    {
                        var pt = s.Points[j];
                        if (pt.Label == null) continue;

                        float cx = r.X + j * slotWidth + slotWidth / 2f;
                        float ly = r.Y + r.Height + 4;

                        var (tw, th) = CoordinateTransform.MeasureText(pt.Label, smallFont, g.Zoom);
                        g.DrawString(pt.Label, smallFont, _diagramAxisLabelColor,
                            cx - tw / 2f, ly);
                    }
                    break; // Only first series for category labels
                }
            }

            // X-axis title
            if (!string.IsNullOrEmpty(_xAxis.Title))
            {
                var (tw, _) = CoordinateTransform.MeasureText(_xAxis.Title, theme.DefaultFont, g.Zoom);
                float tx = r.X + r.Width / 2f - tw / 2f;
                float ty = r.Y + r.Height + (maxPoints > 0 ? 20 : 4);
                g.DrawString(_xAxis.Title, theme.DefaultFont, _diagramAxisLabelColor, tx, ty);
            }
        }
    }

    private void RenderData(Graphics g)
    {
        if (_series.Count == 0) return;

        switch (_chartType)
        {
            case DiagramType.Bar:
                RenderBarChart(g, false);
                break;
            case DiagramType.StackedBar:
                RenderBarChart(g, true);
                break;
            case DiagramType.Pie:
                RenderPieChart(g, false);
                break;
            case DiagramType.Doughnut:
                RenderPieChart(g, true);
                break;
            case DiagramType.Line:
                RenderLineChart(g, false);
                break;
            case DiagramType.Area:
                RenderLineChart(g, true);
                break;
            case DiagramType.Gauge:
                RenderGauge(g);
                break;
        }

        if (_trendLine.Type != TrendLineType.None && _series.Count > 0)
            RenderTrendLine(g);
    }

    private void RenderBarChart(Graphics g, bool stacked)
    {
        var r = _chartAreaCached;
        var bounds = GetDataBounds();

        int maxPoints = 0;
        foreach (var s in _series) maxPoints = Math.Max(maxPoints, s.Points.Count);

        if (maxPoints == 0) return;

        float slotWidth = r.Width / (float)maxPoints;
        float groupWidth = slotWidth * 0.8f;
        float barWidth;

        if (stacked)
        {
            barWidth = groupWidth * 0.9f;
        }
        else
        {
            barWidth = groupWidth / Math.Max(_series.Count, 1) * 0.9f;
        }

        float baselineY = r.Y + r.Height;

        for (int j = 0; j < maxPoints; j++)
        {
            float slotCenterX = r.X + j * slotWidth + slotWidth / 2f;

            double stackedTop = 0;
            double stackedBottom = 0;

            for (int si = 0; si < _series.Count; si++)
            {
                var s = _series[si];
                if (j >= s.Points.Count) continue;

                var pt = s.Points[j];
                var seriesType = s.DiagramType ?? _chartType;
                bool isStacked = seriesType == DiagramType.StackedBar || stacked;

                double val = pt.Value;
                Color barColor = pt.Color ?? s.Color ?? GetPaletteColor(si);

                float barX;
                if (isStacked)
                {
                    barX = slotCenterX - barWidth / 2f;
                }
                else
                {
                    float totalWidth = _series.Count * barWidth;
                    barX = slotCenterX - totalWidth / 2f + si * barWidth;
                }

                double barTopVal = isStacked ? stackedTop : 0;
                double barBottomVal = val > 0 ? 0 : val;
                double barHeightVal = Math.Abs(val);

                if (isStacked)
                {
                    if (val >= 0)
                    {
                        barBottomVal = stackedTop;
                        stackedTop += val;
                    }
                    else
                    {
                        barBottomVal = stackedBottom;
                        stackedBottom += val;
                        barTopVal = val;
                        barHeightVal = Math.Abs(val);
                    }
                }

                int pixelTop = (int)MapValueToPixel(barTopVal + barHeightVal,
                    bounds.MinValue, bounds.MaxValue, r.Y + r.Height, r.Y, _yAxis.Type);
                int pixelBottom = (int)MapValueToPixel(barTopVal,
                    bounds.MinValue, bounds.MaxValue, r.Y + r.Height, r.Y, _yAxis.Type);

                if (pixelTop >= r.Y && pixelBottom <= r.Y + r.Height)
                {
                    g.FillRectangle(barColor, barX, pixelTop, barWidth, pixelBottom - pixelTop);
                    g.DrawRectangle(_diagramAxisLineColor, barX, pixelTop, barWidth, pixelBottom - pixelTop, 0.5f);

                    if (_dataLabelStyle == LabelStyle.Value)
                    {
                        var label = FormatValue(val);
                        var font = ThemeManager.CurrentTheme.SmallFont;
                        var (lw, lh) = CoordinateTransform.MeasureText(label, font, g.Zoom);
                        g.DrawString(label, font, _diagramAxisLabelColor,
                            barX + barWidth / 2f - lw / 2f, pixelTop - lh - 2);
                    }
                }
            }
        }
    }

    private void RenderPieChart(Graphics g, bool doughnut)
    {
        var r = _chartAreaCached;

        int totalPoints = 0;
        foreach (var s in _series) totalPoints += s.Points.Count;

        if (totalPoints == 0) return;

        double totalValue = 0;
        foreach (var s in _series)
            foreach (var pt in s.Points)
                totalValue += Math.Max(pt.Value, 0);

        if (totalValue <= 0) return;

        float cx = r.X + r.Width / 2f;
        float cy = r.Y + r.Height / 2f;
        float diameter = Math.Min(r.Width, r.Height) * 0.85f;
        float px = cx - diameter / 2f;
        float py = cy - diameter / 2f;

        float currentAngle = -90f;
        int pointIndex = 0;

        foreach (var s in _series)
        {
            foreach (var pt in s.Points)
            {
                if (pt.Value <= 0) { pointIndex++; continue; }

                float sweep = (float)(pt.Value / totalValue * 360f);
                Color color = pt.Color ?? s.Color ?? GetPaletteColor(pointIndex);

                g.FillPie(color, px, py, diameter, diameter, currentAngle, sweep);
                g.DrawArc(_diagramAxisLineColor, px, py, diameter, diameter, currentAngle, sweep, 0.5f);

                if (_dataLabelStyle != LabelStyle.None)
                {
                    float midAngle = currentAngle + sweep / 2f;
                    float labelRadius = diameter / 2f * 0.65f;
                    float lx = cx + labelRadius * (float)Math.Cos(midAngle * Math.PI / 180f);
                    float ly = cy + labelRadius * (float)Math.Sin(midAngle * Math.PI / 180f);

                    string label = _dataLabelStyle switch
                    {
                        LabelStyle.Value => FormatValue(pt.Value),
                        LabelStyle.Percentage => $"{pt.Value / totalValue * 100:F1}%",
                        LabelStyle.Category => pt.Label ?? "",
                        _ => ""
                    };

                    if (!string.IsNullOrEmpty(label))
                    {
                        var font = ThemeManager.CurrentTheme.SmallFont;
                        var (lw, lh) = CoordinateTransform.MeasureText(label, font, g.Zoom);
                        g.DrawString(label, font, _diagramAxisLabelColor, lx - lw / 2f, ly - lh / 2f);
                    }
                }

                currentAngle += sweep;
                pointIndex++;
            }
        }

        if (doughnut)
        {
            float holeDiameter = diameter * 0.45f;
            g.FillRectangle(_diagramBackColor,
                cx - holeDiameter / 2f, cy - holeDiameter / 2f,
                holeDiameter, holeDiameter);
            g.FillEllipse(_diagramBackColor,
                cx - holeDiameter / 2f, cy - holeDiameter / 2f,
                holeDiameter, holeDiameter);
        }
    }

    private void RenderLineChart(Graphics g, bool area)
    {
        var r = _chartAreaCached;
        var bounds = GetDataBounds();

        for (int si = 0; si < _series.Count; si++)
        {
            var s = _series[si];
            if (s.Points.Count < 2) continue;

            Color color = s.Color ?? GetPaletteColor(si);
            int maxPoints = 0;
            foreach (var ss in _series) maxPoints = Math.Max(maxPoints, ss.Points.Count);
            if (maxPoints == 0) continue;

            float slotWidth = r.Width / (float)maxPoints;

            var points = new List<(float X, float Y)>();
            for (int j = 0; j < s.Points.Count; j++)
            {
                var pt = s.Points[j];
                float cx = r.X + j * slotWidth + slotWidth / 2f;
                int y = (int)MapValueToPixel(pt.Value, bounds.MinValue, bounds.MaxValue,
                    r.Y + r.Height, r.Y, _yAxis.Type);
                points.Add((cx, y));
            }

            // Draw lines between points
            for (int i = 1; i < points.Count; i++)
            {
                g.DrawLine(color, points[i - 1].X, points[i - 1].Y,
                    points[i].X, points[i].Y, 2f);
            }

            // Draw point markers
            foreach (var (pxX, pyY) in points)
            {
                g.FillEllipse(color, pxX - 3f, pyY - 3f, 6f, 6f);
                g.DrawEllipse(_diagramAxisLineColor, pxX - 3f, pyY - 3f, 6f, 6f, 1f);
            }

            // Fill area under line
            if (area)
            {
                int baselineY = r.Y + r.Height;
                var polygonPoints = new float[points.Count * 2 + 4];
                int idx = 0;
                foreach (var (pxX, pyY) in points)
                {
                    polygonPoints[idx++] = pxX;
                    polygonPoints[idx++] = pyY;
                }
                for (int i = points.Count - 1; i >= 0; i--)
                {
                    polygonPoints[idx++] = points[i].X;
                    polygonPoints[idx++] = baselineY;
                }

                var fillColor = Color.FromArgb(color.A / 3, color.R, color.G, color.B);
                g.FillPolygon(fillColor, polygonPoints);
            }

            // Data labels
            if (_dataLabelStyle == LabelStyle.Value)
            {
                var font = ThemeManager.CurrentTheme.SmallFont;
                for (int j = 0; j < s.Points.Count; j++)
                {
                    var pt = s.Points[j];
                    var label = FormatValue(pt.Value);
                    var (lw, lh) = CoordinateTransform.MeasureText(label, font, g.Zoom);
                    float cx = r.X + j * slotWidth + slotWidth / 2f;
                    int y = (int)MapValueToPixel(pt.Value, bounds.MinValue, bounds.MaxValue,
                        r.Y + r.Height, r.Y, _yAxis.Type);
                    g.DrawString(label, font, _diagramAxisLabelColor, cx - lw / 2f, y - lh - 4);
                }
            }
        }
    }

    private void RenderGauge(Graphics g)
    {
        var r = _chartAreaCached;

        double minVal = _yAxis.MinValue ?? 0;
        double maxVal = _yAxis.MaxValue ?? 100;
        double value = 0;
        if (_series.Count > 0 && _series[0].Points.Count > 0)
            value = _series[0].Points[0].Value;

        value = Math.Clamp(value, minVal, maxVal);

        float cx = r.X + r.Width / 2f;
        float cy = r.Y + r.Height * 0.85f;
        float diameter = Math.Min(r.Width, r.Height) * 1.4f;
        float px = cx - diameter / 2f;
        float py = cy - diameter / 2f;

        float startAngle = 180f;
        float sweepAngle = 180f;

        // Background arc (full gauge)
        g.DrawArc(Color.FromArgb(60, 60, 60), px, py, diameter, diameter, startAngle, sweepAngle, 8f);

        // Value arc
        float fraction = (float)((value - minVal) / (maxVal - minVal));
        float valueSweep = sweepAngle * fraction;
        Color gaugeColor = GetPaletteColor(0);
        g.DrawArc(gaugeColor, px, py, diameter, diameter, startAngle, valueSweep, 8f);

        // Needle
        float needleAngle = startAngle + valueSweep;
        float needleLen = diameter / 2f * 0.8f;
        float nx = cx + needleLen * (float)Math.Cos(needleAngle * Math.PI / 180f);
        float ny = cy + needleLen * (float)Math.Sin(needleAngle * Math.PI / 180f);
        g.DrawLine(_diagramAxisLineColor, cx, cy, nx, ny, 3f);

        // Center dot
        g.FillEllipse(_diagramAxisLineColor, cx - 5f, cy - 5f, 10f, 10f);
        g.FillEllipse(Color.White, cx - 3f, cy - 3f, 6f, 6f);

        // Labels
        var font = ThemeManager.CurrentTheme.SmallFont;
        var minLabel = FormatValue(minVal);
        var maxLabel = FormatValue(maxVal);
        var valLabel = FormatValue(value);

        var (mlw, mlh) = CoordinateTransform.MeasureText(minLabel, font, g.Zoom);
        g.DrawString(minLabel, font, _diagramAxisLabelColor, px - mlw / 2f, cy + 5);

        var (mxw, _) = CoordinateTransform.MeasureText(maxLabel, font, g.Zoom);
        g.DrawString(maxLabel, font, _diagramAxisLabelColor, px + diameter - mxw / 2f, cy + 5);

        var (vw, _) = CoordinateTransform.MeasureText(valLabel, ThemeManager.CurrentTheme.HeadingFont, g.Zoom);
        g.DrawString(valLabel, ThemeManager.CurrentTheme.HeadingFont, gaugeColor,
            cx - vw / 2f, cy - diameter * 0.55f);
    }

    private void RenderTrendLine(Graphics g)
    {
        var r = _chartAreaCached;
        var bounds = GetDataBounds();

        // Combine all points from all series into one dataset
        var allPoints = new List<(double X, double Y)>();
        int pointIndex = 0;
        foreach (var s in _series)
        {
            foreach (var pt in s.Points)
            {
                allPoints.Add((pointIndex, pt.Value));
                pointIndex++;
            }
        }

        if (allPoints.Count < 2) return;

        double[] trendY;
        if (_trendLine.Type == TrendLineType.LinearRegression)
        {
            trendY = CalculateLinearRegression(allPoints);
        }
        else if (_trendLine.Type == TrendLineType.MovingAverage)
        {
            trendY = CalculateMovingAverage(allPoints, _trendLine.MovingAveragePeriod);
        }
        else
        {
            return;
        }

        int maxPoints = 0;
        foreach (var s in _series) maxPoints = Math.Max(maxPoints, s.Points.Count);
        if (maxPoints == 0) return;

        float slotWidth = r.Width / (float)maxPoints;

        var linePoints = new List<(float X, float Y)>();
        for (int i = 0; i < trendY.Length; i++)
        {
            float cx = r.X + i * slotWidth + slotWidth / 2f;
            int y = (int)MapValueToPixel(trendY[i], bounds.MinValue, bounds.MaxValue,
                r.Y + r.Height, r.Y, _yAxis.Type);
            linePoints.Add((cx, y));
        }

        for (int i = 1; i < linePoints.Count; i++)
        {
            g.DrawLine(_trendLine.Color, linePoints[i - 1].X, linePoints[i - 1].Y,
                linePoints[i].X, linePoints[i].Y, _trendLine.LineWidth);
        }
    }

    private static double[] CalculateLinearRegression(List<(double X, double Y)> points)
    {
        int n = points.Count;
        if (n < 2) return points.Select(p => p.Y).ToArray();

        double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;
        foreach (var (x, y) in points)
        {
            sumX += x;
            sumY += y;
            sumXY += x * y;
            sumX2 += x * x;
        }

        double slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
        double intercept = (sumY - slope * sumX) / n;

        return points.Select(p => slope * p.X + intercept).ToArray();
    }

    private static double[] CalculateMovingAverage(List<(double X, double Y)> points, int period)
    {
        if (period < 2) period = 2;
        var result = new double[points.Count];
        for (int i = 0; i < points.Count; i++)
        {
            int count = 0;
            double sum = 0;
            for (int j = Math.Max(0, i - period + 1); j <= i; j++)
            {
                sum += points[j].Y;
                count++;
            }
            result[i] = sum / count;
        }
        return result;
    }

    private void RenderLegend(Graphics g)
    {
        if (!_showLegend || _series.Count == 0) return;

        var theme = ThemeManager.CurrentTheme;
        var font = theme.SmallFont;
        var r = _legendAreaCached;

        g.FillRectangle(_diagramLegendBackColor, r.X, r.Y, r.Width, r.Height);
        g.DrawRectangle(_diagramAxisLineColor, r.X, r.Y, r.Width, r.Height, 0.5f);

        int itemHeight = 20;
        int swatchSize = 12;
        int startY = r.Y + 5;

        for (int i = 0; i < _series.Count; i++)
        {
            var s = _series[i];
            int y = startY + i * itemHeight;
            if (y + itemHeight > r.Y + r.Height) break;

            Color color = s.Color ?? GetPaletteColor(i);
            g.FillRectangle(color, r.X + 5, y + (itemHeight - swatchSize) / 2, swatchSize, swatchSize);

            string name = string.IsNullOrEmpty(s.Name) ? $"Series {i + 1}" : s.Name;
            var (tw, th) = CoordinateTransform.MeasureText(name, font, g.Zoom);
            g.DrawString(name, font, _diagramAxisLabelColor, r.X + 5 + swatchSize + 4,
                y + (itemHeight - th) / 2f);
        }
    }

    private (int Width, int Height) MeasureLegend(Graphics g)
    {
        var font = ThemeManager.CurrentTheme.SmallFont;
        int maxWidth = 0;
        int totalHeight = 0;

        for (int i = 0; i < _series.Count; i++)
        {
            string name = string.IsNullOrEmpty(_series[i].Name) ? $"Series {i + 1}" : _series[i].Name;
            var (tw, _) = CoordinateTransform.MeasureText(name, font, g.Zoom);
            int itemWidth = tw + 25;
            if (itemWidth > maxWidth) maxWidth = itemWidth;
            totalHeight += 20;
        }

        int pad = 12;
        return (maxWidth + pad, totalHeight + pad);
    }

    private Color GetPaletteColor(int index)
    {
        if (_palette.Length == 0)
            return Color.FromArgb(100, 150, 200);
        return _palette[index % _palette.Length];
    }

    /// <summary>
    /// Resets the zoom level to 100% and pan offset to zero.
    /// </summary>
    public void ResetView()
    {
        _zoomLevel = 1.0f;
        _panX = 0;
        _panY = 0;
        Invalidate();
    }

    /// <summary>
    /// Adjusts the zoom to fit the entire chart within the control bounds.
    /// </summary>
    public void FitToBounds()
    {
        _zoomLevel = 1.0f;
        _panX = 0;
        _panY = 0;
        Invalidate();
    }

    /// <inheritdoc />
    protected internal override void OnMouseWheel(EventArgs e)
    {
        var me = e as MouseEventArgs;
        if (me != null)
        {
            _panY += me.Delta * 10f;
            Invalidate();
        }
        base.OnMouseWheel(e);
    }

    /// <inheritdoc />
    protected internal override void OnMouseDown(EventArgs e)
    {
        var me = e as MouseEventArgs;
        if (me != null && me.Button == MouseButtons.Middle)
        {
            _isPanning = true;
            _lastPanPoint = new Point(me.X, me.Y);
            return;
        }
        base.OnMouseDown(e);
    }

    /// <inheritdoc />
    protected internal override void OnMouseUp(EventArgs e)
    {
        var me = e as MouseEventArgs;
        if (me != null && me.Button == MouseButtons.Middle)
        {
            _isPanning = false;
            return;
        }
        base.OnMouseUp(e);
    }

    /// <inheritdoc />
    protected internal override void OnMouseMove(EventArgs e)
    {
        var me = e as MouseEventArgs;
        if (me != null && _isPanning)
        {
            int dx = me.X - _lastPanPoint.X;
            int dy = me.Y - _lastPanPoint.Y;
            _panX += dx;
            _panY += dy;
            _lastPanPoint = new Point(me.X, me.Y);
            Invalidate();
            return;
        }
        base.OnMouseMove(e);
    }

    /// <inheritdoc />
    protected internal override void OnKeyDown(KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.PageUp:
                ZoomLevel += 0.1f;
                e.Handled = true;
                break;
            case Keys.PageDown:
                ZoomLevel -= 0.1f;
                e.Handled = true;
                break;
            case Keys.Home:
                ResetView();
                e.Handled = true;
                break;
        }
        base.OnKeyDown(e);
    }

    /// <summary>
    /// Called when the DataSource property changes.
    /// </summary>
    protected virtual void OnDataSourceChanged()
    {
        if (_dataSourceUpdating) return;
        _dataSourceUpdating = true;
        try
        {
            if (_boundBindingSource != null)
            {
                _boundBindingSource.CurrentChanged -= OnBoundBindingSourceCurrentChanged;
                _boundBindingSource = null;
            }

            _series.Clear();

            if (_dataSource is BindingSource bs)
            {
                _boundBindingSource = bs;
                var list = bs.List;
                if (list != null)
                {
                    list.ListChanged += OnDataSourceListChanged;
                }
                bs.CurrentChanged += OnBoundBindingSourceCurrentChanged;
                PopulateFromDataSource();
            }
            else if (_dataSource != null)
            {
                PopulateFromDataSource();
            }
        }
        finally
        {
            _dataSourceUpdating = false;
        }
    }

    private void OnDataSourceListChanged(object? sender, ListChangedEventArgs e)
    {
        PopulateFromDataSource();
    }

    private void OnBoundBindingSourceCurrentChanged(object? sender, EventArgs e)
    {
    }

    /// <summary>
    /// Populates series from the current DataSource using DisplayMember, ValueMember, and CategoryMember.
    /// </summary>
    public virtual void PopulateFromDataSource()
    {
        if (_dataSourceUpdating) return;
        if (_dataSource == null || string.IsNullOrEmpty(_valueMember)) return;

        _dataSourceUpdating = true;
        try
        {
            _series.Clear();

            IEnumerable? list = null;
            if (_dataSource is BindingSource bs)
                list = bs.List;
            else if (_dataSource is IEnumerable enumerable)
                list = enumerable;

            if (list is null) return;

            var series = new DiagramViewSeries { Name = "Data" };
            foreach (var item in list)
            {
                if (item == null) continue;
                var itemType = item.GetType();

                double value = 0;
                var valProp = itemType.GetProperty(_valueMember);
                if (valProp != null)
                {
                    var val = valProp.GetValue(item);
                    if (val is double d) value = d;
                    else if (val is int iv) value = iv;
                    else if (val is float fv) value = fv;
                    else if (val is long lv) value = lv;
                    else if (val is decimal dec) value = (double)dec;
                }

                string? label = null;
                if (!string.IsNullOrEmpty(_displayMember))
                {
                    var dispProp = itemType.GetProperty(_displayMember);
                    if (dispProp != null)
                        label = dispProp.GetValue(item)?.ToString();
                }

                var pt = new DiagramViewDataPoint(label, value);
                series.Add(pt);
            }

            if (series.Points.Count > 0)
                _series.Add(series);
        }
        finally
        {
            _dataSourceUpdating = false;
        }
    }
}
