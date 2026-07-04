using System;
using System.Collections.Generic;
using System.Reflection;
using OldSchoolForms.Ui.Core;

namespace OldSchoolForms.Ui.Designer.PropertyGrid;

/// <summary>
/// Describes a single property for display and editing in the PropertyGrid.
/// Wraps <see cref="PropertyInfo"/> and caches metadata attributes.
/// </summary>
public class PropertyDescriptor
{
    private readonly PropertyInfo _property;
    private readonly object _target;

    /// <summary>
    /// Gets the display name of the property.
    /// </summary>
    public string Name => _property.Name;

    /// <summary>
    /// Gets the category for grouping in the grid.
    /// </summary>
    public string Category { get; }

    /// <summary>
    /// Gets the property type.
    /// </summary>
    public Type PropertyType => _property.PropertyType;

    /// <summary>
    /// Gets whether this property is read-only.
    /// </summary>
    public bool IsReadOnly => !_property.CanWrite;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="property">The reflection PropertyInfo.</param>
    /// <param name="target">The object instance to read/write values from.</param>
    public PropertyDescriptor(PropertyInfo property, object target)
    {
        _property = property;
        _target = target;

        Category = property.GetCustomAttribute<System.ComponentModel.CategoryAttribute>()?.Category
            ?? GuessCategory(property.Name);
    }

    /// <summary>
    /// Gets the current value of the property on the target.
    /// </summary>
    public object? GetValue()
    {
        try { return _property.GetValue(_target); }
        catch { return null; }
    }

    /// <summary>
    /// Sets the value of the property on the target.
    /// </summary>
    public void SetValue(object? value)
    {
        try
        {
            var converted = ConvertValue(value, PropertyType);
            _property.SetValue(_target, converted);
        }
        catch { }
    }

    private static object? ConvertValue(object? value, Type targetType)
    {
        if (value == null) return null;
        if (value.GetType() == targetType) return value;

        if (targetType == typeof(string))
            return value.ToString();

        if (targetType.IsEnum)
        {
            if (value is string str)
                return Enum.Parse(targetType, str);
            return Enum.ToObject(targetType, value);
        }

        if (targetType == typeof(int))
            return Convert.ToInt32(value);
        if (targetType == typeof(float))
            return Convert.ToSingle(value);
        if (targetType == typeof(bool))
            return Convert.ToBoolean(value);

        return value;
    }

    private static string GuessCategory(string propertyName)
    {
        var layoutProps = new HashSet<string>
        {
            "Location", "Size", "Width", "Height", "X", "Y",
            "Anchor", "Dock", "Padding", "Margin", "Bounds",
            "MinimumSize", "MaximumSize", "PreferredSize"
        };

        var appearanceProps = new HashSet<string>
        {
            "BackColor", "ForeColor", "Font", "Text", "TextAlign",
            "Image", "ImageAlign", "BackgroundImage", "Cursor",
            "FlatStyle", "BorderStyle", "RightToLeft"
        };

        var behaviorProps = new HashSet<string>
        {
            "Enabled", "Visible", "TabStop", "TabIndex",
            "ReadOnly", "AllowDrop", "AutoSize"
        };

        var designProps = new HashSet<string>
        {
            "Name", "Tag", "Modifiers", "GenerateMember", "Locked"
        };

        if (layoutProps.Contains(propertyName)) return "Layout";
        if (appearanceProps.Contains(propertyName)) return "Darstellung";
        if (behaviorProps.Contains(propertyName)) return "Verhalten";
        if (designProps.Contains(propertyName)) return "Design";
        return "Sonstiges";
    }
}
