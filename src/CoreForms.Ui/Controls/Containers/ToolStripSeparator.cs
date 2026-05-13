using CoreForms.Ui.Core;
using CoreForms.Ui.Theming;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Containers;

/// <summary>
/// Represents a separator (divider line) in a ToolStrip.
/// Provides visual grouping of related items.
/// </summary>
public class ToolStripSeparator : ToolStripItem
{
    private readonly int _separatorWidth = 6;

    /// <summary>
    /// Initializes a new instance of ToolStripSeparator.
    /// </summary>
    public ToolStripSeparator()
    {
        Enabled = false;
    }

    /// <summary>
    /// Renders the separator as a vertical line.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
    /// <param name="x">The x-coordinate of the item bounds.</param>
    /// <param name="y">The y-coordinate of the item bounds.</param>
    /// <param name="width">The width of the item bounds.</param>
    /// <param name="height">The height of the item bounds.</param>
    /// <param name="font">The font to use for text rendering.</param>
    /// <param name="zoom">The current zoom factor.</param>
    /// <param name="hovered">Whether the item is currently hovered.</param>
    /// <param name="pressed">Whether the item is currently pressed.</param>
    public override void OnPaint(Graphics g, int x, int y, int width, int height, Font font, float zoom, bool hovered, bool pressed)
    {
        if (!Visible) return;

        var theme = ThemeManager.CurrentTheme;
        int centerX = x + width / 2;
        int lineTop = y + 4;
        int lineBottom = y + height - 4;

        g.DrawLine(theme.ControlDark, centerX - 1, lineTop, centerX - 1, lineBottom);
        g.DrawLine(theme.ControlLight, centerX, lineTop, centerX, lineBottom);
    }

    /// <summary>
    /// Gets the preferred width for the separator.
    /// </summary>
    /// <param name="font">The font (unused for separators).</param>
    /// <param name="zoom">The current zoom factor.</param>
    /// <returns>The fixed separator width.</returns>
    public override int GetPreferredWidth(Font font, float zoom)
    {
        return _separatorWidth;
    }

    /// <summary>
    /// Separators cannot be clicked.
    /// </summary>
    protected override void OnClick()
    {
    }
}
