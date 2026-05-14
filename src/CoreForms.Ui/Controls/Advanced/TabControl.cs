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
        PerformLayout();
    }

    /// <summary>
    /// Arranges tab pages to fill the content area below the tab headers.
    /// </summary>
    protected override void OnLayout()
    {
        var tabHeaderHeight = TabHeaderHeight;
        foreach (var page in _tabPages)
        {
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
        set => _tabHeight = value;
    }

    private (int[] positions, int[] widths) CalculateTabLayout()
    {
        const int horizontalPadding = 16;
        const int minTabWidth = 40;
        var fontSize = (int)(EffectiveFont.Size * EffectiveZoom);
        var charWidth = fontSize / 2;

        var widths = new int[_tabPages.Count];
        var positions = new int[_tabPages.Count];
        int totalWidth = 0;

        for (int i = 0; i < _tabPages.Count; i++)
        {
            var textWidth = _tabPages[i].Text.Length * charWidth + horizontalPadding;
            widths[i] = Math.Max(textWidth, minTabWidth);
            positions[i] = totalWidth;
            totalWidth += widths[i];
        }

        return (positions, widths);
    }

    /// <summary>
    /// Gets the child control at the specified point, returning null for clicks in the tab header area.
    /// </summary>
    /// <param name="point">The point to test, in TabControl coordinates.</param>
    /// <returns>The child control at the point, or null if in the header area.</returns>
    protected override Control? GetChildAtPoint(Point point)
    {
        var tabHeaderHeight = TabHeaderHeight;
        if (point.Y < tabHeaderHeight)
            return null;
        var contentPoint = new Point(point.X, point.Y - tabHeaderHeight);
        // Only the selected tab page should receive hits
        if (SelectedTab != null && SelectedTab.HitTest(contentPoint))
            return SelectedTab;
        return null;
    }

    /// <summary>
    /// Finds the deepest child at the point, shifting coordinates past the tab header.
    /// Manually handles the TabPage descent to avoid re-applying the header offset
    /// through the virtual GetChildAtPoint dispatch.
    /// </summary>
    protected override Control? GetDeepestChildAtPoint(Point point, out Point localPoint)
    {
        var tabHeaderHeight = TabHeaderHeight;
        if (point.Y < tabHeaderHeight)
        {
            localPoint = point;
            return null;
        }
        var contentPoint = new Point(point.X, point.Y - tabHeaderHeight);
        if (SelectedTab == null || !SelectedTab.HitTest(contentPoint))
        {
            localPoint = point;
            return null;
        }
        var tabPageLocal = new Point(contentPoint.X - SelectedTab.X, contentPoint.Y - SelectedTab.Y);
        var deepest = (Control?)SelectedTab;
        var deepestLocal = tabPageLocal;
        FindDeepest(SelectedTab, tabPageLocal, ref deepest, ref deepestLocal);
        localPoint = deepestLocal;
        return deepest;
    }

    /// <summary>
    /// Recursively finds the deepest child control within a container.
    /// </summary>
    private void FindDeepest(ContainerControl container, Point containerLocal, ref Control? deepest, ref Point deepestLocal)
    {
        for (int i = container.Controls.Count - 1; i >= 0; i--)
        {
            var child = container.Controls[i];
            if (!child.Visible || !child.HitTest(containerLocal))
                continue;
            var childLocal = new Point(containerLocal.X - child.X, containerLocal.Y - child.Y);
            if (child is ContainerControl childContainer)
            {
                deepest = child;
                deepestLocal = childLocal;
                FindDeepest(childContainer, childLocal, ref deepest, ref deepestLocal);
                return;
            }
            deepest = child;
            deepestLocal = childLocal;
            return;
        }
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

        var (tabPositions, tabWidths) = CalculateTabLayout();

        // Draw tab headers background
        g.FillRectangle(theme.TabHeaderBackground, 0, 0, Width, tabHeaderHeight);

        // Draw individual tabs with vertical separators
        for (int i = 0; i < _tabPages.Count; i++)
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
                g.DrawString(_tabPages[i].Text, font, theme.TabSelectedText, x + horizontalPadding / 2, CoordinateTransform.CenterVertically(0, tabHeaderHeight, font, EffectiveZoom));
            }
            else
            {
                // Unselected tabs: gray background
                g.FillRectangle(theme.TabUnselectedBackground, x, 0, tabWidth, tabHeaderHeight);
                g.DrawString(_tabPages[i].Text, font, theme.TabUnselectedText, x + horizontalPadding / 2, CoordinateTransform.CenterVertically(0, tabHeaderHeight, font, EffectiveZoom));
            }

            // Vertical separator between tabs (except after last tab)
            if (i < _tabPages.Count - 1)
            {
                var sepX = x + tabWidth;
                g.DrawLine(theme.TabSeparator, sepX, 2, sepX, tabHeaderHeight - 2);
            }
        }

        // Draw separator line below tab headers (only for unselected area)
        var selectedTabX = tabPositions[_selectedIndex];
        var selectedTabWidth = tabWidths[_selectedIndex];

        // Line to the left of selected tab
        if (selectedTabX > 0)
        {
            g.DrawLine(theme.TabSeparator, 0, tabHeaderHeight, selectedTabX, tabHeaderHeight);
        }

        // Line to the right of selected tab
        var rightStart = selectedTabX + selectedTabWidth;
        if (rightStart < Width)
        {
            g.DrawLine(theme.TabSeparator, rightStart, tabHeaderHeight, Width, tabHeaderHeight);
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
    }

    /// <summary>
    /// Raises the MouseDown event to handle tab header clicks.
    /// Content area clicks are routed to child controls by the base ContainerControl.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseDown(EventArgs e)
    {
        var args = e as MouseEventArgs;
        if (args != null && _tabPages.Count > 0)
        {
            var tabHeaderHeight = TabHeaderHeight;
            if (args.Y < tabHeaderHeight)
            {
                var (tabPositions, tabWidths) = CalculateTabLayout();
                var clickedX = args.X;
                for (int i = 0; i < _tabPages.Count; i++)
                {
                    if (clickedX >= tabPositions[i] && clickedX < tabPositions[i] + tabWidths[i])
                    {
                        SelectedIndex = i;
                        return;
                    }
                }
            }
        }
        base.OnMouseDown(e);
    }

    /// <summary>
    /// Raises the MouseUp event. Content area clicks are routed by the base ContainerControl.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseUp(EventArgs e)
    {
        base.OnMouseUp(e);
    }

    /// <summary>
    /// Raises the MouseMove event. Routed by the base ContainerControl.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseMove(EventArgs e)
    {
        base.OnMouseMove(e);
    }

    /// <summary>
    /// Raises the MouseWheel event. Routed by the base ContainerControl.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseWheel(EventArgs e)
    {
        base.OnMouseWheel(e);
    }

    /// <summary>
    /// Raises the KeyDown event and routes to the active control in the selected tab page.
    /// </summary>
    /// <param name="e">A KeyEventArgs that contains the event data.</param>
    protected internal override void OnKeyDown(KeyEventArgs e)
    {
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
            default:
                if (SelectedTab?.ActiveControl != null)
                {
                    SelectedTab.ActiveControl.OnKeyDown(e);
                    if (e.Handled) return;
                }
                break;
        }
        base.OnKeyDown(e);
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
        Invalidate();
    }

    private string _text = "Tab";

    /// <summary>
    /// Gets or sets the text of the tab page (displayed in the tab header).
    /// </summary>
    public new string Text
    {
        get => _text;
        set => _text = value;
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
        Invalidate();
    }

    /// <summary>
    /// Gets the collection of status labels.
    /// </summary>
    public List<ToolStripStatusLabel> Items => _items;

    /// <summary>
    /// Renders the status strip with its items.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
    public override void Render(Graphics g)
    {
        if (!Visible) return;

        var theme = ThemeManager.CurrentTheme;

        g.FillRectangle(BackColor, 0, 0, Width, Height);
        g.DrawLine(theme.StatusStripTopLine, 0, 0, Width, 0);

        if (!string.IsNullOrEmpty(_text))
        {
            var font = EffectiveFont;
            g.DrawString(_text, font, ForeColor, 4, CoordinateTransform.CenterVertically(Height, font, EffectiveZoom));
        }

        int x = 4;
        foreach (var item in _items)
        {
            if (!string.IsNullOrEmpty(item.Text))
            {
                var font = item.Font ?? EffectiveFont;
                g.DrawString(item.Text, font, item.ForeColor, x, CoordinateTransform.CenterVertically(Height, font, EffectiveZoom));
                x += item.Text.Length * (int)(font.Size * EffectiveZoom) / 2 + 10;
            }
        }

        base.Render(g);
    }
}

/// <summary>
/// Represents a label in a StatusStrip.
/// </summary>
public class ToolStripStatusLabel : Component
{
    private string _text = string.Empty;
    private Color _foreColor = SystemColors.ControlText;
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