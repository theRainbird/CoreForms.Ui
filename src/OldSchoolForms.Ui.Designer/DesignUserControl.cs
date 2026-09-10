using OldSchoolForms.Ui.Core;
using OldSchoolForms.Ui.Controls.Containers;
using OldSchoolForms.Ui.Theming;
using Graphics = OldSchoolForms.Ui.Rendering.Graphics;

namespace OldSchoolForms.Ui.Designer;

/// <summary>
/// Simulates a <c>UserControl</c> being designed.
/// Unlike <see cref="DesignForm"/> it has no title bar, so its entire area is a
/// client area that hosts the designed controls. It provides a border so its base
/// area is visually distinguishable from the surrounding design surface.
/// </summary>
public class DesignUserControl : ContainerControl
{
    private BorderStyle _borderStyle = BorderStyle.FixedSingle;

    /// <summary>
    /// Gets or sets the border style rendered around the user control.
    /// </summary>
    public BorderStyle BorderStyle
    {
        get => _borderStyle;
        set { _borderStyle = value; Invalidate(); }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="DesignUserControl"/>.
    /// </summary>
    public DesignUserControl()
    {
        Size = new Size(150, 100);
    }

    /// <summary>
    /// Renders the design-user-control chrome: background and optional border, then child controls.
    /// </summary>
    /// <param name="g">The graphics context.</param>
    public override void Render(Graphics g)
    {
        var theme = ThemeManager.CurrentTheme;

        // Draw user control background in its own background color so its base area is
        // distinguishable from the surface.
        g.FillRectangle(BackColor, 0, 0, Width, Height);

        if (_borderStyle == BorderStyle.FixedSingle)
        {
            g.DrawRectangle(Color.FromArgb(200, 200, 200), 0, 0, Width, Height, 1);
        }

        base.Render(g);
    }

    /// <summary>
    /// Gets the clipping rectangle for child controls (the whole control, no title bar).
    /// </summary>
    protected override Rectangle GetChildClipRectangle()
    {
        return new Rectangle(0, 0, Width, Height);
    }

    /// <summary>
    /// Gets the render offset for child controls (none — the client area starts at the origin).
    /// </summary>
    protected override Point GetChildRenderOffset()
    {
        return Point.Empty;
    }
}
