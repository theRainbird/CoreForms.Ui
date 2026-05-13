using CoreForms.Ui.Core;
using CoreForms.Ui.Theming;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Containers;

/// <summary>
/// Represents a menu item in a MenuStrip.
/// Supports mnemonics via the ampersand prefix (e.g., "&amp;File" shows as <u>F</u>ile and activates with Alt+F).
/// </summary>
public class ToolStripMenuItem : ToolStripItem
{
    private bool _isSelected;
    private bool _isDropDownVisible;
    private readonly List<ToolStripMenuItem> _dropDownItems = new();

    /// <summary>
    /// Initializes a new instance of ToolStripMenuItem.
    /// </summary>
    /// <param name="text">The menu item text, optionally containing an ampersand mnemonic prefix.</param>
    public ToolStripMenuItem(string text)
    {
        Text = text;
    }

    /// <summary>
    /// Gets or sets whether the menu item is selected.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => _isSelected = value;
    }

    /// <summary>
    /// Gets or sets whether the dropdown is visible.
    /// </summary>
    public bool IsDropDownVisible
    {
        get => _isDropDownVisible;
        set => _isDropDownVisible = value;
    }

    /// <summary>
    /// Gets the collection of dropdown items.
    /// </summary>
    public List<ToolStripMenuItem> DropDownItems => _dropDownItems;

    /// <summary>
    /// Renders the menu item with mnemonic support.
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
        var textColor = Enabled ? theme.ToolStripItemText : theme.GrayText;
        string displayText = DisplayText;
        int mnemonicIdx = MnemonicIndex;

        g.DrawString(displayText, font, textColor, x + 5, y + (height - (int)font.Size) / 2);

        if (mnemonicIdx >= 0 && Owner != null)
        {
            float scaledFontSize = font.Size * zoom;
            int charWidth = (int)(scaledFontSize / 2);
            int underlineX = x + 5 + mnemonicIdx * charWidth;
            int underlineY = y + (int)scaledFontSize + (height - (int)font.Size) / 2;
            g.DrawLine(textColor, underlineX, underlineY, underlineX + charWidth, underlineY);
        }
    }

    /// <summary>
    /// Calculates the preferred width for menu item layout.
    /// </summary>
    /// <param name="font">The font to use for text measurement.</param>
    /// <param name="zoom">The current zoom factor.</param>
    /// <returns>The preferred width in pixels.</returns>
    public override int GetPreferredWidth(Font font, float zoom)
    {
        float scaledFontSize = font.Size * zoom;
        return (DisplayText.Length) * (int)(scaledFontSize / 2) + 6;
    }
}
