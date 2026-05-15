using CoreForms.Ui.Core;
using CoreForms.Ui.Theming;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Containers;

/// <summary>
/// Specifies the orientation of a SplitPanel.
/// </summary>
public enum SplitOrientation
{
    /// <summary>
    /// Panels are arranged horizontally (left/right) with a vertical splitter.
    /// </summary>
    Horizontal,

    /// <summary>
    /// Panels are arranged vertically (top/bottom) with a horizontal splitter.
    /// </summary>
    Vertical
}

/// <summary>
/// Represents a container that divides the available space into two resizable panels
/// separated by a movable splitter bar. Similar to WinForms SplitContainer.
/// </summary>
public class SplitPanel : ContainerControl
{
    private SplitOrientation _orientation = SplitOrientation.Vertical;
    private int _splitterWidth = 10;
    private int _splitterDistance = 100;
    private int _panel1MinSize = 25;
    private int _panel2MinSize = 25;
    private BorderStyle _borderStyle = BorderStyle.None;
    private bool _isDragging;
    private bool _isSplitterHovered;
    private int _dragOffset;

    private readonly Panel _panel1;
    private readonly Panel _panel2;

    /// <summary>
    /// Gets or sets the orientation of the splitter.
    /// Horizontal arranges panels left/right with a vertical splitter.
    /// Vertical arranges panels top/bottom with a horizontal splitter.
    /// </summary>
    public SplitOrientation Orientation
    {
        get => _orientation;
        set
        {
            if (_orientation != value)
            {
                _orientation = value;
                PerformLayout();
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets the thickness of the splitter bar in pixels.
    /// </summary>
    public int SplitterWidth
    {
        get => _splitterWidth;
        set
        {
            value = Math.Max(3, value);
            if (_splitterWidth != value)
            {
                _splitterWidth = value;
                PerformLayout();
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets the position of the splitter from the left (Horizontal) or top (Vertical) edge in pixels.
    /// </summary>
    public int SplitterDistance
    {
        get => _splitterDistance;
        set
        {
            value = ClampSplitterDistance(value);
            if (_splitterDistance != value)
            {
                _splitterDistance = value;
                PerformLayout();
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets the minimum size of Panel1 in pixels.
    /// </summary>
    public int Panel1MinSize
    {
        get => _panel1MinSize;
        set
        {
            value = Math.Max(0, value);
            if (_panel1MinSize != value)
            {
                _panel1MinSize = value;
                PerformLayout();
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets the minimum size of Panel2 in pixels.
    /// </summary>
    public int Panel2MinSize
    {
        get => _panel2MinSize;
        set
        {
            value = Math.Max(0, value);
            if (_panel2MinSize != value)
            {
                _panel2MinSize = value;
                PerformLayout();
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets the border style of the SplitPanel.
    /// </summary>
    public BorderStyle BorderStyle
    {
        get => _borderStyle;
        set
        {
            _borderStyle = value;
            Invalidate();
        }
    }

    /// <summary>
    /// Gets the left/top panel. In Horizontal orientation this is the left panel,
    /// in Vertical orientation this is the top panel.
    /// </summary>
    public Panel Panel1 => _panel1;

    /// <summary>
    /// Gets the right/bottom panel. In Horizontal orientation this is the right panel,
    /// in Vertical orientation this is the bottom panel.
    /// </summary>
    public Panel Panel2 => _panel2;

    /// <summary>
    /// Occurs while the splitter is being moved by the user.
    /// </summary>
    public event EventHandler<SplitterDragEventArgs>? SplitterMoving;

    /// <summary>
    /// Occurs when the splitter has finished being moved by the user.
    /// </summary>
    public event EventHandler? SplitterMoved;

    /// <summary>
    /// Initializes a new instance of SplitPanel.
    /// </summary>
    public SplitPanel()
    {
        _backColor = ThemeManager.CurrentTheme.ControlBackground;

        _panel1 = new Panel();
        _panel2 = new Panel();
        _panel1.TabStop = false;
        _panel2.TabStop = false;

        _splitterDistance = 150;
        TabStop = false;

        Controls.Add(_panel1);
        Controls.Add(_panel2);

        Size = new Size(400, 300);
    }

    /// <summary>
    /// Updates the theme colors when the active theme changes.
    /// Panel1 and Panel2 handle their own theme updates autonomously.
    /// </summary>
    /// <param name="newTheme">The new theme.</param>
    public override void OnThemeChanged(Theme newTheme)
    {
        if (!_backColorSet)
            _backColor = newTheme.ControlBackground;
        Invalidate();
    }

    /// <summary>
    /// Arranges Panel1, Panel2, and the splitter area within the control bounds.
    /// </summary>
    protected override void OnLayout()
    {
        if (_panel1 == null || _panel2 == null)
        {
            base.OnLayout();
            return;
        }

        int totalSize = _orientation == SplitOrientation.Vertical ? Height : Width;
        _splitterDistance = ClampSplitterDistance(_splitterDistance);

        if (_orientation == SplitOrientation.Vertical)
        {
            _panel1.Bounds = new Rectangle(0, 0, Width, _splitterDistance);
            int panel2Height = Math.Max(0, Height - _splitterDistance - _splitterWidth);
            _panel2.Bounds = new Rectangle(0, _splitterDistance + _splitterWidth, Width, panel2Height);
        }
        else
        {
            _panel1.Bounds = new Rectangle(0, 0, _splitterDistance, Height);
            int panel2Width = Math.Max(0, Width - _splitterDistance - _splitterWidth);
            _panel2.Bounds = new Rectangle(_splitterDistance + _splitterWidth, 0, panel2Width, Height);
        }

        base.OnLayout();
    }

    /// <summary>
    /// Returns the bounding rectangle of the splitter bar in client coordinates.
    /// </summary>
    private Rectangle GetSplitterRectangle()
    {
        if (_orientation == SplitOrientation.Vertical)
            return new Rectangle(0, _splitterDistance, Width, _splitterWidth);
        else
            return new Rectangle(_splitterDistance, 0, _splitterWidth, Height);
    }

    /// <summary>
    /// Clamps the splitter distance to respect minimum panel sizes and control bounds.
    /// </summary>
    private int ClampSplitterDistance(int distance)
    {
        int totalSize = _orientation == SplitOrientation.Vertical ? Height : Width;
        int maxDistance = Math.Max(0, totalSize - _panel2MinSize - _splitterWidth);
        return Math.Max(_panel1MinSize, Math.Min(distance, maxDistance));
    }

    /// <summary>
    /// Returns the child control at the specified point. Returns null for the splitter area
    /// so that mouse events in the splitter area are handled by SplitPanel.
    /// </summary>
    /// <param name="point">The point in client coordinates.</param>
    /// <returns>The child control at the point, or null.</returns>
    protected override Control? GetChildAtPoint(Point point)
    {
        if (GetSplitterRectangle().Contains(point))
            return null;

        for (int i = Controls.Count - 1; i >= 0; i--)
        {
            var child = Controls[i];
            if (child.Visible && child.Bounds.Contains(point))
                return child;
        }
        return null;
    }

    /// <summary>
    /// Renders the splitter bar with grip indicators on top of the child panels.
    /// Each panel is clipped to its own bounds so children cannot render outside.
    /// </summary>
    /// <param name="g">The Graphics object.</param>
    public override void Render(Graphics g)
    {
        if (!Visible) return;

        var theme = ThemeManager.CurrentTheme;

        RenderPanelClipped(g, _panel1);
        RenderPanelClipped(g, _panel2);

        var splitRect = GetSplitterRectangle();

        var bgColor = BackColor;
        if (_isDragging)
            bgColor = theme.ButtonPressedBackground;
        else if (_isSplitterHovered)
            bgColor = theme.ButtonHoverBackground;

        g.FillRectangle(bgColor, splitRect.X, splitRect.Y, splitRect.Width, splitRect.Height);
        g.DrawRectangle(theme.ButtonBorder, splitRect.X, splitRect.Y, splitRect.Width, splitRect.Height, 1);

        DrawSplitterGrip(g, splitRect, theme);

        if (_borderStyle == BorderStyle.FixedSingle)
            g.DrawRectangle(theme.PanelBorder, 0, 0, Width, Height, 1);
    }

    private static void RenderPanelClipped(Graphics g, Panel panel)
    {
        g.Save();
        g.SetClip(panel.Bounds);
        g.TranslateTransform(panel.X, panel.Y);
        panel.Render(g);
        g.Restore();
        g.ResetClip();
    }

    /// <summary>
    /// Renders overlay elements (like dropdowns) for both panels with clipping.
    /// </summary>
    /// <param name="g">The Graphics object.</param>
    public override void RenderOverlay(Graphics g)
    {
        if (!Visible) return;

        RenderPanelOverlayClipped(g, _panel1);
        RenderPanelOverlayClipped(g, _panel2);
    }

    private static void RenderPanelOverlayClipped(Graphics g, Panel panel)
    {
        g.Save();
        g.SetClip(panel.Bounds);
        g.TranslateTransform(panel.X, panel.Y);
        panel.RenderOverlay(g);
        g.Restore();
        g.ResetClip();
    }

    /// <summary>
    /// Draws grip indicator dots on the splitter bar to indicate it is draggable.
    /// </summary>
    private void DrawSplitterGrip(Graphics g, Rectangle splitRect, Theme theme)
    {
        int dotSize = 3;
        int gap = 4;
        int dotCount = 3;
        Color dotColor = theme.ControlDark;

        if (_orientation == SplitOrientation.Vertical)
        {
            int centerY = splitRect.Y + splitRect.Height / 2 - dotSize / 2;
            int totalWidth = dotCount * dotSize + (dotCount - 1) * gap;
            int startX = splitRect.X + (splitRect.Width - totalWidth) / 2;

            for (int i = 0; i < dotCount; i++)
                g.FillRectangle(dotColor, startX + i * (dotSize + gap), centerY, dotSize, dotSize);
        }
        else
        {
            int centerX = splitRect.X + splitRect.Width / 2 - dotSize / 2;
            int totalHeight = dotCount * dotSize + (dotCount - 1) * gap;
            int startY = splitRect.Y + (splitRect.Height - totalHeight) / 2;

            for (int i = 0; i < dotCount; i++)
                g.FillRectangle(dotColor, centerX, startY + i * (dotSize + gap), dotSize, dotSize);
        }
    }

    /// <summary>
    /// Handles mouse down events to initiate splitter dragging.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseDown(EventArgs e)
    {
        var args = e as MouseEventArgs;
        if (args != null && args.Button == MouseButtons.Left && GetSplitterRectangle().Contains(args.X, args.Y))
        {
            _isDragging = true;
            CapturingMouse = true;
            _dragOffset = _orientation == SplitOrientation.Vertical
                ? args.Y - _splitterDistance
                : args.X - _splitterDistance;
            SplitterMoving?.Invoke(this, new SplitterDragEventArgs { NewPosition = _splitterDistance });
            Invalidate();
            return;
        }
        base.OnMouseDown(e);
    }

    /// <summary>
    /// Handles mouse move events to update the splitter position during drag
    /// and to track hover state over the splitter.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseMove(EventArgs e)
    {
        var args = e as MouseEventArgs;
        if (args != null)
        {
            bool wasHovered = _isSplitterHovered;
            _isSplitterHovered = GetSplitterRectangle().Contains(args.X, args.Y);
            if (_isSplitterHovered != wasHovered)
                Invalidate();

            if (_isDragging)
            {
                int newDistance = (_orientation == SplitOrientation.Vertical ? args.Y : args.X) - _dragOffset;
                newDistance = ClampSplitterDistance(newDistance);
                if (_splitterDistance != newDistance)
                {
                    _splitterDistance = newDistance;
                    SplitterMoving?.Invoke(this, new SplitterDragEventArgs { NewPosition = _splitterDistance });
                    PerformLayout();
                    Invalidate();
                }
                return;
            }
        }
        base.OnMouseMove(e);
    }

    /// <summary>
    /// Handles mouse up events to finish splitter dragging.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseUp(EventArgs e)
    {
        var args = e as MouseEventArgs;
        if (args != null && args.Button == MouseButtons.Left && _isDragging)
        {
            _isDragging = false;
            CapturingMouse = false;
            SplitterMoved?.Invoke(this, EventArgs.Empty);
            Invalidate();
            return;
        }
        base.OnMouseUp(e);
    }
}
