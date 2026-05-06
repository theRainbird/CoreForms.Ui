using CoreForms.Ui.Core;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Containers;

/// <summary>
/// A menu strip that displays a menu bar with menu items.
/// </summary>
public class MenuStrip : ContainerControl
{
    private readonly List<ToolStripMenuItem> _items = new();

    /// <summary>
    /// Initializes a new instance of MenuStrip.
    /// </summary>
    public MenuStrip()
    {
        Size = new Size(400, 24);
        BackColor = SystemColors.Control;
    }

    /// <summary>
    /// Gets the collection of menu items.
    /// </summary>
    public List<ToolStripMenuItem> Items => _items;

    /// <summary>
    /// Renders the menu strip with its items.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
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

/// <summary>
/// A toolbar that displays a collection of tool strip items.
/// </summary>
public class ToolStrip : ContainerControl
{
    private readonly List<ToolStripItem> _items = new();

    /// <summary>
    /// Initializes a new instance of ToolStrip.
    /// </summary>
    public ToolStrip()
    {
        Size = new Size(400, 28);
        BackColor = SystemColors.Control;
    }

    /// <summary>
    /// Gets the collection of tool strip items.
    /// </summary>
    public List<ToolStripItem> Items => _items;

    /// <summary>
    /// Renders the tool strip with its items.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
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

/// <summary>
/// Base class for items in a ToolStrip.
/// </summary>
public class ToolStripItem : Component
{
    /// <summary>
    /// Gets or sets the text of the item.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the width of the item.
    /// </summary>
    public int Width { get; set; } = 24;

    /// <summary>
    /// Gets or sets whether the item is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the mouse is hovering over the item.
    /// </summary>
    public bool IsHovered { get; set; }

    /// <summary>
    /// Gets or sets whether the item is pressed.
    /// </summary>
    public bool IsPressed { get; set; }
}

/// <summary>
/// Represents a button in a ToolStrip.
/// </summary>
public class ToolStripButton : ToolStripItem
{
    /// <summary>
    /// Gets or sets whether the button acts as a toggle button.
    /// </summary>
    public bool IsToggle { get; set; }

    /// <summary>
    /// Gets or sets whether the button is checked.
    /// </summary>
    public bool Checked { get; set; }
}

/// <summary>
/// Represents a separator in a ToolStrip.
/// </summary>
public class ToolStripSeparator : ToolStripItem
{
}