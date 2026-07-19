using System.Collections;
using System.ComponentModel;
using OldSchoolForms.Ui.Core;
using OldSchoolForms.Ui.Data;
using OldSchoolForms.Ui.Theming;
using Graphics = OldSchoolForms.Ui.Rendering.Graphics;

namespace OldSchoolForms.Ui.Controls.Advanced;

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
    private IBindingList? _previousBindingList;

    private readonly ScrollBarEngine _hScrollBar = new();
    private readonly ScrollBarEngine _vScrollBar = new();

    private Rectangle _chartAreaCached;
    private Rectangle _legendAreaCached;
    private float _titleHeight;
    private bool _layoutDirty = true;

    /// <summary>
    /// Initializes a new instance of DiagramView.
    /// </summary>
    public DiagramView()
    {
        Size = new Size(400, 300);
        TabStop = true;
        LoadThemeColors();

        _series.CollectionChanged += (s, e) => InvalidateLayout();

        _hScrollBar.Scroll += (s, e) => { _panX = -_hScrollBar.Value; Invalidate(); };
        _vScrollBar.Scroll += (s, e) => { _panY = -_vScrollBar.Value; Invalidate(); };
    }

    private void InvalidateLayout()
    {
        _layoutDirty = true;
        Invalidate();
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
    private Padding _diagramPadding = new(28, 25, 20, 20);
    private string _title = string.Empty;

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
                InvalidateLayout();
            }
        }
    }

    /// <summary>
    /// Gets the collection of data series in this diagram.
    /// </summary>
    public DiagramViewSeriesCollection Series => _series;

    /// <summary>
    /// Gets or sets the optional diagram title displayed at the top.
    /// </summary>
    public string Title
    {
        get => _title;
        set
        {
            if (_title != value)
            {
                _title = value ?? string.Empty;
                OnPropertyChanged(nameof(Title));
                InvalidateLayout();
            }
        }
    }

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
                InvalidateLayout();
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
                InvalidateLayout();
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
    /// Gets or sets the padding around the chart area (left, top, right, bottom).
    /// Default is 10 pixels on all sides. Set to add space so chart lines
    /// do not touch the control border.
    /// </summary>
    public Padding DiagramPadding
    {
        get => _diagramPadding;
        set
        {
            if (_diagramPadding.Left != value.Left || _diagramPadding.Top != value.Top ||
                _diagramPadding.Right != value.Right || _diagramPadding.Bottom != value.Bottom)
            {
                _diagramPadding = value;
                OnPropertyChanged(nameof(DiagramPadding));
                InvalidateLayout();
            }
        }
    }

    /// <summary>
    /// Gets the trend line configuration.
    /// </summary>
    public DiagramViewTrendLine TrendLine => _trendLine;

    /// <summary>
    /// Gets or sets the red zone upper threshold as a fraction (0-1).
    /// Values below this threshold are in the red zone. Default 0.25.
    /// </summary>
    public double GaugeRedThreshold { get; set; } = 0.25;

    /// <summary>
    /// Gets or sets the yellow zone upper threshold as a fraction (0-1).
    /// Values between red and yellow threshold are in the yellow zone.
    /// Values above the yellow threshold are in the green zone. Default 0.50.
    /// </summary>
    public double GaugeYellowThreshold { get; set; } = 0.50;

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
    protected override void OnBoundsChanged()
    {
        _layoutDirty = true;
        base.OnBoundsChanged();
    }

    /// <inheritdoc />
    public override void OnThemeChanged(Theme newTheme)
    {
        LoadThemeColors();
        base.OnThemeChanged(newTheme);
        InvalidateLayout();
    }

    /// <inheritdoc />
    public override void Render(Graphics g)
    {
        if (!Visible || Width <= 0 || Height <= 0) return;

        if (!_themeColorsLoaded)
            LoadThemeColors();

        var theme = ThemeManager.CurrentTheme;

        g.FillRectangle(BackColor, 0, 0, Width, Height);

        // Clip to control bounds so zoomed content cannot overdraw neighboring controls
        g.SetClip(new Rectangle(0, 0, Width, Height));

        g.Save();

        float cx = Width / 2f;
        float cy = Height / 2f;
        float invZ = 1f / _zoomLevel;
        float zoomOffsetX = _panX + cx * (invZ - 1f);
        float zoomOffsetY = _panY + cy * (invZ - 1f);
        g.TranslateTransform(zoomOffsetX, zoomOffsetY);
        g.Zoom = _zoomLevel;

        if (_layoutDirty)
        {
            float savedZoom = g.Zoom;
            g.Zoom = 1;
            ComputeChartLayout(g, theme);
            g.Zoom = savedZoom;
            _layoutDirty = false;
        }

        if (_chartAreaCached.Width <= 0 || _chartAreaCached.Height <= 0)
        {
            g.Restore();
            g.ResetClip();
            base.Render(g);
            return;
        }

        g.FillRectangle(_diagramBackColor, _chartAreaCached.X, _chartAreaCached.Y,
            _chartAreaCached.Width, _chartAreaCached.Height);

        // Draw diagram title inside the chart area, then render data below it
        Rectangle chartBeforeTitle = _chartAreaCached;
        if (!string.IsNullOrEmpty(_title))
        {
            var titleFont = new Font(ThemeManager.CurrentTheme.DefaultFont.Name,
                ThemeManager.CurrentTheme.DefaultFont.Size * 1.5f, FontStyle.Bold);
            var (tw, th) = CoordinateTransform.MeasureText(_title, titleFont, g.Zoom);
            float tx = _chartAreaCached.X + _chartAreaCached.Width / 2f - tw / 2f;
            float ty = _chartAreaCached.Y + (_titleHeight - th) / 2f;
            g.DrawString(_title, titleFont, _diagramAxisLabelColor, tx, ty);
            // Use data area below title for all rendering (non-destructive — chartBeforeTitle is restored below)
            _chartAreaCached = new Rectangle(_chartAreaCached.X, _chartAreaCached.Y + (int)_titleHeight,
                _chartAreaCached.Width, _chartAreaCached.Height - (int)_titleHeight);
        }

        g.SetClip(_chartAreaCached);

        RenderGridLines(g);
        RenderData(g);

        g.ResetClip();

        RenderAxes(g);
        RenderLegend(g);

        // Restore original chart area for border (includes title space)
        g.DrawRectangle(_diagramAxisLineColor, chartBeforeTitle.X, chartBeforeTitle.Y,
            chartBeforeTitle.Width, chartBeforeTitle.Height, 0.5f);
        _chartAreaCached = chartBeforeTitle;

        g.Restore();
        // Remove the control-bounds clip (added before Save)
        g.ResetClip();
        base.Render(g);
    }

    private void ComputeChartLayout(Graphics g, Theme theme)
    {
        int left = _diagramPadding.Left;
        int top = _diagramPadding.Top;
        int right = Width - _diagramPadding.Right;
        int bottom = Height - _diagramPadding.Bottom;

        // Measure title height (title is drawn at the top of the chart area,
        // data area is shifted down in Render — no space reservation needed here)
        _titleHeight = 0;
        if (!string.IsNullOrEmpty(_title))
        {
            var titleFont = new Font(ThemeManager.CurrentTheme.DefaultFont.Name,
                ThemeManager.CurrentTheme.DefaultFont.Size * 1.5f, FontStyle.Bold);
            var (_, th) = CoordinateTransform.MeasureText(_title, titleFont, g.Zoom);
            _titleHeight = th + 8;
        }

        if (_showLegend && _series.Count > 0)
        {
            var legendSize = MeasureLegend(g);
            switch (_legendPosition)
            {
                case LegendPosition.Right:
                    right -= legendSize.Width + _diagramPadding.Left;
                    int lhR = Math.Min(legendSize.Height, bottom - top);
                    int lyR = top + ((bottom - top) - lhR) / 2;
                    _legendAreaCached = new Rectangle(right + _diagramPadding.Left, lyR, legendSize.Width, lhR);
                    break;
                case LegendPosition.Left:
                    left += legendSize.Width + _diagramPadding.Left;
                    int lhL = Math.Min(legendSize.Height, bottom - top);
                    int lyL = top + ((bottom - top) - lhL) / 2;
                    _legendAreaCached = new Rectangle(_diagramPadding.Left, lyL, legendSize.Width, lhL);
                    break;
                case LegendPosition.Top:
                    top += legendSize.Height + _diagramPadding.Top;
                    _legendAreaCached = new Rectangle(left, _diagramPadding.Top, right - left, legendSize.Height);
                    break;
                case LegendPosition.Bottom:
                    bottom -= legendSize.Height + _diagramPadding.Top;
                    _legendAreaCached = new Rectangle(left, bottom + _diagramPadding.Top, right - left, legendSize.Height);
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
            yAxisWidth = maxLabelWidth + 50;

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

        _chartAreaCached = new Rectangle(left, top, right - left, bottom - top);
    }

    private (double MinValue, double MaxValue) GetDataBounds()
    {
        bool hasData = false;
        double min = double.MaxValue;
        double max = double.MinValue;

        bool isStacked = _chartType == DiagramType.StackedBar;

        if (isStacked)
        {
            // For stacked bars, find the cumulative max per category
            int maxPoints = 0;
            foreach (var s in _series) maxPoints = Math.Max(maxPoints, s.Points.Count);
            for (int j = 0; j < maxPoints; j++)
            {
                double sum = 0;
                foreach (var s in _series)
                {
                    if (j < s.Points.Count)
                    {
                        sum += Math.Max(s.Points[j].Value, 0);
                        hasData = true;
                    }
                }
                if (sum > max) max = sum;
            }
            min = 0;
        }
        else
        {
            foreach (var series in _series)
            {
                foreach (var pt in series.Points)
                {
                    hasData = true;
                    if (pt.Value < min) min = pt.Value;
                    if (pt.Value > max) max = pt.Value;
                }
            }
        }

        if (!hasData) return (0, 100);

        if (_yAxis.MinValue.HasValue) min = _yAxis.MinValue.Value;
        if (_yAxis.MaxValue.HasValue) max = _yAxis.MaxValue.Value;

        if (Math.Abs(max - min) < 0.001) max = min + 100;

        // Add margin, then snap to nice round numbers so axis labels
        // fall on clean values and the top/bottom labels are visible
        double margin = (max - min) * 0.1;
        if (!_yAxis.MinValue.HasValue && min >= 0) min = 0;
        else if (!_yAxis.MinValue.HasValue) min -= margin;
        if (!_yAxis.MaxValue.HasValue) max += margin;

        double interval = ComputeNiceInterval(max - min);
        if (interval > 0 && !_yAxis.MaxValue.HasValue)
            max = Math.Ceiling(max / interval) * interval;
        if (interval > 0 && !_yAxis.MinValue.HasValue && min < 0)
            min = Math.Floor(min / interval) * interval;

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
        if (Math.Abs(value) >= 1_000_000) return $"{value / 1_000_000:F2}M";
        if (Math.Abs(value) >= 1_000) return $"{value / 1_000:F2}K";
        return value.ToString("#,##0.#");
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

        // Y-axis (skip for pie, doughnut, gauge — no meaningful scale)
        bool noAxes = _chartType == DiagramType.Pie || _chartType == DiagramType.Doughnut || _chartType == DiagramType.Gauge;
        if (_yAxis.Visible && !noAxes)
        {
            if (_yAxis.ShowAxisLine)
            {
                g.DrawLine(_diagramAxisLineColor, r.X, r.Y, r.X, r.Y + r.Height, 1f);
            }

            double interval = _yAxis.TickInterval > 0 ? _yAxis.TickInterval : ComputeNiceInterval(bounds.MaxValue - bounds.MinValue);
            double tickStart = Math.Ceiling(bounds.MinValue / interval) * interval;

            float maxLabelWidth = 0;
            for (double v = tickStart; v <= bounds.MaxValue + 0.001; v += interval)
            {
                int y = (int)MapValueToPixel(v, bounds.MinValue, bounds.MaxValue,
                    r.Y + r.Height, r.Y, _yAxis.Type);

                var label = FormatValue(v);
                var (lw, lh) = CoordinateTransform.MeasureText(label, smallFont, g.Zoom);
                if (lw > maxLabelWidth) maxLabelWidth = lw;

                if (_yAxis.ShowLabels)
                {
                    float lx = r.X - lw - 6;
                    float ly = y - lh / 2f;
                    ly = Math.Clamp(ly, r.Y, r.Y + r.Height - lh);
                    if (_yAxis.Type == AxisType.Logarithmic && v <= 0) { }
                    else
                    {
                        g.DrawString(label, smallFont, _diagramAxisLabelColor, lx, ly);
                    }
                }

                if (_xAxis.ShowGridLines && y >= r.Y && y <= r.Y + r.Height)
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

        // X-axis (skip category labels for pie/doughnut/gauge — shown per slice or not applicable)
        if (_xAxis.Visible && !noAxes)
        {
            int maxPoints = 0;
            foreach (var s in _series) maxPoints = Math.Max(maxPoints, s.Points.Count);

            if (_xAxis.ShowAxisLine)
            {
                g.DrawLine(_diagramAxisLineColor, r.X, r.Y + r.Height, r.X + r.Width, r.Y + r.Height, 1f);
            }

            if (maxPoints > 0 && _xAxis.ShowLabels && _series.Count > 0)
            {
                float slotWidth = r.Width / (float)maxPoints;
                var firstSeries = _series[0];

                for (int j = 0; j < firstSeries.Points.Count; j++)
                {
                    var pt = firstSeries.Points[j];
                    if (pt.Label == null) continue;

                    float cx = r.X + j * slotWidth + slotWidth / 2f;
                    float ly = r.Y + r.Height + 4;

                    var (tw, th) = CoordinateTransform.MeasureText(pt.Label, smallFont, g.Zoom);
                    g.DrawString(pt.Label, smallFont, _diagramAxisLabelColor,
                        cx - tw / 2f, ly);
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
        float groupWidth = slotWidth * 0.7f;
        float barWidth;

        if (stacked)
        {
            barWidth = groupWidth * 0.85f;
        }
        else
        {
            barWidth = groupWidth / Math.Max(_series.Count, 1) * 0.85f;
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
                    g.DrawRectangle(Color.White, barX, pixelTop, barWidth, pixelBottom - pixelTop, 1f);

                    if (_dataLabelStyle == LabelStyle.Value)
                    {
                        var label = FormatValue(val);
                        var font = ThemeManager.CurrentTheme.SmallFont;
                        var (lw, lh) = CoordinateTransform.MeasureText(label, font, g.Zoom);
                        float centerY = pixelTop + (pixelBottom - pixelTop) / 2f - lh / 2f;
                        g.DrawString(label, font, _diagramAxisLabelColor,
                            barX + barWidth / 2f - lw / 2f, centerY);
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

        float sliceGap = 2f;
        float currentAngle = -90f;
        int pointIndex = 0;

        foreach (var s in _series)
        {
            foreach (var pt in s.Points)
            {
                if (pt.Value <= 0) { pointIndex++; continue; }

                float sweep = (float)(pt.Value / totalValue * 360f);
                Color color = pt.Color ?? s.Color ?? GetPaletteColor(pointIndex);

                g.FillPie(color, px, py, diameter, diameter, currentAngle, sweep - sliceGap);

                if (_dataLabelStyle != LabelStyle.None)
                {
                    float midAngle = currentAngle + (sweep - sliceGap) / 2f;
                    float pieRadius = diameter / 2f;
                    float midRad = midAngle * (float)Math.PI / 180f;

                    string labelText = _dataLabelStyle switch
                    {
                        LabelStyle.Value => pt.Label != null ? $"{pt.Label}: {FormatValue(pt.Value)}" : FormatValue(pt.Value),
                        LabelStyle.Percentage => pt.Label != null ? $"{pt.Label}: {pt.Value / totalValue * 100:F1}%" : $"{pt.Value / totalValue * 100:F1}%",
                        LabelStyle.Category => pt.Label ?? "",
                        _ => pt.Label ?? ""
                    };

                    if (!string.IsNullOrEmpty(labelText))
                    {
                        var font = ThemeManager.CurrentTheme.SmallFont;
                        var (lw, lh) = CoordinateTransform.MeasureText(labelText, font, g.Zoom);

                        // Chord width at 70% of pie radius (clear doughnut hole at 50%)
                        float labelRadius = pieRadius * 0.70f;
                        float sweepRad = (sweep - sliceGap) * (float)Math.PI / 180f;
                        float chordWidth = 2f * labelRadius * (float)Math.Sin(sweepRad / 2f);
                        float labelPadding = 6f;

                        // Decide inside vs outside label
                        if (lw + labelPadding < chordWidth)
                        {
                            // Inside label — centered in slice, no leader line
                            float lx = cx + labelRadius * (float)Math.Cos(midRad);
                            float ly = cy + labelRadius * (float)Math.Sin(midRad);
                            g.DrawString(labelText, font, _diagramAxisLabelColor,
                                lx - lw / 2f, ly - lh / 2f);
                        }
                        else
                        {
                            // Outside label — leader line from slice edge
                            float edgeRadius = pieRadius * 0.52f;
                            float startX = cx + edgeRadius * (float)Math.Cos(midRad);
                            float startY = cy + edgeRadius * (float)Math.Sin(midRad);

                            float leaderLen = pieRadius * 0.18f;
                            float endX = startX + leaderLen * (float)Math.Cos(midRad);
                            float endY = startY + leaderLen * (float)Math.Sin(midRad);

                            bool isLeft = midAngle > 90 && midAngle < 270;
                            float textX = isLeft ? endX - lw - 4 : endX + 4;

                            g.DrawLine(_diagramAxisLabelColor, startX, startY, endX, endY, 0.5f);
                            g.DrawString(labelText, font, _diagramAxisLabelColor,
                                textX, endY - lh / 2f);
                        }
                    }
                }

                currentAngle += sweep;
                pointIndex++;
            }
        }

        if (doughnut)
        {
            float holeDiameter = diameter * 0.50f;
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
                g.FillEllipse(Color.White, pxX - 4f, pyY - 4f, 8f, 8f);
                g.DrawEllipse(color, pxX - 4f, pyY - 4f, 8f, 8f, 2f);
            }

            // Fill area under line
            if (area)
            {
                int baselineY = r.Y + r.Height;
                var polygonPoints = new float[points.Count * 4];
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

                var fillColor = Color.FromArgb(
                    color.R + (255 - color.R) * 3 / 5,
                    color.G + (255 - color.G) * 3 / 5,
                    color.B + (255 - color.B) * 3 / 5);
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
                    g.DrawString(label, font, _diagramAxisLabelColor, cx - lw / 2f, y - lh - 8);
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

        float fraction = (float)((value - minVal) / (maxVal - minVal));
        double redThresh = Math.Clamp(GaugeRedThreshold, 0, 1);
        double yellowThresh = Math.Clamp(GaugeYellowThreshold, redThresh, 1);

        float cx = r.X + r.Width / 2f;
        float cy = r.Y + r.Height / 2f;
        float diameter = Math.Min(r.Width, r.Height) * 0.88f;
        float radius = diameter / 2f;
        float px = cx - radius;
        float py = cy - radius;

        // Gauge arc: starts at 6:00 (90° = bottom — 0 position),
        // sweeps 300° clockwise to 4:00 (30° = bottom-right — max position)
        // Gap of 60° at the bottom (4:00 to 6:00)
        float startAngle = 90f;
        float sweepAngle = 300f;

        // Outer bezel (full circle housing)
        float bezelWidth = 8f;
        g.DrawArc(Color.FromArgb(45, 45, 50), px - bezelWidth, py - bezelWidth,
            diameter + bezelWidth * 2, diameter + bezelWidth * 2, 0, 360, bezelWidth);
        g.DrawArc(Color.FromArgb(80, 80, 85), px - bezelWidth + 2f, py - bezelWidth + 2f,
            diameter + bezelWidth * 2 - 4f, diameter + bezelWidth * 2 - 4f, 0, 360, 1f);

        // Gauge face (full circle)
        float faceInset = 5f;
        g.FillEllipse(Color.FromArgb(248, 248, 252), px + faceInset, py + faceInset,
            diameter - faceInset * 2, diameter - faceInset * 2);

        // Color zones as thick arcs
        float arcInset = radius * 0.14f;
        float zoneThick = radius * 0.18f;
        float zoneOuterR = radius - arcInset;
        float zoneMidR = zoneOuterR - zoneThick / 2f;

        float greenStart = (float)(startAngle + sweepAngle * yellowThresh);
        float yellowStart = (float)(startAngle + sweepAngle * redThresh);
        float yellowSweep = (float)(sweepAngle * (yellowThresh - redThresh));
        float greenSweep = (float)(sweepAngle * (1.0 - yellowThresh));
        float redSweep = (float)(sweepAngle * redThresh);

        void DrawZoneArc(Color zoneColor, float aStart, float aSweep)
        {
            if (aSweep <= 0) return;
            g.DrawArc(zoneColor, cx - zoneMidR, cy - zoneMidR, zoneMidR * 2, zoneMidR * 2, aStart, aSweep, zoneThick);
        }

        DrawZoneArc(Color.FromArgb(241, 90, 96), startAngle, redSweep);
        DrawZoneArc(Color.FromArgb(237, 176, 32), yellowStart, yellowSweep);
        DrawZoneArc(Color.FromArgb(112, 173, 71), greenStart, greenSweep);

        // Tick marks and labels (only within the arc range)
        int majorTicks = 8;
        int minorTicks = 4;
        var tickFont = new Font(ThemeManager.CurrentTheme.DefaultFont.Name,
            ThemeManager.CurrentTheme.DefaultFont.Size, FontStyle.Bold);
        float tickOuterR = zoneOuterR - zoneThick - 6f;
        float majorTickLen = radius * 0.10f;
        float minorTickLen = radius * 0.05f;
        float labelR = tickOuterR - majorTickLen - 18f;

        for (int i = 0; i <= majorTicks; i++)
        {
            float fracT = (float)i / majorTicks;
            float angle = startAngle + sweepAngle * fracT;
            float rad = angle * (float)Math.PI / 180f;
            float cosA = (float)Math.Cos(rad);
            float sinA = (float)Math.Sin(rad);

            float outerX = cx + tickOuterR * cosA;
            float outerY = cy + tickOuterR * sinA;
            float innerX = cx + (tickOuterR - majorTickLen) * cosA;
            float innerY = cy + (tickOuterR - majorTickLen) * sinA;
            g.DrawLine(Color.FromArgb(50, 50, 55), outerX, outerY, innerX, innerY, 3f);

            double tickVal = minVal + (maxVal - minVal) * fracT;
            string tickLabel = FormatValue(tickVal);
            var (tlw, tlh) = CoordinateTransform.MeasureText(tickLabel, tickFont, g.Zoom);
            float lx = cx + labelR * cosA;
            float ly = cy + labelR * sinA;
            g.DrawString(tickLabel, tickFont, Color.FromArgb(40, 40, 45), lx - tlw / 2f, ly - tlh / 2f);

            if (i < majorTicks)
            {
                for (int j = 1; j < minorTicks; j++)
                {
                    float mFrac = fracT + (float)j / (majorTicks * minorTicks);
                    float mAngle = startAngle + sweepAngle * mFrac;
                    float mRad = mAngle * (float)Math.PI / 180f;
                    float mCos = (float)Math.Cos(mRad);
                    float mSin = (float)Math.Sin(mRad);
                    g.DrawLine(Color.FromArgb(110, 110, 115),
                        cx + tickOuterR * mCos, cy + tickOuterR * mSin,
                        cx + (tickOuterR - minorTickLen) * mCos, cy + (tickOuterR - minorTickLen) * mSin, 1.5f);
                }
            }
        }

        // Needle (pointed triangle)
        float valueAngle = startAngle + sweepAngle * fraction;
        float valRad = valueAngle * (float)Math.PI / 180f;
        float needleLen = tickOuterR - majorTickLen - 4f;
        float needleBaseBack = needleLen * 0.18f;
        float needleWidth = radius * 0.035f;
        float cosV = (float)Math.Cos(valRad);
        float sinV = (float)Math.Sin(valRad);

        g.FillTriangle(Color.FromArgb(210, 45, 45),
            cx + needleBaseBack * -cosV + -needleWidth * sinV,
            cy + needleBaseBack * -sinV + needleWidth * cosV,
            cx + needleBaseBack * -cosV - -needleWidth * sinV,
            cy + needleBaseBack * -sinV - needleWidth * cosV,
            cx + needleLen * cosV, cy + needleLen * sinV);

        // Center hub
        float hubR1 = radius * 0.055f;
        float hubR2 = radius * 0.035f;
        g.FillEllipse(Color.FromArgb(50, 50, 55), cx - hubR1, cy - hubR1, hubR1 * 2, hubR1 * 2);
        g.FillEllipse(Color.FromArgb(190, 190, 195), cx - hubR2, cy - hubR2, hubR2 * 2, hubR2 * 2);
        g.FillEllipse(Color.FromArgb(70, 70, 75), cx - hubR2 * 0.5f, cy - hubR2 * 0.5f, hubR2, hubR2);

        // Value readout (positioned within the 60° gap at the bottom, below the needle tip)
        var valFont = new Font(ThemeManager.CurrentTheme.DefaultFont.Name,
            ThemeManager.CurrentTheme.DefaultFont.Size * 1.5f, FontStyle.Bold);
        var valLabel = FormatValue(value);
        var (vvw, vvh) = CoordinateTransform.MeasureText(valLabel, valFont, g.Zoom);
        float readoutY = cy + needleLen + 20f;
        float pillPad = 14f;
        float pillW = vvw + pillPad * 2;
        float pillH = vvh + pillPad;
        g.FillRectangle(Color.FromArgb(248, 248, 252), cx - pillW / 2f, readoutY - pillH / 2f, pillW, pillH);
        g.DrawRectangle(Color.FromArgb(180, 180, 185), cx - pillW / 2f, readoutY - pillH / 2f, pillW, pillH, 0.5f);
        g.DrawString(valLabel, valFont, Color.FromArgb(210, 45, 45), cx - vvw / 2f, readoutY - vvh / 2f);
    }

    private void RenderTrendLine(Graphics g)
    {
        var r = _chartAreaCached;
        var bounds = GetDataBounds();

        int maxPoints = 0;
        foreach (var s in _series) maxPoints = Math.Max(maxPoints, s.Points.Count);
        if (maxPoints == 0) return;

        float slotWidth = r.Width / (float)maxPoints;

        for (int si = 0; si < _series.Count; si++)
        {
            var s = _series[si];
            if (s.Points.Count < 2) continue;

            Color trendColor = s.Color ?? GetPaletteColor(si);

            var seriesPoints = new List<(double X, double Y)>();
            for (int i = 0; i < s.Points.Count; i++)
                seriesPoints.Add((i, s.Points[i].Value));

            double[] trendY;
            if (_trendLine.Type == TrendLineType.LinearRegression)
                trendY = CalculateLinearRegression(seriesPoints);
            else if (_trendLine.Type == TrendLineType.MovingAverage)
                trendY = CalculateMovingAverage(seriesPoints, _trendLine.MovingAveragePeriod);
            else
                continue;

            var linePoints = new List<(float X, float Y)>();
            for (int i = 0; i < trendY.Length; i++)
            {
                float cx = r.X + i * slotWidth + slotWidth / 2f;
                int y = (int)MapValueToPixel(trendY[i], bounds.MinValue, bounds.MaxValue,
                    r.Y + r.Height, r.Y, _yAxis.Type);
                linePoints.Add((cx, y));
            }

            float shadowOffset = 2.5f;
            Color shadowColor = Color.FromArgb(0, 0, 0, 80);
            Color outlineColor = Color.FromArgb(0, 0, 0, 160);

            for (int i = 1; i < linePoints.Count; i++)
            {
                float x1 = linePoints[i - 1].X;
                float y1 = linePoints[i - 1].Y;
                float x2 = linePoints[i].X;
                float y2 = linePoints[i].Y;

                g.DrawLine(shadowColor, x1 + shadowOffset, y1 + shadowOffset,
                    x2 + shadowOffset, y2 + shadowOffset, _trendLine.LineWidth + 2f);
                g.DrawLine(outlineColor, x1, y1, x2, y2, _trendLine.LineWidth + 4f);
                g.DrawLine(trendColor, x1, y1, x2, y2, _trendLine.LineWidth);
            }
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

        int itemHeight = 22;
        int swatchSize = 14;
        int startY = r.Y + 4;

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
        int itemHeight = 22;

        for (int i = 0; i < _series.Count; i++)
        {
            string name = string.IsNullOrEmpty(_series[i].Name) ? $"Series {i + 1}" : _series[i].Name;
            var (tw, _) = CoordinateTransform.MeasureText(name, font, g.Zoom);
            int itemWidth = tw + 25;
            if (itemWidth > maxWidth) maxWidth = itemWidth;
        }

        int pad = 10;
        int totalHeight = pad + _series.Count * itemHeight + pad;
        return (maxWidth + pad, totalHeight);
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
            if (_previousBindingList != null)
                _previousBindingList.ListChanged -= OnDataSourceListChanged;
            _previousBindingList = null;

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
                    _previousBindingList = list;
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
