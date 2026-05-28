using CoreForms.Ui.Core;
using CoreForms.Ui.Rendering;

namespace CoreForms.Ui.Reports;

/// <summary>
/// A cross-tab (pivot/matrix) report control that dynamically generates
/// row and column headers from data and displays aggregated values in cells.
/// </summary>
public class ReportCrossTab : ReportControl
{
    private readonly List<CrossTabField> _fields = new();

    /// <summary>
    /// Gets the list of field definitions (row, column, and value fields).
    /// </summary>
    public List<CrossTabField> Fields => _fields;

    /// <summary>
    /// Gets or sets the font used for row and column headers.
    /// If null, the effective font of the control is used.
    /// </summary>
    public Font? HeaderFont { get; set; }

    /// <summary>
    /// Gets or sets the background color for headers.
    /// </summary>
    public Color HeaderBackColor { get; set; } = Color.FromArgb(230, 240, 255);

    /// <summary>
    /// Gets or sets the background color for alternating data rows.
    /// Set to <see cref="Color.Transparent"/> to disable alternating.
    /// </summary>
    public Color AlternateRowColor { get; set; } = Color.Transparent;

    /// <summary>
    /// Gets or sets the background color for alternating column groups.
    /// Only applies when <see cref="ColumnField"/> elements exist.
    /// Set to <see cref="Color.Transparent"/> to disable.
    /// </summary>
    public Color AlternateColumnColor { get; set; } = Color.FromArgb(242, 247, 255);

    /// <summary>
    /// Gets or sets the horizontal padding inside each cell in cm.
    /// </summary>
    public Cm CellPadding { get; set; } = 0.1;

    /// <summary>
    /// Gets or sets the fixed height of the column header row in cm.
    /// </summary>
    public Cm ColumnHeaderHeight { get; set; } = 0.6;

    /// <summary>
    /// Gets or sets the fixed width of the row header column in cm.
    /// If 0, the width is auto-calculated from the longest row header text.
    /// </summary>
    public Cm RowHeaderWidth { get; set; }

    /// <summary>
    /// Gets or sets whether grid lines are drawn between cells.
    /// </summary>
    public bool ShowGridLines { get; set; } = true;

    /// <summary>
    /// Gets or sets the color of grid lines.
    /// </summary>
    public Color GridLineColor { get; set; } = Color.FromArgb(200, 200, 200);

    /// <summary>
    /// Gets or sets whether the output is rendered in grayscale.
    /// When true, all colors are desaturated.
    /// </summary>
    public bool GrayScale { get; set; }

    /// <summary>
    /// Gets or sets the vertical padding inside each data row in cm.
    /// </summary>
    public Cm RowPadding { get; set; } = 0.05;

    /// <summary>
    /// Gets or sets the background color for the value header sub-row
    /// when multiple value fields exist.
    /// </summary>
    public Color ValueHeaderBackColor { get; set; } = Color.FromArgb(245, 248, 255);

    /// <summary>
    /// Gets or sets the group field name. When set, the cross-tab only aggregates
    /// records whose <see cref="GroupField"/> matches <see cref="ReportRenderContext.CurrentGroupValue"/>.
    /// This is used to show per-group summaries in group footers.
    /// </summary>
    public string? GroupField { get; set; }

    /// <summary>
    /// Gets the effective header font, falling back to the control font.
    /// </summary>
    public Font EffectiveHeaderFont => HeaderFont ?? EffectiveFont;

    /// <summary>
    /// Measures the content height of the cross-tab by building the matrix
    /// from the report data source and calculating row count × row height.
    /// </summary>
    public override Cm MeasureContent(ReportRenderContext context)
    {
        var (rowKeys, colKeys, _) = BuildMatrix(context);
        if (rowKeys.Count == 0 && colKeys.Count == 0)
            return Height;

        Cm rowH = MeasureRowHeight(context);
        int rowCount = Math.Max(rowKeys.Count, 1);
        int valFieldCount = _fields.Count(f => f.Usage == FieldUsage.ValueField);
        int colFieldCount = _fields.Count(f => f.Usage == FieldUsage.ColumnField);
        Cm colH = colFieldCount > 0 ? ColumnHeaderHeight : Cm.Zero;
        Cm totalHeaderH = colH + (valFieldCount > 1 ? ColumnHeaderHeight * 0.6 : Cm.Zero);
        Cm needed = totalHeaderH + rowH * rowCount;
        return Cm.Max(Height, needed);
    }

    /// <summary>
    /// Renders the cross-tab matrix to the specified graphics surface.
    /// </summary>
    public override void Render(Graphics g, ReportRenderContext context, Cm actualTop, Cm actualHeight)
    {
        if (!Visible || _fields.Count == 0) return;

        DrawBackground(g, context, actualTop, actualHeight);

        var (rowKeys, colKeys, cellData) = BuildMatrix(context);
        if (rowKeys.Count == 0 && colKeys.Count == 0)
            return;

        var rowFields = _fields.Where(f => f.Usage == FieldUsage.RowField).ToList();
        var colFields = _fields.Where(f => f.Usage == FieldUsage.ColumnField).ToList();
        var valFields = _fields.Where(f => f.Usage == FieldUsage.ValueField).ToList();
        bool multiVal = valFields.Count > 1;
        bool hasColumnFields = colFields.Count > 0;

        Cm rowH = MeasureRowHeight(context);
        Cm rowHeaderW = RowHeaderWidth > Cm.Zero ? RowHeaderWidth : MeasureMaxRowHeaderWidth(rowKeys, context);
        Cm colHeaderH = hasColumnFields ? ColumnHeaderHeight : Cm.Zero;
        Cm subColHeaderH = multiVal ? ColumnHeaderHeight * 0.6 : Cm.Zero;
        Cm totalColH = colHeaderH + subColHeaderH;

        int colCount = colKeys.Count * valFields.Count;
        Cm dataW = Width - rowHeaderW;
        if (dataW <= Cm.Zero) dataW = Cm.FromMm(10);
        Cm colW = dataW / Math.Max(colCount, 1);

        float x0 = CmToPx(Left, context);
        float y0 = CmToPx(actualTop, context);
        float rhwPx = CmToPx(rowHeaderW, context);
        float cwPx = CmToPx(colW, context);
        float rhPx = CmToPx(rowH, context);
        float chPx = CmToPx(colHeaderH, context);
        float schPx = CmToPx(subColHeaderH, context);
        float tchPx = CmToPx(totalColH, context);
        float cellPadPx = CmToPx(CellPadding, context);

        Font headerFnt = EffectiveHeaderFont;
        Font dataFnt = EffectiveFont;
        Color headerBg = GrayScale ? Desaturate(HeaderBackColor) : HeaderBackColor;
        Color altRowBg = GrayScale ? Desaturate(AlternateRowColor) : AlternateRowColor;
        Color altColBg = GrayScale ? Desaturate(AlternateColumnColor) : AlternateColumnColor;
        Color gridColor = GrayScale ? Desaturate(GridLineColor) : GridLineColor;
        Color valHeaderBg = GrayScale ? Desaturate(ValueHeaderBackColor) : ValueHeaderBackColor;

        // --- Column headers ---
        // Top-left corner (above row headers, spanning row header width)
        if (hasColumnFields)
        {
            g.FillRectangle(headerBg, x0, y0, rhwPx, chPx);
            if (ShowGridLines)
                g.DrawRectangle(gridColor, x0, y0, rhwPx, chPx, 0.5f);
        }

        if (hasColumnFields)
        {
            // Main column header row (only when actual ColumnFields exist)
            for (int ci = 0; ci < colKeys.Count; ci++)
            {
                float cx = x0 + rhwPx + ci * valFields.Count * cwPx;
                float cw = valFields.Count * cwPx;

                if (ci % 2 == 1 && altColBg.A != 0)
                    g.FillRectangle(altColBg, cx, y0, cw, chPx);
                else
                    g.FillRectangle(headerBg, cx, y0, cw, chPx);
                if (ShowGridLines)
                    g.DrawRectangle(gridColor, cx, y0, cw, chPx, 0.5f);

                string colLabel = colKeys[ci] ?? string.Empty;
                DrawCellText(g, colLabel, headerFnt, ForeColor, cx, y0, cw, chPx, cellPadPx, TextAlignment.Center);
            }
        }

        // Value sub-headers (when multiple value fields exist)
        if (multiVal)
        {
            float subY = y0 + (hasColumnFields ? chPx : 0);
            for (int ci = 0; ci < colKeys.Count; ci++)
            {
                for (int vi = 0; vi < valFields.Count; vi++)
                {
                    float sx = x0 + rhwPx + (ci * valFields.Count + vi) * cwPx;
                    if (ci % 2 == 1 && altColBg.A != 0)
                        g.FillRectangle(altColBg, sx, subY, cwPx, schPx);
                    else
                        g.FillRectangle(valHeaderBg, sx, subY, cwPx, schPx);
                    if (ShowGridLines)
                        g.DrawRectangle(gridColor, sx, subY, cwPx, schPx, 0.5f);

                    DrawCellText(g, valFields[vi].DisplayHeader, dataFnt, ForeColor,
                        sx, subY, cwPx, schPx, cellPadPx, TextAlignment.Center);
                }
            }
        }

        // --- Data rows ---
        for (int ri = 0; ri < rowKeys.Count; ri++)
        {
            float ry = y0 + tchPx + ri * rhPx;
            bool isAlt = ri % 2 == 1 && altRowBg.A != 0;

            // Row header
            if (isAlt && altRowBg.A != 0)
                g.FillRectangle(altRowBg, x0, ry, rhwPx, rhPx);
            g.FillRectangle(headerBg, x0, ry, rhwPx, rhPx);
            if (ShowGridLines)
                g.DrawRectangle(gridColor, x0, ry, rhwPx, rhPx, 0.5f);

            DrawCellText(g, rowKeys[ri] ?? string.Empty, headerFnt, ForeColor,
                x0, ry, rhwPx, rhPx, cellPadPx, TextAlignment.Left);

            // Data cells
            for (int ci = 0; ci < colKeys.Count; ci++)
            {
                bool isAltCol = ci % 2 == 1 && altColBg.A != 0;
                for (int vi = 0; vi < valFields.Count; vi++)
                {
                    float cx = x0 + rhwPx + (ci * valFields.Count + vi) * cwPx;

                    if (isAltCol)
                        g.FillRectangle(altColBg, cx, ry, cwPx, rhPx);
                    else if (isAlt && altRowBg.A != 0)
                        g.FillRectangle(altRowBg, cx, ry, cwPx, rhPx);

                    if (ShowGridLines)
                        g.DrawRectangle(gridColor, cx, ry, cwPx, rhPx, 0.5f);

                    string cellKey = GetCellKey(rowKeys[ri], colKeys[ci]);
                    double value = 0;
                    if (cellData.TryGetValue((cellKey, vi), out var list) && list.Count > 0)
                        value = AggregateValues(list, valFields[vi].Aggregation);

                    string fmt = valFields[vi].Format;
                    string valText = FormatValueRaw(value, fmt);
                    Color valColor = GrayScale ? Desaturate(ForeColor) : ForeColor;
                    DrawCellText(g, valText, dataFnt, valColor,
                        cx, ry, cwPx, rhPx, cellPadPx, TextAlignment.Right);
                }
            }
        }
    }

    private (List<string> rowKeys, List<string> colKeys, Dictionary<(string cellKey, int valIdx), List<double>> cellData) BuildMatrix(ReportRenderContext context)
    {
        var result = new Dictionary<(string, int), List<double>>();
        var rowFieldSet = new HashSet<string>();
        var colFieldSet = new HashSet<string>();
        var rowFields = _fields.Where(f => f.Usage == FieldUsage.RowField).ToList();
        var colFields = _fields.Where(f => f.Usage == FieldUsage.ColumnField).ToList();
        var valFields = _fields.Where(f => f.Usage == FieldUsage.ValueField).ToList();

        if (rowFields.Count == 0 && colFields.Count == 0 && valFields.Count == 0)
            return (new List<string>(), new List<string>(), result);

        var records = GetRecords(context.Report.DataSource);
        if (!string.IsNullOrEmpty(GroupField) && context.CurrentGroupValue != null)
        {
            records = records.Where(r =>
            {
                var t = r.GetType();
                var p = t.GetProperty(GroupField);
                var v = p?.GetValue(r) ?? t.GetField(GroupField)?.GetValue(r);
                return Equals(v, context.CurrentGroupValue);
            }).ToList();
        }
        object? savedRecord = context.CurrentRecord;
        foreach (var record in records)
        {
            context.CurrentRecord = record;

            string rowKey = ComputeCompositeKey(rowFields, context);
            string colKey = ComputeCompositeKey(colFields, context);
            rowFieldSet.Add(rowKey);
            if (colFields.Count > 0)
            {
                if (!string.IsNullOrEmpty(colKey))
                    colFieldSet.Add(colKey);
            }
            else
            {
                colFieldSet.Add(colKey);
            }

            for (int vi = 0; vi < valFields.Count; vi++)
            {
                object? raw = context.GetFieldValue(valFields[vi].DataField);
                double val = ConvertToDouble(raw);
                string cellKey = GetCellKey(rowKey, colKey);
                var dictKey = (cellKey, vi);
                if (!result.TryGetValue(dictKey, out var list))
                {
                    list = new List<double>();
                    result[dictKey] = list;
                }
                list.Add(val);
            }
        }
        context.CurrentRecord = savedRecord;

        var rowKeys = rowFieldSet.OrderBy(x => x).ToList();
        var colKeys = colFieldSet.OrderBy(x => x).ToList();

        // Fill in zero values for missing combinations
        if (rowFields.Count > 0 && colFields.Count > 0)
        {
            foreach (var rk in rowKeys)
            {
                foreach (var ck in colKeys)
                {
                    string cellKey = GetCellKey(rk, ck);
                    for (int vi = 0; vi < valFields.Count; vi++)
                    {
                        var dictKey = (cellKey, vi);
                        if (!result.ContainsKey(dictKey))
                            result[dictKey] = new List<double> { 0 };
                    }
                }
            }
        }

        return (rowKeys, colKeys, result);
    }

    private static string ComputeCompositeKey(List<CrossTabField> fields, ReportRenderContext context)
    {
        if (fields.Count == 0) return string.Empty;
        var parts = new List<string>();
        foreach (var f in fields)
        {
            object? val = context.GetFieldValue(f.DataField);
            parts.Add(val?.ToString() ?? string.Empty);
        }
        return string.Join("|", parts);
    }

    private static string GetCellKey(string rowKey, string colKey) => rowKey + "|" + colKey;

    private static List<object> GetRecords(object? dataSource)
    {
        if (dataSource == null) return new List<object>();
        var records = new List<object>();
        if (dataSource is System.Collections.IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                if (item != null)
                    records.Add(item);
            }
        }
        return records;
    }

    private static double ConvertToDouble(object? value)
    {
        if (value == null) return 0;
        if (value is double d) return d;
        if (value is int iv) return iv;
        if (value is float fv) return fv;
        if (value is long lv) return lv;
        if (value is decimal dec) return (double)dec;
        if (value is short sv) return sv;
        if (value is byte bv) return bv;
        return 0;
    }

    private static double AggregateValues(List<double> values, CrossTabAggregation agg)
    {
        if (values.Count == 0) return 0;
        return agg switch
        {
            CrossTabAggregation.Sum => values.Sum(),
            CrossTabAggregation.Count => values.Count,
            CrossTabAggregation.Avg => values.Average(),
            CrossTabAggregation.Min => values.Min(),
            CrossTabAggregation.Max => values.Max(),
            _ => values.Sum()
        };
    }

    private Cm MeasureRowHeight(ReportRenderContext context)
    {
        float fontSizePx = EffectiveFont.Size * context.RenderDpi / 72f;
        float lineHeightPx = fontSizePx * 1.4f;
        float padPx = CmToPx(RowPadding * 2, context);
        return new Cm((lineHeightPx + padPx) * 2.54 / context.RenderDpi);
    }

    private Cm MeasureMaxRowHeaderWidth(List<string> rowKeys, ReportRenderContext context)
    {
        if (rowKeys.Count == 0) return Cm.FromMm(20);
        float maxW = 0;
        float fontSizePx = EffectiveHeaderFont.Size * context.RenderDpi / 72f;
        float charWidth = fontSizePx * 0.5f;
        foreach (var key in rowKeys)
        {
            float w = (key?.Length ?? 0) * charWidth + CmToPx(CellPadding * 2, context);
            if (w > maxW) maxW = w;
        }
        return new Cm(maxW * 2.54 / context.RenderDpi);
    }

    private static void DrawCellText(Graphics g, string text, Font font, Color color,
        float cx, float cy, float cw, float ch, float padPx, TextAlignment align)
    {
        if (string.IsNullOrEmpty(text)) return;

        var (tw, _) = g.MeasureString(text, font);
        float textX = align switch
        {
            TextAlignment.Right => cx + cw - padPx - tw,
            TextAlignment.Center => cx + cw / 2f - tw / 2f,
            _ => cx + padPx
        };

        float textY = cy + (ch - font.Size * 1.2f) / 2f;
        g.DrawString(text, font, color, textX, textY);
    }

    private static string FormatValueRaw(double value, string? format)
    {
        if (string.IsNullOrEmpty(format))
        {
            if (value == Math.Floor(value) && !double.IsInfinity(value))
                return value.ToString("0");
            return value.ToString("0.##");
        }
        string fmt = format.Contains("{0") ? format : "{0:" + format + "}";
        try { return string.Format(fmt, value); }
        catch { return value.ToString("0.##"); }
    }

    private static Color Desaturate(Color c)
    {
        int gray = (c.R * 77 + c.G * 151 + c.B * 28) / 256;
        return Color.FromArgb(c.A, gray, gray, gray);
    }

    /// <summary>
    /// Converts a Cm value to pixel float at the context's render DPI.
    /// </summary>
    private new float CmToPx(Cm value, ReportRenderContext context) => value.ToPixelF(context.RenderDpi);
}
