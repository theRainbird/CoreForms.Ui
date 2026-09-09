using System;

namespace OldSchoolForms.Ui.Design;

/// <summary>
/// Marks a property in the property browser as having a dedicated value editor
/// (opened via an ellipsis button) instead of an inline text editor.
/// The attribute is evaluated by the property browser; it carries the caption
/// shown on the ellipsis button.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class EditorButtonAttribute : Attribute
{
    /// <summary>
    /// Gets the caption displayed on the ellipsis button.
    /// </summary>
    public string ButtonText { get; }

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="buttonText">The caption shown on the ellipsis button. Defaults to "…".</param>
    public EditorButtonAttribute(string buttonText = "…")
    {
        ButtonText = buttonText;
    }
}
