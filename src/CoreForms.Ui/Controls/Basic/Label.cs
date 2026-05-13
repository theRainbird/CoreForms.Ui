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
        TextAlign = ContentAlignment.MiddleLeft;
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
            float zoom = EffectiveZoom;
            var textSize = Platform.Platform.MeasureText(Text, font, zoom);
            var padding = 3;
            var textX = TextAlign switch
            {
                ContentAlignment.TopLeft or ContentAlignment.MiddleLeft or ContentAlignment.BottomLeft => padding,
                ContentAlignment.TopCenter or ContentAlignment.MiddleCenter or ContentAlignment.BottomCenter => (Width - textSize.width) / 2,
                ContentAlignment.TopRight or ContentAlignment.MiddleRight or ContentAlignment.BottomRight => Width - textSize.width - padding,
                _ => padding
            };
            var textY = TextAlign switch
            {
                ContentAlignment.TopLeft or ContentAlignment.TopCenter or ContentAlignment.TopRight => padding,
                ContentAlignment.MiddleLeft or ContentAlignment.MiddleCenter or ContentAlignment.MiddleRight => CoordinateTransform.CenterVertically(Height, font, zoom),
                ContentAlignment.BottomLeft or ContentAlignment.BottomCenter or ContentAlignment.BottomRight => Height - textSize.height - padding,
                _ => CoordinateTransform.CenterVertically(Height, font, zoom)
            };
            g.DrawString(Text, font, ForeColor, textX, textY);

            base.Render(g);
        }
    }
}