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

        int padLeft = Padding.Left + 2;
        int padTop = Padding.Top + 2;
        int innerWidth = Width - Padding.Horizontal - 4;
        int innerHeight = Height - Padding.Vertical - 4;

        int cellWidth = innerWidth / _columnCount;
        int cellHeight = innerHeight / _rowCount;

        int index = 0;
        for (int row = 0; row < _rowCount && index < Controls.Count; row++)
        {
            for (int col = 0; col < _columnCount && index < Controls.Count; col++)
            {
                var child = Controls[index];
                child._layoutDrivenBoundsChange = true;
                child.Location = new Point(col * cellWidth + padLeft, row * cellHeight + padTop);
                child.Size = new Size(cellWidth - 4, cellHeight - 4);
                child._layoutDrivenBoundsChange = false;
                index++;
            }
        }
    }

    protected override void OnLayout()
    {
        LayoutControls();
    }

    public override void Render(Graphics g)
    {
        if (!Visible) return;

        g.FillRectangle(BackColor, 0, 0, Width, Height);

        int padLeft = Padding.Left + 2;
        int padTop = Padding.Top + 2;
        int innerWidth = Width - Padding.Horizontal - 4;
        int innerHeight = Height - Padding.Vertical - 4;

        int cellWidth = innerWidth / _columnCount;
        int cellHeight = innerHeight / _rowCount;

        for (int row = 0; row <= _rowCount; row++)
        {
            g.DrawLine(Color.FromArgb(180, 180, 180), padLeft, row * cellHeight + padTop, Width - Padding.Right - 2, row * cellHeight + padTop);
        }
        for (int col = 0; col <= _columnCount; col++)
        {
            g.DrawLine(Color.FromArgb(180, 180, 180), col * cellWidth + padLeft, padTop, col * cellWidth + padLeft, Height - Padding.Bottom - 2);
        }

        base.Render(g);
    }
}