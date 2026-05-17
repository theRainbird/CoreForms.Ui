using System.ComponentModel;
using CoreForms.Ui.Core;
using CoreForms.Ui.Data;
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

    /// <summary>
    /// Initializes a new instance of ComboBox.
    /// </summary>
    public ComboBox()
    {
        var theme = ThemeManager.CurrentTheme;
        _backColor = theme.TextBoxBackground;
        Size = new Size(200, 32);
        TabStop = true;
        _dropScrollBar.Scroll += (s, e) => _scrollOffset = _dropScrollBar.Value;
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
        var selectedText = GetItemDisplayText(SelectedItem);
        var btnWidth = 17;
        var btnX = Width - btnWidth;

        var textColor = Enabled ? ForeColor : theme.GrayText;
        g.DrawString(selectedText, font, textColor, 3, (Height - scaledFontSize) / 2);

        g.FillRectangle(theme.ControlBackground, btnX, 1, btnWidth, Height - 2);
        g.DrawLine(theme.ComboBoxDropdownButtonSeparator, btnX, 0, btnX, Height);

        if (Enabled)
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
                int totalHeight = _items.Count * itemHeight;
                bool needsScrollbar = _dropScrollBar.NeedsScrollbar;
                int scrollBarWidth = needsScrollbar ? ScrollBarEngine.DefaultScrollBarSize : 0;

                if (args.X >= 0 && args.X < Width && args.Y >= dropY && args.Y < dropY + dropDownHeight)
                {
                    // Check scrollbar click
                    if (needsScrollbar && args.X >= Width - scrollBarWidth)
                    {
                        var scrollBarBounds = new Rectangle(Width - scrollBarWidth, dropY, scrollBarWidth, dropDownHeight);
                        _dropScrollBar.HandleMouseDown(new Point(args.X, args.Y), scrollBarBounds, ScrollBarCtx);
                        return;
                    }

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
                    _dropScrollBar.ScrollTo(0);
                    CapturingMouse = false;
                    Invalidate();
                    return;
                }
            }

            _droppedDown = false;
            _scrollOffset = 0;
            _hoveredIndex = -1;
            _dropScrollBar.ScrollTo(0);
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
                _dropScrollBar.ViewSize = _dropDownHeight;
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
                int dropY = Height;
                int totalHeight = _items.Count * itemHeight;
                _dropScrollBar.SmallChange = itemHeight;
                _dropScrollBar.ViewSize = _dropDownHeight;
                _dropScrollBar.ContentSize = totalHeight;
                bool needsScrollbar = _dropScrollBar.NeedsScrollbar;
                int scrollBarWidth = needsScrollbar ? ScrollBarEngine.DefaultScrollBarSize : 0;

                // Handle scrollbar hover/drag
                if (needsScrollbar)
                {
                    var scrollBarBounds = new Rectangle(Width - scrollBarWidth, dropY, scrollBarWidth, _dropDownHeight);
                    _dropScrollBar.HandleMouseMove(new Point(args.X, args.Y), scrollBarBounds, ScrollBarCtx);
                }

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
        if (!Enabled) return;
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
                    _dropScrollBar.ScrollTo(0);
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
                    _dropScrollBar.ScrollTo(0);
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
        int selBottom = selTop + itemHeight;

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

    private sealed class ComboBoxScrollBarContext : IScrollBarContext
    {
        private readonly ComboBox _owner;
        public ComboBoxScrollBarContext(ComboBox owner) => _owner = owner;
        public float Zoom => _owner.EffectiveZoom;
        public void Invalidate() => _owner.Invalidate();
        public void CaptureMouse(bool capture) => _owner.CapturingMouse = capture;
    }
}
