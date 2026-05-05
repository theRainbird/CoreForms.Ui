using CoreForms.Ui.Core;
using CoreForms.Ui.Rendering;

namespace CoreForms.Ui.Layout;

public class TableLayoutPanel : ContainerControl
{
    private int _rowCount = 2;
    private int _columnCount = 2;
    private readonly List<TableLayoutStyle> _styles = new();

    public TableLayoutPanel()
    {
        Size = new Size(300, 200);
        BackColor = SystemColors.Control;
    }

    public int RowCount
    {
        get => _rowCount;
        set
        {
            _rowCount = value;
            LayoutChildren();
        }
    }

    public int ColumnCount
    {
        get => _columnCount;
        set
        {
            _columnCount = value;
            LayoutChildren();
        }
    }

    public void LayoutChildren()
    {
        LayoutControls();
    }

    private void LayoutControls()
    {
        if (Controls.Count == 0) return;

        int cellWidth = (Width - 4) / _columnCount;
        int cellHeight = (Height - 4) / _rowCount;

        int index = 0;
        for (int row = 0; row < _rowCount && index < Controls.Count; row++)
        {
            for (int col = 0; col < _columnCount && index < Controls.Count; col++)
            {
                var child = Controls[index];
                child.Location = new Point(col * cellWidth + 2, row * cellHeight + 2);
                child.Size = new Size(cellWidth - 4, cellHeight - 4);
                index++;
            }
        }
    }

    public override void Render(Graphics g)
    {
        if (!Visible) return;

        g.FillRectangle(BackColor, 0, 0, Width, Height);

        int cellWidth = (Width - 4) / _columnCount;
        int cellHeight = (Height - 4) / _rowCount;

        for (int row = 0; row <= _rowCount; row++)
        {
            g.DrawLine(Color.FromArgb(180, 180, 180), 2, row * cellHeight + 2, Width - 2, row * cellHeight + 2);
        }
        for (int col = 0; col <= _columnCount; col++)
        {
            g.DrawLine(Color.FromArgb(180, 180, 180), col * cellWidth + 2, 2, col * cellWidth + 2, Height - 2);
        }

        base.Render(g);
    }
}