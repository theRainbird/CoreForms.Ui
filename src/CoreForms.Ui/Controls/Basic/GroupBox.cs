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
        _backColorSet = true;
        Size = new Size(200, 150);
        Text = LangRes.GetString("GroupBoxDefaultText");
    }

    /// <summary>
    /// Gets the vertical offset for child controls due to the title area.
    /// </summary>
    protected virtual int ContentOffsetY
    {
        get
        {
            var font = EffectiveFont;
            float zoom = EffectiveZoom;
            int titleHeight = (int)(font.Size * zoom);
            return titleHeight / 2;
        }
    }

    /// <summary>
    /// Reports the title offset so that <see cref="ContainerControl.GetDeepestChildAtPoint"/>
    /// correctly transforms child coordinates.
    /// </summary>
    protected internal override Point GetChildRenderOffset() => new Point(0, ContentOffsetY);

    /// <summary>
    /// Gets the clipping rectangle excluding the title area and border.
    /// </summary>
    protected override Rectangle GetChildClipRectangle()
    {
        int offsetY = ContentOffsetY;
        return new Rectangle(1, 0, Width - 2, Height - offsetY);
    }

    /// <summary>
    /// Called when the theme changes. Keeps the transparent background.
    /// </summary>
    /// <param name="newTheme">The new theme that was activated.</param>
    public override void OnThemeChanged(Theme newTheme)
    {
        if (!_backColorSet)
            _backColor = newTheme.ControlBackground;
        if (!_foreColorSet)
            _foreColor = newTheme.ControlText;
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
        int offsetY = ContentOffsetY;

        g.FillRectangle(BackColor, 0, 0, Width, Height);

        g.Save();
        g.TranslateTransform(0, offsetY);
        base.Render(g);
        g.Restore();

        g.DrawLine(borderColor, 0, offsetY, 0, Height - 1, 1);
        g.DrawLine(borderColor, 0, Height - 1, Width - 1, Height - 1, 1);
        g.DrawLine(borderColor, Width - 1, offsetY, Width - 1, Height - 1, 1);

        if (!string.IsNullOrEmpty(Text))
        {
            (int textWidth, _) = g.MeasureString(Text, font, zoom);
            var textColor = Enabled ? ForeColor : theme.GrayText;
            int textX = 12;
            int gap = 4;
            g.DrawString(Text, font, textColor, textX, 0);

            int leftTopEnd = textX - gap;
            if (leftTopEnd > 0)
                g.DrawLine(borderColor, 0, offsetY, leftTopEnd, offsetY, 1);

            int logicalTextWidth = zoom > 0 ? (int)MathF.Round(textWidth / zoom) : textWidth;
            int rightTopStart = textX + logicalTextWidth + gap;
            if (rightTopStart < Width - 1)
                g.DrawLine(borderColor, rightTopStart, offsetY, Width - 1, offsetY, 1);
        }
        else
        {
            g.DrawLine(borderColor, 0, offsetY, Width - 1, offsetY, 1);
        }
    }

    /// <summary>
    /// Renders the overlay with the same offset as Render to ensure child overlays
    /// (like ComboBox dropdowns) are positioned correctly.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
    public override void RenderOverlay(Graphics g)
    {
        if (!Visible) return;

        int offsetY = ContentOffsetY;

        g.Save();
        g.TranslateTransform(0, offsetY);
        base.RenderOverlay(g);
        g.Restore();
    }
}
