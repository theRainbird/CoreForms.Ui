using System.Reflection;
using OldSchoolForms.Ui.Core;

namespace OldSchoolForms.Ui.Design;

/// <summary>
/// Provides a dedicated value editor for a specific property, opened from the
/// property browser via an ellipsis button. Implementations mutate the target
/// control directly (they are not bound to reflection set access) and therefore
/// also work for read-only / collection properties.
/// </summary>
public interface IValueEditor
{
    /// <summary>
    /// Gets the caption displayed on the ellipsis button in the property browser.
    /// </summary>
    string ButtonText { get; }

    /// <summary>
    /// Gets whether this editor is responsible for the given property.
    /// </summary>
    /// <param name="property">The property to test.</param>
    /// <returns>True if this editor handles the property.</returns>
    bool Handles(PropertyInfo property);

    /// <summary>
    /// Opens the dedicated editor for the given control's property.
    /// </summary>
    /// <param name="control">The control that owns the property being edited.</param>
    void Open(Control control);
}
