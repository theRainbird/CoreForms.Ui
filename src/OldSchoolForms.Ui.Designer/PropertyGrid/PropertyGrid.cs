using System;
using System.Collections.Generic;
using System.Linq;
using OldSchoolForms.Ui.Core;
using OldSchoolForms.Ui.Designer.Services;
using OldSchoolForms.Ui.Theming;
using Graphics = OldSchoolForms.Ui.Rendering.Graphics;

namespace OldSchoolForms.Ui.Designer.PropertyGrid;

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
    private const int ColorSwatchSize = 18;
    private const int ColorPaletteCols = 17;
    private const int ColorPaletteMaxVisibleRows = 10;

    private string? _activeCategory;
    private PropertyGridRow? _editingRow;
    private PropertyGridRow? _dropdownRow;
    private int _dropdownSelectedIndex = -1;
    private int _dropdownHoveredIndex = -1;
    private bool _dropdownOpen;
    private int _dropdownValueX;
    private int _dropdownRowY;
    private int _dropdownItemCount;

   private PropertyGridRow? _colorPickerRow;
    private int _colorPickerX;
    private int _colorPickerY;
    private int _colorPickerWidth;
    private int _rgbEditingChannel = -1;
    private PropertyGridRow? _rgbPickerRow;
    private int _systemColorHoveredIndex = -1;

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

        // Handle color picker open
        if (_colorPickerRow != null && _colorPickerRow.IsColorPickerOpen)
        {
            int gridStartY = PropertyGridRow.SystemColorComboBoxHeight;
            int gridBottom = gridStartY + (PropertyGridRow.ColorPaletteMaxVisibleRows - 1) * 19;
            int dropdownHeight = PropertyGridRow.SystemColorCount * PropertyGridRow.SystemColorDropdownItemHeight;
            int totalPickerHeight = PropertyGridRow.SystemColorComboBoxHeight +
                Math.Max(dropdownHeight, gridBottom - gridStartY) +
                PropertyGridRow.RgbSectionHeight;

            if (args.X >= _colorPickerX && args.X < _colorPickerX + _colorPickerWidth &&
                args.Y >= _colorPickerY && args.Y < _colorPickerY + totalPickerHeight)
            {
                int rgbY = _colorPickerY + gridBottom;

                   if (args.Y >= rgbY && args.Y < rgbY + PropertyGridRow.RgbSectionHeight)
                    {
                        int channelIndex = _colorPickerRow.HandleRgbClick(
                            _colorPickerX, _colorPickerY, args.X, args.Y, _colorPickerWidth);
                        if (channelIndex >= 0)
                        {
                            _rgbEditingChannel = channelIndex;
                            _rgbPickerRow = _colorPickerRow;
                            Invalidate();
                            return;
                        }

                        int rgbTop = 6;
                        int rgbLeft = _colorPickerX + 8;
                        int labelWidth = 20;
                        int inputWidth = PropertyGridRow.RgbInputWidth;
                        int spacing = 6;
                        int channelTotalWidth = labelWidth + inputWidth;
                        int rgbAreaWidth = 3 * (channelTotalWidth + spacing) - spacing;
                        int okX = rgbLeft + rgbAreaWidth + 16;
                        int okY = rgbY + rgbTop;
                        int okWidth = 60;
                        int okHeight = 24;
                        if (args.X >= okX && args.X < okX + okWidth && args.Y >= okY && args.Y < okY + okHeight)
                        {
                            _colorPickerRow.CommitRgbFromBuffers();
                            _colorPickerRow = null;
                            _rgbEditingChannel = -1;
                            _rgbPickerRow = null;
                            CapturingMouse = false;
                            Invalidate();
                            return;
                        }
                    }

                  int colorIndex = _colorPickerRow.HandleColorPickerClick(
                        _colorPickerX, _colorPickerY, args.X, args.Y, _colorPickerWidth);
                    if (colorIndex >= 0)
                    {
                        _colorPickerRow.CommitColorSelection(colorIndex);
                        _colorPickerRow = null;
                        _rgbEditingChannel = -1;
                        _rgbPickerRow = null;
                        CapturingMouse = false;
                    }
                    else if (colorIndex == -2)
                    {
                        _colorPickerRow.CancelColorPicker();
                        _colorPickerRow = null;
                        _rgbEditingChannel = -1;
                        _rgbPickerRow = null;
                        CapturingMouse = false;
                    }
                    Invalidate();
                    return;
            }
            else
            {
                _colorPickerRow.CancelColorPicker();
                _colorPickerRow = null;
                _rgbEditingChannel = -1;
                _rgbPickerRow = null;
                CapturingMouse = false;
                Invalidate();
                return;
            }
        }

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
            int valueX = NameColumnWidth;
            int valueWidth = Width - valueX;
            bool dropdownOpened = row.HandleClick(args.X, args.Y, valueX, valueWidth);

            if (dropdownOpened)
            {
                if (row.IsColorProperty)
                {
                    row.StartColorPicker();
                    _dropdownValueX = valueX;
                    _colorPickerRow = row;
                    _colorPickerX = valueX;
                    _colorPickerY = rowY;
                    _colorPickerWidth = valueWidth;
                    CapturingMouse = true;
                    Invalidate();
                }
                else
                {
                    OpenEnumDropdown(row, valueX, rowY);
                }
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

        if (_colorPickerRow != null)
        {
            _colorPickerRow.CancelColorPicker();
            _colorPickerRow = null;
            _rgbEditingChannel = -1;
            _rgbPickerRow = null;
        }

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
        if (_colorPickerRow != null)
        {
            _colorPickerRow.CancelColorPicker();
            _colorPickerRow = null;
            _rgbEditingChannel = -1;
            _rgbPickerRow = null;
            CapturingMouse = false;
        }

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
        // RGB channel editing
        if (_rgbEditingChannel >= 0 && _rgbPickerRow != null)
        {
            if (e.KeyCode == Keys.Escape)
            {
                _rgbPickerRow._rgbEditingChannel = -1;
                _rgbPickerRow._rgbRBuffer = string.Empty;
                _rgbPickerRow._rgbGBuffer = string.Empty;
                _rgbPickerRow._rgbBBuffer = string.Empty;
                _rgbEditingChannel = -1;
                _rgbPickerRow = null;
                Invalidate();
                e.Handled = true;
                return;
            }

            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Tab)
            {
                _rgbPickerRow.CommitRgbFromBuffers();
                _colorPickerRow = null;
                _rgbEditingChannel = -1;
                _rgbPickerRow = null;
                CapturingMouse = false;
                Invalidate();
                e.Handled = true;
                return;
            }

            if (e.KeyCode == Keys.Escape)
            {
                _rgbPickerRow._rgbEditingChannel = -1;
                _rgbEditingChannel = -1;
                _rgbPickerRow = null;
                Invalidate();
                e.Handled = true;
                return;
            }

            if (e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9)
            {
                char key = (char)('0' + (e.KeyCode - Keys.D0));
                _rgbPickerRow.HandleRgbInput(key);
                Invalidate();
                e.Handled = true;
                return;
            }

           // NumPad keys handled via TextInput

            if (e.KeyCode == Keys.Back || e.KeyCode == Keys.Delete)
            {
                _rgbPickerRow.HandleRgbInput('\b');
                Invalidate();
                e.Handled = true;
                return;
            }
        }

        // Color picker keyboard handling
        if (_colorPickerRow != null && _colorPickerRow.IsColorPickerOpen)
        {
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    _colorPickerRow.CancelColorPicker();
                    _colorPickerRow = null;
                    Invalidate();
                    e.Handled = true;
                    return;

                case Keys.Enter:
                    if (_colorPickerRow.ColorPickerHoveredIndex >= 0)
                    {
                        _colorPickerRow.CommitColorSelection(_colorPickerRow.ColorPickerHoveredIndex);
                    }
                    _colorPickerRow = null;
                    Invalidate();
                    e.Handled = true;
                    return;

                case Keys.Up:
                    {
                        int idx = _colorPickerRow.ColorPickerHoveredIndex;
                        if (idx < 0) idx = 0;
                        else idx -= ColorPaletteCols;
                        if (idx < 0) idx = 0;
                        _colorPickerRow.SetColorPickerHovered(Math.Min(idx, PropertyGridRow.ColorPalette.Length - 1));
                        Invalidate();
                        e.Handled = true;
                        return;
                    }
                case Keys.Down:
                    {
                        int idx = _colorPickerRow.ColorPickerHoveredIndex;
                        if (idx < 0) idx = 0;
                        else idx += ColorPaletteCols;
                        _colorPickerRow.SetColorPickerHovered(Math.Min(idx, PropertyGridRow.ColorPalette.Length - 1));
                        Invalidate();
                        e.Handled = true;
                        return;
                    }
                case Keys.Left:
                    {
                        int idx = _colorPickerRow.ColorPickerHoveredIndex;
                        if (idx < 0) idx = 1;
                        else if (idx % ColorPaletteCols > 0) idx--;
                        _colorPickerRow.SetColorPickerHovered(idx);
                        Invalidate();
                        e.Handled = true;
                        return;
                    }
                case Keys.Right:
                    {
                        int idx = _colorPickerRow.ColorPickerHoveredIndex;
                        if (idx < 0) idx = 1;
                        else if ((idx + 1) % ColorPaletteCols != 0) idx++;
                        _colorPickerRow.SetColorPickerHovered(Math.Min(idx, PropertyGridRow.ColorPalette.Length - 1));
                        Invalidate();
                        e.Handled = true;
                        return;
                    }
            }
            return;
        }

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
        // Handle color picker hover
        if (_colorPickerRow != null && _colorPickerRow.IsColorPickerOpen && e is MouseEventArgs mouseArgs)
        {
            int localX = mouseArgs.X - _colorPickerX;
            int localY = mouseArgs.Y - _colorPickerY;

           int totalPickerHeight = PropertyGridRow.SystemColorComboBoxHeight +
                Math.Max(PropertyGridRow.SystemColorCount * PropertyGridRow.SystemColorDropdownItemHeight,
                    (PropertyGridRow.ColorPaletteMaxVisibleRows - 1) * 19) +
                PropertyGridRow.RgbSectionHeight;

            if (localX >= 0 && localY >= 0 && localX < _colorPickerWidth && localY < totalPickerHeight)
            {
                int systemColorAreaHeight = PropertyGridRow.SystemColorComboBoxHeight +
                    PropertyGridRow.SystemColorCount * PropertyGridRow.SystemColorDropdownItemHeight;
                int gridStartY = PropertyGridRow.SystemColorComboBoxHeight;
                int gridBottom = gridStartY + (PropertyGridRow.ColorPaletteMaxVisibleRows - 1) * 19;

                // ComboBox area hover
                if (localY < gridStartY)
                {
                    _systemColorHoveredIndex = -1;
                    _colorPickerRow.SetSystemColorHovered(-1);
                }
                // System colors dropdown hover (only when open)
                else if (_colorPickerRow._systemColorDropdownOpen && localY >= gridStartY && localY < systemColorAreaHeight)
                {
                    int dropIndex = (localY - gridStartY) / PropertyGridRow.SystemColorDropdownItemHeight;
                    if (dropIndex >= 0 && dropIndex < PropertyGridRow.SystemColorCount)
                    {
                        if (_systemColorHoveredIndex != dropIndex)
                        {
                            _systemColorHoveredIndex = dropIndex;
                            _colorPickerRow.SetSystemColorHovered(dropIndex);
                            Invalidate();
                        }
                        _colorPickerRow.SetColorPickerHovered(-999);
                    }
                    else
                    {
                        _systemColorHoveredIndex = -1;
                        _colorPickerRow.SetSystemColorHovered(-1);
                    }
                }
                // Color grid hover
                else if (localY >= gridStartY && localY < gridBottom)
                {
                    int gridY = localY - gridStartY;
                    int row = gridY / 19;
                    int col = localX / 19;

                    _systemColorHoveredIndex = -1;
                    _colorPickerRow.SetSystemColorHovered(-1);

                    if (col >= 0 && col < ColorPaletteCols && row >= 0 && row < ColorPaletteMaxVisibleRows - 1)
                    {
                        int index = PropertyGridRow.SystemColorCount + row * ColorPaletteCols + col;
                        if (_colorPickerRow.ColorPickerHoveredIndex != index)
                        {
                            _colorPickerRow.SetColorPickerHovered(index);
                            Invalidate();
                        }
                    }
                    else if (_colorPickerRow.ColorPickerHoveredIndex != -1 &&
                             _colorPickerRow.ColorPickerHoveredIndex < PropertyGridRow.SystemColorCount)
                    {
                        _colorPickerRow.SetColorPickerHovered(-1);
                        Invalidate();
                    }
                }
            }
            else if (_colorPickerRow.ColorPickerHoveredIndex != -1)
            {
                _colorPickerRow.SetColorPickerHovered(-1);
                Invalidate();
            }
            return;
        }

        if (!_dropdownOpen || _dropdownRow == null)
        {
            base.OnMouseMove(e);
            return;
        }

        if (e is MouseEventArgs)
        {
            MouseEventArgs me = (MouseEventArgs)e;
            if (me.X >= _dropdownValueX && me.X < _dropdownValueX + ValueColumnWidth &&
                me.Y >= _dropdownRowY + RowHeight && me.Y < _dropdownRowY + RowHeight + _dropdownItemCount * DropdownItemHeight)
            {
                int hoveredIndex = (me.Y - _dropdownRowY - RowHeight) / DropdownItemHeight;
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

        if (_colorPickerRow != null && _colorPickerRow.IsColorPickerOpen)
        {
            _colorPickerRow.CancelColorPicker();
            _colorPickerRow = null;
            CapturingMouse = false;
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
        var theme = ThemeManager.CurrentTheme;

        // Color picker overlay
        if (_colorPickerRow != null && _colorPickerRow.IsColorPickerOpen)
        {
            int paletteWidth = Math.Max(340, Width - NameColumnWidth);
            int totalPickerHeight = PropertyGridRow.SystemColorComboBoxHeight +
                Math.Max(PropertyGridRow.SystemColorCount * PropertyGridRow.SystemColorDropdownItemHeight,
                    (PropertyGridRow.ColorPaletteMaxVisibleRows - 1) * 19) +
                PropertyGridRow.RgbSectionHeight;

            Form? topForm = null;
            Control? current = this;
            while (current != null)
            {
                if (current is Form form)
                {
                    topForm = form;
                    break;
                }
                current = current.Parent;
            }

            // Convert PropertyGrid position to form coordinates
            int gridFormX = 0, gridFormY = 0;
            if (topForm != null)
            {
                Control? c = this;
                while (c != null && c != topForm)
                {
                    gridFormX += c.X;
                    gridFormY += c.Y;
                    c = c.Parent;
                }
            }

            int formLeft = topForm?.ClientRectangle.Left ?? 0;
            int formRight = topForm?.ClientRectangle.Right ?? ClientRectangle.Right;
            int formTop = topForm?.ClientRectangle.Top ?? 0;
            int formBottom = topForm?.ClientRectangle.Bottom ?? ClientRectangle.Bottom;

            // Palette edges in form coordinates
            int paletteFormX = gridFormX + _dropdownValueX;
            int paletteFormY = gridFormY + _dropdownRowY + RowHeight;

            // Available space in each direction
            int spaceRight = formRight - (paletteFormX + paletteWidth);
            int spaceLeft = paletteFormX - formLeft;
            int spaceBelow = formBottom - (paletteFormY + totalPickerHeight);
            int spaceAbove = paletteFormY - formTop;

            // Horizontal: prefer side with more space
            int paletteX;
            if (spaceLeft > spaceRight && spaceLeft >= paletteWidth)
                paletteX = _dropdownValueX + ValueColumnWidth - paletteWidth;
            else
                paletteX = _dropdownValueX;

            // Clamp horizontal to form bounds
            int paletteFormX2 = gridFormX + paletteX;
            if (paletteFormX2 < formLeft)
                paletteX = formLeft - gridFormX;
            if (paletteFormX2 + paletteWidth > formRight)
                paletteWidth = Math.Max(200, formRight - paletteFormX2);

            // Vertical: prefer side with more space
            int paletteY;
            if (spaceAbove > spaceBelow && spaceAbove >= totalPickerHeight)
                paletteY = _dropdownRowY - totalPickerHeight;
            else
                paletteY = _dropdownRowY + RowHeight;

            // Clamp vertical to form bounds
            int paletteFormY2 = gridFormY + paletteY;
            if (paletteFormY2 < formTop)
                paletteY = formTop - gridFormY;
            if (paletteFormY2 + totalPickerHeight > formBottom)
                paletteY = formBottom - totalPickerHeight - gridFormY;

            PropertyGridRow.DrawColorPalette(g, paletteX, paletteY, paletteWidth,
                _colorPickerRow.ColorPickerHoveredIndex,
                _colorPickerRow.ColorPickerSelectedIndex,
                _colorPickerRow._rgbRBuffer,
                _colorPickerRow._rgbGBuffer,
                _colorPickerRow._rgbBBuffer,
                _colorPickerRow._rgbEditingChannel,
                theme,
                _colorPickerRow._systemColorDropdownOpen,
                _systemColorHoveredIndex);

            CapturingMouse = true;
            return;
        }

        if (!_dropdownOpen || _dropdownRow == null) return;

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
