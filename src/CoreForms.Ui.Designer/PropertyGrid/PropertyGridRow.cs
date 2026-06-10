using System;
using System.Reflection;
using CoreForms.Ui.Core;
using CoreForms.Ui.Theming;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Designer.PropertyGrid;

/// <summary>
/// Renders a single property row with name label and value editor.
/// Supports inline editing for common types: string, bool, enum, numeric, Color, Point, Size, Font, Anchor, Dock.
/// </summary>
public class PropertyGridRow
{
    public static readonly Color[] ColorPalette = CreateColorPalette();

    private static Color[] CreateColorPalette()
    {
        var colors = new List<Color>();
        colors.Add(SystemColors.Control);
        colors.Add(SystemColors.ControlText);
        colors.Add(SystemColors.Window);
        colors.Add(SystemColors.WindowText);
        colors.Add(SystemColors.Highlight);
        colors.Add(SystemColors.HighlightText);
        colors.Add(SystemColors.ActiveCaption);
        colors.Add(SystemColors.ActiveCaptionText);
        colors.Add(SystemColors.InactiveCaption);
        colors.Add(SystemColors.ControlLight);
        colors.Add(SystemColors.ControlDark);
        colors.Add(SystemColors.GrayText);
        colors.Add(Color.Black);
        colors.Add(Color.White);
        for (int i = 1; i <= 14; i++)
        {
            int gray = i * 16;
            colors.Add(Color.FromArgb(gray, gray, gray));
        }
        var levels = new[] { 0x00, 0x33, 0x66, 0x99, 0xCC, 0xFF };
        for (int b = 0; b < 6; b++)
        {
            for (int r = 0; r < 6; r++)
            {
                for (int g = 0; g < 6; g++)
                {
                    colors.Add(Color.FromArgb(levels[r], levels[g], levels[b]));
                }
            }
        }
        return colors.ToArray();
    }

    private readonly PropertyDescriptor _descriptor;
    private readonly int _nameWidth;
    private bool _isEditing;
    private bool _isEnumDropdownOpen;
    private int _enumSelectedIndex = -1;
    private readonly List<int> _selectedFlagIndices = new();
    private string _editBuffer = string.Empty;
    private int _caretPos;

    private bool _isColorPickerOpen;
    private int _colorPickerHoveredIndex = -1;
    private int _colorPickerSelectedIndex = -1;

    internal bool _systemColorDropdownOpen;
    private int _systemColorHoveredIndex = -1;

    /// <summary>
    /// Gets the index of the currently hovered system color item in the dropdown, or -1.
    /// </summary>
    public int SystemColorHoveredIndex => _systemColorHoveredIndex;

    public const int DropdownArrowWidth = 20;
    public const int ColorSwatchSize = 18;
    public const int ColorPaletteCols = 17;
    public const int ColorPaletteMaxVisibleRows = 10;
    public const int SystemColorCount = 12;
    public const int SystemColorComboBoxHeight = 24;
    public const int SystemColorDropdownItemHeight = 22;
    public const int RgbSectionHeight = 48;
    public const int RgbInputWidth = 42;

 

    /// <summary>
    /// Gets the property category.
    /// </summary>
    public string Category => _descriptor.Category;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="descriptor">The property descriptor.</param>
    /// <param name="nameWidth">Width of the name column in pixels.</param>
    public PropertyGridRow(PropertyDescriptor descriptor, int nameWidth)
    {
        _descriptor = descriptor;
        _nameWidth = nameWidth;
    }

    /// <summary>
    /// Handles a mouse click on this row.
    /// </summary>
    /// <param name="mouseX">The X coordinate of the click.</param>
    /// <param name="mouseY">The Y coordinate of the click.</param>
    /// <param name="valueColumnX">The X position of the value column.</param>
    /// <param name="valueColumnWidth">The width of the value column.</param>
    /// <returns>True if an enum dropdown was opened.</returns>
    public bool HandleClick(int mouseX, int mouseY, int valueColumnX, int valueColumnWidth)
    {
        if (mouseX < valueColumnX || _descriptor.IsReadOnly)
            return false;

        int localX = mouseX - valueColumnX;

        if (_descriptor.PropertyType == typeof(Color))
        {
            StartColorPicker();
            return true;
        }

        if (_descriptor.PropertyType.IsEnum)
        {
            if (localX >= valueColumnWidth - DropdownArrowWidth)
            {
                StartEnumDropdown();
                return true;
            }
        }

        StartEdit();
        return false;
    }

    /// <summary>
    /// Renders the row at the given position.
    /// </summary>
    public void Render(Graphics g, int y, int width, int height, Theme theme)
    {
        int valueX = _nameWidth;
        int valueWidth = width - _nameWidth;

        // Name label
        g.DrawString(_descriptor.Name, theme.DefaultFont, theme.ControlText, 6, y + 5);

        // Value
        var value = _descriptor.GetValue();
        DrawValue(g, value, valueX, y, valueWidth, height, theme);
    }

    private void DrawValue(Graphics g, object? value, int x, int y, int width, int height, Theme theme)
    {
        if (_isEditing)
        {
            // Draw editing field
            g.FillRectangle(Color.White, x, y, width, height);
            g.DrawString(_editBuffer, theme.DefaultFont, theme.ControlText, x + 4, y + 5);

            // Draw caret using actual measured width
            int caretWidth = g.MeasureString(_editBuffer[..Math.Min(_caretPos, _editBuffer.Length)], theme.DefaultFont).width;
            g.DrawLine(theme.ControlText, x + 4 + caretWidth, y + 4, x + 4 + caretWidth, y + height - 4);
            return;
        }

        var type = _descriptor.PropertyType;

        if (value == null)
        {
            g.DrawString("(null)", theme.DefaultFont, theme.GrayText, x + 4, y + 5);
            return;
        }

        if (type == typeof(string))
        {
            var text = value.ToString() ?? "";
            g.DrawString(text, theme.DefaultFont, theme.ControlText, x + 4, y + 5);
        }
        else if (type == typeof(bool))
        {
            bool b = (bool)value;
            DrawCheckBox(g, x + 4, y + 4, b, theme);
            g.DrawString(b ? "True" : "False", theme.DefaultFont, theme.ControlText, x + 24, y + 5);
        }
        else if (type.IsEnum)
        {
            g.DrawString(value.ToString() ?? "", theme.DefaultFont, theme.ControlText, x + 4, y + 5);
            // Dropdown arrow
            g.DrawLine(theme.ControlDark, x + width - 10, y + height / 2 - 2, x + width - 6, y + height / 2 + 2);
            g.DrawLine(theme.ControlDark, x + width - 6, y + height / 2 + 2, x + width - 2, y + height / 2 - 2);
        }
        else if (type == typeof(int) || type == typeof(float))
        {
            g.DrawString(value.ToString() ?? "0", theme.DefaultFont, theme.ControlText, x + 4, y + 5);
        }
        else if (type == typeof(Color))
        {
            var c = (Color)value;
            // Color swatch
            g.FillRectangle(c, x + 4, y + 4, 18, height - 8);
            g.DrawRectangle(theme.ControlDark, x + 4, y + 4, 18, height - 8, 1);
            g.DrawString($"RGB({c.R},{c.G},{c.B})", theme.DefaultFont, theme.ControlText, x + 26, y + 5);
        }
        else if (type == typeof(Point))
        {
            var p = (Point)value;
            g.DrawString($"X={p.X}, Y={p.Y}", theme.DefaultFont, theme.ControlText, x + 4, y + 5);
        }
        else if (type == typeof(Size))
        {
            var s = (Size)value;
            g.DrawString($"W={s.Width}, H={s.Height}", theme.DefaultFont, theme.ControlText, x + 4, y + 5);
        }
        else if (type == typeof(Font))
        {
            var f = (Font)value;
            g.DrawString($"{f.Name}, {f.Size}pt", theme.DefaultFont, theme.ControlText, x + 4, y + 5);
        }
        else if (type == typeof(AnchorStyles))
        {
            var a = (AnchorStyles)value;
            g.DrawString(FormatAnchor(a), theme.DefaultFont, theme.ControlText, x + 4, y + 5);
        }
        else if (type == typeof(DockStyle))
        {
            var d = (DockStyle)value;
            g.DrawString(d.ToString(), theme.DefaultFont, theme.ControlText, x + 4, y + 5);
        }
        else
        {
            g.DrawString(value.ToString() ?? "", theme.DefaultFont, theme.ControlText, x + 4, y + 5);
        }
    }

    private static void DrawCheckBox(Graphics g, int x, int y, bool checked_, Theme theme)
    {
        g.DrawRectangle(theme.CheckboxBorder, x, y, 16, 16, 1);
        if (checked_)
        {
            g.DrawLine(theme.CheckboxCheck, x + 3, y + 8, x + 7, y + 12);
            g.DrawLine(theme.CheckboxCheck, x + 7, y + 12, x + 13, y + 4);
        }
    }

   private static string FormatAnchor(AnchorStyles anchor)
    {
        var parts = new System.Collections.Generic.List<string>();
        if (anchor.HasFlag(AnchorStyles.Top)) parts.Add("Top");
        if (anchor.HasFlag(AnchorStyles.Bottom)) parts.Add("Bottom");
        if (anchor.HasFlag(AnchorStyles.Left)) parts.Add("Left");
        if (anchor.HasFlag(AnchorStyles.Right)) parts.Add("Right");
        return parts.Count > 0 ? string.Join(", ", parts) : "None";
    }

   /// <summary>
    /// Draws the color palette overlay.
    /// </summary>
    /// <param name="g">The graphics object.</param>
    /// <param name="paletteX">X position of the palette.</param>
    /// <param name="paletteY">Y position of the palette.</param>
    /// <param name="paletteWidth">Width of the palette.</param>
    /// <param name="hoveredIndex">Index of the hovered color, or -1.</param>
    /// <param name="selectedIndex">Index of the selected color, or -1.</param>
    /// <param name="rBuffer">Current R input buffer.</param>
    /// <param name="gBuffer">Current G input buffer.</param>
    /// <param name="bBuffer">Current B input buffer.</param>
    /// <param name="editingChannel">Currently editing RGB channel (0-2), or -1.</param>
    /// <param name="theme">The current theme.</param>
    /// <param name="systemColorDropdownOpen">Whether the system colors dropdown is currently open.</param>
    /// <param name="systemColorHoveredIndex">Index of the hovered system color dropdown item, or -1.</param>
    public static void DrawColorPalette(Graphics g, int paletteX, int paletteY, int paletteWidth,
        int hoveredIndex, int selectedIndex, string rBuffer, string gBuffer, string bBuffer,
        int editingChannel, Theme theme, bool systemColorDropdownOpen, int systemColorHoveredIndex)
    {
        int swatchSize = ColorSwatchSize;
        int gap = 1;
        int cellSize = swatchSize + gap;

        int systemComboBoxY = paletteY;
        int systemDropdownY = systemComboBoxY + SystemColorComboBoxHeight;
        int systemDropdownHeight = SystemColorCount * SystemColorDropdownItemHeight;
        int gridStartY = systemDropdownY;
        int gridRows = ColorPaletteMaxVisibleRows - 1;
        int gridBottom = gridStartY + gridRows * cellSize;
        int rgbY = gridBottom;
        int totalHeight = rgbY + RgbSectionHeight;

        g.FillRectangle(Color.FromArgb(245, 245, 245), paletteX, paletteY, paletteWidth, totalHeight);
        g.DrawRectangle(Color.FromArgb(198, 198, 198), paletteX, paletteY, paletteWidth, totalHeight, 1);

        // System colors ComboBox
        int comboBoxX = paletteX + 4;
        int comboBoxY = systemComboBoxY + 2;
        int comboBoxW = paletteWidth - 8;
        int comboBoxH = SystemColorComboBoxHeight - 4;
        int swatchDisplaySize = 18;

        // ComboBox background
        g.FillRectangle(Color.White, comboBoxX, comboBoxY, comboBoxW, comboBoxH);
        g.DrawRectangle(Color.FromArgb(180, 180, 180), comboBoxX, comboBoxY, comboBoxW, comboBoxH, 1);

        // ComboBox swatch
        int selectedSystemIndex = selectedIndex >= 0 && selectedIndex < SystemColorCount ? selectedIndex : -1;
        Color comboBoxColor = selectedSystemIndex >= 0 ? ColorPalette[selectedSystemIndex] : Color.Black;
        g.FillRectangle(comboBoxColor, comboBoxX + 2, comboBoxY + 3, swatchDisplaySize, comboBoxH - 6);
        g.DrawRectangle(Color.FromArgb(150, 150, 150), comboBoxX + 2, comboBoxY + 3, swatchDisplaySize, comboBoxH - 6, 1);

        // ComboBox text (system color name)
        string comboBoxText = selectedSystemIndex >= 0 ? GetSystemColorName(selectedSystemIndex) : "Systemfarben";
        var textMeasure = g.MeasureString(comboBoxText, theme.DefaultFont);
        float textX = comboBoxX + swatchDisplaySize + 6;
        float textY = comboBoxY + (comboBoxH - textMeasure.height) / 2;
        g.DrawString(comboBoxText, theme.DefaultFont, theme.ControlText, textX, textY);

        // ComboBox arrow
        int arrowX = comboBoxX + comboBoxW - 16;
        int arrowY = comboBoxY + comboBoxH / 2;
        g.DrawLine(theme.ControlDark, arrowX, arrowY - 4, arrowX + 6, arrowY);
        g.DrawLine(theme.ControlDark, arrowX + 6, arrowY, arrowX + 12, arrowY - 4);

        // System colors dropdown (when open)
        bool dropdownOpen = systemColorDropdownOpen;
        int actualDropdownHeight = Math.Min(systemDropdownHeight, totalHeight - systemDropdownY - RgbSectionHeight);
        if (dropdownOpen)
        {
            int dropdownBottom = systemDropdownY + actualDropdownHeight;

            g.FillRectangle(Color.White, comboBoxX, systemDropdownY, comboBoxW, actualDropdownHeight);
            g.DrawRectangle(Color.FromArgb(180, 180, 180), comboBoxX, systemDropdownY, comboBoxW, actualDropdownHeight, 1);

            for (int i = 0; i < SystemColorCount; i++)
            {
                int itemY = systemDropdownY + i * SystemColorDropdownItemHeight;
                if (itemY >= dropdownBottom)
                    break;

                int itemEnd = Math.Min(itemY + SystemColorDropdownItemHeight, dropdownBottom);
                int itemH = itemEnd - itemY;
                bool isHovered = i == systemColorHoveredIndex;
                bool isSelected = i == selectedIndex;

                if (isHovered || isSelected)
                {
                    g.FillRectangle(isSelected ? theme.Highlight : Color.FromArgb(230, 240, 250), comboBoxX + 1, itemY, comboBoxW - 2, itemH);
                }

                // Swatch
                g.FillRectangle(ColorPalette[i], comboBoxX + 4, itemY + (itemH - 16) / 2, 16, 16);
                g.DrawRectangle(Color.FromArgb(150, 150, 150), comboBoxX + 4, itemY + (itemH - 16) / 2, 16, 16, 1);

                // Name
                string name = GetSystemColorName(i);
                var nameMeasure = g.MeasureString(name, theme.DefaultFont);
                g.DrawString(name, theme.DefaultFont, theme.ControlText, comboBoxX + 26, itemY + (itemH - nameMeasure.height) / 2);

                // Check mark for selected
                if (isSelected)
                {
                    g.DrawString("\u2713", theme.DefaultFont, theme.HighlightText, comboBoxX + comboBoxW - 20, itemY + (itemH - 14) / 2);
                }
            }
        }

        // Web-safe color grid
        int maxIndex = Math.Min(ColorPalette.Length - SystemColorCount, gridRows * ColorPaletteCols);
        for (int i = 0; i < maxIndex; i++)
        {
            int row = i / ColorPaletteCols;
            int col = i % ColorPaletteCols;

            int x = paletteX + col * cellSize;
            int y = gridStartY + row * cellSize;

            if (dropdownOpen && y < systemDropdownY + actualDropdownHeight)
                continue;

            int paletteIndex = SystemColorCount + i;
            bool isHovered = paletteIndex == hoveredIndex;
            bool isSelected = paletteIndex == selectedIndex;

            g.FillRectangle(Color.FromArgb(245, 245, 245), x, y, cellSize, cellSize);
            g.FillRectangle(ColorPalette[paletteIndex], x, y, swatchSize, swatchSize);

            if (isSelected || isHovered)
            {
                g.DrawRectangle(isSelected ? theme.Highlight : Color.FromArgb(100, 100, 100),
                    x, y, swatchSize, swatchSize, 1);
            }

            if (isSelected)
            {
                g.DrawLine(Color.White, x + 4, y + swatchSize / 2, x + swatchSize - 4, y + 4);
                g.DrawLine(Color.White, x + 4, y + 4, x + swatchSize - 4, y + swatchSize / 2);
            }
        }

        // Divider before RGB section
        if (!dropdownOpen)
        {
            int dividerY = rgbY - 1;
            g.DrawLine(Color.FromArgb(180, 180, 180), paletteX, dividerY, paletteX + paletteWidth, dividerY);
        }

       // RGB input fields
        int rgbTop = rgbY + 6;
        int rgbHeight = 22;
        int rgbLeft = paletteX + 8;
        int labelWidth = 20;
        int inputWidth = RgbInputWidth;
        int spacing = 6;
        int channelTotalWidth = labelWidth + inputWidth; // 62

       string[] channelNames = { "R", "G", "B" };
        string[] buffers = { rBuffer, gBuffer, bBuffer };
        for (int i = 0; i < 3; i++)
        {
            int labelX = rgbLeft + i * (channelTotalWidth + spacing);
            int inputX = labelX + labelWidth;

            Color inputBg = i == editingChannel ? theme.Highlight : theme.ControlBackground;
            Color inputFg = i == editingChannel ? Color.White : theme.ControlText;

            g.DrawString(channelNames[i], theme.DefaultFont, theme.ControlText, labelX, rgbTop);
            g.FillRectangle(inputBg, inputX, rgbTop, inputWidth, rgbHeight);
            g.DrawRectangle(i == editingChannel ? theme.Highlight : Color.FromArgb(180, 180, 180),
                inputX, rgbTop, inputWidth, rgbHeight, 1);

            string displayValue = buffers[i];
            if (displayValue.Length == 0) displayValue = "0";
            else if (displayValue.Length == 1) displayValue = "00" + displayValue;
            else if (displayValue.Length == 2) displayValue = "0" + displayValue;

            var measure = g.MeasureString(displayValue, theme.DefaultFont);
            g.DrawString(displayValue, theme.DefaultFont, inputFg,
                inputX + (inputWidth - measure.width) / 2, rgbTop + (rgbHeight - measure.height) / 2);
        }

        // "OK" button (placed right after RGB inputs, not at far right edge)
        int rgbAreaWidth = 3 * (channelTotalWidth + spacing) - spacing; // 204
        int okX = rgbLeft + rgbAreaWidth + 16;
        int okY = rgbY + 6;
        int okWidth = 60;
        int okHeight = 24;
        g.FillRectangle(Color.FromArgb(240, 240, 240), okX, okY, okWidth, okHeight);
        g.DrawRectangle(theme.ButtonBorder, okX, okY, okWidth, okHeight, 1);
        g.DrawString("OK", theme.DefaultFont, theme.ControlText, okX + (okWidth - 20) / 2, okY + 4);
    }

    private static string GetSystemColorName(int index) => index switch
    {
        0 => "Control",
        1 => "ControlText",
        2 => "Window",
        3 => "WindowText",
        4 => "Highlight",
        5 => "HighlightText",
        6 => "ActiveCaption",
        7 => "ActiveCaptionText",
        8 => "InactiveCaption",
        9 => "ControlLight",
        10 => "ControlDark",
        11 => "GrayText",
        _ => ""
    };

    private static bool IsLightColor(Color color)
    {
        int brightness = (color.R * 299 + color.G * 587 + color.B * 114) / 1000;
        return brightness > 128;
    }

    /// <summary>
    /// Begins inline editing for this row. Initializes the edit buffer with the current value.
    /// </summary>
    public void StartEdit()
    {
        var value = _descriptor.GetValue();
        _editBuffer = value?.ToString() ?? "";
        _caretPos = _editBuffer.Length;
        _isEditing = true;
        _isEnumDropdownOpen = false;
    }

    /// <summary>
    /// Opens the enum dropdown for this row. Initializes the selection to the current value.
    /// </summary>
    public void StartEnumDropdown()
    {
        var type = _descriptor.PropertyType;
        if (!type.IsEnum) return;

        var values = Enum.GetValues(type).Cast<object>().ToArray();
        var currentValue = _descriptor.GetValue();

        _selectedFlagIndices.Clear();

        if (type.GetCustomAttribute<System.FlagsAttribute>() != null && currentValue != null)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (currentValue is Enum enumVal && enumVal.HasFlag((Enum)values[i]))
                    _selectedFlagIndices.Add(i);
            }
        }

        if (_selectedFlagIndices.Count == 0)
        {
            _enumSelectedIndex = -1;
            for (int i = 0; i < values.Length; i++)
            {
                if (currentValue != null && currentValue.Equals(values[i]))
                {
                    _enumSelectedIndex = i;
                    break;
                }
            }
            if (_enumSelectedIndex < 0)
                _enumSelectedIndex = 0;
        }

        _isEnumDropdownOpen = true;
        _isEditing = false;
    }

    /// <summary>
    /// Opens the color picker palette for this row.
    /// </summary>
    public void StartColorPicker()
    {
        var currentColor = _descriptor.GetValue() is Color c ? c : Color.Black;
        _rgbRBuffer = currentColor.R.ToString();
        _rgbGBuffer = currentColor.G.ToString();
        _rgbBBuffer = currentColor.B.ToString();
        _rgbEditingChannel = -1;
        _colorPickerSelectedIndex = -1;
        _colorPickerHoveredIndex = -1;
        _isColorPickerOpen = true;
        _isEditing = false;
        _isEnumDropdownOpen = false;
    }

   /// <summary>
    /// Handles a mouse click on the color palette overlay.
    /// </summary>
    /// <param name="paletteX">X position of the palette overlay.</param>
    /// <param name="paletteY">Y position of the palette overlay.</param>
    /// <param name="mouseX">Mouse X coordinate.</param>
    /// <param name="mouseY">Mouse Y coordinate.</param>
    /// <param name="paletteWidth">Width of the palette.</param>
    /// <returns>The selected color index (>= 0), -1 for interactive area click (keep picker open), -2 to close picker.</returns>
    public int HandleColorPickerClick(int paletteX, int paletteY, int mouseX, int mouseY, int paletteWidth)
    {
        if (!_isColorPickerOpen) return -1;

        int localX = mouseX - paletteX;
        int localY = mouseY - paletteY;

        int systemDropdownY = SystemColorComboBoxHeight;
        int systemDropdownHeight = SystemColorCount * SystemColorDropdownItemHeight;
        int gridStartY = systemDropdownY;
        int gridRows = ColorPaletteMaxVisibleRows - 1;
        int rgbY = gridStartY + gridRows * (ColorSwatchSize + 1);
        int totalHeight = rgbY + RgbSectionHeight;

        if (localX < 0 || localY < 0 || localX >= paletteWidth || localY >= totalHeight)
            return -2;

        // System colors ComboBox area
        int comboBoxX = 4;
        int comboBoxY = 2;
        int comboBoxW = paletteWidth - 8;
        int comboBoxH = SystemColorComboBoxHeight - 4;

        if (localX >= comboBoxX && localX < comboBoxX + comboBoxW && localY >= comboBoxY && localY < comboBoxY + comboBoxH)
        {
            _systemColorDropdownOpen = !_systemColorDropdownOpen;
            return -1;
        }

        // System colors dropdown
        if (_systemColorDropdownOpen && localY >= systemDropdownY && localY < systemDropdownY + systemDropdownHeight)
        {
            int dropdownBottom = Math.Min(systemDropdownY + systemDropdownHeight, totalHeight - RgbSectionHeight);
            if (localY < dropdownBottom)
            {
                int dropIndex = (localY - systemDropdownY) / SystemColorDropdownItemHeight;
                if (dropIndex >= 0 && dropIndex < SystemColorCount)
                {
                    _systemColorDropdownOpen = false;
                    _colorPickerSelectedIndex = dropIndex;
                    return dropIndex;
                }
            }
            return -1;
        }

        // Close dropdown if clicking elsewhere
        if (_systemColorDropdownOpen)
        {
            _systemColorDropdownOpen = false;
        }

        // Color grid section
        if (localY >= gridStartY && localY < rgbY)
        {
            int gridY = localY - gridStartY;
            int gridRow = gridY / 19;
            int gridCol = localX / 19;

            if (gridCol < 0 || gridCol >= ColorPaletteCols || gridRow < 0 || gridRow >= gridRows)
                return -1;

            int gridIndex = SystemColorCount + gridRow * ColorPaletteCols + gridCol;

            if (gridIndex < ColorPalette.Length - 1)
            {
                _colorPickerSelectedIndex = gridIndex;
                return gridIndex;
            }
        }

        // RGB section - keep picker open
        if (localY >= rgbY && localY < totalHeight)
        {
            return -1;
        }

        return -1;
    }

    /// <summary>
    /// Handles a mouse click on the RGB input fields within the color palette overlay.
    /// </summary>
    /// <param name="paletteX">X position of the palette overlay.</param>
    /// <param name="paletteY">Y position of the palette overlay.</param>
    /// <param name="mouseX">Mouse X coordinate.</param>
    /// <param name="mouseY">Mouse Y coordinate.</param>
    /// <param name="paletteWidth">Width of the palette.</param>
    /// <returns>The RGB channel index (0=R, 1=G, 2=B) being edited, or -1.</returns>
    public int HandleRgbClick(int paletteX, int paletteY, int mouseX, int mouseY, int paletteWidth)
    {
        if (!_isColorPickerOpen) return -1;

        int localX = mouseX - paletteX;
        int localY = mouseY - paletteY;

        int gridStartY = SystemColorComboBoxHeight;
        int gridRows = ColorPaletteMaxVisibleRows - 1;
        int rgbY = gridStartY + gridRows * (ColorSwatchSize + 1);
        if (localY < rgbY || localY >= rgbY + RgbSectionHeight)
            return -1;

        int rgbTop = rgbY + 6;
        int rgbHeight = RgbSectionHeight - 12;
        int rgbLeft = 8;
        int labelWidth = 20;
        int inputWidth = RgbInputWidth;
        int spacing = 6;
        int channelTotalWidth = labelWidth + inputWidth;

        for (int i = 0; i < 3; i++)
        {
            int inputX = rgbLeft + i * (channelTotalWidth + spacing) + labelWidth;
            if (localX >= inputX && localX < inputX + inputWidth && localY >= rgbTop && localY < rgbTop + rgbHeight)
            {
                _rgbEditingChannel = i;
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Handles text input for the currently editing RGB channel.
    /// </summary>
    /// <param name="key">The key pressed.</param>
    public void HandleRgbInput(char key)
    {
        if (_rgbEditingChannel < 0 || _rgbEditingChannel > 2) return;

        if (!char.IsDigit(key) && key != '\b') return;

        var channelBuffers = new[] { _rgbRBuffer, _rgbGBuffer, _rgbBBuffer };
        var buffer = channelBuffers[_rgbEditingChannel];

        if (key == '\b')
        {
            if (buffer.Length > 0)
            {
                buffer = buffer.Substring(0, buffer.Length - 1);
                channelBuffers[_rgbEditingChannel] = buffer;
            }
        }
        else
        {
            if (buffer.Length < 3)
            {
                buffer += key;
                channelBuffers[_rgbEditingChannel] = buffer;
            }
        }

        if (buffer.Length == 3)
        {
            CommitRgbFromBuffers();
        }
    }

    /// <summary>
    /// Commits the RGB values from the input buffers to the property.
    /// </summary>
    public void CommitRgbFromBuffers()
    {
        var buffers = new[] { _rgbRBuffer, _rgbGBuffer, _rgbBBuffer };
        byte r = 0, g = 0, b = 0;

        if (byte.TryParse(buffers[0], out r) && byte.TryParse(buffers[1], out g) && byte.TryParse(buffers[2], out b))
        {
            _descriptor.SetValue(Color.FromArgb(r, g, b));
        }

        _rgbEditingChannel = -1;
        _rgbRBuffer = string.Empty;
        _rgbGBuffer = string.Empty;
        _rgbBBuffer = string.Empty;
        _isColorPickerOpen = false;
    }

    internal string _rgbRBuffer = string.Empty;
    internal string _rgbGBuffer = string.Empty;
    internal string _rgbBBuffer = string.Empty;
    internal int _rgbEditingChannel = -1;

    /// <summary>
    /// Commits the color selection from the palette to the property.
    /// </summary>
    /// <param name="colorIndex">The index of the selected color in the palette (0-288).</param>
    public void CommitColorSelection(int colorIndex)
    {
        if (colorIndex < 0 || colorIndex >= ColorPalette.Length)
            return;
        _descriptor.SetValue(ColorPalette[colorIndex]);
        _colorPickerSelectedIndex = colorIndex;
        _isColorPickerOpen = false;
    }

    /// <summary>
    /// Cancels the color picker without saving.
    /// </summary>
    public void CancelColorPicker()
    {
        _isColorPickerOpen = false;
        _colorPickerHoveredIndex = -1;
        _colorPickerSelectedIndex = -1;
    }

    /// <summary>
    /// Selects an item in the enum dropdown by index.
    /// For flags enums, toggles the flag at the given index.
    /// For non-flags enums, sets the single selection.
    /// </summary>
    /// <param name="index">The index of the enum value to select.</param>
    public void SelectEnumItem(int index)
    {
        var type = _descriptor.PropertyType;
        if (!type.IsEnum) return;

        if (type.GetCustomAttribute<System.FlagsAttribute>() != null)
        {
            if (index < 0 || index >= Enum.GetValues(type).Length)
                return;

            if (!_selectedFlagIndices.Contains(index))
                _selectedFlagIndices.Add(index);
            else
                _selectedFlagIndices.Remove(index);
        }
        else
        {
            var values = Enum.GetValues(type).Cast<object>().ToArray();
            if (index >= 0 && index < values.Length)
                _enumSelectedIndex = index;
        }
    }

     /// <summary>
    /// Commits the enum dropdown selection or the current edit buffer to the property.
    /// </summary>
    public void CommitEdit()
    {
        if (_isEnumDropdownOpen)
        {
            var type = _descriptor.PropertyType;
            var isFlags = type.GetCustomAttribute<System.FlagsAttribute>() != null;

            if (isFlags)
            {
                if (_selectedFlagIndices.Count > 0)
                {
                    var values = Enum.GetValues(type).Cast<object>().ToArray();
                    ulong combined = 0;
                    foreach (var idx in _selectedFlagIndices)
                    {
                        var val = Convert.ToUInt64(values[idx]);
                        combined |= val;
                    }
                    _descriptor.SetValue(Enum.ToObject(type, combined));
                }
                else
                {
                    _descriptor.SetValue(Enum.ToObject(type, 0));
                }
            }
            else if (_enumSelectedIndex >= 0)
            {
                var values = Enum.GetValues(type).Cast<object>().ToArray();
                if (_enumSelectedIndex < values.Length)
                    _descriptor.SetValue(values[_enumSelectedIndex]);
            }

            _isEnumDropdownOpen = false;
            _enumSelectedIndex = -1;
            _selectedFlagIndices.Clear();
            return;
        }

        if (!_isEditing) return;

        try
        {
            if (_descriptor.PropertyType == typeof(string))
                _descriptor.SetValue(_editBuffer);
            else if (_descriptor.PropertyType == typeof(int))
                _descriptor.SetValue(int.TryParse(_editBuffer, out var i) ? i : 0);
            else if (_descriptor.PropertyType == typeof(float))
                _descriptor.SetValue(float.TryParse(_editBuffer, out var f) ? f : 0f);
            else if (_descriptor.PropertyType.IsEnum)
            {
                if (Enum.TryParse(_descriptor.PropertyType, _editBuffer, true, out var result))
                    _descriptor.SetValue(result);
            }
            else if (_descriptor.PropertyType == typeof(Color))
            {
                var text = _editBuffer.Trim();
                if (text.StartsWith("RGB(") && text.EndsWith(")"))
                {
                    var inner = text.Substring(4, text.Length - 5);
                    var parts = inner.Split(',');
                    if (parts.Length == 3 &&
                        byte.TryParse(parts[0], out var r) &&
                        byte.TryParse(parts[1], out var g) &&
                        byte.TryParse(parts[2], out var b))
                    {
                        _descriptor.SetValue(Color.FromArgb(r, g, b));
                    }
                }
            }
        }
        catch { }

        _isEditing = false;
    }

  /// <summary>
    /// Cancels editing or closes the enum dropdown without saving.
    /// </summary>
    public void CancelEdit()
    {
        if (_isEnumDropdownOpen)
        {
            _isEnumDropdownOpen = false;
            _enumSelectedIndex = -1;
            _selectedFlagIndices.Clear();
            return;
        }

        _isEditing = false;
    }

    /// <summary>
    /// Handles a key press during editing.
    /// Returns true if the key was handled.
    /// </summary>
    public bool HandleKey(char keyChar)
    {
        if (!_isEditing) return false;

        if (keyChar == '\r')
        {
            CommitEdit();
            return true;
        }

        if (keyChar == '\b' && _caretPos > 0)
        {
            _editBuffer = _editBuffer.Remove(_caretPos - 1, 1);
            _caretPos--;
            return true;
        }

        if (keyChar >= 32)
        {
            _editBuffer = _editBuffer.Insert(_caretPos, keyChar.ToString());
            _caretPos++;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets whether this row is currently being edited.
    /// </summary>
    public bool IsEditing => _isEditing;

    /// <summary>
    /// Gets whether the enum dropdown is currently open for this row.
    /// </summary>
    public bool IsEnumDropdownOpen => _isEnumDropdownOpen;

   /// <summary>
    /// Gets the index of the currently selected item in the enum dropdown.
    /// </summary>
    public int EnumSelectedIndex => _enumSelectedIndex;

    /// <summary>
    /// Gets whether the given index is a selected flag in a flags enum dropdown.
    /// </summary>
    /// <param name="index">The index to check.</param>
    /// <returns>True if the flag at the given index is selected.</returns>
    public bool IsFlagSelected(int index)
    {
        return _selectedFlagIndices.Contains(index);
    }

    /// <summary>
    /// Gets whether this enum type has the Flags attribute (supports multi-selection).
    /// </summary>
    public bool IsFlagsEnum => _descriptor.PropertyType.GetCustomAttribute<System.FlagsAttribute>() != null;

    /// <summary>
    /// Gets the number of enum values for this property type.
    /// </summary>
    public int EnumValueCount
    {
        get
        {
            if (!_descriptor.PropertyType.IsEnum) return 0;
            return Enum.GetValues(_descriptor.PropertyType).Length;
        }
    }

    /// <summary>
    /// Gets the display text for an enum value at the given index.
    /// </summary>
    /// <param name="index">The index of the enum value.</param>
    /// <returns>The formatted display text.</returns>
    public string GetEnumDisplayText(int index)
    {
        var type = _descriptor.PropertyType;
        var values = Enum.GetValues(type).Cast<object>().ToArray();

        if (index < 0 || index >= values.Length)
            return "";

        var value = values[index];

        if (type == typeof(AnchorStyles))
            return FormatAnchor((AnchorStyles)value);

        return value.ToString() ?? "";
    }

    /// <summary>
    /// Gets the current color value of the property.
    /// </summary>
    public Color GetCurrentColor()
    {
        return _descriptor.GetValue() is Color c ? c : Color.Black;
    }

    /// <summary>
    /// Sets the color value on the property.
    /// </summary>
    public void SetColorValue(Color color)
    {
        _descriptor.SetValue(color);
    }

    /// <summary>
    /// Gets or sets whether this row is in enum-dropdown mode.
    /// </summary>
    public bool IsEnumDropdown
    {
        get => _isEnumDropdownOpen;
        set => _isEnumDropdownOpen = value;
    }

    /// <summary>
    /// Gets whether this row's property type is Color.
    /// </summary>
    public bool IsColorProperty => _descriptor.PropertyType == typeof(Color);

    /// <summary>
    /// Gets whether the color picker is currently open for this row.
    /// </summary>
    public bool IsColorPickerOpen => _isColorPickerOpen;

    /// <summary>
    /// Gets the index of the currently hovered color in the palette.
    /// </summary>
    public int ColorPickerHoveredIndex => _colorPickerHoveredIndex;

    /// <summary>
    /// Gets the index of the currently selected color in the palette.
    /// </summary>
    public int ColorPickerSelectedIndex => _colorPickerSelectedIndex;

    /// <summary>
    /// Sets the hovered color index in the palette.
    /// </summary>
    /// <param name="index">The index to set as hovered.</param>
    public void SetColorPickerHovered(int index)
    {
        _colorPickerHoveredIndex = index;
    }

    /// <summary>
    /// Sets the hovered system color dropdown item index.
    /// </summary>
    /// <param name="index">The index to set as hovered, or -1.</param>
    public void SetSystemColorHovered(int index)
    {
        _systemColorHoveredIndex = index;
    }

    /// <summary>
    /// Gets the palette color at the given index.
    /// </summary>
    /// <param name="index">The index (0-288).</param>
    /// <returns>The color at that index.</returns>
    public static Color GetPaletteColor(int index)
    {
        if (index < 0 || index >= ColorPalette.Length)
            return Color.Empty;
        return ColorPalette[index];
    }
}
