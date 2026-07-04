using System.Collections;
using System.ComponentModel;
using OldSchoolForms.Ui.Core;
using OldSchoolForms.Ui.Data;
using OldSchoolForms.Ui.Theming;
using Graphics = OldSchoolForms.Ui.Rendering.Graphics;

namespace OldSchoolForms.Ui.Controls.Advanced;

/// <summary>
/// A Kanban board control that displays data items as cards organized in configurable columns.
/// Supports drag-and-drop between columns and within columns, data binding, keyboard navigation,
/// and configurable state transitions.
/// </summary>
public class KanbanBoardView : Control
{
    private object? _dataSource;
    private string _displayMember = string.Empty;
    private string _descriptionMember = string.Empty;
    private string _valueMember = string.Empty;
    private string _stateMember = string.Empty;
    private string _tagMember = string.Empty;
    private string _categoryMember = string.Empty;
    private string _assignedToMember = string.Empty;
    private bool _dataSourceUpdating;

    private KanbanBoardColumnCollection? _columnsCollection;
    private readonly List<KanbanBoardColumn> _columns = new();

    private BindingSource? _boundBindingSource;
    private IBindingList? _previousBindingList;

    private readonly Dictionary<object, List<KanbanBoardCard>> _columnCards = new();

    // Category colors for chips
    private Dictionary<string, Color> _categoryColors = new();
    private bool _allowDrop = true;
    private bool _allowReorder = true;

    // Scrollbar
    private readonly ScrollBarEngine _vScrollBar = new();
    private KanbanScrollBarContext? _scrollBarContext;
    private int _scrollOffset;
    private int _totalContentHeight;

    private KanbanScrollBarContext VScrollBarContext =>
        _scrollBarContext ??= new KanbanScrollBarContext(this);

    // Drag state
    private KanbanBoardCard? _dragCard;
    private KanbanBoardColumn? _dragFromColumn;
    private int _dragFromIndex;
    private Point _dragStartPoint;
    private Point _dragCurrentPoint;
    private bool _isDragging;
    private KanbanBoardColumn? _dropTargetColumn;
    private int _dropTargetIndex;
    private const int DragThreshold = 8;
    private const int CardHorizPadding = 10;
    private const int CardVertPadding = 8;
    private const int CardGap = 6;
    private const int ColumnHeaderHeight = 32;
    private const int ColumnBodyPadding = 6;
    private const int ColumnGap = 8;

    private long _lastDragRenderTicks;

    // Focus state
    private KanbanBoardCard? _focusedCard;
    private int _focusedCardColumnIndex = -1;
    private int _focusedCardIndex = -1;

    // Layout cache
    private bool _layoutDirty = true;
    private List<ColumnLayout> _columnLayouts = new();
    private readonly Dictionary<string, int> _pillWidthCache = new();
    private int _lastRenderWidth;
    private int _lastRenderHeight;
    private int _lastRenderContentVersion = -1;
    private int _contentVersion;
    private Font? _cachedTitleFont;


    private struct ColumnLayout
    {
        public KanbanBoardColumn Column;
        public Rectangle Bounds;
        public List<CardLayout> CardLayouts;
    }

    private struct CardLayout
    {
        public KanbanBoardCard Card;
        public Rectangle Bounds;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="KanbanBoardView"/>.
    /// </summary>
    public KanbanBoardView()
    {
        var theme = ThemeManager.CurrentTheme;
        _backColor = theme.WindowBackground;
        Size = new Size(800, 400);
        TabStop = true;
        _vScrollBar.Scroll += (s, e) =>
        {
            _scrollOffset = _vScrollBar.Value;
            _layoutDirty = true;
            Invalidate();
        };
    }

    /// <inheritdoc />
    public override void OnThemeChanged(Theme newTheme)
    {
        if (!_backColorSet)
            _backColor = newTheme.WindowBackground;
        foreach (var col in _columns)
            col.ApplyTheme(newTheme);
        Invalidate();
    }

    #region Data Binding Properties

    /// <summary>
    /// Gets or sets the data source for the Kanban board.
    /// Supports <see cref="BindingSource"/>, <see cref="IBindingList"/>, or <see cref="IEnumerable"/>.
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
    /// Gets or sets the property name used as the card title.
    /// </summary>
    public string DisplayMember
    {
        get => _displayMember;
        set
        {
            if (_displayMember != value)
            {
                _displayMember = value;
                RepopulateCards();
                OnPropertyChanged(nameof(DisplayMember));
            }
        }
    }

    /// <summary>
    /// Gets or sets the property name used as the card description text.
    /// </summary>
    public string DescriptionMember
    {
        get => _descriptionMember;
        set
        {
            if (_descriptionMember != value)
            {
                _descriptionMember = value;
                RepopulateCards();
                OnPropertyChanged(nameof(DescriptionMember));
            }
        }
    }

    /// <summary>
    /// Gets or sets the property name used to get the value from each item.
    /// </summary>
    public string ValueMember
    {
        get => _valueMember;
        set
        {
            if (_valueMember != value)
            {
                _valueMember = value;
                RepopulateCards();
                OnPropertyChanged(nameof(ValueMember));
            }
        }
    }

    /// <summary>
    /// Gets or sets the property name used to determine the state/column of each item.
    /// Cards are placed in the column whose <see cref="KanbanBoardColumn.StateValue"/> matches this property's value.
    /// </summary>
    public string StateMember
    {
        get => _stateMember;
        set
        {
            if (_stateMember != value)
            {
                _stateMember = value;
                RepopulateCards();
                OnPropertyChanged(nameof(StateMember));
            }
        }
    }

    /// <summary>
    /// Gets or sets the property name for tags. The property must return <see cref="IEnumerable{T}"/> of <see cref="string"/>.
    /// </summary>
    public string TagMember
    {
        get => _tagMember;
        set
        {
            if (_tagMember != value)
            {
                _tagMember = value;
                RepopulateCards();
                OnPropertyChanged(nameof(TagMember));
            }
        }
    }

    /// <summary>
    /// Gets or sets the property name for the category string.
    /// </summary>
    public string CategoryMember
    {
        get => _categoryMember;
        set
        {
            if (_categoryMember != value)
            {
                _categoryMember = value;
                RepopulateCards();
                OnPropertyChanged(nameof(CategoryMember));
            }
        }
    }

    /// <summary>
    /// Gets or sets the property name for the assigned-to (assignee) string.
    /// </summary>
    public string AssignedToMember
    {
        get => _assignedToMember;
        set
        {
            if (_assignedToMember != value)
            {
                _assignedToMember = value;
                RepopulateCards();
                OnPropertyChanged(nameof(AssignedToMember));
            }
        }
    }

    #endregion

    #region Configuration Properties

    /// <summary>
    /// Gets the collection of columns for this Kanban board.
    /// Columns must be configured before or after setting the data source.
    /// </summary>
    public KanbanBoardColumnCollection Columns =>
        _columnsCollection ??= new KanbanBoardColumnCollection(OnColumnsChanged);

    private void OnColumnsChanged()
    {
        _columns.Clear();
        foreach (var col in _columnsCollection!)
            _columns.Add(col);
        _layoutDirty = true;
        _contentVersion++;
        _pillWidthCache.Clear();
        RepopulateCards();
        Invalidate();
    }

    /// <summary>
    /// Gets or sets the mapping of category names to chip background colors.
    /// </summary>
    public Dictionary<string, Color> CategoryColors
    {
        get => _categoryColors;
        set
        {
            _categoryColors = value ?? new Dictionary<string, Color>();
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets whether drag-and-drop is allowed globally.
    /// </summary>
    public bool AllowDrop
    {
        get => _allowDrop;
        set => _allowDrop = value;
    }

    /// <summary>
    /// Gets or sets whether reordering cards within a column is allowed.
    /// </summary>
    public bool AllowReorder
    {
        get => _allowReorder;
        set => _allowReorder = value;
    }

    /// <summary>
    /// Gets or sets the list of allowed state transitions for drag-and-drop moves.
    /// Each tuple is (FromStateValue, ToStateValue). An empty list means all transitions are allowed.
    /// </summary>
    public List<(object? FromState, object? ToState)> AllowedTransitions { get; set; } = new();

    /// <summary>
    /// Gets the currently selected card, or null if none is selected.
    /// </summary>
    public KanbanBoardCard? SelectedCard { get; private set; }

    /// <summary>
    /// Gets the currently focused card, or null if none is focused.
    /// </summary>
    public KanbanBoardCard? FocusedCard => _focusedCard;

    #endregion

    #region Events

    /// <summary>
    /// Occurs when a card receives focus via mouse or keyboard.
    /// </summary>
    public event EventHandler<KanbanBoardCardEventArgs>? CardGotFocus;

    /// <summary>
    /// Occurs when a card is clicked.
    /// </summary>
    public event EventHandler<KanbanBoardCardEventArgs>? CardClick;

    /// <summary>
    /// Occurs when a card is double-clicked.
    /// </summary>
    public event EventHandler<KanbanBoardCardEventArgs>? CardDoubleClick;

    /// <summary>
    /// Occurs before a card is moved to a different column. Set <see cref="CancelEventArgs.Cancel"/> to true to prevent the move.
    /// </summary>
    public event EventHandler<KanbanBoardCardMovingEventArgs>? CardMoving;

    /// <summary>
    /// Occurs after a card has been moved to a different column.
    /// </summary>
    public event EventHandler<KanbanBoardCardMovingEventArgs>? CardMoved;

    /// <summary>
    /// Occurs before a card is reordered within its column. Set <see cref="CancelEventArgs.Cancel"/> to true to prevent the reorder.
    /// </summary>
    public event EventHandler<KanbanBoardCardReorderingEventArgs>? CardReordering;

    /// <summary>
    /// Occurs after a card has been reordered within its column.
    /// </summary>
    public event EventHandler<KanbanBoardCardReorderingEventArgs>? CardReordered;

    #endregion

    #region Event Raisers

    /// <summary>
    /// Raises the <see cref="CardGotFocus"/> event.
    /// </summary>
    /// <param name="e">The event data.</param>
    protected virtual void OnCardGotFocus(KanbanBoardCardEventArgs e)
    {
        CardGotFocus?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the <see cref="CardClick"/> event.
    /// </summary>
    /// <param name="e">The event data.</param>
    protected virtual void OnCardClick(KanbanBoardCardEventArgs e)
    {
        CardClick?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the <see cref="CardDoubleClick"/> event.
    /// </summary>
    /// <param name="e">The event data.</param>
    protected virtual void OnCardDoubleClick(KanbanBoardCardEventArgs e)
    {
        CardDoubleClick?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the <see cref="CardMoving"/> event.
    /// </summary>
    /// <param name="e">The event data.</param>
    protected virtual void OnCardMoving(KanbanBoardCardMovingEventArgs e)
    {
        CardMoving?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the <see cref="CardMoved"/> event.
    /// </summary>
    /// <param name="e">The event data.</param>
    protected virtual void OnCardMoved(KanbanBoardCardMovingEventArgs e)
    {
        CardMoved?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the <see cref="CardReordering"/> event.
    /// </summary>
    /// <param name="e">The event data.</param>
    protected virtual void OnCardReordering(KanbanBoardCardReorderingEventArgs e)
    {
        CardReordering?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the <see cref="CardReordered"/> event.
    /// </summary>
    /// <param name="e">The event data.</param>
    protected virtual void OnCardReordered(KanbanBoardCardReorderingEventArgs e)
    {
        CardReordered?.Invoke(this, e);
    }

    #endregion

    #region Data Binding Implementation

    /// <summary>
    /// Called when the DataSource property changes. Rebuilds cards from the data source.
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

            ClearAllCards();
            _columnCards.Clear();

            if (_dataSource is BindingSource bs)
            {
                _boundBindingSource = bs;
                var list = bs.List;
                if (list != null)
                {
                    foreach (var item in list)
                        AddCardForItem(item!);
                    list.ListChanged += OnDataSourceListChanged;
                    _previousBindingList = list;
                }
                bs.CurrentChanged += OnBoundBindingSourceCurrentChanged;
            }
            else if (_dataSource is IBindingList bindingList)
            {
                foreach (var item in bindingList)
                    AddCardForItem(item!);
                bindingList.ListChanged += OnDataSourceListChanged;
                _previousBindingList = bindingList;
            }
            else if (_dataSource is IEnumerable enumerable && _dataSource is not string)
            {
                foreach (var item in enumerable)
                    AddCardForItem(item!);
            }

            _layoutDirty = true;
            _contentVersion++;
            _pillWidthCache.Clear();
            Invalidate();
        }
        finally
        {
            _dataSourceUpdating = false;
        }
    }

    private void OnBoundBindingSourceCurrentChanged(object? sender, EventArgs e)
    {
    }

    private void OnDataSourceListChanged(object? sender, ListChangedEventArgs e)
    {
        if (_dataSource is not IBindingList bindingList)
            return;

        switch (e.ListChangedType)
        {
            case ListChangedType.ItemAdded:
                if (e.NewIndex >= 0 && e.NewIndex < bindingList.Count)
                    AddCardForItem(bindingList[e.NewIndex]!);
                break;
            case ListChangedType.ItemDeleted:
                RemoveCardForItem(e.NewIndex);
                break;
            case ListChangedType.ItemChanged:
                if (e.NewIndex >= 0 && e.NewIndex < bindingList.Count)
                    UpdateCardForItem(bindingList[e.NewIndex]!, e.NewIndex);
                break;
            case ListChangedType.Reset:
                ClearAllCards();
                foreach (var item in bindingList)
                    AddCardForItem(item!);
                break;
        }

        Invalidate();
    }

    private void ClearAllCards()
    {
        _columnCards.Clear();
        foreach (var col in _columns)
            col.Cards.Clear();
        _focusedCard = null;
        _focusedCardColumnIndex = -1;
        _focusedCardIndex = -1;
        SelectedCard = null;
    }

    private void AddCardForItem(object item)
    {
        var card = new KanbanBoardCard(item);
        PopulateCardDisplayValues(card);

        var targetCol = FindColumnForState(card.StateValue);
        if (targetCol != null)
        {
            targetCol.Cards.Add(card);
            AddCardToCache(targetCol, card);
        }
    }

    private void RemoveCardForItem(int index)
    {
        foreach (var col in _columns)
        {
            for (int i = 0; i < col.Cards.Count; i++)
            {
                if (GetItemIndex(col.Cards[i]) == index)
                {
                    if (_focusedCard == col.Cards[i])
                    {
                        _focusedCard = null;
                        _focusedCardColumnIndex = -1;
                        _focusedCardIndex = -1;
                    }
                    if (SelectedCard == col.Cards[i])
                        SelectedCard = null;
                    col.Cards.RemoveAt(i);
                    return;
                }
            }
        }
    }

    private void UpdateCardForItem(object item, int index)
    {
        foreach (var col in _columns)
        {
            for (int i = 0; i < col.Cards.Count; i++)
            {
                if (GetItemIndex(col.Cards[i]) == index)
                {
                    var card = col.Cards[i];
                    var oldState = card.StateValue;
                    PopulateCardDisplayValues(card);

                    if (!Equals(oldState, card.StateValue))
                    {
                        col.Cards.RemoveAt(i);
                        var newCol = FindColumnForState(card.StateValue);
                        if (newCol != null)
                        {
                            newCol.Cards.Add(card);
                        }
                    }
                    return;
                }
            }
        }
    }

    private int GetItemIndex(KanbanBoardCard card)
    {
        if (_dataSource is IBindingList bl)
        {
            for (int i = 0; i < bl.Count; i++)
            {
                if (ReferenceEquals(bl[i], card.DataItem))
                    return i;
            }
        }
        return -1;
    }

    private void PopulateCardDisplayValues(KanbanBoardCard card)
    {
        var item = card.DataItem;
        card.DisplayText = GetPropertyString(item, _displayMember);
        card.DescriptionText = GetPropertyString(item, _descriptionMember);
        card.CategoryText = GetPropertyString(item, _categoryMember);
        card.AssignedToText = GetPropertyString(item, _assignedToMember);
        card.Value = GetPropertyValue(item, _valueMember);
        card.StateValue = GetPropertyValue(item, _stateMember);
        card.Tags = GetPropertyEnumerableStrings(item, _tagMember);
    }

    private static string GetPropertyString(object item, string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return string.Empty;
        var prop = item.GetType().GetProperty(propertyName);
        return prop?.GetValue(item)?.ToString() ?? string.Empty;
    }

    private static object? GetPropertyValue(object item, string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return null;
        var prop = item.GetType().GetProperty(propertyName);
        return prop?.GetValue(item);
    }

    private static IReadOnlyList<string> GetPropertyEnumerableStrings(object item, string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return Array.Empty<string>();
        var prop = item.GetType().GetProperty(propertyName);
        if (prop?.GetValue(item) is IEnumerable<string> strings)
            return strings.ToList().AsReadOnly();
        return Array.Empty<string>();
    }

    private static void SetPropertyValue(object item, string propertyName, object? value)
    {
        if (string.IsNullOrEmpty(propertyName))
            return;
        var prop = item.GetType().GetProperty(propertyName);
        if (prop != null && prop.CanWrite)
            prop.SetValue(item, value);
    }

    private KanbanBoardColumn? FindColumnForState(object? stateValue)
    {
        foreach (var col in _columns)
        {
            if (Equals(col.StateValue, stateValue))
                return col;
        }
        return _columns.Count > 0 ? _columns[0] : null;
    }

    private void AddCardToCache(KanbanBoardColumn column, KanbanBoardCard card)
    {
        var key = column.StateValue ?? new object();
        if (!_columnCards.TryGetValue(key, out var list))
        {
            list = new List<KanbanBoardCard>();
            _columnCards[key] = list;
        }
        list.Add(card);
    }

    /// <summary>
    /// Repopulates all cards from the current data source without changing the data source reference.
    /// </summary>
    protected void RepopulateCards()
    {
        if (_dataSource is IEnumerable enumerable && _dataSource is not string)
        {
            ClearAllCards();
            foreach (var item in enumerable)
                AddCardForItem(item!);
            Invalidate();
        }
    }

    #endregion

    #region Layout

    private void ComputeLayout()
    {
        _columnLayouts.Clear();
        if (_columns.Count == 0) return;

        int padLeft = Padding.Left;
        int padTop = Padding.Top;
        int padRight = Padding.Right;
        int padBottom = Padding.Bottom;

        int scrollBarWidth = _vScrollBar.NeedsScrollbar ? ScrollBarEngine.DefaultScrollBarSize : 0;
        int availableWidth = Width - padLeft - padRight - scrollBarWidth;
        int availableHeight = Height - padTop - padBottom;

        int totalGaps = ColumnGap * (_columns.Count - 1);
        int colWidth = (availableWidth - totalGaps) / _columns.Count;
        int extra = (availableWidth - totalGaps) - colWidth * _columns.Count;

        int x = padLeft;
        _totalContentHeight = 0;

        for (int i = 0; i < _columns.Count; i++)
        {
            var col = _columns[i];
            int w = colWidth + (i < extra ? 1 : 0);

            var layouts = new List<CardLayout>();
            int cardY = padTop + ColumnHeaderHeight + ColumnBodyPadding;
            int cardAvailableWidth = w - ColumnBodyPadding * 2;

            foreach (var card in col.Cards)
            {
                int cardHeight = ComputeCardHeight(card, cardAvailableWidth);
                var cardBounds = new Rectangle(
                    x + ColumnBodyPadding,
                    cardY - _scrollOffset,
                    cardAvailableWidth,
                    cardHeight);
                layouts.Add(new CardLayout { Card = card, Bounds = cardBounds });
                cardY += cardHeight + CardGap;
            }

            int colHeight = availableHeight;
            int columnContentEnd = cardY - CardGap + ColumnBodyPadding;
            _totalContentHeight = Math.Max(_totalContentHeight, columnContentEnd);

            var colBounds = new Rectangle(x, padTop, w, colHeight);
            _columnLayouts.Add(new ColumnLayout
            {
                Column = col,
                Bounds = colBounds,
                CardLayouts = layouts
            });

            x += w + ColumnGap;
            if (x >= Width - padRight - scrollBarWidth) break;
        }

        _totalContentHeight = Math.Max(_totalContentHeight, availableHeight);
        _vScrollBar.ViewSize = availableHeight;
        _vScrollBar.ContentSize = _totalContentHeight;
        _vScrollBar.SmallChange = 20;
        _vScrollBar.LargeChange = 80;
    }

    private int ComputeCardHeight(KanbanBoardCard card, int availableWidth)
    {
        int height = CardVertPadding * 2;
        int scaledFontHeight = (int)(EffectiveFont.Size * EffectiveZoom);
        int pillHeight = Math.Max(scaledFontHeight + 6, 22);

        if (!string.IsNullOrEmpty(card.DisplayText))
            height += 18;

        if (!string.IsNullOrEmpty(card.DescriptionText))
            height += 14 + 6;

        if (!string.IsNullOrEmpty(card.CategoryText))
            height += pillHeight + 4;

        if (card.Tags.Count > 0)
            height += pillHeight + 4;

        if (!string.IsNullOrEmpty(card.AssignedToText))
            height += 14 + 4;

        if (card.Value != null)
            height += 14;

        return Math.Max(50, height);
    }

    #endregion

    #region Rendering

    /// <inheritdoc />
    public override void Render(Graphics g)
    {
        if (!Visible) return;

        var theme = ThemeManager.CurrentTheme;

        g.FillRectangle(BackColor, 0, 0, Width, Height);

        if (_layoutDirty || Width != _lastRenderWidth || Height != _lastRenderHeight)
            ComputeLayout();

        _layoutDirty = false;
        _lastRenderWidth = Width;
        _lastRenderHeight = Height;
        _lastRenderContentVersion = _contentVersion;

        var name = EffectiveFont.Name;
        var size = EffectiveFont.Size;
        if (_cachedTitleFont == null || _cachedTitleFont.Name != name || _cachedTitleFont.Size != size)
            _cachedTitleFont = new Font(name, size, FontStyle.Bold);

        foreach (var colLayout in _columnLayouts)
        {
            var col = colLayout.Column;
            var bounds = colLayout.Bounds;

            bool isDropTarget = _dropTargetColumn == col && _isDragging;

            // Column body background
            g.FillRectangle(col.BackColor, bounds.X, bounds.Y, bounds.Width, bounds.Height);

            // Drop target highlight
            if (isDropTarget)
            {
                g.FillRectangle(Color.FromArgb(theme.Highlight.R, theme.Highlight.G, theme.Highlight.B, 40),
                    bounds.X, bounds.Y, bounds.Width, bounds.Height);
                g.DrawRectangle(theme.Highlight, bounds.X, bounds.Y, bounds.Width, bounds.Height, 2);
            }

            // Column header
            g.FillRectangle(col.HeaderColor, bounds.X, bounds.Y, bounds.Width, ColumnHeaderHeight);

            string headerText = $"{col.Name} ({col.CardCount})";
            g.DrawString(headerText, EffectiveFont, Color.White, bounds.X + 6, bounds.Y + 6);

            // Draw cards
            foreach (var cardLayout in colLayout.CardLayouts)
            {
                DrawCard(g, cardLayout, theme);
            }
        }

        // Draw drag ghost
        if (_isDragging && _dragCard != null)
        {
            DrawDragGhost(g, _dragCard, theme);
        }

        // Draw scrollbar
        if (_vScrollBar.NeedsScrollbar)
        {
            var scrollBarBounds = new Rectangle(
                Width - ScrollBarEngine.DefaultScrollBarSize,
                0,
                ScrollBarEngine.DefaultScrollBarSize,
                Height);
            _vScrollBar.Render(g, scrollBarBounds, theme);

            // Divider line between board content and scrollbar
            g.FillRectangle(theme.ControlDark, scrollBarBounds.X - 1, 0, 1, Height);
        }

        base.Render(g);
    }

    private void DrawCard(Graphics g, CardLayout layout, Theme theme)
    {
        var card = layout.Card;
        var r = layout.Bounds;
        bool isFocused = card == _focusedCard;
        bool isSelected = card == SelectedCard;

        var cardBack = isSelected
            ? Color.FromArgb(220, 235, 255)
            : Color.White;
        g.FillRectangle(cardBack, r.X, r.Y, r.Width, r.Height);

        if (isFocused)
            g.DrawRectangle(theme.FocusIndicator, r.X, r.Y, r.Width, r.Height, 2);

        int textX = r.X + CardHorizPadding;
        int textY = r.Y + CardVertPadding;
        int maxTextWidth = r.Width - CardHorizPadding * 2;
        int scaledFontHeight = (int)(EffectiveFont.Size * EffectiveZoom);

        if (!string.IsNullOrEmpty(card.DisplayText))
        {
            g.DrawString(card.DisplayText, _cachedTitleFont!, theme.ControlText, textX, textY);
            textY += 18;
        }

        if (!string.IsNullOrEmpty(card.DescriptionText))
        {
            g.DrawString(card.DescriptionText, EffectiveFont, theme.ControlText, textX, textY);
            textY += 14 + 6;
        }

        if (!string.IsNullOrEmpty(card.CategoryText))
        {
            var chipColor = _categoryColors.TryGetValue(card.CategoryText, out var catColor)
                ? catColor
                : Color.FromArgb(180, 180, 180);
            var chipTextColor = Color.White;

            int pillHeight = Math.Max(scaledFontHeight + 6, 22);
            int chipWidth = GetPillWidth(g, card.CategoryText) + 8;
            chipWidth = Math.Min(chipWidth, maxTextWidth);
            int textOffset = Math.Max(1, (pillHeight - scaledFontHeight) / 2);
            g.FillRectangle(chipColor, textX, textY, chipWidth, pillHeight);
            g.DrawString(card.CategoryText, EffectiveFont, chipTextColor, textX + 4, textY + textOffset);
            textY += pillHeight + 4;
        }

        if (card.Tags.Count > 0)
        {
            int tagX = textX;
            int tagY2 = textY;
            int pillHeight = Math.Max(scaledFontHeight + 6, 22);
            foreach (var tag in card.Tags)
            {
                if (string.IsNullOrEmpty(tag)) continue;
                int tagWidth = GetPillWidth(g, tag) + 8;
                if (tagX + tagWidth > textX + maxTextWidth)
                {
                    tagX = textX;
                    tagY2 += pillHeight + 2;
                }
                var tagColor = Color.FromArgb(230, 230, 235);
                g.FillRectangle(tagColor, tagX, tagY2, tagWidth, pillHeight);
                int textOffset = Math.Max(1, (pillHeight - scaledFontHeight) / 2);
                g.DrawString(tag, EffectiveFont, theme.ControlText, tagX + 4, tagY2 + textOffset);
                tagX += tagWidth + 4;
            }
            textY = tagY2 + pillHeight + 4;
        }

        if (!string.IsNullOrEmpty(card.AssignedToText))
        {
            g.DrawString(card.AssignedToText, EffectiveFont, theme.ControlText, textX, textY);
            textY += 14 + 4;
        }

        if (card.Value != null)
        {
            string valueText = card.Value.ToString() ?? string.Empty;
            g.DrawString(valueText, EffectiveFont, theme.GrayText, textX, textY);
        }
    }

    private int GetPillWidth(Graphics g, string text)
    {
        if (!_pillWidthCache.TryGetValue(text, out var width))
        {
            width = g.MeasureString(text, EffectiveFont, 1.0f).width;
            _pillWidthCache[text] = width;
        }
        return width;
    }

    private void DrawDragGhost(Graphics g, KanbanBoardCard card, Theme theme)
    {
        int ghostWidth = 200;
        int ghostHeight = 80;
        int ghostX = _dragCurrentPoint.X - ghostWidth / 2;
        int ghostY = _dragCurrentPoint.Y - 10;

        g.FillRectangle(Color.FromArgb(200, 220, 240, 180), ghostX, ghostY, ghostWidth, ghostHeight);
        g.DrawRectangle(Color.FromArgb(theme.Highlight.R, theme.Highlight.G, theme.Highlight.B, 100), ghostX, ghostY, ghostWidth, ghostHeight, 2);

        g.DrawString(card.DisplayText, EffectiveFont, theme.ControlText, ghostX + 8, ghostY + 6);

        if (!string.IsNullOrEmpty(card.DescriptionText))
            g.DrawString(card.DescriptionText, EffectiveFont, theme.GrayText, ghostX + 8, ghostY + 26);

        // Show destination info
        string destInfo;
        if (_dropTargetColumn != null && _dropTargetColumn != _dragFromColumn)
            destInfo = $"-> {_dropTargetColumn.Name}";
        else if (_dropTargetColumn != null && _dropTargetColumn == _dragFromColumn)
            destInfo = "Reorder";
        else
            destInfo = "";

        if (!string.IsNullOrEmpty(destInfo))
            g.DrawString(destInfo, EffectiveFont, theme.Highlight, ghostX + 8, ghostY + ghostHeight - 18);
    }

    #endregion

    #region Hit Testing

    private (KanbanBoardColumn? column, KanbanBoardCard? card, int cardIndex, int columnIndex) BoardHitTest(Point point)
    {
        foreach (var colLayout in _columnLayouts)
        {
            if (!colLayout.Bounds.Contains(point.X, point.Y))
                continue;

            int colIndex = _columnLayouts.IndexOf(colLayout);
            int bodyStartY = colLayout.Bounds.Y + ColumnHeaderHeight;
            if (point.Y < bodyStartY)
                return (colLayout.Column, null, 0, colIndex);

            for (int i = 0; i < colLayout.CardLayouts.Count; i++)
            {
                var cardLayout = colLayout.CardLayouts[i];
                if (cardLayout.Bounds.Contains(point.X, point.Y))
                    return (colLayout.Column, cardLayout.Card, i, colIndex);
            }

            return (colLayout.Column, null, colLayout.CardLayouts.Count, colIndex);
        }

        return (null, null, 0, -1);
    }

    private (KanbanBoardColumn? column, int insertIndex) DropTargetHitTest(Point point)
    {
        foreach (var colLayout in _columnLayouts)
        {
            if (!colLayout.Bounds.Contains(point.X, point.Y))
                continue;

            int bodyStartY = colLayout.Bounds.Y + ColumnHeaderHeight;
            if (point.Y <= bodyStartY)
                return (colLayout.Column, 0);

            for (int i = 0; i < colLayout.CardLayouts.Count; i++)
            {
                var cardLayout = colLayout.CardLayouts[i];
                int midY = cardLayout.Bounds.Y + cardLayout.Bounds.Height / 2;
                if (point.Y < midY)
                    return (colLayout.Column, i);
            }

            return (colLayout.Column, colLayout.CardLayouts.Count);
        }

        return (null, 0);
    }

    #endregion

    #region Mouse Handling

    /// <inheritdoc />
    protected internal override void OnMouseDown(EventArgs e)
    {
        if (!Enabled) return;
        if (e is not MouseEventArgs args) { base.OnMouseDown(e); return; }

        // Forward to scrollbar
        var scrollBarBounds = new Rectangle(
            Width - ScrollBarEngine.DefaultScrollBarSize, 0,
            ScrollBarEngine.DefaultScrollBarSize, Height);
        if (_vScrollBar.NeedsScrollbar && scrollBarBounds.Contains(args.X, args.Y))
        {
            _vScrollBar.HandleMouseDown(new Point(args.X, args.Y), scrollBarBounds, VScrollBarContext);
            return;
        }

        Focused = true;
        var point = new Point(args.X, args.Y);
        var (col, card, cardIndex, colIndex) = BoardHitTest(point);

        if (card != null)
        {
            SetFocusedCard(card, colIndex, cardIndex);
            SelectedCard = card;

            if (args.Clicks >= 2)
            {
                OnCardDoubleClick(new KanbanBoardCardEventArgs(card));
                return;
            }

            OnCardClick(new KanbanBoardCardEventArgs(card));

            if (_allowDrop)
            {
                _dragCard = card;
                _dragFromColumn = col;
                _dragFromIndex = cardIndex;
                _dragStartPoint = point;
                _dragCurrentPoint = point;
                _dropTargetColumn = col;
                _dropTargetIndex = cardIndex;
            }
        }
        else
        {
            SelectedCard = null;
        }

        base.OnMouseDown(e);
    }

    /// <inheritdoc />
    protected internal override void OnMouseMove(EventArgs e)
    {
        if (e is not MouseEventArgs args) { base.OnMouseMove(e); return; }

        // Forward to scrollbar
        var scrollBarBounds = new Rectangle(
            Width - ScrollBarEngine.DefaultScrollBarSize, 0,
            ScrollBarEngine.DefaultScrollBarSize, Height);
        if (_vScrollBar.NeedsScrollbar)
            _vScrollBar.HandleMouseMove(new Point(args.X, args.Y), scrollBarBounds, VScrollBarContext);

        var point = new Point(args.X, args.Y);

        if (_dragCard != null && _allowDrop)
        {
            _dragCurrentPoint = point;

            int dx = point.X - _dragStartPoint.X;
            int dy = point.Y - _dragStartPoint.Y;
            int distance = (int)Math.Sqrt(dx * dx + dy * dy);

            if (!_isDragging && distance >= DragThreshold)
            {
                _isDragging = true;
                CapturingMouse = true;
            }

            if (_isDragging)
            {
                var (targetCol, insertIdx) = DropTargetHitTest(point);

                if (_dragFromColumn == targetCol && !_allowReorder)
                {
                    _dropTargetColumn = null;
                    _dropTargetIndex = -1;
                }
                else
                {
                    _dropTargetColumn = targetCol;
                    _dropTargetIndex = insertIdx;
                }

                Invalidate();
            }
        }

        base.OnMouseMove(e);
    }

    /// <inheritdoc />
    protected internal override void OnMouseUp(EventArgs e)
    {
        _vScrollBar.HandleMouseUp(VScrollBarContext);

        if (_dragCard != null)
        {
            if (_isDragging)
            {
                CapturingMouse = false;

                if (_dropTargetColumn != null)
                {
                    if (_dropTargetColumn != _dragFromColumn)
                    {
                        PerformColumnMove(_dragCard, _dragFromColumn!, _dropTargetColumn, _dropTargetIndex);
                    }
                    else if (_allowReorder)
                    {
                        PerformReorder(_dragCard, _dropTargetColumn, _dragFromIndex, _dropTargetIndex);
                    }
                }
            }

            _isDragging = false;
            _dragCard = null;
            _dragFromColumn = null;
            _dropTargetColumn = null;
            _dropTargetIndex = -1;
            Invalidate();
        }

        base.OnMouseUp(e);
    }

    private void PerformColumnMove(KanbanBoardCard card, KanbanBoardColumn fromCol, KanbanBoardColumn toCol, int targetIndex)
    {
        // Check allowed transitions
        if (AllowedTransitions.Count > 0)
        {
            bool allowed = false;
            foreach (var (fromState, toState) in AllowedTransitions)
            {
                if (Equals(fromState, fromCol.StateValue) && Equals(toState, toCol.StateValue))
                {
                    allowed = true;
                    break;
                }
            }
            if (!allowed) return;
        }

        var movingArgs = new KanbanBoardCardMovingEventArgs(card, fromCol, toCol, targetIndex);
        OnCardMoving(movingArgs);
        if (movingArgs.Cancel) return;

        // Move card between columns
        fromCol.Cards.Remove(card);
        toCol.Cards.Insert(Math.Min(targetIndex, toCol.Cards.Count), card);

        // Update state value on data item
        if (!string.IsNullOrEmpty(_stateMember))
        {
            SetPropertyValue(card.DataItem, _stateMember, toCol.StateValue);
            card.StateValue = toCol.StateValue;
        }

        var movedArgs = new KanbanBoardCardMovingEventArgs(card, fromCol, toCol, targetIndex);
        OnCardMoved(movedArgs);
        _layoutDirty = true;
        _contentVersion++;
    }

    private void PerformReorder(KanbanBoardCard card, KanbanBoardColumn column, int oldIndex, int newIndex)
    {
        if (oldIndex == newIndex) return;
        newIndex = Math.Clamp(newIndex, 0, column.Cards.Count - 1);

        var reorderArgs = new KanbanBoardCardReorderingEventArgs(card, column, oldIndex, newIndex);
        OnCardReordering(reorderArgs);
        if (reorderArgs.Cancel) return;

        column.Cards.RemoveAt(oldIndex);
        column.Cards.Insert(newIndex, card);

        var reorderedArgs = new KanbanBoardCardReorderingEventArgs(card, column, oldIndex, newIndex);
        OnCardReordered(reorderedArgs);
        _layoutDirty = true;
        _contentVersion++;
    }

    #endregion

    #region Keyboard Handling

    /// <inheritdoc />
    protected internal override void OnKeyDown(KeyEventArgs e)
    {
        if (!Enabled) return;

        bool handled = false;

        switch (e.KeyCode)
        {
            case Keys.Left:
                handled = MoveFocusToPreviousColumn();
                break;
            case Keys.Right:
                handled = MoveFocusToNextColumn();
                break;
            case Keys.Up:
                handled = MoveFocusToPreviousCard();
                break;
            case Keys.Down:
                handled = MoveFocusToNextCard();
                break;
            case Keys.Tab:
                if (e.Modifiers.HasFlag(ModifierKeys.Shift))
                    handled = MoveFocusToPreviousCard();
                else
                    handled = MoveFocusToNextCard();
                break;
            case Keys.Enter:
            case Keys.Space:
                if (_focusedCard != null)
                {
                    SelectedCard = _focusedCard;
                    OnCardClick(new KanbanBoardCardEventArgs(_focusedCard));
                    handled = true;
                }
                break;
            case Keys.Home:
                handled = MoveFocusToFirstCard();
                break;
            case Keys.End:
                handled = MoveFocusToLastCard();
                break;
        }

        if (handled)
        {
            e.Handled = true;
            Invalidate();
        }

        base.OnKeyDown(e);
    }

    private bool MoveFocusToPreviousColumn()
    {
        if (_focusedCard == null || _focusedCardColumnIndex <= 0) return false;

        int newColIndex = _focusedCardColumnIndex - 1;
        var newCol = _columns[newColIndex];
        if (newCol.Cards.Count == 0) return false;

        int newCardIndex = Math.Min(_focusedCardIndex, newCol.Cards.Count - 1);
        SetFocusedCard(newCol.Cards[newCardIndex], newColIndex, newCardIndex);
        return true;
    }

    private bool MoveFocusToNextColumn()
    {
        if (_focusedCard == null || _focusedCardColumnIndex >= _columns.Count - 1) return false;

        int newColIndex = _focusedCardColumnIndex + 1;
        var newCol = _columns[newColIndex];
        if (newCol.Cards.Count == 0) return false;

        int newCardIndex = Math.Min(_focusedCardIndex, newCol.Cards.Count - 1);
        SetFocusedCard(newCol.Cards[newCardIndex], newColIndex, newCardIndex);
        return true;
    }

    private bool MoveFocusToPreviousCard()
    {
        if (_focusedCard == null) return false;

        var col = _columns[_focusedCardColumnIndex];
        if (_focusedCardIndex > 0)
        {
            int newIndex = _focusedCardIndex - 1;
            SetFocusedCard(col.Cards[newIndex], _focusedCardColumnIndex, newIndex);
            return true;
        }

        // Wrap to previous column
        return MoveFocusToPreviousColumn();
    }

    private bool MoveFocusToNextCard()
    {
        if (_focusedCard == null) return false;

        var col = _columns[_focusedCardColumnIndex];
        if (_focusedCardIndex < col.Cards.Count - 1)
        {
            int newIndex = _focusedCardIndex + 1;
            SetFocusedCard(col.Cards[newIndex], _focusedCardColumnIndex, newIndex);
            return true;
        }

        // Wrap to next column
        return MoveFocusToNextColumn();
    }

    private bool MoveFocusToFirstCard()
    {
        foreach (var col in _columns)
        {
            if (col.Cards.Count > 0)
            {
                int colIndex = _columns.IndexOf(col);
                SetFocusedCard(col.Cards[0], colIndex, 0);
                return true;
            }
        }
        return false;
    }

    private bool MoveFocusToLastCard()
    {
        for (int c = _columns.Count - 1; c >= 0; c--)
        {
            var col = _columns[c];
            if (col.Cards.Count > 0)
            {
                int idx = col.Cards.Count - 1;
                SetFocusedCard(col.Cards[idx], c, idx);
                return true;
            }
        }
        return false;
    }

    private void SetFocusedCard(KanbanBoardCard? card, int colIndex, int cardIndex)
    {
        if (_focusedCard == card) return;

        _focusedCard = card;
        _focusedCardColumnIndex = colIndex;
        _focusedCardIndex = cardIndex;

        if (card != null)
            OnCardGotFocus(new KanbanBoardCardEventArgs(card));

        Invalidate();
    }

    /// <inheritdoc />
    protected internal override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        if (_focusedCard == null)
            MoveFocusToFirstCard();
    }

    /// <inheritdoc />
    protected internal override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        Invalidate();
    }

    /// <inheritdoc />
    protected internal override void OnMouseWheel(EventArgs e)
    {
        if (e is not MouseEventArgs me) { base.OnMouseWheel(e); return; }

        if (_vScrollBar.NeedsScrollbar && !_vScrollBar.IsDragging)
            _vScrollBar.HandleMouseWheel(me.Delta, VScrollBarContext);

        base.OnMouseWheel(e);
    }

    #endregion

    private sealed class KanbanScrollBarContext : IScrollBarContext
    {
        private readonly KanbanBoardView _owner;
        public KanbanScrollBarContext(KanbanBoardView owner) => _owner = owner;
        public float Zoom => _owner.EffectiveZoom;
        public void Invalidate() => _owner.Invalidate();
        public void CaptureMouse(bool capture) => _owner.CapturingMouse = capture;
    }
}
