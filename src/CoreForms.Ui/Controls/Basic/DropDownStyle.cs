namespace CoreForms.Ui.Controls.Basic;

/// <summary>
/// Specifies the style of the drop-down list in a <see cref="ComboBox"/>.
/// </summary>
public enum DropDownStyle
{
    /// <summary>
    /// The list is always visible and the text portion is editable.
    /// </summary>
    Simple,

    /// <summary>
    /// The list drops down and the text portion is editable.
    /// </summary>
    DropDown,

    /// <summary>
    /// The list drops down and the text portion is not editable.
    /// The user must select an item from the drop-down list.
    /// </summary>
    DropDownList
}
