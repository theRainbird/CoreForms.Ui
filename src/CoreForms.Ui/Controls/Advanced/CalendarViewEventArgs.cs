using CoreForms.Ui.Core;

namespace CoreForms.Ui.Controls.Advanced;

/// <summary>
/// Provides data for the DateSelected event of the CalendarView.
/// </summary>
public class DateSelectedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the date that was selected.
    /// </summary>
    public DateTime Date { get; }

    /// <summary>
    /// Initializes a new instance of DateSelectedEventArgs.
    /// </summary>
    /// <param name="date">The selected date.</param>
    public DateSelectedEventArgs(DateTime date)
    {
        Date = date;
    }
}

/// <summary>
/// Provides data for the AppointmentSelected event of the CalendarView.
/// </summary>
public class AppointmentSelectedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the appointment that was selected.
    /// </summary>
    public CalendarAppointment Appointment { get; }

    /// <summary>
    /// Initializes a new instance of AppointmentSelectedEventArgs.
    /// </summary>
    /// <param name="appointment">The selected appointment.</param>
    public AppointmentSelectedEventArgs(CalendarAppointment appointment)
    {
        Appointment = appointment;
    }
}

/// <summary>
/// Provides data for the AppointmentChanged event of the CalendarView.
/// Supports cancellation to revert the change.
/// </summary>
public class CalendarAppointmentChangedEventArgs : CancelEventArgs
{
    /// <summary>
    /// Gets the appointment that was changed.
    /// </summary>
    public CalendarAppointment Appointment { get; }

    /// <summary>
    /// Gets or sets a description of the change (e.g. "move", "resize", "edit").
    /// </summary>
    public string ChangeType { get; set; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of CalendarAppointmentChangedEventArgs.
    /// </summary>
    /// <param name="appointment">The changed appointment.</param>
    /// <param name="changeType">The type of change.</param>
    public CalendarAppointmentChangedEventArgs(CalendarAppointment appointment, string changeType)
        : base(false)
    {
        Appointment = appointment;
        ChangeType = changeType;
    }
}
