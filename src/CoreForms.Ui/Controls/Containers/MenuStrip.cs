using CoreForms.Ui.Core;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Containers;

/// <summary>
/// A menu strip that displays a menu bar with menu items and supports
/// click detection and dropdown menus.
/// </summary>
public class MenuStrip : ContainerControl
{
    private readonly List<ToolStripMenuItem> _items = new();
    private ToolStripMenuItem? _hoverItem;
    private bool _dropDownVisible;
    private ToolStripMenuItem? _openItem;

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
    /// Gets the height of dropdown items.
    /// </summary>
    protected virtual int DropDownItemHeight => 22;

    /// <summary>
    /// Renders the menu strip with its items and optional dropdown.
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
            int textWidth = (item.Text.Length + 2) * (int)font.Size / 2 + 10;
            bool isHovered = item == _hoverItem || item == _openItem;

            if (isHovered)
            {
                g.FillRectangle(Color.FromArgb(200, 200, 200), x, 0, textWidth, Height);
            }

            g.DrawString(item.Text, font, ForeColor, x + 5, (Height - (int)font.Size) / 2);
            x += textWidth;
        }

        base.Render(g);
    }

    /// <summary>
    /// Renders the dropdown overlay on top of other controls.
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

            int maxWidth = 0;
            foreach (var ddItem in _openItem.DropDownItems)
            {
                int w = ddItem.Text.Length * (int)font.Size / 2 + 30;
                if (w > maxWidth) maxWidth = w;
            }

            int dropDownHeight = _openItem.DropDownItems.Count * DropDownItemHeight + 4;

            g.FillRectangle(Color.White, x, y, maxWidth, dropDownHeight);
            g.DrawRectangle(Color.FromArgb(100, 100, 100), x, y, maxWidth, dropDownHeight, 1);

            int itemY = y + 2;
            foreach (var ddItem in _openItem.DropDownItems)
            {
                bool isHovered = ddItem == _hoverDropDownItem;
                if (isHovered)
                {
                    g.FillRectangle(Color.FromArgb(200, 220, 255), x + 1, itemY, maxWidth - 2, DropDownItemHeight);
                }
                g.DrawString(ddItem.Text, font, ForeColor, x + 8, itemY + (DropDownItemHeight - (int)font.Size) / 2);
                itemY += DropDownItemHeight;
            }
        }
    }

    private ToolStripMenuItem? _hoverDropDownItem;

    /// <summary>
    /// Handles mouse down events to detect menu item clicks and dropdown toggling.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseDown(EventArgs e)
    {
        if (e is MouseEventArgs args)
        {
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
        }

        base.OnMouseDown(e);
    }

    /// <summary>
    /// Handles mouse move events for hover highlighting.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseMove(EventArgs e)
    {
        if (e is MouseEventArgs args)
        {
            _hoverItem = HitTestTopItem(args.X, args.Y);

            if (_dropDownVisible && _openItem != null)
            {
                if (args.Y > Height)
                {
                    _hoverDropDownItem = HitTestDropDown(args.X, args.Y);
                }
                else
                {
                    _hoverDropDownItem = null;
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
            x += (_items[i].Text.Length + 2) * (int)font.Size / 2 + 10;
        }
        return x;
    }

    private ToolStripMenuItem? HitTestTopItem(int x, int y)
    {
        if (y < 0 || y > Height)
            return null;

        var font = Font ?? Font.Default;
        int itemX = 4;

        for (int i = 0; i < _items.Count; i++)
        {
            int itemWidth = (_items[i].Text.Length + 2) * (int)font.Size / 2 + 10;
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

        int maxWidth = 0;
        foreach (var ddItem in _openItem.DropDownItems)
        {
            int w = ddItem.Text.Length * (int)font.Size / 2 + 30;
            if (w > maxWidth) maxWidth = w;
        }

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
    }

    private void CloseDropDown()
    {
        _dropDownVisible = false;
        _openItem = null;
        _hoverDropDownItem = null;
    }
}