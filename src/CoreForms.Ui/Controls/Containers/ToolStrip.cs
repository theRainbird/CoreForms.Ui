using CoreForms.Ui.Core;
using CoreForms.Ui.Theming;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Containers;

/// <summary>
/// Specifies the style of the grip handle on a ToolStrip.
/// </summary>
public enum ToolStripGripStyle
{
    /// <summary>
    /// The grip handle is visible.
    /// </summary>
    Visible,

    /// <summary>
    /// The grip handle is hidden.
    /// </summary>
    Hidden
}

/// <summary>
/// A toolbar that displays ToolStripItem controls such as buttons, labels, text boxes, and separators.
/// Supports overflow behavior when items exceed the available width, grip handle for repositioning,
/// and dropdown menus on compatible items.
/// </summary>
public class ToolStrip : ContainerControl
{
    private readonly ToolStripItemCollection _items;
    private ToolStripItem? _hoveredItem;
    private ToolStripItem? _openDropDownItem;
    private bool _dropDownVisible;
    private int _selectedDropDownIndex = -1;
    private ToolStripGripStyle _gripStyle = ToolStripGripStyle.Visible;
    private readonly int _gripWidth = 12;
    private readonly int _overflowButtonWidth = 24;
    private bool _overflowActive;
    private readonly List<ToolStripItem> _overflowItems = new();

    /// <summary>
    /// Initializes a new instance of ToolStrip.
    /// </summary>
    public ToolStrip()
    {
        Size = new Size(400, 28);
        _backColor = ThemeManager.CurrentTheme.ControlBackground;
        TabStop = true;
        Dock = DockStyle.Top;
        _items = new ToolStripItemCollection(this);
    }

    /// <summary>
    /// Called when the theme changes. Updates toolstrip-specific colors.
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
    /// Gets the collection of items in the ToolStrip.
    /// </summary>
    public ToolStripItemCollection Items => _items;

    /// <summary>
    /// Gets or sets the grip style.
    /// </summary>
    public ToolStripGripStyle GripStyle
    {
        get => _gripStyle;
        set
        {
            if (_gripStyle != value)
            {
                _gripStyle = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets the height of items in the ToolStrip.
    /// </summary>
    protected virtual int ItemHeight => CoordinateTransform.GetItemHeight(Font ?? Font.Default, EffectiveZoom);

    /// <summary>
    /// Renders the ToolStrip with its grip, items, and overflow button.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
    public override void Render(Graphics g)
    {
        if (!Visible) return;

        var theme = ThemeManager.CurrentTheme;

        g.FillRectangle(BackColor, 0, 0, Width, Height);
        g.DrawLine(theme.MenuSeparator, 0, Height - 1, Width, Height - 1);

        int xOffset = 0;

        if (_gripStyle == ToolStripGripStyle.Visible)
        {
            RenderGrip(g, 2, 0, _gripWidth - 4, Height);
            xOffset = _gripWidth;
        }

        var font = Font ?? Font.Default;
        float zoom = EffectiveZoom;
        CalculateOverflow();

        var visibleItems = _overflowActive ? _items.Take(_items.Count - _overflowItems.Count).ToList() : _items.ToList();

        foreach (var item in visibleItems)
        {
            if (!item.Visible) continue;

            int itemWidth = item.GetPreferredWidth(font, zoom) + 2;
            bool isHovered = item == _hoveredItem;
            bool isPressed = item.IsPressed;

            if (isHovered || isPressed)
            {
                g.FillRectangle(theme.MenuHover, xOffset, 2, itemWidth, Height - 4);
            }

            item.OnPaint(g, xOffset + 4, 2, itemWidth - 8, Height - 4, font, zoom, isHovered, isPressed);
            item.Owner = this;
            xOffset += itemWidth;
        }

        if (_overflowActive)
        {
            RenderOverflowButton(g, Width - _overflowButtonWidth, 0, _overflowButtonWidth, Height);
        }

        base.Render(g);
    }

    /// <summary>
    /// Renders the dropdown overlay for items with dropdown items.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
    public override void RenderOverlay(Graphics g)
    {
        if (!Visible) return;

        base.RenderOverlay(g);

        if (_dropDownVisible && _openDropDownItem is ToolStripButton tsButton && tsButton.DropDownItems.Count > 0)
        {
            RenderDropDownOverlay(g, tsButton, tsButton.DropDownItems.Cast<ToolStripItem>().ToList());
        }
    }

    private void RenderDropDownOverlay(Graphics g, ToolStripItem ownerItem, List<ToolStripItem> dropDownItems)
    {
        var font = Font ?? Font.Default;
        float zoom = EffectiveZoom;
        int itemHeight = ItemHeight;

        int maxWidth = 0;
        foreach (var item in dropDownItems)
        {
            int w = item.GetPreferredWidth(font, zoom) + 20;
            if (w > maxWidth) maxWidth = w;
        }
        maxWidth = Math.Max(maxWidth, 120);

        int ddX = 4;
        var visibleItems = _overflowActive ? _items.Take(_items.Count - _overflowItems.Count).ToList() : _items.ToList();
        foreach (var item in visibleItems)
        {
            if (item == ownerItem) break;
            ddX += item.GetPreferredWidth(font, zoom) + 2;
        }

        int ddY = Height;
        int ddHeight = dropDownItems.Count * itemHeight + 4;

        var theme = ThemeManager.CurrentTheme;

        g.FillRectangle(theme.MenuDropdownBackground, ddX, ddY, maxWidth, ddHeight);
        g.DrawRectangle(theme.MenuDropdownBorder, ddX, ddY, maxWidth, ddHeight, 1);

        int itemY = ddY + 2;
        for (int i = 0; i < dropDownItems.Count; i++)
        {
            var ddItem = dropDownItems[i];
            bool isHovered = ddItem.IsHovered || i == _selectedDropDownIndex;

            if (isHovered)
            {
                g.FillRectangle(theme.MenuDropdownHover, ddX + 1, itemY, maxWidth - 2, itemHeight);
            }

            ddItem.OnPaint(g, ddX + 6, itemY, maxWidth - 12, itemHeight, font, zoom, isHovered, false);
            itemY += itemHeight;
        }
    }

    /// <summary>
    /// Handles mouse down events for item selection and dropdown toggling.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseDown(EventArgs e)
    {
        if (e is MouseEventArgs args)
        {
            CapturingMouse = true;

            if (_dropDownVisible && _openDropDownItem is ToolStripButton tsButton)
            {
                var ddResult = HitTestDropDown(args.X, args.Y, tsButton);
                if (ddResult != null)
                {
                    ddResult.PerformClick();
                    CloseDropDown();
                    return;
                }

                var topItem = HitTestTopItem(args.X, args.Y);
                if (topItem == _openDropDownItem)
                {
                    CloseDropDown();
                    return;
                }
            }

            if (_overflowActive && args.X >= Width - _overflowButtonWidth)
            {
                ToggleOverflowDropDown();
                return;
            }

            var item = HitTestTopItem(args.X, args.Y);
            if (item != null)
            {
                if (item is ToolStripTextBox textBox)
                {
                    var font = Font ?? Font.Default;
                    float zoom = EffectiveZoom;
                    int startX = _gripStyle == ToolStripGripStyle.Visible ? _gripWidth : 0;
                    int itemX = startX;
                    foreach (var it in _items)
                    {
                        if (it == item) break;
                        if (!it.Visible) continue;
                        itemX += it.GetPreferredWidth(font, zoom) + 2;
                    }
                    textBox.HandleMouseDown(args.X, args.Y, itemX, 0);
                    return;
                }

                item.IsPressed = true;

                if (item is ToolStripButton tsb && tsb.DropDownItems.Count > 0)
                {
                    OpenDropDown(tsb);
                }
                else
                {
                    item.PerformClick();
                    if (item is ToolStripButton tb)
                    {
                        tb.IsPressed = false;
                    }
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
    /// Handles mouse up events.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseUp(EventArgs e)
    {
        if (e is MouseEventArgs args)
        {
            foreach (var item in _items)
            {
                item.IsPressed = false;
            }
        }
        base.OnMouseUp(e);
    }

    /// <summary>
    /// Handles mouse move events for hover highlighting and dropdown navigation.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseMove(EventArgs e)
    {
        if (e is MouseEventArgs args)
        {
            _hoveredItem = HitTestTopItem(args.X, args.Y);

            if (_dropDownVisible && _openDropDownItem is ToolStripButton tsButton)
            {
                if (args.Y >= Height)
                {
                    var ddItem = HitTestDropDown(args.X, args.Y, tsButton);
                    foreach (var item in tsButton.DropDownItems)
                    {
                        item.IsHovered = item == ddItem;
                    }
                    _selectedDropDownIndex = ddItem != null ? tsButton.DropDownItems.IndexOf(ddItem) : -1;
                }
                else
                {
                    foreach (var item in tsButton.DropDownItems)
                    {
                        item.IsHovered = false;
                    }
                    _selectedDropDownIndex = -1;

                    var topItem = HitTestTopItem(args.X, args.Y);
                    if (topItem is ToolStripButton newTsb && newTsb.DropDownItems.Count > 0 && newTsb != _openDropDownItem)
                    {
                        OpenDropDown(newTsb);
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
        _hoveredItem = null;
        foreach (var item in _items)
        {
            item.IsHovered = false;
        }
        base.OnMouseLeave(e);
    }

    /// <summary>
    /// Handles key events for keyboard navigation.
    /// </summary>
    /// <param name="e">A KeyEventArgs that contains the event data.</param>
    protected internal override void OnKeyDown(KeyEventArgs e)
    {
        var focusedTextBox = _items.OfType<ToolStripTextBox>().FirstOrDefault(tb => tb.Focused);
        if (focusedTextBox != null && !e.Handled)
        {
            if (focusedTextBox.HandleKeyDown(e))
            {
                return;
            }
        }

        if (_dropDownVisible && _openDropDownItem is ToolStripButton tsButton)
        {
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    CloseDropDown();
                    e.Handled = true;
                    return;

                case Keys.Down:
                    if (_selectedDropDownIndex < 0)
                        _selectedDropDownIndex = 0;
                    else if (_selectedDropDownIndex < tsButton.DropDownItems.Count - 1)
                        _selectedDropDownIndex++;
                    UpdateDropDownHover(tsButton);
                    e.Handled = true;
                    return;

                case Keys.Up:
                    if (_selectedDropDownIndex < 0)
                        _selectedDropDownIndex = tsButton.DropDownItems.Count - 1;
                    else if (_selectedDropDownIndex > 0)
                        _selectedDropDownIndex--;
                    UpdateDropDownHover(tsButton);
                    e.Handled = true;
                    return;

                case Keys.Enter:
                    if (_selectedDropDownIndex >= 0 && _selectedDropDownIndex < tsButton.DropDownItems.Count)
                    {
                        tsButton.DropDownItems[_selectedDropDownIndex].PerformClick();
                        CloseDropDown();
                        e.Handled = true;
                        return;
                    }
                    break;

                case Keys.Left:
                    {
                        int idx = GetItemIndex(_openDropDownItem);
                        if (idx > 0)
                            SelectItem(_items[idx - 1]);
                        else if (_items.Count > 0)
                            SelectItem(_items[_items.Count - 1]);
                        e.Handled = true;
                        return;
                    }

                case Keys.Right:
                    {
                        int idx = GetItemIndex(_openDropDownItem);
                        if (idx < _items.Count - 1)
                            SelectItem(_items[idx + 1]);
                        else if (_items.Count > 0)
                            SelectItem(_items[0]);
                        e.Handled = true;
                        return;
                    }
            }
        }
        else
        {
            switch (e.KeyCode)
            {
                case Keys.Left:
                    NavigateItems(-1);
                    e.Handled = true;
                    return;

                case Keys.Right:
                    NavigateItems(1);
                    e.Handled = true;
                    return;

                case Keys.Enter:
                case Keys.Space:
                    if (_hoveredItem != null && focusedTextBox == null)
                    {
                        if (_hoveredItem is ToolStripButton tsb && tsb.DropDownItems.Count > 0)
                            OpenDropDown(tsb);
                        else
                            _hoveredItem.PerformClick();
                        e.Handled = true;
                        return;
                    }
                    break;

                case Keys.Down:
                    if (_hoveredItem is ToolStripButton tsb2 && tsb2.DropDownItems.Count > 0)
                    {
                        OpenDropDown(tsb2);
                        e.Handled = true;
                        return;
                    }
                    break;
            }
        }

        base.OnKeyDown(e);
    }

    /// <summary>
    /// Raises the TextInput event, routing to the focused ToolStripTextBox.
    /// </summary>
    /// <param name="text">The input text.</param>
    protected internal override void OnTextInput(string text)
    {
        var focusedTextBox = _items.OfType<ToolStripTextBox>().FirstOrDefault(tb => tb.Focused);
        if (focusedTextBox != null)
        {
            focusedTextBox.HandleTextInput(text);
            return;
        }
        base.OnTextInput(text);
    }

    private void UpdateDropDownHover(ToolStripButton tsButton)
    {
        for (int i = 0; i < tsButton.DropDownItems.Count; i++)
        {
            tsButton.DropDownItems[i].IsHovered = i == _selectedDropDownIndex;
        }
    }

    private void NavigateItems(int direction)
    {
        var visibleItems = _items.Where(i => i.Visible).ToList();
        if (visibleItems.Count == 0) return;

        int currentIndex = _hoveredItem != null ? visibleItems.IndexOf(_hoveredItem) : -1;
        int nextIndex = currentIndex + direction;

        if (nextIndex < 0) nextIndex = visibleItems.Count - 1;
        if (nextIndex >= visibleItems.Count) nextIndex = 0;

        _hoveredItem = visibleItems[nextIndex];
        Invalidate();
    }

    private void SelectItem(ToolStripItem item)
    {
        if (item is ToolStripButton tsb && tsb.DropDownItems.Count > 0)
            OpenDropDown(tsb);
        else
        {
            _hoveredItem = item;
            Invalidate();
        }
    }

    private void RenderGrip(Graphics g, int x, int y, int width, int height)
    {
        var theme = ThemeManager.CurrentTheme;
        var gripColor = theme.GrayText;
        int dotSize = 2;
        int gap = 4;
        int gripDotHeight = 14;
        int startY = y + (height - gripDotHeight) / 2;

        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < 2; col++)
            {
                int dotX = x + col * gap + 1;
                int dotY = startY + row * gap;
                g.FillRectangle(gripColor, dotX, dotY, dotSize, dotSize);
            }
        }
    }

    private void RenderOverflowButton(Graphics g, int x, int y, int width, int height)
    {
        var theme = ThemeManager.CurrentTheme;
        bool isHovered = _hoveredItem == null && false;
        if (isHovered)
        {
            g.FillRectangle(theme.MenuHover, x, y, width, height);
        }

        var font = Font ?? Font.Default;
        g.DrawString(">>", font, theme.ToolStripItemText, x + 3, y + (height - (int)font.Size) / 2);
    }

    private void CalculateOverflow()
    {
        _overflowItems.Clear();
        _overflowActive = false;

        var font = Font ?? Font.Default;
        float zoom = EffectiveZoom;
        int startX = _gripStyle == ToolStripGripStyle.Visible ? _gripWidth : 0;
        int availableWidth = Width - startX - (_overflowActive ? _overflowButtonWidth : 0);
        int currentX = 0;

        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            if (!item.Visible) continue;

            int itemWidth = item.GetPreferredWidth(font, zoom) + 2;
            if (currentX + itemWidth > availableWidth)
            {
                _overflowActive = true;
                for (int j = i; j < _items.Count; j++)
                {
                    if (_items[j].Visible)
                        _overflowItems.Add(_items[j]);
                }
                break;
            }
            currentX += itemWidth;
        }
    }

    private void ToggleOverflowDropDown()
    {
        if (_overflowItems.Count == 0) return;

        if (_dropDownVisible)
        {
            CloseDropDown();
        }
        else
        {
            _dropDownVisible = true;
            _selectedDropDownIndex = -1;
            CapturingMouse = true;
            Invalidate();
        }
    }

    private void OpenDropDown(ToolStripButton item)
    {
        _openDropDownItem = item;
        _dropDownVisible = true;
        _selectedDropDownIndex = -1;
        CapturingMouse = true;
        foreach (var ddItem in item.DropDownItems)
        {
            ddItem.IsHovered = false;
        }
        Invalidate();
    }

    private void CloseDropDown()
    {
        _dropDownVisible = false;
        if (_openDropDownItem is ToolStripButton tsb)
        {
            foreach (var ddItem in tsb.DropDownItems)
            {
                ddItem.IsHovered = false;
            }
        }
        _openDropDownItem = null;
        _selectedDropDownIndex = -1;
        CapturingMouse = false;
        Invalidate();
    }

    private ToolStripItem? HitTestTopItem(int x, int y)
    {
        if (y < 0 || y > Height) return null;

        var font = Font ?? Font.Default;
        float zoom = EffectiveZoom;
        int startX = _gripStyle == ToolStripGripStyle.Visible ? _gripWidth : 0;
        int itemX = startX;

        var visibleItems = _overflowActive ? _items.Take(_items.Count - _overflowItems.Count).ToList() : _items.ToList();

        foreach (var item in visibleItems)
        {
            if (!item.Visible) continue;
            int itemWidth = item.GetPreferredWidth(font, zoom) + 2;
            if (x >= itemX && x < itemX + itemWidth)
            {
                return item;
            }
            itemX += itemWidth;
        }

        return null;
    }

    private ToolStripItem? HitTestDropDown(int x, int y, ToolStripButton ownerItem)
    {
        if (_openDropDownItem == null || ownerItem.DropDownItems.Count == 0) return null;

        var font = Font ?? Font.Default;
        float zoom = EffectiveZoom;
        int itemHeight = ItemHeight;

        int ddX = 4;
        foreach (var item in _items)
        {
            if (item == ownerItem) break;
            ddX += item.GetPreferredWidth(font, zoom) + 2;
        }

        int ddY = Height;
        int maxWidth = 0;
        foreach (var ddItem in ownerItem.DropDownItems)
        {
            int w = ddItem.GetPreferredWidth(font, zoom) + 20;
            if (w > maxWidth) maxWidth = w;
        }

        int dropDownHeight = ownerItem.DropDownItems.Count * itemHeight + 4;

        if (x < ddX || x > ddX + maxWidth || y < ddY || y > ddY + dropDownHeight)
            return null;

        int itemIndex = (y - ddY - 2) / itemHeight;
        if (itemIndex >= 0 && itemIndex < ownerItem.DropDownItems.Count)
        {
            return ownerItem.DropDownItems[itemIndex];
        }

        return null;
    }

    private int GetItemIndex(ToolStripItem item)
    {
        return _items.IndexOf(item);
    }

    /// <summary>
    /// Occurs when an item is added to the ToolStrip.
    /// </summary>
    public event EventHandler<ToolStripItemEventArgs>? ItemAdded;

    /// <summary>
    /// Occurs when an item is removed from the ToolStrip.
    /// </summary>
    public event EventHandler<ToolStripItemEventArgs>? ItemRemoved;

    internal void OnItemAdded(ToolStripItem item)
    {
        item.Owner = this;
        ItemAdded?.Invoke(this, new ToolStripItemEventArgs(item));
        Invalidate();
    }

    internal void OnItemRemoved(ToolStripItem item)
    {
        item.Owner = null;
        ItemRemoved?.Invoke(this, new ToolStripItemEventArgs(item));
        Invalidate();
    }

    internal void ClearTextBoxFocus()
    {
        foreach (var item in _items)
        {
            if (item is ToolStripTextBox tb)
            {
                tb.Focused = false;
            }
        }
    }
}

/// <summary>
/// Provides data for ToolStrip item events.
/// </summary>
public class ToolStripItemEventArgs : EventArgs
{
    /// <summary>
    /// Gets the ToolStripItem associated with the event.
    /// </summary>
    public ToolStripItem Item { get; }

    /// <summary>
    /// Initializes a new instance of ToolStripItemEventArgs.
    /// </summary>
    /// <param name="item">The item associated with the event.</param>
    public ToolStripItemEventArgs(ToolStripItem item)
    {
        Item = item;
    }
}
