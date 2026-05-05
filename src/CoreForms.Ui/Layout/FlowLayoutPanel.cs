using CoreForms.Ui.Core;
using CoreForms.Ui.Rendering;

namespace CoreForms.Ui.Layout;

public class FlowLayoutPanel : ContainerControl
{
    private FlowDirection _flowDirection = FlowDirection.LeftToRight;
    private int _wrapContents = 1;
    private int _padding = 0;

    public FlowLayoutPanel()
    {
        Size = new Size(300, 200);
        BackColor = SystemColors.Control;
    }

    public FlowDirection FlowDirection
    {
        get => _flowDirection;
        set
        {
            _flowDirection = value;
            LayoutChildren();
        }
    }

    public int Padding
    {
        get => _padding;
        set
        {
            _padding = value;
            LayoutChildren();
        }
    }

    public void LayoutChildren()
    {
        LayoutControls();
    }

    private void LayoutControls()
    {
        int x = _padding;
        int y = _padding;
        int rowHeight = 0;
        int maxWidth = 0;

        foreach (Control child in Controls)
        {
            if (!child.Visible) continue;

            if (_flowDirection == FlowDirection.LeftToRight)
            {
                if (x + child.Width > Width - _padding && x > _padding)
                {
                    x = _padding;
                    y += rowHeight + _padding;
                    rowHeight = 0;
                }

                child.Location = new Point(x, y);
                x += child.Width + _padding;
                rowHeight = Math.Max(rowHeight, child.Height);
                maxWidth = Math.Max(maxWidth, x);
            }
            else
            {
                if (y + child.Height > Height - _padding && y > _padding)
                {
                    y = _padding;
                    x += maxWidth + _padding;
                    maxWidth = 0;
                }

                child.Location = new Point(x, y);
                y += child.Height + _padding;
                maxWidth = Math.Max(maxWidth, child.Width);
            }
        }
    }

    public override void Render(Graphics g)
    {
        if (!Visible) return;

        g.FillRectangle(BackColor, 0, 0, Width, Height);

        base.Render(g);
    }
}