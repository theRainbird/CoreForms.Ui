using System.ComponentModel;
using CoreForms.Ui.Core;
using CoreForms.Ui.Data;
using CoreForms.Ui.Theming;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Advanced;

/// <summary>
/// A control that displays data in a grid format with rows and columns.
/// </summary>
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

    private ScrollBarContext VScrollBarContext => _scrollBarContext ??= new ScrollBarContext(this);

    /// <summary>
    /// Initializes a new instance of DataGridView.
    /// </summary>
    public DataGridView()
    {
        _columns = new DataGridViewColumnCollection(Invalidate);
        _rows = new DataGridViewRowCollection(Invalidate);
        var theme = ThemeManager.CurrentTheme;
        _backColor = theme.TextBoxBackground;
        Size = new Size(400, 200);
        TabStop = true;
        _vScrollBar.SmallChange = _rowHeight;
        _vScrollBar.LargeChange = _rowHeight * 3;
        _vScrollBar.Scroll += (s, e) => Invalidate();
    }

    /// <summary>
    /// Called when the theme changes. Updates datagridview-specific colors.
    /// </summary>
    /// <param name="newTheme">The new theme that was activated.</param>
    public override void OnThemeChanged(Theme newTheme)
    {
        if (!_backColorSet)
            _backColor = newTheme.TextBoxBackground;
        if (!_foreColorSet)
            _foreColor = newTheme.ControlText;
        Invalidate();
    }

    /// <summary>
    /// Gets the collection of columns.
    /// </summary>
    public DataGridViewColumnCollection Columns => _columns;

    /// <summary>
    /// Gets the collection of rows.
    /// </summary>
    public DataGridViewRowCollection Rows => _rows;

    /// <summary>
    /// Gets or sets the index of the selected row.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the index of the selected column.
    /// </summary>
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

    /// <summary>
    /// Gets or sets whether the user can add rows.
    /// </summary>
    public bool AllowUserToAddRows
    {
        get => _allowUserToAddRows;
        set
        {
            if (_allowUserToAddRows != value)
            {
                _allowUserToAddRows = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the user can delete rows.
    /// </summary>
    public bool AllowUserToDeleteRows
    {
        get => _allowUserToDeleteRows;
        set
        {
            if (_allowUserToDeleteRows != value)
            {
                _allowUserToDeleteRows = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the grid is read-only.
    /// </summary>
    public bool ReadOnly
    {
        get => _readOnly;
        set
        {
            if (_readOnly != value)
            {
                _readOnly = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether multiple rows can be selected.
    /// </summary>
    public bool MultiSelect
    {
        get => _multiSelect;
        set
        {
            if (_multiSelect != value)
            {
                _multiSelect = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether column headers are visible.
    /// </summary>
    public bool ColumnHeadersVisible
    {
        get => _columnHeadersVisible;
        set
        {
            _columnHeadersVisible = value;
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets whether row headers are visible.
    /// </summary>
    public bool RowHeadersVisible
    {
        get => _rowHeadersVisible;
        set
        {
            _rowHeadersVisible = value;
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets whether grid lines are shown.
    /// </summary>
    public bool ShowGridLines
    {
        get => _showGridLines;
        set
        {
            _showGridLines = value;
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the selection mode.
    /// </summary>
    public DataGridViewSelectionMode SelectionMode
    {
        get => _selectionMode;
        set
        {
            if (_selectionMode != value)
            {
                _selectionMode = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets the data source for this DataGridView.
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
    /// Gets or sets the data member (property path) for nested list data sources.
    /// </summary>
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

    /// <summary>
    /// Gets the selected row.
    /// </summary>
    public DataGridViewRow? SelectedRow => _selectedRowIndex >= 0 && _selectedRowIndex < _rows.Count
        ? _rows[_selectedRowIndex]
        : null;

    /// <summary>
    /// Gets the value of the selected cell.
    /// </summary>
    public object? SelectedValue => SelectedRow != null && _selectedColumnIndex >= 0 && _selectedColumnIndex < _columns.Count
        ? SelectedRow.Cells[_selectedColumnIndex]?.Value
        : null;

    /// <summary>
    /// Gets the value of this control to copy to the clipboard.
    /// Returns the selected cell's value as string.
    /// </summary>
    /// <returns>The selected cell value as string, or null if no selection.</returns>
    protected string? GetClipboardValue() => SelectedValue?.ToString();

    /// <summary>
    /// Copies the selected cell's value to the clipboard.
    /// </summary>
    public void Copy()
    {
        var value = GetClipboardValue();
        if (!string.IsNullOrEmpty(value))
        {
            Core.Clipboard.SetText(value);
        }
    }

    /// <summary>
    /// Occurs when the selection changes.
    /// </summary>
    public event EventHandler? SelectionChanged;

    /// <summary>
    /// Occurs when a cell is clicked.
    /// </summary>
    public event EventHandler? CellClick;

    /// <summary>
    /// Occurs when a cell value changes.
    /// </summary>
    public event EventHandler<DataGridViewCellEventArgs>? CellValueChanged;

    /// <summary>
    /// Occurs when a column header is clicked.
    /// </summary>
    public event EventHandler<DataGridViewCellEventArgs>? ColumnHeaderMouseClick;

    /// <summary>
    /// Raises the SelectionChanged event and syncs the BindingSource/CurrencyManager position.
    /// </summary>
    protected virtual void OnSelectionChanged()
    {
        if (!_dataSourceUpdating && _dataSource != null && _selectedRowIndex >= 0)
        {
            if (_dataSource is BindingSource bs)
            {
                bs.Position = _selectedRowIndex;
            }
            else
            {
                var form = FindForm();
                if (form?.BindingContext != null)
                {
                    try
                    {
                        var mgr = form.BindingContext[_dataSource] as CurrencyManager;
                        if (mgr != null)
                            mgr.Position = _selectedRowIndex;
                    }
                    catch
                    {
                        // Ignore binding context errors
                    }
                }
            }
        }
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Raises the CellClick event.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected virtual void OnCellClick(DataGridViewCellEventArgs e)
    {
        CellClick?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the CellValueChanged event.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected virtual void OnCellValueChanged(DataGridViewCellEventArgs e)
    {
        CellValueChanged?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the ColumnHeaderMouseClick event.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected virtual void OnColumnHeaderMouseClick(DataGridViewCellEventArgs e)
    {
        ColumnHeaderMouseClick?.Invoke(this, e);
    }

    /// <summary>
    /// Renders the DataGridView with all its elements.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
    public override void Render(Graphics g)
    {
        if (!Visible) return;

        var theme = ThemeManager.CurrentTheme;
        float zoom = EffectiveZoom;
        int headerHeight = _columnHeadersVisible ? _rowHeight : 0;
        int rowHeaderWidth = _rowHeadersVisible ? 40 : 0;
        int totalContentHeight = _rows.Count * _rowHeight + (_allowUserToAddRows ? _rowHeight : 0);
        int dataHeight = Height - headerHeight;
        bool needVScroll = totalContentHeight > dataHeight;
        int scrollBarWidth = needVScroll ? 16 : 0;
        int dataWidth = Width - rowHeaderWidth - scrollBarWidth;

        _vScrollBar.ViewSize = dataHeight;
        _vScrollBar.ContentSize = totalContentHeight;

        int rowStart = Math.Max(0, _vScrollBar.Value / _rowHeight);
        int rowEnd = Math.Min(_rows.Count, rowStart + (dataHeight / _rowHeight) + 2);

        g.FillRectangle(BackColor, 0, 0, Width, Height);
        g.DrawRectangle(theme.DataGridViewBorder, 0, 0, Width, Height, 1);

        if (_columnHeadersVisible)
        {
            g.FillRectangle(theme.ControlBackground, 0, 0, Width - scrollBarWidth, headerHeight);
            g.DrawLine(theme.DataGridViewHeaderSeparator, 0, headerHeight, Width - scrollBarWidth, headerHeight);

            g.SetClip(new Rectangle(rowHeaderWidth, 0, dataWidth, headerHeight));
            int x = rowHeaderWidth - _horizontalScrollOffset;
            for (int col = 0; col < _columns.Count; col++)
            {
                var colWidth = _columns[col].Width;
                if (x + colWidth > rowHeaderWidth && x < rowHeaderWidth + dataWidth)
                {
                    int drawX = Math.Max(x, rowHeaderWidth);
                    int drawWidth = Math.Min(x + colWidth, rowHeaderWidth + dataWidth) - drawX;
                    if (drawWidth > 0)
                    {
                        g.DrawRectangle(theme.DataGridViewBorder, drawX, 0, drawWidth, headerHeight, 1);
                        g.SetClip(new Rectangle(drawX, 0, drawWidth, headerHeight));
                        var font = _columns[col].HeaderCell?.Font ?? EffectiveFont;
                        var headerText = _columns[col].HeaderText;
                        float arrowSize = 0.3f * headerHeight;
                        int sortGlyphWidth = _columns[col].SortOrder != SortOrder.None ? (int)(arrowSize * 2) + 4 : 0;
                        headerText = TruncateText(headerText, font, zoom, _columns[col].TextAlign, drawWidth - 8 - sortGlyphWidth);
                        float headerTextX = GetAlignedX(headerText, font, zoom, _columns[col].TextAlign, drawX, drawWidth - sortGlyphWidth, 4);
                        g.DrawString(headerText, font, theme.DataGridViewHeaderText, headerTextX, CoordinateTransform.CenterVertically(0, headerHeight, font, zoom));

                        if (_columns[col].SortOrder != SortOrder.None)
                        {
                            float arrowX = drawX + drawWidth - sortGlyphWidth + 2;
                            float midY = headerHeight / 2f;
                            if (_columns[col].SortOrder == SortOrder.Ascending)
                            {
                                g.FillTriangle(theme.DataGridViewSortArrow,
                                    arrowX, midY + arrowSize,
                                    arrowX + arrowSize, midY + arrowSize,
                                    arrowX + arrowSize / 2f, midY - arrowSize);
                            }
                            else
                            {
                                g.FillTriangle(theme.DataGridViewSortArrow,
                                    arrowX, midY - arrowSize,
                                    arrowX + arrowSize, midY - arrowSize,
                                    arrowX + arrowSize / 2f, midY + arrowSize);
                            }
                        }
                        g.ResetClip();
                    }
                }
                x += colWidth;
            }
            g.ResetClip();
        }

        if (_rowHeadersVisible)
        {
            g.SetClip(new Rectangle(0, headerHeight, rowHeaderWidth, dataHeight));
            g.FillRectangle(theme.ControlBackground, 0, headerHeight, rowHeaderWidth, dataHeight);

            for (int rowIdx = rowStart; rowIdx < rowEnd; rowIdx++)
            {
                int y = headerHeight + (rowIdx * _rowHeight) - _vScrollBar.Value;
                bool isSelected = rowIdx == _selectedRowIndex;
                var headerBg = isSelected ? theme.Highlight : theme.ControlBackground;

                g.FillRectangle(headerBg, 0, y, rowHeaderWidth, _rowHeight);
                g.DrawRectangle(theme.DataGridViewBorder, 0, y, rowHeaderWidth, _rowHeight, 1);
                var font = EffectiveFont;
                g.DrawString((rowIdx + 1).ToString(), font, isSelected ? theme.HighlightText : theme.DataGridViewRowHeaderText, 4, y + (int)CoordinateTransform.CenterVertically(0, _rowHeight, font, zoom));
            }

            if (_allowUserToAddRows)
            {
                int y = headerHeight + (_rows.Count * _rowHeight) - _vScrollBar.Value;
                g.FillRectangle(theme.ControlBackground, 0, y, rowHeaderWidth, _rowHeight);
                g.DrawRectangle(theme.DataGridViewBorder, 0, y, rowHeaderWidth, _rowHeight, 1);
            }

            g.ResetClip();
        }

        g.SetClip(new Rectangle(rowHeaderWidth, headerHeight, dataWidth, dataHeight));

        for (int rowIdx = rowStart; rowIdx < rowEnd; rowIdx++)
        {
            int y = headerHeight + (rowIdx * _rowHeight) - _vScrollBar.Value;

            bool isSelected = rowIdx == _selectedRowIndex;
            bool isAlternate = rowIdx % 2 == 1;

            if (isSelected)
                g.FillRectangle(theme.Highlight, rowHeaderWidth, y, dataWidth, _rowHeight);
            else if (isAlternate)
                g.FillRectangle(theme.AlternateRow, rowHeaderWidth, y, dataWidth, _rowHeight);
        }

        if (_showGridLines)
        {
            for (int rowIdx = rowStart; rowIdx < rowEnd; rowIdx++)
            {
                int y = headerHeight + (rowIdx * _rowHeight) - _vScrollBar.Value;

                g.DrawLine(theme.GridLine, rowHeaderWidth, y + _rowHeight, rowHeaderWidth + dataWidth, y + _rowHeight);

                int x = rowHeaderWidth - _horizontalScrollOffset;
                for (int col = 0; col < _columns.Count; col++)
                {
                    var colWidth = _columns[col].Width;
                    if (x + colWidth > rowHeaderWidth && x < rowHeaderWidth + dataWidth)
                    {
                        int drawX = Math.Max(x, rowHeaderWidth);
                        g.DrawLine(theme.GridLineVertical, drawX, y, drawX, y + _rowHeight);
                    }
                    x += colWidth;
                }
            }
        }

        for (int rowIdx = rowStart; rowIdx < rowEnd; rowIdx++)
        {
            int y = headerHeight + (rowIdx * _rowHeight) - _vScrollBar.Value;
            bool isSelected = rowIdx == _selectedRowIndex;
            var textColor = isSelected ? theme.HighlightText : theme.DataGridViewCellText;

            int x = rowHeaderWidth - _horizontalScrollOffset;
            for (int col = 0; col < _columns.Count; col++)
            {
                var colWidth = _columns[col].Width;
                if (x + colWidth > rowHeaderWidth && x < rowHeaderWidth + dataWidth)
                {
                    int drawX = Math.Max(x, rowHeaderWidth);
                    int drawWidth = Math.Min(x + colWidth, rowHeaderWidth + dataWidth) - drawX;
                    if (drawWidth > 0)
                    {
                        var cell = _rows[rowIdx].Cells.Count > col ? _rows[rowIdx].Cells[col] : null;
                        var colDef = _columns[col];
                        var text = FormatCellValue(cell?.Value, colDef.FormatString);
                        var font = EffectiveFont;
                        int available = drawWidth - 8;
                        text = TruncateText(text, font, zoom, colDef.TextAlign, available > 0 ? available : 0);
                        float textX = GetAlignedX(text, font, zoom, colDef.TextAlign, drawX, drawWidth, 4);
                        g.DrawString(text, font, textColor, textX, y + (int)CoordinateTransform.CenterVertically(0, _rowHeight, font, zoom));
                    }
                }
                x += colWidth;
            }
        }

        if (_allowUserToAddRows)
        {
            int y = headerHeight + (_rows.Count * _rowHeight) - _vScrollBar.Value;
            g.FillRectangle(theme.DataGridViewAddNewRowBackground, rowHeaderWidth, y, dataWidth, _rowHeight);
            g.DrawLine(theme.DataGridViewAddNewRowSeparator, rowHeaderWidth, y, rowHeaderWidth + dataWidth, y);
            var addRowFont = EffectiveFont;
            float scaledAddRowSize = 12 * zoom;
            g.DrawString("*", addRowFont, theme.DataGridViewAddNewRowAsterisk, rowHeaderWidth + 4, y + (_rowHeight - scaledAddRowSize) / 2);
        }

        g.ResetClip();

        if (needVScroll)
        {
            var scrollBarBounds = new Rectangle(Width - scrollBarWidth, headerHeight, scrollBarWidth, dataHeight);
            _vScrollBar.Render(g, scrollBarBounds, theme);
        }

        if (Focused)
            g.DrawRectangle(theme.TextBoxFocusBorder, 0, 0, Width, Height, 2);

        base.Render(g);
    }

    /// <summary>
    /// Raises the MouseDown event and selects a cell or starts column resize.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseDown(EventArgs e)
    {
        var mouseArgs = e as MouseEventArgs;
        if (mouseArgs != null)
        {
            int headerHeight = _columnHeadersVisible ? _rowHeight : 0;
            int rowHeaderWidth = _rowHeadersVisible ? 40 : 0;
            int totalContentHeight = _rows.Count * _rowHeight + (_allowUserToAddRows ? _rowHeight : 0);
            int dataHeight = Height - headerHeight;
            bool needVScroll = totalContentHeight > dataHeight;
            int scrollBarWidth = needVScroll ? ScrollBarEngine.DefaultScrollBarSize : 0;

            // Check if click is in scrollbar area
            if (needVScroll && mouseArgs.X >= Width - scrollBarWidth)
            {
                var scrollBarBounds = new Rectangle(Width - scrollBarWidth, headerHeight, scrollBarWidth, dataHeight);
                _vScrollBar.HandleMouseDown(new Point(mouseArgs.X, mouseArgs.Y), scrollBarBounds, VScrollBarContext);
                return;
            }

            // Check if clicking on a column divider to start resize
            int dividerCol = GetDividerColumnIndex(mouseArgs.X);
            if (dividerCol >= 0 && _columns[dividerCol].Resizable)
            {
                _resizingColumnIndex = dividerCol;
                _resizeStartMouseX = mouseArgs.X;
                _resizeStartWidth = _columns[dividerCol].Width;
                var form = FindForm();
                if (form != null)
                    form.CaptureControl = this;
                return;
            }

            int col = GetColumnIndexAtX(mouseArgs.X);
            int row = (mouseArgs.Y - headerHeight + _vScrollBar.Value) / _rowHeight;

            // Click on column header -> sort
            if (mouseArgs.Y < headerHeight && col >= 0 && col < _columns.Count)
            {
                OnColumnHeaderMouseClick(new DataGridViewCellEventArgs(col, -1));
                if (_columns[col].Sortable)
                {
                    if (col == _sortColumnIndex)
                    {
                        _sortAscending = !_sortAscending;
                    }
                    else
                    {
                        ClearSortColumns();
                        _sortColumnIndex = col;
                        _sortAscending = true;
                    }
                    _columns[col].SortOrder = _sortAscending ? SortOrder.Ascending : SortOrder.Descending;
                    SortRows();
                    Invalidate();
                }
                return;
            }

            if (row >= 0 && row < _rows.Count && col >= 0 && col < _columns.Count)
            {
                _selectedRowIndex = row;
                _selectedColumnIndex = col;
                OnCellClick(new DataGridViewCellEventArgs(col, row));
                OnSelectionChanged();
                Invalidate();
            }
            else if (row >= _rows.Count && _allowUserToAddRows)
            {
                AddRow();
            }
        }

        base.OnMouseDown(e);
    }

    /// <summary>
    /// Raises the KeyDown event to handle navigation.
    /// </summary>
    /// <param name="e">A KeyEventArgs that contains the event data.</param>
    protected internal override void OnKeyDown(KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Up:
                if (_selectedRowIndex > 0)
                {
                    _selectedRowIndex--;
                    EnsureRowVisible(_selectedRowIndex);
                    OnSelectionChanged();
                    Invalidate();
                    e.Handled = true;
                }
                break;
            case Keys.Down:
                if (_selectedRowIndex < _rows.Count - 1)
                {
                    _selectedRowIndex++;
                    EnsureRowVisible(_selectedRowIndex);
                    OnSelectionChanged();
                    Invalidate();
                    e.Handled = true;
                }
                break;
            case Keys.Left:
                if (_selectedColumnIndex > 0)
                {
                    _selectedColumnIndex--;
                    OnSelectionChanged();
                    Invalidate();
                    e.Handled = true;
                }
                break;
            case Keys.Right:
                if (_selectedColumnIndex < _columns.Count - 1)
                {
                    _selectedColumnIndex++;
                    OnSelectionChanged();
                    Invalidate();
                    e.Handled = true;
                }
                break;
            case Keys.Home:
                if (_rows.Count > 0)
                {
                    _selectedRowIndex = 0;
                    _selectedColumnIndex = 0;
                    _vScrollBar.Value = 0;
                    _horizontalScrollOffset = 0;
                    OnSelectionChanged();
                    Invalidate();
                    e.Handled = true;
                }
                break;
            case Keys.End:
                if (_rows.Count > 0)
                {
                    _selectedRowIndex = _rows.Count - 1;
                    _selectedColumnIndex = _columns.Count > 0 ? _columns.Count - 1 : 0;
                    EnsureRowVisible(_selectedRowIndex);
                    OnSelectionChanged();
                    Invalidate();
                    e.Handled = true;
                }
                break;
            case Keys.PageUp:
                if (_rows.Count > 0 && _selectedRowIndex > 0)
                {
                    int headerHeight = _columnHeadersVisible ? _rowHeight : 0;
                    int visibleRows = (Height - headerHeight) / _rowHeight;
                    _selectedRowIndex = Math.Max(0, _selectedRowIndex - visibleRows);
                    EnsureRowVisible(_selectedRowIndex);
                    OnSelectionChanged();
                    Invalidate();
                    e.Handled = true;
                }
                break;
            case Keys.PageDown:
                if (_rows.Count > 0 && _selectedRowIndex < _rows.Count - 1)
                {
                    int headerHeight = _columnHeadersVisible ? _rowHeight : 0;
                    int visibleRows = (Height - headerHeight) / _rowHeight;
                    _selectedRowIndex = Math.Min(_rows.Count - 1, _selectedRowIndex + visibleRows);
                    EnsureRowVisible(_selectedRowIndex);
                    OnSelectionChanged();
                    Invalidate();
                    e.Handled = true;
                }
                break;
        }

        if (e.Modifiers.HasFlag(ModifierKeys.Control) && e.KeyCode == Keys.C)
        {
            Copy();
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    private void EnsureRowVisible(int rowIndex)
    {
        int headerHeight = _columnHeadersVisible ? _rowHeight : 0;
        int dataHeight = Height - headerHeight;
        int rowTop = rowIndex * _rowHeight;
        int rowBottom = rowTop + _rowHeight;

        if (rowTop < _vScrollBar.Value)
            _vScrollBar.Value = rowTop;
        else if (rowBottom > _vScrollBar.Value + dataHeight)
            _vScrollBar.Value = rowBottom - dataHeight;
    }

    /// <summary>
    /// Raises the MouseWheel event to handle vertical scrolling.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseWheel(EventArgs e)
    {
        if (_vScrollBar.IsDragging) return;

        var mouseArgs = e as MouseEventArgs;
        if (mouseArgs != null)
        {
            int headerHeight = _columnHeadersVisible ? _rowHeight : 0;
            int totalContentHeight = _rows.Count * _rowHeight + (_allowUserToAddRows ? _rowHeight : 0);
            int dataHeight = Height - headerHeight;
            _vScrollBar.ViewSize = dataHeight;
            _vScrollBar.ContentSize = totalContentHeight;

            if (_vScrollBar.NeedsScrollbar)
            {
                _vScrollBar.HandleMouseWheel(mouseArgs.Delta, VScrollBarContext);
            }
        }
        base.OnMouseWheel(e);
    }

    /// <summary>
    /// Raises the MouseUp event to handle scrollbar interaction and column resize end.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseUp(EventArgs e)
    {
        if (_resizingColumnIndex >= 0)
        {
            _resizingColumnIndex = -1;
            var form = FindForm();
            if (form != null)
                form.CaptureControl = null;
            return;
        }
        if (_vScrollBar.IsDragging || _vScrollBar.IsUpButtonPressed || _vScrollBar.IsDownButtonPressed)
        {
            _vScrollBar.HandleMouseUp(VScrollBarContext);
        }
        base.OnMouseUp(e);
    }

    /// <summary>
    /// Raises the MouseMove event to handle scrollbar hover/drag and column resize.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseMove(EventArgs e)
    {
        var mouseArgs = e as MouseEventArgs;
        if (mouseArgs == null)
        {
            base.OnMouseMove(e);
            return;
        }

        // Handle column resize
        if (_resizingColumnIndex >= 0)
        {
            int delta = mouseArgs.X - _resizeStartMouseX;
            int newWidth = Math.Max(MinColumnWidth, _resizeStartWidth + delta);
            _columns[_resizingColumnIndex].Width = newWidth;
            Invalidate();
            return;
        }

        // Handle scrollbar drag
        if (_vScrollBar.IsDragging)
        {
            int headerHeight = _columnHeadersVisible ? _rowHeight : 0;
            int totalContentHeight = _rows.Count * _rowHeight + (_allowUserToAddRows ? _rowHeight : 0);
            int dataHeight = Height - headerHeight;
            bool needVScroll = totalContentHeight > dataHeight;
            int scrollBarWidth = needVScroll ? ScrollBarEngine.DefaultScrollBarSize : 0;
            var scrollBarBounds = new Rectangle(Width - scrollBarWidth, headerHeight, scrollBarWidth, dataHeight);
            _vScrollBar.HandleMouseMove(new Point(mouseArgs.X, mouseArgs.Y), scrollBarBounds, VScrollBarContext);
            return;
        }

        // Update cursor when hovering over a column divider
        var form = FindForm();
        int dividerCol = GetDividerColumnIndex(mouseArgs.X);
        if (dividerCol >= 0 && _columns[dividerCol].Resizable)
        {
            if (form != null)
                form.Cursor = SystemCursorType.SizeWE;
        }
        else
        {
            if (form != null && form.Cursor == SystemCursorType.SizeWE)
                form.Cursor = null;
        }

        base.OnMouseMove(e);
    }

    /// <summary>
    /// Called when the DataSource property changes. Populates rows from the data source.
    /// </summary>
    protected virtual void OnDataSourceChanged()
    {
        if (_dataSourceUpdating) return;
        _dataSourceUpdating = true;
        try
        {
            _rows.Clear();
            ClearSort();
            _selectedRowIndex = -1;
            _selectedColumnIndex = -1;

            if (_dataSource is System.Collections.IEnumerable enumerable && _dataSource is not string)
            {
                foreach (var item in enumerable)
                {
                    var row = CreateRowFromDataItem(item);
                    _rows.Add(row);
                }
            }

            if (_dataSource is IBindingList bindingList)
            {
                bindingList.ListChanged += OnDataSourceListChanged;
            }

            Invalidate();
        }
        finally
        {
            _dataSourceUpdating = false;
        }
    }

    /// <summary>
    /// Creates a DataGridViewRow from a data source item, populating cells via DataPropertyName.
    /// </summary>
    /// <param name="item">The data source item.</param>
    /// <returns>The populated row.</returns>
    protected DataGridViewRow CreateRowFromDataItem(object? item)
    {
        var row = new DataGridViewRow { DataBoundItem = item };
        for (int col = 0; col < _columns.Count; col++)
        {
            var colDef = _columns[col];
            object? value = null;

            if (item != null && !string.IsNullOrEmpty(colDef.DataPropertyName))
            {
                var prop = item.GetType().GetProperty(colDef.DataPropertyName);
                if (prop != null)
                {
                    value = prop.GetValue(item);
                }
            }

            row.Cells.Add(CreateCell(value));
        }
        return row;
    }

    /// <summary>
    /// Handles ListChanged events from the data source to keep rows in sync.
    /// </summary>
    protected virtual void OnDataSourceListChanged(object? sender, ListChangedEventArgs e)
    {
        if (_dataSource is not IBindingList bindingList)
            return;

        switch (e.ListChangedType)
        {
            case ListChangedType.ItemAdded:
                if (e.NewIndex >= 0 && e.NewIndex <= _rows.Count)
                {
                    var item = bindingList[e.NewIndex];
                    var newRow = CreateRowFromDataItem(item);
                    if (e.NewIndex < _rows.Count)
                    {
                        var tempRows = new List<DataGridViewRow>();
                        while (_rows.Count > e.NewIndex)
                        {
                            tempRows.Add(_rows[_rows.Count - 1]);
                            _rows.Remove(_rows[_rows.Count - 1]);
                        }
                        _rows.Add(newRow);
                        while (tempRows.Count > 0)
                        {
                            var idx = _rows.Count;
                            var r = tempRows[tempRows.Count - 1];
                            tempRows.RemoveAt(tempRows.Count - 1);
                            // re-wrap existing item
                            _rows.Add(r);
                        }
                    }
                    else
                    {
                        _rows.Add(newRow);
                    }
                    if (_sortColumnIndex >= 0)
                        SortRows();
                    Invalidate();
                }
                break;

            case ListChangedType.ItemDeleted:
                if (e.NewIndex >= 0 && e.NewIndex < _rows.Count)
                {
                    // remove actual row data
                    var tempRows = new List<DataGridViewRow>();
                    for (int i = 0; i < _rows.Count; i++)
                    {
                        if (i != e.NewIndex)
                            tempRows.Add(_rows[i]);
                    }
                    _rows.Clear();
                    foreach (var r in tempRows)
                        _rows.Add(r);
                    if (_selectedRowIndex >= _rows.Count)
                        _selectedRowIndex = Math.Max(0, _rows.Count - 1);
                    if (_sortColumnIndex >= 0)
                        SortRows();
                    Invalidate();
                }
                break;

            case ListChangedType.ItemChanged:
                if (e.NewIndex >= 0 && e.NewIndex < _rows.Count && e.NewIndex < bindingList.Count)
                {
                    var item = bindingList[e.NewIndex];
                    _rows[e.NewIndex].DataBoundItem = item;
                    if (!string.IsNullOrEmpty(_columns[0].DataPropertyName))
                    {
                        var updatedRow = CreateRowFromDataItem(item);
                        _rows[e.NewIndex].Cells.Clear();
                        foreach (var cell in updatedRow.Cells)
                            _rows[e.NewIndex].Cells.Add(cell);
                    }
                    if (_sortColumnIndex >= 0)
                        SortRows();
                    Invalidate();
                }
                break;

            case ListChangedType.Reset:
                _rows.Clear();
                foreach (var item in bindingList)
                {
                    _rows.Add(CreateRowFromDataItem(item));
                }
                if (_sortColumnIndex >= 0)
                    SortRows();
                _selectedRowIndex = _rows.Count > 0 ? 0 : -1;
                Invalidate();
                break;
        }
    }

    internal void NotifyCellValueChanged(int columnIndex, int rowIndex, object? newValue)
    {
        if (_dataSourceUpdating) return;

        if (_dataSource is IBindingList && rowIndex >= 0 && rowIndex < _rows.Count)
        {
            var row = _rows[rowIndex];
            var dataItem = row.DataBoundItem;
            if (dataItem != null && columnIndex >= 0 && columnIndex < _columns.Count)
            {
                var colDef = _columns[columnIndex];
                if (!string.IsNullOrEmpty(colDef.DataPropertyName))
                {
                    var prop = dataItem.GetType().GetProperty(colDef.DataPropertyName);
                    if (prop != null && prop.CanWrite)
                    {
                        prop.SetValue(dataItem, newValue);
                    }
                }
            }
        }
    }

    private DataGridViewCell CreateCell(object? value)
    {
        var cell = new DataGridViewCell { Value = value };
        cell.OnValueChanged = Invalidate;
        return cell;
    }

    /// <summary>
    /// Adds a new row with the specified values.
    /// </summary>
    /// <param name="values">The values for the new row.</param>
    public void AddRow(params object[] values)
    {
        var row = new DataGridViewRow();
        for (int i = 0; i < _columns.Count; i++)
        {
            var value = i < values.Length ? values[i] : "";
            row.Cells.Add(CreateCell(value));
        }
        _rows.Add(row);
        Invalidate();
    }

    /// <summary>
    /// Clears all rows from the grid.
    /// </summary>
    public void Clear()
    {
        _rows.Clear();
        ClearSort();
        _selectedRowIndex = -1;
        _selectedColumnIndex = -1;
        Invalidate();
    }

    /// <summary>
    /// Clears the current sort, restoring the original row order.
    /// </summary>
    public void ClearSort()
    {
        _sortColumnIndex = -1;
        for (int i = 0; i < _columns.Count; i++)
            _columns[i].SortOrder = SortOrder.None;
    }

    private void ClearSortColumns()
    {
        for (int i = 0; i < _columns.Count; i++)
            _columns[i].SortOrder = SortOrder.None;
    }

    /// <summary>
    /// Sorts the rows by the current sort column using raw cell values.
    /// </summary>
    private void SortRows()
    {
        if (_sortColumnIndex < 0 || _sortColumnIndex >= _columns.Count)
            return;

        int colIdx = _sortColumnIndex;
        bool ascending = _sortAscending;

        _rows.Sort((a, b) =>
        {
            var valA = colIdx < a.Cells.Count ? a.Cells[colIdx]?.Value : null;
            var valB = colIdx < b.Cells.Count ? b.Cells[colIdx]?.Value : null;
            int result = CompareCellValues(valA, valB);
            return ascending ? result : -result;
        });
    }

    private static int CompareCellValues(object? a, object? b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return -1;
        if (b == null) return 1;

        if (a is IComparable comparableA && b is IComparable comparableB)
        {
            try { return comparableA.CompareTo(comparableB); }
            catch { }
        }

        return string.Compare(a.ToString(), b.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the column index at the given x-coordinate (in form-relative coordinates).
    /// </summary>
    private int GetColumnIndexAtX(int x)
    {
        int rowHeaderWidth = _rowHeadersVisible ? 40 : 0;
        int cx = rowHeaderWidth - _horizontalScrollOffset;
        for (int i = 0; i < _columns.Count; i++)
        {
            int colWidth = _columns[i].Width;
            if (x >= cx && x < cx + colWidth)
                return i;
            cx += colWidth;
        }
        return -1;
    }

    /// <summary>
    /// Gets the index of the column whose right edge divider the x-coordinate is near,
    /// or -1 if not near any divider.
    /// </summary>
    private int GetDividerColumnIndex(int x)
    {
        int rowHeaderWidth = _rowHeadersVisible ? 40 : 0;
        int cx = rowHeaderWidth - _horizontalScrollOffset;
        for (int i = 0; i < _columns.Count; i++)
        {
            cx += _columns[i].Width;
            if (Math.Abs(x - cx) <= DividerThreshold)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Truncates text to fit within the specified logical width, adding ellipsis ("...") when truncated.
    /// For right-aligned text the right portion is kept; for left/center the left portion is kept.
    /// </summary>
    private static string TruncateText(string text, Font font, float zoom, DataGridViewContentAlignment alignment, float maxWidthLogical)
    {
        if (string.IsNullOrEmpty(text) || zoom <= 0f) return text;

        int maxWidthPixels = (int)(maxWidthLogical * zoom);
        int textWidth = CoordinateTransform.MeasureText(text, font, zoom).width;
        if (textWidth <= maxWidthPixels) return text;

        const string ellipsis = "...";
        int ellipsisWidth = CoordinateTransform.MeasureText(ellipsis, font, zoom).width;
        int availablePixels = maxWidthPixels - ellipsisWidth;

        if (availablePixels <= 0) return ellipsis;

        if (alignment == DataGridViewContentAlignment.Right)
        {
            for (int i = text.Length - 1; i >= 0; i--)
            {
                var sub = text.Substring(i);
                if (CoordinateTransform.MeasureText(sub, font, zoom).width <= availablePixels)
                    return ellipsis + sub;
            }
            return ellipsis;
        }
        else
        {
            for (int i = 1; i <= text.Length; i++)
            {
                var sub = text.Substring(0, i);
                if (CoordinateTransform.MeasureText(sub, font, zoom).width > availablePixels)
                    return text.Substring(0, i - 1) + ellipsis;
            }
            return text + ellipsis;
        }
    }

    /// <summary>
    /// Formats a cell value using the specified format string.
    /// </summary>
    /// <param name="value">The cell value.</param>
    /// <param name="formatString">The format string (e.g. "N2", "d"), or null to use ToString().</param>
    /// <returns>The formatted text.</returns>
    private static string FormatCellValue(object? value, string? formatString)
    {
        if (value == null) return "";
        if (formatString != null)
        {
            try { return string.Format($"{{0:{formatString}}}", value); }
            catch { }
        }
        return value.ToString() ?? "";
    }

    /// <summary>
    /// Calculates the X position for text based on alignment within a cell.
    /// All parameters are in logical coordinates; textWidth from MeasureText is in pixels
    /// and is divided by zoom to convert to logical.
    /// </summary>
    private static float GetAlignedX(string text, Font font, float zoom, DataGridViewContentAlignment alignment, float cellX, float cellWidth, int padding)
    {
        if (alignment == DataGridViewContentAlignment.Left || string.IsNullOrEmpty(text))
            return cellX + padding;

        float textWidthLogical = CoordinateTransform.MeasureText(text, font, zoom).width / Math.Max(zoom, 0.001f);

        return alignment switch
        {
            DataGridViewContentAlignment.Center => cellX + (cellWidth - textWidthLogical) / 2f,
            DataGridViewContentAlignment.Right => cellX + cellWidth - textWidthLogical - padding,
            _ => cellX + padding
        };
    }
}

    /// <summary>
    /// Provides scrollbar context for the DataGridView.
    /// </summary>
internal sealed class ScrollBarContext : IScrollBarContext
{
    private readonly DataGridView _owner;

    /// <summary>
    /// Initializes a new instance of ScrollBarContext.
    /// </summary>
    /// <param name="owner">The owning DataGridView.</param>
    public ScrollBarContext(DataGridView owner) => _owner = owner;

    /// <inheritdoc/>
    public float Zoom => _owner.EffectiveZoom;

    /// <inheritdoc/>
    public void Invalidate() => _owner.Invalidate();

    /// <inheritdoc/>
    public void CaptureMouse(bool capture) => _owner.CapturingMouse = capture;
}

/// <summary>
/// Specifies the selection mode of a DataGridView.
/// </summary>
public enum DataGridViewSelectionMode
{
    /// <summary>
    /// Selection by clicking the row header.
    /// </summary>
    RowHeaderSelect,

    /// <summary>
    /// Selection by clicking the column header.
    /// </summary>
    ColumnHeaderSelect,

    /// <summary>
    /// Full row selection.
    /// </summary>
    FullRowSelect,

    /// <summary>
    /// Full column selection.
    /// </summary>
    FullColumnSelect,

    /// <summary>
    /// Cell selection only.
    /// </summary>
    CellSelect
}