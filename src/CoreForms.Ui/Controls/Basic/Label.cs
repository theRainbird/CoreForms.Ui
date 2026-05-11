using CoreForms.Ui.Core;
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
        BackColor = SystemColors.Control;
        Size = new Size(150, 28);
    }

    /// <summary>
    /// Renders the label with its text.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
    public override void Render(Rendering.Graphics g)
    {
        if (Visible)
        {
            g.FillRectangle(BackColor, 0, 0, Width, Height);

            var font = EffectiveFont;
            g.DrawString(Text, font, ForeColor, 3, CoordinateTransform.CenterVertically(Height, font, EffectiveZoom));

            base.Render(g);
        }
    }
}