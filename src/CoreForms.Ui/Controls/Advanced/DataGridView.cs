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
    private readonly DataGridViewColumnCollection _columns = new();
    private readonly DataGridViewRowCollection _rows = new();
    private int _selectedRowIndex = -1;
    private int _selectedColumnIndex = -1;
    private int _rowHeight = 30;
    private int _columnWidth = 100;
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
    private readonly ScrollBarEngine _vScrollBar = new();
    private ScrollBarContext? _scrollBarContext;

    private ScrollBarContext VScrollBarContext => _scrollBarContext ??= new ScrollBarContext(this);

    /// <summary>
    /// Initializes a new instance of DataGridView.
    /// </summary>
    public DataGridView()
    {
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
        set => _allowUserToAddRows = value;
    }

    /// <summary>
    /// Gets or sets whether the user can delete rows.
    /// </summary>
    public bool AllowUserToDeleteRows
    {
        get => _allowUserToDeleteRows;
        set => _allowUserToDeleteRows = value;
    }

    /// <summary>
    /// Gets or sets whether the grid is read-only.
    /// </summary>
    public bool ReadOnly
    {
        get => _readOnly;
        set => _readOnly = value;
    }

    /// <summary>
    /// Gets or sets whether multiple rows can be selected.
    /// </summary>
    public bool MultiSelect
    {
        get => _multiSelect;
        set => _multiSelect = value;
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
        set => _selectionMode = value;
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
                        var font = _columns[col].HeaderCell?.Font ?? EffectiveFont;
                        g.DrawString(_columns[col].HeaderText, font, theme.DataGridViewHeaderText, drawX + 4, CoordinateTransform.CenterVertically(0, headerHeight, font, zoom));
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
                var headerBg = isSelected ? SystemColors.Highlight : theme.ControlBackground;

                g.FillRectangle(headerBg, 0, y, rowHeaderWidth, _rowHeight);
                g.DrawRectangle(theme.DataGridViewBorder, 0, y, rowHeaderWidth, _rowHeight, 1);
                var font = EffectiveFont;
                g.DrawString((rowIdx + 1).ToString(), font, isSelected ? SystemColors.HighlightText : theme.DataGridViewRowHeaderText, 4, y + (int)CoordinateTransform.CenterVertically(0, _rowHeight, font, zoom));
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
                g.FillRectangle(SystemColors.Highlight, rowHeaderWidth, y, dataWidth, _rowHeight);
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
            var textColor = isSelected ? SystemColors.HighlightText : theme.DataGridViewCellText;

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
                        var text = cell?.Value?.ToString() ?? "";
                        var font = EffectiveFont;
                        g.DrawString(text, font, textColor, drawX + 4, y + (int)CoordinateTransform.CenterVertically(0, _rowHeight, font, zoom));
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
    /// Raises the MouseDown event and selects a cell.
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

            int col = (mouseArgs.X - rowHeaderWidth + _horizontalScrollOffset) / _columnWidth;
            int row = (mouseArgs.Y - headerHeight + _vScrollBar.Value) / _rowHeight;

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
    /// Raises the MouseUp event to handle scrollbar interaction.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseUp(EventArgs e)
    {
        if (_vScrollBar.IsDragging || _vScrollBar.IsUpButtonPressed || _vScrollBar.IsDownButtonPressed)
        {
            _vScrollBar.HandleMouseUp(VScrollBarContext);
        }
        base.OnMouseUp(e);
    }

    /// <summary>
    /// Raises the MouseMove event to handle scrollbar hover and drag.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseMove(EventArgs e)
    {
        if (_vScrollBar.IsDragging)
        {
            var mouseArgs = e as MouseEventArgs;
            if (mouseArgs != null)
            {
                int headerHeight = _columnHeadersVisible ? _rowHeight : 0;
                int totalContentHeight = _rows.Count * _rowHeight + (_allowUserToAddRows ? _rowHeight : 0);
                int dataHeight = Height - headerHeight;
                bool needVScroll = totalContentHeight > dataHeight;
                int scrollBarWidth = needVScroll ? ScrollBarEngine.DefaultScrollBarSize : 0;
                var scrollBarBounds = new Rectangle(Width - scrollBarWidth, headerHeight, scrollBarWidth, dataHeight);
                _vScrollBar.HandleMouseMove(new Point(mouseArgs.X, mouseArgs.Y), scrollBarBounds, VScrollBarContext);
            }
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

            row.Cells.Add(new DataGridViewCell { Value = value });
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
                    Invalidate();
                }
                break;

            case ListChangedType.Reset:
                _rows.Clear();
                foreach (var item in bindingList)
                {
                    _rows.Add(CreateRowFromDataItem(item));
                }
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
            row.Cells.Add(new DataGridViewCell { Value = value });
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
        _selectedRowIndex = -1;
        _selectedColumnIndex = -1;
        Invalidate();
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