using CoreForms.Ui.Core;
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
    private bool _allowUserToAddRows = true;
    private bool _allowUserToDeleteRows = true;
    private bool _readOnly;
    private bool _multiSelect;
    private bool _columnHeadersVisible = true;
    private bool _rowHeadersVisible = true;
    private bool _showGridLines = true;
    private DataGridViewSelectionMode _selectionMode = DataGridViewSelectionMode.RowHeaderSelect;
    private int _verticalScrollOffset;
    private int _horizontalScrollOffset;

    /// <summary>
    /// Initializes a new instance of DataGridView.
    /// </summary>
    public DataGridView()
    {
        BackColor = Color.White;
        Size = new Size(400, 200);
        TabStop = true;
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
    /// Raises the SelectionChanged event.
    /// </summary>
    protected virtual void OnSelectionChanged()
    {
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

        int headerHeight = _columnHeadersVisible ? _rowHeight : 0;
        int rowHeaderWidth = _rowHeadersVisible ? 40 : 0;
        int totalContentHeight = _rows.Count * _rowHeight + (_allowUserToAddRows ? _rowHeight : 0);
        int dataHeight = Height - headerHeight;
        bool needVScroll = totalContentHeight > dataHeight;
        int scrollBarWidth = needVScroll ? 16 : 0;
        int dataWidth = Width - rowHeaderWidth - scrollBarWidth;

        int maxScroll = Math.Max(0, totalContentHeight - dataHeight);
        if (_verticalScrollOffset > maxScroll) _verticalScrollOffset = maxScroll;
        if (_verticalScrollOffset < 0) _verticalScrollOffset = 0;

        int rowStart = Math.Max(0, _verticalScrollOffset / _rowHeight);
        int rowEnd = Math.Min(_rows.Count, rowStart + (dataHeight / _rowHeight) + 2);

        g.FillRectangle(BackColor, 0, 0, Width, Height);
        g.DrawRectangle(Color.FromArgb(180, 180, 180), 0, 0, Width, Height, 1);

        if (_columnHeadersVisible)
        {
            g.FillRectangle(SystemColors.Control, 0, 0, Width - scrollBarWidth, headerHeight);
            g.DrawLine(Color.FromArgb(150, 150, 150), 0, headerHeight, Width - scrollBarWidth, headerHeight);

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
                        g.DrawRectangle(Color.FromArgb(180, 180, 180), drawX, 0, drawWidth, headerHeight, 1);
                        var font = _columns[col].HeaderCell?.Font ?? Font.Default;
                        g.DrawString(_columns[col].HeaderText, font, Color.Black, drawX + 4, (headerHeight - (int)font.Size) / 2);
                    }
                }
                x += colWidth;
            }
            g.ResetClip();
        }

        if (_rowHeadersVisible)
        {
            g.SetClip(new Rectangle(0, headerHeight, rowHeaderWidth, dataHeight));
            g.FillRectangle(SystemColors.Control, 0, headerHeight, rowHeaderWidth, dataHeight);

            for (int rowIdx = rowStart; rowIdx < rowEnd; rowIdx++)
            {
                int y = headerHeight + (rowIdx * _rowHeight) - _verticalScrollOffset;
                bool isSelected = rowIdx == _selectedRowIndex;
                var headerBg = isSelected ? SystemColors.Highlight : SystemColors.Control;

                g.FillRectangle(headerBg, 0, y, rowHeaderWidth, _rowHeight);
                g.DrawRectangle(Color.FromArgb(180, 180, 180), 0, y, rowHeaderWidth, _rowHeight, 1);
                var font = Font.Default;
                g.DrawString((rowIdx + 1).ToString(), font, isSelected ? SystemColors.HighlightText : Color.Black, 4, y + (_rowHeight - (int)font.Size) / 2);
            }

            if (_allowUserToAddRows)
            {
                int y = headerHeight + (_rows.Count * _rowHeight) - _verticalScrollOffset;
                g.FillRectangle(SystemColors.Control, 0, y, rowHeaderWidth, _rowHeight);
                g.DrawRectangle(Color.FromArgb(180, 180, 180), 0, y, rowHeaderWidth, _rowHeight, 1);
            }

            g.ResetClip();
        }

        g.SetClip(new Rectangle(rowHeaderWidth, headerHeight, dataWidth, dataHeight));

        for (int rowIdx = rowStart; rowIdx < rowEnd; rowIdx++)
        {
            int y = headerHeight + (rowIdx * _rowHeight) - _verticalScrollOffset;

            bool isSelected = rowIdx == _selectedRowIndex;
            bool isAlternate = rowIdx % 2 == 1;

            if (isSelected)
                g.FillRectangle(SystemColors.Highlight, rowHeaderWidth, y, dataWidth, _rowHeight);
            else if (isAlternate)
                g.FillRectangle(Color.FromArgb(245, 245, 245), rowHeaderWidth, y, dataWidth, _rowHeight);
        }

        if (_showGridLines)
        {
            for (int rowIdx = rowStart; rowIdx < rowEnd; rowIdx++)
            {
                int y = headerHeight + (rowIdx * _rowHeight) - _verticalScrollOffset;

                g.DrawLine(Color.FromArgb(180, 180, 180), rowHeaderWidth, y + _rowHeight, rowHeaderWidth + dataWidth, y + _rowHeight);

                int x = rowHeaderWidth - _horizontalScrollOffset;
                for (int col = 0; col < _columns.Count; col++)
                {
                    var colWidth = _columns[col].Width;
                    if (x + colWidth > rowHeaderWidth && x < rowHeaderWidth + dataWidth)
                    {
                        int drawX = Math.Max(x, rowHeaderWidth);
                        g.DrawLine(Color.FromArgb(220, 220, 220), drawX, y, drawX, y + _rowHeight);
                    }
                    x += colWidth;
                }
            }
        }

        for (int rowIdx = rowStart; rowIdx < rowEnd; rowIdx++)
        {
            int y = headerHeight + (rowIdx * _rowHeight) - _verticalScrollOffset;
            bool isSelected = rowIdx == _selectedRowIndex;
            var textColor = isSelected ? SystemColors.HighlightText : Color.Black;

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
                        var font = Font.Default;
                        g.DrawString(text, font, textColor, drawX + 4, y + (_rowHeight - (int)font.Size) / 2);
                    }
                }
                x += colWidth;
            }
        }

        if (_allowUserToAddRows)
        {
            int y = headerHeight + (_rows.Count * _rowHeight) - _verticalScrollOffset;
            g.FillRectangle(Color.FromArgb(250, 250, 250), rowHeaderWidth, y, dataWidth, _rowHeight);
            g.DrawLine(Color.FromArgb(150, 150, 150), rowHeaderWidth, y, rowHeaderWidth + dataWidth, y);
            g.DrawString("*", Font.Default, Color.FromArgb(150, 150, 150), rowHeaderWidth + 4, y + (_rowHeight - 12) / 2);
        }

        g.ResetClip();

        if (needVScroll)
        {
            int scrollBarX = Width - scrollBarWidth;
            int scrollBarY = headerHeight;
            g.FillRectangle(Color.FromArgb(240, 240, 240), scrollBarX, scrollBarY, scrollBarWidth, dataHeight);
            g.DrawRectangle(Color.FromArgb(180, 180, 180), scrollBarX, scrollBarY, scrollBarWidth, dataHeight, 1);

            float thumbHeightRatio = (float)dataHeight / totalContentHeight;
            int thumbHeight = Math.Max(20, (int)(dataHeight * thumbHeightRatio));
            int maxScrollVal = Math.Max(1, totalContentHeight - dataHeight);
            float thumbPosRatio = (float)_verticalScrollOffset / maxScrollVal;
            int thumbY = scrollBarY + (int)(thumbPosRatio * (dataHeight - thumbHeight));

            g.FillRectangle(Color.FromArgb(190, 190, 190), scrollBarX + 2, thumbY, scrollBarWidth - 4, thumbHeight);
            g.DrawRectangle(Color.FromArgb(150, 150, 150), scrollBarX + 2, thumbY, scrollBarWidth - 4, thumbHeight, 1);
        }

        if (Focused)
            g.DrawRectangle(Color.FromArgb(0, 120, 215), 0, 0, Width, Height, 2);

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

            int col = (mouseArgs.X - rowHeaderWidth + _horizontalScrollOffset) / _columnWidth;
            int row = (mouseArgs.Y - headerHeight + _verticalScrollOffset) / _rowHeight;

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
                    _verticalScrollOffset = 0;
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
        base.OnKeyDown(e);
    }

    private void EnsureRowVisible(int rowIndex)
    {
        int headerHeight = _columnHeadersVisible ? _rowHeight : 0;
        int dataHeight = Height - headerHeight;
        int rowTop = rowIndex * _rowHeight;
        int rowBottom = rowTop + _rowHeight;

        if (rowTop < _verticalScrollOffset)
            _verticalScrollOffset = rowTop;
        else if (rowBottom > _verticalScrollOffset + dataHeight)
            _verticalScrollOffset = rowBottom - dataHeight;
    }

    /// <summary>
    /// Raises the MouseWheel event to handle vertical scrolling.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseWheel(EventArgs e)
    {
        var mouseArgs = e as MouseEventArgs;
        if (mouseArgs != null)
        {
            _verticalScrollOffset -= mouseArgs.Delta * _rowHeight;
            int headerHeight = _columnHeadersVisible ? _rowHeight : 0;
            int totalContentHeight = _rows.Count * _rowHeight + (_allowUserToAddRows ? _rowHeight : 0);
            int dataHeight = Height - headerHeight;
            int maxScroll = Math.Max(0, totalContentHeight - dataHeight);
            _verticalScrollOffset = Math.Max(0, Math.Min(_verticalScrollOffset, maxScroll));

            Invalidate();
        }
        base.OnMouseWheel(e);
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
/// Provides a collection for DataGridView columns.
/// </summary>
public class DataGridViewColumnCollection
{
    private readonly List<DataGridViewColumn> _columns = new();

    /// <summary>
    /// Gets the number of columns.
    /// </summary>
    public int Count => _columns.Count;

    /// <summary>
    /// Gets the column at the specified index.
    /// </summary>
    /// <param name="index">The index.</param>
    public DataGridViewColumn this[int index] => _columns[index];

    /// <summary>
    /// Adds a column to the collection.
    /// </summary>
    /// <param name="column">The column to add.</param>
    public void Add(DataGridViewColumn column)
    {
        _columns.Add(column);
    }

    /// <summary>
    /// Removes a column from the collection.
    /// </summary>
    /// <param name="column">The column to remove.</param>
    public void Remove(DataGridViewColumn column)
    {
        _columns.Remove(column);
    }

    /// <summary>
    /// Removes all columns from the collection.
    /// </summary>
    public void Clear()
    {
        _columns.Clear();
    }
}

/// <summary>
/// Provides a collection for DataGridView rows.
/// </summary>
public class DataGridViewRowCollection
{
    private readonly List<DataGridViewRow> _rows = new();

    /// <summary>
    /// Gets the number of rows.
    /// </summary>
    public int Count => _rows.Count;

    /// <summary>
    /// Gets the row at the specified index.
    /// </summary>
    /// <param name="index">The index.</param>
    public DataGridViewRow this[int index] => _rows[index];

    /// <summary>
    /// Adds a row to the collection.
    /// </summary>
    /// <param name="row">The row to add.</param>
    public void Add(DataGridViewRow row)
    {
        _rows.Add(row);
    }

    /// <summary>
    /// Removes a row from the collection.
    /// </summary>
    /// <param name="row">The row to remove.</param>
    public void Remove(DataGridViewRow row)
    {
        _rows.Remove(row);
    }

    /// <summary>
    /// Removes all rows from the collection.
    /// </summary>
    public void Clear()
    {
        _rows.Clear();
    }
}

/// <summary>
/// Represents a column in a DataGridView.
/// </summary>
public class DataGridViewColumn
{
    /// <summary>
    /// Gets or sets the name of the column.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Gets or sets the header text.
    /// </summary>
    public string HeaderText { get; set; } = "";

    /// <summary>
    /// Gets or sets the width of the column.
    /// </summary>
    public int Width { get; set; } = 100;

    /// <summary>
    /// Gets or sets the header cell.
    /// </summary>
    public DataGridViewHeaderCell? HeaderCell { get; set; }

    /// <summary>
    /// Gets or sets whether the column is read-only.
    /// </summary>
    public bool ReadOnly { get; set; }

    /// <summary>
    /// Gets or sets the type of values in the column.
    /// </summary>
    public Type? ValueType { get; set; }
}

/// <summary>
/// Represents a row in a DataGridView.
/// </summary>
public class DataGridViewRow
{
    /// <summary>
    /// Gets the collection of cells in the row.
    /// </summary>
    public List<DataGridViewCell> Cells { get; } = new();
}

/// <summary>
/// Represents a cell in a DataGridView.
/// </summary>
public class DataGridViewCell
{
    /// <summary>
    /// Gets or sets the value of the cell.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Gets or sets the style of the cell.
    /// </summary>
    public string? Style { get; set; }
}

/// <summary>
/// Represents a header cell in a DataGridView column.
/// </summary>
public class DataGridViewHeaderCell
{
    /// <summary>
    /// Gets or sets the text of the header cell.
    /// </summary>
    public string Text { get; set; } = "";

    /// <summary>
    /// Gets or sets the font of the header cell.
    /// </summary>
    public Font? Font { get; set; }
}

/// <summary>
/// Provides data for cell click events.
/// </summary>
public class DataGridViewCellEventArgs : EventArgs
{
    /// <summary>
    /// Gets the column index of the cell.
    /// </summary>
    public int ColumnIndex { get; }

    /// <summary>
    /// Gets the row index of the cell.
    /// </summary>
    public int RowIndex { get; }

    /// <summary>
    /// Initializes a new instance of DataGridViewCellEventArgs.
    /// </summary>
    /// <param name="columnIndex">The column index.</param>
    /// <param name="rowIndex">The row index.</param>
    public DataGridViewCellEventArgs(int columnIndex, int rowIndex)
    {
        ColumnIndex = columnIndex;
        RowIndex = rowIndex;
    }
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