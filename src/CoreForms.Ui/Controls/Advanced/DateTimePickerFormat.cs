namespace CoreForms.Ui.Controls.Advanced;

/// <summary>
/// Specifies the format of the date and time displayed in a <see cref="DateTimePicker"/>.
/// </summary>
public enum DateTimePickerFormat
{
    /// <summary>
    /// Displays the date in the long date format (culture-dependent, e.g. "dddd, MMMM dd, yyyy").
    /// </summary>
    Long,

    /// <summary>
    /// Displays the date in the short date format (culture-dependent, e.g. "MM/dd/yyyy").
    /// </summary>
    Short,

    /// <summary>
    /// Displays the time in a culture-dependent time format (e.g. "HH:mm:ss").
    /// </summary>
    Time,

    /// <summary>
    /// Displays the value using a custom format string specified by <see cref="DateTimePicker.CustomFormat"/>.
    /// </summary>
    Custom,
}
