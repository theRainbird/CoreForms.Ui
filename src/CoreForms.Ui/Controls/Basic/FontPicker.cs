using CoreForms.Ui.Core;
using CoreForms.Ui.Theming;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Basic;

/// <summary>
/// A control for selecting a font from all system-installed fonts.
/// Displays a two-column dropdown with font names (in the system font)
/// and a live preview (in the actual font). Supports keyboard search/filter.
/// Font enumeration uses SKFontManager (lazy, memory-efficient) instead of
/// filesystem scanning.
/// </summary>
public class FontPicker : Control
{
    private string[] _filteredFonts = Array.Empty<string>();
    private int _selectedIndex = -1;
    private int _hoveredIndex = -1;
    private bool _droppedDown;
    private int _scrollOffset;
    private readonly ScrollBarEngine _scrollBar = new();
    private FontPickerScrollBarContext? _scrollBarContext;
    private string _filterText = string.Empty;
    private string _selectedFontFamily = string.Empty;
    private float _fontSize = 14f;
    private string _previewText = "The quick brown fox jumps over the lazy dog";
    private int _dropDownHeight = 200;

    private const int DropdownButtonWidth = 17;
    private const int NameColumnWidth = 150;
    private const int PreviewColumnWidth = 220;
    private const int DropdownMinWidth = 380;
    private const int ColumnGap = 8;

    private FontPickerScrollBarContext ScrollBarCtx =>
        _scrollBarContext ??= new FontPickerScrollBarContext(this);

    /// <summary>
    /// Initializes a new instance of the <see cref="FontPicker"/> class.
    /// </summary>
    public FontPicker()
    {
        var theme = ThemeManager.CurrentTheme;
        _backColor = theme.TextBoxBackground;
        Size = new Size(220, 32);
        TabStop = true;
        _scrollBar.Scroll += (s, e) =>
        {
            _scrollOffset = _scrollBar.Value;
            Invalidate();
        };
        RefreshFontList();
    }

    /// <summary>
    /// Gets or sets the selected font family name.
    /// </summary>
    public string SelectedFontFamily
    {
        get => _selectedFontFamily;
        set
        {
            if (_selectedFontFamily != value)
            {
                _selectedFontFamily = value ?? string.Empty;
                _selectedIndex = FindFontIndex(_selectedFontFamily);
                Invalidate();
                OnSelectedFontChanged();
                OnPropertyChanged(nameof(SelectedFontFamily));
            }
        }
    }

    /// <summary>
    /// Gets or sets the font size used for the preview column.
    /// </summary>
    public float FontSize
    {
        get => _fontSize;
        set
        {
            if (_fontSize != value && value > 0)
            {
                _fontSize = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets the text displayed in the preview column for each item.
    /// </summary>
    public string PreviewText
    {
        get => _previewText;
        set
        {
            _previewText = value ?? string.Empty;
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the height of the dropdown list.
    /// </summary>
    public int DropDownHeight
    {
        get => _dropDownHeight;
        set => _dropDownHeight = Math.Max(20, value);
    }

    /// <summary>
    /// Gets or sets whether the dropdown is currently open.
    /// </summary>
    public bool DroppedDown
    {
        get => _droppedDown;
        set
        {
            if (_droppedDown != value)
            {
                _droppedDown = value;
                if (_droppedDown)
                {
                    CapturingMouse = true;
                    EnsureSelectedVisible();
                }
                else
                {
                    _filterText = string.Empty;
                    _scrollOffset = 0;
                    _scrollBar.ScrollTo(0);
                    _hoveredIndex = -1;
                    CapturingMouse = false;
                }
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Occurs when the selected font changes.
    /// </summary>
    public event EventHandler? SelectedFontChanged;

    /// <summary>
    /// Refreshes the font list from the system. Call this if fonts are installed/uninstalled at runtime.
    /// </summary>
    public void RefreshFontList()
    {
        FontManager.RefreshFontList();
        ApplyFilter();
    }

    /// <summary>
    /// Raises the <see cref="SelectedFontChanged"/> event.
    /// </summary>
    protected virtual void OnSelectedFontChanged()
    {
        SelectedFontChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Called when the theme changes.
    /// </summary>
    /// <param name="newTheme">The new theme.</param>
    public override void OnThemeChanged(Theme newTheme)
    {
        if (!_backColorSet)
            _backColor = newTheme.TextBoxBackground;
        if (!_foreColorSet)
            _foreColor = newTheme.ControlText;
        Invalidate();
    }

    /// <summary>
    /// Renders the font picker control.
    /// </summary>
    /// <param name="g">The graphics object.</param>
    public override void Render(Graphics g)
    {
        if (!Visible) return;

        var theme = ThemeManager.CurrentTheme;

        g.FillRectangle(BackColor, 0, 0, Width, Height);

        if (Focused)
            g.DrawRectangle(theme.TextBoxFocusBorder, 0, 0, Width, Height, 2);
        else
            g.DrawRectangle(theme.TextBoxBorder, 0, 0, Width, Height, 1);

        if (Enabled)
            DrawFocusIndicator(g);

        RenderTextArea(g, theme);
        RenderDropdownButton(g, theme);

        base.Render(g);
    }

    private void RenderTextArea(Graphics g, Theme theme)
    {
        var font = EffectiveFont;
        float zoom = EffectiveZoom;
        float scaledFontSize = font.Size * zoom;

        bool hasFilter = !string.IsNullOrEmpty(_filterText);
        var displayText = hasFilter ? _filterText : _selectedFontFamily;
        var textColor = hasFilter ? theme.GrayText : (Enabled ? ForeColor : theme.GrayText);

        float textY = (Height - scaledFontSize) / 2;
        g.DrawString(displayText, font, textColor, 3, textY);
    }

    private void RenderDropdownButton(Graphics g, Theme theme)
    {
        var btnX = Width - DropdownButtonWidth;

        g.FillRectangle(theme.ControlBackground, btnX, 1, DropdownButtonWidth, Height - 2);
        g.DrawLine(theme.ComboBoxDropdownButtonSeparator, btnX, 0, btnX, Height);

        var cx = btnX + DropdownButtonWidth / 2;
        var cy = Height / 2;
        var tw = 4;
        var th = 3;
        g.FillTriangle(theme.ComboBoxDropdownArrow,
            cx - tw, cy - th,
            cx + tw, cy - th,
            cx, cy + th);
    }

    /// <summary>
    /// Renders the dropdown overlay with font list.
    /// </summary>
    /// <param name="g">The graphics object.</param>
    public override void RenderOverlay(Graphics g)
    {
        if (!Visible) return;
        base.RenderOverlay(g);

        if (!_droppedDown) return;

        var theme = ThemeManager.CurrentTheme;
        var itemHeight = GetItemHeight();
        var totalHeight = _filteredFonts.Length * itemHeight;
        var scrollBarWidth = ScrollBarEngine.DefaultScrollBarSize;
        var dropDownWidth = GetDropDownWidth();
        var needsScrollbar = totalHeight > _dropDownHeight;
        var listWidth = needsScrollbar ? dropDownWidth - scrollBarWidth : dropDownWidth;
        int dropY = Height;

        g.FillRectangle(theme.MenuDropdownBackground, 0, dropY, dropDownWidth, _dropDownHeight);
        g.DrawRectangle(theme.MenuDropdownBorder, 0, dropY, dropDownWidth, _dropDownHeight, 1);

        _scrollBar.SmallChange = itemHeight;
        _scrollBar.ViewSize = _dropDownHeight;
        _scrollBar.ContentSize = totalHeight;
        if (needsScrollbar)
        {
            var scrollBarBounds = new Rectangle(dropDownWidth - scrollBarWidth, dropY, scrollBarWidth, _dropDownHeight);
            _scrollBar.Render(g, scrollBarBounds, theme);
        }

        g.SetClip(new Rectangle(0, dropY, listWidth, _dropDownHeight));

        for (int i = 0; i < _filteredFonts.Length; i++)
        {
            var y = dropY + 2 + i * itemHeight - _scrollOffset;

            if (y + itemHeight <= dropY) continue;
            if (y >= dropY + _dropDownHeight) break;

            var isSelected = i == _selectedIndex;
            var isHovered = i == _hoveredIndex;

            Color textColor;
            if (isSelected)
            {
                g.FillRectangle(theme.Highlight, 1, y, listWidth - 2, itemHeight);
                textColor = theme.HighlightText;
            }
            else if (isHovered)
            {
                g.FillRectangle(theme.HoverHighlight, 1, y, listWidth - 2, itemHeight);
                textColor = ForeColor;
            }
            else
            {
                textColor = ForeColor;
            }

            var familyName = _filteredFonts[i];

            // Column 1: font name in system font
            var nameFont = EffectiveFont;
            g.DrawString(familyName, nameFont, isSelected ? theme.HighlightText : ForeColor, 4, y + 2);

            // Column separator line
            int separatorX = 4 + NameColumnWidth + ColumnGap / 2;
            g.DrawLine(theme.GridLineVertical, separatorX, y, separatorX, y + itemHeight);

            // Column 2: preview in the actual font
            // Only load typeface for visible items (lazy, RAM-efficient)
            int col2X = separatorX + ColumnGap / 2 + 2;
            var previewFont = new Font(familyName, FontSize, FontStyle.Regular);
            g.DrawString(PreviewText, previewFont, isSelected ? theme.HighlightText : ForeColor, col2X, y + 2);
        }

        g.ResetClip();
    }

    /// <summary>
    /// Tests whether a point is within the control or dropdown bounds.
    /// </summary>
    public override bool HitTest(Point point)
    {
        if (Bounds.Contains(point))
            return true;

        if (_droppedDown)
        {
            int dropDownWidth = GetDropDownWidth();
            var dropBounds = new Rectangle(X, Y + Height, dropDownWidth, _dropDownHeight);
            if (dropBounds.Contains(point))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Handles mouse down events.
    /// </summary>
    protected internal override void OnMouseDown(EventArgs e)
    {
        if (!Enabled) return;

        if (!Focused)
            Focused = true;

        var args = e as MouseEventArgs;
        if (args == null) return;

        if (_droppedDown)
        {
            var itemHeight = GetItemHeight();
            int dropDownWidth = GetDropDownWidth();
            int dropY = Height;
            int totalHeight = _filteredFonts.Length * itemHeight;
            bool needsScrollbar = _scrollBar.NeedsScrollbar;
            int scrollBarWidth = needsScrollbar ? ScrollBarEngine.DefaultScrollBarSize : 0;

            if (args.X >= 0 && args.X < dropDownWidth && args.Y >= dropY && args.Y < dropY + _dropDownHeight)
            {
                if (needsScrollbar && args.X >= dropDownWidth - scrollBarWidth)
                {
                    var scrollBarBounds = new Rectangle(dropDownWidth - scrollBarWidth, dropY, scrollBarWidth, _dropDownHeight);
                    _scrollBar.HandleMouseDown(new Point(args.X, args.Y), scrollBarBounds, ScrollBarCtx);
                    return;
                }

                int localY = args.Y - dropY - 2 + _scrollOffset;
                if (localY >= 0)
                {
                    int index = localY / itemHeight;
                    if (index >= 0 && index < _filteredFonts.Length)
                    {
                        SelectFontAtIndex(index);
                    }
                }

                _filterText = string.Empty;
                _droppedDown = false;
                _scrollOffset = 0;
                _hoveredIndex = -1;
                _scrollBar.ScrollTo(0);
                CapturingMouse = false;
                Invalidate();
                return;
            }

            _filterText = string.Empty;
            _droppedDown = false;
            _scrollOffset = 0;
            _hoveredIndex = -1;
            _scrollBar.ScrollTo(0);
            CapturingMouse = false;
            Invalidate();
            return;
        }

        if (args.X >= Width - DropdownButtonWidth)
        {
            _droppedDown = true;
            CapturingMouse = true;
            EnsureSelectedVisible();
            Invalidate();
        }
    }

    /// <summary>
    /// Handles mouse up events.
    /// </summary>
    protected internal override void OnMouseUp(EventArgs e)
    {
        if (_scrollBar.IsDragging || _scrollBar.IsUpButtonPressed || _scrollBar.IsDownButtonPressed)
        {
            _scrollBar.HandleMouseUp(ScrollBarCtx);
            return;
        }

        if (!_droppedDown && CapturingMouse)
        {
            CapturingMouse = false;
            return;
        }

        base.OnMouseUp(e);
    }

    /// <summary>
    /// Handles mouse wheel events for scrolling.
    /// </summary>
    protected internal override void OnMouseWheel(EventArgs e)
    {
        if (!Enabled) return;
        if (_droppedDown && !_scrollBar.IsDragging)
        {
            var args = e as MouseEventArgs;
            if (args != null)
            {
                var itemHeight = GetItemHeight();
                _scrollBar.SmallChange = itemHeight;
                _scrollBar.ViewSize = _dropDownHeight;
                _scrollBar.ContentSize = _filteredFonts.Length * itemHeight;
                if (_scrollBar.NeedsScrollbar)
                {
                    _scrollBar.HandleMouseWheel(args.Delta, ScrollBarCtx);
                }
            }
            return;
        }

        base.OnMouseWheel(e);
    }

    /// <summary>
    /// Handles mouse move events for hover tracking.
    /// </summary>
    protected internal override void OnMouseMove(EventArgs e)
    {
        if (!Enabled) return;
        if (_droppedDown)
        {
            var args = e as MouseEventArgs;
            if (args != null)
            {
                var itemHeight = GetItemHeight();
                int dropY = Height;
                _scrollBar.SmallChange = itemHeight;
                _scrollBar.ViewSize = _dropDownHeight;
                _scrollBar.ContentSize = _filteredFonts.Length * itemHeight;
                bool needsScrollbar = _scrollBar.NeedsScrollbar;
                int scrollBarWidth = needsScrollbar ? ScrollBarEngine.DefaultScrollBarSize : 0;

                if (needsScrollbar)
                {
                    int dropDownWidth = GetDropDownWidth();
                    var scrollBarBounds = new Rectangle(dropDownWidth - scrollBarWidth, dropY, scrollBarWidth, _dropDownHeight);
                    _scrollBar.HandleMouseMove(new Point(args.X, args.Y), scrollBarBounds, ScrollBarCtx);
                }

                if (args.Y >= dropY && args.Y < dropY + _dropDownHeight)
                {
                    int localY = args.Y - dropY - 2 + _scrollOffset;
                    if (localY >= 0)
                    {
                        int index = localY / itemHeight;
                        if (index >= 0 && index < _filteredFonts.Length)
                        {
                            if (_hoveredIndex != index)
                            {
                                _hoveredIndex = index;
                                Invalidate();
                            }
                        }
                        else if (_hoveredIndex != -1)
                        {
                            _hoveredIndex = -1;
                            Invalidate();
                        }
                    }
                    else if (_hoveredIndex != -1)
                    {
                        _hoveredIndex = -1;
                        Invalidate();
                    }
                }
                else if (_hoveredIndex != -1)
                {
                    _hoveredIndex = -1;
                    Invalidate();
                }
            }
            return;
        }

        base.OnMouseMove(e);
    }

    /// <summary>
    /// Handles key down events for navigation and selection.
    /// </summary>
    protected internal override void OnKeyDown(KeyEventArgs e)
    {
        if (!Enabled) return;

        switch (e.KeyCode)
        {
            case Keys.F4:
                DroppedDown = !_droppedDown;
                e.Handled = true;
                break;

            case Keys.Down:
                if (_droppedDown)
                {
                    int nextIndex = _selectedIndex + 1;
                    if (nextIndex < _filteredFonts.Length)
                    {
                        SelectFontAtIndex(nextIndex);
                        EnsureSelectedVisible();
                    }
                }
                else
                {
                    DroppedDown = true;
                }
                e.Handled = true;
                break;

            case Keys.Up:
                if (_droppedDown)
                {
                    int prevIndex = _selectedIndex - 1;
                    if (prevIndex >= 0)
                    {
                        SelectFontAtIndex(prevIndex);
                        EnsureSelectedVisible();
                    }
                }
                e.Handled = true;
                break;

            case Keys.Enter:
                if (_droppedDown)
                {
                    if (_selectedIndex >= 0 && _selectedIndex < _filteredFonts.Length)
                        _selectedFontFamily = _filteredFonts[_selectedIndex];
                    _filterText = string.Empty;
                    DroppedDown = false;
                }
                e.Handled = true;
                break;

            case Keys.Escape:
                if (_droppedDown)
                {
                    _filterText = string.Empty;
                    DroppedDown = false;
                }
                e.Handled = true;
                break;

            case Keys.Back:
                if (_filterText.Length > 0)
                {
                    _filterText = _filterText.Substring(0, _filterText.Length - 1);
                    ApplyFilterAndReopen();
                }
                e.Handled = true;
                break;

            default:
                base.OnKeyDown(e);
                return;
        }

        base.OnKeyDown(e);
    }

    /// <summary>
    /// Handles text input for filtering the font list.
    /// </summary>
    protected internal override void OnTextInput(string text)
    {
        if (!Enabled) return;

        if (text.Length == 1 && !char.IsControl(text[0]))
        {
            _filterText += text;
            ApplyFilterAndReopen();
        }
    }

    /// <summary>
    /// Handles losing focus by closing the dropdown.
    /// </summary>
    protected internal override void OnLostFocus(EventArgs e)
    {
        if (_droppedDown)
        {
            _filterText = string.Empty;
            _droppedDown = false;
            _scrollOffset = 0;
            _scrollBar.ScrollTo(0);
            _hoveredIndex = -1;
            CapturingMouse = false;
            Invalidate();
        }
        base.OnLostFocus(e);
    }

    private void ApplyFilterAndReopen()
    {
        ApplyFilter();
        if (!_droppedDown)
        {
            _droppedDown = true;
            CapturingMouse = true;
        }
        _scrollOffset = 0;
        _scrollBar.ScrollTo(0);

        if (_filteredFonts.Length > 0)
        {
            _selectedIndex = 0;
            _selectedFontFamily = _filteredFonts[0];
            OnSelectedFontChanged();
        }

        Invalidate();
    }

    private void ApplyFilter()
    {
        _filteredFonts = FontManager.SearchFontFamilies(_filterText);
    }

    private void SelectFontAtIndex(int index)
    {
        if (index >= 0 && index < _filteredFonts.Length)
        {
            _selectedIndex = index;
            _selectedFontFamily = _filteredFonts[index];
            OnSelectedFontChanged();
            Invalidate();
        }
    }

    private int FindFontIndex(string familyName)
    {
        for (int i = 0; i < _filteredFonts.Length; i++)
        {
            if (string.Equals(_filteredFonts[i], familyName, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    private void EnsureSelectedVisible()
    {
        if (_selectedIndex < 0) return;

        var itemHeight = GetItemHeight();
        int selTop = _selectedIndex * itemHeight;

        int oldOffset = _scrollOffset;
        _scrollBar.ViewSize = _dropDownHeight;
        _scrollBar.ContentSize = _filteredFonts.Length * itemHeight;
        _scrollBar.EnsureVisible(selTop, itemHeight);
        if (_scrollBar.Value != oldOffset)
        {
            _scrollOffset = _scrollBar.Value;
            Invalidate();
        }
    }

    private int GetItemHeight()
    {
        int systemItemHeight = CoordinateTransform.GetItemHeight(EffectiveFont, EffectiveZoom);
        int previewItemHeight = (int)(FontSize * EffectiveZoom) + 6;
        return Math.Max(systemItemHeight, previewItemHeight);
    }

    private int GetDropDownWidth()
    {
        return Math.Max(Width, DropdownMinWidth);
    }

    private sealed class FontPickerScrollBarContext : IScrollBarContext
    {
        private readonly FontPicker _owner;
        public FontPickerScrollBarContext(FontPicker owner) => _owner = owner;
        public float Zoom => _owner.EffectiveZoom;
        public void Invalidate() => _owner.Invalidate();
        public void CaptureMouse(bool capture) => _owner.CapturingMouse = capture;
    }
}
