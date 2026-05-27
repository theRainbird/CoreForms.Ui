using System.ComponentModel;
using CoreForms.Ui.Core;
using CoreForms.Ui.Data;
using CoreForms.Ui.Theming;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Basic;

/// <summary>
/// A control that displays a list of items from which the user can select.
/// </summary>
public class ListBox : Control
{
    private readonly List<object> _items = new();
    private int _selectedIndex = -1;
    private object? _dataSource;
    private string _displayMember = string.Empty;
    private string _valueMember = string.Empty;
    private bool _dataSourceUpdating;
    private BindingSource? _boundBindingSource;
    private IBindingList? _previousBindingList;
    private int _scrollOffset;
    private readonly ScrollBarEngine _vScrollBar = new();
    private ListBoxScrollBarContext? _scrollBarContext;

    private ListBoxScrollBarContext VScrollBarContext => _scrollBarContext ??= new ListBoxScrollBarContext(this);

    /// <summary>
    /// Initializes a new instance of ListBox.
    /// </summary>
    public ListBox()
    {
        var theme = ThemeManager.CurrentTheme;
        _backColor = theme.TextBoxBackground;
        _foreColor = theme.TextBoxText;
        Size = new Size(150, 120);
        TabStop = true;
        _vScrollBar.Scroll += (s, e) =>
        {
            _scrollOffset = _vScrollBar.Value;
            Invalidate();
        };
    }

    /// <summary>
    /// Called when the theme changes. Updates listbox-specific colors.
    /// </summary>
    /// <param name="newTheme">The new theme that was activated.</param>
    public override void OnThemeChanged(Theme newTheme)
    {
        if (!_backColorSet)
            _backColor = newTheme.TextBoxBackground;
        if (!_foreColorSet)
            _foreColor = newTheme.TextBoxText;
        Invalidate();
    }

    /// <summary>
    /// Gets or sets the value displayed while a data source is being set.
    /// </summary>
    protected string? DataSourceDisplayValue { get; set; }

    /// <summary>
    /// Gets the inner item list. Items are displayed according to DisplayMember if set.
    /// When DataSource is set, items are populated from the data source.
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
    /// Gets or sets the data source for this list box.
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
                EnsureSelectedVisible();
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
    /// Renders the list box with its items and scrollbar.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
    public override void Render(Rendering.Graphics g)
    {
        if (!Visible) return;

        var theme = ThemeManager.CurrentTheme;
        var font = EffectiveFont;
        var itemHeight = CoordinateTransform.GetItemHeight(font, EffectiveZoom);
        int totalContentHeight = _items.Count * itemHeight;

        _vScrollBar.ViewSize = Height;
        _vScrollBar.ContentSize = totalContentHeight;
        _vScrollBar.SmallChange = itemHeight;
        _vScrollBar.LargeChange = itemHeight * 3;

        bool needScrollbar = _vScrollBar.NeedsScrollbar;
        int scrollBarWidth = needScrollbar ? ScrollBarEngine.DefaultScrollBarSize : 0;
        int listWidth = Width - scrollBarWidth;

        g.FillRectangle(BackColor, 0, 0, Width, Height);

        g.SetClip(new Rectangle(0, 0, listWidth, Height));

        var textColor = Enabled ? ForeColor : theme.GrayText;
        int yStart = -_scrollOffset;
        for (int i = 0; i < _items.Count; i++)
        {
            int y = yStart + i * itemHeight;
            if (y + itemHeight <= 0) continue;
            if (y >= Height) break;

            var isSelected = i == _selectedIndex;
            var displayText = GetItemDisplayText(_items[i]);

            if (isSelected)
            {
                if (Enabled)
                    g.FillRectangle(theme.Highlight, 1, y, listWidth - 2, itemHeight);
                g.DrawString(displayText, font, Enabled ? theme.HighlightText : textColor, 4, y + 2);
            }
            else
            {
                g.DrawString(displayText, font, textColor, 4, y + 2);
            }
        }

        g.ResetClip();

        if (needScrollbar)
        {
            var scrollBarBounds = new Rectangle(listWidth, 0, scrollBarWidth, Height);
            _vScrollBar.Render(g, scrollBarBounds, theme);
        }

        // Draw border after scrollbar to ensure it stays on top
        if (Focused)
            g.DrawRectangle(theme.TextBoxFocusBorder, 0, 0, Width, Height, 2);
        else
            g.DrawRectangle(theme.TextBoxBorder, 0, 0, Width, Height, 1);

        base.Render(g);
    }

    /// <summary>
    /// Raises the MouseDown event and selects an item.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseDown(EventArgs e)
    {
        if (!Enabled) return;
        var mouseArgs = e as MouseEventArgs;
        if (mouseArgs != null)
        {
            int scrollBarWidth = _vScrollBar.NeedsScrollbar ? ScrollBarEngine.DefaultScrollBarSize : 0;

            if (_vScrollBar.NeedsScrollbar && mouseArgs.X >= Width - scrollBarWidth)
            {
                var scrollBarBounds = new Rectangle(Width - scrollBarWidth, 0, scrollBarWidth, Height);
                _vScrollBar.HandleMouseDown(new Point(mouseArgs.X, mouseArgs.Y), scrollBarBounds, VScrollBarContext);
                return;
            }

            var font = EffectiveFont;
            var itemHeight = CoordinateTransform.GetItemHeight(font, EffectiveZoom);
            var index = (mouseArgs.Y + _scrollOffset - 2) / itemHeight;

            if (index >= 0 && index < _items.Count)
            {
                SelectedIndex = index;
            }
        }

        base.OnMouseDown(e);
    }

    /// <summary>
    /// Raises the MouseUp event to handle scrollbar interaction.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseUp(EventArgs e)
    {
        if (_vScrollBar.IsDragging || _vScrollBar.IsUpButtonPressed || _vScrollBar.IsDownButtonPressed)
        {
            _vScrollBar.HandleMouseUp(VScrollBarContext);
        }
        base.OnMouseUp(e);
    }

    /// <summary>
    /// Raises the MouseMove event to handle scrollbar hover and drag.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseMove(EventArgs e)
    {
        if (_vScrollBar.NeedsScrollbar)
        {
            var mouseArgs = e as MouseEventArgs;
            if (mouseArgs != null)
            {
                var scrollBarBounds = new Rectangle(Width - ScrollBarEngine.DefaultScrollBarSize, 0, ScrollBarEngine.DefaultScrollBarSize, Height);
                _vScrollBar.HandleMouseMove(new Point(mouseArgs.X, mouseArgs.Y), scrollBarBounds, VScrollBarContext);
            }
        }
        base.OnMouseMove(e);
    }

    /// <summary>
    /// Raises the MouseWheel event to handle vertical scrolling.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseWheel(EventArgs e)
    {
        if (!Enabled) return;
        var me = e as MouseEventArgs;
        if (me != null)
        {
            var itemHeight = CoordinateTransform.GetItemHeight(EffectiveFont, EffectiveZoom);
            int totalContentHeight = _items.Count * itemHeight;
            _vScrollBar.SmallChange = itemHeight;
            _vScrollBar.ViewSize = Height;
            _vScrollBar.ContentSize = totalContentHeight;
            if (_vScrollBar.NeedsScrollbar && !_vScrollBar.IsDragging)
            {
                _vScrollBar.HandleMouseWheel(me.Delta, VScrollBarContext);
            }
        }
        base.OnMouseWheel(e);
    }

    /// <summary>
    /// Raises the KeyDown event to handle navigation.
    /// </summary>
    /// <param name="e">A KeyEventArgs that contains the event data.</param>
    protected internal override void OnKeyDown(KeyEventArgs e)
    {
        if (!Enabled) return;
        switch (e.KeyCode)
        {
            case Keys.Up:
                if (_selectedIndex > 0)
                {
                    SelectedIndex--;
                    e.Handled = true;
                }
                break;
            case Keys.Down:
                if (_selectedIndex < _items.Count - 1)
                {
                    SelectedIndex++;
                    e.Handled = true;
                }
                break;
            case Keys.Home:
                if (_items.Count > 0)
                {
                    SelectedIndex = 0;
                    e.Handled = true;
                }
                break;
            case Keys.End:
                if (_items.Count > 0)
                {
                    SelectedIndex = _items.Count - 1;
                    e.Handled = true;
                }
                break;
        }
        base.OnKeyDown(e);
    }

    private void EnsureSelectedVisible()
    {
        if (_selectedIndex < 0) return;

        var font = EffectiveFont;
        var itemHeight = CoordinateTransform.GetItemHeight(font, EffectiveZoom);
        _vScrollBar.ViewSize = Height;
        _vScrollBar.ContentSize = _items.Count * itemHeight;
        _vScrollBar.EnsureVisible(_selectedIndex * itemHeight, itemHeight);
    }

    /// <summary>
    /// Raises the SelectedIndexChanged event.
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
            if (_previousBindingList != null)
                _previousBindingList.ListChanged -= OnDataSourceListChanged;
            _previousBindingList = null;

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
                    _previousBindingList = list;
                }
                bs.CurrentChanged += OnBoundBindingSourceCurrentChanged;
            }
            else if (_dataSource is IBindingList bindingList)
            {
                foreach (var item in bindingList)
                    _items.Add(item!);
                bindingList.ListChanged += OnDataSourceListChanged;
                _previousBindingList = bindingList;
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
                {
                    _items.Insert(e.NewIndex, bindingList[e.NewIndex]!);
                }
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

    private sealed class ListBoxScrollBarContext : IScrollBarContext
    {
        private readonly ListBox _owner;
        public ListBoxScrollBarContext(ListBox owner) => _owner = owner;
        public float Zoom => _owner.EffectiveZoom;
        public void Invalidate() => _owner.Invalidate();
        public void CaptureMouse(bool capture) => _owner.CapturingMouse = capture;
    }
}
