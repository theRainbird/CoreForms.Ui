namespace CoreForms.Ui.Core;

/// <summary>
/// Represents the visual state of a control.
/// </summary>
[Flags]
public enum ControlState
{
    /// <summary>
    /// No special state.
    /// </summary>
    None = 0,

    /// <summary>
    /// The control is hovered by the mouse.
    /// </summary>
    Hovered = 1,

    /// <summary>
    /// The control has a mouse button pressed.
    /// </summary>
    Pressed = 2,

    /// <summary>
    /// The control has input focus.
    /// </summary>
    Focused = 4
}