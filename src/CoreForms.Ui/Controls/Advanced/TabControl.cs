using CoreForms.Ui.Core;
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
        BackColor = SystemColors.Control;
        TabStop = true;
    }

    /// <summary>
    /// Gets the collection of tab pages.
    /// </summary>
    public List<TabPage> TabPages => _tabPages;

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

        g.FillRectangle(BackColor, 0, 0, Width, Height);

        var font = Font ?? Font.Default;

        for (int i = 0; i < _tabPages.Count; i++)
        {
            var y = 0;
            var height = _tabHeight + 2;

            if (i == _selectedIndex)
            {
                g.FillRectangle(SystemColors.Control, i * 100, y, 100, height);
            }
            else
            {
                g.FillRectangle(Color.FromArgb(210, 210, 210), i * 100, y, 100, height);
            }

            g.DrawString(_tabPages[i].Text, font, i == _selectedIndex ? Color.Black : Color.FromArgb(100, 100, 100),
                i * 100 + 5, (_tabHeight - (int)font.Size) / 2);
        }

        g.DrawLine(Color.FromArgb(150, 150, 150), 0, _tabHeight + 2, Width, _tabHeight + 2);

        if (SelectedTab != null)
        {
            g.Save();
            g.TranslateTransform(0, _tabHeight + 3);
            SelectedTab.Render(g);
            g.Restore();
        }

        base.Render(g);
    }

    /// <summary>
    /// Raises the SelectedIndexChanged event.
    /// </summary>
    protected virtual void OnSelectedIndexChanged()
    {
        SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Raises the KeyDown event to handle tab navigation.
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
        }
        base.OnKeyDown(e);
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
        BackColor = Color.White;
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
        BackColor = SystemColors.Control;
        Dock = DockStyle.Bottom;
    }

    /// <summary>
    /// Gets the collection of status labels.
    /// </summary>
    public List<ToolStripStatusLabel> Items => _items;

    /// <summary>
    /// Gets or sets the text displayed in the status strip.
    /// </summary>
    public string Text
    {
        get => _text;
        set => _text = value;
    }

    /// <summary>
    /// Renders the status strip with its items.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
    public override void Render(Graphics g)
    {
        if (!Visible) return;

        g.FillRectangle(BackColor, 0, 0, Width, Height);
        g.DrawLine(Color.FromArgb(150, 150, 150), 0, 0, Width, 0);

        if (!string.IsNullOrEmpty(_text))
        {
            var font = Font ?? Font.Default;
            g.DrawString(_text, font, ForeColor, 4, (Height - (int)font.Size) / 2);
        }

        int x = 4;
        foreach (var item in _items)
        {
            if (!string.IsNullOrEmpty(item.Text))
            {
                var font = item.Font ?? Font.Default;
                g.DrawString(item.Text, font, item.ForeColor, x, (Height - (int)font.Size) / 2);
                x += item.Text.Length * (int)font.Size / 2 + 10;
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