using System.Reflection;

namespace OldSchoolForms.Ui.Data;

/// <summary>
/// Provides a lightweight property descriptor based on reflection.
/// Used by the binding infrastructure to get and set property values.
/// </summary>
public class ReflectionPropertyDescriptor
{
    /// <summary>
    /// Gets the name of the property.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the type of the property.
    /// </summary>
    public Type PropertyType { get; }

    private readonly PropertyInfo _property;

    /// <summary>
    /// Initializes a new instance for the specified property.
    /// </summary>
    /// <param name="property">The PropertyInfo to wrap.</param>
    /// <exception cref="ArgumentNullException">Thrown when property is null.</exception>
    public ReflectionPropertyDescriptor(PropertyInfo property)
    {
        _property = property ?? throw new ArgumentNullException(nameof(property));
        Name = property.Name;
        PropertyType = property.PropertyType;
    }

    /// <summary>
    /// Gets the property value from the specified component.
    /// </summary>
    /// <param name="component">The object to read from.</param>
    /// <returns>The property value.</returns>
    public object? GetValue(object component)
    {
        return _property.GetValue(component);
    }

    /// <summary>
    /// Sets the property value on the specified component.
    /// </summary>
    /// <param name="component">The object to write to.</param>
    /// <param name="value">The value to set.</param>
    public void SetValue(object component, object? value)
    {
        _property.SetValue(component, value);
    }

    /// <summary>
    /// Gets the property descriptor for the named property on the given type.
    /// </summary>
    /// <param name="type">The type to search.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <returns>The descriptor, or null if the property is not found.</returns>
    public static ReflectionPropertyDescriptor? GetProperty(Type type, string propertyName)
    {
        var prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
        if (prop == null || !prop.CanRead)
            return null;
        return new ReflectionPropertyDescriptor(prop);
    }
}
