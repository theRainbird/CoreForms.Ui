using OldSchoolForms.Ui.Core;
using OldSchoolForms.Ui.Theming;
using OldSchoolForms.Ui.Rendering;

namespace OldSchoolForms.Ui.Layout;

/// <summary>
/// A layout panel that arranges child controls in a flow direction (left-to-right or top-down).
/// </summary>
public class FlowLayoutPanel : ContainerControl
{
    private FlowDirection _flowDirection = FlowDirection.LeftToRight;
    private Point[]? _layoutCache;
    private int _cachedLayoutVersion = -1;

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
            MarkLayoutDirty();
            PerformLayout();
        }
    }

    /// <summary>
    /// Gets or sets the padding (all sides) for child controls.
    /// </summary>
    public new int Padding
    {
        get => base.Padding.Left;
        set => base.Padding = new OldSchoolForms.Ui.Core.Padding(value);
    }

    private void LayoutControls()
    {
        if (_cachedLayoutVersion == _layoutVersion && _layoutCache != null && _layoutCache.Length == Controls.Count)
        {
            for (int i = 0; i < Controls.Count; i++)
            {
                var child = Controls[i];
                if (!child.Visible) continue;
                child._layoutDrivenBoundsChange = true;
                child.Location = _layoutCache[i];
                child._layoutDrivenBoundsChange = false;
            }
            return;
        }

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

        var cache = Controls.Count > 0 ? new Point[Controls.Count] : null;

        for (int i = 0; i < Controls.Count; i++)
        {
            var child = Controls[i];
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
                cache![i] = child.Location;
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
                cache![i] = child.Location;
                y += child.Height + padTop;
                maxWidth = Math.Max(maxWidth, child.Width);
            }
        }

        _layoutCache = cache;
        _cachedLayoutVersion = _layoutVersion;
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