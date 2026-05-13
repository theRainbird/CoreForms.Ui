using CoreForms.Ui.Core;
using CoreForms.Ui.Theming;
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
    private int _hoveredIndex = -1;

    /// <summary>
    /// Initializes a new instance of ComboBox.
    /// </summary>
    public ComboBox()
    {
        var theme = ThemeManager.CurrentTheme;
        _backColor = theme.TextBoxBackground;
        Size = new Size(200, 32);
        TabStop = true;
        TextAlign = ContentAlignment.MiddleLeft;
    }

    /// <summary>
    /// Called when the theme changes. Updates combobox-specific colors.
    /// </summary>
    /// <param name="newTheme">The new theme that was activated.</param>
    public override void OnThemeChanged(Theme newTheme)
    {
        if (!_backColorSet)
            _backColor = newTheme.TextBoxBackground;
        Invalidate();
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

        var theme = ThemeManager.CurrentTheme;

        g.FillRectangle(BackColor, 0, 0, Width, Height);

        if (Focused)
            g.DrawRectangle(theme.TextBoxFocusBorder, 0, 0, Width, Height, 2);
        else
            g.DrawRectangle(theme.TextBoxBorder, 0, 0, Width, Height, 1);

        var font = EffectiveFont;
        float zoom = EffectiveZoom;
        float scaledFontSize = font.Size * zoom;
        var selectedText = SelectedItem?.ToString() ?? "";
        var btnWidth = 17;
        var btnX = Width - btnWidth;
        var textAreaWidth = Width - btnWidth;
        var textSize = Platform.Platform.MeasureText(selectedText, font, zoom);
        var padding = 3;

        var textX = TextAlign switch
        {
            ContentAlignment.TopLeft or ContentAlignment.MiddleLeft or ContentAlignment.BottomLeft => padding,
            ContentAlignment.TopCenter or ContentAlignment.MiddleCenter or ContentAlignment.BottomCenter => (textAreaWidth - textSize.width) / 2,
            ContentAlignment.TopRight or ContentAlignment.MiddleRight or ContentAlignment.BottomRight => textAreaWidth - textSize.width - padding,
            _ => padding
        };
        var textY = TextAlign switch
        {
            ContentAlignment.TopLeft or ContentAlignment.TopCenter or ContentAlignment.TopRight => padding,
            ContentAlignment.MiddleLeft or ContentAlignment.MiddleCenter or ContentAlignment.MiddleRight => (Height - scaledFontSize) / 2,
            ContentAlignment.BottomLeft or ContentAlignment.BottomCenter or ContentAlignment.BottomRight => Height - scaledFontSize - padding,
            _ => (Height - scaledFontSize) / 2
        };

        g.DrawString(selectedText, font, ForeColor, textX, textY);

        g.FillRectangle(theme.ControlBackground, btnX, 1, btnWidth, Height - 2);
        g.DrawLine(theme.ComboBoxDropdownButtonSeparator, btnX, 0, btnX, Height);

        DrawFocusIndicator(g);

        var cx = btnX + btnWidth / 2;
        var cy = Height / 2;
        var tw = 4;
        var th = 3;
        g.FillTriangle(theme.ComboBoxDropdownArrow,
            cx - tw, cy - th,
            cx + tw, cy - th,
            cx, cy + th);

        base.Render(g);
    }

    private int GetItemHeight()
    {
        var font = EffectiveFont;
        return CoordinateTransform.GetItemHeight(font, EffectiveZoom);
    }

    /// <summary>
    /// Renders the dropdown list below the combo box when visible.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
    public override void RenderOverlay(Graphics g)
    {
        if (!Visible) return;

        base.RenderOverlay(g);

        if (!_droppedDown) return;

        var theme = ThemeManager.CurrentTheme;
        var font = EffectiveFont;
        var itemHeight = GetItemHeight();
        var totalHeight = _items.Count * itemHeight;
        var scrollBarWidth = 16;
        var needsScrollbar = totalHeight > _dropDownHeight;
        var listWidth = needsScrollbar ? Width - scrollBarWidth : Width;
        var dropY = Height;

        g.FillRectangle(theme.MenuDropdownBackground, 0, dropY, Width, _dropDownHeight);
        g.DrawRectangle(theme.MenuDropdownBorder, 0, dropY, Width, _dropDownHeight, 1);

        if (needsScrollbar)
        {
            int scrollBarX = Width - scrollBarWidth;

            g.FillRectangle(theme.ScrollbarTrack, scrollBarX, dropY, scrollBarWidth, _dropDownHeight);
            g.DrawRectangle(theme.ScrollbarBorder, scrollBarX, dropY, scrollBarWidth, _dropDownHeight, 1);

            int maxScroll = totalHeight - _dropDownHeight + 4;
            float thumbHeightRatio = (float)_dropDownHeight / totalHeight;
            int thumbHeight = Math.Max(20, (int)(_dropDownHeight * thumbHeightRatio));
            int thumbY = dropY + (maxScroll > 0 ? (int)((float)_scrollOffset / maxScroll * (_dropDownHeight - thumbHeight)) : 0);

            g.FillRectangle(theme.ScrollbarThumb, scrollBarX + 2, thumbY, scrollBarWidth - 4, thumbHeight);
            g.DrawRectangle(theme.ScrollbarThumbBorder, scrollBarX + 2, thumbY, scrollBarWidth - 4, thumbHeight, 1);
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
            else if (i == _hoveredIndex)
            {
                g.FillRectangle(theme.HoverHighlight, 1, y, listWidth - 2, itemHeight);
                g.DrawString(_items[i]?.ToString() ?? "", font, ForeColor, 4, y + 2);
            }
            else
            {
                g.DrawString(_items[i]?.ToString() ?? "", font, ForeColor, 4, y + 2);
            }
        }

        g.ResetClip();
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
            _hoveredIndex = -1;
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
        if (!Focused)
        {
            Focused = true;
        }

        if (_droppedDown)
        {
            var args = e as MouseEventArgs;
            if (args != null)
            {
                var itemHeight = GetItemHeight();
                int dropY = Height;
                int dropDownHeight = _dropDownHeight;

                if (args.X >= 0 && args.X < Width && args.Y >= dropY && args.Y < dropY + dropDownHeight)
                {
                    int localY = args.Y - dropY - 2 + _scrollOffset;
                    if (localY >= 0)
                    {
                        int index = localY / itemHeight;
                        if (index >= 0 && index < _items.Count)
                        {
                            SelectedIndex = index;
                        }
                    }

                    _droppedDown = false;
                    _scrollOffset = 0;
                    _hoveredIndex = -1;
                    Invalidate();
                    return;
                }
            }

            _droppedDown = false;
            _scrollOffset = 0;
            _hoveredIndex = -1;
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
    }

    /// <summary>
    /// Raises the MouseUp event to release mouse capture after dropdown closes.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseUp(EventArgs e)
    {
        if (!_droppedDown && CapturingMouse)
        {
            CapturingMouse = false;
            return;
        }
        base.OnMouseUp(e);
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
                var itemHeight = GetItemHeight();
                int delta = args.Delta * itemHeight;
                ScrollBy(delta);
            }
            return;
        }

        base.OnMouseWheel(e);
    }

    /// <summary>
    /// Raises the MouseMove event to track hover state in dropdown.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseMove(EventArgs e)
    {
        if (_droppedDown)
        {
            var args = e as MouseEventArgs;
            if (args != null)
            {
                var itemHeight = GetItemHeight();
                int dropY = Height;

                if (args.Y >= dropY && args.Y < dropY + _dropDownHeight)
                {
                    int localY = args.Y - dropY - 2 + _scrollOffset;
                    if (localY >= 0)
                    {
                        int index = localY / itemHeight;
                        if (index >= 0 && index < _items.Count)
                        {
                            if (_hoveredIndex != index)
                            {
                                _hoveredIndex = index;
                                Invalidate();
                            }
                        }
                        else
                        {
                            if (_hoveredIndex != -1)
                            {
                                _hoveredIndex = -1;
                                Invalidate();
                            }
                        }
                    }
                    else
                    {
                        if (_hoveredIndex != -1)
                        {
                            _hoveredIndex = -1;
                            Invalidate();
                        }
                    }
                }
                else
                {
                    if (_hoveredIndex != -1)
                    {
                        _hoveredIndex = -1;
                        Invalidate();
                    }
                }
            }
            return;
        }

        base.OnMouseMove(e);
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
        var itemHeight = GetItemHeight();
        int totalHeight = _items.Count * itemHeight;
        int maxScroll = Math.Max(0, totalHeight - _dropDownHeight + 4);

        _scrollOffset = Math.Max(0, Math.Min(maxScroll, _scrollOffset + delta));
        Invalidate();
    }

    private void EnsureSelectedVisible()
    {
        if (_selectedIndex < 0) return;

        var itemHeight = GetItemHeight();
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
    /// Gets the value of this control to copy to the clipboard.
    /// Returns the selected item's text.
    /// </summary>
    /// <returns>The selected item as string, or null if no selection.</returns>
    protected string? GetClipboardValue() => SelectedItem?.ToString();

    /// <summary>
    /// Occurs when the selected index changes.
    /// </summary>
    public event EventHandler? SelectedIndexChanged;
}
