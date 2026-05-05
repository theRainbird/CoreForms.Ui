using CoreForms.Ui.Core;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Containers;

public class Panel : ContainerControl
{
    private BorderStyle _borderStyle = BorderStyle.None;

    public Panel()
    {
        BackColor = SystemColors.Control;
        Size = new Size(200, 150);
    }

    public BorderStyle BorderStyle
    {
        get => _borderStyle;
        set
        {
            _borderStyle = value;
            Invalidate();
        }
    }

    public override void Render(Graphics g)
    {
        if (!Visible) return;

        g.FillRectangle(BackColor, 0, 0, Width, Height);

        if (_borderStyle == BorderStyle.FixedSingle)
        {
            g.DrawRectangle(Color.FromArgb(128, 128, 128), 0, 0, Width, Height, 1);
        }

        base.Render(g);
    }
}

public enum BorderStyle
{
    None,
    FixedSingle,
    Fixed3D
}