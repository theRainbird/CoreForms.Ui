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
    /// Called when the theme changes. Keeps the transparent background.
    /// </summary>
    /// <param name="newTheme">The new theme that was activated.</param>
    public override void OnThemeChanged(Theme newTheme)
    {
        Invalidate();
    }

    /// <summary>
    /// Gets the deepest child control at the specified point, accounting for the title offset.
    /// </summary>
    /// <param name="point">The point in this container's coordinate space.</param>
    /// <param name="localPoint">The resulting point in the deepest child's coordinate space.</param>
    /// <returns>The deepest child control, or null if none found.</returns>
    protected override Control? GetDeepestChildAtPoint(Point point, out Point localPoint)
    {
        var adjustedPoint = new Point(point.X, point.Y - ContentOffsetY);
        return base.GetDeepestChildAtPoint(adjustedPoint, out localPoint);
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

        int textX = 10;

        g.DrawLine(borderColor, 0, offsetY, 0, Height - 1, 1);
        g.DrawLine(borderColor, 0, Height - 1, Width - 1, Height - 1, 1);
        g.DrawLine(borderColor, Width - 1, offsetY, Width - 1, Height - 1, 1);

        if (!string.IsNullOrEmpty(Text))
        {
            (int textWidth, _) = g.MeasureString(Text, font, zoom);
            var textColor = Enabled ? ForeColor : theme.GrayText;
            g.DrawString(Text, font, textColor, textX, 0);

            int leftEnd = textX - 10;
            if (leftEnd > 0)
                g.DrawLine(borderColor, 0, offsetY, leftEnd, offsetY, 1);

            int rightStart = textX + textWidth + 10;
            if (rightStart < Width - 1)
                g.DrawLine(borderColor, rightStart, offsetY, Width - 1, offsetY, 1);
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
