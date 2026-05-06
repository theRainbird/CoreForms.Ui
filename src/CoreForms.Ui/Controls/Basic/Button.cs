using CoreForms.Ui.Core;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Basic;

/// <summary>
/// A control that can be clicked to perform an action.
/// </summary>
public class Button : Control
{
    private bool _isPressed;
    private bool _isHovered;

    /// <summary>
    /// Initializes a new instance of Button.
    /// </summary>
    public Button()
    {
        BackColor = SystemColors.Control;
        Size = new Size(120, 40);
        TabStop = true;
    }

    /// <summary>
    /// Renders the button with its background, border, and text.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
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

        if (Focused)
        {
            g.DrawRectangle(Color.FromArgb(0, 120, 215), 0, 0, Width, Height, 2);
        }

        var font = Font ?? Font.Default;
        var textSize = Text.Length * (int)font.Size * 0.6f;
        var x = (Width - textSize) / 2;
        var y = (Height - (int)font.Size) / 2;
        g.DrawString(Text, font, ForeColor, x > 0 ? x : 3, y > 0 ? y : 3);

        base.Render(g);
    }

    /// <summary>
    /// Raises the MouseDown event and tracks pressed state.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseDown(EventArgs e)
    {
        _isPressed = true;
        base.OnMouseDown(e);
    }

    /// <summary>
    /// Raises the MouseUp event and tracks pressed state.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseUp(EventArgs e)
    {
        _isPressed = false;
        base.OnMouseUp(e);
    }

    /// <summary>
    /// Raises the MouseEnter event and tracks hover state.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected override void OnMouseEnter(EventArgs e)
    {
        _isHovered = true;
        base.OnMouseEnter(e);
    }

    /// <summary>
    /// Raises the MouseLeave event and tracks hover state.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected override void OnMouseLeave(EventArgs e)
    {
        _isHovered = false;
        base.OnMouseLeave(e);
    }

    /// <summary>
    /// Raises the KeyDown event to handle Enter and Space keys.
    /// </summary>
    /// <param name="e">A KeyEventArgs that contains the event data.</param>
    protected internal override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
        {
            PerformClick();
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }

    /// <summary>
    /// Programmatically clicks the button, raising the Click event.
    /// </summary>
    public void PerformClick()
    {
        OnClick(EventArgs.Empty);
    }
}