using CoreForms.Ui.Core;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Advanced;

public class TabControl : ContainerControl
{
    private readonly List<TabPage> _tabPages = new();
    private int _selectedIndex = 0;
    private int _tabHeight = 24;

    public TabControl()
    {
        Size = new Size(400, 300);
        BackColor = SystemColors.Control;
    }

    public List<TabPage> TabPages => _tabPages;

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

    public TabPage? SelectedTab => _selectedIndex >= 0 && _selectedIndex < _tabPages.Count
        ? _tabPages[_selectedIndex]
        : null;

    public int TabHeight
    {
        get => _tabHeight;
        set => _tabHeight = value;
    }

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

    protected virtual void OnSelectedIndexChanged()
    {
        SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? SelectedIndexChanged;
}

public class TabPage : ContainerControl
{
    public TabPage()
    {
        Size = new Size(400, 250);
        BackColor = Color.White;
    }

    private string _text = "Tab";

    public string Text
    {
        get => _text;
        set => _text = value;
    }

    public override void Render(Graphics g)
    {
        if (!Visible) return;

        g.FillRectangle(BackColor, 0, 0, Width, Height);

        base.Render(g);
    }
}

public class StatusStrip : ContainerControl
{
    private string _text = string.Empty;
    private readonly List<ToolStripStatusLabel> _items = new();

    public StatusStrip()
    {
        Size = new Size(400, 24);
        BackColor = SystemColors.Control;
        Dock = DockStyle.Bottom;
    }

    public List<ToolStripStatusLabel> Items => _items;

    public string Text
    {
        get => _text;
        set => _text = value;
    }

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

public class ToolStripStatusLabel : Component
{
    private string _text = string.Empty;
    private Color _foreColor = SystemColors.ControlText;
    private Font? _font;

    public string Text
    {
        get => _text;
        set => _text = value;
    }

    public Color ForeColor
    {
        get => _foreColor;
        set => _foreColor = value;
    }

    public Font? Font
    {
        get => _font;
        set => _font = value;
    }
}