using OldSchoolForms.Ui.Core;

namespace OldSchoolForms.Ui.Controls.Advanced;

/// <summary>
/// Represents a single appointment or event displayed in a CalendarView.
/// </summary>
public class CalendarAppointment
{
    /// <summary>
    /// Gets the unique identifier for this appointment.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the subject/title of the appointment.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the start time of the appointment.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Gets or sets the end time of the appointment.
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Gets or sets the location of the appointment.
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Gets or sets the category color used for the appointment bar in the calendar.
    /// </summary>
    public Color CategoryColor { get; set; }

    /// <summary>
    /// Gets or sets whether this appointment spans the entire day.
    /// </summary>
    public bool IsAllDay { get; set; }

    /// <summary>
    /// Gets or sets whether this appointment is read-only and cannot be moved, resized, or edited.
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Returns a string representation of the appointment.
    /// </summary>
    public override string ToString()
    {
        var start = StartTime.ToString("HH:mm");
        var end = EndTime.ToString("HH:mm");
        return Location != null
            ? $"{Subject} ({start}-{end}, {Location})"
            : $"{Subject} ({start}-{end})";
    }
}
