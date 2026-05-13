using CoreForms.Ui.Core;
using CoreForms.Ui.Theming;
using CoreForms.Ui.Rendering;

namespace CoreForms.Ui.Layout;

/// <summary>
/// A layout panel that arranges child controls in a flow direction (left-to-right or top-down).
/// </summary>
public class FlowLayoutPanel : ContainerControl
{
    private FlowDirection _flowDirection = FlowDirection.LeftToRight;
    
    /// <summary>
    /// Initializes a new instance of FlowLayoutPanel.
    /// </summary>
    public FlowLayoutPanel()
    {
        Size = new Size(300, 200);
        _backColor = ThemeManager.CurrentTheme.ControlBackground;
    }

    /// <summary>
    /// Called when the theme changes. Updates flowlayoutpanel-specific colors.
    /// </summary>
    /// <param name="newTheme">The new theme that was activated.</param>
    public override void OnThemeChanged(Theme newTheme)
    {
        if (!_backColorSet)
            _backColor = newTheme.ControlBackground;
        Invalidate();
    }

    /// <summary>
    /// Gets or sets the direction in which child controls are arranged.
    /// </summary>
    public FlowDirection FlowDirection
    {
        get => _flowDirection;
        set
        {
            _flowDirection = value;
            LayoutChildren();
        }
    }

    /// <summary>
    /// Gets or sets the padding (all sides) for child controls.
    /// </summary>
    public new int Padding
    {
        get => base.Padding.Left;
        set => base.Padding = new CoreForms.Ui.Core.Padding(value);
    }

    /// <summary>
    /// Performs layout of child controls.
    /// </summary>
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

    /// <summary>
    /// Called when the control needs to perform layout.
    /// </summary>
    protected override void OnLayout()
    {
        LayoutControls();
    }

    /// <summary>
    /// Renders the control and its background.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
    public override void Render(Graphics g)
    {
        if (!Visible) return;

        g.FillRectangle(BackColor, 0, 0, Width, Height);

        base.Render(g);
    }
}