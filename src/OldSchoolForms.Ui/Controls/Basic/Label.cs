using OldSchoolForms.Ui.Core;
using OldSchoolForms.Ui.Theming;
using Graphics = OldSchoolForms.Ui.Rendering.Graphics;

namespace OldSchoolForms.Ui.Controls.Basic;

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
    /// Called when the theme changes. Updates ForeColor while keeping the transparent background.
    /// </summary>
    /// <param name="newTheme">The new theme that was activated.</param>
    public override void OnThemeChanged(Theme newTheme)
    {
        if (!_foreColorSet)
            _foreColor = newTheme.ControlText;
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

            var theme = ThemeManager.CurrentTheme;
            var textColor = Enabled ? ForeColor : theme.GrayText;
            var font = EffectiveFont;
            g.DrawString(Text, font, textColor, 3, CoordinateTransform.CenterVertically(Height, font, EffectiveZoom));

            base.Render(g);
        }
    }
}