using CoreForms.Ui.Core;
using CoreForms.Ui.Theming;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Basic;

/// <summary>
/// A control that can be clicked to perform an action.
/// </summary>
public class Button : Control
{
    /// <summary>
    /// Initializes a new instance of Button.
    /// </summary>
    public Button()
    {
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

        var theme = ThemeManager.CurrentTheme;
        var bgColor = BackColor;
        if (IsPressed)
            bgColor = theme.ButtonPressedBackground;
        else if (IsHovered)
            bgColor = theme.ButtonHoverBackground;

        g.FillRectangle(bgColor, 0, 0, Width, Height);
        g.DrawRectangle(theme.ButtonBorder, 0, 0, Width, Height, 1);

        DrawFocusIndicator(g);

        var font = EffectiveFont;
        float zoom = EffectiveZoom;
        var textSize = Text.Length * font.Size * zoom * 0.6f;
        var x = (Width - textSize) / 2;
        var y = CoordinateTransform.CenterVertically(Height, font, zoom);
        g.DrawString(Text, font, ForeColor, x > 0 ? x : 3, y > 0 ? y : 3);

        base.Render(g);
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