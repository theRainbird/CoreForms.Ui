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

    /// <summary>
    /// Renders the tab control with its tab headers and selected page.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
    public override void Render(Graphics g)
    {
        if (!Visible) return;

        var theme = ThemeManager.CurrentTheme;
        var font = EffectiveFont;
        var tabHeaderHeight = _tabHeight + 2;

        // Draw tab headers background
        g.FillRectangle(theme.TabHeaderBackground, 0, 0, Width, tabHeaderHeight);

        // Draw individual tabs
        for (int i = 0; i < _tabPages.Count; i++)
        {
            var x = i * 100;
            var width = 100;

            if (i == _selectedIndex)
            {
                // Selected tab: white background, no bottom line
                g.FillRectangle(theme.TabSelectedBackground, x, 0, width, tabHeaderHeight);
                g.DrawString(_tabPages[i].Text, font, theme.TabSelectedText, x + 5, CoordinateTransform.CenterVertically(0, tabHeaderHeight, font, EffectiveZoom));
            }
            else
            {
                // Unselected tabs: gray background
                g.FillRectangle(theme.TabUnselectedBackground, x, 0, width, tabHeaderHeight);
                g.DrawString(_tabPages[i].Text, font, theme.TabUnselectedText, x + 5, CoordinateTransform.CenterVertically(0, tabHeaderHeight, font, EffectiveZoom));
            }
        }

        // Draw separator line (only for unselected area)
        var selectedTabX = _selectedIndex * 100;
        var selectedTabWidth = 100;

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

        // Draw content area background
        g.FillRectangle(theme.TabContentBackground, 0, tabHeaderHeight + 1, Width, Height - tabHeaderHeight - 1);

        // Render selected tab page content
        if (SelectedTab != null)
        {
            g.Save();
            g.TranslateTransform(0, tabHeaderHeight + 1);
            SelectedTab.Render(g);
            g.Restore();
        }

        // Draw border around entire control (after content to ensure visibility)
        g.DrawRectangle(theme.ControlDark, 0, 0, Width, Height, 1);
    }

    /// <summary>
    /// Raises the MouseDown event to handle tab header clicks and route to tab page content.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseDown(EventArgs e)
    {
        var args = e as MouseEventArgs;
        if (args != null && _tabPages.Count > 0)
        {
            var tabHeaderHeight = _tabHeight + 2;
            if (args.Y < tabHeaderHeight)
            {
                var clickedIndex = args.X / 100;
                if (clickedIndex >= 0 && clickedIndex < _tabPages.Count)
                {
                    SelectedIndex = clickedIndex;
                    return;
                }
            }
            else if (SelectedTab != null)
            {
                var localArgs = new MouseEventArgs(args.Button, args.Clicks, args.X, args.Y - tabHeaderHeight - 1, args.Delta);
                SelectedTab.OnMouseDown(localArgs);
                if (SelectedTab.ActiveControl != null)
                {
                    SelectedTab.ActiveControl.Focused = true;
                }
                return;
            }
        }
        base.OnMouseDown(e);
    }

    /// <summary>
    /// Raises the MouseUp event and routes to the selected tab page.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseUp(EventArgs e)
    {
        var args = e as MouseEventArgs;
        if (args != null && _tabPages.Count > 0 && SelectedTab != null)
        {
            var tabHeaderHeight = _tabHeight + 2;
            if (args.Y >= tabHeaderHeight)
            {
                var localArgs = new MouseEventArgs(args.Button, args.Clicks, args.X, args.Y - tabHeaderHeight - 1, args.Delta);
                SelectedTab.OnMouseUp(localArgs);
                return;
            }
            // Header area click - tab may have switched, but don't route to non-selected TabPages
            return;
        }
        base.OnMouseUp(e);
    }

    /// <summary>
    /// Raises the MouseMove event and routes to the selected tab page.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseMove(EventArgs e)
    {
        var args = e as MouseEventArgs;
        if (args != null && _tabPages.Count > 0 && SelectedTab != null)
        {
            var tabHeaderHeight = _tabHeight + 2;
            if (args.Y >= tabHeaderHeight)
            {
                var localArgs = new MouseEventArgs(args.Button, args.Clicks, args.X, args.Y - tabHeaderHeight - 1, args.Delta);
                SelectedTab.OnMouseMove(localArgs);
                return;
            }
            // Header area move - don't route to non-selected TabPages
            return;
        }
        base.OnMouseMove(e);
    }

    /// <summary>
    /// Raises the MouseWheel event and routes to the selected tab page.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseWheel(EventArgs e)
    {
        var args = e as MouseEventArgs;
        if (args != null && _tabPages.Count > 0 && SelectedTab != null)
        {
            var tabHeaderHeight = _tabHeight + 2;
            if (args.Y >= tabHeaderHeight)
            {
                SelectedTab.OnMouseWheel(e);
                return;
            }
        }
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
    public string Text
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