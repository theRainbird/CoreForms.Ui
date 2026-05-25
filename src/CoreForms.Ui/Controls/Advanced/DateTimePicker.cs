using System.ComponentModel;
using System.Globalization;
using CoreForms.Ui.Core;
using CoreForms.Ui.Theming;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Advanced;

/// <summary>
/// Represents a Windows date/time picker control that allows the user to select a date, a time,
/// or both, depending on the <see cref="Format"/> property.
/// Supports data binding via the <see cref="Value"/> and <see cref="Checked"/> properties.
/// </summary>
public class DateTimePicker : Control
{
    #region Constants

    private const int DefaultControlHeight = 32;
    private const int DropDownButtonWidth = 17;
    private const int CheckBoxSize = 13;
    private const int CheckBoxMargin = 5;

    // Calendar dropdown
    private const int CalHeaderHeight = 28;
    private const int CalDayHeaderHeight = 20;
    private const int CalCellWidth = 28;
    private const int CalCellHeight = 20;
    private const int CalTodayButtonHeight = 24;
    private const int CalPadding = 4;

    // Time dropdown
    private const int TimeItemHeight = 24;
    private const int TimeColumnWidth = 60;
    private const int TimeColPadding = 8;
    private const int TimeHeaderHeight = 24;

    private static readonly string[] DayNameHeaders = CreateDayHeaders();

    private static string[] CreateDayHeaders()
    {
        var ci = CultureInfo.CurrentCulture;
        var names = new string[7];
        for (int i = 0; i < 7; i++)
        {
            int dayIndex = (int)DayOfWeek.Monday + i;
            if (dayIndex > 6) dayIndex -= 7;
            names[i] = ci.DateTimeFormat.GetDayName((DayOfWeek)dayIndex).Substring(0, 2);
        }
        return names;
    }

    #endregion

    #region Fields

    private DateTime _value;
    private DateTime _minDate = DateTime.MinValue;
    private DateTime _maxDate = DateTime.MaxValue;
    private DateTimePickerFormat _format = DateTimePickerFormat.Short;
    private string? _customFormat;
    private bool _showUpDown;
    private bool _showCheckBox;
    private bool _checked = true;
    private bool _droppedDown;
    private int _dropDownWidth;

    // Calendar state
    private DateTime _viewDate;
    private DateTime? _hoveredDate;
    private Rectangle _calPrevBtnRect;
    private Rectangle _calNextBtnRect;
    private Rectangle _calTodayBtnRect;
    private readonly List<(DateTime date, Rectangle rect)> _calCells = new();
    private int _calGridLeft;
    private int _calGridTop;

    // Time picker state
    private int _hourScrollOffset;
    private int _minuteScrollOffset;
    private int _hoveredHour = -1;
    private int _hoveredMinute = -1;
    private int _activeTimeColumn; // 0 = hour, 1 = minute
    private readonly ScrollBarEngine _hourScrollBar = new();
    private readonly ScrollBarEngine _minuteScrollBar = new();

    // Spinner field navigation (ShowUpDown mode)
    private enum DateFieldType { Day, Month, Year, Hour, Minute, Second, AmPm }
    private DateFieldType _activeField = DateFieldType.Day;
    private readonly List<(DateFieldType type, float x, float width)> _fieldMeasurements = new();

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the date/time value assigned to the control.
    /// Data bindings can be established on this property.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is outside the <see cref="MinDate"/> or <see cref="MaxDate"/> range.</exception>
    [Bindable(true)]
    public DateTime Value
    {
        get => _value;
        set
        {
            if (_value == value) return;
            if (value < _minDate || value > _maxDate)
                throw new ArgumentOutOfRangeException(nameof(Value), value, "Value is outside the valid date range.");
            _value = value;
            OnValueChanged();
            OnPropertyChanged(nameof(Value));
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the minimum date that can be selected.
    /// </summary>
    public DateTime MinDate
    {
        get => _minDate;
        set
        {
            if (_minDate == value) return;
            _minDate = value;
            if (_value < _minDate)
                Value = _minDate;
            OnPropertyChanged(nameof(MinDate));
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the maximum date that can be selected.
    /// </summary>
    public DateTime MaxDate
    {
        get => _maxDate;
        set
        {
            if (_maxDate == value) return;
            _maxDate = value;
            if (_value > _maxDate)
                Value = _maxDate;
            OnPropertyChanged(nameof(MaxDate));
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the format of the date and time displayed in the control.
    /// </summary>
    public DateTimePickerFormat Format
    {
        get => _format;
        set
        {
            if (_format == value) return;
            _format = value;
            // Update default value for Time mode
            if (_format == DateTimePickerFormat.Time && _value == DateTime.MinValue)
                _value = DateTime.Now;
            else if (_format != DateTimePickerFormat.Time && _value.TimeOfDay == TimeSpan.Zero == false)
                _value = _value.Date;
            AdjustActiveFieldForFormat();
            OnPropertyChanged(nameof(Format));
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the custom format string used when <see cref="Format"/> is <see cref="DateTimePickerFormat.Custom"/>.
    /// </summary>
    public string? CustomFormat
    {
        get => _customFormat;
        set
        {
            if (_customFormat == value) return;
            _customFormat = value;
            OnPropertyChanged(nameof(CustomFormat));
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets whether the control uses up/down buttons instead of a drop-down calendar.
    /// </summary>
    public bool ShowUpDown
    {
        get => _showUpDown;
        set
        {
            if (_showUpDown == value) return;
            _showUpDown = value;
            if (_droppedDown) CloseDropDown();
            OnPropertyChanged(nameof(ShowUpDown));
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets whether a check box is displayed to the left of the formatted value.
    /// When <see cref="Checked"/> is false, the value is displayed grayed out.
    /// </summary>
    public bool ShowCheckBox
    {
        get => _showCheckBox;
        set
        {
            if (_showCheckBox == value) return;
            _showCheckBox = value;
            OnPropertyChanged(nameof(ShowCheckBox));
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets whether the <see cref="Value"/> is considered set. When false, the value is
    /// displayed in a disabled style. Data bindings can be established on this property.
    /// </summary>
    [Bindable(true)]
    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value) return;
            _checked = value;
            OnCheckedChanged();
            OnPropertyChanged(nameof(Checked));
            Invalidate();
        }
    }

    /// <summary>
    /// Gets the formatted text representation of the current <see cref="Value"/> according to the
    /// active <see cref="Format"/> and <see cref="CustomFormat"/>.
    /// </summary>
    public new string Text => GetFormattedValue();

    /// <summary>
    /// Gets or sets the width of the drop-down calendar/time picker in pixels.
    /// When set to 0 or less, the width is calculated automatically.
    /// </summary>
    public int DropDownWidth
    {
        get => _dropDownWidth;
        set
        {
            if (_dropDownWidth == value) return;
            _dropDownWidth = value;
            OnPropertyChanged(nameof(DropDownWidth));
            Invalidate();
        }
    }

    #endregion

    #region Events

    /// <summary>
    /// Occurs when the <see cref="Value"/> property changes.
    /// </summary>
    public event EventHandler? ValueChanged;

    /// <summary>
    /// Occurs when the <see cref="Checked"/> property changes.
    /// </summary>
    public event EventHandler? CheckedChanged;

    /// <summary>
    /// Occurs when the drop-down calendar or time picker is shown.
    /// </summary>
    public event EventHandler? DropDown;

    /// <summary>
    /// Occurs when the drop-down calendar or time picker is closed.
    /// </summary>
    public event EventHandler? CloseUp;

    /// <summary>
    /// Raises the <see cref="ValueChanged"/> event.
    /// </summary>
    protected virtual void OnValueChanged()
    {
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Raises the <see cref="CheckedChanged"/> event.
    /// </summary>
    protected virtual void OnCheckedChanged()
    {
        CheckedChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Raises the <see cref="DropDown"/> event.
    /// </summary>
    protected virtual void OnDropDown()
    {
        DropDown?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Raises the <see cref="CloseUp"/> event.
    /// </summary>
    protected virtual void OnCloseUp()
    {
        CloseUp?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Constructor & Theme

    /// <summary>
    /// Initializes a new instance of DateTimePicker.
    /// </summary>
    public DateTimePicker()
    {
        var now = DateTime.Now;
        _value = now.Date;
        _viewDate = new DateTime(_value.Year, _value.Month, 1);
        Size = new Size(200, DefaultControlHeight);
        TabStop = true;
        _backColor = ThemeManager.CurrentTheme.TextBoxBackground;
        _foreColor = ThemeManager.CurrentTheme.TextBoxText;
        _activeField = DateFieldType.Day;

        _hourScrollBar.Scroll += (s, e) =>
        {
            _hourScrollOffset = _hourScrollBar.Value;
            Invalidate();
        };
        _minuteScrollBar.Scroll += (s, e) =>
        {
            _minuteScrollOffset = _minuteScrollBar.Value;
            Invalidate();
        };
    }

    /// <summary>
    /// Called when the theme changes.
    /// </summary>
    public override void OnThemeChanged(Theme newTheme)
    {
        if (!_backColorSet)
            _backColor = newTheme.TextBoxBackground;
        if (!_foreColorSet)
            _foreColor = newTheme.TextBoxText;
        Invalidate();
    }

    #endregion

    #region Rendering

    /// <summary>
    /// Renders the DateTimePicker control.
    /// </summary>
    public override void Render(Graphics g)
    {
        if (!Visible) return;

        var theme = ThemeManager.CurrentTheme;
        g.FillRectangle(BackColor, 0, 0, Width, Height);

        if (Focused)
            g.DrawRectangle(theme.TextBoxFocusBorder, 0, 0, Width, Height, 2);
        else
            g.DrawRectangle(theme.TextBoxBorder, 0, 0, Width, Height, 1);

        if (Enabled)
            DrawFocusIndicator(g);

        RenderMainArea(g, theme);

        base.Render(g);
    }

    private void RenderMainArea(Graphics g, Theme theme)
    {
        var font = EffectiveFont;
        float zoom = EffectiveZoom;
        float scaledFontSize = font.Size * zoom;
        float textY = CoordinateTransform.CenterVertically(Height, font, zoom);

        int textLeft = TextLeftEdge();
        int textRight = _showUpDown ? Width - DropDownButtonWidth : Width - DropDownButtonWidth;

        int textWidth = textRight - textLeft - 2;
        if (textWidth < 0) textWidth = 0;

        float textX = textLeft;

        g.SetClip(new Rectangle(textLeft, 0, textWidth, Height));

        var textColor = Enabled && _checked ? ForeColor : theme.GrayText;
        string displayText = GetFormattedValue();
        g.DrawString(displayText, font, textColor, textX, textY);

        // Draw active field underline in ShowUpDown mode
        if (_showUpDown && Focused && Enabled && _checked)
        {
            MeasureFields(g, textX, textY);
            foreach (var (ft, fx, fw) in _fieldMeasurements)
            {
                if (ft == _activeField)
                {
                    float underlineY = textY + font.Size * EffectiveZoom + 2;
                    g.DrawLine(theme.FocusIndicator, fx, underlineY, fx + fw, underlineY, 2);
                    break;
                }
            }
        }

        g.ResetClip();

        // Checkbox
        if (_showCheckBox)
        {
            int cbX = CheckBoxMargin;
            int cbY = (Height - CheckBoxSize) / 2;
            int cbSize = CheckBoxSize;

            g.DrawRectangle(theme.TextBoxBorder, cbX, cbY, cbSize, cbSize, 1);
            if (_checked)
            {
                // Draw a checkmark
                g.DrawLine(theme.ControlText, cbX + 2, cbY + cbSize / 2, cbX + cbSize / 2, cbY + cbSize - 2, 2);
                g.DrawLine(theme.ControlText, cbX + cbSize / 2, cbY + cbSize - 2, cbX + cbSize - 2, cbY + 2, 2);
            }
        }

        // Dropdown or up/down button
        if (_showUpDown)
            RenderUpDownButtons(g, theme);
        else
            RenderDropDownButton(g, theme);
    }

    private int TextLeftEdge()
    {
        int left = 3;
        if (_showCheckBox)
            left += CheckBoxSize + CheckBoxMargin + 2;
        return left;
    }

    private void RenderDropDownButton(Graphics g, Theme theme)
    {
        int btnX = Width - DropDownButtonWidth;
        int btnY = 1;
        int btnH = Height - 2;

        g.FillRectangle(theme.ControlBackground, btnX, btnY, DropDownButtonWidth, btnH);
        g.DrawLine(theme.ComboBoxDropdownButtonSeparator, btnX, 0, btnX, Height);

        int cx = btnX + DropDownButtonWidth / 2;
        int cy = Height / 2;
        int tw = 4;
        int th = 3;
        g.FillTriangle(theme.ComboBoxDropdownArrow,
            cx - tw, cy - th,
            cx + tw, cy - th,
            cx, cy + th);
    }

    private void RenderUpDownButtons(Graphics g, Theme theme)
    {
        int btnX = Width - DropDownButtonWidth;
        int halfH = Height / 2;

        g.FillRectangle(theme.ControlBackground, btnX, 0, DropDownButtonWidth, Height);
        g.DrawLine(theme.ComboBoxDropdownButtonSeparator, btnX, 0, btnX, Height);
        g.DrawLine(theme.ComboBoxDropdownButtonSeparator, btnX, halfH, Width, halfH);

        // Up arrow
        {
            int cx = btnX + DropDownButtonWidth / 2;
            int cy = halfH / 2;
            int tw = 4;
            int th = 3;
            g.FillTriangle(theme.ComboBoxDropdownArrow,
                cx - tw, cy + th,
                cx + tw, cy + th,
                cx, cy - th);
        }

        // Down arrow
        {
            int cx = btnX + DropDownButtonWidth / 2;
            int cy = halfH + halfH / 2;
            int tw = 4;
            int th = 3;
            g.FillTriangle(theme.ComboBoxDropdownArrow,
                cx - tw, cy - th,
                cx + tw, cy - th,
                cx, cy + th);
        }
    }

    // ---- Dropdown overlay rendering ----

    /// <summary>
    /// Renders the drop-down calendar or time picker overlay when the dropdown is open.
    /// </summary>
    public override void RenderOverlay(Graphics g)
    {
        if (!Visible || !_droppedDown) return;

        base.RenderOverlay(g);

        var theme = ThemeManager.CurrentTheme;
        bool hasDate = ShowCalendarInDropdown();
        bool hasTime = ShowTimeInDropdown();

        if (hasDate && !hasTime)
        {
            RenderCalendarOnlyDropdown(g, theme);
        }
        else if (hasTime && !hasDate)
        {
            RenderTimeOnlyDropdown(g, theme);
        }
        else
        {
            RenderCombinedDropdown(g, theme);
        }
    }

    private bool ShowCalendarInDropdown()
    {
        if (_showUpDown) return false;
        return _format == DateTimePickerFormat.Long || _format == DateTimePickerFormat.Short ||
               (_format == DateTimePickerFormat.Custom);
    }

    private bool ShowTimeInDropdown()
    {
        if (_showUpDown) return false;
        return _format == DateTimePickerFormat.Time ||
               (_format == DateTimePickerFormat.Custom);
    }

    private void RenderCalendarOnlyDropdown(Graphics g, Theme theme)
    {
        int ddW = CalcDropdownWidth();
        int ddH = CalcDropdownHeight();
        int dropY = Height;

        g.FillRectangle(theme.MenuDropdownBackground, 0, dropY, ddW, ddH);
        g.DrawRectangle(theme.MenuDropdownBorder, 0, dropY, ddW, ddH, 1);

        int calX = CalPadding;
        int calY = dropY + CalPadding;

        RenderCalendarHeader(g, theme, calX, calY, ddW - CalPadding * 2);
        RenderCalendarGrid(g, theme, calX, calY + CalHeaderHeight, ddW - CalPadding * 2);
        RenderTodayButton(g, theme, calX, calY + CalHeaderHeight + CalDayHeaderHeight + 6 * CalCellHeight,
            ddW - CalPadding * 2);
    }

    private void RenderTimeOnlyDropdown(Graphics g, Theme theme)
    {
        int ddW = CalcDropdownWidth();
        int ddH = CalcDropdownHeight();
        int dropY = Height;

        g.FillRectangle(theme.MenuDropdownBackground, 0, dropY, ddW, ddH);
        g.DrawRectangle(theme.MenuDropdownBorder, 0, dropY, ddW, ddH, 1);

        int timeX = TimeColPadding;
        int timeY = dropY + TimeColPadding;

        RenderTimeHeader(g, theme, timeX, timeY, ddW - TimeColPadding * 2);
        RenderTimeColumns(g, theme, timeX, timeY + TimeHeaderHeight, ddW - TimeColPadding * 2,
            ddH - TimeColPadding - TimeHeaderHeight);
    }

    private void RenderCombinedDropdown(Graphics g, Theme theme)
    {
        int calW = CalPadding * 2 + 7 * CalCellWidth;
        int timeW = CalcTimeWidth();
        int ddW = calW + timeW + 4;
        int ddH = CalcDropdownHeight();
        int dropY = Height;

        g.FillRectangle(theme.MenuDropdownBackground, 0, dropY, ddW, ddH);
        g.DrawRectangle(theme.MenuDropdownBorder, 0, dropY, ddW, ddH, 1);

        // Calendar on left
        int calX = CalPadding;
        int calY = dropY + CalPadding;
        RenderCalendarHeader(g, theme, calX, calY, calW - CalPadding * 2);
        RenderCalendarGrid(g, theme, calX, calY + CalHeaderHeight, calW - CalPadding * 2);

        // Time picker on right
        int timeX = calW + 2;
        int timeY = dropY + TimeColPadding;
        RenderTimeHeader(g, theme, timeX + TimeColPadding, timeY, timeW);
        RenderTimeColumns(g, theme, timeX + TimeColPadding, timeY + TimeHeaderHeight, timeW,
            ddH - TimeColPadding - TimeHeaderHeight);
    }

    private void RenderCalendarHeader(Graphics g, Theme theme, int x, int y, int w)
    {
        var font = EffectiveFont;
        string title = _viewDate.ToString("MMMM yyyy", CultureInfo.CurrentCulture);

        // Prev button
        int btnSize = CalHeaderHeight - 4;
        _calPrevBtnRect = new Rectangle(x, y + 2, btnSize, btnSize);
        int cx = _calPrevBtnRect.X + _calPrevBtnRect.Width / 2;
        int cy = _calPrevBtnRect.Y + _calPrevBtnRect.Height / 2;
        g.FillTriangle(theme.ControlText,
            cx, cy - 3, cx, cy + 3, cx - 4, cy);

        // Next button
        _calNextBtnRect = new Rectangle(x + w - btnSize, y + 2, btnSize, btnSize);
        cx = _calNextBtnRect.X + _calNextBtnRect.Width / 2;
        cy = _calNextBtnRect.Y + _calNextBtnRect.Height / 2;
        g.FillTriangle(theme.ControlText,
            cx, cy - 3, cx, cy + 3, cx + 4, cy);

        // Title
        var titleSize = g.MeasureString(title, font);
        float titleX = x + (w - titleSize.width) / 2;
        float titleY = CoordinateTransform.CenterVertically(y, CalHeaderHeight, font, EffectiveZoom);
        g.DrawString(title, font, theme.ControlText, titleX, titleY);
    }

    private void RenderCalendarGrid(Graphics g, Theme theme, int x, int y, int w)
    {
        _calCells.Clear();
        _calGridLeft = x;
        _calGridTop = y;

        var ci = CultureInfo.CurrentCulture;
        float zoom = EffectiveZoom;
        var font = EffectiveFont;

        // Day headers
        int firstDOW = (int)ci.DateTimeFormat.FirstDayOfWeek;
        for (int i = 0; i < 7; i++)
        {
            int dayIndex = (firstDOW + i) % 7;
            string name = ci.DateTimeFormat.GetAbbreviatedDayName((DayOfWeek)dayIndex);
            float nx = x + i * CalCellWidth + (CalCellWidth - g.MeasureString(name, font).width) / 2;
            float ny = CoordinateTransform.CenterVertically(y, CalDayHeaderHeight, font, zoom);
            g.DrawString(name, font, theme.GrayText, nx, ny);
        }

        y += CalDayHeaderHeight;

        // Determine first day of month and number of days
        int daysInMonth = DateTime.DaysInMonth(_viewDate.Year, _viewDate.Month);
        DateTime firstOfMonth = new DateTime(_viewDate.Year, _viewDate.Month, 1);
        int startCol = ((int)firstOfMonth.DayOfWeek - firstDOW + 7) % 7;

        DateTime today = DateTime.Today;

        for (int day = 1; day <= daysInMonth; day++)
        {
            int row = (startCol + day - 1) / 7;
            int col = (startCol + day - 1) % 7;

            int cellX = x + col * CalCellWidth;
            int cellY = y + row * CalCellHeight;
            var cellRect = new Rectangle(cellX, cellY, CalCellWidth, CalCellHeight);

            DateTime date = new DateTime(_viewDate.Year, _viewDate.Month, day);
            _calCells.Add((date, cellRect));

            bool isToday = date == today;
            bool isSelected = date == _value.Date;
            bool isHovered = _hoveredDate == date;

            if (isSelected)
            {
                g.FillRectangle(theme.Highlight, cellX + 1, cellY + 1, CalCellWidth - 2, CalCellHeight - 2);
            }
            else if (isHovered)
            {
                g.FillRectangle(theme.HoverHighlight, cellX + 1, cellY + 1, CalCellWidth - 2, CalCellHeight - 2);
            }

            var textColor = isSelected
                ? theme.HighlightText
                : theme.ControlText;

            if (isToday && !isSelected)
            {
                g.FillRectangle(theme.CalendarTodayHighlight, cellX + 1, cellY + 1, CalCellWidth - 2, CalCellHeight - 2);
            }

            string dayText = day.ToString();
            var daySize = g.MeasureString(dayText, font);
            float dx = cellX + (CalCellWidth - daySize.width) / 2;
            float dy = CoordinateTransform.CenterVertically(cellY, CalCellHeight, font, zoom);
            g.DrawString(dayText, font, textColor, dx, dy);

            if (isToday && !isSelected)
            {
                g.DrawRectangle(theme.FocusIndicator, cellX + 1, cellY + 1, CalCellWidth - 2, CalCellHeight - 2, 1);
            }
        }
    }

    private void RenderTodayButton(Graphics g, Theme theme, int x, int y, int w)
    {
        _calTodayBtnRect = new Rectangle(x, y, w, CalTodayButtonHeight);

        g.DrawLine(theme.Border, x, y, x + w, y, 1);

        var font = EffectiveFont;
        string today = LangRes.GetString("Today") ?? "Today";
        float zoom = EffectiveZoom;
        var size = g.MeasureString(today, font);
        float tx = x + (w - size.width) / 2;
        float ty = CoordinateTransform.CenterVertically(y, CalTodayButtonHeight, font, zoom);
        g.DrawString(today, font, theme.ControlText, tx, ty);
    }

    private void RenderTimeHeader(Graphics g, Theme theme, int x, int y, int w)
    {
        var font = EffectiveFont;
        float zoom = EffectiveZoom;

        int colW = (w - 8 - TimeColPadding) / 2;
        string hourLabel = LangRes.GetString("Hour") ?? "Hour";
        string minLabel = LangRes.GetString("Minute") ?? "Minute";

        var hSize = g.MeasureString(hourLabel, font);
        var mSize = g.MeasureString(minLabel, font);

        float hx = x;
        float hy = CoordinateTransform.CenterVertically(y, TimeHeaderHeight, font, zoom);
        g.DrawString(hourLabel, font, theme.ControlText, hx, hy);

        float mx = x + colW + TimeColPadding + (colW - mSize.width) / 2;
        float my = hy;
        g.DrawString(minLabel, font, theme.ControlText, mx, my);
    }

    private void RenderTimeColumns(Graphics g, Theme theme, int x, int y, int w, int h)
    {
        int colW = (w - TimeColPadding) / 2;
        int hourColEndX = x + colW;
        int minColX = x + colW + TimeColPadding;

        // Separator
        int sepX = hourColEndX + TimeColPadding / 2;
        int sepY = 2;
        g.DrawLine(theme.Border, sepX, y - sepY, sepX, y + h + sepY, 1);

        // Hours
        RenderTimeColumn(g, theme, x, y, colW, h, true);
        // Minutes
        RenderTimeColumn(g, theme, minColX, y, colW, h, false);
    }

    private void RenderTimeColumn(Graphics g, Theme theme, int x, int y, int w, int h, bool isHour)
    {
        var font = EffectiveFont;
        float zoom = EffectiveZoom;
        int count = isHour ? 24 : 60;
        int scrollOff = isHour ? _hourScrollOffset : _minuteScrollOffset;
        var scrollBar = isHour ? _hourScrollBar : _minuteScrollBar;

        g.SetClip(new Rectangle(x, y, w, h));

        for (int i = 0; i < count; i++)
        {
            int itemY = y + i * TimeItemHeight - scrollOff;
            if (itemY + TimeItemHeight <= y) continue;
            if (itemY >= y + h) break;

            string label = isHour ? i.ToString("D2") : i.ToString("D2");
            int currentVal = isHour ? _value.Hour : _value.Minute;
            int hovered = isHour ? _hoveredHour : _hoveredMinute;

            bool isSelected = i == currentVal;
            bool isHovered = i == hovered;

            if (isSelected)
            {
                g.FillRectangle(theme.Highlight, x, itemY, w, TimeItemHeight);
            }
            else if (isHovered)
            {
                g.FillRectangle(theme.HoverHighlight, x, itemY, w, TimeItemHeight);
            }

            var textColor = isSelected ? theme.HighlightText : theme.ControlText;
            var size = g.MeasureString(label, font);
            float lx = x + (w - size.width) / 2;
            float ly = CoordinateTransform.CenterVertically(itemY, TimeItemHeight, font, zoom);
            g.DrawString(label, font, textColor, lx, ly);
        }

        g.ResetClip();

        // Scrollbar
        int totalContent = count * TimeItemHeight;
        if (totalContent > h)
        {
            scrollBar.SmallChange = TimeItemHeight;
            scrollBar.LargeChange = TimeItemHeight * 5;
            scrollBar.ViewSize = h;
            scrollBar.ContentSize = totalContent;
            scrollBar.Render(g, new Rectangle(x + w - ScrollBarEngine.DefaultScrollBarSize, y,
                ScrollBarEngine.DefaultScrollBarSize, h), theme);
        }
        else
        {
            // Reset scrollbar state when not needed
            scrollBar.ViewSize = h;
            scrollBar.ContentSize = totalContent;
        }
    }

    #endregion

    #region Mouse Handling

    /// <summary>
    /// Raises the MouseDown event.
    /// </summary>
    protected internal override void OnMouseDown(EventArgs e)
    {
        if (!Enabled) return;

        Focused = true;

        var args = e as MouseEventArgs;
        if (args == null) return;

        if (_droppedDown)
        {
            HandleDropDownMouseDown(args);
            return;
        }

        // Checkbox click
        if (_showCheckBox)
        {
            int cbX = CheckBoxMargin;
            int cbY = (Height - CheckBoxSize) / 2;
            var cbRect = new Rectangle(cbX, cbY, CheckBoxSize, CheckBoxSize);
            if (cbRect.Contains(args.X, args.Y))
            {
                Checked = !_checked;
                return;
            }
        }

        // Dropdown button click
        if (!_showUpDown && args.X >= Width - DropDownButtonWidth)
        {
            _droppedDown = true;
            CapturingMouse = true;
            InitializeDropdownState();
            OnDropDown();
            Invalidate();
            return;
        }

        // Up/Down button click
        if (_showUpDown && args.X >= Width - DropDownButtonWidth)
        {
            if (args.Y < Height / 2)
                IncrementValue();
            else
                DecrementValue();
            return;
        }

        // Click on text area in ShowUpDown mode → select field
        if (_showUpDown && args.X < Width - DropDownButtonWidth)
        {
            int textLeft = TextLeftEdge();
            if (args.X >= textLeft)
            {
                float cx = args.X;
                foreach (var (ft, fx, fw) in _fieldMeasurements)
                {
                    if (cx >= fx && cx <= fx + fw)
                    {
                        _activeField = ft;
                        Invalidate();
                        return;
                    }
                }
            }
            return;
        }

        // Click on text area - open dropdown if not ShowUpDown
        if (!_showUpDown && !_droppedDown)
        {
            _droppedDown = true;
            CapturingMouse = true;
            InitializeDropdownState();
            OnDropDown();
            Invalidate();
        }
    }

    /// <summary>
    /// Raises the MouseUp event.
    /// </summary>
    protected internal override void OnMouseUp(EventArgs e)
    {
        if (!_droppedDown && CapturingMouse)
        {
            CapturingMouse = false;
            return;
        }

        if (_hourScrollBar.IsDragging || _hourScrollBar.IsUpButtonPressed || _hourScrollBar.IsDownButtonPressed)
        {
            _hourScrollBar.HandleMouseUp(GetHourScrollBarCtx());
            return;
        }
        if (_minuteScrollBar.IsDragging || _minuteScrollBar.IsUpButtonPressed || _minuteScrollBar.IsDownButtonPressed)
        {
            _minuteScrollBar.HandleMouseUp(GetMinuteScrollBarCtx());
            return;
        }

        base.OnMouseUp(e);
    }

    /// <summary>
    /// Raises the MouseMove event.
    /// </summary>
    protected internal override void OnMouseMove(EventArgs e)
    {
        if (!Enabled) return;

        var args = e as MouseEventArgs;
        if (args == null) return;

        if (_droppedDown)
        {
            HandleDropDownMouseMove(args);
            return;
        }

        base.OnMouseMove(e);
    }

    /// <summary>
    /// Raises the MouseWheel event.
    /// </summary>
    protected internal override void OnMouseWheel(EventArgs e)
    {
        if (!Enabled) return;

        var args = e as MouseEventArgs;
        if (args == null) return;

        if (_droppedDown)
        {
            // Scroll the dropdown (calendar month or time column)
            if (ShowTimeInDropdown() && !ShowCalendarInDropdown())
            {
                // Time-only dropdown
                int scrollDelta = (int)args.Delta;
                if (_activeTimeColumn == 0)
                {
                    int maxScroll = Math.Max(0, 24 * TimeItemHeight - CalcTimeColumnHeight());
                    _hourScrollOffset = Math.Clamp(_hourScrollOffset - scrollDelta, 0, maxScroll);
                    _hourScrollBar.Value = _hourScrollOffset;
                }
                else
                {
                    int maxScroll = Math.Max(0, 60 * TimeItemHeight - CalcTimeColumnHeight());
                    _minuteScrollOffset = Math.Clamp(_minuteScrollOffset - scrollDelta, 0, maxScroll);
                    _minuteScrollBar.Value = _minuteScrollOffset;
                }
            }
            else
            {
                // Calendar dropdown - navigate month
                if (args.Delta > 0)
                    NavigateMonth(-1);
                else
                    NavigateMonth(1);
            }
            Invalidate();
            return;
        }

        base.OnMouseWheel(e);
    }

    /// <summary>
    /// Raises the MouseLeave event.
    /// </summary>
    protected override void OnMouseLeave(EventArgs e)
    {
        if (_hoveredDate != null)
        {
            _hoveredDate = null;
            Invalidate();
        }
        if (_hoveredHour != -1)
        {
            _hoveredHour = -1;
            Invalidate();
        }
        if (_hoveredMinute != -1)
        {
            _hoveredMinute = -1;
            Invalidate();
        }

        base.OnMouseLeave(e);
    }

    private void HandleDropDownMouseDown(MouseEventArgs args)
    {
        bool hasDate = ShowCalendarInDropdown();
        bool hasTime = ShowTimeInDropdown();
        int dropY = Height;

        if (hasDate)
        {
            // Calendar navigation buttons (rects already in control coordinates)
            if (_calPrevBtnRect.Contains(args.X, args.Y))
            {
                NavigateMonth(-1);
                Invalidate();
                return;
            }
            if (_calNextBtnRect.Contains(args.X, args.Y))
            {
                NavigateMonth(1);
                Invalidate();
                return;
            }
            if (_calTodayBtnRect.Contains(args.X, args.Y))
            {
                SelectDate(DateTime.Today);
                Invalidate();
                return;
            }

            // Day cells (rects already in control coordinates)
            foreach (var (date, rect) in _calCells)
            {
                if (rect.Contains(args.X, args.Y))
                {
                    SelectDate(date);
                    if (!hasTime)
                    {
                        CloseDropDown();
                    }
                    Invalidate();
                    return;
                }
            }
        }

        if (hasTime)
        {
            // Check for time column clicks
            int timeX = CalcTimeX();
            int timeY = dropY + TimeColPadding + TimeHeaderHeight;
            int colH = CalcTimeColumnHeight();
            int colW = (CalcTimeWidth() - TimeColPadding) / 2;

            // Hour column
            if (args.X >= timeX && args.X < timeX + colW)
            {
                int localY = args.Y - timeY + _hourScrollOffset;
                int index = localY / TimeItemHeight;
                if (index >= 0 && index < 24)
                {
                    var newVal = new DateTime(_value.Year, _value.Month, _value.Day, index, _value.Minute, 0);
                    Value = newVal;
                    Checked = true;
                    Invalidate();
                }
                _activeTimeColumn = 0;
                return;
            }

            // Minute column
            int minColX = timeX + colW + TimeColPadding;
            if (args.X >= minColX && args.X < minColX + colW)
            {
                int localY = args.Y - timeY + _minuteScrollOffset;
                int index = localY / TimeItemHeight;
                if (index >= 0 && index < 60)
                {
                    var newVal = new DateTime(_value.Year, _value.Month, _value.Day, _value.Hour, index, 0);
                    Value = newVal;
                    Checked = true;
                    Invalidate();
                }
                _activeTimeColumn = 1;
                return;
            }

            // Scrollbars for time columns
            var hourSbCtx = GetHourScrollBarCtx();
            var minSbCtx = GetMinuteScrollBarCtx();

            int scrollBarSize = ScrollBarEngine.DefaultScrollBarSize;
            int hourSbX = timeX + colW - scrollBarSize;
            if (args.X >= hourSbX && args.X < timeX + colW && 24 * TimeItemHeight > colH)
            {
                _hourScrollBar.HandleMouseDown(new Point(args.X, args.Y),
                    new Rectangle(hourSbX, timeY, scrollBarSize, colH), hourSbCtx);
                return;
            }

            int minSbX = minColX + colW - scrollBarSize;
            if (args.X >= minSbX && args.X < minColX + colW && 60 * TimeItemHeight > colH)
            {
                _minuteScrollBar.HandleMouseDown(new Point(args.X, args.Y),
                    new Rectangle(minSbX, timeY, scrollBarSize, colH), minSbCtx);
                return;
            }
        }

        // Click outside dropdown content - close it
        CloseDropDown();
    }

    private void HandleDropDownMouseMove(MouseEventArgs args)
    {
        bool hasDate = ShowCalendarInDropdown();
        bool hasTime = ShowTimeInDropdown();
        int dropY = Height;

        bool needsInvalidate = false;

        if (hasDate)
        {
            // Calendar hover
            DateTime? prevHover = _hoveredDate;
            _hoveredDate = null;

            // Day cells (rects already in control coordinates)
            foreach (var (date, rect) in _calCells)
            {
                if (rect.Contains(args.X, args.Y))
                {
                    _hoveredDate = date;
                    break;
                }
            }

            if (_hoveredDate != prevHover) needsInvalidate = true;

            // Scrollbar hovers
            if (hasTime)
            {
                int timeX = CalcTimeX();
                int timeY = dropY + TimeColPadding + TimeHeaderHeight;
                int colH = CalcTimeColumnHeight();
                int colW = (CalcTimeWidth() - TimeColPadding) / 2;
                int scrollBarSize = ScrollBarEngine.DefaultScrollBarSize;

                int hourSbX = timeX + colW - scrollBarSize;
                if (24 * TimeItemHeight > colH)
                {
                    _hourScrollBar.HandleMouseMove(new Point(args.X, args.Y),
                        new Rectangle(hourSbX, timeY, scrollBarSize, colH), GetHourScrollBarCtx());
                }

                int minColX = timeX + colW + TimeColPadding;
                int minSbX = minColX + colW - scrollBarSize;
                if (60 * TimeItemHeight > colH)
                {
                    _minuteScrollBar.HandleMouseMove(new Point(args.X, args.Y),
                        new Rectangle(minSbX, timeY, scrollBarSize, colH), GetMinuteScrollBarCtx());
                }
            }

            if (needsInvalidate) Invalidate();
            return;
        }

        if (hasTime)
        {
            int timeX = CalcTimeX();
            int timeY = dropY + TimeColPadding + TimeHeaderHeight;
            int colH = CalcTimeColumnHeight();
            int colW = (CalcTimeWidth() - TimeColPadding) / 2;

            int prevHoveredHour = _hoveredHour;
            int prevHoveredMinute = _hoveredMinute;
            _hoveredHour = -1;
            _hoveredMinute = -1;

            // Hour column
            if (args.X >= timeX && args.X < timeX + colW)
            {
                int localY = args.Y - timeY + _hourScrollOffset;
                int index = localY / TimeItemHeight;
                if (index >= 0 && index < 24)
                    _hoveredHour = index;
            }

            // Minute column
            int minColX = timeX + colW + TimeColPadding;
            if (args.X >= minColX && args.X < minColX + colW)
            {
                int localY = args.Y - timeY + _minuteScrollOffset;
                int index = localY / TimeItemHeight;
                if (index >= 0 && index < 60)
                    _hoveredMinute = index;
            }

            // Scrollbar hover
            int scrollBarSize = ScrollBarEngine.DefaultScrollBarSize;
            int hourSbX = timeX + colW - scrollBarSize;
            if (24 * TimeItemHeight > colH)
            {
                _hourScrollBar.HandleMouseMove(new Point(args.X, args.Y),
                    new Rectangle(hourSbX, timeY, scrollBarSize, colH), GetHourScrollBarCtx());
            }

            int minSbX2 = minColX + colW - scrollBarSize;
            if (60 * TimeItemHeight > colH)
            {
                _minuteScrollBar.HandleMouseMove(new Point(args.X, args.Y),
                    new Rectangle(minSbX2, timeY, scrollBarSize, colH), GetMinuteScrollBarCtx());
            }

            if (_hoveredHour != prevHoveredHour || _hoveredMinute != prevHoveredMinute)
                Invalidate();
        }
    }

    #endregion

    #region Keyboard & Focus Handling

    /// <summary>
    /// Raises the KeyDown event.
    /// </summary>
    protected internal override void OnKeyDown(KeyEventArgs e)
    {
        if (!Enabled) return;

        if (_droppedDown)
        {
            bool hasDate = ShowCalendarInDropdown();
            bool hasTime = ShowTimeInDropdown();

            switch (e.KeyCode)
            {
                case Keys.Escape:
                    CloseDropDown();
                    e.Handled = true;
                    break;

                case Keys.Enter:
                    if (hasDate && !hasTime)
                    {
                        // If a date was hovered, select it
                        if (_hoveredDate.HasValue)
                        {
                            SelectDate(_hoveredDate.Value);
                        }
                    }
                    CloseDropDown();
                    e.Handled = true;
                    break;

                case Keys.Left:
                    if (hasDate)
                    {
                        var newVal = _value.AddDays(-1);
                        SelectDate(newVal);
                        e.Handled = true;
                    }
                    else if (hasTime)
                    {
                        _activeTimeColumn = 0;
                        Invalidate();
                        e.Handled = true;
                    }
                    break;

                case Keys.Right:
                    if (hasDate)
                    {
                        var newVal = _value.AddDays(1);
                        SelectDate(newVal);
                        e.Handled = true;
                    }
                    else if (hasTime)
                    {
                        _activeTimeColumn = 1;
                        Invalidate();
                        e.Handled = true;
                    }
                    break;

                case Keys.Up:
                    if (hasDate)
                    {
                        var newVal = _value.AddDays(-7);
                        SelectDate(newVal);
                        e.Handled = true;
                    }
                    else if (hasTime)
                    {
                        if (_activeTimeColumn == 0)
                        {
                            var newVal = _value.AddHours(-1);
                            if (newVal >= _minDate) Value = newVal;
                        }
                        else
                        {
                            var newVal = _value.AddMinutes(-1);
                            if (newVal >= _minDate) Value = newVal;
                        }
                        EnsureTimeVisible();
                        e.Handled = true;
                    }
                    break;

                case Keys.Down:
                    if (hasDate)
                    {
                        var newVal = _value.AddDays(7);
                        SelectDate(newVal);
                        e.Handled = true;
                    }
                    else if (hasTime)
                    {
                        if (_activeTimeColumn == 0)
                        {
                            var newVal = _value.AddHours(1);
                            if (newVal <= _maxDate) Value = newVal;
                        }
                        else
                        {
                            var newVal = _value.AddMinutes(1);
                            if (newVal <= _maxDate) Value = newVal;
                        }
                        EnsureTimeVisible();
                        e.Handled = true;
                    }
                    break;

                case Keys.PageUp:
                    if (hasDate)
                    {
                        NavigateMonth(-1);
                        Invalidate();
                        e.Handled = true;
                    }
                    break;

                case Keys.PageDown:
                    if (hasDate)
                    {
                        NavigateMonth(1);
                        Invalidate();
                        e.Handled = true;
                    }
                    break;

                case Keys.Home:
                    if (hasDate)
                    {
                        SelectDate(DateTime.Today);
                        e.Handled = true;
                    }
                    break;
            }

            if (e.Handled) return;
        }

        switch (e.KeyCode)
        {
            case Keys.F4:
                if (_showUpDown) break;
                if (_droppedDown)
                    CloseDropDown();
                else
                {
                    _droppedDown = true;
                    CapturingMouse = true;
                    InitializeDropdownState();
                    OnDropDown();
                    Invalidate();
                }
                e.Handled = true;
                break;

            case Keys.Left:
                if (_showUpDown)
                {
                    CycleActiveField(-1);
                    e.Handled = true;
                }
                break;

            case Keys.Right:
                if (_showUpDown)
                {
                    CycleActiveField(1);
                    e.Handled = true;
                }
                break;

            case Keys.Up:
                IncrementValue();
                e.Handled = true;
                break;

            case Keys.Down:
                DecrementValue();
                e.Handled = true;
                break;
        }

        base.OnKeyDown(e);
    }

    /// <summary>
    /// Raises the LostFocus event and closes the dropdown if open.
    /// </summary>
    protected internal override void OnLostFocus(EventArgs e)
    {
        if (_droppedDown)
            CloseDropDown();
        base.OnLostFocus(e);
    }

    #endregion

    #region HitTest

    /// <summary>
    /// Tests whether the specified point is within the control bounds (including dropdown overlay).
    /// </summary>
    public override bool HitTest(Point point)
    {
        if (Bounds.Contains(point))
            return true;

        if (_droppedDown)
        {
            var dropBounds = new Rectangle(X, Y + Height, CalcDropdownWidth(), CalcDropdownHeight());
            if (dropBounds.Contains(point))
                return true;
        }

        return false;
    }

    #endregion

    #region Dropdown Management

    private void InitializeDropdownState()
    {
        _viewDate = new DateTime(_value.Year, _value.Month, 1);
        _hoveredDate = null;
        _hoveredHour = -1;
        _hoveredMinute = -1;
        _hourScrollOffset = Math.Max(0, _value.Hour * TimeItemHeight - CalcTimeColumnHeight() / 2);
        _minuteScrollOffset = Math.Max(0, _value.Minute * TimeItemHeight - CalcTimeColumnHeight() / 2);
        _hourScrollBar.Value = _hourScrollOffset;
        _minuteScrollBar.Value = _minuteScrollOffset;
        _activeTimeColumn = 0;
    }

    /// <summary>
    /// Opens the drop-down calendar or time picker.
    /// </summary>
    public void OpenDropDown()
    {
        if (_droppedDown || _showUpDown) return;
        _droppedDown = true;
        CapturingMouse = true;
        InitializeDropdownState();
        OnDropDown();
        Invalidate();
    }

    /// <summary>
    /// Closes the drop-down calendar or time picker.
    /// </summary>
    public void CloseDropDown()
    {
        if (!_droppedDown) return;
        _droppedDown = false;
        CapturingMouse = false;
        _hoveredDate = null;
        _hoveredHour = -1;
        _hoveredMinute = -1;
        _calCells.Clear();
        OnCloseUp();
        Invalidate();
    }

    #endregion

    #region Helper Methods

    private string GetFormatString()
    {
        var ci = CultureInfo.CurrentCulture;
        return _format switch
        {
            DateTimePickerFormat.Long => ci.DateTimeFormat.LongDatePattern,
            DateTimePickerFormat.Short => ci.DateTimeFormat.ShortDatePattern,
            DateTimePickerFormat.Time => ci.DateTimeFormat.LongTimePattern,
            DateTimePickerFormat.Custom => _customFormat ?? ci.DateTimeFormat.ShortDatePattern,
            _ => ci.DateTimeFormat.ShortDatePattern,
        };
    }

    private string GetFormattedValue()
    {
        if (!_checked) return string.Empty;
        string fmt = GetFormatString();
        return _value.ToString(fmt, CultureInfo.CurrentCulture);
    }

    private DateTime ValidateValue(DateTime dt)
    {
        if (dt < _minDate) return _minDate;
        if (dt > _maxDate) return _maxDate;
        return dt;
    }

    private void SelectDate(DateTime date)
    {
        date = ValidateValue(date.Date + _value.TimeOfDay);
        if (date != _value)
        {
            _value = date;
            OnValueChanged();
            OnPropertyChanged(nameof(Value));
            if (!_checked) Checked = true;
        }
        _viewDate = new DateTime(date.Year, date.Month, 1);
    }

    // ── ShowUpDown field navigation ──

    private struct FormatToken
    {
        public string Raw;
        public bool IsField;
        public string FieldSpec; // e.g. "dd", "MM", "yyyy", "HH", "mm", "ss", "tt"
    }

    private List<FormatToken> ParseFormat(string format)
    {
        var tokens = new List<FormatToken>();
        int i = 0;
        while (i < format.Length)
        {
            if (format[i] == '\'')
            {
                int end = format.IndexOf('\'', i + 1);
                if (end < 0) end = format.Length - 1;
                tokens.Add(new FormatToken { Raw = format.Substring(i + 1, end - i - 1), IsField = false });
                i = end + 1;
                continue;
            }

            char c = format[i];
            if (c == 'd' || c == 'M' || c == 'y' || c == 'h' || c == 'H' || c == 'm' || c == 's' || c == 't')
            {
                int start = i;
                while (i < format.Length && format[i] == c) i++;
                string spec = format.Substring(start, i - start);
                tokens.Add(new FormatToken { Raw = spec, IsField = true, FieldSpec = spec });
            }
            else
            {
                int start = i;
                while (i < format.Length && format[i] != '\'' && "dMyHhmsft".IndexOf(format[i]) < 0) i++;
                tokens.Add(new FormatToken { Raw = format.Substring(start, i - start), IsField = false });
            }
        }
        return tokens;
    }

    private DateFieldType? TokenToFieldType(string spec)
    {
        if (string.IsNullOrEmpty(spec)) return null;
        char c = spec[0];
        return c switch
        {
            'd' => DateFieldType.Day,
            'M' => DateFieldType.Month,
            'y' => DateFieldType.Year,
            'h' or 'H' => DateFieldType.Hour,
            'm' => DateFieldType.Minute,
            's' => DateFieldType.Second,
            't' => DateFieldType.AmPm,
            _ => null,
        };
    }

    private void MeasureFields(Graphics g, float textX, float textY)
    {
        _fieldMeasurements.Clear();
        string fmt = GetFormatString();
        var tokens = ParseFormat(fmt);
        var font = EffectiveFont;
        float x = textX;

        foreach (var token in tokens)
        {
            if (token.IsField)
            {
                string valueText = _value.ToString(token.FieldSpec, CultureInfo.CurrentCulture);
                var size = g.MeasureString(valueText, font);
                var ft = TokenToFieldType(token.FieldSpec);
                if (ft.HasValue)
                    _fieldMeasurements.Add((ft.Value, x, size.width));
                x += size.width;
            }
            else
            {
                var size = g.MeasureString(token.Raw, font);
                x += size.width;
            }
        }
    }

    private void AdjustActiveFieldForFormat()
    {
        string fmt = GetFormatString();
        var tokens = ParseFormat(fmt);
        var fields = tokens.Where(t => t.IsField).Select(t => TokenToFieldType(t.FieldSpec)).OfType<DateFieldType>().Distinct().ToList();
        if (fields.Count == 0) return;

        // Cycle: if current field doesn't exist in format, pick the first available
        if (!fields.Contains(_activeField))
            _activeField = fields[0];
        // In Date-only formats, never default to Hour/Minute
        if (_format != DateTimePickerFormat.Time && _format != DateTimePickerFormat.Custom &&
            (_activeField == DateFieldType.Hour || _activeField == DateFieldType.Minute))
            _activeField = DateFieldType.Day;
    }

    private void SetFieldValue(int delta)
    {
        AdjustActiveFieldForFormat();
        var dt = _value;
        switch (_activeField)
        {
            case DateFieldType.Day:
                dt = dt.AddDays(delta);
                break;
            case DateFieldType.Month:
                dt = dt.AddMonths(delta);
                break;
            case DateFieldType.Year:
                dt = dt.AddYears(delta);
                break;
            case DateFieldType.Hour:
                dt = dt.AddHours(delta);
                break;
            case DateFieldType.Minute:
                dt = dt.AddMinutes(delta);
                break;
            case DateFieldType.Second:
                dt = dt.AddSeconds(delta);
                break;
        }
        dt = ValidateValue(dt);
        if (dt != _value)
            Value = dt;
    }

    private void CycleActiveField(int direction)
    {
        AdjustActiveFieldForFormat();
        string fmt = GetFormatString();
        var tokens = ParseFormat(fmt);
        var fields = tokens.Where(t => t.IsField).Select(t => TokenToFieldType(t.FieldSpec)).OfType<DateFieldType>().Distinct().ToList();
        if (fields.Count == 0) return;

        int idx = fields.IndexOf(_activeField);
        if (idx < 0) idx = 0;
        idx = (idx + direction + fields.Count) % fields.Count;
        _activeField = fields[idx];
        Invalidate();
    }

    private void IncrementValue()
    {
        if (_showUpDown)
        {
            SetFieldValue(1);
            return;
        }

        DateTime newVal;
        if (_format == DateTimePickerFormat.Time)
            newVal = _value.AddHours(1);
        else
            newVal = _value.AddDays(1);

        if (newVal <= _maxDate)
            Value = newVal;
    }

    private void DecrementValue()
    {
        if (_showUpDown)
        {
            SetFieldValue(-1);
            return;
        }

        DateTime newVal;
        if (_format == DateTimePickerFormat.Time)
            newVal = _value.AddHours(-1);
        else
            newVal = _value.AddDays(-1);

        if (newVal >= _minDate)
            Value = newVal;
    }

    private void NavigateMonth(int delta)
    {
        _viewDate = _viewDate.AddMonths(delta);
    }

    private void EnsureTimeVisible()
    {
        int hourRow = _value.Hour;
        int maxHourScroll = Math.Max(0, 24 * TimeItemHeight - CalcTimeColumnHeight());
        if (_hourScrollOffset > hourRow * TimeItemHeight)
            _hourScrollOffset = hourRow * TimeItemHeight;
        else if (_hourScrollOffset + CalcTimeColumnHeight() < (hourRow + 1) * TimeItemHeight)
            _hourScrollOffset = (hourRow + 1) * TimeItemHeight - CalcTimeColumnHeight();
        _hourScrollOffset = Math.Clamp(_hourScrollOffset, 0, maxHourScroll);
        _hourScrollBar.Value = _hourScrollOffset;

        int minRow = _value.Minute;
        int maxMinScroll = Math.Max(0, 60 * TimeItemHeight - CalcTimeColumnHeight());
        if (_minuteScrollOffset > minRow * TimeItemHeight)
            _minuteScrollOffset = minRow * TimeItemHeight;
        else if (_minuteScrollOffset + CalcTimeColumnHeight() < (minRow + 1) * TimeItemHeight)
            _minuteScrollOffset = (minRow + 1) * TimeItemHeight - CalcTimeColumnHeight();
        _minuteScrollOffset = Math.Clamp(_minuteScrollOffset, 0, maxMinScroll);
        _minuteScrollBar.Value = _minuteScrollOffset;
    }

    private int CalcDropdownWidth()
    {
        if (_dropDownWidth > 0)
            return _dropDownWidth;

        bool hasDate = ShowCalendarInDropdown();
        bool hasTime = ShowTimeInDropdown();

        if (hasDate && hasTime)
            return CalPadding * 2 + 7 * CalCellWidth + 4 + CalcTimeWidth();

        if (hasDate)
            return Math.Max(Width, CalPadding * 2 + 7 * CalCellWidth + CalTodayButtonHeight);

        if (hasTime)
            return Math.Max(Width, CalcTimeWidth() + TimeColPadding * 2);

        return Width;
    }

    private int CalcDropdownHeight()
    {
        bool hasDate = ShowCalendarInDropdown();
        bool hasTime = ShowTimeInDropdown();

        int dateH = 0;
        if (hasDate)
            dateH = CalPadding * 2 + CalHeaderHeight + CalDayHeaderHeight + 6 * CalCellHeight + CalTodayButtonHeight;

        int timeH = 0;
        if (hasTime)
        {
            int visibleItems = Math.Min(6, 24); // Show max 6 items
            timeH = TimeColPadding * 2 + TimeHeaderHeight + visibleItems * TimeItemHeight;
        }

        if (hasDate && hasTime)
            return Math.Max(dateH, timeH);

        return Math.Max(dateH, timeH);
    }

    private int CalcTimeWidth()
    {
        return TimeColPadding + 2 * TimeColumnWidth + ScrollBarEngine.DefaultScrollBarSize * 2 + TimeColPadding;
    }

    private int CalcTimeColumnHeight()
    {
        int visibleItems = Math.Min(6, 24);
        return visibleItems * TimeItemHeight;
    }

    private int CalcTimeX()
    {
        if (ShowCalendarInDropdown() && ShowTimeInDropdown())
            return CalPadding * 2 + 7 * CalCellWidth + 4;
        return TimeColPadding;
    }

    // ScrollBar contexts for time columns
    private sealed class DtpScrollBarContext : IScrollBarContext
    {
        private readonly DateTimePicker _owner;
        public DtpScrollBarContext(DateTimePicker owner) => _owner = owner;
        public float Zoom => _owner.EffectiveZoom;
        public void Invalidate() => _owner.Invalidate();
        public void CaptureMouse(bool capture) => _owner.CapturingMouse = capture;
    }

    private DtpScrollBarContext? _hourScrollBarCtx;
    private DtpScrollBarContext? _minuteScrollBarCtx;

    private DtpScrollBarContext GetHourScrollBarCtx() =>
        _hourScrollBarCtx ??= new DtpScrollBarContext(this);

    private DtpScrollBarContext GetMinuteScrollBarCtx() =>
        _minuteScrollBarCtx ??= new DtpScrollBarContext(this);

    #endregion
}
