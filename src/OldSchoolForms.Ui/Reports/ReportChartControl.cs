using OldSchoolForms.Ui.Controls.Advanced;
using OldSchoolForms.Ui.Core;
using OldSchoolForms.Ui.Rendering;

namespace OldSchoolForms.Ui.Reports;

/// <summary>
/// A report control that renders business diagrams (bar, pie, line, area, gauge, etc.)
/// using the same chart types as <see cref="DiagramView"/>.
/// Supports multiple series, axes with scales, trend lines, and a legend.
/// </summary>
public class ReportChartControl : ReportControl
{
    private readonly DiagramViewSeriesCollection _series = new();
    private readonly DiagramViewAxisConfig _xAxis = new();
    private readonly DiagramViewAxisConfig _yAxis = new();
    private readonly DiagramViewTrendLine _trendLine = new();
    private Color[] _palette = new[]
    {
        Color.FromArgb(70, 130, 180), Color.FromArgb(220, 100, 80),
        Color.FromArgb(80, 170, 100), Color.FromArgb(230, 170, 60),
        Color.FromArgb(150, 90, 170), Color.FromArgb(60, 180, 190),
        Color.FromArgb(200, 120, 60), Color.FromArgb(120, 130, 150),
    };

    /// <summary>
    /// Gets or sets the type of chart to render.
    /// </summary>
    public DiagramType ChartType { get; set; } = DiagramType.Bar;

    /// <summary>
    /// Gets the collection of data series.
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
    /// Gets the trend line configuration.
    /// </summary>
    public DiagramViewTrendLine TrendLine => _trendLine;

    /// <summary>
    /// Gets or sets the legend position.
    /// </summary>
    public LegendPosition LegendPosition { get; set; } = LegendPosition.Right;

    /// <summary>
    /// Gets or sets whether the legend is displayed.
    /// </summary>
    public bool ShowLegend { get; set; } = true;

    /// <summary>
    /// Gets or sets the data label display style.
    /// </summary>
    public LabelStyle DataLabelStyle { get; set; } = LabelStyle.None;

    /// <summary>
    /// Gets or sets the optional chart title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the red zone upper threshold as a fraction (0-1) for gauge charts.
    /// </summary>
    public double GaugeRedThreshold { get; set; } = 0.25;

    /// <summary>
    /// Gets or sets the yellow zone upper threshold as a fraction (0-1) for gauge charts.
    /// </summary>
    public double GaugeYellowThreshold { get; set; } = 0.50;

    /// <summary>
    /// Gets or sets whether the chart is rendered in grayscale.
    /// </summary>
    public bool GrayScale { get; set; }

    /// <summary>
    /// Gets or sets the background color of the chart area.
    /// </summary>
    public Color DiagramBackColor { get; set; } = Color.FromArgb(248, 248, 252);

    /// <summary>
    /// Gets or sets the color of grid lines.
    /// </summary>
    public Color GridLineColor { get; set; } = Color.FromArgb(220, 220, 225);

    /// <summary>
    /// Gets or sets the color of axis lines.
    /// </summary>
    public Color AxisLineColor { get; set; } = Color.FromArgb(160, 160, 165);

    /// <summary>
    /// Gets or sets the color of axis labels.
    /// </summary>
    public Color AxisLabelColor { get; set; } = Color.FromArgb(60, 60, 65);

    /// <summary>
    /// Gets or sets the background color of the legend.
    /// </summary>
    public Color LegendBackColor { get; set; } = Color.FromArgb(248, 248, 252);

    /// <summary>
    /// Gets or sets the color palette used when a series has no explicit color.
    /// </summary>
    public Color[] Palette
    {
        get => _palette;
        set => _palette = value ?? Array.Empty<Color>();
    }

    /// <summary>
    /// Gets or sets the padding inside the chart area in cm (left, top, right, bottom).
    /// </summary>
    public (Cm Left, Cm Top, Cm Right, Cm Bottom) ChartPadding { get; set; } = (0.3, 0.3, 0.3, 0.3);

    /// <summary>
    /// Measures the content height based on the chart type and data.
    /// </summary>
    public override Cm MeasureContent(ReportRenderContext context)
    {
        if (!Visible || _series.Count == 0 || _series.All(s => s.Points.Count == 0))
            return Height;

        return Height > Cm.Zero ? Height : new Cm(5.0);
    }

    /// <summary>
    /// Renders the chart to the specified graphics surface.
    /// </summary>
    public override void Render(Graphics g, ReportRenderContext context, Cm actualTop, Cm actualHeight)
    {
        if (!Visible || Width <= Cm.Zero || actualHeight <= Cm.Zero) return;

        DrawBackground(g, context, actualTop, actualHeight);

        float pxX = CmToPx(Left, context);
        float pxY = CmToPx(actualTop, context);
        float pw = CmToPx(Width, context);
        float ph = CmToPx(actualHeight, context);

        float padL = CmToPx(ChartPadding.Left, context);
        float padT = CmToPx(ChartPadding.Top, context);
        float padR = CmToPx(ChartPadding.Right, context);
        float padB = CmToPx(ChartPadding.Bottom, context);

        float caX = pxX + padL, caY = pxY + padT;
        float caW = pw - padL - padR, caH = ph - padT - padB;

        if (caW <= 0 || caH <= 0) return;

        Color diagBg = GrayScale ? Desaturate(DiagramBackColor) : DiagramBackColor;
        Color gridCol = GrayScale ? Desaturate(GridLineColor) : GridLineColor;
        Color axisCol = GrayScale ? Desaturate(AxisLineColor) : AxisLineColor;
        Color axisLabelCol = GrayScale ? Desaturate(AxisLabelColor) : AxisLabelColor;
        Color legendBg = GrayScale ? Desaturate(LegendBackColor) : LegendBackColor;

        g.FillRectangle(diagBg, caX, caY, caW, caH);

        // Title
        float titleHeight = 0;
        if (!string.IsNullOrEmpty(Title))
        {
            var titleFont = new Font(EffectiveFont.Name, EffectiveFont.Size * 1.5f, FontStyle.Bold);
            var (tw, th) = g.MeasureString(Title, titleFont);
            float tx = caX + caW / 2f - tw / 2f;
            float ty = caY + 4;
            g.DrawString(Title, titleFont, axisLabelCol, tx, ty);
            titleHeight = th + 8;
        }

        float daX = caX, daY = caY + titleHeight, daW = caW, daH = caH - titleHeight;

        if (daH <= 0) return;

        // Legend
        float legX = 0, legY = 0, legW = 0, legH = 0;
        bool hasLegend = false;
        if (ShowLegend && _series.Count > 0)
        {
            hasLegend = true;
            MeasureAndPositionLegend(g, daX, daY, daW, daH, out legX, out legY, out legW, out legH);
            switch (LegendPosition)
            {
                case LegendPosition.Right:
                    daW = legX - daX;
                    break;
                case LegendPosition.Left:
                    daX += legW + 4;
                    daW -= legW + 4;
                    break;
                case LegendPosition.Top:
                    daY += legH + 4;
                    daH -= legH + 4;
                    break;
                case LegendPosition.Bottom:
                    daH -= legH + 4;
                    break;
            }
        }

        bool isGauge = ChartType == DiagramType.Gauge;
        bool isPie = ChartType == DiagramType.Pie || ChartType == DiagramType.Doughnut;
        bool isCircular = isGauge || isPie;

        // Axis layout
        int yAxisWidth = 0;
        if (!isCircular && YAxis.Visible)
        {
            var bounds = GetDataBounds();
            var font = EffectiveFont;
            int tickCount = EstimateTickCount(bounds.Max - bounds.Min, YAxis.TickInterval);
            float maxLabelWidth = 0;
            for (int i = 0; i <= tickCount; i++)
            {
                double val = bounds.Min + i * (bounds.Max - bounds.Min) / Math.Max(tickCount, 1);
                var label = FormatChartValue(val);
                var (lw, _) = g.MeasureString(label, font);
                if (lw > maxLabelWidth) maxLabelWidth = lw;
            }
            yAxisWidth = (int)maxLabelWidth + 20;

            if (!string.IsNullOrEmpty(YAxis.Title))
            {
                var (tw, _) = g.MeasureString(YAxis.Title, font);
                yAxisWidth += (int)tw + 5;
            }
        }

        int xAxisHeight = 0;
        if (!isCircular && XAxis.Visible)
        {
            var font = EffectiveFont;
            float maxLabelHeight = 0;
            foreach (var s in _series)
            {
                foreach (var pt in s.Points)
                {
                    if (pt.Label != null)
                    {
                        var (_, lh) = g.MeasureString(pt.Label, font);
                        if (lh > maxLabelHeight) maxLabelHeight = lh;
                    }
                }
            }
            xAxisHeight = (int)maxLabelHeight + 12;

            if (!string.IsNullOrEmpty(XAxis.Title))
            {
                var (_, th) = g.MeasureString(XAxis.Title, font);
                xAxisHeight += (int)th + 4;
            }
        }

        float paX = daX + yAxisWidth, paY = daY;
        float paW = daW - yAxisWidth, paH = daH - xAxisHeight;

        if (paW <= 0 || paH <= 0) return;

        if (!isCircular)
            RenderGridLines(g, paX, paY, paW, paH, gridCol);

        switch (ChartType)
        {
            case DiagramType.Bar:
                RenderBarChart(g, paX, paY, paW, paH, false);
                break;
            case DiagramType.StackedBar:
                RenderBarChart(g, paX, paY, paW, paH, true);
                break;
            case DiagramType.Pie:
                RenderPieChart(g, paX, paY, paW, paH, false);
                break;
            case DiagramType.Doughnut:
                RenderPieChart(g, paX, paY, paW, paH, true);
                break;
            case DiagramType.Line:
                RenderLineChart(g, paX, paY, paW, paH, false);
                break;
            case DiagramType.Area:
                RenderLineChart(g, paX, paY, paW, paH, true);
                break;
            case DiagramType.Gauge:
                RenderGauge(g, paX, paY, paW, paH);
                break;
        }

        if (TrendLine.Type != TrendLineType.None && _series.Count > 0 && !isCircular)
            RenderTrendLine(g, paX, paY, paW, paH);

        if (!isCircular)
            RenderAxes(g, paX, paY, paW, paH, yAxisWidth, xAxisHeight, axisCol, axisLabelCol);

        if (hasLegend)
            RenderLegend(g, legX, legY, legW, legH, legendBg, axisCol, axisLabelCol);

        g.DrawRectangle(axisCol, caX, caY, caW, caH, 0.5f);
    }

    private void RenderBarChart(Graphics g, float paX, float paY, float paW, float paH, bool stacked)
    {
        var bounds = GetDataBounds();
        int maxPoints = 0;
        foreach (var s in _series) maxPoints = Math.Max(maxPoints, s.Points.Count);
        if (maxPoints == 0) return;

        float slotWidth = paW / maxPoints;
        float groupWidth = slotWidth * 0.7f;
        float barWidth = stacked
            ? groupWidth * 0.85f
            : groupWidth / Math.Max(_series.Count, 1) * 0.85f;

        for (int j = 0; j < maxPoints; j++)
        {
            float slotCenterX = paX + j * slotWidth + slotWidth / 2f;
            double stackedTop = 0, stackedBottom = 0;

            for (int si = 0; si < _series.Count; si++)
            {
                var s = _series[si];
                if (j >= s.Points.Count) continue;
                var pt = s.Points[j];
                var seriesType = s.DiagramType ?? ChartType;
                bool isStacked = seriesType == DiagramType.StackedBar || stacked;

                double val = pt.Value;
                Color barColor = ResolveColor(pt.Color ?? s.Color, si);

                float barX = isStacked
                    ? slotCenterX - barWidth / 2f
                    : slotCenterX - (_series.Count * barWidth) / 2f + si * barWidth;

                double barTopVal = isStacked ? stackedTop : 0;
                double barBottomVal = val > 0 ? 0 : val;
                double barHeightVal = Math.Abs(val);

                if (isStacked)
                {
                    if (val >= 0) { barBottomVal = stackedTop; stackedTop += val; }
                    else { barBottomVal = stackedBottom; stackedBottom += val; barTopVal = val; }
                }

                float pixelTop = MapValueToPixelF(barTopVal + barHeightVal,
                    bounds.Min, bounds.Max, paY + paH, paY);
                float pixelBottom = MapValueToPixelF(barTopVal,
                    bounds.Min, bounds.Max, paY + paH, paY);

                if (pixelTop >= paY && pixelBottom <= paY + paH)
                {
                    g.FillRectangle(barColor, barX, pixelTop, barWidth, pixelBottom - pixelTop);
                    g.DrawRectangle(Color.White, barX, pixelTop, barWidth, pixelBottom - pixelTop, 1f);

                    if (DataLabelStyle == LabelStyle.Value)
                    {
                        var label = FormatChartValue(val);
                        var (lw, lh) = g.MeasureString(label, EffectiveFont);
                        float labelY;
                        if (isStacked)
                        {
                            // Stacked: label centered inside the segment
                            labelY = pixelTop + (pixelBottom - pixelTop) / 2f - lh / 2f;
                        }
                        else
                        {
                            // Normal bar: label above the bar
                            labelY = pixelTop - lh - 2;
                        }
                        g.DrawString(label, EffectiveFont, AxisLabelColor,
                            barX + barWidth / 2f - lw / 2f, labelY);
                    }
                }
            }
        }
    }

    private void RenderPieChart(Graphics g, float paX, float paY, float paW, float paH, bool doughnut)
    {
        int totalPoints = 0;
        foreach (var s in _series) totalPoints += s.Points.Count;
        if (totalPoints == 0) return;

        double totalValue = 0;
        foreach (var s in _series)
            foreach (var pt in s.Points)
                totalValue += Math.Max(pt.Value, 0);
        if (totalValue <= 0) return;

        float cx = paX + paW / 2f;
        float cy = paY + paH / 2f;
        float diameter = Math.Min(paW, paH) * 0.85f;
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
                Color color = ResolveColor(pt.Color ?? s.Color, pointIndex);

                g.FillPie(color, px, py, diameter, diameter, currentAngle, sweep - sliceGap);

                if (DataLabelStyle != LabelStyle.None)
                {
                    float midAngle = currentAngle + (sweep - sliceGap) / 2f;
                    float pieRadius = diameter / 2f;
                    float midRad = midAngle * (float)Math.PI / 180f;

                    string labelText = DataLabelStyle switch
                    {
                        LabelStyle.Value => pt.Label != null
                            ? $"{pt.Label}: {FormatChartValue(pt.Value)}"
                            : FormatChartValue(pt.Value),
                        LabelStyle.Percentage => pt.Label != null
                            ? $"{pt.Label}: {pt.Value / totalValue * 100:F1}%"
                            : $"{pt.Value / totalValue * 100:F1}%",
                        LabelStyle.Category => pt.Label ?? "",
                        _ => pt.Label ?? ""
                    };

                    if (!string.IsNullOrEmpty(labelText))
                    {
                        var font = EffectiveFont;
                        var (lw, lh) = g.MeasureString(labelText, font);
                        float labelRadius = pieRadius * 0.70f;
                        float sweepRad = (sweep - sliceGap) * (float)Math.PI / 180f;
                        float chordWidth = 2f * labelRadius * (float)Math.Sin(sweepRad / 2f);
                        float labelPadding = 6f;

                        if (lw + labelPadding < chordWidth)
                        {
                            float lx = cx + labelRadius * (float)Math.Cos(midRad);
                            float ly = cy + labelRadius * (float)Math.Sin(midRad);
                            g.DrawString(labelText, font, AxisLabelColor, lx - lw / 2f, ly - lh / 2f);
                        }
                        else
                        {
                            float edgeRadius = pieRadius * 0.52f;
                            float startX = cx + edgeRadius * (float)Math.Cos(midRad);
                            float startY = cy + edgeRadius * (float)Math.Sin(midRad);
                            float leaderLen = pieRadius * 0.18f;
                            float endX = startX + leaderLen * (float)Math.Cos(midRad);
                            float endY = startY + leaderLen * (float)Math.Sin(midRad);
                            bool isLeft = midAngle > 90 && midAngle < 270;
                            float textX = isLeft ? endX - lw - 4 : endX + 4;
                            g.DrawLine(AxisLabelColor, startX, startY, endX, endY, 0.5f);
                            g.DrawString(labelText, font, AxisLabelColor, textX, endY - lh / 2f);
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
            g.FillEllipse(DiagramBackColor, cx - holeDiameter / 2f, cy - holeDiameter / 2f,
                holeDiameter, holeDiameter);
        }
    }

    private void RenderLineChart(Graphics g, float paX, float paY, float paW, float paH, bool area)
    {
        var bounds = GetDataBounds();
        int maxPoints = 0;
        foreach (var s in _series) maxPoints = Math.Max(maxPoints, s.Points.Count);
        if (maxPoints == 0) return;

        float slotWidth = paW / maxPoints;

        for (int si = 0; si < _series.Count; si++)
        {
            var s = _series[si];
            if (s.Points.Count < 2) continue;

            Color color = ResolveColor(s.Color, si);
            var points = new List<(float X, float Y)>();

            for (int j = 0; j < s.Points.Count; j++)
            {
                var pt = s.Points[j];
                float cx = paX + j * slotWidth + slotWidth / 2f;
                float y = MapValueToPixelF(pt.Value, bounds.Min, bounds.Max,
                    paY + paH, paY);
                points.Add((cx, y));
            }

            for (int i = 1; i < points.Count; i++)
                g.DrawLine(color, points[i - 1].X, points[i - 1].Y, points[i].X, points[i].Y, 2f);

            foreach (var (pX, pY) in points)
            {
                g.FillEllipse(Color.White, pX - 4f, pY - 4f, 8f, 8f);
                g.DrawEllipse(color, pX - 4f, pY - 4f, 8f, 8f, 2f);
            }

            if (area)
            {
                int baselineY = (int)(paY + paH);
                var polyPoints = new float[points.Count * 4];
                int idx = 0;
                foreach (var (pX, pY) in points) { polyPoints[idx++] = pX; polyPoints[idx++] = pY; }
                for (int i = points.Count - 1; i >= 0; i--)
                { polyPoints[idx++] = points[i].X; polyPoints[idx++] = baselineY; }

                var fillColor = Color.FromArgb(
                    color.R + (255 - color.R) * 3 / 5,
                    color.G + (255 - color.G) * 3 / 5,
                    color.B + (255 - color.B) * 3 / 5);
                g.FillPolygon(fillColor, polyPoints);
            }

            if (DataLabelStyle == LabelStyle.Value)
            {
                for (int j = 0; j < s.Points.Count; j++)
                {
                    var pt = s.Points[j];
                    var label = FormatChartValue(pt.Value);
                    var (lw, lh) = g.MeasureString(label, EffectiveFont);
                    float cx = paX + j * slotWidth + slotWidth / 2f;
                    float y = MapValueToPixelF(pt.Value, bounds.Min, bounds.Max,
                        paY + paH, paY);
                    g.DrawString(label, EffectiveFont, AxisLabelColor, cx - lw / 2f, y - lh - 8);
                }
            }
        }
    }

    private void RenderGridLines(Graphics g, float paX, float paY, float paW, float paH, Color color)
    {
        var bounds = GetDataBounds();
        double interval = YAxis.TickInterval > 0
            ? YAxis.TickInterval
            : ComputeNiceInterval(bounds.Max - bounds.Min);
        double start = Math.Ceiling(bounds.Min / interval) * interval;

        for (double v = start; v <= bounds.Max; v += interval)
        {
            float y = MapValueToPixelF(v, bounds.Min, bounds.Max,
                paY + paH, paY);
            if (y >= paY && y <= paY + paH)
                g.DrawLine(color, paX, y, paX + paW, y, 0.5f);
        }
    }

    private void RenderAxes(Graphics g, float paX, float paY, float paW, float paH, int yAxisWidth, int xAxisHeight,
        Color axisColor, Color labelColor)
    {
        var bounds = GetDataBounds();
        var font = EffectiveFont;

        // Y-axis
        if (YAxis.Visible)
        {
            if (YAxis.ShowAxisLine)
                g.DrawLine(axisColor, paX, paY, paX, paY + paH, 1f);

            double interval = YAxis.TickInterval > 0
                ? YAxis.TickInterval
                : ComputeNiceInterval(bounds.Max - bounds.Min);
            double tickStart = Math.Ceiling(bounds.Min / interval) * interval;

            for (double v = tickStart; v <= bounds.Max + 0.001; v += interval)
            {
                float y = MapValueToPixelF(v, bounds.Min, bounds.Max,
                    paY + paH, paY);

                if (YAxis.ShowLabels)
                {
                    var label = FormatChartValue(v);
                    var (lw, lh) = g.MeasureString(label, font);
                    float lx = paX - lw - 6;
                    float ly = y - lh / 2f;
                    ly = Math.Clamp(ly, paY, paY + paH - lh);
                    g.DrawString(label, font, labelColor, lx, ly);
                }

                if (XAxis.ShowGridLines && y >= paY && y <= paY + paH)
                    g.DrawLine(GridLineColor, paX, y, paX + paW, y, 0.5f);
            }

            if (!string.IsNullOrEmpty(YAxis.Title))
            {
                var (_, th) = g.MeasureString(YAxis.Title, font);
                float ty = paY + paH / 2f - th;
                float tx = paX - yAxisWidth + 4;
                g.DrawString(YAxis.Title, font, labelColor, tx, ty);
            }
        }

        // X-axis
        if (XAxis.Visible)
        {
            int maxPoints = 0;
            foreach (var s in _series) maxPoints = Math.Max(maxPoints, s.Points.Count);

            if (XAxis.ShowAxisLine)
                g.DrawLine(axisColor, paX, paY + paH, paX + paW, paY + paH, 1f);

            if (maxPoints > 0 && XAxis.ShowLabels)
            {
                float slotWidth = paW / maxPoints;
                foreach (var s in _series)
                {
                    for (int j = 0; j < s.Points.Count; j++)
                    {
                        var pt = s.Points[j];
                        if (pt.Label == null) continue;
                        float cx = paX + j * slotWidth + slotWidth / 2f;
                        float ly = paY + paH + 4;
                        var (tw, _) = g.MeasureString(pt.Label, font);
                        g.DrawString(pt.Label, font, labelColor, cx - tw / 2f, ly);
                    }
                    break;
                }
            }

            if (!string.IsNullOrEmpty(XAxis.Title))
            {
                var (tw, _) = g.MeasureString(XAxis.Title, font);
                float tx = paX + paW / 2f - tw / 2f;
                float ty = paY + paH + (maxPoints > 0 ? 20 : 4);
                g.DrawString(XAxis.Title, font, labelColor, tx, ty);
            }
        }
    }

    private void RenderGauge(Graphics g, float paX, float paY, float paW, float paH)
    {
        double minVal = YAxis.MinValue ?? 0;
        double maxVal = YAxis.MaxValue ?? 100;
        double value = 0;
        if (_series.Count > 0 && _series[0].Points.Count > 0)
            value = _series[0].Points[0].Value;
        value = Math.Clamp(value, minVal, maxVal);

        float fraction = (float)((value - minVal) / (maxVal - minVal));
        double redThresh = Math.Clamp(GaugeRedThreshold, 0, 1);
        double yellowThresh = Math.Clamp(GaugeYellowThreshold, redThresh, 1);

        float cx = paX + paW / 2f;
        float cy = paY + paH / 2f;
        float diameter = Math.Min(paW, paH) * 0.88f;
        float radius = diameter / 2f;
        float px = cx - radius;
        float py = cy - radius;

        float startAngle = 90f;
        float sweepAngle = 300f;
        float bezelWidth = 8f;

        g.DrawArc(Color.FromArgb(45, 45, 50), px - bezelWidth, py - bezelWidth,
            diameter + bezelWidth * 2, diameter + bezelWidth * 2, 0, 360, bezelWidth);
        g.DrawArc(Color.FromArgb(80, 80, 85), px - bezelWidth + 2f, py - bezelWidth + 2f,
            diameter + bezelWidth * 2 - 4f, diameter + bezelWidth * 2 - 4f, 0, 360, 1f);

        float faceInset = 5f;
        g.FillEllipse(Color.FromArgb(248, 248, 252), px + faceInset, py + faceInset,
            diameter - faceInset * 2, diameter - faceInset * 2);

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
            if (GrayScale) zoneColor = Desaturate(zoneColor);
            g.DrawArc(zoneColor, cx - zoneMidR, cy - zoneMidR, zoneMidR * 2, zoneMidR * 2,
                aStart, aSweep, zoneThick);
        }

        DrawZoneArc(Color.FromArgb(241, 90, 96), startAngle, redSweep);
        DrawZoneArc(Color.FromArgb(237, 176, 32), yellowStart, yellowSweep);
        DrawZoneArc(Color.FromArgb(112, 173, 71), greenStart, greenSweep);

        int majorTicks = 8;
        int minorTicks = 4;
        var tickFont = EffectiveFont;
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
            string tickLabel = FormatChartValue(tickVal);
            var (tlw, tlh) = g.MeasureString(tickLabel, tickFont);
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

        float hubR1 = radius * 0.055f;
        float hubR2 = radius * 0.035f;
        g.FillEllipse(Color.FromArgb(50, 50, 55), cx - hubR1, cy - hubR1, hubR1 * 2, hubR1 * 2);
        g.FillEllipse(Color.FromArgb(190, 190, 195), cx - hubR2, cy - hubR2, hubR2 * 2, hubR2 * 2);
        g.FillEllipse(Color.FromArgb(70, 70, 75), cx - hubR2 * 0.5f, cy - hubR2 * 0.5f, hubR2, hubR2);

        var valFont = new Font(EffectiveFont.Name, EffectiveFont.Size * 1.5f, FontStyle.Bold);
        var valLabel = FormatChartValue(value);
        var (vvw, vvh) = g.MeasureString(valLabel, valFont);
        float readoutY = cy + needleLen + 20f;
        float pillPad = 14f;
        float pillW = vvw + pillPad * 2;
        float pillH = vvh + pillPad;
        g.FillRectangle(Color.FromArgb(248, 248, 252), cx - pillW / 2f, readoutY - pillH / 2f, pillW, pillH);
        g.DrawRectangle(Color.FromArgb(180, 180, 185), cx - pillW / 2f, readoutY - pillH / 2f, pillW, pillH, 0.5f);
        g.DrawString(valLabel, valFont, Color.FromArgb(210, 45, 45), cx - vvw / 2f, readoutY - vvh / 2f);
    }

    private void RenderTrendLine(Graphics g, float paX, float paY, float paW, float paH)
    {
        var bounds = GetDataBounds();
        int maxPoints = 0;
        foreach (var s in _series) maxPoints = Math.Max(maxPoints, s.Points.Count);
        if (maxPoints == 0) return;

        float slotWidth = paW / maxPoints;

        for (int si = 0; si < _series.Count; si++)
        {
            var s = _series[si];
            if (s.Points.Count < 2) continue;

            Color trendColor = ResolveColor(s.Color, si);
            var seriesPoints = new List<(double X, double Y)>();
            for (int i = 0; i < s.Points.Count; i++)
                seriesPoints.Add((i, s.Points[i].Value));

            double[] trendY;
            if (TrendLine.Type == TrendLineType.LinearRegression)
                trendY = CalculateLinearRegression(seriesPoints);
            else if (TrendLine.Type == TrendLineType.MovingAverage)
                trendY = CalculateMovingAverage(seriesPoints, TrendLine.MovingAveragePeriod);
            else
                continue;

            var linePoints = new List<(float X, float Y)>();
            for (int i = 0; i < trendY.Length; i++)
            {
                float cx = paX + i * slotWidth + slotWidth / 2f;
                float y = MapValueToPixelF(trendY[i], bounds.Min, bounds.Max,
                    paY + paH, paY);
                linePoints.Add((cx, y));
            }

            float shadowOffset = 2.5f;
            Color shadowColor = Color.FromArgb(0, 0, 0, 80);
            Color outlineColor = Color.FromArgb(0, 0, 0, 160);

            for (int i = 1; i < linePoints.Count; i++)
            {
                float x1 = linePoints[i - 1].X, y1 = linePoints[i - 1].Y;
                float x2 = linePoints[i].X, y2 = linePoints[i].Y;
                g.DrawLine(shadowColor, x1 + shadowOffset, y1 + shadowOffset,
                    x2 + shadowOffset, y2 + shadowOffset, TrendLine.LineWidth + 2f);
                g.DrawLine(outlineColor, x1, y1, x2, y2, TrendLine.LineWidth + 4f);
                g.DrawLine(trendColor, x1, y1, x2, y2, TrendLine.LineWidth);
            }
        }
    }

    private void MeasureAndPositionLegend(Graphics g, float daX, float daY, float daW, float daH,
        out float legX, out float legY, out float legW, out float legH)
    {
        int itemHeight = 22;
        int pad = 10;

        var font = EffectiveFont;
        float maxWidth = 0;
        for (int i = 0; i < _series.Count; i++)
        {
            string name = string.IsNullOrEmpty(_series[i].Name) ? $"Series {i + 1}" : _series[i].Name;
            var (tw, _) = g.MeasureString(name, font);
            if (tw + 25 > maxWidth) maxWidth = tw + 25;
        }

        float legendW = maxWidth + pad;
        float legendH = pad + _series.Count * itemHeight + pad;

        switch (LegendPosition)
        {
            case LegendPosition.Right:
                legX = daX + daW - legendW - 4; legY = daY; legW = legendW; legH = legendH; break;
            case LegendPosition.Left:
                legX = daX; legY = daY; legW = legendW; legH = legendH; break;
            case LegendPosition.Top:
                legX = daX; legY = daY; legW = daW; legH = legendH; break;
            case LegendPosition.Bottom:
                legX = daX; legY = daY + daH - legendH; legW = daW; legH = legendH; break;
            default:
                legX = 0; legY = 0; legW = 0; legH = 0; break;
        }
    }

    private void RenderLegend(Graphics g, float legX, float legY, float legW, float legH,
        Color bgColor, Color borderColor, Color labelColor)
    {
        g.FillRectangle(bgColor, legX, legY, legW, legH);
        g.DrawRectangle(borderColor, legX, legY, legW, legH, 0.5f);

        int itemHeight = 22;
        int swatchSize = 14;
        var font = EffectiveFont;

        for (int i = 0; i < _series.Count; i++)
        {
            var s = _series[i];
            float y = legY + 4 + i * itemHeight;
            if (y + itemHeight > legY + legH) break;

            Color color = ResolveColor(s.Color, i);
            g.FillRectangle(color, legX + 5, y + (itemHeight - swatchSize) / 2, swatchSize, swatchSize);

            string name = string.IsNullOrEmpty(s.Name) ? $"Series {i + 1}" : s.Name;
            var (tw, th) = g.MeasureString(name, font);
            g.DrawString(name, font, labelColor, legX + 5 + swatchSize + 4, y + (itemHeight - th) / 2f);
        }
    }

    private (double Min, double Max) GetDataBounds()
    {
        bool hasData = false;
        double min = double.MaxValue, max = double.MinValue;
        bool isStacked = ChartType == DiagramType.StackedBar;

        if (isStacked)
        {
            int maxPoints = 0;
            foreach (var s in _series) maxPoints = Math.Max(maxPoints, s.Points.Count);
            for (int j = 0; j < maxPoints; j++)
            {
                double sum = 0;
                foreach (var s in _series)
                {
                    if (j < s.Points.Count) { sum += Math.Max(s.Points[j].Value, 0); hasData = true; }
                }
                if (sum > max) max = sum;
            }
            min = 0;
        }
        else
        {
            foreach (var s in _series)
            {
                foreach (var pt in s.Points)
                {
                    hasData = true;
                    if (pt.Value < min) min = pt.Value;
                    if (pt.Value > max) max = pt.Value;
                }
            }
        }

        if (!hasData) return (0, 100);
        if (YAxis.MinValue.HasValue) min = YAxis.MinValue.Value;
        if (YAxis.MaxValue.HasValue) max = YAxis.MaxValue.Value;
        if (Math.Abs(max - min) < 0.001) max = min + 100;

        double margin = (max - min) * 0.1;
        if (!YAxis.MinValue.HasValue && min >= 0) min = 0;
        else if (!YAxis.MinValue.HasValue) min -= margin;
        if (!YAxis.MaxValue.HasValue) max += margin;

        double interval = ComputeNiceInterval(max - min);
        if (interval > 0 && !YAxis.MaxValue.HasValue)
            max = Math.Ceiling(max / interval) * interval;
        if (interval > 0 && !YAxis.MinValue.HasValue && min < 0)
            min = Math.Floor(min / interval) * interval;

        return (min, max);
    }

    private float MapValueToPixelF(double value, double minValue, double maxValue, float pixelMin, float pixelMax)
    {
        if (Math.Abs(maxValue - minValue) < 0.001) return pixelMin;

        if (YAxis.Type == AxisType.Logarithmic)
        {
            double logMin = Math.Log10(Math.Max(minValue, 1));
            double logMax = Math.Log10(Math.Max(maxValue, 1));
            double logVal = Math.Log10(Math.Max(value, 1));
            if (Math.Abs(logMax - logMin) < 0.001) return pixelMin;
            return pixelMin + (float)((logVal - logMin) / (logMax - logMin) * (pixelMax - pixelMin));
        }

        return pixelMin + (float)((value - minValue) / (maxValue - minValue) * (pixelMax - pixelMin));
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
        return (normalized <= 1.5 ? 1 : normalized <= 3.5 ? 2 : normalized <= 7.5 ? 5 : 10) * magnitude;
    }

    private static string FormatChartValue(double value)
    {
        if (Math.Abs(value) >= 1_000_000) return $"{value / 1_000_000:F2}M";
        if (Math.Abs(value) >= 1_000) return $"{value / 1_000:F2}K";
        return value.ToString("#,##0.#");
    }

    private static double[] CalculateLinearRegression(List<(double X, double Y)> points)
    {
        int n = points.Count;
        if (n < 2) return points.Select(p => p.Y).ToArray();
        double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;
        foreach (var (x, y) in points) { sumX += x; sumY += y; sumXY += x * y; sumX2 += x * x; }
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
            for (int j = Math.Max(0, i - period + 1); j <= i; j++) { sum += points[j].Y; count++; }
            result[i] = sum / count;
        }
        return result;
    }

    private Color ResolveColor(Color? seriesColor, int index)
    {
        Color c = seriesColor ?? _palette[index % _palette.Length];
        return GrayScale ? Desaturate(c) : c;
    }

    private static Color Desaturate(Color c)
    {
        int gray = (c.R * 77 + c.G * 151 + c.B * 28) / 256;
        return Color.FromArgb(c.A, gray, gray, gray);
    }

    private new float CmToPx(Cm value, ReportRenderContext context) => value.ToPixelF(context.RenderDpi);
}
