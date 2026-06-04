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
    private readonly PropertyDescriptor _descriptor;
    private readonly int _nameWidth;
    private bool _isEditing;
    private bool _isEnumDropdownOpen;
    private int _enumSelectedIndex = -1;
    private readonly List<int> _selectedFlagIndices = new();
    private string _editBuffer = string.Empty;
    private int _caretPos;

    private const int DropdownArrowWidth = 20;

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
        if (mouseX <= valueColumnX || _descriptor.IsReadOnly)
            return false;

        int localX = mouseX - valueColumnX;

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
    /// Gets or sets whether this row is in enum-dropdown mode.
    /// </summary>
    public bool IsEnumDropdown
    {
        get => _isEnumDropdownOpen;
        set => _isEnumDropdownOpen = value;
    }
}
