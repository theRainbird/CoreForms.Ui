using CoreForms.Ui.Core;

namespace CoreForms.Ui.Controls.Containers;

/// <summary>
/// Represents a menu item in a MenuStrip.
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
    /// <param name="text">The menu item text.</param>
    public ToolStripMenuItem(string text)
    {
        _text = text;
    }

    /// <summary>
    /// Gets or sets the text of the menu item.
    /// </summary>
    public string Text
    {
        get => _text;
        set => _text = value;
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
}