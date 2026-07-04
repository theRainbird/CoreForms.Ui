using System.ComponentModel;
using System.Collections;
using OldSchoolForms.Ui.Core;
using OldSchoolForms.Ui.Data;
using OldSchoolForms.Ui.Theming;
using Graphics = OldSchoolForms.Ui.Rendering.Graphics;

namespace OldSchoolForms.Ui.Controls.Advanced;

/// <summary>
/// Displays data in a cross-tabulated (pivot) format with row and column dimensions,
/// aggregated value cells, and interactive drilldown capability.
/// Supports hierarchical row/column fields, multiple aggregation types, and drill-through.
/// </summary>
public class PivotTable : ContainerControl
{
    // Core collections
    private readonly PivotTableFieldCollection _fields;
    private object? _dataSource;
    private string _dataMember = string.Empty;
    private IBindingList? _previousBindingList;
    private List<object> _records = new();
    private bool _dataSourceUpdating;

    // Matrix engine
    private readonly PivotTableMatrix _matrix = new();
    private bool _matrixDirty = true;
    private bool _rebuildPending;
    private readonly Dictionary<string, object?> _filterValues = new(StringComparer.OrdinalIgnoreCase);

    // Layout constants
    private const int DataRowHeight = 26;
    private const int HeaderRowHeight = 28;
    private const int IndentWidth = 18;
    private const int DrillButtonSize = 14;
    private const int CellPadding = 4;
    private const int MinColumnWidth = 60;

    // Layout state
    private int _rowHeaderWidth = 150;
    private int _totalDataWidth;
    private int _totalDataHeight;

    // Scroll
    private readonly ScrollBarEngine _vScrollBar = new();
    private readonly ScrollBarEngine _hScrollBar = new();

    // Selection
    private int _selectedRowIndex = -1;
    private int _selectedColumnIndex = -1;
    private int _selectedValueIndex;

    // Sort
    private int _sortFieldIndex = -1;
    private bool _sortAscending = true;

    // Column resize
    private const int DividerThreshold = 5;
    private const int MinColWidth = 40;
    private int _resizingColumnIndex = -1;
    private int _resizeStartMouseX;
    private int _resizeStartWidth;
    private readonly Dictionary<int, int> _columnWidthOverrides = new();

    // Filter dropdown state
    private bool _isFilterDropdownOpen;

    private bool _headerBgColorSet;
    private bool _headerTextColorSet;
    private bool _totalBgColorSet;
    private bool _altRowColorSet;
    private bool _gridLineColorSet;
    private bool _selCellColorSet;

    /// <summary>
    /// Initializes a new instance of <see cref="PivotTable"/>.
    /// </summary>
    public PivotTable()
    {
        _fields = new PivotTableFieldCollection(OnFieldsChanged);

        var theme = ThemeManager.CurrentTheme;
        ApplyTheme(theme);
        Size = new Size(600, 400);
        TabStop = true;

        _vScrollBar.SmallChange = DataRowHeight;
        _vScrollBar.LargeChange = DataRowHeight * 5;
        _vScrollBar.Scroll += (s, e) => Invalidate();

        _hScrollBar.Orientation = ScrollBarEngine.ScrollBarOrientation.Horizontal;
        _hScrollBar.SmallChange = 20;
        _hScrollBar.LargeChange = 100;
        _hScrollBar.Scroll += (s, e) => Invalidate();
    }

    private void ApplyTheme(Theme theme)
    {
        if (!_backColorSet) _backColor = theme.WindowBackground;
        if (!_foreColorSet) _foreColor = theme.ControlText;
        if (!_headerBgColorSet) _headerBgColor = theme.PivotTableHeaderBackground;
        if (!_headerTextColorSet) _headerTextColor = theme.PivotTableHeaderText;
        if (!_totalBgColorSet) _totalBgColor = theme.PivotTableTotalBackground;
        if (!_altRowColorSet) _alternateRowColor = theme.PivotTableAlternateRow;
        if (!_gridLineColorSet) _gridLineColor = theme.PivotTableGridLine;
        if (!_selCellColorSet) _selectedCellColor = theme.PivotTableSelection;
    }

    /// <inheritdoc />
    public override void OnThemeChanged(Theme newTheme)
    {
        ApplyTheme(newTheme);
        Invalidate();
    }

    // ──────────────────────────────────────────────
    //  Public Properties
    // ──────────────────────────────────────────────

    /// <summary>
    /// Gets the collection of field definitions (row, column, value, and filter fields).
    /// </summary>
    public PivotTableFieldCollection Fields => _fields;

    /// <summary>
    /// Gets or sets the data source for the pivot table.
    /// Supports <see cref="IEnumerable"/>, <see cref="IList"/>,
    /// <see cref="IBindingList"/>, and <see cref="BindingSource"/>.
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
            }
        }
    }

    /// <summary>
    /// Gets or sets the data member (property path) when the data source contains multiple lists.
    /// </summary>
    public string DataMember
    {
        get => _dataMember;
        set
        {
            if (_dataMember != value)
            {
                _dataMember = value ?? string.Empty;
                OnDataSourceChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the layout mode for row fields.
    /// </summary>
    public PivotTableLayoutMode LayoutMode { get; set; } = PivotTableLayoutMode.Compact;

    /// <summary>
    /// Gets or sets which totals are shown.
    /// </summary>
    public PivotTableTotalVisibility TotalVisibility { get; set; } = PivotTableTotalVisibility.All;

    /// <summary>
    /// Gets the current drilldown state dictionary (node path -> is expanded).
    /// </summary>
    public IReadOnlyDictionary<string, bool> DrillState => _matrix.GetDrillState();

    /// <summary>
    /// Gets the underlying matrix engine (for advanced operations).
    /// </summary>
    internal PivotTableMatrix Matrix => _matrix;

    /// <summary>
    /// Gets or sets the selected row index in the visible data area.
    /// </summary>
    public int SelectedRowIndex
    {
        get => _selectedRowIndex;
        set
        {
            if (_selectedRowIndex != value)
            {
                _selectedRowIndex = value;
                OnSelectionChanged();
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets the selected column index in the visible data area.
    /// </summary>
    public int SelectedColumnIndex
    {
        get => _selectedColumnIndex;
        set
        {
            if (_selectedColumnIndex != value)
            {
                _selectedColumnIndex = value;
                OnSelectionChanged();
                Invalidate();
            }
        }
    }

    private Color _headerBgColor = Color.FromArgb(240, 240, 240);
    private Color _headerTextColor = Color.FromArgb(0, 0, 0);
    private Color _alternateRowColor = Color.FromArgb(245, 245, 250);
    private Color _totalBgColor = Color.FromArgb(230, 235, 245);
    private Color _gridLineColor = Color.FromArgb(200, 200, 200);
    private Color _drillBtnColor = Color.FromArgb(80, 80, 80);
    private Color _selectedCellColor = Color.FromArgb(200, 220, 255);

    /// <summary>
    /// Gets or sets the background color for row and column header cells.
    /// </summary>
    public Color HeaderBackgroundColor
    {
        get => _headerBgColor;
        set { _headerBgColor = value; _headerBgColorSet = true; Invalidate(); }
    }

    /// <summary>
    /// Gets or sets the text color for header cells.
    /// </summary>
    public Color HeaderTextColor
    {
        get => _headerTextColor;
        set { _headerTextColor = value; _headerTextColorSet = true; Invalidate(); }
    }

    /// <summary>
    /// Gets or sets the background color for alternating data rows.
    /// </summary>
    public Color AlternateRowColor
    {
        get => _alternateRowColor;
        set { _alternateRowColor = value; _altRowColorSet = true; Invalidate(); }
    }

    /// <summary>
    /// Gets or sets the background color for total rows and columns.
    /// </summary>
    public Color TotalBackgroundColor
    {
        get => _totalBgColor;
        set { _totalBgColor = value; _totalBgColorSet = true; Invalidate(); }
    }

    /// <summary>
    /// Gets or sets the grid line color.
    /// </summary>
    public Color GridLineColor
    {
        get => _gridLineColor;
        set { _gridLineColor = value; _gridLineColorSet = true; Invalidate(); }
    }

    /// <summary>
    /// Gets or sets the drilldown button (+/-) color.
    /// </summary>
    public Color DrillButtonColor
    {
        get => _drillBtnColor;
        set { _drillBtnColor = value; Invalidate(); }
    }

    /// <summary>
    /// Gets or sets the selected cell background color.
    /// </summary>
    public Color SelectedCellColor
    {
        get => _selectedCellColor;
        set { _selectedCellColor = value; _selCellColorSet = true; Invalidate(); }
    }

    /// <summary>
    /// Gets or sets whether grid lines are drawn.
    /// </summary>
    public bool ShowGridLines { get; set; } = true;

    // ──────────────────────────────────────────────
    //  Events
    // ──────────────────────────────────────────────

    /// <summary>
    /// Occurs when a data cell value is being formatted for display.
    /// </summary>
    public event EventHandler<PivotTableCellFormattingEventArgs>? CellFormatting;

    /// <summary>
    /// Occurs when a data cell is being painted.
    /// </summary>
    public event EventHandler<PivotTableCellPaintingEventArgs>? CellPainting;

    /// <summary>
    /// Occurs when a cell is clicked.
    /// </summary>
    public event EventHandler<PivotTableCellClickEventArgs>? CellClick;

    /// <summary>
    /// Occurs when a drilldown node is expanded or collapsed.
    /// </summary>
    public event EventHandler<PivotTableDrilldownEventArgs>? DrilldownToggled;

    /// <summary>
    /// Occurs when a data cell is double-clicked, providing detail records for drill-through.
    /// </summary>
    public event EventHandler<PivotTableDrillThroughEventArgs>? DrillThrough;

    /// <summary>
    /// Occurs when the selected row or column changes.
    /// </summary>
    public event EventHandler? SelectionChanged;

    /// <summary>
    /// Occurs when the matrix has been rebuilt (after data or field changes).
    /// </summary>
    public event EventHandler? MatrixRebuilt;

    // ──────────────────────────────────────────────
    //  Event raisers
    // ──────────────────────────────────────────────

    /// <summary>
    /// Raises the <see cref="SelectionChanged"/> event.
    /// </summary>
    protected virtual void OnSelectionChanged()
    {
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Raises the <see cref="MatrixRebuilt"/> event.
    /// </summary>
    protected virtual void OnMatrixRebuilt()
    {
        MatrixRebuilt?.Invoke(this, EventArgs.Empty);
    }

    // ──────────────────────────────────────────────
    //  Data Source Handling
    // ──────────────────────────────────────────────

    private void OnDataSourceChanged()
    {
        if (_dataSourceUpdating) return;
        _dataSourceUpdating = true;

        // Unsubscribe from previous binding list
        if (_previousBindingList != null)
        {
            _previousBindingList.ListChanged -= OnListChanged;
            _previousBindingList = null;
        }

        // Resolve data source
        var resolved = ResolveDataSource();
        _records = resolved.ToList();

        // Subscribe to binding list changes
        if (_dataSource is IBindingList bl)
        {
            _previousBindingList = bl;
            bl.ListChanged += OnListChanged;
        }
        else if (_dataSource is BindingSource bs)
        {
            if (bs.List is IBindingList bsl)
            {
                _previousBindingList = bsl;
                bsl.ListChanged += OnListChanged;
            }
        }

        _matrixDirty = true;
        RebuildMatrix();
        _dataSourceUpdating = false;
    }

    private IEnumerable<object> ResolveDataSource()
    {
        if (_dataSource == null)
            return Enumerable.Empty<object>();

        // BindingSource
        if (_dataSource is BindingSource bs)
        {
            if (!string.IsNullOrEmpty(_dataMember))
            {
                var member = bs.List?.OfType<object>().FirstOrDefault();
                if (member != null)
                {
                    var prop = member.GetType().GetProperty(_dataMember);
                    if (prop?.GetValue(member) is IEnumerable nested)
                        return nested.Cast<object>();
                }
            }
            return bs.List?.OfType<object>() ?? Enumerable.Empty<object>();
        }

        // IBindingList / IList
        if (_dataSource is IEnumerable enumerable)
            return enumerable.Cast<object>();

        return Enumerable.Empty<object>();
    }

    private void OnListChanged(object? sender, ListChangedEventArgs e)
    {
        RebuildMatrix();
    }

    private void OnFieldsChanged()
    {
        _matrixDirty = true;
        RebuildMatrix();
    }

    // ──────────────────────────────────────────────
    //  Matrix Building
    // ──────────────────────────────────────────────

    /// <summary>
    /// Rebuilds the internal matrix from the current data source and field definitions.
    /// Called automatically when data or fields change.
    /// </summary>
    public void RebuildMatrix()
    {
        _rebuildPending = false;

        var rowFields = _fields.RowFields;
        var colFields = _fields.ColumnFields;
        var valFields = _fields.ValueFields;
        var filterFields = _fields.FilterFields;

        if (rowFields.Count == 0 && colFields.Count == 0 && valFields.Count == 0)
        {
            _matrixDirty = false;
            OnMatrixRebuilt();
            Invalidate();
            return;
        }

        _matrix.Build(
            _records,
            rowFields,
            colFields,
            valFields,
            filterFields,
            _filterValues,
            _matrix.GetDrillState(),
            TotalVisibility);

        _matrixDirty = false;
        UpdateScrollBars();
        OnMatrixRebuilt();
        Invalidate();
    }

    private void UpdateScrollBars()
    {
        _totalDataWidth = ComputeTotalDataWidth();
        _totalDataHeight = _matrix.FlatRows.Count * DataRowHeight;

        var scrollBarContext = GetScrollBarContext();
        _vScrollBar.ContentSize = _totalDataHeight;
        _vScrollBar.ViewSize = Math.Max(0, ComputeDataAreaHeight());
        _hScrollBar.ContentSize = _totalDataWidth;
        _hScrollBar.ViewSize = Math.Max(0, ComputeDataAreaWidth());
    }

    // ──────────────────────────────────────────────
    //  Layout Calculation
    // ──────────────────────────────────────────────

    private int ComputeDataAreaHeight()
    {
        int headerH = ComputeColumnHeaderHeight();
        return Height - headerH - (_vScrollBar.NeedsScrollbar ? 0 : 0);
    }

    private int ComputeDataAreaWidth()
    {
        int rhw = _rowHeaderWidth;
        return Width - rhw - (_hScrollBar.NeedsScrollbar ? 0 : 0);
    }

    private int ComputeColumnHeaderHeight()
    {
        int colFieldCount = _fields.ColumnFields.Count;
        int valFieldCount = _fields.ValueFields.Count;
        if (colFieldCount == 0 && valFieldCount == 0)
            return 0;

        int h = 0;
        if (colFieldCount > 0)
            h += colFieldCount * HeaderRowHeight;
        if (valFieldCount > 1)
            h += HeaderRowHeight; // sub-header for value field names
        if (colFieldCount == 0 && valFieldCount > 0)
            h = HeaderRowHeight; // single header for values

        return h;
    }

    private int ComputeTotalDataWidth()
    {
        int colCount = _matrix.FlatColumns.Count;
        int valCount = _matrix.ValueFieldCount;
        if (colCount == 0) return 0;

        // Distribute available width equally among columns
        int dataW = Width - _rowHeaderWidth - 20; // approximate
        return Math.Max(colCount * MinColumnWidth, dataW);
    }

    private int GetColumnWidth(int visibleIndex)
    {
        if (_columnWidthOverrides.TryGetValue(visibleIndex, out int overrideWidth))
            return overrideWidth;

        int colCount = _matrix.FlatColumns.Count;
        if (colCount == 0) return MinColumnWidth;

        int dataW = ComputeDataAreaWidth();
        if (dataW <= 0) dataW = Width - _rowHeaderWidth - 2;
        return Math.Max(MinColumnWidth, dataW / colCount);
    }

    // ──────────────────────────────────────────────
    //  Rendering
    // ──────────────────────────────────────────────

    /// <inheritdoc />
    public override void Render(Graphics g)
    {
        if (_matrixDirty || _rebuildPending)
            RebuildMatrix();

        if (!_matrix.IsBuilt)
        {
            g.FillRectangle(_backColor, 0, 0, Width, Height);
            return;
        }

        var theme = ThemeManager.CurrentTheme;
        int colHeaderH = ComputeColumnHeaderHeight();
        int rowH = DataRowHeight;

        // Compute visible scroll range
        int scrollY = _vScrollBar.Value;
        int scrollX = _hScrollBar.Value;

        // Draw background
        g.FillRectangle(_backColor, 0, 0, Width, Height);

        // Determine row header width
        _rowHeaderWidth = ComputeRowHeaderWidth();

        int rhw = _rowHeaderWidth;
        int dataW = Width - rhw;

        bool hasRowFields = _fields.RowFields.Count > 0;
        bool hasColFields = _fields.ColumnFields.Count > 0;
        bool hasValFields = _fields.ValueFields.Count > 0;

        // ── Column Headers ──
        if (colHeaderH > 0 && hasColFields)
        {
            DrawColumnHeaders(g, rhw, colHeaderH);
        }
        else if (colHeaderH > 0 && !hasColFields && hasValFields)
        {
            // Single header row for value field names
            g.FillRectangle(HeaderBackgroundColor, rhw, 0, dataW, HeaderRowHeight);
            if (ShowGridLines)
                g.DrawRectangle(GridLineColor, rhw, 0, dataW, HeaderRowHeight, 1);

            int xOff = rhw;
            int valColW = dataW / Math.Max(_matrix.ValueFieldCount, 1);
            for (int vi = 0; vi < _matrix.ValueFieldCount; vi++)
            {
                string text = _fields.ValueFields.Count > vi ? _fields.ValueFields[vi].Name : ("Value " + vi);
                DrawCellText(g, text, theme.DefaultFont, HeaderTextColor,
                    xOff + scrollX, 0, valColW, HeaderRowHeight, true);
                xOff += valColW;
            }
        }

        // ── Corner Cell ──
        if (colHeaderH > 0)
        {
            g.FillRectangle(HeaderBackgroundColor, 0, 0, rhw, colHeaderH);
            if (ShowGridLines)
                g.DrawRectangle(GridLineColor, 0, 0, rhw, colHeaderH, 1);

            string cornerText = _fields.RowFields.Count > 0 ? _fields.RowFields[0].Name : "Values";
            DrawCellText(g, cornerText, theme.DefaultFont, HeaderTextColor,
                0, 0, rhw, colHeaderH, true);
        }

        // ── Row Headers + Data Cells ──
        int dataY = colHeaderH;
        int firstVisibleRow = Math.Max(0, scrollY / rowH);
        int lastVisibleRow = Math.Min(_matrix.FlatRows.Count - 1,
            firstVisibleRow + (Height - colHeaderH) / rowH + 1);

        for (int ri = firstVisibleRow; ri <= lastVisibleRow && ri < _matrix.FlatRows.Count; ri++)
        {
            var flatRow = _matrix.FlatRows[ri];
            int ry = dataY + ri * rowH - scrollY;
            bool isTotal = flatRow.IsTotal || flatRow.IsGrandTotal;
            bool isHeader = flatRow.IsGroupHeader && !flatRow.IsTotal;

            // Background
            Color rowBg = isTotal ? TotalBackgroundColor :
                          ri % 2 == 1 ? AlternateRowColor :
                          _backColor;
            g.FillRectangle(rowBg, 0, ry, Width, rowH);

            // ── Row Header ──
            if (hasRowFields)
            {
                int indent = flatRow.Level * IndentWidth;

                g.FillRectangle(isHeader ? HeaderBackgroundColor : rowBg, 0, ry, rhw, rowH);

                // Drilldown button
                if (flatRow.HasChildren)
                {
                    int btnX = indent + 2;
                    int btnY = ry + (rowH - DrillButtonSize) / 2;
                    DrawDrillButton(g, btnX, btnY, flatRow.IsExpanded);
                }

                // Row header text
                int textX = indent + (flatRow.HasChildren ? DrillButtonSize + 4 : 4);
                int textW = rhw - textX;
                Font headerFont = isTotal ? new Font(theme.DefaultFont.Name, theme.DefaultFont.Size, FontStyle.Bold) : theme.DefaultFont;
                DrawCellText(g, flatRow.Value, headerFont,
                    isTotal ? theme.FocusIndicator : HeaderTextColor,
                    textX, ry, Math.Max(textW, 1), rowH, false);

                if (ShowGridLines)
                    g.DrawRectangle(GridLineColor, 0, ry, rhw, rowH, 1);
            }

            // ── Data Cells ──
            if (!isHeader && _matrix.FlatColumns.Count > 0)
            {
                int valCount = _matrix.ValueFieldCount;
                int colAccum = rhw + scrollX;
                for (int ci = 0; ci < _matrix.FlatColumns.Count; ci++)
                {
                    var flatCol = _matrix.FlatColumns[ci];
                    int colWidth = GetColumnWidth(ci);
                    int subColW = valCount > 1 ? colWidth / valCount : colWidth;

                    for (int vi = 0; vi < valCount; vi++)
                    {
                        int cx = colAccum + vi * subColW;
                        int cw = vi == valCount - 1 ? colWidth - vi * subColW : subColW;
                        cw = Math.Max(1, cw);

                        if (cx + cw < 0 || cx > Width) continue;

                        bool isSelected = _selectedRowIndex == flatRow.DataIndex
                            && _selectedColumnIndex == flatCol.DataIndex
                            && _selectedValueIndex == vi;

                        string displayText = string.Empty;
                        Color cellBg = isSelected ? SelectedCellColor : rowBg;
                        Color cellFg = _foreColor;

                        if (flatRow.DataIndex >= 0 && flatCol.DataIndex >= 0)
                        {
                            double rawVal = _matrix.GetValue(flatRow.DataIndex, flatCol.DataIndex, vi);
                            displayText = FormatValue(rawVal, vi);
                        }

                        var formatArgs = new PivotTableCellFormattingEventArgs(
                            ri, ci, flatRow.Level, flatCol.Level,
                            displayText, null);
                        CellFormatting?.Invoke(this, formatArgs);
                        displayText = formatArgs.FormattedValue ?? displayText;

                        var paintArgs = new PivotTableCellPaintingEventArgs(
                            g, new Rectangle(cx, ry, cw, rowH),
                            ri, ci, displayText, cellFg, cellBg,
                            isSelected, isTotal);
                        CellPainting?.Invoke(this, paintArgs);

                        if (!paintArgs.Handled)
                        {
                            g.FillRectangle(paintArgs.BackColor, cx, ry, cw, rowH);
                            DrawCellTextRight(g, paintArgs.DisplayText ?? displayText, theme.DefaultFont,
                                paintArgs.ForeColor,
                                cx, ry, cw, rowH);
                        }

                        if (ShowGridLines)
                            g.DrawRectangle(GridLineColor, cx, ry, cw, rowH, 1);
                    }
                    colAccum += colWidth;
                }
            }
            else if (isHeader && ShowGridLines)
            {
                // Draw separator line below group header
                g.DrawLine(GridLineColor, rhw, ry + rowH - 1, Width, ry + rowH - 1);
            }
        }

        // Draw scrollbars
        DrawScrollBars(g, theme);

        g.ResetClip();
        base.Render(g);
    }

    private void DrawColumnHeaders(Graphics g, int rhw, int colHeaderH)
    {
        var theme = ThemeManager.CurrentTheme;
        var flatCols = _matrix.FlatColumns;
        int scrollX = _hScrollBar.Value;

        int GetCx(int ci) => rhw + Enumerable.Range(0, ci).Sum(i => GetColumnWidth(i)) + scrollX;

        for (int level = 0; level < _fields.ColumnFields.Count; level++)
        {
            int headerY = level * HeaderRowHeight;

            for (int ci = 0; ci < flatCols.Count; ci++)
            {
                var fc = flatCols[ci];
                if (fc.Level != level && fc.Level != -1) continue;

                int cx = GetCx(ci);
                // For spanning headers, sum widths across all spanned leaf sub-columns
                int cw = fc.Span > 1 && fc.Level == level
                    ? Enumerable.Range(ci, Math.Min(fc.Span, flatCols.Count - ci)).Sum(i => GetColumnWidth(i))
                    : GetColumnWidth(ci);

                if (cx + cw < rhw || cx > Width) continue;

                g.FillRectangle(HeaderBackgroundColor, cx, headerY, cw, HeaderRowHeight);

                string text = fc.Value;
                DrawCellText(g, text, theme.DefaultFont, HeaderTextColor,
                    cx + CellPadding, headerY, cw - CellPadding * 2, HeaderRowHeight, true);

                if (ShowGridLines)
                    g.DrawRectangle(GridLineColor, cx, headerY, cw, HeaderRowHeight, 1);
            }
        }

        // Value sub-headers if multiple value fields
        if (_matrix.ValueFieldCount > 1 && _fields.ColumnFields.Count > 0)
        {
            int subY = _fields.ColumnFields.Count * HeaderRowHeight;
            for (int ci = 0; ci < flatCols.Count; ci++)
            {
                int cx = GetCx(ci);
                int colWidth = GetColumnWidth(ci);
                int subColW = colWidth / _matrix.ValueFieldCount;
                for (int vi = 0; vi < _matrix.ValueFieldCount; vi++)
                {
                    int scx = cx + vi * subColW;
                    int scw = vi == _matrix.ValueFieldCount - 1 ? colWidth - vi * subColW : subColW;
                    scw = Math.Max(1, scw);
                    g.FillRectangle(HeaderBackgroundColor, scx, subY, scw, HeaderRowHeight);
                    string vn = _fields.ValueFields.Count > vi ? _fields.ValueFields[vi].Name : ("V" + vi);
                    DrawCellText(g, vn, theme.DefaultFont, HeaderTextColor,
                        scx, subY, scw, HeaderRowHeight, true);
                    if (ShowGridLines)
                        g.DrawRectangle(GridLineColor, scx, subY, scw, HeaderRowHeight, 1);
                }
            }
        }
    }

    private void DrawDrillButton(Graphics g, int x, int y, bool isExpanded)
    {
        int s = DrillButtonSize;
        // Background
        g.FillRectangle(Color.FromArgb(220, 220, 220), x, y, s, s);
        g.DrawRectangle(DrillButtonColor, x, y, s, s, 1);

        // Minus or plus sign
        int midX = x + s / 2;
        int midY = y + s / 2;
        int half = 4;

        // Horizontal line
        g.DrawLine(DrillButtonColor, midX - half, midY, midX + half, midY, 1.5f);

        // Vertical line (only for collapsed/plus state)
        if (!isExpanded)
        {
            g.DrawLine(DrillButtonColor, midX, midY - half, midX, midY + half, 1.5f);
        }
    }

    private void DrawCellText(Graphics g, string text, Font font, Color color,
        int x, int y, int w, int h, bool center)
    {
        DrawCellTextAligned(g, text, font, color, x, y, w, h, center, false);
    }

    private void DrawCellTextRight(Graphics g, string text, Font font, Color color,
        int x, int y, int w, int h)
    {
        DrawCellTextAligned(g, text, font, color, x, y, w, h, false, true);
    }

    private void DrawCellTextAligned(Graphics g, string text, Font font, Color color,
        int x, int y, int w, int h, bool center, bool right)
    {
        if (string.IsNullOrEmpty(text)) return;

        var (tw, th) = g.MeasureString(text, font);
        float tx, ty;
        ty = y + (h - th) / 2f;

        if (center)
            tx = x + (w - tw) / 2f;
        else if (right)
            tx = x + w - tw - CellPadding;
        else
            tx = x + CellPadding;

        g.SetClip(new Rectangle(x, y, w, h));
        g.DrawString(text, font, color, tx, ty);
        g.ResetClip();
    }

    private void DrawScrollBars(Graphics g, Theme theme)
    {
        int scrollBarWidth = 16;
        int rhw = _rowHeaderWidth;
        int colHeaderH = ComputeColumnHeaderHeight();
        int dataH = Height - colHeaderH;
        int dataW = Width - rhw;

        // Vertical scrollbar
        if (_vScrollBar.NeedsScrollbar)
        {
            _vScrollBar.Render(g, new Rectangle(Width - scrollBarWidth, colHeaderH,
                scrollBarWidth, dataH), theme);
        }

        // Horizontal scrollbar
        if (_hScrollBar.NeedsScrollbar)
        {
            _hScrollBar.Render(g, new Rectangle(rhw, Height - scrollBarWidth,
                dataW, scrollBarWidth), theme);
        }
    }

    private int ComputeRowHeaderWidth()
    {
        var flatRows = _matrix.FlatRows;
        if (flatRows.Count == 0) return 120;

        int maxDepth = _matrix.MaxRowLevel;
        int maxTextWidth = 80; // base estimate

        // Estimate max text width based on content
        var theme = ThemeManager.CurrentTheme;
        foreach (var row in flatRows)
        {
            if (string.IsNullOrEmpty(row.Value)) continue;
            int estLen = row.Value.Length * 7; // rough pixel estimate per char at 14pt
            if (estLen > maxTextWidth) maxTextWidth = estLen;
        }

        return Math.Max(120, maxTextWidth + (maxDepth + 1) * IndentWidth + DrillButtonSize + 10);
    }

    // ──────────────────────────────────────────────
    //  Value Formatting
    // ──────────────────────────────────────────────

    private string FormatValue(double value, int valFieldIndex)
    {
        if (valFieldIndex < 0 || valFieldIndex >= _fields.ValueFields.Count)
        {
            if (value == Math.Floor(value) && !double.IsInfinity(value))
                return value.ToString("0");
            return value.ToString("0.##");
        }

        var field = _fields.ValueFields[valFieldIndex];
        string? fmt = field.FormatString;

        if (string.IsNullOrEmpty(fmt))
        {
            if (value == Math.Floor(value) && !double.IsInfinity(value))
                return value.ToString("0");
            return value.ToString("0.##");
        }

        try
        {
            var f = fmt.Contains("{0") ? fmt : "{0:" + fmt + "}";
            return string.Format(f, value);
        }
        catch
        {
            return value.ToString("0.##");
        }
    }

    // ──────────────────────────────────────────────
    //  Hit Testing & Mouse Handling
    // ──────────────────────────────────────────────

    /// <inheritdoc />
    protected internal override void OnMouseDown(EventArgs e)
    {
        var m = (MouseEventArgs)e;
        base.OnMouseDown(e);

        // Close filter dropdown on any click
        if (_isFilterDropdownOpen)
        {
            _isFilterDropdownOpen = false;
            Invalidate();
        }

        int colHeaderH = ComputeColumnHeaderHeight();
        int rhw = _rowHeaderWidth;

        // Check column divider resize
        if (m.Y >= 0 && m.Y < colHeaderH && m.X >= rhw)
        {
            int dc = GetDividerColumnIndex(m.X, rhw);
            if (dc >= 0)
            {
                _resizingColumnIndex = dc;
                _resizeStartMouseX = m.X;
                _resizeStartWidth = GetColumnWidth(dc);
                var f = FindForm();
                if (f != null) f.CaptureControl = this;
                return;
            }
            // Column header click (for sort)
            HandleColumnHeaderClick(m);
            return;
        }

        // Check drilldown button click in row header area
        if (m.Y >= colHeaderH && m.X >= 0 && m.X < rhw)
        {
            HandleRowHeaderClick(m, colHeaderH);
            return;
        }

        // Check data cell click
        if (m.Y >= colHeaderH && m.X >= rhw)
        {
            if (m.Clicks == 2)
            {
                HandleDrillThrough(m, colHeaderH, rhw);
            }
            else
            {
                HandleDataCellClick(m, colHeaderH, rhw);
            }
            return;
        }

        // Check scrollbar clicks
        var ctx = GetScrollBarContext();
        if (_vScrollBar.NeedsScrollbar &&
            m.X >= Width - 16 && m.X <= Width)
        {
            var vBounds = new Rectangle(Width - 16, colHeaderH, 16, Height - colHeaderH);
            _vScrollBar.HandleMouseDown(new Point(m.X, m.Y), vBounds, ctx);
            return;
        }
        if (_hScrollBar.NeedsScrollbar &&
            m.Y >= Height - 16 && m.Y <= Height)
        {
            var hBounds = new Rectangle(rhw, Height - 16, Width - rhw, 16);
            _hScrollBar.HandleMouseDown(new Point(m.X, m.Y), hBounds, ctx);
            return;
        }
    }

    /// <inheritdoc />
    protected internal override void OnMouseUp(EventArgs e)
    {
        base.OnMouseUp(e);
        if (_resizingColumnIndex >= 0)
        {
            _resizingColumnIndex = -1;
            var f = FindForm();
            if (f != null) f.CaptureControl = null;
        }
        var ctx = GetScrollBarContext();
        _vScrollBar.HandleMouseUp(ctx);
        _hScrollBar.HandleMouseUp(ctx);
    }

    /// <inheritdoc />
    protected internal override void OnMouseMove(EventArgs e)
    {
        base.OnMouseMove(e);
        var m = (MouseEventArgs)e;
        var ctx = GetScrollBarContext();
        int colHeaderH = ComputeColumnHeaderHeight();
        int rhw = _rowHeaderWidth;

        // Column resize drag
        if (_resizingColumnIndex >= 0)
        {
            int newWidth = Math.Max(MinColWidth, _resizeStartWidth + m.X - _resizeStartMouseX);
            _columnWidthOverrides[_resizingColumnIndex] = newWidth;
            Invalidate();
            return;
        }

        // Column divider cursor
        if (m.Y >= 0 && m.Y < colHeaderH && m.X >= rhw)
        {
            int dc = GetDividerColumnIndex(m.X, rhw);
            var form = FindForm();
            if (dc >= 0)
            {
                if (form != null) form.Cursor = SystemCursorType.SizeWE;
            }
            else
            {
                if (form != null && form.Cursor == SystemCursorType.SizeWE) form.Cursor = null;
            }
        }

        var vBounds = new Rectangle(Width - 16, colHeaderH, 16, Height - colHeaderH);
        var hBounds = new Rectangle(rhw, Height - 16, Width - rhw, 16);
        _vScrollBar.HandleMouseMove(new Point(m.X, m.Y), vBounds, ctx);
        _hScrollBar.HandleMouseMove(new Point(m.X, m.Y), hBounds, ctx);
    }

    /// <inheritdoc />
    protected internal override void OnMouseWheel(EventArgs e)
    {
        base.OnMouseWheel(e);
        var m = (MouseEventArgs)e;
        var ctx = GetScrollBarContext();
        _vScrollBar.HandleMouseWheel(m.Delta, ctx);
    }

    /// <summary>
    /// Finds the column index whose right edge divider is within <see cref="DividerThreshold"/> pixels of the given x-coordinate.
    /// </summary>
    private int GetDividerColumnIndex(int x, int rhw)
    {
        int scrollX = _hScrollBar.Value;
        int cx = rhw + scrollX;
        for (int ci = 0; ci < _matrix.FlatColumns.Count; ci++)
        {
            cx += GetColumnWidth(ci);
            if (Math.Abs(x - cx) <= DividerThreshold)
                return ci;
        }
        return -1;
    }

    private void HandleColumnHeaderClick(MouseEventArgs m)
    {
        int rhw = _rowHeaderWidth;
        int scrollX = _hScrollBar.Value;
        int cx = rhw + scrollX;

        for (int ci = 0; ci < _matrix.FlatColumns.Count; ci++)
        {
            int cw = GetColumnWidth(ci);
            if (m.X >= cx && m.X < cx + cw)
            {
                if (_sortFieldIndex == ci)
                    _sortAscending = !_sortAscending;
                else
                {
                    _sortFieldIndex = ci;
                    _sortAscending = true;
                }
                Invalidate();
                return;
            }
            cx += cw;
        }
    }

    private void HandleRowHeaderClick(MouseEventArgs m, int colHeaderH)
    {
        int scrollY = _vScrollBar.Value;
        int rowH = DataRowHeight;
        int flatRowIndex = (m.Y - colHeaderH + scrollY) / rowH;

        if (flatRowIndex < 0 || flatRowIndex >= _matrix.FlatRows.Count) return;

        var flatRow = _matrix.FlatRows[flatRowIndex];
        if (!flatRow.HasChildren) return;

        // Check if click is on the drilldown button
        int indent = flatRow.Level * IndentWidth;
        int btnX = indent + 2;
        int btnY = colHeaderH + flatRowIndex * rowH - scrollY + (rowH - DrillButtonSize) / 2;

        if (m.X >= btnX && m.X <= btnX + DrillButtonSize &&
            m.Y >= btnY && m.Y <= btnY + DrillButtonSize)
        {
            // Toggle drilldown
            bool newState = _matrix.ToggleDrilldown(flatRow.Path);
            RebuildMatrix();
            DrilldownToggled?.Invoke(this, new PivotTableDrilldownEventArgs(
                flatRow.Level, flatRow.Path, flatRow.Value, newState));
        }
    }

    private void HandleDataCellClick(MouseEventArgs m, int colHeaderH, int rhw)
    {
        int scrollY = _vScrollBar.Value;
        int rowH = DataRowHeight;

        int flatRowIndex = (m.Y - colHeaderH + scrollY) / rowH;
        if (flatRowIndex < 0 || flatRowIndex >= _matrix.FlatRows.Count) return;

        var flatRow = _matrix.FlatRows[flatRowIndex];
        if (flatRow.IsGroupHeader && !flatRow.IsTotal) return;

        int flatColIndex = HitTestColumn(m.X, rhw);
        if (flatColIndex < 0) return;

        _selectedRowIndex = flatRow.DataIndex;
        _selectedColumnIndex = _matrix.FlatColumns[flatColIndex].DataIndex;
        OnSelectionChanged();
        Invalidate();

        CellClick?.Invoke(this, new PivotTableCellClickEventArgs(
            _selectedRowIndex, _selectedColumnIndex, flatRow.IsTotal));
    }

    private void HandleDrillThrough(MouseEventArgs m, int colHeaderH, int rhw)
    {
        int scrollY = _vScrollBar.Value;
        int rowH = DataRowHeight;

        int flatRowIndex = (m.Y - colHeaderH + scrollY) / rowH;
        if (flatRowIndex < 0 || flatRowIndex >= _matrix.FlatRows.Count) return;

        var flatRow = _matrix.FlatRows[flatRowIndex];
        if (flatRow.IsGroupHeader && !flatRow.IsTotal) return;

        int flatColIndex = HitTestColumn(m.X, rhw);
        if (flatColIndex < 0) return;

        var details = _matrix.GetDetails(
            flatRow.DataIndex,
            _matrix.FlatColumns[flatColIndex].DataIndex,
            _selectedValueIndex);

        var args = new PivotTableDrillThroughEventArgs(
            flatRow.DataIndex, flatColIndex, details);
        DrillThrough?.Invoke(this, args);
    }

    private int HitTestColumn(int mouseX, int rhw)
    {
        int scrollX = _hScrollBar.Value;
        int cx = rhw + scrollX;
        for (int ci = 0; ci < _matrix.FlatColumns.Count; ci++)
        {
            int cw = GetColumnWidth(ci);
            if (mouseX >= cx && mouseX < cx + cw)
                return ci;
            cx += cw;
        }
        return -1;
    }

    // ──────────────────────────────────────────────
    //  Keyboard Handling
    // ──────────────────────────────────────────────

    /// <inheritdoc />
    protected internal override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        bool handled = true;

        switch (e.KeyCode)
        {
            case Keys.Down:
                _selectedRowIndex = Math.Min(_selectedRowIndex + 1, _matrix.DataRowCount - 1);
                EnsureVisible();
                break;
            case Keys.Up:
                _selectedRowIndex = Math.Max(_selectedRowIndex - 1, 0);
                EnsureVisible();
                break;
            case Keys.Right:
                _selectedColumnIndex = Math.Min(_selectedColumnIndex + 1, _matrix.DataColumnCount - 1);
                break;
            case Keys.Left:
                _selectedColumnIndex = Math.Max(_selectedColumnIndex - 1, 0);
                break;
            case Keys.Home:
                _selectedRowIndex = 0;
                _selectedColumnIndex = 0;
                EnsureVisible();
                break;
            case Keys.End:
                _selectedRowIndex = _matrix.DataRowCount - 1;
                _selectedColumnIndex = _matrix.DataColumnCount - 1;
                EnsureVisible();
                break;
            case Keys.PageUp:
                _selectedRowIndex = Math.Max(_selectedRowIndex - 10, 0);
                EnsureVisible();
                break;
            case Keys.PageDown:
                _selectedRowIndex = Math.Min(_selectedRowIndex + 10, _matrix.DataRowCount - 1);
                EnsureVisible();
                break;
            default:
                handled = false;
                break;
        }

        if (handled)
        {
            e.Handled = true;
            Invalidate();
        }
    }

    private void EnsureVisible()
    {
        int rowH = DataRowHeight;
        int colHeaderH = ComputeColumnHeaderHeight();
        int dataH = Height - colHeaderH;

        if (_selectedRowIndex >= 0)
        {
            int targetY = _selectedRowIndex * rowH;
            int scrollY = _vScrollBar.Value;
            if (targetY < scrollY)
                _vScrollBar.ScrollTo(targetY);
            else if (targetY + rowH > scrollY + dataH)
                _vScrollBar.ScrollTo(targetY + rowH - dataH);
        }
    }

    // ──────────────────────────────────────────────
    //  ScrollBar Context
    // ──────────────────────────────────────────────

    private ScrollBarContext? _scrollBarContext;

    private ScrollBarContext GetScrollBarContext()
    {
        _scrollBarContext ??= new ScrollBarContext(this);
        return _scrollBarContext;
    }

    private class ScrollBarContext : IScrollBarContext
    {
        private readonly PivotTable _owner;

        public ScrollBarContext(PivotTable owner)
        {
            _owner = owner;
        }

        public float Zoom => _owner.EffectiveZoom;
        public void Invalidate() => _owner.Invalidate();
        public void CaptureMouse(bool capture) => _owner.CapturingMouse = capture;
    }

    // ──────────────────────────────────────────────
    //  Public Methods
    // ──────────────────────────────────────────────

    /// <summary>
    /// Expands all drilldown nodes.
    /// </summary>
    public void ExpandAll()
    {
        _matrix.ClearDrillState();
        RebuildMatrix();
    }

    /// <summary>
    /// Collapses all drilldown nodes to the first level.
    /// </summary>
    public void CollapseAll()
    {
        _matrix.CollapseAllDrillState();
        RebuildMatrix();
    }

    /// <summary>
    /// Sets a filter value for a filter field.
    /// </summary>
    /// <param name="dataPropertyName">The field's data property name.</param>
    /// <param name="value">The value to filter on, or null to remove the filter.</param>
    public void SetFilter(string dataPropertyName, object? value)
    {
        if (value == null)
            _filterValues.Remove(dataPropertyName);
        else
            _filterValues[dataPropertyName] = value;

        RebuildMatrix();
    }

    /// <summary>
    /// Clears all active filters.
    /// </summary>
    public void ClearFilters()
    {
        _filterValues.Clear();
        RebuildMatrix();
    }
}
