using CoreForms.Ui.Core;

namespace CoreForms.Ui.Controls.Containers;

/// <summary>
/// Represents a menu item in a MenuStrip.
/// Supports mnemonics via the ampersand prefix (e.g., "&amp;File" shows as <u>F</u>ile and activates with Alt+F).
/// </summary>
public class ToolStripMenuItem : Component
{
    private string _text = string.Empty;
    private bool _isSelected;
    private bool _isDropDownVisible;
    private readonly List<ToolStripMenuItem> _dropDownItems = new();
    private int _width;

    /// <summary>
    /// Initializes a new instance of ToolStripMenuItem.
    /// </summary>
    /// <param name="text">The menu item text, optionally containing an ampersand mnemonic prefix.</param>
    public ToolStripMenuItem(string text)
    {
        _text = text;
    }

    /// <summary>
    /// Gets or sets the text of the menu item.
    /// The ampersand character (&amp;) marks the mnemonic key.
    /// </summary>
    public string Text
    {
        get => _text;
        set => _text = value;
    }

    /// <summary>
    /// Gets the display text with the mnemonic ampersand removed.
    /// </summary>
    public string DisplayText => StripMnemonic(_text);

    /// <summary>
    /// Gets the mnemonic key character, or null if none is specified.
    /// The mnemonic is the character following the first ampersand in the text.
    /// </summary>
    public char? Mnemonic => GetMnemonicChar(_text);

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
    /// Occurs when the menu item is clicked.
    /// </summary>
    public event EventHandler? Click;

    /// <summary>
    /// Raises the Click event.
    /// </summary>
    public void OnClick()
    {
        Click?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Returns the index of the mnemonic character in the display text.
    /// </summary>
    internal int MnemonicIndex => GetMnemonicIndex(_text);

    private static string StripMnemonic(string text)
    {
        if (text == null) return string.Empty;
        int idx = text.IndexOf('&');
        if (idx >= 0 && idx < text.Length - 1)
            return text.Substring(0, idx) + text.Substring(idx + 1);
        if (idx >= 0 && idx == text.Length - 1)
            return text.Substring(0, idx);
        return text;
    }

    private static char? GetMnemonicChar(string text)
    {
        if (text == null) return null;
        int idx = text.IndexOf('&');
        if (idx >= 0 && idx < text.Length - 1)
            return char.ToUpperInvariant(text[idx + 1]);
        return null;
    }

    private static int GetMnemonicIndex(string text)
    {
        if (text == null) return -1;
        int idx = text.IndexOf('&');
        if (idx >= 0 && idx < text.Length - 1)
            return idx;
        return -1;
    }
}