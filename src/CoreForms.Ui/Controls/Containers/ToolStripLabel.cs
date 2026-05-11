using CoreForms.Ui.Core;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Containers;

/// <summary>
/// Represents a non-interactive label in a ToolStrip.
/// Can optionally behave as a clickable link.
/// </summary>
public class ToolStripLabel : ToolStripItem
{
    private bool _isLink;
    private Color _linkColor = Color.FromArgb(0, 102, 204);
    private Color _visitedLinkColor = Color.FromArgb(128, 0, 128);
    private bool _visited;

    /// <summary>
    /// Initializes a new instance of ToolStripLabel.
    /// </summary>
    public ToolStripLabel() { }

    /// <summary>
    /// Initializes a new instance of ToolStripLabel with the specified text.
    /// </summary>
    /// <param name="text">The label text.</param>
    public ToolStripLabel(string text)
    {
        Text = text;
    }

    /// <summary>
    /// Gets or sets whether the label behaves as a clickable link.
    /// </summary>
    public bool IsLink
    {
        get => _isLink;
        set
        {
            if (_isLink != value)
            {
                _isLink = value;
                Owner?.Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets the color of the link text.
    /// </summary>
    public Color LinkColor
    {
        get => _linkColor;
        set
        {
            _linkColor = value;
            Owner?.Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the color of the visited link text.
    /// </summary>
    public Color VisitedLinkColor
    {
        get => _visitedLinkColor;
        set
        {
            _visitedLinkColor = value;
            Owner?.Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets whether the link has been visited.
    /// </summary>
    public bool Visited
    {
        get => _visited;
        set
        {
            if (_visited != value)
            {
                _visited = value;
                Owner?.Invalidate();
            }
        }
    }

    /// <summary>
    /// Raises the Click event and marks the link as visited if IsLink is enabled.
    /// </summary>
    protected override void OnClick()
    {
        if (_isLink)
        {
            _visited = true;
        }
        base.OnClick();
    }

    /// <summary>
    /// Renders the label text, optionally styled as a link.
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

        Color textColor;
        if (_isLink)
        {
            textColor = _visited ? _visitedLinkColor : _linkColor;
        }
        else
        {
            textColor = Enabled ? Color.Black : SystemColors.GrayText;
        }

        if (hovered && _isLink)
        {
            g.FillRectangle(Color.FromArgb(30, 0, 0, 0), x, y, width, height);
        }

        string displayText = DisplayText;
        int textX = x + Padding.Left + 4;
        int textY = y + (height - (int)font.Size) / 2;

        g.DrawString(displayText, font, textColor, textX, textY);

        if (_isLink)
        {
            int textWidth = MeasureTextWidth(displayText, font, zoom);
            int underlineY = textY + (int)(font.Size * zoom);
            g.DrawLine(textColor, textX, underlineY, textX + textWidth, underlineY);
        }
    }

    /// <summary>
    /// Calculates the preferred width for layout.
    /// </summary>
    /// <param name="font">The font to use for text measurement.</param>
    /// <param name="zoom">The current zoom factor.</param>
    /// <returns>The preferred width in pixels.</returns>
    public override int GetPreferredWidth(Font font, float zoom)
    {
        return MeasureTextWidth(DisplayText, font, zoom) + Padding.Horizontal + 4;
    }
}
