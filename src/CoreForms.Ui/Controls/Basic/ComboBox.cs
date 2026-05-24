using System.ComponentModel;
using CoreForms.Ui.Controls.Advanced;
using CoreForms.Ui.Core;
using CoreForms.Ui.Data;
using CoreForms.Ui.Theming;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Basic;

/// <summary>
/// A control that presents a drop-down list of items with configurable <see cref="DropDownStyle"/>.
/// </summary>
public class ComboBox : Control
{
    private readonly List<object> _items = new();
    private int _selectedIndex = -1;
    private bool _droppedDown;
    private object? _dataSource;
    private string _displayMember = string.Empty;
    private string _valueMember = string.Empty;
    private bool _dataSourceUpdating;
    private BindingSource? _boundBindingSource;
    private int _dropDownHeight = 120;
    private int _scrollOffset;
    private int _hoveredIndex = -1;
    private readonly ScrollBarEngine _dropScrollBar = new();
    private ComboBoxScrollBarContext? _scrollBarContext;

    private ComboBoxScrollBarContext ScrollBarCtx => _scrollBarContext ??= new ComboBoxScrollBarContext(this);

    private DropDownStyle _dropDownStyle = DropDownStyle.DropDownList;
    private readonly TextEditorEngine _engine = new();
    private ComboBoxTextEditorContext? _engineContext;
    private bool _syncingSelection;
    private bool _autoCompleting;
    private int _lastAutoCompleteTypedLength = -1;

    private readonly ComboBoxColumnCollection _columns;
    private int _dropDownWidth;
    private bool _columnHeadersVisible = true;

    /// <summary>
    /// Initializes a new instance of ComboBox.
    /// </summary>
    public ComboBox()
    {
        _columns = new ComboBoxColumnCollection(Invalidate);
        var theme = ThemeManager.CurrentTheme;
        _backColor = theme.TextBoxBackground;
        Size = new Size(200, 32);
        TabStop = true;
        _dropScrollBar.Scroll += (s, e) => _scrollOffset = _dropScrollBar.Value;
        _engine.TextChanged += (s, e) =>
        {
            if (!_autoCompleting && !_syncingSelection)
            {
                OnTextChanged();
                Invalidate();
            }
            if (!_autoCompleting && !_syncingSelection && _dropDownStyle != DropDownStyle.DropDownList)
                PerformAutoComplete();
        };
    }

    /// <summary>
    /// Called when the theme changes. Updates combobox-specific colors.
    /// </summary>
    /// <param name="newTheme">The new theme that was activated.</param>
    public override void OnThemeChanged(Theme newTheme)
    {
        if (!_backColorSet)
            _backColor = newTheme.TextBoxBackground;
        if (!_foreColorSet)
            _foreColor = newTheme.ControlText;
        Invalidate();
    }

    /// <summary>
    /// Gets the collection of items in the combo box.
    /// </summary>
    public IList<object> Items => _itemsCollection ??= new ObjectCollection(_items, Invalidate);

    private ObjectCollection? _itemsCollection;

    private sealed class ObjectCollection : IList<object>
    {
        private readonly List<object> _items;
        private readonly Action _invalidate;

        public ObjectCollection(List<object> items, Action invalidate)
        {
            _items = items;
            _invalidate = invalidate;
        }

        public void Add(object item) { _items.Add(item); _invalidate(); }
        public void Clear() { _items.Clear(); _invalidate(); }
        public bool Remove(object item) { var r = _items.Remove(item); if (r) _invalidate(); return r; }
        public void RemoveAt(int index) { _items.RemoveAt(index); _invalidate(); }
        public void Insert(int index, object item) { _items.Insert(index, item); _invalidate(); }
        public int Count => _items.Count;
        public bool IsReadOnly => false;
        public bool Contains(object item) => _items.Contains(item);
        public int IndexOf(object item) => _items.IndexOf(item);
        public void CopyTo(object[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
        public IEnumerator<object> GetEnumerator() => _items.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _items.GetEnumerator();
        public object this[int index] { get => _items[index]; set { _items[index] = value; _invalidate(); } }
    }

    /// <summary>
    /// Gets or sets the data source for this combo box.
    /// </summary>
    public object? DataSource
    {
        get => _dataSource;
        set
        {
            if (_dataSource != value)
            {
                _dataSource = value;
                OnDataSourceChanged();
                OnPropertyChanged(nameof(DataSource));
            }
        }
    }

    /// <summary>
    /// Gets or sets the property to display for each item.
    /// </summary>
    public string DisplayMember
    {
        get => _displayMember;
        set
        {
            if (_displayMember != value)
            {
                _displayMember = value;
                OnPropertyChanged(nameof(DisplayMember));
                SyncEngineText();
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets the property to use as the value for each item.
    /// </summary>
    public string ValueMember
    {
        get => _valueMember;
        set
        {
            if (_valueMember != value)
            {
                _valueMember = value;
                OnPropertyChanged(nameof(ValueMember));
                Invalidate();
            }
        }
    }

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
                SyncEngineText();
                OnSelectedIndexChanged();
                OnPropertyChanged(nameof(SelectedIndex));
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
    /// Gets or sets the value of the selected item using the ValueMember property.
    /// </summary>
    public object? SelectedValue
    {
        get
        {
            var item = SelectedItem;
            if (item == null || string.IsNullOrEmpty(_valueMember))
                return item;
            var prop = item.GetType().GetProperty(_valueMember);
            return prop?.GetValue(item);
        }
        set
        {
            if (string.IsNullOrEmpty(_valueMember) || value == null)
                return;
            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (item == null) continue;
                var prop = item.GetType().GetProperty(_valueMember);
                if (prop == null) continue;
                var val = prop.GetValue(item);
                if (Equals(val, value))
                {
                    SelectedIndex = i;
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets the height of the drop-down list.
    /// </summary>
    public int DropDownHeight
    {
        get => _dropDownHeight;
        set => _dropDownHeight = value;
    }

    /// <summary>
    /// Gets or sets the style of the drop-down list.
    /// </summary>
    public DropDownStyle DropDownStyle
    {
        get => _dropDownStyle;
        set
        {
            if (_dropDownStyle == value) return;
            _dropDownStyle = value;

            if (value == DropDownStyle.Simple)
            {
                _droppedDown = true;
                CapturingMouse = false;
            }
            else
            {
                _droppedDown = false;
                _scrollOffset = 0;
                _dropScrollBar.ScrollTo(0);
                _hoveredIndex = -1;
                CapturingMouse = false;
            }

            if (value != DropDownStyle.DropDownList)
            {
                _syncingSelection = true;
                _engine.Text = GetItemDisplayText(SelectedItem);
                _engine.EnsureCursorVisible(EngineContext);
                _syncingSelection = false;
            }

            OnPropertyChanged(nameof(DropDownStyle));
            Invalidate();
        }
    }

    /// <summary>
    /// Gets the collection of columns for the multi-column drop-down list.
    /// When non-empty and <see cref="DataSource"/> is set, the drop-down renders
    /// multiple columns with optional headers.
    /// </summary>
    public ComboBoxColumnCollection Columns => _columns;

    /// <summary>
    /// Gets or sets the width of the drop-down list in pixels.
    /// When 0 (default), the width is auto-calculated as the maximum of the
    /// control width and the sum of all column widths (plus borders and scrollbar).
    /// </summary>
    public int DropDownWidth
    {
        get => _dropDownWidth;
        set => _dropDownWidth = Math.Max(0, value);
    }

    /// <summary>
    /// Gets or sets whether column headers are shown in the multi-column drop-down.
    /// </summary>
    public bool ColumnHeadersVisible
    {
        get => _columnHeadersVisible;
        set
        {
            if (_columnHeadersVisible == value) return;
            _columnHeadersVisible = value;
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the text in the combo box. In <see cref="DropDownStyle.DropDownList"/> mode, the getter returns
    /// the selected item's display text and the setter has no effect.
    /// </summary>
    public new string Text
    {
        get
        {
            if (_dropDownStyle == DropDownStyle.DropDownList)
                return GetItemDisplayText(SelectedItem);
            return _engine.Text;
        }
        set
        {
            if (_dropDownStyle == DropDownStyle.DropDownList)
            {
                TrySelectByText(value);
                return;
            }
            _syncingSelection = true;
            _engine.Text = value ?? string.Empty;
            _engine.EnsureCursorVisible(EngineContext);
            _syncingSelection = false;
            OnTextChanged();
            OnPropertyChanged(nameof(Text));
            Invalidate();
        }
    }

    /// <summary>
    /// Occurs when the text changes in editable modes (<see cref="DropDownStyle.DropDown"/> or <see cref="DropDownStyle.Simple"/>).
    /// </summary>
    public event EventHandler? TextChanged;

    /// <summary>
    /// Raises the <see cref="TextChanged"/> event.
    /// </summary>
    protected override void OnTextChanged()
    {
        base.OnTextChanged();
        TextChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Handles text input for editable modes.
    /// </summary>
    /// <param name="text">The text input to process.</param>
    protected internal override void OnTextInput(string text)
    {
        if (!Enabled) return;
        if (_dropDownStyle == DropDownStyle.DropDownList) return;
        _engine.HandleTextInput(text, EngineContext);
    }

    private ComboBoxTextEditorContext EngineContext =>
        _engineContext ??= new ComboBoxTextEditorContext(this);

    private void SyncEngineText()
    {
        if (_dropDownStyle == DropDownStyle.DropDownList) return;
        _lastAutoCompleteTypedLength = -1;
        if (_selectedIndex < 0 || _selectedIndex >= _items.Count)
        {
            _syncingSelection = true;
            _engine.Text = string.Empty;
            _syncingSelection = false;
            return;
        }
        _syncingSelection = true;
        _engine.Text = GetItemDisplayText(_items[_selectedIndex]);
        _engine.EnsureCursorVisible(EngineContext);
        _syncingSelection = false;
    }

    private void PerformAutoComplete()
    {
        if (_dropDownStyle == DropDownStyle.DropDownList) return;
        if (_autoCompleting || _syncingSelection) return;
        if (_items.Count == 0) return;

        string typedText = _engine.Text;
        if (string.IsNullOrEmpty(typedText)) return;

        if (typedText.Length < _lastAutoCompleteTypedLength)
            _lastAutoCompleteTypedLength = typedText.Length;

        if (typedText.Length <= _lastAutoCompleteTypedLength)
            return;

        _autoCompleting = true;
        try
        {
            for (int i = 0; i < _items.Count; i++)
            {
                string itemText = GetItemDisplayText(_items[i]);
                if (string.IsNullOrEmpty(itemText)) continue;

                if (itemText.StartsWith(typedText, StringComparison.OrdinalIgnoreCase) &&
                    !itemText.Equals(typedText, StringComparison.Ordinal))
                {
                    int typedLen = typedText.Length;
                    _engine.Text = itemText;
                    _engine.SetSelectionRange(typedLen, itemText.Length - typedLen);
                    _engine.EnsureCursorVisible(EngineContext);

                    if (_selectedIndex != i)
                        SelectedIndex = i;

                    _lastAutoCompleteTypedLength = typedLen;
                    Invalidate();
                    return;
                }
            }
        }
        finally
        {
            _autoCompleting = false;
        }
    }

    private void TrySelectByText(string? value)
    {
        if (string.IsNullOrEmpty(value)) return;
        for (int i = 0; i < _items.Count; i++)
        {
            if (string.Equals(GetItemDisplayText(_items[i]), value, StringComparison.OrdinalIgnoreCase))
            {
                SelectedIndex = i;
                return;
            }
        }
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

        if (Enabled)
            DrawFocusIndicator(g);

        if (_dropDownStyle == DropDownStyle.Simple)
        {
            RenderSimple(g, theme);
        }
        else
        {
            RenderDropDownStyle(g, theme);
        }

        base.Render(g);
    }

    private void RenderSimple(Graphics g, Theme theme)
    {
        int textBoxHeight = GetTextBoxHeight();
        int listY = textBoxHeight;

        RenderEditableText(g, theme, 0, textBoxHeight, Width - 6);

        int listHeight = Height - listY;
        if (listHeight <= 0) return;

        if (_columns.Count > 0 && _dataSource != null)
        {
            RenderSimpleMultiColumn(g, theme, listY, listHeight);
            return;
        }

        var font = EffectiveFont;
        var itemHeight = GetItemHeight();
        var totalHeight = _items.Count * itemHeight;
        var scrollBarWidth = 16;
        var needsScrollbar = totalHeight > listHeight;
        var listWidth = needsScrollbar ? Width - scrollBarWidth : Width;

        g.FillRectangle(theme.MenuDropdownBackground, 0, listY, Width, listHeight);
        g.DrawRectangle(theme.MenuDropdownBorder, 0, listY, Width, listHeight, 1);

        _dropScrollBar.SmallChange = itemHeight;
        _dropScrollBar.ViewSize = listHeight;
        _dropScrollBar.ContentSize = totalHeight;
        if (needsScrollbar)
        {
            var scrollBarBounds = new Rectangle(Width - scrollBarWidth, listY, scrollBarWidth, listHeight);
            _dropScrollBar.Render(g, scrollBarBounds, theme);
        }

        g.SetClip(new Rectangle(0, listY, listWidth, listHeight));

        for (int i = 0; i < _items.Count; i++)
        {
            var y = listY + 2 + i * itemHeight - _scrollOffset;

            if (y + itemHeight <= listY) continue;
            if (y >= listY + listHeight) break;

            if (i == _selectedIndex)
            {
                g.FillRectangle(theme.Highlight, 1, y, listWidth - 2, itemHeight);
                g.DrawString(GetItemDisplayText(_items[i]), font, theme.HighlightText, 4, y + 2);
            }
            else if (i == _hoveredIndex)
            {
                g.FillRectangle(theme.HoverHighlight, 1, y, listWidth - 2, itemHeight);
                g.DrawString(GetItemDisplayText(_items[i]), font, ForeColor, 4, y + 2);
            }
            else
            {
                g.DrawString(GetItemDisplayText(_items[i]), font, ForeColor, 4, y + 2);
            }
        }

        g.ResetClip();
    }

    private void RenderSimpleMultiColumn(Graphics g, Theme theme, int listY, int listHeight)
    {
        var font = EffectiveFont;
        float zoom = EffectiveZoom;
        var itemHeight = GetItemHeight();
        int headerHeight = GetColumnHeaderHeight();
        int totalHeight = _items.Count * itemHeight;
        var scrollBarWidth = ScrollBarEngine.DefaultScrollBarSize;
        var needsScrollbar = totalHeight + headerHeight > listHeight;
        var listWidth = needsScrollbar ? Width - scrollBarWidth : Width;

        g.FillRectangle(theme.MenuDropdownBackground, 0, listY, listWidth, listHeight);
        g.DrawRectangle(theme.MenuDropdownBorder, 0, listY, listWidth, listHeight, 1);

        _dropScrollBar.SmallChange = itemHeight;
        _dropScrollBar.ViewSize = listHeight;
        _dropScrollBar.ContentSize = totalHeight + headerHeight;
        if (needsScrollbar)
        {
            var scrollBarBounds = new Rectangle(Width - scrollBarWidth, listY, scrollBarWidth, listHeight);
            _dropScrollBar.Render(g, scrollBarBounds, theme);
        }

        g.SetClip(new Rectangle(0, listY, listWidth, listHeight));

        // Pre-compute column start positions
        var colStarts = new int[_columns.Count];
        int colX = 2;
        for (int i = 0; i < _columns.Count; i++)
        {
            colStarts[i] = colX;
            colX += _columns[i].Width;
        }

        int contentTop = listY;

        var headerBottom = contentTop + headerHeight;

        // Column headers
        if (_columnHeadersVisible)
        {
            g.FillRectangle(theme.ControlBackground, 0, contentTop, listWidth, headerHeight);
            for (int i = 0; i < _columns.Count; i++)
            {
                var col = _columns[i];
                int cx = colStarts[i];
                g.DrawRectangle(theme.MenuDropdownBorder, cx, contentTop, col.Width, headerHeight, 1);
                var headerText = col.HeaderText;
                var hFont = font;
                headerText = TruncateText(headerText, hFont, zoom, col.TextAlign, col.Width - 8);
                float hx = GetAlignedX(headerText, hFont, zoom, col.TextAlign, cx, col.Width, 4);
                float hy = contentTop + CoordinateTransform.CenterVertically(0, headerHeight, hFont, zoom);
                g.DrawString(headerText, hFont, theme.ControlText, hx, hy);

                if (i < _columns.Count - 1)
                    g.DrawLine(theme.GridLineVertical, cx + col.Width, contentTop, cx + col.Width, headerBottom);
            }
            contentTop += headerHeight;
        }

        // Items
        for (int i = 0; i < _items.Count; i++)
        {
            var y = contentTop + 2 + i * itemHeight - _scrollOffset;
            if (y + itemHeight <= contentTop) continue;
            if (y >= listY + listHeight) break;

            Color textColor;
            if (i == _selectedIndex)
            {
                g.FillRectangle(theme.Highlight, 1, y, listWidth - 2, itemHeight);
                textColor = theme.HighlightText;
            }
            else if (i == _hoveredIndex)
            {
                g.FillRectangle(theme.HoverHighlight, 1, y, listWidth - 2, itemHeight);
                textColor = ForeColor;
            }
            else
            {
                textColor = ForeColor;
            }

            for (int c = 0; c < _columns.Count; c++)
            {
                var col = _columns[c];
                var cellText = GetColumnCellText(_items[i], col);
                cellText = TruncateText(cellText, font, zoom, col.TextAlign, col.Width - 8);
                float tx = GetAlignedX(cellText, font, zoom, col.TextAlign, colStarts[c], col.Width, 4);
                g.DrawString(cellText, font, textColor, tx, y + 2);

                if (c < _columns.Count - 1)
                    g.DrawLine(theme.GridLineVertical, colStarts[c] + col.Width, y, colStarts[c] + col.Width, y + itemHeight);
            }
        }

        g.ResetClip();
    }

    private void RenderDropDownStyle(Graphics g, Theme theme)
    {
        var btnWidth = 17;
        var btnX = Width - btnWidth;

        if (_dropDownStyle == DropDownStyle.DropDown)
        {
            RenderEditableText(g, theme, 0, Height, Width - btnWidth - 6);
        }
        else
        {
            var font = EffectiveFont;
            float zoom = EffectiveZoom;
            float scaledFontSize = font.Size * zoom;
            var textColor = Enabled ? ForeColor : theme.GrayText;
            g.DrawString(GetItemDisplayText(SelectedItem), font, textColor, 3, (Height - scaledFontSize) / 2);
        }

        g.FillRectangle(theme.ControlBackground, btnX, 1, btnWidth, Height - 2);
        g.DrawLine(theme.ComboBoxDropdownButtonSeparator, btnX, 0, btnX, Height);

        var cx = btnX + btnWidth / 2;
        var cy = Height / 2;
        var tw = 4;
        var th = 3;
        g.FillTriangle(theme.ComboBoxDropdownArrow,
            cx - tw, cy - th,
            cx + tw, cy - th,
            cx, cy + th);
    }

    private void RenderEditableText(Graphics g, Theme theme, int areaY, int areaHeight, int textWidth)
    {
        var font = EffectiveFont;
        float zoom = EffectiveZoom;
        float scaledFontSize = font.Size * zoom;
        float textY = CoordinateTransform.CenterVertically(areaHeight, font, zoom) + areaY;
        float textX = 3 - _engine.ScrollOffset;

        g.SetClip(new Rectangle(3, areaY, textWidth, areaHeight));

        var textColor = Enabled ? ForeColor : theme.GrayText;
        string displayText = _engine.DisplayText;
        var context = EngineContext;

        if (Enabled && _engine.HasSelection && Focused)
        {
            int selStart = _engine.SelectionStartIndex;
            int selEnd = _engine.SelectionEndIndex;
            string beforeSel = displayText.Substring(0, selStart);
            string selStr = displayText.Substring(selStart, selEnd - selStart);
            float selX = textX + _engine.MeasureTextWidth(beforeSel, context);
            float selWidth = Math.Max(_engine.MeasureTextWidth(selStr, context), 2);

            g.DrawString(displayText, font, textColor, textX, textY);
            g.FillRectangle(theme.Highlight, selX, textY, selWidth, scaledFontSize + 2);
            g.DrawString(selStr, font, theme.HighlightText, selX, textY);
        }
        else
        {
            g.DrawString(displayText, font, textColor, textX, textY);
        }

        if (Enabled && Focused && TextEditorEngine.IsCursorBlinkVisible)
        {
            string textBeforeCursor = displayText.Substring(0, _engine.CursorPosition);
            float cursorX = textX + _engine.MeasureTextWidth(textBeforeCursor, context);
            g.DrawLine(theme.CursorLine, cursorX, textY, cursorX, textY + scaledFontSize, 1);
        }

        g.ResetClip();
    }

    private int GetItemHeight()
    {
        var font = EffectiveFont;
        return CoordinateTransform.GetItemHeight(font, EffectiveZoom);
    }

    private int GetTextBoxHeight()
    {
        return Math.Max(GetItemHeight() + 8, 20);
    }

    /// <summary>
    /// Renders the dropdown list below the combo box when visible.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
    public override void RenderOverlay(Graphics g)
    {
        if (!Visible) return;

        base.RenderOverlay(g);

        if (!_droppedDown || _dropDownStyle == DropDownStyle.Simple) return;

        if (_columns.Count > 0 && _dataSource != null)
        {
            RenderMultiColumnDropDown(g);
            return;
        }

        var theme = ThemeManager.CurrentTheme;
        var font = EffectiveFont;
        var itemHeight = GetItemHeight();
        var totalHeight = _items.Count * itemHeight;
        var scrollBarWidth = 16;
        var needsScrollbar = totalHeight > _dropDownHeight;
        var listWidth = needsScrollbar ? Width - scrollBarWidth : Width;
        int dropY = Height;

        g.FillRectangle(theme.MenuDropdownBackground, 0, dropY, Width, _dropDownHeight);
        g.DrawRectangle(theme.MenuDropdownBorder, 0, dropY, Width, _dropDownHeight, 1);

        _dropScrollBar.SmallChange = itemHeight;
        _dropScrollBar.ViewSize = _dropDownHeight;
        _dropScrollBar.ContentSize = totalHeight;
        if (needsScrollbar)
        {
            var scrollBarBounds = new Rectangle(Width - scrollBarWidth, dropY, scrollBarWidth, _dropDownHeight);
            _dropScrollBar.Render(g, scrollBarBounds, theme);
        }

        g.SetClip(new Rectangle(0, dropY, listWidth, _dropDownHeight));

        for (int i = 0; i < _items.Count; i++)
        {
            var y = dropY + 2 + i * itemHeight - _scrollOffset;

            if (y + itemHeight <= dropY) continue;
            if (y >= dropY + _dropDownHeight) break;

            if (i == _selectedIndex)
            {
                g.FillRectangle(theme.Highlight, 1, y, listWidth - 2, itemHeight);
                var displayText = GetItemDisplayText(_items[i]);
                g.DrawString(displayText, font, theme.HighlightText, 4, y + 2);
            }
            else if (i == _hoveredIndex)
            {
                var displayText = GetItemDisplayText(_items[i]);
                g.FillRectangle(theme.HoverHighlight, 1, y, listWidth - 2, itemHeight);
                g.DrawString(displayText, font, ForeColor, 4, y + 2);
            }
            else
            {
                var displayText = GetItemDisplayText(_items[i]);
                g.DrawString(displayText, font, ForeColor, 4, y + 2);
            }
        }

        g.ResetClip();
    }

    /// <summary>
    /// Tests whether the specified point is within the control bounds (including dropdown overlay).
    /// </summary>
    /// <param name="point">The point to test.</param>
    /// <returns>True if the point is within bounds; otherwise, false.</returns>
    public override bool HitTest(Point point)
    {
        if (Bounds.Contains(point))
            return true;

        if (_droppedDown && _dropDownStyle != DropDownStyle.Simple)
        {
            int dropWidth = GetDropDownWidth();
            var dropBounds = new Rectangle(X, Y + Height, dropWidth, _dropDownHeight);
            if (dropBounds.Contains(point))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Raises the LostFocus event and closes the dropdown (except in Simple mode).
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnLostFocus(EventArgs e)
    {
        if (_dropDownStyle != DropDownStyle.Simple && _droppedDown)
        {
            _droppedDown = false;
            _scrollOffset = 0;
            _dropScrollBar.ScrollTo(0);
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
        if (!Enabled) return;

        if (!Focused)
            Focused = true;

        var args = e as MouseEventArgs;
        if (args == null) return;

        if (_dropDownStyle == DropDownStyle.Simple)
        {
            HandleSimpleMouseDown(args);
            return;
        }

        if (_dropDownStyle == DropDownStyle.DropDown)
        {
            HandleDropDownMouseDown(args);
            return;
        }

        HandleDropDownListMouseDown(args);
    }

    private void HandleSimpleMouseDown(MouseEventArgs args)
    {
        int textBoxHeight = GetTextBoxHeight();

        if (args.Y < textBoxHeight)
        {
            int logicalX = args.X - 3 + _engine.ScrollOffset;
            _engine.HandleMouseDown(logicalX, EngineContext);
            return;
        }

        int listY = textBoxHeight;
        int listHeight = Height - listY;
        if (listHeight <= 0) return;

        var itemHeight = GetItemHeight();
        int totalHeight = _items.Count * itemHeight;
        bool needsScrollbar = _dropScrollBar.NeedsScrollbar;
        int scrollBarWidth = needsScrollbar ? ScrollBarEngine.DefaultScrollBarSize : 0;

        if (needsScrollbar && args.X >= Width - scrollBarWidth)
        {
            var scrollBarBounds = new Rectangle(Width - scrollBarWidth, listY, scrollBarWidth, listHeight);
            _dropScrollBar.HandleMouseDown(new Point(args.X, args.Y), scrollBarBounds, ScrollBarCtx);
            return;
        }

        if (args.Y >= listY && args.Y < listY + listHeight)
        {
            int listOffset = GetHeaderOffset();
            int localY = args.Y - listY - listOffset - 2 + _scrollOffset;
            if (localY >= 0)
            {
                int index = localY / itemHeight;
                if (index >= 0 && index < _items.Count)
                    SelectedIndex = index;
            }

            Invalidate();
        }
    }

    private void HandleDropDownMouseDown(MouseEventArgs args)
    {
        if (_droppedDown)
        {
            var itemHeight = GetItemHeight();
            int dropY = Height;
            int dropWidth = GetDropDownWidth();
            int totalHeight = _items.Count * itemHeight;
            bool needsScrollbar = _dropScrollBar.NeedsScrollbar;
            int scrollBarWidth = needsScrollbar ? ScrollBarEngine.DefaultScrollBarSize : 0;

            if (args.X >= 0 && args.X < dropWidth && args.Y >= dropY && args.Y < dropY + _dropDownHeight)
            {
                if (needsScrollbar && args.X >= dropWidth - scrollBarWidth)
                {
                    var scrollBarBounds = new Rectangle(dropWidth - scrollBarWidth, dropY, scrollBarWidth, _dropDownHeight);
                    _dropScrollBar.HandleMouseDown(new Point(args.X, args.Y), scrollBarBounds, ScrollBarCtx);
                    return;
                }

                int headerOffset = GetHeaderOffset();
                int localY = args.Y - dropY - headerOffset - 2 + _scrollOffset;
                if (localY >= 0)
                {
                    int index = localY / itemHeight;
                    if (index >= 0 && index < _items.Count)
                        SelectedIndex = index;
                }

                _droppedDown = false;
                _scrollOffset = 0;
                _hoveredIndex = -1;
                _dropScrollBar.ScrollTo(0);
                Invalidate();
                return;
            }

            _droppedDown = false;
            _scrollOffset = 0;
            _hoveredIndex = -1;
            _dropScrollBar.ScrollTo(0);
            Invalidate();
            return;
        }

        if (args.X >= Width - 17)
        {
            _droppedDown = true;
            CapturingMouse = true;
            EnsureSelectedVisible();
            Invalidate();
        }
        else
        {
            int logicalX = args.X - 3 + _engine.ScrollOffset;
            _engine.HandleMouseDown(logicalX, EngineContext);
        }
    }

    private void HandleDropDownListMouseDown(MouseEventArgs args)
    {
        if (_droppedDown)
        {
            var itemHeight = GetItemHeight();
            int dropY = Height;
            int dropWidth = GetDropDownWidth();
            int totalHeight = _items.Count * itemHeight;
            bool needsScrollbar = _dropScrollBar.NeedsScrollbar;
            int scrollBarWidth = needsScrollbar ? ScrollBarEngine.DefaultScrollBarSize : 0;

            if (args.X >= 0 && args.X < dropWidth && args.Y >= dropY && args.Y < dropY + _dropDownHeight)
            {
                if (needsScrollbar && args.X >= dropWidth - scrollBarWidth)
                {
                    var scrollBarBounds = new Rectangle(dropWidth - scrollBarWidth, dropY, scrollBarWidth, _dropDownHeight);
                    _dropScrollBar.HandleMouseDown(new Point(args.X, args.Y), scrollBarBounds, ScrollBarCtx);
                    return;
                }

                int headerOffset = GetHeaderOffset();
                int localY = args.Y - dropY - headerOffset - 2 + _scrollOffset;
                if (localY >= 0)
                {
                    int index = localY / itemHeight;
                    if (index >= 0 && index < _items.Count)
                        SelectedIndex = index;
                }

                _droppedDown = false;
                _scrollOffset = 0;
                _hoveredIndex = -1;
                _dropScrollBar.ScrollTo(0);
                Invalidate();
                return;
            }

            _droppedDown = false;
            _scrollOffset = 0;
            _hoveredIndex = -1;
            _dropScrollBar.ScrollTo(0);
            Invalidate();
            return;
        }

        if (args.X >= Width - 17)
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
        if (_dropScrollBar.IsDragging || _dropScrollBar.IsUpButtonPressed || _dropScrollBar.IsDownButtonPressed)
        {
            _dropScrollBar.HandleMouseUp(ScrollBarCtx);
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
    /// Raises the MouseWheel event to handle scrolling.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseWheel(EventArgs e)
    {
        if (!Enabled) return;
        if (_droppedDown && !_dropScrollBar.IsDragging)
        {
            var args = e as MouseEventArgs;
            if (args != null)
            {
                var itemHeight = GetItemHeight();
                _dropScrollBar.SmallChange = itemHeight;
                _dropScrollBar.ViewSize = _dropDownHeight - GetHeaderOffset();
                _dropScrollBar.ContentSize = _items.Count * itemHeight;
                if (_dropScrollBar.NeedsScrollbar)
                {
                    _dropScrollBar.HandleMouseWheel(args.Delta, ScrollBarCtx);
                }
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
        if (!Enabled) return;
        if (_droppedDown)
        {
            var args = e as MouseEventArgs;
            if (args != null)
            {
                var itemHeight = GetItemHeight();
                int dropY = _dropDownStyle == DropDownStyle.Simple ? GetTextBoxHeight() : Height;
                int viewHeight = _dropDownStyle == DropDownStyle.Simple ? Height - dropY : _dropDownHeight;
                int headerOffset = GetHeaderOffset();
                int totalHeight = _items.Count * itemHeight;
                _dropScrollBar.SmallChange = itemHeight;
                _dropScrollBar.ViewSize = viewHeight - headerOffset;
                _dropScrollBar.ContentSize = totalHeight;
                bool needsScrollbar = _dropScrollBar.NeedsScrollbar;
                int scrollBarWidth = needsScrollbar ? ScrollBarEngine.DefaultScrollBarSize : 0;

                if (needsScrollbar)
                {
                    int dropWidth = _dropDownStyle == DropDownStyle.Simple ? Width : GetDropDownWidth();
                    var scrollBarBounds = new Rectangle(dropWidth - scrollBarWidth, dropY, scrollBarWidth, viewHeight);
                    _dropScrollBar.HandleMouseMove(new Point(args.X, args.Y), scrollBarBounds, ScrollBarCtx);
                }

                if (args.Y >= dropY && args.Y < dropY + viewHeight)
                {
                    int localY = args.Y - dropY - headerOffset - 2 + _scrollOffset;
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
    /// Raises the KeyDown event to handle keyboard navigation and text editing.
    /// </summary>
    /// <param name="e">A KeyEventArgs that contains the event data.</param>
    protected internal override void OnKeyDown(KeyEventArgs e)
    {
        if (!Enabled) return;

        if (_dropDownStyle != DropDownStyle.DropDownList)
        {
            _engine.HandleKeyDown(e, EngineContext);
            if (e.Handled)
            {
                base.OnKeyDown(e);
                return;
            }
        }

        switch (e.KeyCode)
        {
            case Keys.F4:
                if (_dropDownStyle == DropDownStyle.Simple) break;
                _droppedDown = !_droppedDown;
                if (_droppedDown)
                {
                    CapturingMouse = true;
                    EnsureSelectedVisible();
                }
                else
                {
                    _scrollOffset = 0;
                    _dropScrollBar.ScrollTo(0);
                    CapturingMouse = false;
                }
                Invalidate();
                e.Handled = true;
                break;
            case Keys.Down:
                if (_selectedIndex < _items.Count - 1)
                {
                    SelectedIndex++;
                    if (_droppedDown)
                        EnsureSelectedVisible();
                }
                e.Handled = true;
                break;
            case Keys.Up:
                if (_selectedIndex > 0)
                {
                    SelectedIndex--;
                    if (_droppedDown)
                        EnsureSelectedVisible();
                }
                e.Handled = true;
                break;
            case Keys.Enter:
                if (_dropDownStyle == DropDownStyle.Simple) break;
                if (_droppedDown)
                {
                    _droppedDown = false;
                    _scrollOffset = 0;
                    _dropScrollBar.ScrollTo(0);
                    CapturingMouse = false;
                    Invalidate();
                }
                e.Handled = true;
                break;
            case Keys.Escape:
                if (_dropDownStyle == DropDownStyle.Simple) break;
                if (_droppedDown)
                {
                    _droppedDown = false;
                    _scrollOffset = 0;
                    _dropScrollBar.ScrollTo(0);
                    CapturingMouse = false;
                    Invalidate();
                }
                e.Handled = true;
                break;
        }
        base.OnKeyDown(e);
    }

    private void EnsureSelectedVisible()
    {
        if (_selectedIndex < 0) return;

        var itemHeight = GetItemHeight();
        int selTop = _selectedIndex * itemHeight;

        int oldOffset = _scrollOffset;
        _dropScrollBar.ViewSize = _dropDownHeight;
        _dropScrollBar.ContentSize = _items.Count * itemHeight;
        _dropScrollBar.EnsureVisible(selTop, itemHeight);
        if (_dropScrollBar.Value != oldOffset)
        {
            _scrollOffset = _dropScrollBar.Value;
            Invalidate();
        }
    }

    /// <summary>
    /// Raises the SelectedIndexChanged event and syncs the BindingSource/CurrencyManager position.
    /// </summary>
    protected virtual void OnSelectedIndexChanged()
    {
        if (!_dataSourceUpdating && _dataSource != null && _selectedIndex >= 0)
        {
            if (_dataSource is BindingSource bs)
            {
                bs.Position = _selectedIndex;
            }
            else
            {
                var form = FindForm();
                if (form?.BindingContext != null)
                {
                    try
                    {
                        var mgr = form.BindingContext[_dataSource] as CurrencyManager;
                        if (mgr != null)
                            mgr.Position = _selectedIndex;
                    }
                    catch
                    {
                        // Ignore binding context errors
                    }
                }
            }
        }
        SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Called when the DataSource property changes. Rebuilds items from the data source.
    /// </summary>
    protected virtual void OnDataSourceChanged()
    {
        if (_dataSourceUpdating) return;
        _dataSourceUpdating = true;
        try
        {
            if (_boundBindingSource != null)
            {
                _boundBindingSource.CurrentChanged -= OnBoundBindingSourceCurrentChanged;
                _boundBindingSource = null;
            }

            _items.Clear();

            if (_dataSource is BindingSource bs)
            {
                _boundBindingSource = bs;
                var list = bs.List;
                if (list != null)
                {
                    foreach (var item in list)
                        _items.Add(item!);
                    list.ListChanged += OnDataSourceListChanged;
                }
                bs.CurrentChanged += OnBoundBindingSourceCurrentChanged;
            }
            else if (_dataSource is IBindingList bindingList)
            {
                foreach (var item in bindingList)
                    _items.Add(item!);
                bindingList.ListChanged += OnDataSourceListChanged;
            }
            else if (_dataSource is System.Collections.IEnumerable enumerable && _dataSource is not string)
            {
                foreach (var item in enumerable)
                    _items.Add(item!);
            }

            _selectedIndex = _items.Count > 0 ? 0 : -1;
            SyncEngineText();
            Invalidate();
        }
        finally
        {
            _dataSourceUpdating = false;
        }
    }

    private void OnBoundBindingSourceCurrentChanged(object? sender, EventArgs e)
    {
        if (!_dataSourceUpdating && _boundBindingSource != null)
            SelectedIndex = _boundBindingSource.Position;
    }

    private void OnDataSourceListChanged(object? sender, ListChangedEventArgs e)
    {
        if (_dataSource is not IBindingList bindingList)
            return;

        switch (e.ListChangedType)
        {
            case ListChangedType.ItemAdded:
                if (e.NewIndex >= 0 && e.NewIndex < bindingList.Count)
                    _items.Insert(e.NewIndex, bindingList[e.NewIndex]!);
                break;
            case ListChangedType.ItemDeleted:
                if (e.NewIndex >= 0 && e.NewIndex < _items.Count)
                    _items.RemoveAt(e.NewIndex);
                break;
            case ListChangedType.ItemChanged:
                if (e.NewIndex >= 0 && e.NewIndex < bindingList.Count && e.NewIndex < _items.Count)
                    _items[e.NewIndex] = bindingList[e.NewIndex]!;
                break;
            case ListChangedType.Reset:
                _items.Clear();
                foreach (var item in bindingList)
                    _items.Add(item!);
                break;
        }

        if (_selectedIndex >= _items.Count)
            _selectedIndex = Math.Max(0, _items.Count - 1);

        Invalidate();
    }

    /// <summary>
    /// Gets the display text for an item based on the DisplayMember property.
    /// </summary>
    protected string GetItemDisplayText(object? item)
    {
        if (item == null)
            return string.Empty;
        if (string.IsNullOrEmpty(_displayMember))
            return item.ToString() ?? string.Empty;
        var prop = item.GetType().GetProperty(_displayMember);
        if (prop == null)
            return item.ToString() ?? string.Empty;
        return prop.GetValue(item)?.ToString() ?? string.Empty;
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

    /// <summary>
    /// Gets the display text for an item in the given column.
    /// </summary>
    private string GetColumnCellText(object item, ComboBoxColumn column)
    {
        if (column.DataPropertyName != null)
        {
            var prop = item.GetType().GetProperty(column.DataPropertyName);
            if (prop != null)
            {
                var val = prop.GetValue(item);
                return FormatCellValue(val, column.FormatString);
            }
        }
        return string.Empty;
    }

    /// <summary>
    /// Formats a cell value using the specified format string.
    /// </summary>
    private static string FormatCellValue(object? value, string? formatString)
    {
        if (value == null) return "";
        if (formatString != null)
        {
            try { return string.Format($"{{0:{formatString}}}", value); }
            catch { }
        }
        return value.ToString() ?? "";
    }

    /// <summary>
    /// Calculates the X position for text based on alignment within a cell.
    /// </summary>
    private static float GetAlignedX(string text, Font font, float zoom, DataGridViewContentAlignment alignment, float cellX, float cellWidth, int padding)
    {
        if (alignment == DataGridViewContentAlignment.Left || string.IsNullOrEmpty(text))
            return cellX + padding;

        float textWidthLogical = CoordinateTransform.MeasureText(text, font, zoom).width / Math.Max(zoom, 0.001f);

        return alignment switch
        {
            DataGridViewContentAlignment.Center => cellX + (cellWidth - textWidthLogical) / 2f,
            DataGridViewContentAlignment.Right => cellX + cellWidth - textWidthLogical - padding,
            _ => cellX + padding
        };
    }

    /// <summary>
    /// Truncates text with ellipsis to fit within a maximum width, respecting alignment.
    /// </summary>
    private static string TruncateText(string text, Font font, float zoom, DataGridViewContentAlignment alignment, float maxWidthLogical)
    {
        if (string.IsNullOrEmpty(text) || zoom <= 0f) return text;

        int maxWidthPixels = (int)(maxWidthLogical * zoom);
        int textWidth = CoordinateTransform.MeasureText(text, font, zoom).width;
        if (textWidth <= maxWidthPixels) return text;

        const string ellipsis = "...";
        int ellipsisWidth = CoordinateTransform.MeasureText(ellipsis, font, zoom).width;
        int availablePixels = maxWidthPixels - ellipsisWidth;

        if (availablePixels <= 0) return ellipsis;

        if (alignment == DataGridViewContentAlignment.Right)
        {
            for (int i = text.Length - 1; i >= 0; i--)
            {
                var sub = text.Substring(i);
                if (CoordinateTransform.MeasureText(sub, font, zoom).width <= availablePixels)
                    return ellipsis + sub;
            }
            return ellipsis;
        }
        else
        {
            for (int i = 1; i <= text.Length; i++)
            {
                var sub = text.Substring(0, i);
                if (CoordinateTransform.MeasureText(sub, font, zoom).width > availablePixels)
                    return text.Substring(0, i - 1) + ellipsis;
            }
            return text + ellipsis;
        }
    }

    /// <summary>
    /// Returns the height of the column header row.
    /// </summary>
    private int GetColumnHeaderHeight()
    {
        return _columnHeadersVisible ? GetItemHeight() : 0;
    }

    /// <summary>
    /// Returns the header offset that item Y positions start after, for multi-column mode.
    /// </summary>
    private int GetHeaderOffset()
    {
        return _columns.Count > 0 && _dataSource != null ? GetColumnHeaderHeight() : 0;
    }

    /// <summary>
    /// Returns the width of the drop-down overlay. When <see cref="DropDownWidth"/> is set (> 0),
    /// that value is used. Otherwise auto-calculates from column widths or falls back to control width.
    /// </summary>
    private int GetDropDownWidth()
    {
        if (_dropDownWidth > 0)
            return _dropDownWidth;

        if (_columns.Count > 0)
        {
            int total = 2;
            for (int i = 0; i < _columns.Count; i++)
                total += _columns[i].Width;
            int totalHeight = _items.Count * GetItemHeight();
            if (totalHeight > _dropDownHeight)
                total += ScrollBarEngine.DefaultScrollBarSize;
            return Math.Max(Width, total);
        }

        return Width;
    }

    /// <summary>
    /// Renders the drop-down list with multiple columns (requires <see cref="Columns"/> and <see cref="DataSource"/>).
    /// </summary>
    private void RenderMultiColumnDropDown(Graphics g)
    {
        var theme = ThemeManager.CurrentTheme;
        var font = EffectiveFont;
        float zoom = EffectiveZoom;
        var itemHeight = GetItemHeight();
        int headerHeight = GetColumnHeaderHeight();
        int totalContentHeight = headerHeight + _items.Count * itemHeight;
        int totalHeight = _items.Count * itemHeight;
        var scrollBarWidth = ScrollBarEngine.DefaultScrollBarSize;
        bool needsScrollbar = totalContentHeight > _dropDownHeight;
        int dropDownWidth = GetDropDownWidth();
        int viewHeight = Math.Min(_dropDownHeight, totalContentHeight);

        int dropY = Height;

        g.FillRectangle(theme.MenuDropdownBackground, 0, dropY, dropDownWidth, _dropDownHeight);
        g.DrawRectangle(theme.MenuDropdownBorder, 0, dropY, dropDownWidth, _dropDownHeight, 1);

        _dropScrollBar.SmallChange = itemHeight;
        _dropScrollBar.ViewSize = _dropDownHeight - headerHeight;
        _dropScrollBar.ContentSize = totalHeight;
        if (needsScrollbar)
        {
            var scrollBarBounds = new Rectangle(dropDownWidth - scrollBarWidth, dropY, scrollBarWidth, _dropDownHeight);
            _dropScrollBar.Render(g, scrollBarBounds, theme);
        }

        int listWidth = needsScrollbar ? dropDownWidth - scrollBarWidth : dropDownWidth;

        // Pre-compute column start positions
        var colStarts = new int[_columns.Count];
        int colX = 2;
        for (int i = 0; i < _columns.Count; i++)
        {
            colStarts[i] = colX;
            colX += _columns[i].Width;
        }

        var headerBottom = dropY + headerHeight;
        var itemBottom = dropY + _dropDownHeight;

        // Column headers
        if (_columnHeadersVisible)
        {
            g.SetClip(new Rectangle(0, dropY, listWidth, headerHeight));
            g.FillRectangle(theme.ControlBackground, 0, dropY, listWidth, headerHeight);
            for (int i = 0; i < _columns.Count; i++)
            {
                var col = _columns[i];
                int cx = colStarts[i];
                g.DrawRectangle(theme.MenuDropdownBorder, cx, dropY, col.Width, headerHeight, 1);
                var headerText = col.HeaderText;
                var hFont = font;
                headerText = TruncateText(headerText, hFont, zoom, col.TextAlign, col.Width - 8);
                float hx = GetAlignedX(headerText, hFont, zoom, col.TextAlign, cx, col.Width, 4);
                float hy = dropY + CoordinateTransform.CenterVertically(0, headerHeight, hFont, zoom);
                g.DrawString(headerText, hFont, theme.ControlText, hx, hy);

                // Vertical grid line after each column (except last) in header
                if (i < _columns.Count - 1)
                    g.DrawLine(theme.GridLineVertical, cx + col.Width, dropY, cx + col.Width, headerBottom);
            }
            g.ResetClip();
        }

        // Items
        g.SetClip(new Rectangle(0, headerBottom, listWidth, _dropDownHeight - headerHeight));
        for (int i = 0; i < _items.Count; i++)
        {
            var y = headerBottom + 2 + i * itemHeight - _scrollOffset;
            if (y + itemHeight <= headerBottom) continue;
            if (y >= itemBottom) break;

            Color textColor;
            if (i == _selectedIndex)
            {
                g.FillRectangle(theme.Highlight, 1, y, listWidth - 2, itemHeight);
                textColor = theme.HighlightText;
            }
            else if (i == _hoveredIndex)
            {
                g.FillRectangle(theme.HoverHighlight, 1, y, listWidth - 2, itemHeight);
                textColor = ForeColor;
            }
            else
            {
                textColor = ForeColor;
            }

            for (int c = 0; c < _columns.Count; c++)
            {
                var col = _columns[c];
                var cellText = GetColumnCellText(_items[i], col);
                cellText = TruncateText(cellText, font, zoom, col.TextAlign, col.Width - 8);
                float tx = GetAlignedX(cellText, font, zoom, col.TextAlign, colStarts[c], col.Width, 4);
                g.DrawString(cellText, font, textColor, tx, y + 2);

                // Vertical grid line after each column (except last)
                if (c < _columns.Count - 1)
                    g.DrawLine(theme.GridLineVertical, colStarts[c] + col.Width, y, colStarts[c] + col.Width, y + itemHeight);
            }
        }
        g.ResetClip();
    }

    private sealed class ComboBoxScrollBarContext : IScrollBarContext
    {
        private readonly ComboBox _owner;
        public ComboBoxScrollBarContext(ComboBox owner) => _owner = owner;
        public float Zoom => _owner.EffectiveZoom;
        public void Invalidate() => _owner.Invalidate();
        public void CaptureMouse(bool capture) => _owner.CapturingMouse = capture;
    }

    private sealed class ComboBoxTextEditorContext : ITextEditorContext
    {
        private readonly ComboBox _owner;
        public ComboBoxTextEditorContext(ComboBox owner) => _owner = owner;
        public Font Font => _owner.EffectiveFont;
        public float Zoom => _owner.EffectiveZoom;
        public int TextAreaWidth
        {
            get
            {
                if (_owner._dropDownStyle == DropDownStyle.Simple)
                    return _owner.Width - 6;
                return _owner.Width - 20;
            }
        }
        public int TextAreaHeight => _owner.Height;
        public void Invalidate() => _owner.Invalidate();
    }
}
