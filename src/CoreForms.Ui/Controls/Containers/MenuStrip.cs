using CoreForms.Ui.Core;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Containers;

/// <summary>
/// A menu strip that displays a menu bar with menu items and supports
/// click detection, dropdown menus, keyboard navigation, and Alt+mnemonic activation.
/// </summary>
public class MenuStrip : ContainerControl
{
    private readonly List<ToolStripMenuItem> _items = new();
    private ToolStripMenuItem? _hoverItem;
    private ToolStripMenuItem? _hoverDropDownItem;
    private bool _dropDownVisible;
    private ToolStripMenuItem? _openItem;
    private int _selectedDropDownIndex = -1;
    private bool _menuMode;

    /// <summary>
    /// Initializes a new instance of MenuStrip.
    /// </summary>
    public MenuStrip()
    {
        Size = new Size(400, 24);
        BackColor = SystemColors.Control;
        TabStop = true;
    }

    /// <summary>
    /// Gets the collection of menu items.
    /// </summary>
    public List<ToolStripMenuItem> Items => _items;

    /// <summary>
    /// Gets or sets whether the menu strip is in menu mode (activated by pressing Alt).
    /// </summary>
    public bool MenuMode
    {
        get => _menuMode;
        set => _menuMode = value;
    }

    /// <summary>
    /// Gets the height of dropdown items.
    /// </summary>
    protected virtual int DropDownItemHeight => 22;

    /// <summary>
    /// Renders the menu strip with its items and hover highlighting.
    /// Mnemonic characters are underlined when in menu mode.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
    public override void Render(Graphics g)
    {
        if (!Visible) return;

        g.FillRectangle(BackColor, 0, 0, Width, Height);
        g.DrawLine(Color.FromArgb(180, 180, 180), 0, Height - 1, Width, Height - 1);

        var font = Font ?? Font.Default;
        int x = 4;

        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            int textWidth = GetItemWidth(item, font);
            bool isHovered = item == _hoverItem || item == _openItem;

            if (isHovered)
            {
                g.FillRectangle(Color.FromArgb(200, 200, 200), x, 0, textWidth, Height);
            }

            RenderItemText(g, item, font, x + 5, (Height - (int)font.Size) / 2, _menuMode || isHovered || _dropDownVisible);
            x += textWidth;
        }

        base.Render(g);
    }

    /// <summary>
    /// Renders the dropdown overlay on top of other controls.
    /// Mnemonic characters are underlined when in menu mode.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
    public override void RenderOverlay(Graphics g)
    {
        if (!Visible) return;

        base.RenderOverlay(g);

        if (_dropDownVisible && _openItem != null && _openItem.DropDownItems.Count > 0)
        {
            var font = Font ?? Font.Default;
            int x = GetItemX(GetItemIndex(_openItem));
            int y = Height;

            int maxWidth = GetDropDownWidth(_openItem, font);
            int dropDownHeight = _openItem.DropDownItems.Count * DropDownItemHeight + 4;

            g.FillRectangle(Color.White, x, y, maxWidth, dropDownHeight);
            g.DrawRectangle(Color.FromArgb(100, 100, 100), x, y, maxWidth, dropDownHeight, 1);

            int itemY = y + 2;
            for (int i = 0; i < _openItem.DropDownItems.Count; i++)
            {
                var ddItem = _openItem.DropDownItems[i];
                bool isHovered = ddItem == _hoverDropDownItem || i == _selectedDropDownIndex;
                if (isHovered)
                {
                    g.FillRectangle(Color.FromArgb(200, 220, 255), x + 1, itemY, maxWidth - 2, DropDownItemHeight);
                }
                RenderItemText(g, ddItem, font, x + 8, itemY + (DropDownItemHeight - (int)font.Size) / 2, true);
                itemY += DropDownItemHeight;
            }
        }
    }

    /// <summary>
    /// Handles mouse down events to detect menu item clicks and dropdown toggling.
    /// Captures the mouse when a dropdown is open to receive clicks in the dropdown area.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseDown(EventArgs e)
    {
        if (e is MouseEventArgs args)
        {
            CapturingMouse = true;

            if (_dropDownVisible && _openItem != null)
            {
                var ddResult = HitTestDropDown(args.X, args.Y);
                if (ddResult != null)
                {
                    ddResult.OnClick();
                    CloseDropDown();
                    return;
                }

                var topItem = HitTestTopItem(args.X, args.Y);
                if (topItem != null && topItem.DropDownItems.Count > 0)
                {
                    OpenDropDown(topItem);
                    return;
                }

                CloseDropDown();
                return;
            }

            var item = HitTestTopItem(args.X, args.Y);
            if (item != null)
            {
                if (item.DropDownItems.Count > 0)
                {
                    OpenDropDown(item);
                }
                else
                {
                    item.OnClick();
                }
            }
            else
            {
                CapturingMouse = false;
            }
        }

        base.OnMouseDown(e);
    }

    /// <summary>
    /// Handles mouse move events for hover highlighting and dropdown navigation.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseMove(EventArgs e)
    {
        if (e is MouseEventArgs args)
        {
            _hoverItem = HitTestTopItem(args.X, args.Y);

            if (_dropDownVisible && _openItem != null)
            {
                if (args.Y >= Height)
                {
                    _hoverDropDownItem = HitTestDropDown(args.X, args.Y);
                    _selectedDropDownIndex = _hoverDropDownItem != null
                        ? _openItem.DropDownItems.IndexOf(_hoverDropDownItem)
                        : -1;
                }
                else
                {
                    _hoverDropDownItem = null;
                    _selectedDropDownIndex = -1;
                    var topItem = HitTestTopItem(args.X, args.Y);
                    if (topItem != null && topItem.DropDownItems.Count > 0 && topItem != _openItem)
                    {
                        OpenDropDown(topItem);
                    }
                }
            }
        }

        base.OnMouseMove(e);
    }

    /// <summary>
    /// Handles mouse leave events to clear hover state.
    /// </summary>
    protected override void OnMouseLeave(EventArgs e)
    {
        _hoverItem = null;
        _hoverDropDownItem = null;
        base.OnMouseLeave(e);
    }

    /// <summary>
    /// Handles key events for keyboard navigation of menus.
    /// Escape closes the dropdown. Arrow keys navigate dropdown items.
    /// Enter selects the current dropdown item. Alt+letter activates mnemonics.
    /// Alt alone toggles menu mode.
    /// </summary>
    /// <param name="e">A KeyEventArgs that contains the event data.</param>
    protected internal override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Menu || e.Modifiers == ModifierKeys.Alt)
        {
            if (!_menuMode)
            {
                _menuMode = true;
                e.Handled = true;
                return;
            }

            if (_dropDownVisible)
            {
                CloseDropDown();
                _menuMode = false;
                e.Handled = true;
                return;
            }
        }

        if (_menuMode && e.Modifiers == ModifierKeys.Alt && e.KeyCode != Keys.Menu)
        {
            char key = (char)e.KeyCode;
            var match = FindMnemonicItem(key);
            if (match != null)
            {
                if (match.DropDownItems.Count > 0)
                    OpenDropDown(match);
                else
                    match.OnClick();
                e.Handled = true;
                return;
            }
        }

        if (_menuMode && e.Modifiers == ModifierKeys.None)
        {
            char key = (char)e.KeyCode;
            var match = FindMnemonicItem(key);
            if (match != null)
            {
                if (match.DropDownItems.Count > 0)
                    OpenDropDown(match);
                else
                {
                    match.OnClick();
                    CloseDropDown();
                    _menuMode = false;
                }
                e.Handled = true;
                return;
            }
        }

        if (_dropDownVisible && _openItem != null)
        {
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    CloseDropDown();
                    _menuMode = false;
                    e.Handled = true;
                    return;

                case Keys.Down:
                    if (_selectedDropDownIndex < 0)
                        _selectedDropDownIndex = 0;
                    else if (_selectedDropDownIndex < _openItem.DropDownItems.Count - 1)
                        _selectedDropDownIndex++;
                    _hoverDropDownItem = _selectedDropDownIndex >= 0
                        ? _openItem.DropDownItems[_selectedDropDownIndex]
                        : null;
                    e.Handled = true;
                    return;

                case Keys.Up:
                    if (_selectedDropDownIndex < 0)
                        _selectedDropDownIndex = _openItem.DropDownItems.Count - 1;
                    else if (_selectedDropDownIndex > 0)
                        _selectedDropDownIndex--;
                    _hoverDropDownItem = _selectedDropDownIndex >= 0
                        ? _openItem.DropDownItems[_selectedDropDownIndex]
                        : null;
                    e.Handled = true;
                    return;

                case Keys.Enter:
                    if (_selectedDropDownIndex >= 0 && _selectedDropDownIndex < _openItem.DropDownItems.Count)
                    {
                        _openItem.DropDownItems[_selectedDropDownIndex].OnClick();
                        CloseDropDown();
                        _menuMode = false;
                        e.Handled = true;
                        return;
                    }
                    break;

                case Keys.Left:
                    {
                        int idx = GetItemIndex(_openItem);
                        if (idx > 0)
                            OpenDropDown(_items[idx - 1]);
                        e.Handled = true;
                        return;
                    }

                case Keys.Right:
                    {
                        int idx = GetItemIndex(_openItem);
                        if (idx < _items.Count - 1)
                            OpenDropDown(_items[idx + 1]);
                        e.Handled = true;
                        return;
                    }
            }
        }

        base.OnKeyDown(e);
    }

    /// <summary>
    /// Processes a mnemonic key press, activating the matching menu item if found.
    /// </summary>
    /// <param name="charCode">The character code of the pressed key.</param>
    /// <returns>True if the mnemonic was processed; otherwise, false.</returns>
    public override bool ProcessMnemonic(char charCode)
    {
        var match = FindMnemonicItem(char.ToUpperInvariant(charCode));
        if (match != null)
        {
            if (match.DropDownItems.Count > 0)
                OpenDropDown(match);
            else
            {
                match.OnClick();
                CloseDropDown();
            }
            return true;
        }
        return false;
    }

    private ToolStripMenuItem? FindMnemonicItem(char key)
    {
        char upper = char.ToUpperInvariant(key);
        foreach (var item in _items)
        {
            if (item.Mnemonic == upper)
                return item;
        }
        return null;
    }

    private void RenderItemText(Graphics g, ToolStripMenuItem item, Font font, int x, int y, bool showMnemonic)
    {
        string displayText = item.DisplayText;
        int mnemonicIdx = item.MnemonicIndex;

        g.DrawString(displayText, font, ForeColor, x, y);

        if (mnemonicIdx >= 0 && showMnemonic)
        {
            int charWidth = (int)font.Size / 2;
            int underlineX = x + mnemonicIdx * charWidth;
            int underlineY = y + (int)font.Size;
            g.DrawLine(ForeColor, underlineX, underlineY, underlineX + charWidth, underlineY);
        }
    }

    private int GetItemIndex(ToolStripMenuItem item)
    {
        return _items.IndexOf(item);
    }

    private int GetItemX(int index)
    {
        var font = Font ?? Font.Default;
        int x = 4;
        for (int i = 0; i < index && i < _items.Count; i++)
        {
            x += GetItemWidth(_items[i], font);
        }
        return x;
    }

    private int GetItemWidth(ToolStripMenuItem item, Font font)
    {
        return (item.DisplayText.Length + 2) * (int)font.Size / 2 + 10;
    }

    private int GetDropDownWidth(ToolStripMenuItem item, Font font)
    {
        int maxWidth = 0;
        foreach (var ddItem in item.DropDownItems)
        {
            int w = ddItem.DisplayText.Length * (int)font.Size / 2 + 30;
            if (w > maxWidth) maxWidth = w;
        }
        return maxWidth;
    }

    private ToolStripMenuItem? HitTestTopItem(int x, int y)
    {
        if (y < 0 || y > Height)
            return null;

        var font = Font ?? Font.Default;
        int itemX = 4;

        for (int i = 0; i < _items.Count; i++)
        {
            int itemWidth = GetItemWidth(_items[i], font);
            if (x >= itemX && x < itemX + itemWidth)
            {
                return _items[i];
            }
            itemX += itemWidth;
        }

        return null;
    }

    private ToolStripMenuItem? HitTestDropDown(int x, int y)
    {
        if (_openItem == null || _openItem.DropDownItems.Count == 0)
            return null;

        var font = Font ?? Font.Default;
        int ddX = GetItemX(GetItemIndex(_openItem));
        int ddY = Height;

        int maxWidth = GetDropDownWidth(_openItem, font);
        int dropDownHeight = _openItem.DropDownItems.Count * DropDownItemHeight + 4;

        if (x < ddX || x > ddX + maxWidth || y < ddY || y > ddY + dropDownHeight)
            return null;

        int itemIndex = (y - ddY - 2) / DropDownItemHeight;
        if (itemIndex >= 0 && itemIndex < _openItem.DropDownItems.Count)
        {
            return _openItem.DropDownItems[itemIndex];
        }

        return null;
    }

    private void OpenDropDown(ToolStripMenuItem item)
    {
        _openItem = item;
        _dropDownVisible = true;
        _selectedDropDownIndex = -1;
        _hoverDropDownItem = null;
        CapturingMouse = true;
    }

    private void CloseDropDown()
    {
        _dropDownVisible = false;
        _openItem = null;
        _hoverDropDownItem = null;
        _selectedDropDownIndex = -1;
        CapturingMouse = false;
    }
}