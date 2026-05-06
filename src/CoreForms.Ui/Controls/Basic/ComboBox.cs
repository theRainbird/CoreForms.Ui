using CoreForms.Ui.Core;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Basic;

/// <summary>
/// A control that presents a drop-down list of items.
/// </summary>
public class ComboBox : Control
{
    private readonly List<object> _items = new();
    private int _selectedIndex = -1;
    private bool _droppedDown;
    private int _dropDownHeight = 120;
    private int _scrollOffset;

    /// <summary>
    /// Initializes a new instance of ComboBox.
    /// </summary>
    public ComboBox()
    {
        BackColor = Color.White;
        Size = new Size(200, 32);
        TabStop = true;
    }

    /// <summary>
    /// Gets the collection of items in the combo box.
    /// </summary>
    public List<object> Items => _items;

    /// <summary>
    /// Gets or sets the index of the selected item.
    /// </summary>
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

    /// <summary>
    /// Gets the selected item.
    /// </summary>
    public object? SelectedItem => _selectedIndex >= 0 && _selectedIndex < _items.Count
        ? _items[_selectedIndex]
        : null;

    /// <summary>
    /// Gets or sets the height of the drop-down list.
    /// </summary>
    public int DropDownHeight
    {
        get => _dropDownHeight;
        set => _dropDownHeight = value;
    }

    /// <summary>
    /// Renders the combo box with its text and dropdown button.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
    public override void Render(Graphics g)
    {
        if (!Visible) return;

        g.FillRectangle(BackColor, 0, 0, Width, Height);

        var font = Font ?? Font.Default;
        var selectedText = SelectedItem?.ToString() ?? "";
        var btnWidth = 17;
        var btnX = Width - btnWidth;

        g.DrawString(selectedText, font, ForeColor, 3, (Height - (int)font.Size) / 2);

        g.FillRectangle(SystemColors.Control, btnX, 1, btnWidth, Height - 2);
        g.DrawLine(Color.FromArgb(128, 128, 128), btnX, 0, btnX, Height);

        if (Focused)
            g.DrawRectangle(Color.FromArgb(0, 120, 215), 0, 0, Width, Height, 2);
        else
            g.DrawRectangle(Color.FromArgb(128, 128, 128), 0, 0, Width, Height, 1);

        var cx = btnX + btnWidth / 2;
        var cy = Height / 2;
        var tw = 4;
        var th = 3;
        g.FillTriangle(Color.FromArgb(80, 80, 80),
            cx - tw, cy - th,
            cx + tw, cy - th,
            cx, cy + th);

        base.Render(g);
    }

    /// <summary>
    /// Renders the dropdown list when visible.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
    public override void RenderOverlay(Graphics g)
    {
        if (!Visible || !_droppedDown) return;

        var font = Font ?? Font.Default;
        var itemHeight = (int)font.Size + 4;
        var totalHeight = _items.Count * itemHeight;
        var scrollBarWidth = 16;
        var needsScrollbar = totalHeight > _dropDownHeight;
        var listWidth = needsScrollbar ? Width - scrollBarWidth : Width;
        var dropY = Height;

        g.FillRectangle(Color.White, 0, dropY, Width, _dropDownHeight);
        g.DrawRectangle(Color.FromArgb(128, 128, 128), 0, dropY, Width, _dropDownHeight, 1);

        if (needsScrollbar)
        {
            int scrollBarX = Width - scrollBarWidth;

            g.FillRectangle(Color.FromArgb(240, 240, 240), scrollBarX, dropY, scrollBarWidth, _dropDownHeight);
            g.DrawRectangle(Color.FromArgb(180, 180, 180), scrollBarX, dropY, scrollBarWidth, _dropDownHeight, 1);

            int maxScroll = totalHeight - _dropDownHeight + 4;
            float thumbHeightRatio = (float)_dropDownHeight / totalHeight;
            int thumbHeight = Math.Max(20, (int)(_dropDownHeight * thumbHeightRatio));
            int thumbY = dropY + (maxScroll > 0 ? (int)((float)_scrollOffset / maxScroll * (_dropDownHeight - thumbHeight)) : 0);

            g.FillRectangle(Color.FromArgb(190, 190, 190), scrollBarX + 2, thumbY, scrollBarWidth - 4, thumbHeight);
            g.DrawRectangle(Color.FromArgb(150, 150, 150), scrollBarX + 2, thumbY, scrollBarWidth - 4, thumbHeight, 1);
        }

        g.SetClip(new Rectangle(0, dropY, listWidth, _dropDownHeight));

        for (int i = 0; i < _items.Count; i++)
        {
            var y = dropY + 2 + i * itemHeight - _scrollOffset;

            if (y + itemHeight <= dropY) continue;
            if (y >= dropY + _dropDownHeight) break;

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

        g.ResetClip();

        base.RenderOverlay(g);
    }

    /// <summary>
    /// Tests whether the specified point is within the control bounds (including dropdown).
    /// </summary>
    /// <param name="point">The point to test.</param>
    /// <returns>True if the point is within bounds; otherwise, false.</returns>
    public override bool HitTest(Point point)
    {
        if (Bounds.Contains(point))
            return true;

        if (_droppedDown)
        {
            var dropBounds = new Rectangle(X, Y + Height, Width, _dropDownHeight);
            if (dropBounds.Contains(point))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Raises the LostFocus event and closes the dropdown.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnLostFocus(EventArgs e)
    {
        if (_droppedDown)
        {
            _droppedDown = false;
            _scrollOffset = 0;
            CapturingMouse = false;
            Invalidate();
        }
        base.OnLostFocus(e);
    }

    /// <summary>
    /// Raises the MouseDown event to handle clicks.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseDown(EventArgs e)
    {
        if (_droppedDown)
        {
            var args = e as MouseEventArgs;
            if (args != null)
            {
                var font = Font ?? Font.Default;
                var itemHeight = (int)font.Size + 4;
                int dropY = Height;
                int clickY = args.Y - dropY - 2 + _scrollOffset;

                if (clickY >= 0 && args.Y >= dropY)
                {
                    int index = clickY / itemHeight;
                    if (index >= 0 && index < _items.Count)
                    {
                        SelectedIndex = index;
                    }
                }
            }

            _droppedDown = false;
            _scrollOffset = 0;
            CapturingMouse = false;
            Invalidate();
            return;
        }

        var mouseArgs = e as MouseEventArgs;
        if (mouseArgs != null && mouseArgs.X >= Width - 17)
        {
            _droppedDown = true;
            CapturingMouse = true;
            EnsureSelectedVisible();
            Invalidate();
        }

        base.OnMouseDown(e);
    }

    /// <summary>
    /// Raises the MouseWheel event to handle scrolling.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseWheel(EventArgs e)
    {
        if (_droppedDown)
        {
            var args = e as MouseEventArgs;
            if (args != null)
            {
                var font = Font ?? Font.Default;
                var itemHeight = (int)font.Size + 4;
                int delta = -args.Delta * itemHeight;
                ScrollBy(delta);
            }
            return;
        }

        base.OnMouseWheel(e);
    }

    /// <summary>
    /// Raises the KeyDown event to handle keyboard navigation.
    /// </summary>
    /// <param name="e">A KeyEventArgs that contains the event data.</param>
    protected internal override void OnKeyDown(KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.F4:
                _droppedDown = !_droppedDown;
                if (_droppedDown)
                {
                    CapturingMouse = true;
                    EnsureSelectedVisible();
                }
                else
                {
                    _scrollOffset = 0;
                    CapturingMouse = false;
                }
                Invalidate();
                e.Handled = true;
                break;
            case Keys.Down:
                if (_droppedDown)
                {
                    if (_selectedIndex < _items.Count - 1)
                    {
                        SelectedIndex++;
                        EnsureSelectedVisible();
                    }
                }
                else if (_selectedIndex < _items.Count - 1)
                {
                    SelectedIndex++;
                }
                e.Handled = true;
                break;
            case Keys.Up:
                if (_droppedDown)
                {
                    if (_selectedIndex > 0)
                    {
                        SelectedIndex--;
                        EnsureSelectedVisible();
                    }
                }
                else if (_selectedIndex > 0)
                {
                    SelectedIndex--;
                }
                e.Handled = true;
                break;
            case Keys.Enter:
                if (_droppedDown)
                {
                    _droppedDown = false;
                    _scrollOffset = 0;
                    CapturingMouse = false;
                    Invalidate();
                }
                e.Handled = true;
                break;
            case Keys.Escape:
                if (_droppedDown)
                {
                    _droppedDown = false;
                    _scrollOffset = 0;
                    CapturingMouse = false;
                    Invalidate();
                }
                e.Handled = true;
                break;
        }
        base.OnKeyDown(e);
    }

    private void ScrollBy(int delta)
    {
        var font = Font ?? Font.Default;
        var itemHeight = (int)font.Size + 4;
        int totalHeight = _items.Count * itemHeight;
        int maxScroll = Math.Max(0, totalHeight - _dropDownHeight + 4);

        _scrollOffset = Math.Max(0, Math.Min(maxScroll, _scrollOffset + delta));
        Invalidate();
    }

    private void EnsureSelectedVisible()
    {
        if (_selectedIndex < 0) return;

        var font = Font ?? Font.Default;
        var itemHeight = (int)font.Size + 4;
        int totalHeight = _items.Count * itemHeight;
        int maxScroll = Math.Max(0, totalHeight - _dropDownHeight + 4);

        int selTop = _selectedIndex * itemHeight;
        int selBottom = selTop + itemHeight;

        if (_scrollOffset > selTop)
            _scrollOffset = selTop;
        else if (_scrollOffset + _dropDownHeight - 4 < selBottom)
            _scrollOffset = Math.Min(maxScroll, selBottom - _dropDownHeight + 4);

        Invalidate();
    }

    /// <summary>
    /// Raises the SelectedIndexChanged event.
    /// </summary>
    protected virtual void OnSelectedIndexChanged()
    {
        SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Occurs when the selected index changes.
    /// </summary>
    public event EventHandler? SelectedIndexChanged;
}