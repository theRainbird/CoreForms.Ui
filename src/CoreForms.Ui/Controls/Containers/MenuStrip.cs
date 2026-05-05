using CoreForms.Ui.Core;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Containers;

public class MenuStrip : ContainerControl
{
    private readonly List<ToolStripMenuItem> _items = new();

    public MenuStrip()
    {
        Size = new Size(400, 24);
        BackColor = SystemColors.Control;
    }

    public List<ToolStripMenuItem> Items => _items;

    public override void Render(Graphics g)
    {
        if (!Visible) return;

        g.FillRectangle(BackColor, 0, 0, Width, Height);
        g.DrawLine(Color.FromArgb(180, 180, 180), 0, Height - 1, Width, Height - 1);

        var font = Font ?? Font.Default;
        int x = 4;

        foreach (var item in _items)
        {
            g.DrawString(item.Text, font, ForeColor, x, (Height - (int)font.Size) / 2);
            x += (item.Text.Length + 2) * (int)font.Size / 2 + 10;
        }

        base.Render(g);
    }
}

public class ToolStripMenuItem : Component
{
    private string _text = string.Empty;
    private bool _isSelected;
    private bool _isDropDownVisible;
    private readonly List<ToolStripMenuItem> _dropDownItems = new();
    private int _width;

    public ToolStripMenuItem(string text)
    {
        _text = text;
    }

    public string Text
    {
        get => _text;
        set => _text = value;
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => _isSelected = value;
    }

    public bool IsDropDownVisible
    {
        get => _isDropDownVisible;
        set => _isDropDownVisible = value;
    }

    public List<ToolStripMenuItem> DropDownItems => _dropDownItems;

    public event EventHandler? Click;

    public void OnClick()
    {
        Click?.Invoke(this, EventArgs.Empty);
    }
}

public class ToolStrip : ContainerControl
{
    private readonly List<ToolStripItem> _items = new();

    public ToolStrip()
    {
        Size = new Size(400, 28);
        BackColor = SystemColors.Control;
    }

    public List<ToolStripItem> Items => _items;

    public override void Render(Graphics g)
    {
        if (!Visible) return;

        g.FillRectangle(BackColor, 0, 0, Width, Height);
        g.DrawLine(Color.FromArgb(180, 180, 180), 0, Height - 1, Width, Height - 1);

        int x = 2;
        var font = Font ?? Font.Default;

        foreach (var item in _items)
        {
            if (item is ToolStripButton button)
            {
                if (button.Enabled)
                {
                    var bgColor = button.IsPressed ? Color.FromArgb(180, 180, 180) :
                                  button.IsHovered ? Color.FromArgb(220, 220, 220) : BackColor;
                    g.FillRectangle(bgColor, x, 2, button.Width, Height - 3);
                }

                g.DrawRectangle(Color.FromArgb(150, 150, 150), x, 2, button.Width, Height - 3, 1);
                x += button.Width + 1;
            }
            else if (item is ToolStripSeparator)
            {
                g.DrawLine(Color.FromArgb(180, 180, 180), x + 2, 4, x + 2, Height - 4);
                x += 4;
            }
        }

        base.Render(g);
    }
}

public class ToolStripItem : Component
{
    public string Text { get; set; } = string.Empty;
    public int Width { get; set; } = 24;
    public bool Enabled { get; set; } = true;
    public bool IsHovered { get; set; }
    public bool IsPressed { get; set; }
}

public class ToolStripButton : ToolStripItem
{
    public bool IsToggle { get; set; }
    public bool Checked { get; set; }
}

public class ToolStripSeparator : ToolStripItem
{
}