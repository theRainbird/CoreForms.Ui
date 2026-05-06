using CoreForms.Ui.Core;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Advanced;

public class DataGridView : ContainerControl
{
    private readonly DataGridViewColumnCollection _columns = new();
    private readonly DataGridViewRowCollection _rows = new();
    private int _selectedRowIndex = -1;
    private int _selectedColumnIndex = -1;
    private int _firstVisibleRow;
    private int _firstVisibleColumn;
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

    public DataGridView()
    {
        BackColor = Color.White;
        Size = new Size(400, 200);
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
        set => _allowUserToAddRows = value;
    }

    public bool AllowUserToDeleteRows
    {
        get => _allowUserToDeleteRows;
        set => _allowUserToDeleteRows = value;
    }

    public bool ReadOnly
    {
        get => _readOnly;
        set => _readOnly = value;
    }

    public bool MultiSelect
    {
        get => _multiSelect;
        set => _multiSelect = value;
    }

    public bool ColumnHeadersVisible
    {
        get => _columnHeadersVisible;
        set
        {
            _columnHeadersVisible = value;
            Invalidate();
        }
    }

    public bool RowHeadersVisible
    {
        get => _rowHeadersVisible;
        set
        {
            _rowHeadersVisible = value;
            Invalidate();
        }
    }

    public bool ShowGridLines
    {
        get => _showGridLines;
        set
        {
            _showGridLines = value;
            Invalidate();
        }
    }

    public DataGridViewSelectionMode SelectionMode
    {
        get => _selectionMode;
        set => _selectionMode = value;
    }

    public DataGridViewRow? SelectedRow => _selectedRowIndex >= 0 && _selectedRowIndex < _rows.Count
        ? _rows[_selectedRowIndex]
        : null;

    public object? SelectedValue => SelectedRow != null && _selectedColumnIndex >= 0 && _selectedColumnIndex < _columns.Count
        ? SelectedRow.Cells[_selectedColumnIndex]?.Value
        : null;

    public event EventHandler? SelectionChanged;
    public event EventHandler? CellClick;
    public event EventHandler<DataGridViewCellEventArgs>? CellValueChanged;

    protected virtual void OnSelectionChanged()
    {
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void OnCellClick(DataGridViewCellEventArgs e)
    {
        CellClick?.Invoke(this, e);
    }

    protected virtual void OnCellValueChanged(DataGridViewCellEventArgs e)
    {
        CellValueChanged?.Invoke(this, e);
    }

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

        base.Render(g);
    }

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
                Invalidate();
            }
            else if (row >= _rows.Count && _allowUserToAddRows)
            {
                AddRow();
            }
        }

        base.OnMouseDown(e);
    }

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

    public void Clear()
    {
        _rows.Clear();
        _selectedRowIndex = -1;
        _selectedColumnIndex = -1;
        Invalidate();
    }
}

public class DataGridViewColumnCollection
{
    private readonly List<DataGridViewColumn> _columns = new();

    public int Count => _columns.Count;

    public DataGridViewColumn this[int index] => _columns[index];

    public void Add(DataGridViewColumn column)
    {
        _columns.Add(column);
    }

    public void Remove(DataGridViewColumn column)
    {
        _columns.Remove(column);
    }

    public void Clear()
    {
        _columns.Clear();
    }
}

public class DataGridViewRowCollection
{
    private readonly List<DataGridViewRow> _rows = new();

    public int Count => _rows.Count;

    public DataGridViewRow this[int index] => _rows[index];

    public void Add(DataGridViewRow row)
    {
        _rows.Add(row);
    }

    public void Remove(DataGridViewRow row)
    {
        _rows.Remove(row);
    }

    public void Clear()
    {
        _rows.Clear();
    }
}

public class DataGridViewColumn
{
    public string Name { get; set; } = "";
    public string HeaderText { get; set; } = "";
    public int Width { get; set; } = 100;
    public DataGridViewHeaderCell? HeaderCell { get; set; }
    public bool ReadOnly { get; set; }
    public Type? ValueType { get; set; }
}

public class DataGridViewRow
{
    public List<DataGridViewCell> Cells { get; } = new();
}

public class DataGridViewCell
{
    public object? Value { get; set; }
    public string? Style { get; set; }
}

public class DataGridViewHeaderCell
{
    public string Text { get; set; } = "";
    public Font? Font { get; set; }
}

public class DataGridViewCellEventArgs : EventArgs
{
    public int ColumnIndex { get; }
    public int RowIndex { get; }

    public DataGridViewCellEventArgs(int columnIndex, int rowIndex)
    {
        ColumnIndex = columnIndex;
        RowIndex = rowIndex;
    }
}

public enum DataGridViewSelectionMode
{
    RowHeaderSelect,
    ColumnHeaderSelect,
    FullRowSelect,
    FullColumnSelect,
    CellSelect
}