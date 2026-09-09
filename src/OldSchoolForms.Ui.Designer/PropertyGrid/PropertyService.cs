using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OldSchoolForms.Ui.Core;
using OldSchoolForms.Ui.Design;

namespace OldSchoolForms.Ui.Designer.PropertyGrid;

/// <summary>
/// Uses reflection to enumerate public properties of a control
/// and wraps them in <see cref="PropertyDescriptor"/> instances
/// with categorization and filtering.
/// </summary>
public class PropertyService
{
    private static readonly HashSet<string> HiddenProperties = new()
    {
        "Controls", "Parent", "Site", "Handle", "WindowId",
        "CaptureControl", "IsOpaque", "Focused", "CapturingMouse",
        "EffectiveFont", "EffectiveZoom", "Dirty", "ClientSize",
        "ClientRectangle", "ClientSizePixels", "State", "ActiveControl",
        "BindingContext", "DataBindings", "Cursor", "CursorType",
        "Modal", "DialogResult", "WindowState", "FormBorderStyle",
        "RequiresRender", "HasDirtyDescendant", "CachedForm",
        "LayoutDrivenBoundsChange", "LayoutVersion", "LayoutSuspended",
        "LayoutDirty", "BackColorSet", "ForeColorSet", "Zoom",
        "SelectedItem", "SelectedIndex", "Items", "DropDownItems",
        "IsDropDownVisible", "IsSelected", "PreferredSize"
    };

    private static bool _editorsRegistered;

    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyService"/> and registers
    /// the built-in value editors.
    /// </summary>
    public PropertyService()
    {
        if (!_editorsRegistered)
        {
            ValueEditorRegistry.Register(new TabPagesEditor());
            _editorsRegistered = true;
        }
    }

    private static readonly HashSet<Type> SupportedTypes = new()
    {
        typeof(string), typeof(int), typeof(float), typeof(bool),
        typeof(Color), typeof(Point), typeof(Size), typeof(Padding),
        typeof(Font), typeof(FontStyle),
        typeof(AnchorStyles), typeof(DockStyle),
        typeof(FormBorderStyle),
        typeof(FormWindowState)
    };

    /// <summary>
    /// Gets all browsable property descriptors for the given control,
    /// grouped and sorted by category.
    /// </summary>
    /// <param name="control">The control to inspect.</param>
    /// <returns>A list of property descriptors.</returns>
    public List<PropertyDescriptor> GetProperties(Control control)
    {
        if (control == null) return new List<PropertyDescriptor>();

        var properties = control.GetType().GetProperties(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(p => p.CanRead)
            .Where(p => !HiddenProperties.Contains(p.Name))
            .Where(p => IsSupportedType(p.PropertyType) || HasEditorButton(p))
            .Select(p => new PropertyDescriptor(p, control, ResolveEditor(p)))
            .OrderBy(p => GetCategoryOrder(p.Category))
            .ThenBy(p => p.Name)
            .ToList();

        // Also include inherited key properties from Control
        var inherited = typeof(Control).GetProperties(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(p => p.CanRead)
            .Where(p => !HiddenProperties.Contains(p.Name))
            .Where(p => IsSupportedType(p.PropertyType) || HasEditorButton(p))
            .Select(p => new PropertyDescriptor(p, control, ResolveEditor(p)))
            .OrderBy(p => GetCategoryOrder(p.Category))
            .ThenBy(p => p.Name);

        properties.AddRange(inherited);

        return properties.DistinctBy(p => p.Name).ToList();
    }

    private static bool HasEditorButton(PropertyInfo property)
        => property.GetCustomAttribute<EditorButtonAttribute>() != null;

    private static IValueEditor? ResolveEditor(PropertyInfo property)
        => ValueEditorRegistry.Find(property);

    private static bool IsSupportedType(Type type)
    {
        if (SupportedTypes.Contains(type)) return true;
        if (type.IsEnum) return true;
        if (type == typeof(string)) return true;
        return false;
    }

    private static int GetCategoryOrder(string category)
    {
        return category switch
        {
            "Darstellung" => 0,
            "Verhalten" => 1,
            "Layout" => 2,
            "Design" => 4,
            "Data" => 3,
            "Sonstiges" => 5,
            _ => 99
        };
    }
}
