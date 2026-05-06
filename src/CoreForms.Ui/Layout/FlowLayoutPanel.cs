using CoreForms.Ui.Core;
using CoreForms.Ui.Rendering;

namespace CoreForms.Ui.Layout;

public class FlowLayoutPanel : ContainerControl
{
    private FlowDirection _flowDirection = FlowDirection.LeftToRight;
    private int _wrapContents = 1;

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

    public new int Padding
    {
        get => base.Padding.Left;
        set => base.Padding = new CoreForms.Ui.Core.Padding(value);
    }

    public void LayoutChildren()
    {
        LayoutControls();
    }

    private void LayoutControls()
    {
        int padLeft = base.Padding.Left;
        int padTop = base.Padding.Top;
        int padRight = base.Padding.Right;
        int padBottom = base.Padding.Bottom;

        int x = padLeft;
        int y = padTop;
        int rowHeight = 0;
        int maxWidth = 0;
        int availableWidth = Width - padLeft - padRight;
        int availableHeight = Height - padTop - padBottom;

        foreach (Control child in Controls)
        {
            if (!child.Visible) continue;

            if (_flowDirection == FlowDirection.LeftToRight)
            {
                if (x + child.Width > padLeft + availableWidth && x > padLeft)
                {
                    x = padLeft;
                    y += rowHeight + padTop;
                    rowHeight = 0;
                }

                child._layoutDrivenBoundsChange = true;
                child.Location = new Point(x, y);
                child._layoutDrivenBoundsChange = false;
                x += child.Width + padLeft;
                rowHeight = Math.Max(rowHeight, child.Height);
                maxWidth = Math.Max(maxWidth, x);
            }
            else
            {
                if (y + child.Height > padTop + availableHeight && y > padTop)
                {
                    y = padTop;
                    x += maxWidth + padLeft;
                    maxWidth = 0;
                }

                child._layoutDrivenBoundsChange = true;
                child.Location = new Point(x, y);
                child._layoutDrivenBoundsChange = false;
                y += child.Height + padTop;
                maxWidth = Math.Max(maxWidth, child.Width);
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

        base.Render(g);
    }
}