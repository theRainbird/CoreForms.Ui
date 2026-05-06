using CoreForms.Ui.Core;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Basic;

public class ComboBox : Control
{
    private readonly List<object> _items = new();
    private int _selectedIndex = -1;
    private bool _droppedDown;
    private int _dropDownHeight = 120;

    public ComboBox()
    {
        BackColor = Color.White;
        Size = new Size(200, 32);
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

    public int DropDownHeight
    {
        get => _dropDownHeight;
        set => _dropDownHeight = value;
    }

    public override void Render(Graphics g)
    {
        if (!Visible) return;

        g.FillRectangle(BackColor, 0, 0, Width, Height);

        if (Focused)
            g.DrawRectangle(SystemColors.Highlight, 0, 0, Width, Height, 2);
        else
            g.DrawRectangle(Color.FromArgb(128, 128, 128), 0, 0, Width, Height, 1);

        var font = Font ?? Font.Default;
        var selectedText = SelectedItem?.ToString() ?? "";
        g.DrawString(selectedText, font, ForeColor, 3, (Height - (int)font.Size) / 2);

        g.FillRectangle(SystemColors.Control, Width - 20, 0, 20, Height);
        // Draw downward-pointing arrow
        g.DrawLine(Color.FromArgb(100, 100, 100), Width - 13, Height / 3, Width - 5, Height * 2 / 3);
        g.DrawLine(Color.FromArgb(100, 100, 100), Width - 5, Height * 2 / 3, Width + 3, Height / 3);

        if (_droppedDown)
        {
            // Clear dropdown area first
            g.FillRectangle(Color.White, 0, Height, Width, _dropDownHeight);
            g.DrawRectangle(Color.FromArgb(128, 128, 128), 0, Height, Width, _dropDownHeight, 1);

            var itemHeight = (int)font.Size + 4;
            int totalHeight = _items.Count * itemHeight;
            int scrollBarWidth = 16;
            int listWidth = Width - (totalHeight > _dropDownHeight ? scrollBarWidth : 0);
            
            // Draw scrollbar if needed
            if (totalHeight > _dropDownHeight)
            {
                int scrollBarX = Width - scrollBarWidth;
                int scrollBarY = Height;
                int scrollBarHeight = _dropDownHeight;
                
                // Scrollbar background
                g.FillRectangle(Color.FromArgb(240, 240, 240), scrollBarX, scrollBarY, scrollBarWidth, scrollBarHeight);
                g.DrawRectangle(Color.FromArgb(180, 180, 180), scrollBarX, scrollBarY, scrollBarWidth, scrollBarHeight, 1);
                
                // Scroll thumb
                float thumbHeightRatio = (float)_dropDownHeight / totalHeight;
                int thumbHeight = Math.Max(20, (int)(scrollBarHeight * thumbHeightRatio));
                int thumbY = scrollBarY; // Simplified - no scroll offset yet
                
                g.FillRectangle(Color.FromArgb(190, 190, 190), scrollBarX + 2, thumbY, scrollBarWidth - 4, thumbHeight);
                g.DrawRectangle(Color.FromArgb(150, 150, 150), scrollBarX + 2, thumbY, scrollBarWidth - 4, thumbHeight, 1);
            }
            
            for (int i = 0; i < _items.Count; i++)
            {
                var y = Height + 2 + i * itemHeight;
                if (y > Height + _dropDownHeight) break;
                
                if (i == _selectedIndex)
                {
                    g.FillRectangle(SystemColors.Highlight, 1, y, listWidth - 2, itemHeight);
                    g.DrawString(_items[i]?.ToString() ?? "", font, SystemColors.HighlightText, 4, y + 2);
                }
                else
                {
                    g.DrawString(_items[i]?.ToString() ?? "", font, ForeColor, 4, y + 2);
                }
            }
        }

        base.Render(g);
    }

    protected internal override void OnMouseDown(EventArgs e)
    {
        _droppedDown = !_droppedDown;
        Invalidate();
        base.OnMouseDown(e);
    }

    protected internal override void OnKeyDown(KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Down:
                if (_selectedIndex < _items.Count - 1)
                {
                    SelectedIndex++;
                    e.Handled = true;
                }
                break;
            case Keys.Up:
                if (_selectedIndex > 0)
                {
                    SelectedIndex--;
                    e.Handled = true;
                }
                break;
            case Keys.Enter:
            case Keys.Space:
                _droppedDown = !_droppedDown;
                Invalidate();
                e.Handled = true;
                break;
            case Keys.Escape:
                _droppedDown = false;
                Invalidate();
                e.Handled = true;
                break;
        }
        base.OnKeyDown(e);
    }

    protected virtual void OnSelectedIndexChanged()
    {
        SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? SelectedIndexChanged;
}