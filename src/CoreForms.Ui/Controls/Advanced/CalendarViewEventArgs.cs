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
