using CoreForms.Ui.Core;

namespace CoreForms.Ui.Theming;

/// <summary>
/// Dark theme with modern dark colors for reduced eye strain.
/// </summary>
public class DarkTheme : Theme
{
    /// <summary>
    /// Initializes a new instance of DarkTheme.
    /// </summary>
    public DarkTheme() : base("Dark")
    {
    }

    // Core system colors
    /// <inheritdoc />
    public override Color ControlBackground => Color.FromArgb(45, 45, 48);

    /// <inheritdoc />
    public override Color ControlText => Color.FromArgb(220, 220, 220);

    /// <inheritdoc />
    public override Color WindowBackground => Color.FromArgb(32, 32, 32);

    /// <inheritdoc />
    public override Color WindowText => Color.FromArgb(220, 220, 220);

    /// <inheritdoc />
    public override Color Highlight => Color.FromArgb(0, 120, 212);

    /// <inheritdoc />
    public override Color HighlightText => Color.FromArgb(255, 255, 255);

    /// <inheritdoc />
    public override Color ActiveCaption => Color.FromArgb(30, 30, 30);

    /// <inheritdoc />
    public override Color ActiveCaptionText => Color.FromArgb(220, 220, 220);

    /// <inheritdoc />
    public override Color InactiveCaption => Color.FromArgb(60, 60, 60);

    /// <inheritdoc />
    public override Color ControlLight => Color.FromArgb(60, 60, 63);

    /// <inheritdoc />
    public override Color ControlDark => Color.FromArgb(70, 70, 73);

    /// <inheritdoc />
    public override Color GrayText => Color.FromArgb(120, 120, 120);

    // Extended theme colors
    /// <inheritdoc />
    public override Color Border => Color.FromArgb(80, 80, 83);

    /// <inheritdoc />
    public override Color FocusIndicator => Color.FromArgb(0, 120, 212);

    /// <inheritdoc />
    public override Color ProgressBarFill => Color.FromArgb(0, 120, 212);

    /// <inheritdoc />
    public override Color ScrollbarTrack => Color.FromArgb(45, 45, 48);

    /// <inheritdoc />
    public override Color ScrollbarThumb => Color.FromArgb(90, 90, 93);

    /// <inheritdoc />
    public override Color ScrollbarThumbBorder => Color.FromArgb(70, 70, 73);

    /// <inheritdoc />
    public override Color ScrollbarBorder => Color.FromArgb(60, 60, 63);

    /// <inheritdoc />
    public override Color GridLine => Color.FromArgb(60, 60, 63);

    /// <inheritdoc />
    public override Color GridLineVertical => Color.FromArgb(50, 50, 53);

    /// <inheritdoc />
    public override Color AlternateRow => Color.FromArgb(38, 38, 38);

    /// <inheritdoc />
    public override Color HoverHighlight => Color.FromArgb(60, 60, 80);

    /// <inheritdoc />
    public override Color CheckboxBackground => Color.FromArgb(50, 50, 53);

    /// <inheritdoc />
    public override Color CheckboxBorder => Color.FromArgb(120, 120, 123);

    /// <inheritdoc />
    public override Color CheckboxCheck => Color.FromArgb(220, 220, 220);

    /// <inheritdoc />
    public override Color TextBoxBackground => Color.FromArgb(50, 50, 53);

    /// <inheritdoc />
    public override Color TextBoxText => Color.FromArgb(220, 220, 220);

    /// <inheritdoc />
    public override Color TextBoxBorder => Color.FromArgb(80, 80, 83);

    /// <inheritdoc />
    public override Color TextBoxFocusBorder => Color.FromArgb(0, 120, 212);

    /// <inheritdoc />
    public override Color ContentBackground => Color.FromArgb(45, 45, 48);

    /// <inheritdoc />
    public override Color TabHeaderBackground => Color.FromArgb(40, 40, 43);

    /// <inheritdoc />
    public override Color TabSelectedBackground => Color.FromArgb(50, 50, 53);

    /// <inheritdoc />
    public override Color TabSelectedText => Color.FromArgb(220, 220, 220);

    /// <inheritdoc />
    public override Color TabUnselectedBackground => Color.FromArgb(35, 35, 38);

    /// <inheritdoc />
    public override Color TabUnselectedText => Color.FromArgb(150, 150, 153);

    /// <inheritdoc />
    public override Color TabSeparator => Color.FromArgb(70, 70, 73);

    /// <inheritdoc />
    public override Color TabSelectedBorder => Color.FromArgb(80, 80, 83);

    /// <inheritdoc />
    public override Color TabContentBackground => Color.FromArgb(50, 50, 53);

    /// <inheritdoc />
    public override Color MenuSeparator => Color.FromArgb(70, 70, 73);

    /// <inheritdoc />
    public override Color MenuHover => Color.FromArgb(60, 60, 63);

    /// <inheritdoc />
    public override Color MenuDropdownBackground => Color.FromArgb(45, 45, 48);

    /// <inheritdoc />
    public override Color MenuDropdownBorder => Color.FromArgb(80, 80, 83);

    /// <inheritdoc />
    public override Color MenuDropdownHover => Color.FromArgb(60, 60, 80);

    /// <inheritdoc />
    public override Color DataGridViewBorder => Color.FromArgb(70, 70, 73);

    /// <inheritdoc />
    public override Color DataGridViewHeaderSeparator => Color.FromArgb(60, 60, 63);

    /// <inheritdoc />
    public override Color DataGridViewHeaderText => Color.FromArgb(200, 200, 200);

    /// <inheritdoc />
    public override Color DataGridViewRowHeaderText => Color.FromArgb(200, 200, 200);

    /// <inheritdoc />
    public override Color DataGridViewCellText => Color.FromArgb(220, 220, 220);

    /// <inheritdoc />
    public override Color DataGridViewAddNewRowBackground => Color.FromArgb(40, 40, 43);

    /// <inheritdoc />
    public override Color DataGridViewAddNewRowSeparator => Color.FromArgb(70, 70, 73);

    /// <inheritdoc />
    public override Color DataGridViewAddNewRowAsterisk => Color.FromArgb(120, 120, 123);

    /// <inheritdoc />
    public override Color DataGridViewSortArrow => Color.FromArgb(180, 180, 180);

    /// <inheritdoc />
    public override Color DataGridViewGroupHeaderBackground => Color.FromArgb(55, 55, 58);

    /// <inheritdoc />
    public override Color DataGridViewGroupHeaderText => Color.FromArgb(220, 220, 220);

    /// <inheritdoc />
    public override Color DataGridViewGroupingBarBackground => Color.FromArgb(50, 50, 53);

    /// <inheritdoc />
    public override Color DataGridViewGroupingBarText => Color.FromArgb(160, 160, 160);

    /// <inheritdoc />
    public override Color DataGridViewGroupingBarPillBackground => Color.FromArgb(70, 70, 75);

    /// <inheritdoc />
    public override Color TableLayoutGridLine => Color.FromArgb(70, 70, 73);

    /// <inheritdoc />
    public override Color PanelBorder => Color.FromArgb(80, 80, 83);

    /// <inheritdoc />
    public override Color MessageBoxOverlay => Color.FromArgb(128, 0, 0, 0);

    /// <inheritdoc />
    public override Color MessageBoxBorder => Color.FromArgb(80, 80, 83);

    /// <inheritdoc />
    public override Color ButtonHoverBackground => Color.FromArgb(60, 60, 63);

    /// <inheritdoc />
    public override Color ButtonPressedBackground => Color.FromArgb(35, 35, 38);

    /// <inheritdoc />
    public override Color ButtonBorder => Color.FromArgb(80, 80, 83);

    /// <inheritdoc />
    public override Color ComboBoxDropdownArrow => Color.FromArgb(180, 180, 183);

    /// <inheritdoc />
    public override Color ComboBoxDropdownButtonSeparator => Color.FromArgb(80, 80, 83);

    /// <inheritdoc />
    public override Color ToolStripItemText => Color.FromArgb(220, 220, 220);

    /// <inheritdoc />
    public override Color StatusStripTopLine => Color.FromArgb(70, 70, 73);

    /// <inheritdoc />
    public override Color CursorLine => Color.FromArgb(220, 220, 220);

    /// <inheritdoc />
    public override Color SeparatorDark => Color.FromArgb(90, 90, 93);

    /// <inheritdoc />
    public override Color SeparatorLight => Color.FromArgb(60, 60, 63);

    // Calendar colors
    /// <inheritdoc />
    public override Color CalendarHeaderBackground => Color.FromArgb(58, 58, 60);

    /// <inheritdoc />
    public override Color CalendarTodayHighlight => Color.FromArgb(58, 95, 122);

    /// <inheritdoc />
    public override Color CalendarSelectedBorder => Color.FromArgb(0, 120, 212);

    /// <inheritdoc />
    public override Color CalendarWeekendBackground => Color.FromArgb(42, 42, 45);

    /// <inheritdoc />
    public override Color CalendarWorkHoursBackground => Color.FromArgb(50, 50, 53);

    /// <inheritdoc />
    public override Color CalendarAppointmentDefault => Color.FromArgb(0, 120, 212);

    /// <inheritdoc />
    public override Color CalendarTimeRulerText => Color.FromArgb(140, 140, 140);

    /// <inheritdoc />
    public override Color CalendarDayHeaderBackground => Color.FromArgb(50, 50, 53);

    // Fonts
    /// <inheritdoc />
    public override Font DefaultFont => ResolveFont(new[] { "Segoe UI", "Liberation Sans", "DejaVu Sans", "FreeSans", "Arial" }, 14f);

    /// <inheritdoc />
    public override Font HeadingFont => ResolveFont(new[] { "Segoe UI", "Liberation Sans", "DejaVu Sans", "FreeSans", "Arial" }, 16f, FontStyle.Bold);

    /// <inheritdoc />
    public override Font SmallFont => ResolveFont(new[] { "Segoe UI", "Liberation Sans", "DejaVu Sans", "FreeSans", "Arial" }, 11f);

    /// <inheritdoc />
    public override Font MonospaceFont => ResolveFont(new[] { "Cascadia Mono", "Consolas", "Liberation Mono", "DejaVu Sans Mono", "FreeMono", "Courier New" }, 13f);
}
