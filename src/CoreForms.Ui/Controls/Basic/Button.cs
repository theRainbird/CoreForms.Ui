using CoreForms.Ui.Core;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Basic;

public class Button : Control
{
    private bool _isPressed;
    private bool _isHovered;

    public Button()
    {
        BackColor = SystemColors.Control;
        Size = new Size(120, 40);
        TabStop = true;
    }

    public override void Render(Rendering.Graphics g)
    {
        if (!Visible) return;

        var bgColor = BackColor;
        if (_isPressed)
            bgColor = Color.FromArgb(bgColor.R - 20, bgColor.G - 20, bgColor.B - 20);
        else if (_isHovered)
            bgColor = Color.FromArgb(Math.Min(255, bgColor.R + 15), Math.Min(255, bgColor.G + 15), Math.Min(255, bgColor.B + 15));

        g.FillRectangle(bgColor, 0, 0, Width, Height);
        g.DrawRectangle(Color.FromArgb(128, 128, 128), 0, 0, Width, Height, 1);

        var font = Font ?? Font.Default;
        var textSize = Text.Length * (int)font.Size * 0.6f;
        var x = (Width - textSize) / 2;
        var y = (Height - (int)font.Size) / 2;
        g.DrawString(Text, font, ForeColor, x > 0 ? x : 3, y > 0 ? y : 3);

        base.Render(g);
    }

    protected internal override void OnMouseDown(EventArgs e)
    {
        _isPressed = true;
        base.OnMouseDown(e);
    }

    protected internal override void OnMouseUp(EventArgs e)
    {
        _isPressed = false;
        base.OnMouseUp(e);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _isHovered = true;
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _isHovered = false;
        base.OnMouseLeave(e);
    }

    public void PerformClick()
    {
        OnClick(EventArgs.Empty);
    }

    public bool TabStop { get; set; }
}