using CoreForms.Ui.Core;
using CoreForms.Ui.Theming;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Basic;

/// <summary>
/// A container control that displays a bordered group box with a title.
/// Used to logically group related controls within a form.
/// </summary>
public class GroupBox : ContainerControl
{
    /// <summary>
    /// Initializes a new instance of GroupBox.
    /// </summary>
    public GroupBox()
    {
        _backColor = Color.Transparent;
        Size = new Size(200, 150);
        Text = "GroupBox";
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
    /// Renders the GroupBox with its border and title text.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
    public override void Render(Graphics g)
    {
        if (!Visible) return;

        var theme = ThemeManager.CurrentTheme;
        var borderColor = theme.ControlDark;
        var font = EffectiveFont;
        float zoom = EffectiveZoom;

        int titleHeight = (int)(font.Size * zoom);
        int titleWidth = Text.Length > 0 ? (int)(Text.Length * font.Size * zoom * 0.6f) + 10 : 0;
        int halfTitle = titleHeight / 2;

        g.FillRectangle(BackColor, 0, 0, Width, Height);

        base.Render(g);

        if (!string.IsNullOrEmpty(Text))
        {
            g.FillRectangle(BackColor, 4, 0, titleWidth, titleHeight);
            g.DrawString(Text, font, ForeColor, 6, 0);
        }

        g.DrawLine(borderColor, 2, halfTitle, 4, halfTitle, 1);
        g.DrawLine(borderColor, titleWidth + 4, halfTitle, Width - 1, halfTitle, 1);

        g.DrawLine(borderColor, 0, halfTitle, 0, Height - 1, 1);
        g.DrawLine(borderColor, 0, Height - 1, Width - 1, Height - 1, 1);
        g.DrawLine(borderColor, Width - 1, halfTitle, Width - 1, Height - 1, 1);
    }
}
