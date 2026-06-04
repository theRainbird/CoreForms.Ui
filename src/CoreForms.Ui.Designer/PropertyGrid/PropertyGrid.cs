using System;
using System.Collections.Generic;
using System.Linq;
using CoreForms.Ui.Core;
using CoreForms.Ui.Designer.Services;
using CoreForms.Ui.Theming;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Designer.PropertyGrid;

/// <summary>
/// A property grid control that displays and edits properties of the currently
/// selected design item. Connects to <see cref="SelectionService"/> to observe
/// selection changes and re-populates the property list.
/// </summary>
public class PropertyGrid : ContainerControl
{
    private readonly PropertyService _propertyService;
    private readonly SelectionService _selectionService;
    private readonly List<PropertyGridRow> _rows = new();

    private const int HeaderHeight = 24;
    private const int RowHeight = 26;
    private const int NameColumnWidth = 140;
    private const int ValueColumnWidth = 160;
    private const int DropdownArrowWidth = 20;
    private const int DropdownItemHeight = 22;
    private const int MaxDropdownHeight = 150;

    private string? _activeCategory;
    private PropertyGridRow? _editingRow;
    private PropertyGridRow? _dropdownRow;
    private int _dropdownSelectedIndex = -1;
    private int _dropdownHoveredIndex = -1;
    private bool _dropdownOpen;
    private int _dropdownValueX;
    private int _dropdownRowY;
    private int _dropdownItemCount;

    /// <summary>
    /// Gets the current target control whose properties are being edited.
    /// </summary>
    public Control? TargetControl { get; private set; }

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="selectionService">The selection service to observe.</param>
    public PropertyGrid(SelectionService selectionService)
    {
        _propertyService = new PropertyService();
        _selectionService = selectionService ?? throw new ArgumentNullException(nameof(selectionService));
        BackColor = ThemeManager.CurrentTheme.ControlLight;

        _selectionService.SelectionChanged += OnSelectionChanged;
    }

    /// <summary>
    /// Filters the grid to show only a specific category.
    /// Pass null to show all categories.
    /// </summary>
    public void FilterByCategory(string? category)
    {
        _activeCategory = category;
        RebuildRows();
        Invalidate();
    }

    /// <summary>
    /// Forces a refresh of all property values from the selected control.
    /// </summary>
    public void RefreshValues()
    {
        RebuildRows();
        Invalidate();
    }

    private void OnSelectionChanged(object? sender, EventArgs e)
    {
        TargetControl = _selectionService.PrimarySelection?.Control;
        RebuildRows();
        Invalidate();
    }

    private void RebuildRows()
    {
        _rows.Clear();
        if (TargetControl == null) return;

        var properties = _propertyService.GetProperties(TargetControl);

        foreach (var prop in properties)
        {
            if (_activeCategory != null && prop.Category != _activeCategory)
                continue;
            _rows.Add(new PropertyGridRow(prop, NameColumnWidth));
        }
    }

    protected override void OnMouseDown(EventArgs e)
    {
        if (e is not MouseEventArgs args) return;

        int rowIndex = HitTestRow(args.Y);

       // Handle dropdown open
        if (_dropdownOpen && _dropdownRow != null)
        {
            if (args.X >= _dropdownValueX && args.X < _dropdownValueX + ValueColumnWidth &&
                args.Y >= _dropdownRowY + RowHeight && args.Y < _dropdownRowY + RowHeight + MaxDropdownHeight)
            {
                int itemIndex = (args.Y - _dropdownRowY - RowHeight) / DropdownItemHeight;
                if (itemIndex >= 0 && itemIndex < _dropdownItemCount)
                {
                    if (_dropdownRow.IsFlagsEnum)
                    {
                        _dropdownRow.SelectEnumItem(itemIndex);
                        _dropdownHoveredIndex = itemIndex;
                        Invalidate();
                    }
                    else
                    {
                        _dropdownRow.SelectEnumItem(itemIndex);
                        _dropdownRow.CommitEdit();
                        _dropdownSelectedIndex = itemIndex;
                        CloseEnumDropdown();
                        Invalidate();
                    }
                    return;
                }
            }
            else
            {
                _dropdownRow.CommitEdit();
                CloseEnumDropdown();
                Invalidate();
            }
        }

        // Commit any existing edit before processing new click
        if (_editingRow != null)
        {
            _editingRow.CommitEdit();
            _editingRow = null;
        }

        if (rowIndex >= 0 && rowIndex < _rows.Count)
        {
            var row = _rows[rowIndex];
            int rowY = GetRowY(rowIndex);
            int valueX = NameColumnWidth + 4;
            int valueWidth = Width - valueX - 8;
            bool dropdownOpened = row.HandleClick(args.X, args.Y, valueX, valueWidth);

            if (dropdownOpened)
            {
                OpenEnumDropdown(row, valueX, rowY);
            }
            else if (row.IsEditing)
            {
                _editingRow = row;
                var form = FindForm();
                if (form != null)
                {
                    form.ActiveControl = this;
                }
                Invalidate();
            }
        }

        base.OnMouseDown(e);
    }

    private int HitTestRow(int y)
    {
        int drawY = HeaderHeight + 4;

        string? currentCat = null;
        for (int i = 0; i < _rows.Count; i++)
        {
            if (currentCat != _rows[i].Category)
            {
                currentCat = _rows[i].Category;
                drawY += 20; // category header
            }

            if (y >= drawY && y < drawY + RowHeight)
                return i;

            drawY += RowHeight;
        }

        return -1;
    }

    private int GetRowY(int rowIndex)
    {
        int drawY = HeaderHeight + 4;

        string? currentCat = null;
        for (int i = 0; i < _rows.Count; i++)
        {
            if (currentCat != _rows[i].Category)
            {
                currentCat = _rows[i].Category;
                drawY += 20; // category header
            }

            if (i == rowIndex)
                return drawY;

            drawY += RowHeight;
        }

        return 0;
    }

    private void OpenEnumDropdown(PropertyGridRow row, int valueX, int rowY)
    {
        CloseEnumDropdown();

        _dropdownOpen = true;
        _dropdownRow = row;
        _dropdownSelectedIndex = row.EnumSelectedIndex;
        _dropdownValueX = valueX;
        _dropdownRowY = rowY;
        _dropdownItemCount = row.EnumValueCount;

        CapturingMouse = true;
        Invalidate();
    }

    private void CloseEnumDropdown()
    {
        if (!_dropdownOpen) return;

        _dropdownOpen = false;
        _dropdownRow = null;
        _dropdownSelectedIndex = -1;
        _dropdownHoveredIndex = -1;
        _dropdownItemCount = 0;

        CapturingMouse = false;
        Invalidate();
    }

    protected override void OnTextInput(string text)
    {
        if (_editingRow != null && text.Length > 0)
        {
            if (_editingRow.HandleKey(text[0]))
            {
                Invalidate();
                return;
            }
        }
        base.OnTextInput(text);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (_dropdownOpen && _dropdownRow != null)
        {
            switch (e.KeyCode)
            {
                case Keys.Up:
                    _dropdownSelectedIndex--;
                    if (_dropdownSelectedIndex < 0)
                        _dropdownSelectedIndex = _dropdownItemCount - 1;
                    _dropdownRow.SelectEnumItem(_dropdownSelectedIndex);
                    _dropdownHoveredIndex = _dropdownSelectedIndex;
                    Invalidate();
                    e.Handled = true;
                    return;

                case Keys.Down:
                    _dropdownSelectedIndex++;
                    if (_dropdownSelectedIndex >= _dropdownItemCount)
                        _dropdownSelectedIndex = 0;
                    _dropdownRow.SelectEnumItem(_dropdownSelectedIndex);
                    _dropdownHoveredIndex = _dropdownSelectedIndex;
                    Invalidate();
                    e.Handled = true;
                    return;

                case Keys.Enter:
                    _dropdownRow.SelectEnumItem(_dropdownSelectedIndex);
                    _dropdownRow.CommitEdit();
                    CloseEnumDropdown();
                    Invalidate();
                    e.Handled = true;
                    return;

                case Keys.Escape:
                    _dropdownRow.CancelEdit();
                    CloseEnumDropdown();
                    Invalidate();
                    e.Handled = true;
                    return;
            }
        }
        else if (_editingRow != null)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    _editingRow.CommitEdit();
                    _editingRow = null;
                    Invalidate();
                    e.Handled = true;
                    return;

                case Keys.Escape:
                    _editingRow.CancelEdit();
                    _editingRow = null;
                    Invalidate();
                    e.Handled = true;
                    return;

                case Keys.Back:
                    if (_editingRow.HandleKey('\b'))
                    {
                        Invalidate();
                        e.Handled = true;
                    }
                    return;

                case Keys.Delete:
                    if (_editingRow.HandleKey('\x03'))
                    {
                        Invalidate();
                        e.Handled = true;
                    }
                    return;

                case Keys.Tab:
                    _editingRow.CommitEdit();
                    _editingRow = null;
                    Invalidate();
                    break;
            }
        }
        base.OnKeyDown(e);
    }

    protected override void OnMouseMove(EventArgs e)
    {
        if (!_dropdownOpen || _dropdownRow == null)
        {
            base.OnMouseMove(e);
            return;
        }

        if (e is MouseEventArgs mouseArgs)
        {
            if (mouseArgs.X >= _dropdownValueX && mouseArgs.X < _dropdownValueX + ValueColumnWidth &&
                mouseArgs.Y >= _dropdownRowY + RowHeight && mouseArgs.Y < _dropdownRowY + RowHeight + _dropdownItemCount * DropdownItemHeight)
            {
                int hoveredIndex = (mouseArgs.Y - _dropdownRowY - RowHeight) / DropdownItemHeight;
                if (hoveredIndex >= 0 && hoveredIndex < _dropdownItemCount)
                {
                    if (_dropdownHoveredIndex != hoveredIndex)
                    {
                        _dropdownHoveredIndex = hoveredIndex;
                        Invalidate();
                    }
                }
            }
            else
            {
                if (_dropdownHoveredIndex != -1)
                {
                    _dropdownHoveredIndex = -1;
                    Invalidate();
                }
            }
        }

        base.OnMouseMove(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
    }

    protected override void OnLostFocus(EventArgs e)
    {
        if (_editingRow != null)
        {
            _editingRow.CommitEdit();
            _editingRow = null;
        }

        if (_dropdownOpen)
        {
            _dropdownRow?.CancelEdit();
            CloseEnumDropdown();
        }

        base.OnLostFocus(e);
    }

    public override void Render(Graphics g)
    {
        var theme = ThemeManager.CurrentTheme;

        // Background
        g.FillRectangle(theme.ControlLight, 0, 0, Width, Height);

        // Header
        g.FillRectangle(theme.ActiveCaption, 0, 0, Width, HeaderHeight);
        g.DrawString("Eigenschaften", theme.DefaultFont, theme.ActiveCaptionText, 6, 4);

        if (TargetControl == null)
        {
            g.DrawString("Keine Auswahl", theme.DefaultFont, theme.GrayText, 10, HeaderHeight + 10);
            return;
        }

        // Draw rows
        int drawY = HeaderHeight + 4;
        string? currentCat = null;

        foreach (var row in _rows)
        {
            // Category header
            if (currentCat != row.Category)
            {
                currentCat = row.Category;
                g.FillRectangle(theme.ControlDark, 0, drawY, Width, 20);
                g.DrawString(currentCat, theme.SmallFont, theme.ControlText, 4, drawY + 2);
                drawY += 20;
            }

            // Row background (alternating)
            var rowBack = _rows.IndexOf(row) % 2 == 0
                ? theme.ControlLight
                : theme.AlternateRow;
            g.FillRectangle(rowBack, 0, drawY, Width, RowHeight);

            // Row content
            row.Render(g, drawY, Width, RowHeight, theme);

            drawY += RowHeight;
        }
    }

    public override void RenderOverlay(Graphics g)
    {
        if (!_dropdownOpen || _dropdownRow == null) return;

        var theme = ThemeManager.CurrentTheme;
        int dropdownHeight = Math.Min(_dropdownItemCount * DropdownItemHeight, MaxDropdownHeight);

        // Background overlay
        g.FillRectangle(theme.ControlLight, _dropdownValueX, _dropdownRowY + RowHeight,
            ValueColumnWidth, dropdownHeight);

        // Border
        g.DrawRectangle(theme.ComboBoxDropdownArrow, _dropdownValueX, _dropdownRowY + RowHeight,
            ValueColumnWidth, dropdownHeight);

        // Draw enum values
        for (int i = 0; i < _dropdownItemCount; i++)
        {
            int itemY = _dropdownRowY + RowHeight + i * DropdownItemHeight;
            int itemH = i == _dropdownItemCount - 1 && dropdownHeight < _dropdownItemCount * DropdownItemHeight
                ? dropdownHeight - (i * DropdownItemHeight)
                : DropdownItemHeight;

            bool isSelected = _dropdownRow.IsFlagSelected(i);
            bool isHovered = i == _dropdownHoveredIndex;
            bool isSingleSelected = !_dropdownRow.IsFlagsEnum && i == _dropdownSelectedIndex;

            Color itemText;
            Color itemBg;

            if (isSingleSelected || isHovered)
            {
                itemBg = theme.Highlight;
                itemText = theme.HighlightText;
            }
            else if (isSelected)
            {
                itemBg = theme.HoverHighlight;
                itemText = theme.ControlText;
            }
            else
            {
                itemBg = theme.ControlLight;
                itemText = theme.ControlText;
            }

            g.FillRectangle(itemBg, _dropdownValueX, itemY, ValueColumnWidth, itemH);

            if (isSelected)
            {
                g.DrawString("\u2713", theme.DefaultFont, itemText, _dropdownValueX + 4, itemY + 4);
                g.DrawString(_dropdownRow.GetEnumDisplayText(i), theme.DefaultFont, itemText,
                    _dropdownValueX + 18, itemY + 2);
            }
            else
            {
                g.DrawString(_dropdownRow.GetEnumDisplayText(i), theme.DefaultFont, itemText,
                    _dropdownValueX + 4, itemY + 2);
            }
        }
    }
}
