using OldSchoolForms.Ui.Core;
using OldSchoolForms.Ui.Theming;
using Graphics = OldSchoolForms.Ui.Rendering.Graphics;

namespace OldSchoolForms.Ui.Controls.Separators;

/// <summary>
/// Specifies whether the separator line is drawn horizontally or vertically.
/// </summary>
public enum SeperatorOrientation
{
    /// <summary>
    /// The separator is drawn as a horizontal line (default).
    /// </summary>
    Horizontal,

    /// <summary>
    /// The separator is drawn as a vertical line.
    /// </summary>
    Vertical
}

/// <summary>
/// A control that draws a separator line (horizontal or vertical) to visually
/// divide content regions within a container. The line colors are taken from
/// the active theme (SeparatorDark / SeparatorLight).
/// </summary>
public class SeperatorControl : Control
{
    private SeperatorOrientation _orientation = SeperatorOrientation.Horizontal;
    private int _lineWidth = 1;

    /// <summary>
    /// Initializes a new instance of SeperatorControl.
    /// </summary>
    public SeperatorControl()
    {
        Size = new Size(100, 2);
        TabStop = false;
    }

    /// <summary>
    /// Gets or sets the orientation of the separator line.
    /// </summary>
    public SeperatorOrientation Orientation
    {
        get => _orientation;
        set
        {
            if (_orientation != value)
            {
                _orientation = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets the thickness of the separator line in pixels.
    /// </summary>
    public int LineWidth
    {
        get => _lineWidth;
        set
        {
            if (_lineWidth != value)
            {
                _lineWidth = Math.Max(1, value);
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Renders the separator line using the active theme's separator colors.
    /// A dark line is drawn at the top/left edge and a light line below/to the right,
    /// creating a subtle etched appearance.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
    public override void Render(Graphics g)
    {
        if (!Visible) return;

        var theme = ThemeManager.CurrentTheme;

        if (_orientation == SeperatorOrientation.Horizontal)
        {
            int y = Height / 2;
            g.DrawLine(theme.SeparatorDark, 0, y, Width, y, _lineWidth);
            g.DrawLine(theme.SeparatorLight, 0, y + _lineWidth, Width, y + _lineWidth, _lineWidth);
        }
        else
        {
            int x = Width / 2;
            g.DrawLine(theme.SeparatorDark, x, 0, x, Height, _lineWidth);
            g.DrawLine(theme.SeparatorLight, x + _lineWidth, 0, x + _lineWidth, Height, _lineWidth);
        }

        base.Render(g);
    }
}
