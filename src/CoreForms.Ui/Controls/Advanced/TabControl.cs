using CoreForms.Ui.Core;
using CoreForms.Ui.Theming;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Advanced;

/// <summary>
/// A control that displays tab pages that can be selected by the user.
/// </summary>
public class TabControl : ContainerControl
{
    private readonly List<TabPage> _tabPages = new();
    private int _selectedIndex = 0;
    private int _tabHeight = 24;

    private const int OverflowButtonWidth = 17;
    private bool _showOverflowButton;
    private bool _overflowDroppedDown;
    private int _overflowHoveredIndex = -1;
    private int _overflowScrollOffset;
    private int _overflowDropDownHeight = 160;
    private int _lastVisibleCount;
    private int _firstVisibleTab;
    private readonly ScrollBarEngine _overflowScrollBar = new();
    private OverflowScrollBarContext? _overflowScrollBarContext;

    private OverflowScrollBarContext OverflowScrollBarCtx => _overflowScrollBarContext ??= new OverflowScrollBarContext(this);

    private int TabHeaderHeight => _tabHeight + 2;

    /// <summary>
    /// Returns the tab header height as the render offset applied to child controls.
    /// </summary>
    /// <returns>The offset for child rendering.</returns>
    protected internal override Point GetChildRenderOffset() => new Point(0, TabHeaderHeight);

    /// <summary>
    /// Initializes a new instance of TabControl.
    /// </summary>
    public TabControl()
    {
        Size = new Size(400, 300);
        _backColor = ThemeManager.CurrentTheme.ControlBackground;
        TabStop = true;
    }

    /// <summary>
    /// Called when the theme changes. Updates tabcontrol-specific colors.
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
    /// Gets the collection of tab pages.
    /// </summary>
    public List<TabPage> TabPages => _tabPages;

    /// <summary>
    /// Adds a tab page to the collection.
    /// </summary>
    /// <param name="page">The tab page to add.</param>
    public void AddTabPage(TabPage page)
    {
        page.TabStop = false;
        _tabPages.Add(page);
        Controls.Add(page);
    }

    /// <summary>
    /// Arranges tab pages to fill the content area below the tab headers.
    /// Ensures each page has its Parent set so FindForm/EffectiveZoom work correctly.
    /// </summary>
    protected override void OnLayout()
    {
        var tabHeaderHeight = TabHeaderHeight;
        foreach (var page in _tabPages)
        {
            if (page.Parent == null)
                page.Parent = this;
            page.Bounds = new Rectangle(0, 0, Width, Math.Max(0, Height - tabHeaderHeight));
        }
        base.OnLayout();
    }

    /// <summary>
    /// Gets or sets the index of the selected tab page.
    /// </summary>
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (value >= 0 && value < _tabPages.Count && _selectedIndex != value)
            {
                _selectedIndex = value;
                OnSelectedIndexChanged();
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets the selected tab page.
    /// </summary>
    public TabPage? SelectedTab => _selectedIndex >= 0 && _selectedIndex < _tabPages.Count
        ? _tabPages[_selectedIndex]
        : null;

    /// <summary>
    /// Gets or sets the height of the tab headers.
    /// </summary>
    public int TabHeight
    {
        get => _tabHeight;
        set
        {
            if (_tabHeight != value)
            {
                _tabHeight = value;
                MarkLayoutDirty();
                PerformLayout();
                Invalidate();
            }
        }
    }

    private (int[] positions, int[] widths, int visibleCount) CalculateTabLayout()
    {
        const int horizontalPadding = 16;
        const int minTabWidth = 40;
        float zoom = EffectiveZoom;

        var widths = new int[_tabPages.Count];
        int totalAllWidth = 0;
        for (int i = 0; i < _tabPages.Count; i++)
        {
            var text = _tabPages[i].Text;
            var measured = CoordinateTransform.MeasureText(text, EffectiveFont, zoom);
            var textLogicalWidth = measured.width / Math.Max(zoom, 0.001f);
            widths[i] = Math.Max((int)(textLogicalWidth + horizontalPadding), minTabWidth);
            totalAllWidth += widths[i];
        }

        var positions = new int[_tabPages.Count];
        int currentX = 0;

        if (totalAllWidth <= Width && _tabPages.Count > 0)
        {
            _showOverflowButton = false;
            _firstVisibleTab = 0;
            _lastVisibleCount = _tabPages.Count;
            for (int i = 0; i < _tabPages.Count; i++)
            {
                positions[i] = currentX;
                currentX += widths[i];
            }
            return (positions, widths, _tabPages.Count);
        }

        _showOverflowButton = true;
        int availableWidth = Width - OverflowButtonWidth;

        // Determine visible range that includes the selected tab
        // Start from selected tab and go backwards
        int lastVisible = _selectedIndex;
        int usedWidth = widths[_selectedIndex];
        int firstVisible = _selectedIndex;
        while (firstVisible > 0 && usedWidth + widths[firstVisible - 1] <= availableWidth)
        {
            firstVisible--;
            usedWidth += widths[firstVisible];
        }

        // Try to add tabs after the selected tab if space allows
        int rightEdge = usedWidth;
        for (int i = _selectedIndex + 1; i < _tabPages.Count; i++)
        {
            if (rightEdge + widths[i] <= availableWidth)
            {
                rightEdge += widths[i];
                lastVisible = i;
            }
            else
            {
                break;
            }
        }

        // Compute positions for visible range
        currentX = 0;
        for (int i = firstVisible; i <= lastVisible; i++)
        {
            positions[i] = currentX;
            currentX += widths[i];
        }

        int visibleCount = lastVisible - firstVisible + 1;
        _firstVisibleTab = firstVisible;
        _lastVisibleCount = visibleCount;
        return (positions, widths, visibleCount);
    }

    /// <summary>
    /// Gets the child control at the specified point, returning null for clicks in the tab header area.
    /// Adjusts the point by the tab header offset before checking bounds, since the TabPage
    /// is visually rendered below the header but its Bounds origin is at (0,0).
    /// </summary>
    /// <param name="point">The point to test, in TabControl coordinates.</param>
    /// <returns>The child control at the point, or null if in the header area.</returns>
    protected override Control? GetChildAtPoint(Point point)
    {
        var tabHeaderHeight = TabHeaderHeight;
        if (point.Y < tabHeaderHeight)
            return null;
        var adjusted = new Point(point.X, point.Y - tabHeaderHeight);
        if (SelectedTab != null && SelectedTab.HitTest(adjusted))
            return SelectedTab;
        return null;
    }

    /// <summary>
    /// Renders the tab control with its tab headers and selected page.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
    public override void Render(Graphics g)
    {
        if (!Visible) return;

        var theme = ThemeManager.CurrentTheme;
        var font = EffectiveFont;
        var tabHeaderHeight = TabHeaderHeight;
        const int horizontalPadding = 16;

        var (tabPositions, tabWidths, visibleCount) = CalculateTabLayout();

        // Draw tab headers background
        g.FillRectangle(theme.TabHeaderBackground, 0, 0, Width, tabHeaderHeight);

        var tabTextColor = Enabled ? theme.TabSelectedText : theme.GrayText;
        var unselectedTabTextColor = Enabled ? theme.TabSelectedText : theme.GrayText;

        // Draw individual tabs with vertical separators
        for (int i = _firstVisibleTab; i < _firstVisibleTab + visibleCount; i++)
        {
            var x = tabPositions[i];
            var tabWidth = tabWidths[i];

            if (i == _selectedIndex)
            {
                // Selected tab: white background, border on top/right/left (not bottom)
                g.FillRectangle(theme.TabSelectedBackground, x, 0, tabWidth, tabHeaderHeight);
                // Top border
                g.DrawLine(theme.TabSelectedBorder, x, 0, x + tabWidth, 0);
                // Left border
                g.DrawLine(theme.TabSelectedBorder, x, 0, x, tabHeaderHeight);
                // Right border
                g.DrawLine(theme.TabSelectedBorder, x + tabWidth, 0, x + tabWidth, tabHeaderHeight);
                g.DrawString(_tabPages[i].Text, font, tabTextColor, x + horizontalPadding / 2, CoordinateTransform.CenterVertically(0, tabHeaderHeight, font, EffectiveZoom));
            }
            else
            {
                // Unselected tabs: gray background
                g.FillRectangle(theme.TabUnselectedBackground, x, 0, tabWidth, tabHeaderHeight);
                g.DrawString(_tabPages[i].Text, font, unselectedTabTextColor, x + horizontalPadding / 2, CoordinateTransform.CenterVertically(0, tabHeaderHeight, font, EffectiveZoom));
            }

            // Vertical separator between tabs (except after last tab)
            if (i < _firstVisibleTab + visibleCount - 1)
            {
                var sepX = x + tabWidth;
                g.DrawLine(theme.TabSeparator, sepX, 2, sepX, tabHeaderHeight - 2);
            }
        }

        // Draw overflow dropdown button
        if (_showOverflowButton)
        {
            var btnX = Width - OverflowButtonWidth;
            g.FillRectangle(theme.ControlBackground, btnX, 1, OverflowButtonWidth, tabHeaderHeight - 2);
            g.DrawLine(theme.ComboBoxDropdownButtonSeparator, btnX, 0, btnX, tabHeaderHeight);

            var cx = btnX + OverflowButtonWidth / 2;
            var cy = tabHeaderHeight / 2;
            var tw = 4;
            var th = 3;
            g.FillTriangle(theme.ComboBoxDropdownArrow,
                cx - tw, cy - th,
                cx + tw, cy - th,
                cx, cy + th);
        }

        // Draw separator line below tab headers (only for unselected area)
        if (visibleCount > 0 && _selectedIndex >= _firstVisibleTab && _selectedIndex < _firstVisibleTab + visibleCount)
        {
            var selectedTabX = tabPositions[_selectedIndex];
            var selectedTabWidth = tabWidths[_selectedIndex];

            if (_showOverflowButton)
            {
                var btnLeftEdge = Width - OverflowButtonWidth;
                // Line to the left of selected tab
                if (selectedTabX > 0)
                    g.DrawLine(theme.TabSeparator, 0, tabHeaderHeight, selectedTabX, tabHeaderHeight);
                // Line to the right of selected tab
                var rightStart = selectedTabX + selectedTabWidth;
                if (rightStart < btnLeftEdge)
                    g.DrawLine(theme.TabSeparator, rightStart, tabHeaderHeight, btnLeftEdge, tabHeaderHeight);
            }
            else
            {
                // Line to the left of selected tab
                if (selectedTabX > 0)
                    g.DrawLine(theme.TabSeparator, 0, tabHeaderHeight, selectedTabX, tabHeaderHeight);
                // Line to the right of selected tab
                var rightStart = selectedTabX + selectedTabWidth;
                if (rightStart < Width)
                    g.DrawLine(theme.TabSeparator, rightStart, tabHeaderHeight, Width, tabHeaderHeight);
            }
        }

        // Draw content area background (start at tabHeaderHeight to hide any child control borders at top)
        g.FillRectangle(theme.TabContentBackground, 0, tabHeaderHeight, Width, Height - tabHeaderHeight);

        // Render selected tab page content
        if (SelectedTab != null)
        {
            g.Save();
            g.TranslateTransform(0, tabHeaderHeight);
            SelectedTab.Render(g);
            g.Restore();
        }

        // Draw border around entire control (after content to ensure visibility)
        g.DrawRectangle(theme.ControlDark, 0, 0, Width, Height, 1);
    }

    /// <summary>
    /// Renders overlays (like dropdowns) with the same offset as Render to ensure
    /// child controls are positioned correctly.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
    public override void RenderOverlay(Graphics g)
    {
        if (!Visible) return;

        var tabHeaderHeight = TabHeaderHeight;

        if (SelectedTab != null)
        {
            g.Save();
            g.TranslateTransform(0, tabHeaderHeight);
            SelectedTab.RenderOverlay(g);
            g.Restore();
        }

        if (!_overflowDroppedDown) return;

        var theme = ThemeManager.CurrentTheme;
        var font = EffectiveFont;
        var itemHeight = CoordinateTransform.GetItemHeight(font, EffectiveZoom);
        var overflowItems = GetOverflowTabs();
        var totalHeight = overflowItems.Count * itemHeight;
        var scrollBarWidth = ScrollBarEngine.DefaultScrollBarSize;
        var needsScrollbar = totalHeight > _overflowDropDownHeight;
        const int fullListWidth = OverflowButtonWidth + 150;
        var contentListWidth = needsScrollbar ? fullListWidth - scrollBarWidth : fullListWidth;
        var btnX = Width - OverflowButtonWidth;
        var dropX = btnX - (fullListWidth - OverflowButtonWidth);
        int dropY = tabHeaderHeight;

        g.FillRectangle(theme.MenuDropdownBackground, dropX, dropY, fullListWidth, _overflowDropDownHeight);
        g.DrawRectangle(theme.MenuDropdownBorder, dropX, dropY, fullListWidth, _overflowDropDownHeight, 1);

        _overflowScrollBar.SmallChange = itemHeight;
        _overflowScrollBar.ViewSize = _overflowDropDownHeight;
        _overflowScrollBar.ContentSize = totalHeight;
        if (needsScrollbar)
        {
            var scrollBarBounds = new Rectangle(dropX + contentListWidth, dropY, scrollBarWidth, _overflowDropDownHeight);
            _overflowScrollBar.Render(g, scrollBarBounds, theme);
        }

        _overflowScrollOffset = _overflowScrollBar.Value;

        g.SetClip(new Rectangle(dropX, dropY, contentListWidth, _overflowDropDownHeight));

        for (int i = 0; i < overflowItems.Count; i++)
        {
            var y = dropY + 2 + i * itemHeight - _overflowScrollOffset;

            if (y + itemHeight <= dropY) continue;
            if (y >= dropY + _overflowDropDownHeight) break;

            var actualIndex = _tabPages.IndexOf(overflowItems[i]);

            if (actualIndex == _selectedIndex)
            {
                g.FillRectangle(theme.Highlight, dropX + 1, y, contentListWidth - 2, itemHeight);
                g.DrawString(overflowItems[i].Text, font, theme.HighlightText, dropX + 4, y + 2);
            }
            else if (i == _overflowHoveredIndex)
            {
                g.FillRectangle(theme.HoverHighlight, dropX + 1, y, contentListWidth - 2, itemHeight);
                g.DrawString(overflowItems[i].Text, font, theme.ControlText, dropX + 4, y + 2);
            }
            else
            {
                g.DrawString(overflowItems[i].Text, font, theme.ControlText, dropX + 4, y + 2);
            }
        }

        g.ResetClip();
    }

    /// <summary>
    /// Tests hit test including the overflow dropdown area when open.
    /// </summary>
    public override bool HitTest(Point point)
    {
        if (Bounds.Contains(point))
            return true;

        if (_overflowDroppedDown)
        {
            const int fullListWidth = OverflowButtonWidth + 150;
            var btnX = Width - OverflowButtonWidth;
            var dropX = btnX - (fullListWidth - OverflowButtonWidth);
            var dropBounds = new Rectangle(X + dropX, Y + TabHeaderHeight, fullListWidth, _overflowDropDownHeight);
            if (dropBounds.Contains(point))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the list of tab pages that are not in the visible tab header area.
    /// </summary>
    private List<TabPage> GetOverflowTabs()
    {
        if (!_showOverflowButton || _lastVisibleCount >= _tabPages.Count)
            return new List<TabPage>();

        var overflow = new List<TabPage>();
        for (int i = 0; i < _tabPages.Count; i++)
        {
            if (i < _firstVisibleTab || i >= _firstVisibleTab + _lastVisibleCount)
                overflow.Add(_tabPages[i]);
        }
        return overflow;
    }

    private void CloseOverflowDropdown()
    {
        _overflowDroppedDown = false;
        _overflowScrollOffset = 0;
        _overflowHoveredIndex = -1;
        _overflowScrollBar.ScrollTo(0);
        CapturingMouse = false;
        Invalidate();
    }

    /// <summary>
    /// Raises the MouseDown event to handle tab header clicks and overflow dropdown.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseDown(EventArgs e)
    {
        if (!Enabled) return;
        var args = e as MouseEventArgs;
        if (args == null) return;

        // Handle overflow dropdown item selection and button click
        var tabHeaderHeight = TabHeaderHeight;

        if (_overflowDroppedDown)
        {
            var itemHeight = CoordinateTransform.GetItemHeight(EffectiveFont, EffectiveZoom);
            var overflowItems = GetOverflowTabs();
            var totalHeight = overflowItems.Count * itemHeight;
            var scrollBarWidth = ScrollBarEngine.DefaultScrollBarSize;
            bool needsScrollbar = totalHeight > _overflowDropDownHeight;
            const int fullListWidth = OverflowButtonWidth + 150;
            var contentListWidth = needsScrollbar ? fullListWidth - scrollBarWidth : fullListWidth;
            var btnX = Width - OverflowButtonWidth;
            var dropX = btnX - (fullListWidth - OverflowButtonWidth);
            int dropY = tabHeaderHeight;

            if (args.X >= dropX && args.X < dropX + fullListWidth && args.Y >= dropY && args.Y < dropY + _overflowDropDownHeight)
            {
                if (needsScrollbar && args.X >= dropX + contentListWidth)
                {
                    var scrollBarBounds = new Rectangle(dropX + contentListWidth, dropY, scrollBarWidth, _overflowDropDownHeight);
                    _overflowScrollBar.HandleMouseDown(new Point(args.X, args.Y), scrollBarBounds, OverflowScrollBarCtx);
                    return;
                }

                int localY = args.Y - dropY - 2 + _overflowScrollOffset;
                if (localY >= 0)
                {
                    int index = localY / itemHeight;
                    if (index >= 0 && index < overflowItems.Count)
                    {
                        var actualIndex = _tabPages.IndexOf(overflowItems[index]);
                        if (actualIndex >= 0)
                            SelectedIndex = actualIndex;
                    }
                }

                CloseOverflowDropdown();
                return;
            }

            CloseOverflowDropdown();
            return;
        }

        // Handle overflow button click
        if (_showOverflowButton && args.Y < tabHeaderHeight && args.X >= Width - OverflowButtonWidth && args.X < Width)
        {
            _overflowDroppedDown = true;
            CapturingMouse = true;
            _overflowScrollOffset = 0;
            _overflowHoveredIndex = -1;
            Invalidate();
            return;
        }

        // Handle tab header clicks
        if (args.Y < tabHeaderHeight && _tabPages.Count > 0)
        {
            var (tabPositions, tabWidths, _) = CalculateTabLayout();
            var clickedX = args.X;
            for (int i = _firstVisibleTab; i < _firstVisibleTab + _lastVisibleCount; i++)
            {
                if (clickedX >= tabPositions[i] && clickedX < tabPositions[i] + tabWidths[i])
                {
                    SelectedIndex = i;
                    return;
                }
            }
        }

        Console.WriteLine($"[TabControl] OnMouseDown args=({args.X},{args.Y}) tabHeaderHeight={tabHeaderHeight} -> delegating to base.OnMouseDown");
        base.OnMouseDown(e);
    }

    /// <summary>
    /// Raises the MouseUp event. Handles scrollbar release in overflow dropdown.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseUp(EventArgs e)
    {
        if (_overflowScrollBar.IsDragging || _overflowScrollBar.IsUpButtonPressed || _overflowScrollBar.IsDownButtonPressed)
        {
            _overflowScrollBar.HandleMouseUp(OverflowScrollBarCtx);
            return;
        }

        if (!_overflowDroppedDown && CapturingMouse)
        {
            CapturingMouse = false;
            return;
        }
        base.OnMouseUp(e);
    }

    /// <summary>
    /// Raises the MouseMove event for hover tracking in overflow dropdown and scrollbar.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseMove(EventArgs e)
    {
        if (!Enabled) return;

        if (_overflowDroppedDown)
        {
            var args = e as MouseEventArgs;
            if (args != null)
            {
                var itemHeight = CoordinateTransform.GetItemHeight(EffectiveFont, EffectiveZoom);
                var overflowItems = GetOverflowTabs();
                var totalHeight = overflowItems.Count * itemHeight;
                var scrollBarWidth = ScrollBarEngine.DefaultScrollBarSize;
                const int fullListWidth = OverflowButtonWidth + 150;
                var contentListWidth = totalHeight > _overflowDropDownHeight ? fullListWidth - scrollBarWidth : fullListWidth;
                var btnX = Width - OverflowButtonWidth;
                var dropX = btnX - (fullListWidth - OverflowButtonWidth);
                int dropY = TabHeaderHeight;

                _overflowScrollBar.SmallChange = itemHeight;
                _overflowScrollBar.ViewSize = _overflowDropDownHeight;
                _overflowScrollBar.ContentSize = totalHeight;
                bool needsScrollbar = _overflowScrollBar.NeedsScrollbar;

                if (needsScrollbar)
                {
                    var scrollBarBounds = new Rectangle(dropX + contentListWidth, dropY, scrollBarWidth, _overflowDropDownHeight);
                    _overflowScrollBar.HandleMouseMove(new Point(args.X, args.Y), scrollBarBounds, OverflowScrollBarCtx);
                }

                if (args.Y >= dropY && args.Y < dropY + _overflowDropDownHeight)
                {
                    int localY = args.Y - dropY - 2 + _overflowScrollOffset;
                    if (localY >= 0)
                    {
                        int index = localY / itemHeight;
                        if (index >= 0 && index < overflowItems.Count)
                        {
                            if (_overflowHoveredIndex != index)
                            {
                                _overflowHoveredIndex = index;
                                Invalidate();
                            }
                        }
                        else if (_overflowHoveredIndex != -1)
                        {
                            _overflowHoveredIndex = -1;
                            Invalidate();
                        }
                    }
                    else if (_overflowHoveredIndex != -1)
                    {
                        _overflowHoveredIndex = -1;
                        Invalidate();
                    }
                }
                else if (_overflowHoveredIndex != -1)
                {
                    _overflowHoveredIndex = -1;
                    Invalidate();
                }
            }
            return;
        }

        base.OnMouseMove(e);
    }

    /// <summary>
    /// Raises the MouseWheel event for scrolling in the overflow dropdown.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseWheel(EventArgs e)
    {
        if (!Enabled) return;
        if (_overflowDroppedDown && !_overflowScrollBar.IsDragging)
        {
            var args = e as MouseEventArgs;
            if (args != null)
            {
                var itemHeight = CoordinateTransform.GetItemHeight(EffectiveFont, EffectiveZoom);
                var overflowItems = GetOverflowTabs();
                _overflowScrollBar.SmallChange = itemHeight;
                _overflowScrollBar.ViewSize = _overflowDropDownHeight;
                _overflowScrollBar.ContentSize = overflowItems.Count * itemHeight;
                if (_overflowScrollBar.NeedsScrollbar)
                    _overflowScrollBar.HandleMouseWheel(args.Delta, OverflowScrollBarCtx);
            }
            return;
        }
        base.OnMouseWheel(e);
    }

    /// <summary>
    /// Raises the KeyDown event and handles overflow dropdown keyboard navigation.
    /// </summary>
    /// <param name="e">A KeyEventArgs that contains the event data.</param>
    protected internal override void OnKeyDown(KeyEventArgs e)
    {
        if (!Enabled) return;

        var overflowItems = GetOverflowTabs();

        if (_overflowDroppedDown)
        {
            switch (e.KeyCode)
            {
                case Keys.Down:
                    if (_selectedIndex < _tabPages.Count - 1)
                    {
                        SelectedIndex++;
                        if (overflowItems.Count > 0)
                            EnsureOverflowSelectedVisible();
                    }
                    e.Handled = true;
                    break;
                case Keys.Up:
                    if (_selectedIndex > 0)
                    {
                        SelectedIndex--;
                        if (overflowItems.Count > 0)
                            EnsureOverflowSelectedVisible();
                    }
                    e.Handled = true;
                    break;
                case Keys.Enter:
                    CloseOverflowDropdown();
                    e.Handled = true;
                    break;
                case Keys.Escape:
                    CloseOverflowDropdown();
                    e.Handled = true;
                    break;
                default:
                    base.OnKeyDown(e);
                    break;
            }
            return;
        }

        switch (e.KeyCode)
        {
            case Keys.Left:
                if (_selectedIndex > 0)
                {
                    SelectedIndex--;
                    e.Handled = true;
                }
                break;
            case Keys.Right:
                if (_selectedIndex < _tabPages.Count - 1)
                {
                    SelectedIndex++;
                    e.Handled = true;
                }
                break;
            case Keys.F4:
                if (_showOverflowButton && overflowItems.Count > 0)
                {
                    _overflowDroppedDown = true;
                    CapturingMouse = true;
                    _overflowScrollOffset = 0;
                    _overflowHoveredIndex = -1;
                    EnsureOverflowSelectedVisible();
                    Invalidate();
                    e.Handled = true;
                }
                break;
            default:
                if (SelectedTab != null)
                {
                    var activeControl = SelectedTab.ActiveControl;
                    if (activeControl == null)
                    {
                        activeControl = GetFirstFocusableControl(SelectedTab);
                        if (activeControl != null)
                            SelectedTab.ActiveControl = activeControl;
                    }
                    if (activeControl != null)
                    {
                        activeControl.OnKeyDown(e);
                        if (e.Handled) return;
                    }
                }
                break;
        }
        base.OnKeyDown(e);
    }

    /// <summary>
    /// Gets the first focusable control within the specified container.
    /// Recursively searches child containers for the first control that can receive focus.
    /// </summary>
    /// <param name="container">The container to search in.</param>
    /// <returns>The first focusable control, or null if none found.</returns>
    private static Control? GetFirstFocusableControl(Control container)
    {
        foreach (Control child in container.Controls)
        {
            if (child.Visible && child.Enabled && child.TabStop)
                return child;
            if (child is ContainerControl subContainer)
            {
                var result = GetFirstFocusableControl(subContainer);
                if (result != null)
                    return result;
            }
        }
        return null;
    }

    private void EnsureOverflowSelectedVisible()
    {
        var overflowItems = GetOverflowTabs();
        if (overflowItems.Count == 0) return;

        var selInOverflow = overflowItems.FindIndex(t => _tabPages.IndexOf(t) == _selectedIndex);
        if (selInOverflow < 0) return;

        var itemHeight = CoordinateTransform.GetItemHeight(EffectiveFont, EffectiveZoom);
        int selTop = selInOverflow * itemHeight;
        int oldOffset = _overflowScrollOffset;
        _overflowScrollBar.ViewSize = _overflowDropDownHeight;
        _overflowScrollBar.ContentSize = overflowItems.Count * itemHeight;
        _overflowScrollBar.EnsureVisible(selTop, itemHeight);
        if (_overflowScrollBar.Value != oldOffset)
        {
            _overflowScrollOffset = _overflowScrollBar.Value;
            Invalidate();
        }
    }

    /// <summary>
    /// Raises the LostFocus event and closes the overflow dropdown.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnLostFocus(EventArgs e)
    {
        if (_overflowDroppedDown)
            CloseOverflowDropdown();
        base.OnLostFocus(e);
    }

    /// <summary>
    /// Raises the KeyUp event and routes to the active control in the selected tab page.
    /// </summary>
    /// <param name="e">A KeyEventArgs that contains the event data.</param>
    protected internal override void OnKeyUp(KeyEventArgs e)
    {
        if (SelectedTab?.ActiveControl != null)
        {
            SelectedTab.ActiveControl.OnKeyUp(e);
            if (e.Handled) return;
        }
        base.OnKeyUp(e);
    }

    /// <summary>
    /// Raises the KeyPress event and routes to the active control in the selected tab page.
    /// </summary>
    /// <param name="e">A KeyPressEventArgs that contains the event data.</param>
    protected internal override void OnKeyPress(KeyPressEventArgs e)
    {
        if (SelectedTab?.ActiveControl != null)
        {
            SelectedTab.ActiveControl.OnKeyPress(e);
            if (e.Handled) return;
        }
        base.OnKeyPress(e);
    }

    /// <summary>
    /// Raises the TextInput event and routes to the active control in the selected tab page.
    /// </summary>
    /// <param name="text">The input text.</param>
    protected internal override void OnTextInput(string text)
    {
        if (SelectedTab?.ActiveControl != null)
        {
            SelectedTab.ActiveControl.OnTextInput(text);
        }
    }

    /// <summary>
    /// Raises the SelectedIndexChanged event.
    /// </summary>
    protected virtual void OnSelectedIndexChanged()
    {
        SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Occurs when the selected tab index changes.
    /// </summary>
    public event EventHandler? SelectedIndexChanged;

    private sealed class OverflowScrollBarContext : IScrollBarContext
    {
        private readonly TabControl _owner;
        public OverflowScrollBarContext(TabControl owner) => _owner = owner;
        public float Zoom => _owner.EffectiveZoom;
        public void Invalidate() => _owner.Invalidate();
        public void CaptureMouse(bool capture) => _owner.CapturingMouse = capture;
    }
}

/// <summary>
/// Represents a single tab page in a TabControl.
/// </summary>
public class TabPage : ContainerControl
{
    /// <summary>
    /// Initializes a new instance of TabPage.
    /// </summary>
    public TabPage()
    {
        Size = new Size(400, 250);
        _backColor = ThemeManager.CurrentTheme.TabContentBackground;
        TabStop = false;
    }

    /// <summary>
    /// Called when the theme changes. Updates tabpage-specific colors.
    /// </summary>
    /// <param name="newTheme">The new theme that was activated.</param>
    public override void OnThemeChanged(Theme newTheme)
    {
        if (!_backColorSet)
            _backColor = newTheme.TabContentBackground;
        if (!_foreColorSet)
            _foreColor = newTheme.ControlText;
        Invalidate();
    }

    private string _text = "Tab";

    /// <summary>
    /// Gets or sets the text of the tab page (displayed in the tab header).
    /// </summary>
    public new string Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Renders the tab page with its background.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
    public override void Render(Graphics g)
    {
        if (!Visible) return;

        g.FillRectangle(BackColor, 0, 0, Width, Height);

        base.Render(g);
    }
}

/// <summary>
/// A status strip that displays information at the bottom of a form.
/// </summary>
public class StatusStrip : ContainerControl
{
    private string _text = string.Empty;
    private readonly List<ToolStripStatusLabel> _items = new();

    /// <summary>
    /// Initializes a new instance of StatusStrip.
    /// </summary>
    public StatusStrip()
    {
        Size = new Size(400, 24);
        _backColor = ThemeManager.CurrentTheme.ControlBackground;
        Dock = DockStyle.Bottom;
    }

    /// <summary>
    /// Called when the theme changes. Updates statusstrip-specific colors.
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
    /// Renders the StatusStrip with its background and top border line.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
    public override void Render(Graphics g)
    {
        if (!Visible) return;

        var theme = ThemeManager.CurrentTheme;

        g.FillRectangle(BackColor, 0, 0, Width, Height);
        g.DrawLine(theme.StatusStripTopLine, 0, 0, Width, 0, 1);

        base.Render(g);
    }
}

/// <summary>
/// Represents a label in a StatusStrip.
/// </summary>
public class ToolStripStatusLabel : Component
{
    private string _text = string.Empty;
    private Color _foreColor = ThemeManager.CurrentTheme.ControlText;
    private Font? _font;

    /// <summary>
    /// Gets or sets the text of the status label.
    /// </summary>
    public string Text
    {
        get => _text;
        set => _text = value;
    }

    /// <summary>
    /// Gets or sets the foreground color of the status label.
    /// </summary>
    public Color ForeColor
    {
        get => _foreColor;
        set => _foreColor = value;
    }

    /// <summary>
    /// Gets or sets the font of the status label.
    /// </summary>
    public Font? Font
    {
        get => _font;
        set => _font = value;
    }
}