using CoreForms.Ui.Core;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Basic;

public class Label : Control
{
    public Label()
    {
        BackColor = SystemColors.Control;
        Size = new Size(150, 28);
    }

    public override void Render(Rendering.Graphics g)
    {
        if (Visible)
        {
            g.FillRectangle(BackColor, 0, 0, Width, Height);

            var font = Font ?? Font.Default;
            g.DrawString(Text, font, ForeColor, 3, (Height - (int)font.Size) / 2);

            base.Render(g);
        }
    }
}