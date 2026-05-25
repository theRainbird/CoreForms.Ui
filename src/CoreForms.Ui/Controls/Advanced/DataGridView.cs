using System.ComponentModel;
using CoreForms.Ui.Core;
using CoreForms.Ui.Data;
using CoreForms.Ui.Theming;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Advanced;

public class DataGridView : ContainerControl
{
    private readonly DataGridViewColumnCollection _columns;
    private readonly DataGridViewRowCollection _rows;
    private int _selectedRowIndex = -1;
    private int _selectedColumnIndex = -1;
    private int _rowHeight = 30;
    private object? _dataSource;
    private string _dataMember = string.Empty;
    private bool _dataSourceUpdating;
    private bool _allowUserToAddRows = true;
    private bool _allowUserToDeleteRows = true;
    private bool _readOnly;
    private bool _multiSelect;
    private bool _columnHeadersVisible = true;
    private bool _rowHeadersVisible = true;
    private bool _showGridLines = true;
    private DataGridViewSelectionMode _selectionMode = DataGridViewSelectionMode.RowHeaderSelect;
    private int _horizontalScrollOffset;
    private const int DividerThreshold = 5;
    private const int MinColumnWidth = 20;
    private int _resizingColumnIndex = -1;
    private int _resizeStartMouseX;
    private int _resizeStartWidth;
    private readonly ScrollBarEngine _vScrollBar = new();
    private ScrollBarContext? _scrollBarContext;
    private int _sortColumnIndex = -1;
    private bool _sortAscending = true;
    private readonly List<int> _groupedColumnIndices = new();
    private readonly List<DataGridViewGroup> _groupRoots = new();
    private readonly List<DataGridViewGroup> _allGroups = new();
    private bool _showGroupingBar;
    private Font _groupHeaderFont = Font.Default;
    private Color _groupHeaderForeColor = Color.FromArgb(0, 0, 0);
    private Color _groupHeaderBackColor = Color.FromArgb(230, 230, 235);
    private bool _groupHeaderForeColorSet;
    private bool _groupHeaderBackColorSet;
    private DataGridViewContentAlignment _groupHeaderTextAlign = DataGridViewContentAlignment.Left;
    private int _groupHeaderHeight = 30;
    private int _groupHeaderIndent = 20;
    private int _groupingBarHeight = 30;
    private int _secondarySortColumnIndex = -1;
    private bool _secondarySortAscending = true;
    private int _dragSourceColumnIndex = -1;
    private int _dragStartX;
    private int _dragStartY;
    private int _dragCurrentX;
    private int _dragCurrentY;
    private bool _isDraggingIntoBar;
    private bool _isDraggingPillOut;
    private int _dragRemoveLevelIndex = -1;
    private const int DragThreshold = 8;

    private ScrollBarContext VScrollBarContext => _scrollBarContext ??= new ScrollBarContext(this);

    public DataGridView()
    {
        _columns = new DataGridViewColumnCollection(Invalidate);
        _rows = new DataGridViewRowCollection(Invalidate);
        var theme = ThemeManager.CurrentTheme;
        _backColor = theme.TextBoxBackground;
        _groupHeaderForeColor = theme.DataGridViewGroupHeaderText;
        _groupHeaderBackColor = theme.DataGridViewGroupHeaderBackground;
        Size = new Size(400, 200);
        TabStop = true;
        _vScrollBar.SmallChange = _rowHeight;
        _vScrollBar.LargeChange = _rowHeight * 3;
        _vScrollBar.Scroll += (s, e) => Invalidate();
    }

    public override void OnThemeChanged(Theme newTheme)
    {
        if (!_backColorSet) _backColor = newTheme.TextBoxBackground;
        if (!_foreColorSet) _foreColor = newTheme.ControlText;
        if (!_groupHeaderForeColorSet) _groupHeaderForeColor = newTheme.DataGridViewGroupHeaderText;
        if (!_groupHeaderBackColorSet) _groupHeaderBackColor = newTheme.DataGridViewGroupHeaderBackground;
        if (_groupedColumnIndices.Count > 0) BuildGroups();
        else Invalidate();
    }

    public DataGridViewColumnCollection Columns => _columns;
    public DataGridViewRowCollection Rows => _rows;

    public int SelectedRowIndex
    {
        get => _selectedRowIndex;
        set
        {
            if (value >= -1 && value < _rows.Count)
            {
                _selectedRowIndex = value;
                OnSelectionChanged();
                Invalidate();
            }
        }
    }

    public int SelectedColumnIndex
    {
        get => _selectedColumnIndex;
        set
        {
            if (value >= -1 && value < _columns.Count)
            {
                _selectedColumnIndex = value;
                OnSelectionChanged();
                Invalidate();
            }
        }
    }

    public bool AllowUserToAddRows
    {
        get => _allowUserToAddRows;
        set { if (_allowUserToAddRows != value) { _allowUserToAddRows = value; Invalidate(); } }
    }

    public bool AllowUserToDeleteRows
    {
        get => _allowUserToDeleteRows;
        set { if (_allowUserToDeleteRows != value) { _allowUserToDeleteRows = value; Invalidate(); } }
    }

    public bool ReadOnly
    {
        get => _readOnly;
        set { if (_readOnly != value) { _readOnly = value; Invalidate(); } }
    }

    public bool MultiSelect
    {
        get => _multiSelect;
        set { if (_multiSelect != value) { _multiSelect = value; Invalidate(); } }
    }

    public bool ColumnHeadersVisible
    {
        get => _columnHeadersVisible;
        set { _columnHeadersVisible = value; Invalidate(); }
    }

    public bool RowHeadersVisible
    {
        get => _rowHeadersVisible;
        set { _rowHeadersVisible = value; Invalidate(); }
    }

    public bool ShowGridLines
    {
        get => _showGridLines;
        set { _showGridLines = value; Invalidate(); }
    }

    public DataGridViewSelectionMode SelectionMode
    {
        get => _selectionMode;
        set { if (_selectionMode != value) { _selectionMode = value; Invalidate(); } }
    }

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

    public string DataMember
    {
        get => _dataMember;
        set
        {
            if (_dataMember != value)
            {
                _dataMember = value;
                OnPropertyChanged(nameof(DataMember));
                Invalidate();
            }
        }
    }

    public DataGridViewRow? SelectedRow => _selectedRowIndex >= 0 && _selectedRowIndex < _rows.Count
        ? _rows[_selectedRowIndex] : null;

    public object? SelectedValue => SelectedRow != null && _selectedColumnIndex >= 0 && _selectedColumnIndex < _columns.Count
        ? SelectedRow.Cells[_selectedColumnIndex]?.Value : null;

    public IReadOnlyList<int> GroupedColumnIndices => _groupedColumnIndices;
    public IReadOnlyList<DataGridViewGroup> GroupRoots => _groupRoots;

    public void AddGroupColumn(int columnIndex)
    {
        if (columnIndex < 0 || columnIndex >= _columns.Count) return;
        if (!_columns[columnIndex].Groupable) return;
        if (_groupedColumnIndices.Contains(columnIndex)) return;
        _groupedColumnIndices.Add(columnIndex);
        BuildGroups();
        Invalidate();
    }

    public void RemoveGroupColumn(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= _groupedColumnIndices.Count) return;
        _groupedColumnIndices.RemoveAt(levelIndex);
        BuildGroups();
        Invalidate();
    }

    public void ClearGrouping()
    {
        if (_groupedColumnIndices.Count == 0) return;
        _groupedColumnIndices.Clear();
        _groupRoots.Clear();
        _allGroups.Clear();
        _secondarySortColumnIndex = -1;
        ClearSortColumns();
        Invalidate();
    }

    public bool ShowGroupingBar
    {
        get => _showGroupingBar;
        set { if (_showGroupingBar != value) { _showGroupingBar = value; Invalidate(); } }
    }

    public int GroupingBarHeight
    {
        get => _groupingBarHeight;
        set { if (_groupingBarHeight != value && value > 0) { _groupingBarHeight = value; Invalidate(); } }
    }

    public Font GroupHeaderFont
    {
        get => _groupHeaderFont;
        set { if (_groupHeaderFont != value) { _groupHeaderFont = value ?? Font.Default; Invalidate(); } }
    }

    public Color GroupHeaderForeColor
    {
        get => _groupHeaderForeColor;
        set { _groupHeaderForeColor = value; _groupHeaderForeColorSet = true; Invalidate(); }
    }

    public Color GroupHeaderBackColor
    {
        get => _groupHeaderBackColor;
        set { _groupHeaderBackColor = value; _groupHeaderBackColorSet = true; Invalidate(); }
    }

    public DataGridViewContentAlignment GroupHeaderTextAlign
    {
        get => _groupHeaderTextAlign;
        set { if (_groupHeaderTextAlign != value) { _groupHeaderTextAlign = value; Invalidate(); } }
    }

    public int GroupHeaderHeight
    {
        get => _groupHeaderHeight;
        set { if (_groupHeaderHeight != value && value > 0) { _groupHeaderHeight = value; Invalidate(); } }
    }

    public int GroupHeaderIndent
    {
        get => _groupHeaderIndent;
        set { if (_groupHeaderIndent != value && value >= 0) { _groupHeaderIndent = value; Invalidate(); } }
    }

    public event EventHandler? SelectionChanged;
    public event EventHandler? CellClick;
    public event EventHandler<DataGridViewCellEventArgs>? CellValueChanged;
    public event EventHandler<DataGridViewCellEventArgs>? ColumnHeaderMouseClick;
    public event EventHandler<DataGridViewGroupHeaderFormattingEventArgs>? GroupHeaderFormatting;

    protected virtual void OnSelectionChanged()
    {
        if (!_dataSourceUpdating && _dataSource != null && _selectedRowIndex >= 0)
        {
            if (_dataSource is BindingSource bs) { bs.Position = _selectedRowIndex; }
            else
            {
                var form = FindForm();
                if (form?.BindingContext != null)
                {
                    try
                    {
                        var mgr = form.BindingContext[_dataSource] as CurrencyManager;
                        if (mgr != null) mgr.Position = _selectedRowIndex;
                    }
                    catch { }
                }
            }
        }
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void OnCellClick(DataGridViewCellEventArgs e) => CellClick?.Invoke(this, e);
    protected virtual void OnCellValueChanged(DataGridViewCellEventArgs e) => CellValueChanged?.Invoke(this, e);
    protected virtual void OnColumnHeaderMouseClick(DataGridViewCellEventArgs e) => ColumnHeaderMouseClick?.Invoke(this, e);
    protected virtual void OnGroupHeaderFormatting(DataGridViewGroupHeaderFormattingEventArgs e) => GroupHeaderFormatting?.Invoke(this, e);

    private int GetDataAreaY() => (_showGroupingBar ? _groupingBarHeight : 0) + (_columnHeadersVisible ? _rowHeight : 0);

    private int GetTotalContentHeight()
    {
        if (_groupedColumnIndices.Count > 0)
            return CalculateTreeContentHeight(_groupRoots) + (_allowUserToAddRows ? _rowHeight : 0);
        return _rows.Count * _rowHeight + (_allowUserToAddRows ? _rowHeight : 0);
    }

    public override void Render(Graphics g)
    {
        if (!Visible) return;
        var theme = ThemeManager.CurrentTheme;
        float zoom = EffectiveZoom;
        int gbH = _showGroupingBar ? _groupingBarHeight : 0;
        int hH = _columnHeadersVisible ? _rowHeight : 0;
        int daY = gbH + hH;
        int rhw = _rowHeadersVisible ? 40 : 0;
        bool grouped = _groupedColumnIndices.Count > 0;
        int tch = GetTotalContentHeight();
        int dH = Height - daY;
        bool needVS = tch > dH;
        int sbw = needVS ? 16 : 0;
        int dw = Width - rhw - sbw;
        _vScrollBar.ViewSize = dH;
        _vScrollBar.ContentSize = tch;
        g.FillRectangle(BackColor, 0, 0, Width, Height);
        g.DrawRectangle(theme.DataGridViewBorder, 0, 0, Width, Height, 1);
        if (_showGroupingBar) RenderGroupingBar(g, theme, zoom, gbH, sbw);
        if (_columnHeadersVisible)
        {
            g.FillRectangle(theme.ControlBackground, 0, gbH, Width - sbw, hH);
            g.DrawLine(theme.DataGridViewHeaderSeparator, 0, daY, Width - sbw, daY);
            g.SetClip(new Rectangle(rhw, gbH, dw, hH));
            int cx = rhw - _horizontalScrollOffset;
            for (int c = 0; c < _columns.Count; c++)
            {
                int cw = _columns[c].Width;
                if (cx + cw > rhw && cx < rhw + dw)
                {
                    int dx = Math.Max(cx, rhw);
                    int dw2 = Math.Min(cx + cw, rhw + dw) - dx;
                    if (dw2 > 0)
                    {
                        g.DrawRectangle(theme.DataGridViewBorder, dx, gbH, dw2, hH, 1);
                        g.SetClip(new Rectangle(dx, gbH, dw2, hH));
                        var f = _columns[c].HeaderCell?.Font ?? EffectiveFont;
                        var ht = _columns[c].HeaderText;
                        bool arrow = _columns[c].SortOrder != SortOrder.None;
                        float agx = 0, amy = 0, ahh = 0, ahw = 0;
                        int sgw = 0;
                        if (arrow) { ahh = 3.5f; ahw = 4.5f; sgw = (int)(ahw * 2) + 6; agx = dx + dw2 - sgw + 3; amy = gbH + hH / 2f; }
                        ht = TruncateText(ht, f, zoom, _columns[c].TextAlign, dw2 - 8 - sgw);
                        float htx = GetAlignedX(ht, f, zoom, _columns[c].TextAlign, dx, dw2 - sgw, 4);
                        g.DrawString(ht, f, theme.DataGridViewHeaderText, htx, gbH + CoordinateTransform.CenterVertically(0, hH, f, zoom));
                        if (arrow)
                        {
                            if (_columns[c].SortOrder == SortOrder.Ascending)
                                g.FillTriangle(theme.DataGridViewSortArrow, agx, amy + ahh, agx + ahw * 2, amy + ahh, agx + ahw, amy - ahh);
                            else
                                g.FillTriangle(theme.DataGridViewSortArrow, agx, amy - ahh, agx + ahw * 2, amy - ahh, agx + ahw, amy + ahh);
                        }
                        g.ResetClip();
                    }
                }
                cx += cw;
            }
            g.ResetClip();
        }
        if (grouped) { g.SetClip(new Rectangle(0, daY, Width - sbw, dH)); RenderGroupedContent(g, theme, zoom, daY, dH, rhw, dw); }
        else RenderFlatContent(g, theme, zoom, daY, dH, rhw, dw);
        if (_allowUserToAddRows)
        {
            int ny = daY + tch - _vScrollBar.Value - _rowHeight;
            if (ny + _rowHeight > daY && ny < daY + dH)
            {
                g.FillRectangle(theme.DataGridViewAddNewRowBackground, rhw, ny, dw, _rowHeight);
                g.DrawLine(theme.DataGridViewAddNewRowSeparator, rhw, ny, rhw + dw, ny);
                g.DrawString("*", EffectiveFont, theme.DataGridViewAddNewRowAsterisk, rhw + 4, ny + (_rowHeight - 12 * zoom) / 2f);
                if (_rowHeadersVisible) { g.FillRectangle(theme.ControlBackground, 0, ny, rhw, _rowHeight); g.DrawRectangle(theme.DataGridViewBorder, 0, ny, rhw, _rowHeight, 1); }
            }
        }
        g.ResetClip();
        RenderDragIndicator(g, theme, zoom);
        if (needVS) { _vScrollBar.Render(g, new Rectangle(Width - sbw, daY, sbw, dH), theme); }
        if (Focused) g.DrawRectangle(theme.TextBoxFocusBorder, 0, 0, Width, Height, 2);
        base.Render(g);
    }

    private void RenderGroupingBar(Graphics g, Theme theme, float zoom, int bh, int sbw)
    {
        g.FillRectangle(theme.DataGridViewGroupingBarBackground, 0, 0, Width - sbw, bh);
        g.DrawLine(theme.DataGridViewHeaderSeparator, 0, bh, Width - sbw, bh);
        if (_groupedColumnIndices.Count > 0)
        {
            int px = 4;
            for (int i = 0; i < _groupedColumnIndices.Count; i++)
            {
                int ci = _groupedColumnIndices[i];
                string pt = _columns[ci].HeaderText;
                int tw = CoordinateTransform.MeasureText(pt, _groupHeaderFont, zoom).width;
                int pw = tw + 24;
                int ph = bh - 6;
                int py = (bh - ph) / 2;
                var pbg = (i == _dragRemoveLevelIndex && _isDraggingPillOut) ? Color.FromArgb(255, 100, 100) : theme.DataGridViewGroupingBarPillBackground;
                g.FillRectangle(pbg, px, py, pw, ph);
                g.DrawRectangle(theme.DataGridViewBorder, px, py, pw, ph, 1);
                g.DrawString(pt, _groupHeaderFont, theme.DataGridViewGroupingBarText, px + 4, py + (ph - _groupHeaderFont.Size * zoom) / 2f);
                g.DrawString("\u00D7", _groupHeaderFont, theme.DataGridViewGroupingBarText, px + pw - 16, py + (ph - _groupHeaderFont.Size * zoom) / 2f);
                px += pw + 4;
            }
        }
        else
        {
            const string hint = "Spalte hierher ziehen zum Gruppieren";
            g.DrawString(hint, _groupHeaderFont, theme.DataGridViewGroupingBarText, 4, (bh - _groupHeaderFont.Size * zoom) / 2f);
        }
        // Highlight grouping bar when dragging a column over it
        if (_isDraggingIntoBar && _dragCurrentY >= 0 && _dragCurrentY < bh)
        {
            g.FillRectangle(Color.FromArgb(60, 100, 180, 255), 0, 0, Width - sbw, bh);
        }
    }

    private void RenderDragIndicator(Graphics g, Theme theme, float zoom)
    {
        if (!_isDraggingIntoBar || _dragSourceColumnIndex < 0) return;
        int ci = _dragSourceColumnIndex;
        if (ci >= _columns.Count) return;
        string text = _columns[ci].HeaderText;
        var font = _groupHeaderFont;
        int tw = CoordinateTransform.MeasureText(text, font, zoom).width;
        int pw = tw + 16;
        int ph = _groupingBarHeight;
        int px = _dragCurrentX - pw / 2;
        int py = _dragCurrentY - ph / 2;
        g.FillRectangle(Color.FromArgb(200, 80, 120, 200), px, py, pw, ph);
        g.DrawRectangle(theme.DataGridViewBorder, px, py, pw, ph, 1);
        g.DrawString(text, font, Color.White, px + 4, py + (ph - font.Size * zoom) / 2f);
    }

    private void RenderFlatContent(Graphics g, Theme theme, float zoom, int daY, int dH, int rhw, int dw)
    {
        int so = _vScrollBar.Value;
        int rs = Math.Max(0, so / _rowHeight);
        int re = Math.Min(_rows.Count, rs + (dH / _rowHeight) + 2);
        if (_rowHeadersVisible)
        {
            g.SetClip(new Rectangle(0, daY, rhw, dH));
            for (int i = rs; i < re; i++)
            {
                int y = daY + i * _rowHeight - so;
                bool sel = i == _selectedRowIndex;
                var bg = sel ? theme.Highlight : theme.ControlBackground;
                g.FillRectangle(bg, 0, y, rhw, _rowHeight);
                g.DrawRectangle(theme.DataGridViewBorder, 0, y, rhw, _rowHeight, 1);
                g.DrawString((i + 1).ToString(), EffectiveFont, sel ? theme.HighlightText : theme.DataGridViewRowHeaderText, 4, y + (int)CoordinateTransform.CenterVertically(0, _rowHeight, EffectiveFont, zoom));
            }
            g.ResetClip();
        }
        g.SetClip(new Rectangle(rhw, daY, dw, dH));
        for (int i = rs; i < re; i++)
        {
            int y = daY + i * _rowHeight - so;
            bool sel = i == _selectedRowIndex;
            if (sel) g.FillRectangle(theme.Highlight, rhw, y, dw, _rowHeight);
            else if (i % 2 == 1) g.FillRectangle(theme.AlternateRow, rhw, y, dw, _rowHeight);
        }
        if (_showGridLines)
        {
            for (int i = rs; i < re; i++)
            {
                int y = daY + i * _rowHeight - so;
                g.DrawLine(theme.GridLine, rhw, y + _rowHeight, rhw + dw, y + _rowHeight);
                int x = rhw - _horizontalScrollOffset;
                for (int c = 0; c < _columns.Count; c++)
                {
                    int cw = _columns[c].Width;
                    if (x + cw > rhw && x < rhw + dw) { int dx = Math.Max(x, rhw); g.DrawLine(theme.GridLineVertical, dx, y, dx, y + _rowHeight); }
                    x += cw;
                }
            }
        }
        for (int i = rs; i < re; i++)
        {
            int y = daY + i * _rowHeight - so;
            bool sel = i == _selectedRowIndex;
            var tc = sel ? theme.HighlightText : theme.DataGridViewCellText;
            int x = rhw - _horizontalScrollOffset;
            for (int c = 0; c < _columns.Count; c++)
            {
                int cw = _columns[c].Width;
                if (x + cw > rhw && x < rhw + dw)
                {
                    int dx = Math.Max(x, rhw);
                    int dw2 = Math.Min(x + cw, rhw + dw) - dx;
                    if (dw2 > 0)
                    {
                        var cell = _rows[i].Cells.Count > c ? _rows[i].Cells[c] : null;
                        var cd = _columns[c];
                        var t = FormatCellValue(cell?.Value, cd.FormatString);
                        var f = EffectiveFont;
                        int av = dw2 - 8;
                        t = TruncateText(t, f, zoom, cd.TextAlign, av > 0 ? av : 0);
                        float tx = GetAlignedX(t, f, zoom, cd.TextAlign, dx, dw2, 4);
                        g.DrawString(t, f, tc, tx, y + (int)CoordinateTransform.CenterVertically(0, _rowHeight, f, zoom));
                    }
                }
                x += cw;
            }
        }
        g.ResetClip();
    }

    private void RenderGroupedContent(Graphics g, Theme theme, float zoom, int daY, int dH, int rhw, int dw)
    {
        int so = _vScrollBar.Value;
        int ve = so + dH;
        int cp = 0;
        int gr = 0;
        foreach (var group in _groupRoots)
            RenderGroupRecursive(g, theme, zoom, group, daY, so, ve, rhw, dw, dH, ref cp, ref gr);
    }

    private void RenderGroupRecursive(Graphics g, Theme theme, float zoom, DataGridViewGroup group, int daY, int so, int ve, int rhw, int dw, int dH, ref int cp, ref int gr)
    {
        int hy = daY + cp - so;
        if (cp + _groupHeaderHeight > so && cp < ve)
        {
            var bg = group.CustomBackColor ?? _groupHeaderBackColor;
            var fg = group.CustomForeColor ?? _groupHeaderForeColor;
            var f = group.CustomFont ?? _groupHeaderFont;
            int indent = group.Level * _groupHeaderIndent;
            string txt = group.CustomHeaderText ?? "";
            g.FillRectangle(bg, 0, hy, Width, _groupHeaderHeight);
            g.DrawLine(theme.DataGridViewHeaderSeparator, 0, hy + _groupHeaderHeight - 1, Width, hy + _groupHeaderHeight - 1);
            string ind = group.IsCollapsed ? "\u25B6 " : "\u25BC ";
            int iw = CoordinateTransform.MeasureText(ind, f, zoom).width;
            g.DrawString(ind, f, fg, indent + 4, hy + CoordinateTransform.CenterVertically(0, _groupHeaderHeight, f, zoom));
            int tx = indent + 4 + iw;
            float ty = hy + CoordinateTransform.CenterVertically(0, _groupHeaderHeight, f, zoom);
            int mw = Width - tx - 8;
            string disp = TruncateText(txt, f, zoom, DataGridViewContentAlignment.Left, mw);
            g.DrawString(disp, f, fg, tx, ty);
            if (group.IsCollapsed)
            {
                string cnt = $" [{group.TotalRowCount}]";
                g.DrawString(cnt, f, fg, tx + CoordinateTransform.MeasureText(txt, f, zoom).width + 4, ty);
            }
        }
        cp += _groupHeaderHeight;
        if (!group.IsCollapsed && cp < ve)
        {
            if (group.ChildGroups.Count > 0)
            {
                foreach (var child in group.ChildGroups)
                {
                    RenderGroupRecursive(g, theme, zoom, child, daY, so, ve, rhw, dw, dH, ref cp, ref gr);
                    if (cp >= ve + _rowHeight) break;
                }
            }
            else
            {
                foreach (int ri in group.RowIndices)
                {
                    int ry = daY + cp - so;
                    if (cp + _rowHeight > so && cp < ve)
                    {
                        bool sel = ri == _selectedRowIndex;
                        bool alt = gr % 2 == 1;
                        if (_rowHeadersVisible)
                        {
                            g.SetClip(new Rectangle(0, daY, rhw, dH));
                            var hbg = sel ? theme.Highlight : theme.ControlBackground;
                            g.FillRectangle(hbg, 0, ry, rhw, _rowHeight);
                            g.DrawRectangle(theme.DataGridViewBorder, 0, ry, rhw, _rowHeight, 1);
                            g.DrawString((gr + 1).ToString(), EffectiveFont, sel ? theme.HighlightText : theme.DataGridViewRowHeaderText, 4, ry + (int)CoordinateTransform.CenterVertically(0, _rowHeight, EffectiveFont, zoom));
                            g.ResetClip();
                        }
                        g.SetClip(new Rectangle(rhw, daY, dw, dH));
                        if (sel) g.FillRectangle(theme.Highlight, rhw, ry, dw, _rowHeight);
                        else if (alt) g.FillRectangle(theme.AlternateRow, rhw, ry, dw, _rowHeight);
                        if (_showGridLines) g.DrawLine(theme.GridLine, rhw, ry + _rowHeight, rhw + dw, ry + _rowHeight);
                        int x = rhw - _horizontalScrollOffset;
                        for (int c = 0; c < _columns.Count; c++)
                        {
                            int cw = _columns[c].Width;
                            if (x + cw > rhw && x < rhw + dw)
                            {
                                int dx = Math.Max(x, rhw);
                                int dw2 = Math.Min(x + cw, rhw + dw) - dx;
                                if (dw2 > 0 && _showGridLines) g.DrawLine(theme.GridLineVertical, dx, ry, dx, ry + _rowHeight);
                                if (dw2 > 0)
                                {
                                    var cell = _rows[ri].Cells.Count > c ? _rows[ri].Cells[c] : null;
                                    var cd = _columns[c];
                                    var t = FormatCellValue(cell?.Value, cd.FormatString);
                                    var f = EffectiveFont;
                                    var tc = sel ? theme.HighlightText : theme.DataGridViewCellText;
                                    int av = dw2 - 8;
                                    t = TruncateText(t, f, zoom, cd.TextAlign, av > 0 ? av : 0);
                                    float tx = GetAlignedX(t, f, zoom, cd.TextAlign, dx, dw2, 4);
                                    g.DrawString(t, f, tc, tx, ry + (int)CoordinateTransform.CenterVertically(0, _rowHeight, f, zoom));
                                }
                            }
                            x += cw;
                        }
                        g.ResetClip();
                    }
                    cp += _rowHeight;
                    gr++;
                    if (cp >= ve + _rowHeight) break;
                }
            }
        }
        else if (group.IsCollapsed) { cp += GroupSubtreeHeight(group) - _groupHeaderHeight; gr += group.TotalRowCount; }
    }

    protected internal override void OnMouseDown(EventArgs e)
    {
        var m = e as MouseEventArgs;
        if (m == null) { base.OnMouseDown(e); return; }
        int gbH = _showGroupingBar ? _groupingBarHeight : 0;
        int hH = _columnHeadersVisible ? _rowHeight : 0;
        int daY = gbH + hH;
        int rhw = _rowHeadersVisible ? 40 : 0;
        int tch = GetTotalContentHeight();
        int dH = Height - daY;
        bool needVS = tch > dH;
        int sbw = needVS ? 16 : 0;
        if (needVS && m.X >= Width - sbw) { _vScrollBar.HandleMouseDown(new Point(m.X, m.Y), new Rectangle(Width - sbw, daY, sbw, dH), VScrollBarContext); return; }
        int dc = GetDividerColumnIndex(m.X);
        if (dc >= 0 && _columns[dc].Resizable) { _resizingColumnIndex = dc; _resizeStartMouseX = m.X; _resizeStartWidth = _columns[dc].Width; var f = FindForm(); if (f != null) f.CaptureControl = this; return; }
        if (_showGroupingBar && m.Y < gbH) { HandleGroupingBarClick(m); return; }
        int col = GetColumnIndexAtX(m.X);
        bool grouped = _groupedColumnIndices.Count > 0;
        if (m.Y >= gbH && m.Y < daY && col >= 0 && col < _columns.Count)
        {
            if (_showGroupingBar && col >= 0) { _dragSourceColumnIndex = col; _dragStartX = m.X; _dragStartY = m.Y; }
            OnColumnHeaderMouseClick(new DataGridViewCellEventArgs(col, -1));
            if (_columns[col].Sortable)
            {
                if (grouped)
                {
                    if (!_groupedColumnIndices.Contains(col))
                    {
                        if (col == _secondarySortColumnIndex) _secondarySortAscending = !_secondarySortAscending;
                        else { _secondarySortColumnIndex = col; _secondarySortAscending = true; }
                        ClearSortColumns(); _columns[col].SortOrder = _secondarySortAscending ? SortOrder.Ascending : SortOrder.Descending; SortRowsWithinGroups(); Invalidate();
                    }
                }
                else
                {
                    if (col == _sortColumnIndex) _sortAscending = !_sortAscending;
                    else { ClearSortColumns(); _sortColumnIndex = col; _sortAscending = true; }
                    _columns[col].SortOrder = _sortAscending ? SortOrder.Ascending : SortOrder.Descending; SortRows(); Invalidate();
                }
            }
            return;
        }
        if (m.Y >= daY)
        {
            if (grouped)
            {
                var hit = HitTestGroupTree(_groupRoots, m.Y - daY + _vScrollBar.Value);
                if (hit.group != null)
                {
                    if (hit.rowIndex < 0) { hit.group.IsCollapsed = !hit.group.IsCollapsed; Invalidate(); }
                    else if (hit.rowIndex < hit.group.RowIndices.Count)
                    {
                        int actual = hit.group.RowIndices[hit.rowIndex];
                        if (actual >= 0 && actual < _rows.Count && col >= 0 && col < _columns.Count) { _selectedRowIndex = actual; _selectedColumnIndex = col; OnCellClick(new DataGridViewCellEventArgs(col, actual)); OnSelectionChanged(); Invalidate(); }
                    }
                }
            }
            else
            {
                int row = (m.Y - daY + _vScrollBar.Value) / _rowHeight;
                if (row >= 0 && row < _rows.Count && col >= 0 && col < _columns.Count) { _selectedRowIndex = row; _selectedColumnIndex = col; OnCellClick(new DataGridViewCellEventArgs(col, row)); OnSelectionChanged(); Invalidate(); }
                else if (row >= _rows.Count && _allowUserToAddRows) AddRow();
            }
        }
        base.OnMouseDown(e);
    }

    private void HandleGroupingBarClick(MouseEventArgs m)
    {
        int px = 4;
        for (int i = 0; i < _groupedColumnIndices.Count; i++)
        {
            int ci = _groupedColumnIndices[i];
            string pt = _columns[ci].HeaderText;
            int tw = CoordinateTransform.MeasureText(pt, _groupHeaderFont, EffectiveZoom).width;
            int pw = tw + 24;
            int ph = _groupingBarHeight - 6;
            int py = (_groupingBarHeight - ph) / 2;
            int cx = px + pw - 16;
            if (m.X >= cx && m.X <= cx + 14 && m.Y >= py && m.Y <= py + ph) { RemoveGroupColumn(i); return; }
            if (m.X >= px && m.X <= px + pw && m.Y >= py && m.Y <= py + ph) { _isDraggingPillOut = true; _dragRemoveLevelIndex = i; _dragStartY = m.Y; var f = FindForm(); if (f != null) f.CaptureControl = this; return; }
            px += pw + 4;
        }
    }

    protected internal override void OnMouseUp(EventArgs e)
    {
        var m = e as MouseEventArgs;
        if (_isDraggingIntoBar)
        {
            _isDraggingIntoBar = false; var f = FindForm(); if (f != null) f.CaptureControl = null;
            if (m != null && _showGroupingBar && m.Y >= 0 && m.Y < _groupingBarHeight) AddGroupColumn(_dragSourceColumnIndex);
            _dragSourceColumnIndex = -1; Invalidate(); return;
        }
        if (_isDraggingPillOut)
        {
            _isDraggingPillOut = false; var f = FindForm(); if (f != null) f.CaptureControl = null;
            if (m != null) { int bh = _showGroupingBar ? _groupingBarHeight : 0; if (m.Y >= bh || m.Y < 0) RemoveGroupColumn(_dragRemoveLevelIndex); }
            _dragRemoveLevelIndex = -1; Invalidate(); return;
        }
        if (_resizingColumnIndex >= 0) { _resizingColumnIndex = -1; var f = FindForm(); if (f != null) f.CaptureControl = null; return; }
        if (_vScrollBar.IsDragging || _vScrollBar.IsUpButtonPressed || _vScrollBar.IsDownButtonPressed) _vScrollBar.HandleMouseUp(VScrollBarContext);
        base.OnMouseUp(e);
    }

    protected internal override void OnMouseMove(EventArgs e)
    {
        var m = e as MouseEventArgs;
        if (m == null) { base.OnMouseMove(e); return; }
        if (_resizingColumnIndex >= 0) { _columns[_resizingColumnIndex].Width = Math.Max(MinColumnWidth, _resizeStartWidth + m.X - _resizeStartMouseX); Invalidate(); return; }
        if (_vScrollBar.IsDragging)
        {
            int daY = GetDataAreaY(); int tch = GetTotalContentHeight(); int dH = Height - daY; bool needVS = tch > dH; int sbw = needVS ? 16 : 0;
            _vScrollBar.HandleMouseMove(new Point(m.X, m.Y), new Rectangle(Width - sbw, daY, sbw, dH), VScrollBarContext); return;
        }
        if (_dragSourceColumnIndex >= 0 && _showGroupingBar)
        {
            _dragCurrentX = m.X;
            _dragCurrentY = m.Y;
            if (Math.Abs(m.Y - _dragStartY) > DragThreshold && !_isDraggingIntoBar) { _isDraggingIntoBar = true; var f = FindForm(); if (f != null) f.CaptureControl = this; Invalidate(); return; }
            if (_isDraggingIntoBar) { Invalidate(); return; }
        }
        if (_isDraggingPillOut) { _dragCurrentX = m.X; _dragCurrentY = m.Y; if (Math.Abs(m.Y - _dragStartY) > DragThreshold) Invalidate(); return; }
        var form = FindForm();
        int dc = GetDividerColumnIndex(m.X);
        if (dc >= 0 && _columns[dc].Resizable) { if (form != null) form.Cursor = SystemCursorType.SizeWE; }
        else { if (form != null && form.Cursor == SystemCursorType.SizeWE) form.Cursor = null; }
        base.OnMouseMove(e);
    }

    protected internal override void OnMouseWheel(EventArgs e)
    {
        if (_vScrollBar.IsDragging) return;
        var m = e as MouseEventArgs;
        if (m != null) { int daY = GetDataAreaY(); _vScrollBar.ViewSize = Height - daY; _vScrollBar.ContentSize = GetTotalContentHeight(); if (_vScrollBar.NeedsScrollbar) _vScrollBar.HandleMouseWheel(m.Delta, VScrollBarContext); }
        base.OnMouseWheel(e);
    }

    protected internal override void OnKeyDown(KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Up: if (_selectedRowIndex > 0) { _selectedRowIndex--; EnsureRowVisible(_selectedRowIndex); OnSelectionChanged(); Invalidate(); e.Handled = true; } break;
            case Keys.Down: if (_selectedRowIndex < _rows.Count - 1) { _selectedRowIndex++; EnsureRowVisible(_selectedRowIndex); OnSelectionChanged(); Invalidate(); e.Handled = true; } break;
            case Keys.Left: if (_selectedColumnIndex > 0) { _selectedColumnIndex--; OnSelectionChanged(); Invalidate(); e.Handled = true; } break;
            case Keys.Right: if (_selectedColumnIndex < _columns.Count - 1) { _selectedColumnIndex++; OnSelectionChanged(); Invalidate(); e.Handled = true; } break;
            case Keys.Home: if (_rows.Count > 0) { _selectedRowIndex = 0; _selectedColumnIndex = 0; _vScrollBar.Value = 0; _horizontalScrollOffset = 0; OnSelectionChanged(); Invalidate(); e.Handled = true; } break;
            case Keys.End: if (_rows.Count > 0) { _selectedRowIndex = _rows.Count - 1; _selectedColumnIndex = _columns.Count > 0 ? _columns.Count - 1 : 0; EnsureRowVisible(_selectedRowIndex); OnSelectionChanged(); Invalidate(); e.Handled = true; } break;
            case Keys.PageUp: if (_rows.Count > 0 && _selectedRowIndex > 0) { int vis = (Height - GetDataAreaY()) / _rowHeight; _selectedRowIndex = Math.Max(0, _selectedRowIndex - vis); EnsureRowVisible(_selectedRowIndex); OnSelectionChanged(); Invalidate(); e.Handled = true; } break;
            case Keys.PageDown: if (_rows.Count > 0 && _selectedRowIndex < _rows.Count - 1) { int vis = (Height - GetDataAreaY()) / _rowHeight; _selectedRowIndex = Math.Min(_rows.Count - 1, _selectedRowIndex + vis); EnsureRowVisible(_selectedRowIndex); OnSelectionChanged(); Invalidate(); e.Handled = true; } break;
        }
        if (e.Modifiers.HasFlag(ModifierKeys.Control) && e.KeyCode == Keys.C) { Copy(); e.Handled = true; }
        base.OnKeyDown(e);
    }

    private void EnsureRowVisible(int rowIndex)
    {
        int daY = GetDataAreaY(); int dH = Height - daY; int rt = rowIndex * _rowHeight; int rb = rt + _rowHeight;
        if (rt < _vScrollBar.Value) _vScrollBar.Value = rt;
        else if (rb > _vScrollBar.Value + dH) _vScrollBar.Value = rb - dH;
    }

    protected virtual void OnDataSourceChanged()
    {
        if (_dataSourceUpdating) return;
        _dataSourceUpdating = true;
        try
        {
            _rows.Clear(); ClearSort(); _selectedRowIndex = -1; _selectedColumnIndex = -1;
            if (_dataSource is System.Collections.IEnumerable enumerable && _dataSource is not string) { foreach (var item in enumerable) _rows.Add(CreateRowFromDataItem(item)); }
            if (_dataSource is IBindingList bindingList) bindingList.ListChanged += OnDataSourceListChanged;
            if (_groupedColumnIndices.Count > 0) BuildGroups();
            Invalidate();
        }
        finally { _dataSourceUpdating = false; }
    }

    protected DataGridViewRow CreateRowFromDataItem(object? item)
    {
        var row = new DataGridViewRow { DataBoundItem = item };
        for (int c = 0; c < _columns.Count; c++)
        {
            object? v = null;
            if (item != null && !string.IsNullOrEmpty(_columns[c].DataPropertyName)) { var p = item.GetType().GetProperty(_columns[c].DataPropertyName); if (p != null) v = p.GetValue(item); }
            row.Cells.Add(CreateCell(v));
        }
        return row;
    }

    protected virtual void OnDataSourceListChanged(object? sender, ListChangedEventArgs e)
    {
        if (_dataSource is not IBindingList bl) return;
        bool g = _groupedColumnIndices.Count > 0;
        switch (e.ListChangedType)
        {
            case ListChangedType.ItemAdded:
                if (e.NewIndex >= 0 && e.NewIndex <= _rows.Count)
                {
                    var item = bl[e.NewIndex]; var nr = CreateRowFromDataItem(item);
                    if (e.NewIndex < _rows.Count)
                    {
                        var tmp = new List<DataGridViewRow>(); while (_rows.Count > e.NewIndex) { tmp.Add(_rows[_rows.Count - 1]); _rows.Remove(_rows[_rows.Count - 1]); }
                        _rows.Add(nr); while (tmp.Count > 0) { var r = tmp[tmp.Count - 1]; tmp.RemoveAt(tmp.Count - 1); _rows.Add(r); }
                    }
                    else _rows.Add(nr);
                    if (g) BuildGroups(); else if (_sortColumnIndex >= 0) SortRows(); Invalidate();
                }
                break;
            case ListChangedType.ItemDeleted:
                if (e.NewIndex >= 0 && e.NewIndex < _rows.Count)
                {
                    var rows = new List<DataGridViewRow>(); for (int i = 0; i < _rows.Count; i++) if (i != e.NewIndex) rows.Add(_rows[i]);
                    _rows.Clear(); foreach (var r in rows) _rows.Add(r);
                    if (_selectedRowIndex >= _rows.Count) _selectedRowIndex = Math.Max(0, _rows.Count - 1);
                    if (g) BuildGroups(); else if (_sortColumnIndex >= 0) SortRows(); Invalidate();
                }
                break;
            case ListChangedType.ItemChanged:
                if (e.NewIndex >= 0 && e.NewIndex < _rows.Count && e.NewIndex < bl.Count)
                {
                    var item = bl[e.NewIndex]; _rows[e.NewIndex].DataBoundItem = item;
                    if (!string.IsNullOrEmpty(_columns[0].DataPropertyName))
                    {
                        var ur = CreateRowFromDataItem(item); _rows[e.NewIndex].Cells.Clear();
                        foreach (var c in ur.Cells) _rows[e.NewIndex].Cells.Add(c);
                    }
                    if (g) BuildGroups(); else if (_sortColumnIndex >= 0) SortRows(); Invalidate();
                }
                break;
            case ListChangedType.Reset:
                _rows.Clear(); foreach (var item in bl) _rows.Add(CreateRowFromDataItem(item));
                if (g) BuildGroups(); else if (_sortColumnIndex >= 0) SortRows();
                _selectedRowIndex = _rows.Count > 0 ? 0 : -1; Invalidate();
                break;
        }
    }

    internal void NotifyCellValueChanged(int columnIndex, int rowIndex, object? newValue)
    {
        if (_dataSourceUpdating) return;
        if (_dataSource is IBindingList && rowIndex >= 0 && rowIndex < _rows.Count)
        {
            var row = _rows[rowIndex]; var di = row.DataBoundItem;
            if (di != null && columnIndex >= 0 && columnIndex < _columns.Count)
            {
                var cd = _columns[columnIndex];
                if (!string.IsNullOrEmpty(cd.DataPropertyName)) { var p = di.GetType().GetProperty(cd.DataPropertyName); if (p != null && p.CanWrite) p.SetValue(di, newValue); }
            }
        }
    }

    private DataGridViewCell CreateCell(object? value) { var cell = new DataGridViewCell { Value = value }; cell.OnValueChanged = Invalidate; return cell; }

    public void AddRow(params object[] values)
    {
        var row = new DataGridViewRow();
        for (int i = 0; i < _columns.Count; i++) row.Cells.Add(CreateCell(i < values.Length ? values[i] : ""));
        _rows.Add(row);
        if (_groupedColumnIndices.Count > 0) BuildGroups();
        Invalidate();
    }

    public void Clear() { _rows.Clear(); ClearSort(); _groupRoots.Clear(); _allGroups.Clear(); _selectedRowIndex = -1; _selectedColumnIndex = -1; Invalidate(); }

    public void ClearSort() { _sortColumnIndex = -1; _secondarySortColumnIndex = -1; for (int i = 0; i < _columns.Count; i++) _columns[i].SortOrder = SortOrder.None; }

    private void ClearSortColumns() { for (int i = 0; i < _columns.Count; i++) _columns[i].SortOrder = SortOrder.None; }

    private void SortRows()
    {
        if (_sortColumnIndex < 0 || _sortColumnIndex >= _columns.Count) return;
        int ci = _sortColumnIndex; bool asc = _sortAscending;
        _rows.Sort((a, b) => { var va = ci < a.Cells.Count ? a.Cells[ci]?.Value : null; var vb = ci < b.Cells.Count ? b.Cells[ci]?.Value : null; int r = CompareCellValues(va, vb); return asc ? r : -r; });
    }

    private static int CompareCellValues(object? a, object? b)
    {
        if (a == null && b == null) return 0; if (a == null) return -1; if (b == null) return 1;
        if (a is IComparable ca && b is IComparable cb) { try { return ca.CompareTo(cb); } catch { } }
        return string.Compare(a.ToString(), b.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    protected string? GetClipboardValue() => SelectedValue?.ToString();
    public void Copy() { var v = GetClipboardValue(); if (!string.IsNullOrEmpty(v)) Core.Clipboard.SetText(v); }

    protected void BuildGroups()
    {
        _groupRoots.Clear(); _allGroups.Clear();
        if (_groupedColumnIndices.Count == 0 || _rows.Count == 0) return;
        _rows.Sort((a, b) =>
        {
            for (int i = 0; i < _groupedColumnIndices.Count; i++)
            {
                int ci = _groupedColumnIndices[i];
                var va = ci < a.Cells.Count ? a.Cells[ci]?.Value : null;
                var vb = ci < b.Cells.Count ? b.Cells[ci]?.Value : null;
                int r = CompareCellValues(va, vb); if (r != 0) return r;
            }
            return 0;
        });
        int ri = 0;
        while (ri < _rows.Count)
        {
            DataGridViewGroup? cur = null;
            List<DataGridViewGroup> level = _groupRoots;
            for (int lv = 0; lv < _groupedColumnIndices.Count; lv++)
            {
                int ci = _groupedColumnIndices[lv];
                var cv = ci < _rows[ri].Cells.Count ? _rows[ri].Cells[ci]?.Value : null;
                DataGridViewGroup? match = null;
                foreach (var g in level) { if (g.ColumnIndex == ci && CompareCellValues(g.Key, cv) == 0) { match = g; break; } }
                if (match == null) { match = new DataGridViewGroup(ci, lv, cv, cur); _allGroups.Add(match); level.Add(match); }
                cur = match;
                level = (List<DataGridViewGroup>)match.ChildGroups;
            }
            while (ri < _rows.Count)
            {
                bool ok = true; var tg = cur; int tr = ri;
                for (int lv = _groupedColumnIndices.Count - 1; lv >= 0; lv--)
                {
                    if (tg == null) break;
                    int ci = _groupedColumnIndices[lv]; var cv = ci < _rows[tr].Cells.Count ? _rows[tr].Cells[ci]?.Value : null;
                    if (CompareCellValues(tg.Key, cv) != 0) { ok = false; break; }
                    tg = tg.Parent;
                }
                if (!ok) break;
                cur?.AddRowIndex(ri);
                ri++;
            }
        }
        foreach (var g in _allGroups)
        {
            int ci = g.ColumnIndex; string fv = FormatCellValue(g.Key, _columns[ci].FormatString);
            string def = $"{_columns[ci].HeaderText} : {fv}";
            var args = new DataGridViewGroupHeaderFormattingEventArgs(ci, g.Level, g.Key, g.TotalRowCount, def);
            OnGroupHeaderFormatting(args);
            g.CustomHeaderText = args.HeaderText; g.CustomFont = args.Font; g.CustomForeColor = args.ForeColor; g.CustomBackColor = args.BackColor; g.CustomTextAlign = args.TextAlign;
        }
        if (_secondarySortColumnIndex >= 0) SortRowsWithinGroups();
    }

    protected void SortRowsWithinGroups()
    {
        if (_secondarySortColumnIndex < 0) return;
        int sc = _secondarySortColumnIndex; bool asc = _secondarySortAscending;
        foreach (var g in _allGroups) { if (g.RowIndices.Count > 0) g.SortRowIndices((a, b) => { var va = _rows[a].Cells.Count > sc ? _rows[a].Cells[sc]?.Value : null; var vb = _rows[b].Cells.Count > sc ? _rows[b].Cells[sc]?.Value : null; int r = CompareCellValues(va, vb); return asc ? r : -r; }); }
    }

    private int CalculateTreeContentHeight(IReadOnlyList<DataGridViewGroup> groups) { int h = 0; foreach (var g in groups) h += GroupSubtreeHeight(g); return h; }

    private int GroupSubtreeHeight(DataGridViewGroup group)
    {
        int h = _groupHeaderHeight;
        if (!group.IsCollapsed) { if (group.ChildGroups.Count > 0) { foreach (var c in group.ChildGroups) h += GroupSubtreeHeight(c); } else h += group.RowIndices.Count * _rowHeight; }
        return h;
    }

    private (DataGridViewGroup? group, int rowIndex) HitTestGroupTree(IReadOnlyList<DataGridViewGroup> groups, int cy)
    {
        foreach (var g in groups)
        {
            if (cy < _groupHeaderHeight) return (g, -1);
            cy -= _groupHeaderHeight;
            if (!g.IsCollapsed)
            {
                if (g.ChildGroups.Count > 0) { var r = HitTestGroupTree(g.ChildGroups, cy); if (r.group != null) return r; }
                else { int ri = cy / _rowHeight; if (ri < g.RowIndices.Count) return (g, ri); cy -= g.RowIndices.Count * _rowHeight; }
            }
            else cy -= GroupSubtreeHeight(g) - _groupHeaderHeight;
        }
        return (null, -1);
    }

    private int GetColumnIndexAtX(int x)
    {
        int rhw = _rowHeadersVisible ? 40 : 0; int cx = rhw - _horizontalScrollOffset;
        for (int i = 0; i < _columns.Count; i++) { int cw = _columns[i].Width; if (x >= cx && x < cx + cw) return i; cx += cw; }
        return -1;
    }

    private int GetDividerColumnIndex(int x)
    {
        int rhw = _rowHeadersVisible ? 40 : 0; int cx = rhw - _horizontalScrollOffset;
        for (int i = 0; i < _columns.Count; i++) { cx += _columns[i].Width; if (Math.Abs(x - cx) <= DividerThreshold) return i; }
        return -1;
    }

    private static string TruncateText(string text, Font font, float zoom, DataGridViewContentAlignment alignment, float maxW)
    {
        if (string.IsNullOrEmpty(text) || zoom <= 0f) return text;
        int mp = (int)(maxW * zoom);
        int tw = CoordinateTransform.MeasureText(text, font, zoom).width;
        if (tw <= mp) return text;
        const string dots = "...";
        int dw = CoordinateTransform.MeasureText(dots, font, zoom).width;
        int av = mp - dw;
        if (av <= 0) return dots;
        if (alignment == DataGridViewContentAlignment.Right) { for (int i = text.Length - 1; i >= 0; i--) { var sub = text.Substring(i); if (CoordinateTransform.MeasureText(sub, font, zoom).width <= av) return dots + sub; } return dots; }
        else { for (int i = 1; i <= text.Length; i++) { var sub = text.Substring(0, i); if (CoordinateTransform.MeasureText(sub, font, zoom).width > av) return text.Substring(0, i - 1) + dots; } return text + dots; }
    }

    private static string FormatCellValue(object? value, string? formatString)
    {
        if (value == null) return "";
        if (formatString != null) { try { return string.Format($"{{0:{formatString}}}", value); } catch { } }
        return value.ToString() ?? "";
    }

    private static float GetAlignedX(string text, Font font, float zoom, DataGridViewContentAlignment alignment, float cx, float cw, int pad)
    {
        if (alignment == DataGridViewContentAlignment.Left || string.IsNullOrEmpty(text)) return cx + pad;
        float tw = CoordinateTransform.MeasureText(text, font, zoom).width / Math.Max(zoom, 0.001f);
        return alignment switch { DataGridViewContentAlignment.Center => cx + (cw - tw) / 2f, DataGridViewContentAlignment.Right => cx + cw - tw - pad, _ => cx + pad };
    }
}

internal sealed class ScrollBarContext : IScrollBarContext
{
    private readonly DataGridView _owner;
    public ScrollBarContext(DataGridView owner) => _owner = owner;
    public float Zoom => _owner.EffectiveZoom;
    public void Invalidate() => _owner.Invalidate();
    public void CaptureMouse(bool capture) => _owner.CapturingMouse = capture;
}

public enum DataGridViewSelectionMode
{
    RowHeaderSelect, ColumnHeaderSelect, FullRowSelect, FullColumnSelect, CellSelect
}
