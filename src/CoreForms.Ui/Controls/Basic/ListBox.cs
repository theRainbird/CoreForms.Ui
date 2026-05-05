using CoreForms.Ui.Core;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Basic;

public class ListBox : Control
{
    private readonly List<object> _items = new();
    private int _selectedIndex = -1;

    public ListBox()
    {
        BackColor = Color.White;
        Size = new Size(150, 120);
    }

    public List<object> Items => _items;

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (_selectedIndex != value && value >= -1 && value < _items.Count)
            {
                _selectedIndex = value;
                OnSelectedIndexChanged();
                Invalidate();
            }
        }
    }

    public object? SelectedItem => _selectedIndex >= 0 && _selectedIndex < _items.Count
        ? _items[_selectedIndex]
        : null;

    public override void Render(Rendering.Graphics g)
    {
        if (!Visible) return;

        g.FillRectangle(BackColor, 0, 0, Width, Height);
        g.DrawRectangle(Color.FromArgb(128, 128, 128), 0, 0, Width, Height, 1);

        var font = Font ?? Font.Default;
        var itemHeight = (int)font.Size + 4;
        var y = 2;

        for (int i = 0; i < _items.Count && y < Height; i++)
        {
            var isSelected = i == _selectedIndex;

            if (isSelected)
            {
                g.FillRectangle(SystemColors.Highlight, 1, y, Width - 2, itemHeight);
                g.DrawString(_items[i]?.ToString() ?? "", font, SystemColors.HighlightText, 4, y + 2);
            }
            else
            {
                g.DrawString(_items[i]?.ToString() ?? "", font, ForeColor, 4, y + 2);
            }

            y += itemHeight;
        }

        base.Render(g);
    }

    protected internal override void OnMouseDown(EventArgs e)
    {
        var mouseArgs = e as MouseEventArgs;
        if (mouseArgs != null)
        {
            var font = Font ?? Font.Default;
            var itemHeight = (int)font.Size + 4;
            var index = (mouseArgs.Y - 2) / itemHeight;

            if (index >= 0 && index < _items.Count)
            {
                SelectedIndex = index;
            }
        }

        base.OnMouseDown(e);
    }

    protected virtual void OnSelectedIndexChanged()
    {
        SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? SelectedIndexChanged;
}