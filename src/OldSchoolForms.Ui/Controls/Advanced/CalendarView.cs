using System.Globalization;
using OldSchoolForms.Ui.Controls.Basic;
using OldSchoolForms.Ui.Core;
using OldSchoolForms.Ui.Theming;
using Graphics = OldSchoolForms.Ui.Rendering.Graphics;

namespace OldSchoolForms.Ui.Controls.Advanced;

/// <summary>
/// A calendar control supporting Month, Week, and Day views with appointment display,
/// navigation, and an appointment editing dialog. Similar in appearance to common Groupware.
/// </summary>
public class CalendarView : ContainerControl
{
    #region Constants

    private const int HeaderHeight = 46;
    private const int NavButtonWidth = 28;
    private const int NavButtonHeight = 24;
    private const int ViewTabWidth = 52;
    private const int ViewTabHeight = 24;
    private const int MonthDayHeaderHeight = 24;
    private const int TimeRulerWidth = 52;
    private const int SlotHeight = 18;
    private const int MiniCalWidth = 170;
    private const int MiniCalCellSize = 20;
    private const int MiniCalHeaderHeight = 24;
    private const int AllDayAreaHeight = 22;
    private const int DayHeaderHeight = 44;
    private const int MaxMonthAppointmentRows = 4;

    private static readonly string[] DayNames = { "Mo", "Di", "Mi", "Do", "Fr", "Sa", "So" };
    private static readonly string[] TimeSlots = GenerateTimeSlots();

    private static string[] GenerateTimeSlots()
    {
        var slots = new string[48];
        for (int i = 0; i < 48; i++)
        {
            int h = i / 2;
            int m = (i % 2) * 30;
            slots[i] = $"{h:D2}:{m:D2}";
        }
        return slots;
    }

    public static readonly Color[] CategoryColors =
    {
        Color.FromArgb(100, 160, 220),
        Color.FromArgb(220, 120, 110),
        Color.FromArgb(110, 180, 110),
        Color.FromArgb(220, 200, 100),
        Color.FromArgb(170, 130, 210),
        Color.FromArgb(100, 190, 190),
        Color.FromArgb(230, 160, 100),
        Color.FromArgb(160, 160, 160),
    };

    #endregion

    #region Fields

    private CalendarViewType _viewType = CalendarViewType.Month;
    private DateTime _currentDate;
    private DateTime _selectedDate;
    private readonly List<CalendarAppointment> _appointments = new();
    private int _scrollOffset;
    private int _maxScrollOffset;
    private bool _showMiniCal = true;

    // Header hit-test rectangles (set during Render)
    private Rectangle _prevBtnRect;
    private Rectangle _todayBtnRect;
    private Rectangle _nextBtnRect;
    private Rectangle _monthTabRect;
    private Rectangle _weekTabRect;
    private Rectangle _dayTabRect;

    // Mini calendar hit-test state
    private Rectangle _miniCalBounds;
    private Rectangle _miniCalPrevRect;
    private Rectangle _miniCalNextRect;
    private int _miniCalHoveredButton; // 0=none, 1=prev, 2=next
    private int _miniCalPressedButton; // 0=none, 1=prev, 2=next
    private readonly List<(DateTime date, Rectangle rect)> _miniCalCells = new();

    // Week/day appointment layout cache (rebuilt on each render)
    private readonly List<AppointmentLayout> _currentAppLayouts = new();

    // Scrollbar
    private readonly ScrollBarEngine _vScrollBar = new();
    private Rectangle _scrollBarBounds;
    private int _dayColWidth;

    // Month view grid positions (for drag hit-testing)
    private int _monthGridX;
    private int _monthGridY;
    private int _monthCellW;
    private int _monthCellH;

    // Appointment selection, drag, and resize state
    private CalendarAppointment? _selectedAppointment;
    private bool _allowEdit = true;
    private bool _isDragging;
    private bool _isResizing;
    private enum DragHandle { None, Top, Bottom, LeftEdge, RightEdge, Body }
    private DragHandle _dragHandle = DragHandle.None;
    private int _dragStartMouseX;
    private int _dragStartMouseY;
    private DateTime _dragOriginalStart;
    private DateTime _dragOriginalEnd;
    private int _dragOriginalScrollOffset;
    private DateTime? _dragTargetDate;
    private TimeSpan _dragOriginalDuration;
    private const int ResizeHandleHeight = 7;
    private const int ResizeHandleWidth = 7;

    #endregion

    #region Structs

    private struct AppointmentLayout
    {
        public CalendarAppointment Appointment;
        public Rectangle Bounds;
        public int Column;
        public int TotalColumns;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the current view type (Month, Week, or Day).
    /// </summary>
    public CalendarViewType ViewType
    {
        get => _viewType;
        set
        {
            if (_viewType == value) return;
            _viewType = value;
            UpdateCurrentDateForView();
            _scrollOffset = 0;
            Invalidate();
            OnViewChanged();
        }
    }

    /// <summary>
    /// Gets or sets the currently selected date.
    /// </summary>
    public DateTime SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (_selectedDate == value) return;
            _selectedDate = value.Date;
            Invalidate();
            OnDateSelected(new DateSelectedEventArgs(_selectedDate));
        }
    }

    /// <summary>
    /// Gets the list of appointments displayed in the calendar.
    /// </summary>
    public IList<CalendarAppointment> Appointments => _appointments;

    /// <summary>
    /// Gets or sets whether appointments can be moved, resized, or edited via the dialog.
    /// When false, the calendar is read-only. Default is true.
    /// </summary>
    public bool AllowEdit
    {
        get => _allowEdit;
        set
        {
            if (_allowEdit == value) return;
            _allowEdit = value;
            if (!_allowEdit)
            {
                _selectedAppointment = null;
                _isDragging = false;
                _isResizing = false;
            }
            Invalidate();
        }
    }

    /// <summary>
    /// Gets the currently selected (focused) appointment, or null if none.
    /// </summary>
    public CalendarAppointment? SelectedAppointment => _selectedAppointment;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of CalendarView.
    /// </summary>
    public CalendarView()
    {
        var now = DateTime.Today;
        _currentDate = new DateTime(now.Year, now.Month, 1);
        _selectedDate = now;
        Size = new Size(800, 500);
        TabStop = true;
        _backColor = ThemeManager.CurrentTheme.ControlBackground;

        _vScrollBar.SmallChange = SlotHeight;
        _vScrollBar.LargeChange = SlotHeight * 6;
        _vScrollBar.Scroll += (s, e) =>
        {
            _scrollOffset = _vScrollBar.Value;
            Invalidate();
        };
    }

    #endregion

    #region Theme

    /// <summary>
    /// Called when the theme changes.
    /// </summary>
    public override void OnThemeChanged(Theme newTheme)
    {
        if (!_backColorSet)
            _backColor = newTheme.ControlBackground;
        if (!_foreColorSet)
            _foreColor = newTheme.ControlText;
        Invalidate();
    }

    #endregion

    #region Navigation

    /// <summary>
    /// Navigates to the next period (month/week/day depending on view).
    /// </summary>
    public void GoToNext()
    {
        switch (_viewType)
        {
            case CalendarViewType.Month:
                _currentDate = _currentDate.AddMonths(1);
                break;
            case CalendarViewType.Week:
                _currentDate = _currentDate.AddDays(7);
                break;
            case CalendarViewType.Day:
                _currentDate = _currentDate.AddDays(1);
                break;
        }
        _scrollOffset = 0;
        Invalidate();
    }

    /// <summary>
    /// Navigates to the previous period (month/week/day depending on view).
    /// </summary>
    public void GoToPrevious()
    {
        switch (_viewType)
        {
            case CalendarViewType.Month:
                _currentDate = _currentDate.AddMonths(-1);
                break;
            case CalendarViewType.Week:
                _currentDate = _currentDate.AddDays(-7);
                break;
            case CalendarViewType.Day:
                _currentDate = _currentDate.AddDays(-1);
                break;
        }
        _scrollOffset = 0;
        Invalidate();
    }

    /// <summary>
    /// Navigates to today and selects it.
    /// </summary>
    public void GoToToday()
    {
        _selectedDate = DateTime.Today;
        switch (_viewType)
        {
            case CalendarViewType.Month:
                _currentDate = new DateTime(_selectedDate.Year, _selectedDate.Month, 1);
                break;
            case CalendarViewType.Week:
                _currentDate = StartOfWeek(_selectedDate);
                break;
            case CalendarViewType.Day:
                _currentDate = _selectedDate;
                break;
        }
        _scrollOffset = 0;
        Invalidate();
        OnDateSelected(new DateSelectedEventArgs(_selectedDate));
    }

    /// <summary>
    /// Opens the appointment dialog to create or edit an appointment.
    /// </summary>
    /// <param name="appointment">The appointment to edit, or null to create a new one.</param>
    /// <returns>The modified or newly created appointment, or null if cancelled.</returns>
    public CalendarAppointment? ShowAppointmentDialog(CalendarAppointment? appointment)
    {
        return AppointmentDialogOverlay.Show(this, appointment, _selectedDate);
    }

    #endregion

    #region Date Helpers

    private static DateTime StartOfWeek(DateTime date)
    {
        int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff).Date;
    }

    private static DateTime EndOfWeek(DateTime date)
    {
        return StartOfWeek(date).AddDays(6);
    }

    private static int DaysInMonth(DateTime date)
    {
        return DateTime.DaysInMonth(date.Year, date.Month);
    }

    private static int GetWeekColumn(DateTime date)
    {
        return (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
    }

    private void UpdateCurrentDateForView()
    {
        switch (_viewType)
        {
            case CalendarViewType.Month:
                _currentDate = new DateTime(_selectedDate.Year, _selectedDate.Month, 1);
                break;
            case CalendarViewType.Week:
                _currentDate = StartOfWeek(_selectedDate);
                break;
            case CalendarViewType.Day:
                _currentDate = _selectedDate;
                break;
        }
    }

    private string GetHeaderTitle()
    {
        var ci = CultureInfo.CurrentCulture;
        switch (_viewType)
        {
            case CalendarViewType.Month:
                return _currentDate.ToString("MMMM yyyy", ci);
            case CalendarViewType.Week:
            {
                var start = _currentDate;
                var end = start.AddDays(6);
                if (start.Month == end.Month)
                    return $"{start:MMMM} {start:dd} – {end:dd}, {start.Year}";
                return $"{start:MMM} {start:dd} – {end:MMM} {end:dd}, {end.Year}";
            }
            case CalendarViewType.Day:
                return _currentDate.ToString("dddd, MMMM dd, yyyy", ci);
            default:
                return string.Empty;
        }
    }

    #endregion

    #region Rendering

    /// <summary>
    /// Renders the calendar control.
    /// </summary>
    public override void Render(Graphics g)
    {
        if (!Visible) return;

        var theme = ThemeManager.CurrentTheme;
        g.FillRectangle(BackColor, 0, 0, Width, Height);

        _showMiniCal = Width >= 620;

        RenderHeader(g);

        int viewTop = HeaderHeight;
        int viewHeight = Height - HeaderHeight;
        int viewLeft = 0;
        int viewWidth = Width;

        if (_showMiniCal)
        {
            int miniCalH = _viewType == CalendarViewType.Month ? Height - HeaderHeight : Height - HeaderHeight;
            RenderMiniCalendar(g, 0, HeaderHeight, MiniCalWidth, miniCalH);

            if (_viewType == CalendarViewType.Month)
            {
                viewLeft = MiniCalWidth;
                viewWidth = Width - MiniCalWidth;
            }
        }
        else
        {
            _miniCalBounds = Rectangle.Empty;
            _miniCalCells.Clear();
        }

        switch (_viewType)
        {
            case CalendarViewType.Month:
                RenderMonthView(g, viewLeft, viewTop, viewWidth, viewHeight);
                break;
            case CalendarViewType.Week:
                RenderWeekView(g, viewLeft, viewTop, viewWidth, viewHeight);
                break;
            case CalendarViewType.Day:
                RenderDayView(g, viewLeft, viewTop, viewWidth, viewHeight);
                break;
        }
    }

    #region Header

    private void RenderHeader(Graphics g)
    {
        var theme = ThemeManager.CurrentTheme;
        var font = theme.DefaultFont;
        int y = 0;

        g.FillRectangle(theme.CalendarHeaderBackground, 0, y, Width, HeaderHeight);
        g.DrawLine(theme.Border, 0, HeaderHeight - 1, Width, HeaderHeight - 1, 1);

        int buttonY = (HeaderHeight - NavButtonHeight) / 2;
        int leftMargin = 8;

        // Previous button
        _prevBtnRect = new Rectangle(leftMargin, buttonY, NavButtonWidth, NavButtonHeight);
        DrawNavButton(g, _prevBtnRect, "\u25C0", false, theme);

        // Today button
        int todayW = 54;
        _todayBtnRect = new Rectangle(_prevBtnRect.Right + 2, buttonY, todayW, NavButtonHeight);
        DrawNavButton(g, _todayBtnRect, "Heute", false, theme);

        // Next button
        _nextBtnRect = new Rectangle(_todayBtnRect.Right + 2, buttonY, NavButtonWidth, NavButtonHeight);
        DrawNavButton(g, _nextBtnRect, "\u25B6", false, theme);

        // Title text centered between nav buttons and view tabs
        var title = GetHeaderTitle();
        var titleSize = g.MeasureString(title, font);

        // View tabs on right
        int viewTabY = (HeaderHeight - ViewTabHeight) / 2;
        int tabsRight = Width - 8;
        _dayTabRect = new Rectangle(tabsRight - ViewTabWidth, viewTabY, ViewTabWidth, ViewTabHeight);
        _weekTabRect = new Rectangle(_dayTabRect.Left - ViewTabWidth - 2, viewTabY, ViewTabWidth, ViewTabHeight);
        _monthTabRect = new Rectangle(_weekTabRect.Left - ViewTabWidth - 2, viewTabY, ViewTabWidth, ViewTabHeight);

        DrawViewTab(g, _monthTabRect, "Monat", _viewType == CalendarViewType.Month, theme);
        DrawViewTab(g, _weekTabRect, "Woche", _viewType == CalendarViewType.Week, theme);
        DrawViewTab(g, _dayTabRect, "Tag", _viewType == CalendarViewType.Day, theme);

        // Draw title centered in available space
        int titleLeft = _nextBtnRect.Right + 10;
        int titleRight = _monthTabRect.Left - 10;
        int titleCenterX = titleLeft + (titleRight - titleLeft) / 2;
        int titleX = titleCenterX - titleSize.width / 2;
        int titleY = (HeaderHeight - titleSize.height) / 2;
        g.DrawString(title, font, theme.ControlText, titleX, titleY);
    }

    private static void DrawNavButton(Graphics g, Rectangle rect, string text, bool isPressed, Theme theme)
    {
        var bg = isPressed ? theme.ButtonPressedBackground : theme.CalendarHeaderBackground;
        g.FillRectangle(bg, rect.X, rect.Y, rect.Width, rect.Height);
        g.DrawRectangle(theme.Border, rect.X, rect.Y, rect.Width, rect.Height, 1);

        var textSize = g.MeasureString(text, theme.DefaultFont);
        int tx = rect.X + (rect.Width - textSize.width) / 2;
        int ty = (int)CoordinateTransform.CenterVertically(rect.Y, rect.Height, theme.DefaultFont, g.Zoom) + 1;
        g.DrawString(text, theme.DefaultFont, theme.ControlText, tx < rect.X ? rect.X + 2 : tx, ty);
    }

    private static void DrawViewTab(Graphics g, Rectangle rect, string text, bool isSelected, Theme theme)
    {
        var bg = isSelected ? theme.ControlBackground : theme.CalendarHeaderBackground;
        var fg = isSelected ? theme.ControlText : theme.GrayText;
        g.FillRectangle(bg, rect.X, rect.Y, rect.Width, rect.Height);
        g.DrawRectangle(theme.Border, rect.X, rect.Y, rect.Width, rect.Height, 1);

        var textSize = g.MeasureString(text, theme.DefaultFont);
        int tx = rect.X + (rect.Width - textSize.width) / 2;
        int ty = (int)CoordinateTransform.CenterVertically(rect.Y, rect.Height, theme.DefaultFont, g.Zoom) + 1;
        g.DrawString(text, theme.DefaultFont, fg, tx < rect.X ? rect.X + 2 : tx, ty);
    }

    #endregion

    #region Mini Calendar

    private void RenderMiniCalendar(Graphics g, int x, int y, int w, int h)
    {
        var theme = ThemeManager.CurrentTheme;
        _miniCalBounds = new Rectangle(x + 6, y + 4, w - 12, h - 8);

        var bounds = _miniCalBounds;
        g.FillRectangle(theme.ControlBackground, bounds.X, bounds.Y, bounds.Width, bounds.Height);
        g.DrawRectangle(theme.Border, bounds.X, bounds.Y, bounds.Width, bounds.Height, 1);

        // Mini calendar header with month/year and prev/next
        int headerX = bounds.X;
        int headerY = bounds.Y;
        int headerW = bounds.Width;
        int headerH = MiniCalHeaderHeight;

        g.FillRectangle(theme.CalendarDayHeaderBackground, headerX, headerY, headerW, headerH);
        g.DrawLine(theme.Border, headerX, headerY + headerH, headerX + headerW, headerY + headerH, 1);

        var miniFont = theme.SmallFont;
        var miniTitle = _currentDate.ToString("MMM yyyy");
        var miniTitleSize = g.MeasureString(miniTitle, miniFont);
        int miniTitleX = headerX + (headerW - miniTitleSize.width) / 2;
        int miniTitleY = headerY + (headerH - miniTitleSize.height) / 2;
        g.DrawString(miniTitle, miniFont, theme.ControlText, miniTitleX, miniTitleY);

        int arrowBtnSize = 18;
        int arrowBtnPad = 3;
        _miniCalPrevRect = new Rectangle(headerX + arrowBtnPad, headerY + (headerH - arrowBtnSize) / 2, arrowBtnSize, arrowBtnSize);
        _miniCalNextRect = new Rectangle(headerX + headerW - arrowBtnSize - arrowBtnPad, headerY + (headerH - arrowBtnSize) / 2, arrowBtnSize, arrowBtnSize);

        DrawMiniCalArrowButton(g, _miniCalPrevRect, true,
            _miniCalPressedButton == 1, _miniCalHoveredButton == 1, theme);
        DrawMiniCalArrowButton(g, _miniCalNextRect, false,
            _miniCalPressedButton == 2, _miniCalHoveredButton == 2, theme);

        // Day headers
        int gridY = headerY + headerH + 2;
        int cellSize = Math.Min(MiniCalCellSize, (bounds.Width - 4) / 7);
        int gridX = bounds.X + 2;

        for (int i = 0; i < 7; i++)
        {
            int cx = gridX + i * cellSize;
            g.DrawString(DayNames[i], miniFont, theme.GrayText, cx + 2, gridY + 1);
        }

        // Day grid
        int firstDow = (7 + (new DateTime(_currentDate.Year, _currentDate.Month, 1).DayOfWeek - DayOfWeek.Monday)) % 7;
        int daysInMonth = DaysInMonth(_currentDate);
        var today = DateTime.Today;

        _miniCalCells.Clear();
        int startY = gridY + cellSize + 1;
        int dayNum = 1;
        bool started = false;

        for (int row = 0; row < 6; row++)
        {
            for (int col = 0; col < 7; col++)
            {
                if (!started && col < firstDow)
                    continue;

                if (!started)
                {
                    started = true;
                }

                if (dayNum > daysInMonth)
                    break;

                var date = new DateTime(_currentDate.Year, _currentDate.Month, dayNum);
                int cx = gridX + col * cellSize;
                int cy = startY + row * cellSize;
                var cellRect = new Rectangle(cx, cy, cellSize, cellSize);

                bool isToday = date == today;
                bool isSelected = date == _selectedDate;

                if (isToday)
                    g.FillRectangle(theme.CalendarTodayHighlight, cx, cy, cellSize, cellSize);
                if (isSelected)
                    g.DrawRectangle(theme.FocusIndicator, cx, cy, cellSize, cellSize, 1);

                g.DrawString(dayNum.ToString(), miniFont, theme.ControlText, cx + 3, cy + 2);
                _miniCalCells.Add((date, cellRect));

                dayNum++;
            }
            if (dayNum > daysInMonth)
                break;
        }
    }

    private static void DrawMiniCalArrowButton(Graphics g, Rectangle rect, bool isLeft,
        bool isPressed, bool isHovered, Theme theme)
    {
        var bg = theme.CalendarDayHeaderBackground;
        if (isPressed)
            bg = theme.ButtonPressedBackground;
        else if (isHovered)
            bg = theme.ButtonHoverBackground;

        g.FillRectangle(bg, rect.X, rect.Y, rect.Width, rect.Height);
        g.DrawRectangle(theme.Border, rect.X, rect.Y, rect.Width, rect.Height, 1);

        int cx = rect.X + rect.Width / 2;
        int cy = rect.Y + rect.Height / 2;
        int r = 4;
        var arrowColor = theme.ControlText;

        if (isLeft)
        {
            g.FillTriangle(arrowColor,
                cx + r, cy - r,
                cx + r, cy + r,
                cx - r, cy);
        }
        else
        {
            g.FillTriangle(arrowColor,
                cx - r, cy - r,
                cx - r, cy + r,
                cx + r, cy);
        }
    }

    #endregion

    #region Month View

    private void RenderMonthView(Graphics g, int x, int y, int w, int h)
    {
        var theme = ThemeManager.CurrentTheme;
        var font = theme.DefaultFont;
        var smallFont = theme.SmallFont;
        var today = DateTime.Today;

        // Day header row
        int dayHeaderH = MonthDayHeaderHeight;
        int gridY = y + dayHeaderH;
        int cellW = w / 7;
        int cellH = (h - dayHeaderH) / 6;
        int firstDow = (7 + (new DateTime(_currentDate.Year, _currentDate.Month, 1).DayOfWeek - DayOfWeek.Monday)) % 7;
        int daysInMonth = DaysInMonth(_currentDate);

        // Previous month days to fill first week
        int prevMonthDays = firstDow;
        var prevMonth = _currentDate.AddMonths(-1);
        int prevMonthDaysCount = DateTime.DaysInMonth(prevMonth.Year, prevMonth.Month);

        // Next month days to fill last week
        int totalCells = firstDow + daysInMonth;
        int nextMonthDays = (7 - (totalCells % 7)) % 7;
        if (totalCells + nextMonthDays > 42)
            nextMonthDays = 7 - nextMonthDays;

        // Draw day headers
        g.FillRectangle(theme.CalendarDayHeaderBackground, x, y, w, dayHeaderH);
        g.DrawLine(theme.Border, x, y + dayHeaderH, x + w, y + dayHeaderH, 1);

        for (int i = 0; i < 7; i++)
        {
            int cx = x + i * cellW;
            g.DrawLine(theme.GridLineVertical, cx + cellW - 1, y, cx + cellW - 1, y + h, 1);
            var daySize = g.MeasureString(DayNames[i], smallFont);
            int dx = cx + (cellW - daySize.width) / 2;
            g.DrawString(DayNames[i], smallFont, theme.ControlText, dx, y + (dayHeaderH - daySize.height) / 2);
        }

        // Draw cells
        _currentAppLayouts.Clear();
        int day = 1;
        int prevDay = prevMonthDaysCount - prevMonthDays + 1;
        int nextDay = 1;
        Rectangle selectedCellRect = Rectangle.Empty;

        for (int row = 0; row < 6; row++)
        {
            for (int col = 0; col < 7; col++)
            {
                int cellIndex = row * 7 + col;
                int cx = x + col * cellW;
                int cy = gridY + row * cellH;

                bool isCurrentMonth = cellIndex >= firstDow && day <= daysInMonth;
                DateTime cellDate;
                bool isToday = false;
                bool isSelected = false;
                Color cellBg;
                Color dayColor;

                if (isCurrentMonth)
                {
                    cellDate = new DateTime(_currentDate.Year, _currentDate.Month, day);
                    isToday = cellDate == today;
                    isSelected = cellDate == _selectedDate;
                    cellBg = (col >= 5) ? theme.CalendarWeekendBackground : theme.WindowBackground;
                    if (isToday)
                        cellBg = theme.CalendarTodayHighlight;
                    dayColor = theme.ControlText;
                    day++;
                }
                else if (cellIndex < firstDow)
                {
                    // Previous month
                    cellDate = new DateTime(prevMonth.Year, prevMonth.Month, prevDay);
                    cellBg = theme.ControlLight;
                    dayColor = theme.GrayText;
                    prevDay++;
                }
                else
                {
                    // Next month
                    int nextMonth = _currentDate.Month + 1;
                    int nextYear = _currentDate.Year;
                    if (nextMonth > 12) { nextMonth = 1; nextYear++; }
                    cellDate = new DateTime(nextYear, nextMonth, nextDay);
                    cellBg = theme.ControlLight;
                    dayColor = theme.GrayText;
                    nextDay++;
                }

                g.FillRectangle(cellBg, cx, cy, cellW, cellH);

                // Highlight drag target cell
                if (_isDragging && _dragTargetDate.HasValue && cellDate == _dragTargetDate.Value)
                    g.FillRectangle(theme.HoverHighlight, cx, cy, cellW, cellH);

                if (isSelected)
                    selectedCellRect = new Rectangle(cx, cy, cellW, cellH);

                // Day number
                int dayNumX = cx + 3;
                int dayNumY = cy + 2;
                g.DrawString(cellDate.Day.ToString(), smallFont, dayColor, dayNumX, dayNumY);

                // Appointments in cell
                var dayApps = _appointments
                    .Where(a => a.StartTime.Date <= cellDate && a.EndTime.Date >= cellDate && !a.IsAllDay)
                    .ToList();

                int appY = cy + DayHeaderHeight;

                var appFont = new Font(theme.DefaultFont.Name, 9f);

                int maxApps = Math.Min(dayApps.Count, MaxMonthAppointmentRows);
                for (int i = 0; i < maxApps; i++)
                {
                    var app = dayApps[i];
                    int barH = 16;
                    Color barColor = app.CategoryColor.A > 0 ? app.CategoryColor : theme.CalendarAppointmentDefault;
                    g.FillRectangle(barColor, cx + 2, appY, cellW - 4, barH);

                    var appText = TruncateText(g, app.Subject, appFont, cellW - 8);
                    g.DrawString(appText, appFont, Color.White, cx + 4, appY + 2);

                    _currentAppLayouts.Add(new AppointmentLayout
                    {
                        Appointment = app,
                        Bounds = new Rectangle(cx + 2, appY, cellW - 4, barH),
                        Column = 0,
                        TotalColumns = 1
                    });

                    appY += barH + 1;
                }

                if (dayApps.Count > MaxMonthAppointmentRows)
                {
                    string more = $"+{dayApps.Count - MaxMonthAppointmentRows} mehr";
                    g.DrawString(more, smallFont, theme.GrayText, cx + 3, appY);
                }
            }
        }

        // Draw grid lines on top of backgrounds but behind selection border
        for (int row = 0; row < 6; row++)
        {
            for (int col = 0; col < 7; col++)
            {
                int cx = x + col * cellW;
                int cy = gridY + row * cellH;
                g.DrawLine(theme.GridLineVertical, cx + cellW - 1, cy, cx + cellW - 1, cy + cellH, 1);
                g.DrawLine(theme.GridLine, cx, cy + cellH - 1, cx + cellW, cy + cellH - 1, 1);
            }
        }

        // Draw selection border on top of everything so grid lines don't cut it
        if (selectedCellRect.Width > 0)
        {
            g.DrawRectangle(theme.FocusIndicator,
                selectedCellRect.X, selectedCellRect.Y,
                selectedCellRect.Width, selectedCellRect.Height, 2);
        }

        // Draw focus border on selected appointment
        if (_selectedAppointment != null)
        {
            foreach (var layout in _currentAppLayouts)
            {
                if (layout.Appointment == _selectedAppointment)
                {
                    g.DrawRectangle(theme.FocusIndicator, layout.Bounds.X - 1, layout.Bounds.Y - 1,
                        layout.Bounds.Width + 2, layout.Bounds.Height + 2, 2);
                    break;
                }
            }
        }
    }

    #endregion

    #region Week View

    private void RenderWeekView(Graphics g, int x, int y, int w, int h)
    {
        var theme = ThemeManager.CurrentTheme;
        var font = theme.DefaultFont;
        var smallFont = theme.SmallFont;
        var today = DateTime.Today;

        _dayColWidth = (w - TimeRulerWidth) / 7;
        int dayColW = _dayColWidth;
        if (dayColW < 60) dayColW = 60;

        int totalContentH = 48 * SlotHeight;
        int viewportH = h;
        _maxScrollOffset = Math.Max(0, totalContentH - viewportH);
        _scrollOffset = Math.Clamp(_scrollOffset, 0, _maxScrollOffset);
        _vScrollBar.ContentSize = totalContentH;
        _vScrollBar.ViewSize = viewportH;
        _vScrollBar.Value = _scrollOffset;

        // Day headers with date
        int dayHeaderH = DayHeaderHeight;
        g.FillRectangle(theme.CalendarDayHeaderBackground, x, y, w, dayHeaderH);
        g.DrawLine(theme.Border, x, y + dayHeaderH, x + w, y + dayHeaderH, 1);

        for (int i = 0; i < 7; i++)
        {
            var day = _currentDate.AddDays(i);
            int cx = x + TimeRulerWidth + i * dayColW;
            bool isToday = day == today;

            if (isToday)
                g.FillRectangle(theme.CalendarTodayHighlight, cx, y, dayColW, dayHeaderH);

            var dayName = DayNames[i];
            var dayNameSize = g.MeasureString(dayName, smallFont);
            int dx = cx + (dayColW - dayNameSize.width) / 2;
            g.DrawString(dayName, smallFont, theme.ControlText, dx, y + 2);

            string dayNum = day.Day.ToString();
            var dayNumSize = g.MeasureString(dayNum, font);
            int dnX = cx + (dayColW - dayNumSize.width) / 2;
            g.DrawString(dayNum, font, theme.ControlText, dnX, y + dayNameSize.height + 2);

            g.DrawLine(theme.GridLineVertical, cx - 1, y, cx - 1, y + h, 1);
        }

        // Clear layout cache and populate with current render
        _currentAppLayouts.Clear();

        // All-day appointments area
        var allDayApps = new List<CalendarAppointment>();
        for (int i = 0; i < 7; i++)
        {
            var day = _currentDate.AddDays(i);
            allDayApps.AddRange(_appointments.Where(a => a.IsAllDay && a.StartTime.Date <= day && a.EndTime.Date >= day));
        }

        int allDayY = y + dayHeaderH;
        if (allDayApps.Count > 0)
        {
            g.FillRectangle(theme.ControlLight, x, allDayY, w, AllDayAreaHeight);
            g.DrawLine(theme.Border, x, allDayY + AllDayAreaHeight, x + w, allDayY + AllDayAreaHeight, 1);

            int adX = x + TimeRulerWidth + 4;
            foreach (var app in allDayApps)
            {
                var barColor = app.CategoryColor.A > 0 ? app.CategoryColor : theme.CalendarAppointmentDefault;
                var textSize = g.MeasureString(app.Subject, smallFont);
                int adW = Math.Min(textSize.width + 8, w - TimeRulerWidth - 8);
                g.FillRectangle(barColor, adX, allDayY + 3, adW, AllDayAreaHeight - 6);

                g.DrawString(app.Subject, smallFont, Color.White, adX + 4, allDayY + 3);
                _currentAppLayouts.Add(new AppointmentLayout
                {
                    Appointment = app,
                    Bounds = new Rectangle(adX, allDayY + 3, adW, AllDayAreaHeight - 6),
                    Column = 0,
                    TotalColumns = 1
                });
                adX += adW + 4;
            }

            dayHeaderH += AllDayAreaHeight;
        }

        // Time slots
        int timeTop = y + dayHeaderH - _scrollOffset;
        int slotH = SlotHeight;

        for (int slot = 0; slot < 48; slot++)
        {
            int slotY = timeTop + slot * slotH;
            if (slotY + slotH < y || slotY > y + h)
                continue;

            bool isWorkHour = slot >= 16 && slot < 40; // 8:00 - 20:00
            var slotBg = (slot % 2 == 0) ? theme.WindowBackground : theme.ControlLight;
            if (isWorkHour)
                slotBg = theme.CalendarWorkHoursBackground;

            g.FillRectangle(slotBg, x, slotY, w, slotH);

            // Time label
            if (slot % 2 == 0)
            {
                int hour = slot / 2;
                string timeLabel = $"{hour:D2}:00";
                var timeSize = g.MeasureString(timeLabel, smallFont);
                g.DrawString(timeLabel, smallFont, theme.CalendarTimeRulerText, x + 4, slotY + 2);
            }

            // Horizontal grid line
            g.DrawLine(theme.GridLine, x + TimeRulerWidth, slotY + slotH - 1, x + w, slotY + slotH - 1, 1);

            // Vertical day separators
            for (int i = 0; i < 7; i++)
            {
                int cx = x + TimeRulerWidth + i * dayColW;
                g.DrawLine(theme.GridLineVertical, cx - 1, slotY, cx - 1, slotY + slotH, 1);
            }
        }

        // Draw appointments in week view (keep all-day layouts already added above)
        int headerBottom = y + dayHeaderH;
        for (int i = 0; i < 7; i++)
        {
            var day = _currentDate.AddDays(i);
            int dayX = x + TimeRulerWidth + i * dayColW;
            var dayLayouts = LayoutDayAppointments(day, dayX, dayColW, timeTop, slotH);

            foreach (var layout in dayLayouts)
            {
                if (layout.Bounds.Y + layout.Bounds.Height < y || layout.Bounds.Y > y + h)
                    continue;

                // Clip appointment to below header
                int clipY = Math.Max(layout.Bounds.Y, headerBottom);
                int clipH = layout.Bounds.Height - (clipY - layout.Bounds.Y);
                if (clipH <= 0) continue;

                var barColor = layout.Appointment.CategoryColor.A > 0
                    ? layout.Appointment.CategoryColor
                    : theme.CalendarAppointmentDefault;
                g.FillRectangle(barColor, layout.Bounds.X, clipY, layout.Bounds.Width, clipH);

                if (clipY == layout.Bounds.Y)
                {
                    var appText = TruncateText(g, layout.Appointment.Subject, smallFont, layout.Bounds.Width - 4);
                    g.DrawString(appText, smallFont, Color.White, layout.Bounds.X + 2, clipY + 2);
                }

                var clippedLayout = new AppointmentLayout
                {
                    Appointment = layout.Appointment,
                    Bounds = new Rectangle(layout.Bounds.X, clipY, layout.Bounds.Width, clipH),
                    Column = layout.Column,
                    TotalColumns = layout.TotalColumns
                };
                _currentAppLayouts.Add(clippedLayout);
            }
        }

        // Draw focus border and resize handles on selected appointment
        if (_selectedAppointment != null)
        {
            DrawAppointmentFocusAndHandles(g, _currentAppLayouts, theme);
        }

        // Scrollbar
        _scrollBarBounds = new Rectangle(x + w - 14, y + dayHeaderH, 14, h - dayHeaderH);
        _vScrollBar.Render(g, _scrollBarBounds, theme);
    }

    #endregion

    #region Day View

    private void RenderDayView(Graphics g, int x, int y, int w, int h)
    {
        var theme = ThemeManager.CurrentTheme;
        var font = theme.DefaultFont;
        var smallFont = theme.SmallFont;
        var today = DateTime.Today;

        int dayColW = w - TimeRulerWidth;
        int totalContentH = 48 * SlotHeight;
        int viewportH = h;
        _maxScrollOffset = Math.Max(0, totalContentH - viewportH);
        _scrollOffset = Math.Clamp(_scrollOffset, 0, _maxScrollOffset);
        _vScrollBar.ContentSize = totalContentH;
        _vScrollBar.ViewSize = viewportH;
        _vScrollBar.Value = _scrollOffset;

        // Day header
        int dayHeaderH = DayHeaderHeight;
        g.FillRectangle(theme.CalendarDayHeaderBackground, x, y, w, dayHeaderH);
        g.DrawLine(theme.Border, x, y + dayHeaderH, x + w, y + dayHeaderH, 1);

        var day = _currentDate;
        bool isToday = day == today;
        if (isToday)
            g.FillRectangle(theme.CalendarTodayHighlight, x + TimeRulerWidth, y, dayColW, dayHeaderH);

        var dayName = day.ToString("dddd");
        var dayNameSize = g.MeasureString(dayName, smallFont);
        g.DrawString(dayName, smallFont, theme.ControlText, x + TimeRulerWidth + 4, y + 2);

        string dayStr = day.ToString("MMMM dd, yyyy");
        var dayStrSize = g.MeasureString(dayStr, font);
        g.DrawString(dayStr, font, theme.ControlText, x + TimeRulerWidth + 4, y + dayNameSize.height + 2);

        // Clear layout cache and populate with current render
        _currentAppLayouts.Clear();

        // All-day appointments
        var allDayApps = _appointments
            .Where(a => a.IsAllDay && a.StartTime.Date <= day && a.EndTime.Date >= day)
            .ToList();

        int allDayY = y + dayHeaderH;
        if (allDayApps.Count > 0)
        {
            g.FillRectangle(theme.ControlLight, x, allDayY, w, AllDayAreaHeight);
            g.DrawLine(theme.Border, x, allDayY + AllDayAreaHeight, x + w, allDayY + AllDayAreaHeight, 1);

            int adX = x + TimeRulerWidth + 4;
            foreach (var app in allDayApps)
            {
                var barColor = app.CategoryColor.A > 0 ? app.CategoryColor : theme.CalendarAppointmentDefault;
                int adW = Math.Min(g.MeasureString(app.Subject, smallFont).width + 8, dayColW - 8);
                g.FillRectangle(barColor, adX, allDayY + 3, adW, AllDayAreaHeight - 6);
                g.DrawString(app.Subject, smallFont, Color.White, adX + 4, allDayY + 3);

                _currentAppLayouts.Add(new AppointmentLayout
                {
                    Appointment = app,
                    Bounds = new Rectangle(adX, allDayY + 3, adW, AllDayAreaHeight - 6),
                    Column = 0,
                    TotalColumns = 1
                });
                adX += adW + 4;
            }

            dayHeaderH += AllDayAreaHeight;
        }

        // Time slots
        int timeTop = y + dayHeaderH - _scrollOffset;
        int slotH = SlotHeight;

        for (int slot = 0; slot < 48; slot++)
        {
            int slotY = timeTop + slot * slotH;
            if (slotY + slotH < y || slotY > y + h)
                continue;

            bool isWorkHour = slot >= 16 && slot < 40;
            var slotBg = (slot % 2 == 0) ? theme.WindowBackground : theme.ControlLight;
            if (isWorkHour)
                slotBg = theme.CalendarWorkHoursBackground;

            g.FillRectangle(slotBg, x, slotY, w, slotH);

            if (slot % 2 == 0)
            {
                int hour = slot / 2;
                string timeLabel = $"{hour:D2}:00";
                g.DrawString(timeLabel, smallFont, theme.CalendarTimeRulerText, x + 4, slotY + 2);
            }

            g.DrawLine(theme.GridLine, x + TimeRulerWidth, slotY + slotH - 1, x + w, slotY + slotH - 1, 1);
            g.DrawLine(theme.GridLineVertical, x + TimeRulerWidth - 1, slotY, x + TimeRulerWidth - 1, slotY + slotH, 1);
        }

        // Appointments for day (all-day already added above)
        var dayLayouts = LayoutDayAppointments(day, x + TimeRulerWidth, dayColW, timeTop, slotH);
        int dayHeaderBottom = y + dayHeaderH;

        foreach (var layout in dayLayouts)
        {
            if (layout.Bounds.Y + layout.Bounds.Height < y || layout.Bounds.Y > y + h)
                continue;

            int clipY = Math.Max(layout.Bounds.Y, dayHeaderBottom);
            int clipH = layout.Bounds.Height - (clipY - layout.Bounds.Y);
            if (clipH <= 0) continue;

            var barColor = layout.Appointment.CategoryColor.A > 0
                ? layout.Appointment.CategoryColor
                : theme.CalendarAppointmentDefault;
            g.FillRectangle(barColor, layout.Bounds.X, clipY, layout.Bounds.Width, clipH);

            if (clipY == layout.Bounds.Y)
            {
                var text = TruncateText(g, layout.Appointment.Subject, smallFont, layout.Bounds.Width - 4);
                g.DrawString(text, smallFont, Color.White, layout.Bounds.X + 2, clipY + 2);
            }

            var clippedLayout = new AppointmentLayout
            {
                Appointment = layout.Appointment,
                Bounds = new Rectangle(layout.Bounds.X, clipY, layout.Bounds.Width, clipH),
                Column = layout.Column,
                TotalColumns = layout.TotalColumns
            };
            _currentAppLayouts.Add(clippedLayout);
        }

        // Draw focus border and resize handles on selected appointment
        if (_selectedAppointment != null)
        {
            DrawAppointmentFocusAndHandles(g, _currentAppLayouts, theme);
        }

        // Scrollbar
        _scrollBarBounds = new Rectangle(x + w - 14, y + dayHeaderH, 14, h - dayHeaderH);
        _vScrollBar.Render(g, _scrollBarBounds, theme);
    }

    #endregion

    #region Appointment Layout Helpers

    private List<AppointmentLayout> LayoutDayAppointments(DateTime day, int dayLeft, int dayWidth, int timeTop, int slotH)
    {
        var result = new List<AppointmentLayout>();

        var dayApps = _appointments
            .Where(a => !a.IsAllDay && a.StartTime.Date <= day && a.EndTime.Date >= day)
            .OrderBy(a => a.StartTime)
            .ThenBy(a => a.EndTime)
            .ToList();

        if (dayApps.Count == 0)
            return result;

        // Assign columns to resolve overlaps (meeting rooms algorithm)
        var columns = new List<DateTime>(); // end times per column
        int[] colIndex = new int[dayApps.Count];

        for (int idx = 0; idx < dayApps.Count; idx++)
        {
            var app = dayApps[idx];
            int col = 0;
            for (; col < columns.Count; col++)
            {
                if (columns[col] <= app.StartTime)
                {
                    columns[col] = app.EndTime;
                    break;
                }
            }
            if (col >= columns.Count)
            {
                columns.Add(app.EndTime);
            }
            colIndex[idx] = col;
        }

        int totalCols = columns.Count;
        int colW = Math.Max(20, dayWidth / totalCols);

        for (int idx = 0; idx < dayApps.Count; idx++)
        {
            var app = dayApps[idx];
            double startM = Math.Max(0, (app.StartTime - day).TotalMinutes);
            double endM = Math.Min(24 * 60, (app.EndTime - day).TotalMinutes);

            // Snap to 15-minute grid
            int startSlot = (int)(startM / 30);
            int endSlot = (int)Math.Ceiling(endM / 30);
            if (endSlot <= startSlot) endSlot = startSlot + 1;

            int y = timeTop + startSlot * slotH;
            int h = (endSlot - startSlot) * slotH;
            int x = dayLeft + colIndex[idx] * colW;

            // Only show if at least partially visible
            result.Add(new AppointmentLayout
            {
                Appointment = app,
                Bounds = new Rectangle(x, y, colW, h),
                Column = colIndex[idx],
                TotalColumns = totalCols
            });
        }

        return result;
    }

    #endregion

    #region Helpers

    private static string TruncateText(Graphics g, string text, Font font, int maxWidth)
    {
        var size = g.MeasureString(text, font);
        if (size.width <= maxWidth)
            return text;

        for (int i = text.Length - 1; i > 0; i--)
        {
            var truncated = text[..i] + "\u2026";
            var ts = g.MeasureString(truncated, font);
            if (ts.width <= maxWidth)
                return truncated;
        }

        return string.Empty;
    }

    private void DrawAppointmentFocusAndHandles(Graphics g, List<AppointmentLayout> layouts, Theme theme)
    {
        // First pass: find all layout entries for this appointment
        AppointmentLayout? firstEntry = null;
        AppointmentLayout? lastEntry = null;
        int count = 0;
        foreach (var layout in layouts)
        {
            if (layout.Appointment != _selectedAppointment) continue;
            if (firstEntry == null) firstEntry = layout;
            lastEntry = layout;
            count++;
        }

        if (firstEntry == null) return;

        // Draw focus border on all segments
        var firstBounds = firstEntry.Value.Bounds;
        var lastBounds = lastEntry!.Value.Bounds;
        bool isMultiDay = count > 1;
        bool canEdit = _allowEdit && !_selectedAppointment!.IsReadOnly;

        foreach (var layout in layouts)
        {
            if (layout.Appointment != _selectedAppointment) continue;

            var r = layout.Bounds;
            g.DrawRectangle(theme.FocusIndicator, r.X - 1, r.Y - 1, r.Width + 2, r.Height + 2, 2);

            bool isFirst = r.X == firstBounds.X && r.Y == firstBounds.Y;
            bool isLast = r.X == lastBounds.X && r.Y == lastBounds.Y;

            // Top/bottom handles: only on first and last segments
            if (canEdit && (_viewType == CalendarViewType.Week || _viewType == CalendarViewType.Day) && r.Height >= ResizeHandleHeight * 3)
            {
                if (isFirst || !isMultiDay)
                {
                    int handleW = Math.Min(r.Width - 8, 40);
                    int handleX = r.X + (r.Width - handleW) / 2;
                    g.FillRectangle(theme.FocusIndicator, handleX, r.Y - 2, handleW, ResizeHandleHeight);
                }

                if (isLast || !isMultiDay)
                {
                    int handleW = Math.Min(r.Width - 8, 40);
                    int handleX = r.X + (r.Width - handleW) / 2;
                    g.FillRectangle(theme.FocusIndicator, handleX, r.Bottom - ResizeHandleHeight + 2, handleW, ResizeHandleHeight);
                }
            }

            // Left/right resize handles: left on first day, right on last day (week view)
            if (canEdit && _viewType == CalendarViewType.Week && r.Height >= ResizeHandleHeight * 3)
            {
                int handleH = Math.Min(r.Height - 8, 30);
                int handleY = r.Y + (r.Height - handleH) / 2;

                if (isFirst || !isMultiDay)
                    g.FillRectangle(theme.FocusIndicator, r.X - 3, handleY, ResizeHandleWidth, handleH);

                if (isLast || !isMultiDay)
                    g.FillRectangle(theme.FocusIndicator, r.Right - ResizeHandleWidth + 3, handleY, ResizeHandleWidth, handleH);
            }
        }
    }

    #endregion

    #endregion

    #region Hit Testing & Mouse/Keyboard

    private enum HitTarget
    {
        None,
        PrevButton,
        TodayButton,
        NextButton,
        MonthTab,
        WeekTab,
        DayTab,
        MiniCalDate,
        MiniCalPrev,
        MiniCalNext,
        MonthCell,
        Appointment,
        AppointmentResizeTop,
        AppointmentResizeBottom,
        AppointmentResizeLeft,
        AppointmentResizeRight
    }

    private (HitTarget target, object? data) CalendarHitTest(Point point)
    {
        // Header buttons
        if (_prevBtnRect.Contains(point)) return (HitTarget.PrevButton, null);
        if (_todayBtnRect.Contains(point)) return (HitTarget.TodayButton, null);
        if (_nextBtnRect.Contains(point)) return (HitTarget.NextButton, null);
        if (_monthTabRect.Contains(point)) return (HitTarget.MonthTab, null);
        if (_weekTabRect.Contains(point)) return (HitTarget.WeekTab, null);
        if (_dayTabRect.Contains(point)) return (HitTarget.DayTab, null);

        // Mini calendar
        if (_miniCalBounds.Contains(point))
        {
            if (_miniCalPrevRect.Contains(point)) return (HitTarget.MiniCalPrev, null);
            if (_miniCalNextRect.Contains(point)) return (HitTarget.MiniCalNext, null);

            foreach (var (date, rect) in _miniCalCells)
            {
                if (rect.Contains(point))
                    return (HitTarget.MiniCalDate, date);
            }
        }

        // Resize handles on selected appointment (week/day views only)
        if ((_viewType == CalendarViewType.Week || _viewType == CalendarViewType.Day) && _selectedAppointment != null)
        {
            foreach (var layout in _currentAppLayouts)
            {
                if (layout.Appointment != _selectedAppointment) continue;
                if (!_allowEdit || layout.Appointment.IsReadOnly) break;

                int topBotHandleW = Math.Min(layout.Bounds.Width - 8, 40);
                int topBotHandleX = layout.Bounds.X + (layout.Bounds.Width - topBotHandleW) / 2;

                var topRect = new Rectangle(topBotHandleX, layout.Bounds.Y - 2, topBotHandleW, ResizeHandleHeight + 2);
                var bottomRect = new Rectangle(topBotHandleX, layout.Bounds.Bottom - ResizeHandleHeight, topBotHandleW, ResizeHandleHeight + 2);

                if (topRect.Contains(point))
                    return (HitTarget.AppointmentResizeTop, layout.Appointment);
                if (bottomRect.Contains(point))
                    return (HitTarget.AppointmentResizeBottom, layout.Appointment);

                // Left/right handles only in week view and for tall enough appointments
                if (_viewType == CalendarViewType.Week && layout.Bounds.Height >= ResizeHandleHeight * 3)
                {
                    int leftRightHandleH = Math.Min(layout.Bounds.Height - 8, 30);
                    int leftRightHandleY = layout.Bounds.Y + (layout.Bounds.Height - leftRightHandleH) / 2;

                    var leftRect = new Rectangle(layout.Bounds.X - 3, leftRightHandleY, ResizeHandleWidth + 4, leftRightHandleH);
                    var rightRect = new Rectangle(layout.Bounds.Right - ResizeHandleWidth - 1, leftRightHandleY, ResizeHandleWidth + 4, leftRightHandleH);

                    if (leftRect.Contains(point))
                        return (HitTarget.AppointmentResizeLeft, layout.Appointment);
                    if (rightRect.Contains(point))
                        return (HitTarget.AppointmentResizeRight, layout.Appointment);
                }

                break;
            }
        }

        // Appointments (check first since they're on top)
        foreach (var layout in _currentAppLayouts)
        {
            if (layout.Bounds.Contains(point))
                return (HitTarget.Appointment, layout.Appointment);
        }

        // Month view cell hit testing
        if (_viewType == CalendarViewType.Month && point.Y >= HeaderHeight + MonthDayHeaderHeight)
        {
            int monthLeft = _showMiniCal ? MiniCalWidth : 0;
            int monthW = _showMiniCal ? Width - MiniCalWidth : Width;
            int relX = point.X - monthLeft;
            int relY = point.Y - HeaderHeight - MonthDayHeaderHeight;
            int cellW = monthW / 7;
            int cellH = (Height - HeaderHeight - MonthDayHeaderHeight) / 6;
            int col = relX / cellW;
            int row = relY / cellH;

            if (col >= 0 && col < 7 && row >= 0 && row < 6)
            {
                int firstDow = (7 + (new DateTime(_currentDate.Year, _currentDate.Month, 1).DayOfWeek - DayOfWeek.Monday)) % 7;
                int daysInMonth = DaysInMonth(_currentDate);
                int cellIndex = row * 7 + col;

                if (cellIndex >= firstDow && cellIndex - firstDow + 1 <= daysInMonth)
                {
                    int dayNum = cellIndex - firstDow + 1;
                    var date = new DateTime(_currentDate.Year, _currentDate.Month, dayNum);
                    return (HitTarget.MonthCell, date);
                }
            }
        }

        return (HitTarget.None, null);
    }

    /// <summary>
    /// Handles mouse down events for navigation and selection.
    /// </summary>
    protected internal override void OnMouseDown(EventArgs e)
    {
        if (e is not MouseEventArgs args) { base.OnMouseDown(e); return; }

        // Forward scrollbar clicks
        if (_scrollBarBounds.Width > 0 && _scrollBarBounds.Contains(args.X, args.Y))
        {
            _vScrollBar.HandleMouseDown(new Point(args.X, args.Y), _scrollBarBounds, ScrollBarCtx);
            return;
        }

        var point = new Point(args.X, args.Y);
        var (target, data) = CalendarHitTest(point);

        switch (target)
        {
            case HitTarget.PrevButton:
                GoToPrevious();
                return;
            case HitTarget.NextButton:
                GoToNext();
                return;
            case HitTarget.TodayButton:
                GoToToday();
                return;
            case HitTarget.MonthTab:
                ViewType = CalendarViewType.Month;
                return;
            case HitTarget.WeekTab:
                ViewType = CalendarViewType.Week;
                return;
            case HitTarget.DayTab:
                ViewType = CalendarViewType.Day;
                return;
            case HitTarget.MiniCalDate:
                if (data is DateTime miniDate)
                {
                    var oldSelected = _selectedDate;
                    _selectedDate = miniDate;

                    if (_viewType == CalendarViewType.Day)
                        _currentDate = miniDate;
                    else if (_viewType == CalendarViewType.Week)
                    {
                        var diff = (miniDate - _currentDate).Days;
                        int weekStart = (miniDate.DayOfWeek - DayOfWeek.Monday + 7) % 7;
                        _currentDate = miniDate.AddDays(-weekStart);
                    }
                    else
                    {
                        _currentDate = new DateTime(miniDate.Year, miniDate.Month, 1);
                    }

                    OnDateSelected(new DateSelectedEventArgs(miniDate));
                    Invalidate();
                }
                return;
            case HitTarget.MiniCalPrev:
            {
                _miniCalPressedButton = 1;
                _currentDate = _currentDate.AddMonths(-1);
                Invalidate();
                return;
            }
            case HitTarget.MiniCalNext:
            {
                _miniCalPressedButton = 2;
                _currentDate = _currentDate.AddMonths(1);
                Invalidate();
                return;
            }
            case HitTarget.AppointmentResizeTop:
            case HitTarget.AppointmentResizeBottom:
            case HitTarget.AppointmentResizeLeft:
            case HitTarget.AppointmentResizeRight:
                if (data is CalendarAppointment resizeApp && _allowEdit && !resizeApp.IsReadOnly)
                {
                    _selectedAppointment = resizeApp;
                    OnAppointmentSelected(new AppointmentSelectedEventArgs(resizeApp));
                    _isResizing = true;
                    if (target == HitTarget.AppointmentResizeTop) _dragHandle = DragHandle.Top;
                    else if (target == HitTarget.AppointmentResizeBottom) _dragHandle = DragHandle.Bottom;
                    else if (target == HitTarget.AppointmentResizeLeft) _dragHandle = DragHandle.LeftEdge;
                    else _dragHandle = DragHandle.RightEdge;
                    _dragStartMouseX = point.X;
                    _dragStartMouseY = point.Y;
                    _dragOriginalStart = resizeApp.StartTime;
                    _dragOriginalEnd = resizeApp.EndTime;
                    _dragOriginalDuration = resizeApp.EndTime - resizeApp.StartTime;
                    _dragOriginalScrollOffset = _scrollOffset;
                    Invalidate();
                }
                return;
            case HitTarget.MonthCell:
                if (data is DateTime cellDate)
                {
                    _selectedAppointment = null;
                    _selectedDate = cellDate;
                    OnDateSelected(new DateSelectedEventArgs(cellDate));
                    Invalidate();
                }
                return;
            case HitTarget.Appointment:
                if (data is CalendarAppointment app)
                {
                    _selectedAppointment = app;
                    OnAppointmentSelected(new AppointmentSelectedEventArgs(app));

                    // Start drag in week/day/month views if editable
                    if (_allowEdit && !app.IsReadOnly && !app.IsAllDay)
                    {
                        _isDragging = true;
                        _dragHandle = DragHandle.Body;
                        _dragStartMouseX = point.X;
                        _dragStartMouseY = point.Y;
                        _dragOriginalStart = app.StartTime;
                        _dragOriginalEnd = app.EndTime;
                        _dragOriginalDuration = app.EndTime - app.StartTime;
                        _dragOriginalScrollOffset = _scrollOffset;
                        _dragTargetDate = null;
                    }

                    // Double-click opens edit dialog
                    if (args.Clicks >= 2)
                    {
                        var result = ShowAppointmentDialog(app);
                        if (result != null)
                        {
                            int idx = _appointments.IndexOf(app);
                            if (idx >= 0)
                                _appointments[idx] = result;
                            _selectedAppointment = result;

                            var changedArgs = new CalendarAppointmentChangedEventArgs(result, "edit");
                            OnAppointmentChanged(changedArgs);
                            if (changedArgs.Cancel && idx >= 0)
                                _appointments[idx] = app;

                            Invalidate();
                        }
                    }
                }
                return;
        }

        // Click on empty space: clear appointment selection
        if (data == null || target == HitTarget.None)
        {
            _selectedAppointment = null;
        }

        // Click on empty area in week/day view: create new appointment at that time
        if (args.Clicks >= 2 && (_viewType == CalendarViewType.Week || _viewType == CalendarViewType.Day))
        {
            var newApp = new CalendarAppointment
            {
                StartTime = _selectedDate,
                EndTime = _selectedDate.AddHours(1),
                CategoryColor = CategoryColors[0]
            };
            var result = ShowAppointmentDialog(newApp);
            if (result != null)
            {
                _appointments.Add(result);
                Invalidate();
            }
            return;
        }

        // Click on empty area in month view: select day
        if (_viewType == CalendarViewType.Month && data is DateTime emptyDate)
        {
            _selectedAppointment = null;
            _selectedDate = emptyDate;
            OnDateSelected(new DateSelectedEventArgs(emptyDate));
            Invalidate();
            return;
        }

        base.OnMouseDown(e);
    }

    private CalendarViewScrollBarContext ScrollBarCtx => _scrollBarCtx ??= new CalendarViewScrollBarContext(this);
    private CalendarViewScrollBarContext? _scrollBarCtx;

    /// <summary>
    /// Handles mouse wheel scrolling in week/day views.
    /// </summary>
    protected internal override void OnMouseWheel(EventArgs e)
    {
        if (e is MouseEventArgs args && (_viewType == CalendarViewType.Week || _viewType == CalendarViewType.Day))
        {
            _vScrollBar.HandleMouseWheel(args.Delta, ScrollBarCtx);
            Invalidate();
            return;
        }
        base.OnMouseWheel(e);
    }

    /// <summary>
    /// Handles mouse up to clear pressed button states and finalize drag/resize.
    /// </summary>
    protected internal override void OnMouseUp(EventArgs e)
    {
        if (_miniCalPressedButton != 0)
        {
            _miniCalPressedButton = 0;
            Invalidate();
        }

        if (_isResizing || _isDragging)
        {
            var changeType = _isResizing ? "resize" : "move";
            var args = new CalendarAppointmentChangedEventArgs(_selectedAppointment!, changeType);
            OnAppointmentChanged(args);

            if (args.Cancel && _selectedAppointment != null)
            {
                _selectedAppointment.StartTime = _dragOriginalStart;
                _selectedAppointment.EndTime = _dragOriginalEnd;
            }

            _isDragging = false;
            _isResizing = false;
            _dragHandle = DragHandle.None;
            _dragTargetDate = null;
            Invalidate();
        }

        _vScrollBar.HandleMouseUp(ScrollBarCtx);
        base.OnMouseUp(e);
    }

    /// <summary>
    /// Handles mouse move for hover effects, drag/resize, and scrollbar interaction.
    /// </summary>
    protected internal override void OnMouseMove(EventArgs e)
    {
        if (e is MouseEventArgs args)
        {
            bool handled = false;

            if (_isDragging && _selectedAppointment != null)
            {
                if (_viewType == CalendarViewType.Month)
                {
                    // Month view: only change date, keep time and duration
                    int monthLeft = _showMiniCal ? MiniCalWidth : 0;
                    int monthW = _showMiniCal ? Width - MiniCalWidth : Width;
                    int monthGridX = monthLeft;
                    int monthGridY = HeaderHeight + MonthDayHeaderHeight;
                    int cellW = monthW / 7;
                    int cellH = (Height - HeaderHeight - MonthDayHeaderHeight) / 6;

                    int relX = args.X - monthGridX;
                    int relY = args.Y - monthGridY;
                    if (relX >= 0 && relY >= 0)
                    {
                        int col = relX / cellW;
                        int row = relY / cellH;
                        if (col >= 0 && col < 7 && row >= 0 && row < 6)
                        {
                            int firstDow = (7 + (new DateTime(_currentDate.Year, _currentDate.Month, 1).DayOfWeek - DayOfWeek.Monday)) % 7;
                            int daysInMonth = DateTime.DaysInMonth(_currentDate.Year, _currentDate.Month);
                            int cellIndex = row * 7 + col;

                            if (cellIndex >= firstDow && cellIndex - firstDow < daysInMonth)
                            {
                                int dayNum = cellIndex - firstDow + 1;
                                var targetDate = new DateTime(_currentDate.Year, _currentDate.Month, dayNum);

                                if (_dragTargetDate != targetDate)
                                {
                                    _dragTargetDate = targetDate;
                                    int dayOffset = (targetDate - _dragOriginalStart.Date).Days;
                                    _selectedAppointment.StartTime = _dragOriginalStart.Date.AddDays(dayOffset).Add(_dragOriginalStart.TimeOfDay);
                                    _selectedAppointment.EndTime = _selectedAppointment.StartTime.Add(_dragOriginalDuration);
                                    Invalidate();
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Week/day view: change by slots (vertical) and days (horizontal)
                    int deltaY = args.Y - _dragStartMouseY;
                    int deltaX = args.X - _dragStartMouseX;
                    int deltaSlots = (int)Math.Round((double)deltaY / SlotHeight);
                    int minutesDelta = deltaSlots * 30;
                    int dayDelta = (_viewType == CalendarViewType.Week && _dayColWidth > 0)
                        ? (int)Math.Round((double)deltaX / _dayColWidth) : 0;

                    var newStart = _dragOriginalStart.Date
                        .AddDays(dayDelta)
                        .Add(_dragOriginalStart.TimeOfDay)
                        .AddMinutes(minutesDelta);
                    var duration = _dragOriginalEnd - _dragOriginalStart;
                    _selectedAppointment.StartTime = newStart;
                    _selectedAppointment.EndTime = newStart.Add(duration);
                    Invalidate();
                }
                handled = true;
            }
            else if (_isResizing && _selectedAppointment != null)
            {
                int deltaY = args.Y - _dragStartMouseY;
                int deltaX = args.X - _dragStartMouseX;
                int deltaSlots = (int)Math.Round((double)deltaY / SlotHeight);
                int minutesDelta = deltaSlots * 30;
                int dayDelta = (_viewType == CalendarViewType.Week && _dayColWidth > 0
                    && (_dragHandle == DragHandle.LeftEdge || _dragHandle == DragHandle.RightEdge))
                    ? (int)Math.Round((double)deltaX / _dayColWidth) : 0;

                if (_dragHandle == DragHandle.Top)
                {
                    var newStart = _dragOriginalStart.AddMinutes(minutesDelta);
                    if (newStart < _dragOriginalEnd)
                        _selectedAppointment.StartTime = newStart;
                }
                else if (_dragHandle == DragHandle.Bottom)
                {
                    var newEnd = _dragOriginalEnd.AddMinutes(minutesDelta);
                    if (newEnd > _selectedAppointment.StartTime)
                        _selectedAppointment.EndTime = newEnd;
                }
                else if (_dragHandle == DragHandle.LeftEdge)
                {
                    var newStart = _dragOriginalStart.Date
                        .AddDays(dayDelta)
                        .Add(_dragOriginalStart.TimeOfDay)
                        .AddMinutes(minutesDelta);
                    if (newStart < _selectedAppointment.EndTime)
                        _selectedAppointment.StartTime = newStart;
                }
                else if (_dragHandle == DragHandle.RightEdge)
                {
                    var newEnd = _dragOriginalEnd.Date
                        .AddDays(dayDelta)
                        .Add(_dragOriginalEnd.TimeOfDay)
                        .AddMinutes(minutesDelta);
                    if (newEnd > _selectedAppointment.StartTime)
                        _selectedAppointment.EndTime = newEnd;
                }
                Invalidate();
                handled = true;
            }

            if (!handled)
            {
                // Cursor for resize handles
                var form = FindForm();
                bool overResizeNS = false;
                bool overResizeWE = false;
                if (_selectedAppointment != null && (_viewType == CalendarViewType.Week || _viewType == CalendarViewType.Day))
                {
                    foreach (var layout in _currentAppLayouts)
                    {
                        if (layout.Appointment != _selectedAppointment) continue;
                        if (!_allowEdit || layout.Appointment.IsReadOnly) break;

                        int tbHandleW = Math.Min(layout.Bounds.Width - 8, 40);
                        int tbHandleX = layout.Bounds.X + (layout.Bounds.Width - tbHandleW) / 2;

                        var topRect = new Rectangle(tbHandleX, layout.Bounds.Y - 3, tbHandleW, ResizeHandleHeight + 4);
                        var bottomRect = new Rectangle(tbHandleX, layout.Bounds.Bottom - ResizeHandleHeight - 1, tbHandleW, ResizeHandleHeight + 4);

                        if (topRect.Contains(args.X, args.Y) || bottomRect.Contains(args.X, args.Y))
                            overResizeNS = true;

                        if (_viewType == CalendarViewType.Week && layout.Bounds.Height >= ResizeHandleHeight * 3)
                        {
                            int lrHandleH = Math.Min(layout.Bounds.Height - 8, 30);
                            int lrHandleY = layout.Bounds.Y + (layout.Bounds.Height - lrHandleH) / 2;

                            var leftRect = new Rectangle(layout.Bounds.X - 3, lrHandleY, ResizeHandleWidth + 4, lrHandleH);
                            var rightRect = new Rectangle(layout.Bounds.Right - ResizeHandleWidth - 1, lrHandleY, ResizeHandleWidth + 4, lrHandleH);

                            if (leftRect.Contains(args.X, args.Y) || rightRect.Contains(args.X, args.Y))
                                overResizeWE = true;
                        }

                        break;
                    }
                }

                if (overResizeNS)
                {
                    if (form != null) form.Cursor = SystemCursorType.SizeNS;
                }
                else if (overResizeWE)
                {
                    if (form != null) form.Cursor = SystemCursorType.SizeWE;
                }
                else
                {
                    if (form != null && (form.Cursor == SystemCursorType.SizeNS || form.Cursor == SystemCursorType.SizeWE))
                        form.Cursor = null;
                }

                // Forward to scrollbar
                if (_scrollBarBounds.Width > 0 && _scrollBarBounds.Contains(args.X, args.Y))
                {
                    _vScrollBar.HandleMouseMove(new Point(args.X, args.Y), _scrollBarBounds, ScrollBarCtx);
                }

                // Mini calendar hover
                int newHover = 0;
                if (_miniCalBounds.Contains(args.X, args.Y))
                {
                    if (_miniCalPrevRect.Contains(args.X, args.Y))
                        newHover = 1;
                    else if (_miniCalNextRect.Contains(args.X, args.Y))
                        newHover = 2;
                }
                if (newHover != _miniCalHoveredButton)
                {
                    _miniCalHoveredButton = newHover;
                    Invalidate();
                }
            }
        }
        base.OnMouseMove(e);
    }

    /// <summary>
    /// Handles mouse leave to clear hover state.
    /// </summary>
    protected override void OnMouseLeave(EventArgs e)
    {
        if (_miniCalHoveredButton != 0)
        {
            _miniCalHoveredButton = 0;
            Invalidate();
        }
        var form = FindForm();
        if (form != null && (form.Cursor == SystemCursorType.SizeNS || form.Cursor == SystemCursorType.SizeWE))
            form.Cursor = null;
        _vScrollBar.HandleMouseLeave(ScrollBarCtx);
        base.OnMouseLeave(e);
    }

    /// <summary>
    /// Handles keyboard navigation.
    /// </summary>
    protected internal override void OnKeyDown(KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Left:
                if (_viewType == CalendarViewType.Day || _viewType == CalendarViewType.Week)
                    GoToPrevious();
                else
                {
                    _selectedDate = _selectedDate.AddDays(-1);
                    if (_selectedDate.Month != _currentDate.Month)
                        _currentDate = new DateTime(_selectedDate.Year, _selectedDate.Month, 1);
                    OnDateSelected(new DateSelectedEventArgs(_selectedDate));
                    Invalidate();
                }
                e.Handled = true;
                return;
            case Keys.Right:
                if (_viewType == CalendarViewType.Day || _viewType == CalendarViewType.Week)
                    GoToNext();
                else
                {
                    _selectedDate = _selectedDate.AddDays(1);
                    if (_selectedDate.Month != _currentDate.Month)
                        _currentDate = new DateTime(_selectedDate.Year, _selectedDate.Month, 1);
                    OnDateSelected(new DateSelectedEventArgs(_selectedDate));
                    Invalidate();
                }
                e.Handled = true;
                return;
            case Keys.Up:
                if (_viewType == CalendarViewType.Day || _viewType == CalendarViewType.Week)
                {
                    _scrollOffset = Math.Max(0, _scrollOffset - SlotHeight * 2);
                    _vScrollBar.Value = _scrollOffset;
                    Invalidate();
                }
                e.Handled = true;
                return;
            case Keys.Down:
                if (_viewType == CalendarViewType.Day || _viewType == CalendarViewType.Week)
                {
                    _scrollOffset = Math.Min(_maxScrollOffset, _scrollOffset + SlotHeight * 2);
                    _vScrollBar.Value = _scrollOffset;
                    Invalidate();
                }
                e.Handled = true;
                return;
            case Keys.Home:
                _selectedDate = DateTime.Today;
                GoToToday();
                e.Handled = true;
                return;
            case Keys.M:
                ViewType = CalendarViewType.Month;
                e.Handled = true;
                return;
            case Keys.W:
                ViewType = CalendarViewType.Week;
                e.Handled = true;
                return;
            case Keys.D:
                ViewType = CalendarViewType.Day;
                e.Handled = true;
                return;
            case Keys.N:
                if ((e.Modifiers & ModifierKeys.Control) != 0)
                {
                    var newApp = new CalendarAppointment
                    {
                        StartTime = _selectedDate.AddHours(9),
                        EndTime = _selectedDate.AddHours(10),
            CategoryColor = CalendarView.CategoryColors[0]
                    };
                    var result = ShowAppointmentDialog(newApp);
                    if (result != null)
                    {
                        _appointments.Add(result);
                        Invalidate();
                    }
                    e.Handled = true;
                    return;
                }
                break;
            case Keys.Delete:
            {
                // Find and delete selected appointment via hit test area (focus-based)
                var hitApp = _currentAppLayouts.Find(l =>
                    l.Appointment.StartTime.Date == _selectedDate);
                if (hitApp.Appointment != null)
                {
                    _appointments.Remove(hitApp.Appointment);
                    Invalidate();
                    e.Handled = true;
                }
                return;
            }
        }

        base.OnKeyDown(e);
    }

    #endregion

    #region Events

    /// <summary>
    /// Raised when the selected date changes.
    /// </summary>
    public event EventHandler<DateSelectedEventArgs>? DateSelected;

    /// <summary>
    /// Raised when an appointment is clicked.
    /// </summary>
    public event EventHandler<AppointmentSelectedEventArgs>? AppointmentSelected;

    /// <summary>
    /// Raised when the view type changes.
    /// </summary>
    public event EventHandler? ViewChanged;

    /// <summary>
    /// Raises the DateSelected event.
    /// </summary>
    protected virtual void OnDateSelected(DateSelectedEventArgs e)
    {
        DateSelected?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the AppointmentSelected event.
    /// </summary>
    protected virtual void OnAppointmentSelected(AppointmentSelectedEventArgs e)
    {
        AppointmentSelected?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the ViewChanged event.
    /// </summary>
    protected virtual void OnViewChanged()
    {
        ViewChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Raised when an appointment has been moved, resized, or edited.
    /// Set e.Cancel = true to revert the change.
    /// </summary>
    public event EventHandler<CalendarAppointmentChangedEventArgs>? AppointmentChanged;

    /// <summary>
    /// Raises the AppointmentChanged event.
    /// </summary>
    /// <param name="e">The event data. If e.Cancel is set to true by a handler, the change is reverted.</param>
    protected virtual void OnAppointmentChanged(CalendarAppointmentChangedEventArgs e)
    {
        AppointmentChanged?.Invoke(this, e);
    }
    #endregion
}

#region ScrollBar Context

internal sealed class CalendarViewScrollBarContext : IScrollBarContext
{
    private readonly CalendarView _owner;

    public CalendarViewScrollBarContext(CalendarView owner) => _owner = owner;

    public float Zoom => _owner.EffectiveZoom;

    public void Invalidate() => _owner.Invalidate();

    public void CaptureMouse(bool capture) => _owner.CapturingMouse = capture;
}

#endregion

#region Appointment Dialog Overlay

/// <summary>
/// Internal overlay dialog for creating or editing calendar appointments.
/// </summary>
internal class AppointmentDialogOverlay : ContainerControl
{
    private readonly Form _owner;
    private readonly CalendarAppointment _appointment;
    private readonly bool _isNew;
    private DialogResult _dialogResult = DialogResult.None;

    private readonly TextBox _subjectBox;
    private readonly TextBox _locationBox;
    private readonly TextBox _startDateBox;
    private readonly ComboBox _startTimeBox;
    private readonly TextBox _endDateBox;
    private readonly ComboBox _endTimeBox;
    private readonly CheckBox _allDayCheck;
    private readonly List<Button> _colorButtons = new();
    private Color _selectedColor;
    private readonly Button _okButton;
    private readonly Button _cancelButton;

    private int _dialogX;
    private int _dialogY;
    private int _dialogW;
    private int _dialogH;

    /// <summary>
    /// Gets the dialog result.
    /// </summary>
    public DialogResult DialogResult => _dialogResult;

    /// <summary>
    /// Gets the edited or newly created appointment.
    /// </summary>
    public CalendarAppointment Appointment => _appointment;

    private AppointmentDialogOverlay(Form owner, CalendarAppointment appointment, bool isNew)
    {
        _owner = owner;
        _appointment = appointment;
        _isNew = isNew;
        _selectedColor = appointment.CategoryColor.A > 0 ? appointment.CategoryColor : Color.FromArgb(0, 120, 215);

        Bounds = new Rectangle(0, 0, owner.Width, owner.Height);
        Visible = true;
        Enabled = true;
        TabStop = true;

        _backColor = ThemeManager.CurrentTheme.ControlBackground;
        _foreColor = ThemeManager.CurrentTheme.ControlText;

        // Create child controls
        var font = ThemeManager.CurrentTheme.DefaultFont;
        int y = 30;
        int col1 = 80;
        int col2 = 200;
        int ctrlH = 28;
        int gap = 6;
        int margin = 16;

        // Subject
        AddLabel(LangRes.GetString("AppointmentSubject") + ":", margin, y + 4, col1, ctrlH);
        _subjectBox = new TextBox
        {
            Text = appointment.Subject,
            Bounds = new Rectangle(margin + col1, y, col2, ctrlH),
            TabStop = true
        };
        Controls.Add(_subjectBox);
        y += ctrlH + gap;

        // Location
        AddLabel(LangRes.GetString("AppointmentLocation") + ":", margin, y + 4, col1, ctrlH);
        _locationBox = new TextBox
        {
            Text = appointment.Location ?? string.Empty,
            Bounds = new Rectangle(margin + col1, y, col2, ctrlH),
            TabStop = true
        };
        Controls.Add(_locationBox);
        y += ctrlH + gap;

        // Start date
        AddLabel(LangRes.GetString("AppointmentStart") + ":", margin, y + 4, col1, ctrlH);
        _startDateBox = new TextBox
        {
            Text = appointment.StartTime.ToString("yyyy-MM-dd"),
            Bounds = new Rectangle(margin + col1, y, 100, ctrlH),
            TabStop = true
        };
        Controls.Add(_startDateBox);

        _startTimeBox = new ComboBox();
        _startTimeBox.Items.Add("00:00"); // ensure at least one item
        _startTimeBox.Items.Clear();
        for (int i = 0; i < 48; i++)
        {
            int h = i / 2;
            int m = (i % 2) * 30;
            _startTimeBox.Items.Add($"{h:D2}:{m:D2}");
        }
        _startTimeBox.Bounds = new Rectangle(margin + col1 + 106, y, 80, ctrlH);
        _startTimeBox.Text = appointment.StartTime.ToString("HH:mm");
        _startTimeBox.TabStop = true;
        Controls.Add(_startTimeBox);
        y += ctrlH + gap;

        // End date
        AddLabel(LangRes.GetString("AppointmentEnd") + ":", margin, y + 4, col1, ctrlH);
        _endDateBox = new TextBox
        {
            Text = appointment.EndTime.ToString("yyyy-MM-dd"),
            Bounds = new Rectangle(margin + col1, y, 100, ctrlH),
            TabStop = true
        };
        Controls.Add(_endDateBox);

        _endTimeBox = new ComboBox();
        _endTimeBox.Items.Add("00:00"); // ensure at least one item
        _endTimeBox.Items.Clear();
        for (int i = 0; i < 48; i++)
        {
            int h = i / 2;
            int m = (i % 2) * 30;
            _endTimeBox.Items.Add($"{h:D2}:{m:D2}");
        }
        _endTimeBox.Bounds = new Rectangle(margin + col1 + 106, y, 80, ctrlH);
        _endTimeBox.Text = appointment.EndTime.ToString("HH:mm");
        _endTimeBox.TabStop = true;
        Controls.Add(_endTimeBox);
        y += ctrlH + gap;

        // All-day
        _allDayCheck = new CheckBox
        {
            Text = LangRes.GetString("AppointmentAllDay"),
            Checked = appointment.IsAllDay,
            Bounds = new Rectangle(margin + col1, y, 140, ctrlH),
            TabStop = true
        };
        Controls.Add(_allDayCheck);
        y += ctrlH + gap + 4;

        // Color selection
        AddLabel(LangRes.GetString("AppointmentColor") + ":", margin, y + 4, col1, ctrlH);
        int colorBtnSize = 22;
        int colorGap = 4;
        var colors = CalendarView.CategoryColors;
        for (int i = 0; i < colors.Length; i++)
        {
            var color = colors[i];
            int idx = i;
            int bx = margin + col1 + i * (colorBtnSize + colorGap);
            var btn = new Button
            {
                Text = string.Empty,
                Bounds = new Rectangle(bx, y, colorBtnSize, colorBtnSize),
                BackColor = color,
                TabStop = false
            };
            btn.Click += (s, ev) =>
            {
                _selectedColor = color;
                UpdateColorButtonHighlights();
            };
            _colorButtons.Add(btn);
            Controls.Add(btn);
        }
        _selectedColor = appointment.CategoryColor.A > 0 ? appointment.CategoryColor : colors[0];

        y += colorBtnSize + gap + 8;

        // Buttons
        int btnW = 90;
        int btnGap = 10;
        int btnY = y;

        _okButton = new Button
        {
            Text = LangRes.GetString("OK"),
            Bounds = new Rectangle(margin + col1 + col2 - 2 * btnW - btnGap, btnY, btnW, 28),
            TabStop = true
        };
        _okButton.Click += (s, ev) => ConfirmDialog();
        Controls.Add(_okButton);

        _cancelButton = new Button
        {
            Text = LangRes.GetString("Cancel"),
            Bounds = new Rectangle(margin + col1 + col2 - btnW, btnY, btnW, 28),
            TabStop = true
        };
        _cancelButton.Click += (s, ev) =>
        {
            _dialogResult = DialogResult.Cancel;
        };
        Controls.Add(_cancelButton);

        y += 28 + margin;

        // Dialog dimensions
        _dialogW = Math.Max(320, margin + col1 + col2 + margin);
        _dialogH = Math.Max(200, y);
        _dialogX = (owner.Width - _dialogW) / 2;
        _dialogY = (owner.Height - _dialogH) / 2;

        UpdateColorButtonHighlights();
    }

    private void AddLabel(string text, int x, int y, int w, int h)
    {
        var lbl = new Label
        {
            Text = text,
            Bounds = new Rectangle(x, y, w, h),
            TabStop = false
        };
        Controls.Add(lbl);
    }

    private void UpdateColorButtonHighlights()
    {
        // Selection is tracked internally via _selectedColor;
        // buttons are colored squares that the user clicks to pick a color.
    }

    private void ConfirmDialog()
    {
        // Parse values
        if (!DateTime.TryParse(_startDateBox.Text, out var startDate))
            startDate = _appointment.StartTime.Date;

        if (!DateTime.TryParse(_endDateBox.Text, out var endDate))
            endDate = _appointment.EndTime.Date;

        var startTimeStr = _startTimeBox.Text;
        var endTimeStr = _endTimeBox.Text;

        if (TimeSpan.TryParse(startTimeStr, out var startTs))
            startDate = startDate.Date + startTs;

        if (TimeSpan.TryParse(endTimeStr, out var endTs))
            endDate = endDate.Date + endTs;

        if (_allDayCheck.Checked)
        {
            startDate = startDate.Date;
            endDate = endDate.Date.AddDays(1);
        }

        _appointment.Subject = _subjectBox.Text;
        _appointment.StartTime = startDate;
        _appointment.EndTime = endDate;
        _appointment.Location = _locationBox.Text;
        _appointment.IsAllDay = _allDayCheck.Checked;
        _appointment.CategoryColor = _selectedColor;

        _dialogResult = DialogResult.OK;
    }

    /// <summary>
    /// Shows the appointment dialog as a modal overlay.
    /// </summary>
    /// <param name="owner">The CalendarView owner.</param>
    /// <param name="appointment">The appointment to edit, or null to create a new one.</param>
    /// <param name="defaultDate">The default date for a new appointment.</param>
    /// <returns>The modified appointment, or null if cancelled.</returns>
    public static CalendarAppointment? Show(CalendarView owner, CalendarAppointment? appointment, DateTime defaultDate)
    {
        var form = owner.FindForm();
        if (form == null) return null;

        bool isNew = appointment == null;
        var app = appointment ?? new CalendarAppointment
        {
            Subject = "Neuer Termin",
            StartTime = defaultDate.AddHours(9),
            EndTime = defaultDate.AddHours(10),
            CategoryColor = CalendarView.CategoryColors[0]
        };

        var overlay = new AppointmentDialogOverlay(form, app, isNew);
        form.Controls.Add(overlay);
        form.PerformLayout();

        try
        {
            // Set focus to subject box
            overlay._subjectBox.Focused = true;
            overlay._owner.ActiveControl = overlay._subjectBox;

            while (overlay._dialogResult == DialogResult.None)
            {
                Platform.Platform.ProcessEvents(Application.Instance);
            }

            return overlay._dialogResult == DialogResult.OK ? overlay._appointment : null;
        }
        finally
        {
            form.Controls.Remove(overlay);
            form.PerformLayout();
        }
    }

    /// <summary>
    /// Renders the overlay background and dialog box.
    /// </summary>
    public override void Render(Graphics g)
    {
        if (!Visible) return;

        if (Bounds.Width != _owner.Width || Bounds.Height != _owner.Height)
        {
            Bounds = new Rectangle(0, 0, _owner.Width, _owner.Height);
        }

        var theme = ThemeManager.CurrentTheme;

        // Dimming overlay
        g.FillRectangle(theme.MessageBoxOverlay, 0, 0, _owner.Width, _owner.Height);

        // Dialog box
        g.FillRectangle(theme.ControlBackground, _dialogX, _dialogY, _dialogW, _dialogH);
        g.DrawRectangle(theme.MessageBoxBorder, _dialogX, _dialogY, _dialogW, _dialogH, 2);

        // Title bar
        int titleBarH = 28;
        g.FillRectangle(theme.ActiveCaption, _dialogX, _dialogY, _dialogW, titleBarH);
        var titleFont = theme.DefaultFont;
        string title = _isNew ? "Neuer Termin" : "Termin bearbeiten";
        g.DrawString(title, titleFont, theme.ActiveCaptionText, _dialogX + 10, _dialogY + 5);

        base.Render(g);
    }
}

#endregion
