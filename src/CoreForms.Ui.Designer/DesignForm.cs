using System;
using CoreForms.Ui.Core;
using CoreForms.Ui.Theming;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Designer;

/// <summary>
/// Simulates a form being designed. Rendered as a child of <see cref="DesignSurface"/>,
/// it provides a title bar, client area, and hosts the designed controls.
/// Unlike a real <see cref="Form"/>, this is a lightweight container that does not
/// create a native window.
/// </summary>
public class DesignForm : ContainerControl
{
    private string _formTitle = "Form1";
    private const int TitleBarHeight = 30;

    /// <summary>
    /// Gets or sets the simulated form title displayed in the title bar.
    /// </summary>
    public string FormTitle
    {
        get => _formTitle;
        set { _formTitle = value; Invalidate(); }
    }

    /// <summary>
    /// Gets the client area inside the form (below the title bar).
    /// This is where designed controls are placed.
    /// </summary>
    public Rectangle FormClientArea => new Rectangle(
        X + 1, Y + TitleBarHeight + 1,
        Width - 2, Height - TitleBarHeight - 2);

    /// <summary>
    /// Initializes a new instance of <see cref="DesignForm"/>.
    /// </summary>
    public DesignForm()
    {
        Size = new Size(800, 600);
    }

    /// <summary>
    /// Renders the design-form chrome: shadow, border, title bar, then child controls.
    /// </summary>
    /// <param name="g">The graphics context.</param>
    public override void Render(Graphics g)
    {
        var theme = ThemeManager.CurrentTheme;

        // Draw drop shadow
        g.FillRectangle(Color.FromArgb(0, 0, 0, 30), X + 4, Y + 4, Width, Height);

        // Draw form background (white)
        g.FillRectangle(Color.White, X, Y, Width, Height);

        // Draw title bar
        var titleBarColor = theme.ActiveCaption;
        g.FillRectangle(titleBarColor, X, Y, Width, TitleBarHeight);

        // Draw title text
        var titleFont = new Font("Segoe UI", 11f, FontStyle.Regular);
        g.DrawString(_formTitle, titleFont, Color.White, X + 8, Y + (TitleBarHeight - 16) / 2f);

        // Draw form border
        var borderColor = Color.FromArgb(200, 200, 200);
        g.DrawRectangle(borderColor, X, Y, Width, Height, 1);

        // Render child controls (designed controls go here)
        base.Render(g);
    }

    /// <summary>
    /// Gets the clipping rectangle for child controls, excluding the title bar area.
    /// </summary>
    protected override Rectangle GetChildClipRectangle()
    {
        return new Rectangle(1, TitleBarHeight + 1, Width - 2, Height - TitleBarHeight - 2);
    }

    /// <summary>
    /// Gets the render offset for child controls (below the title bar).
    /// </summary>
    protected override Point GetChildRenderOffset()
    {
        return new Point(1, TitleBarHeight + 1);
    }

    /// <summary>
    /// Returns the form-relative position of this design form.
    /// </summary>
    public override Point GetFormRelativePosition()
    {
        int x = X, y = Y;
        Control? current = Parent;
        while (current != null && current is not Form)
        {
            x += current.X;
            y += current.Y;
            current = current.Parent;
        }
        return new Point(x, y);
    }
}
