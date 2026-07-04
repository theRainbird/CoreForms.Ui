using OldSchoolForms.Ui.Controls.Advanced;
using OldSchoolForms.Ui.Controls;
using Graphics = OldSchoolForms.Ui.Rendering.Graphics;
using OldSchoolForms.Ui.Core;

namespace OldSchoolForms.Ui.Demo.UserControls;

/// <summary>
/// Demonstrates the CalendarView control with sample appointments.
/// </summary>
public class CalendarPage : UserControl
{
    /// <summary>
    /// Occurs when the status text should be updated.
    /// </summary>
    public event EventHandler<StatusTextChangedEventArgs>? StatusTextChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarPage"/> class.
    /// </summary>
    public CalendarPage()
    {
        var calendar = new CalendarView
        {
            Dock = DockStyle.Fill,
            ViewType = CalendarViewType.Month
        };

        var today = DateTime.Today;
        calendar.Appointments.Add(new CalendarAppointment
        {
            Subject = SR.GetString("AppointmentTeamMeeting"),
            StartTime = today.AddHours(10),
            EndTime = today.AddHours(11),
            Location = SR.GetString("AppointmentRoom101"),
            CategoryColor = CalendarView.CategoryColors[0]
        });

        calendar.Appointments.Add(new CalendarAppointment
        {
            Subject = SR.GetString("AppointmentLunch"),
            StartTime = today.AddHours(12),
            EndTime = today.AddHours(13),
            Location = SR.GetString("AppointmentCafeteria"),
            CategoryColor = CalendarView.CategoryColors[2]
        });

        calendar.Appointments.Add(new CalendarAppointment
        {
            Subject = SR.GetString("AppointmentProjectReview"),
            StartTime = today.AddHours(14).AddMinutes(30),
            EndTime = today.AddHours(16),
            Location = SR.GetString("AppointmentConfRoom"),
            CategoryColor = CalendarView.CategoryColors[1]
        });

        calendar.Appointments.Add(new CalendarAppointment
        {
            Subject = SR.GetString("AppointmentWorkshop"),
            StartTime = today.AddDays(1).AddHours(9),
            EndTime = today.AddDays(1).AddHours(12),
            Location = SR.GetString("AppointmentTrainingRoom"),
            CategoryColor = CalendarView.CategoryColors[3]
        });

        calendar.Appointments.Add(new CalendarAppointment
        {
            Subject = SR.GetString("AppointmentSprintPlanning"),
            StartTime = today.AddDays(3).AddHours(10),
            EndTime = today.AddDays(3).AddHours(12),
            Location = SR.GetString("AppointmentRoom204"),
            CategoryColor = CalendarView.CategoryColors[0]
        });

        calendar.Appointments.Add(new CalendarAppointment
        {
            Subject = SR.GetString("AppointmentConference"),
            StartTime = today.AddDays(10),
            EndTime = today.AddDays(12),
            IsAllDay = true,
            CategoryColor = CalendarView.CategoryColors[4]
        });

        calendar.DateSelected += (s, e) =>
        {
            OnStatusTextChanged(string.Format(SR.GetString("StatusSelectedDateFormat"), e.Date.ToString("dddd, MMMM dd, yyyy")));
        };

        calendar.ViewChanged += (s, e) =>
        {
            OnStatusTextChanged(string.Format(SR.GetString("StatusViewFormat"), calendar.ViewType));
        };

        Controls.Add(calendar);
    }

    private void OnStatusTextChanged(string text)
    {
        StatusTextChanged?.Invoke(this, new StatusTextChangedEventArgs(text));
    }
}
