using CoreForms.Ui.Core;
using CoreForms.Ui.Theming;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Basic;

/// <summary>
/// A control that displays static text.
/// </summary>
public class Label : Control
{
    /// <summary>
    /// Initializes a new instance of Label.
    /// </summary>
    public Label()
    {
        _backColor = Color.Transparent;
        Size = new Size(150, 28);
    }

    /// <summary>
    /// Called when the theme changes. Keeps the transparent background.
    /// </summary>
    /// <param name="newTheme">The new theme that was activated.</param>
    public override void OnThemeChanged(Theme newTheme)
    {
        base.OnThemeChanged(newTheme);
        Invalidate();
    }

    /// <summary>
    /// Renders the label with its text.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
    public override void Render(Rendering.Graphics g)
    {
        if (Visible)
        {
            if (BackColor.A > 0)
            {
                g.FillRectangle(BackColor, 0, 0, Width, Height);
            }

            var font = EffectiveFont;
            g.DrawString(Text, font, ForeColor, 3, CoordinateTransform.CenterVertically(Height, font, EffectiveZoom));

            base.Render(g);
        }
    }
}