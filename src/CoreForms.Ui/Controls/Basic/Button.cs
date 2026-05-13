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
        var textSize = Platform.Platform.MeasureText(Text, font, zoom);
        var padding = 6;
        var textX = TextAlign switch
        {
            ContentAlignment.TopLeft or ContentAlignment.MiddleLeft or ContentAlignment.BottomLeft => padding,
            ContentAlignment.TopCenter or ContentAlignment.MiddleCenter or ContentAlignment.BottomCenter => (Width - textSize.width) / 2,
            ContentAlignment.TopRight or ContentAlignment.MiddleRight or ContentAlignment.BottomRight => Width - textSize.width - padding,
            _ => (Width - textSize.width) / 2
        };
        var textY = TextAlign switch
        {
            ContentAlignment.TopLeft or ContentAlignment.TopCenter or ContentAlignment.TopRight => padding,
            ContentAlignment.MiddleLeft or ContentAlignment.MiddleCenter or ContentAlignment.MiddleRight => CoordinateTransform.CenterVertically(Height, font, zoom),
            ContentAlignment.BottomLeft or ContentAlignment.BottomCenter or ContentAlignment.BottomRight => Height - textSize.height - padding,
            _ => CoordinateTransform.CenterVertically(Height, font, zoom)
        };
        g.DrawString(Text, font, ForeColor, textX > padding ? textX : padding, textY > padding ? textY : padding);

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