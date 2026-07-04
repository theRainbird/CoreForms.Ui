using OldSchoolForms.Ui.Core;

namespace OldSchoolForms.Ui.Theming;

/// <summary>
/// Light theme with traditional Windows-style colors.
/// </summary>
public class LightTheme : Theme
{
    /// <summary>
    /// Initializes a new instance of LightTheme.
    /// </summary>
    public LightTheme() : base("Light")
    {
    }

    // Core system colors
    /// <inheritdoc />
    public override Color ControlBackground => Color.FromArgb(212, 208, 200);

    /// <inheritdoc />
    public override Color ControlText => Color.FromArgb(0, 0, 0);

    /// <inheritdoc />
    public override Color WindowBackground => Color.FromArgb(255, 255, 255);

    /// <inheritdoc />
    public override Color WindowText => Color.FromArgb(0, 0, 0);

    /// <inheritdoc />
    public override Color Highlight => Color.FromArgb(10, 36, 99);

    /// <inheritdoc />
    public override Color HighlightText => Color.FromArgb(255, 255, 255);

    /// <inheritdoc />
    public override Color ActiveCaption => Color.FromArgb(10, 36, 99);

    /// <inheritdoc />
    public override Color ActiveCaptionText => Color.FromArgb(255, 255, 255);

    /// <inheritdoc />
    public override Color InactiveCaption => Color.FromArgb(128, 128, 128);

    /// <inheritdoc />
    public override Color ControlLight => Color.FromArgb(240, 240, 240);

    /// <inheritdoc />
    public override Color ControlDark => Color.FromArgb(160, 160, 160);

    /// <inheritdoc />
    public override Color GrayText => Color.FromArgb(128, 128, 128);

    // Extended theme colors
    /// <inheritdoc />
    public override Color Border => Color.FromArgb(128, 128, 128);

    /// <inheritdoc />
    public override Color FocusIndicator => Color.FromArgb(0, 120, 215);

    /// <inheritdoc />
    public override Color ProgressBarFill => Color.FromArgb(0, 120, 215);

    /// <inheritdoc />
    public override Color ScrollbarTrack => Color.FromArgb(240, 240, 240);

    /// <inheritdoc />
    public override Color ScrollbarThumb => Color.FromArgb(190, 190, 190);

    /// <inheritdoc />
    public override Color ScrollbarThumbBorder => Color.FromArgb(150, 150, 150);

    /// <inheritdoc />
    public override Color ScrollbarBorder => Color.FromArgb(180, 180, 180);

    /// <inheritdoc />
    public override Color GridLine => Color.FromArgb(180, 180, 180);

    /// <inheritdoc />
    public override Color GridLineVertical => Color.FromArgb(220, 220, 220);

    /// <inheritdoc />
    public override Color AlternateRow => Color.FromArgb(245, 245, 245);

    /// <inheritdoc />
    public override Color HoverHighlight => Color.FromArgb(200, 220, 255);

    /// <inheritdoc />
    public override Color CheckboxBackground => Color.FromArgb(255, 255, 255);

    /// <inheritdoc />
    public override Color CheckboxBorder => Color.FromArgb(128, 128, 128);

    /// <inheritdoc />
    public override Color CheckboxCheck => Color.FromArgb(0, 0, 0);

    /// <inheritdoc />
    public override Color TextBoxBackground => Color.FromArgb(255, 255, 255);

    /// <inheritdoc />
    public override Color TextBoxText => Color.FromArgb(0, 0, 0);

    /// <inheritdoc />
    public override Color TextBoxBorder => Color.FromArgb(128, 128, 128);

    /// <inheritdoc />
    public override Color TextBoxFocusBorder => Color.FromArgb(0, 120, 215);

    /// <inheritdoc />
    public override Color ContentBackground => Color.FromArgb(255, 255, 255);

    /// <inheritdoc />
    public override Color TabHeaderBackground => Color.FromArgb(230, 230, 230);

    /// <inheritdoc />
    public override Color TabSelectedBackground => Color.FromArgb(255, 255, 255);

    /// <inheritdoc />
    public override Color TabSelectedText => Color.FromArgb(0, 0, 0);

    /// <inheritdoc />
    public override Color TabUnselectedBackground => Color.FromArgb(210, 210, 210);

    /// <inheritdoc />
    public override Color TabUnselectedText => Color.FromArgb(100, 100, 100);

    /// <inheritdoc />
    public override Color TabSeparator => Color.FromArgb(150, 150, 150);

    /// <inheritdoc />
    public override Color TabSelectedBorder => Color.FromArgb(180, 180, 180);

    /// <inheritdoc />
    public override Color TabContentBackground => Color.FromArgb(255, 255, 255);

    /// <inheritdoc />
    public override Color MenuSeparator => Color.FromArgb(180, 180, 180);

    /// <inheritdoc />
    public override Color MenuHover => Color.FromArgb(200, 200, 200);

    /// <inheritdoc />
    public override Color MenuDropdownBackground => Color.FromArgb(255, 255, 255);

    /// <inheritdoc />
    public override Color MenuDropdownBorder => Color.FromArgb(100, 100, 100);

    /// <inheritdoc />
    public override Color MenuDropdownHover => Color.FromArgb(200, 220, 255);

    /// <inheritdoc />
    public override Color DataGridViewBorder => Color.FromArgb(180, 180, 180);

    /// <inheritdoc />
    public override Color DataGridViewHeaderSeparator => Color.FromArgb(150, 150, 150);

    /// <inheritdoc />
    public override Color DataGridViewHeaderText => Color.FromArgb(0, 0, 0);

    /// <inheritdoc />
    public override Color DataGridViewRowHeaderText => Color.FromArgb(0, 0, 0);

    /// <inheritdoc />
    public override Color DataGridViewCellText => Color.FromArgb(0, 0, 0);

    /// <inheritdoc />
    public override Color DataGridViewAddNewRowBackground => Color.FromArgb(250, 250, 250);

    /// <inheritdoc />
    public override Color DataGridViewAddNewRowSeparator => Color.FromArgb(150, 150, 150);

    /// <inheritdoc />
    public override Color DataGridViewAddNewRowAsterisk => Color.FromArgb(150, 150, 150);

    /// <inheritdoc />
    public override Color DataGridViewSortArrow => Color.FromArgb(100, 100, 100);

    /// <inheritdoc />
    public override Color DataGridViewGroupHeaderBackground => Color.FromArgb(230, 230, 235);

    /// <inheritdoc />
    public override Color DataGridViewGroupHeaderText => Color.FromArgb(0, 0, 0);

    /// <inheritdoc />
    public override Color DataGridViewGroupingBarBackground => Color.FromArgb(245, 245, 245);

    /// <inheritdoc />
    public override Color DataGridViewGroupingBarText => Color.FromArgb(100, 100, 100);

    /// <inheritdoc />
    public override Color DataGridViewGroupingBarPillBackground => Color.FromArgb(200, 200, 210);

    /// <inheritdoc />
    public override Color PivotTableHeaderBackground => Color.FromArgb(240, 240, 240);

    /// <inheritdoc />
    public override Color PivotTableHeaderText => Color.FromArgb(0, 0, 0);

    /// <inheritdoc />
    public override Color PivotTableTotalBackground => Color.FromArgb(230, 235, 245);

    /// <inheritdoc />
    public override Color PivotTableAlternateRow => Color.FromArgb(245, 245, 250);

    /// <inheritdoc />
    public override Color PivotTableGridLine => Color.FromArgb(200, 200, 200);

    /// <inheritdoc />
    public override Color PivotTableSelection => Color.FromArgb(200, 220, 255);

    /// <inheritdoc />
    public override Color TableLayoutGridLine => Color.FromArgb(180, 180, 180);

    /// <inheritdoc />
    public override Color PanelBorder => Color.FromArgb(128, 128, 128);

    /// <inheritdoc />
    public override Color MessageBoxOverlay => Color.FromArgb(128, 0, 0, 0);

    /// <inheritdoc />
    public override Color MessageBoxBorder => Color.FromArgb(100, 100, 100);

    /// <inheritdoc />
    public override Color ButtonHoverBackground => Color.FromArgb(240, 240, 240);

    /// <inheritdoc />
    public override Color ButtonPressedBackground => Color.FromArgb(220, 220, 220);

    /// <inheritdoc />
    public override Color ButtonBorder => Color.FromArgb(128, 128, 128);

    /// <inheritdoc />
    public override Color ComboBoxDropdownArrow => Color.FromArgb(80, 80, 80);

    /// <inheritdoc />
    public override Color ComboBoxDropdownButtonSeparator => Color.FromArgb(128, 128, 128);

    /// <inheritdoc />
    public override Color ToolStripItemText => Color.FromArgb(0, 0, 0);

    /// <inheritdoc />
    public override Color StatusStripTopLine => Color.FromArgb(150, 150, 150);

    /// <inheritdoc />
    public override Color CursorLine => Color.FromArgb(0, 0, 0);

    /// <inheritdoc />
    public override Color SeparatorDark => Color.FromArgb(180, 180, 180);

    /// <inheritdoc />
    public override Color SeparatorLight => Color.FromArgb(255, 255, 255);

    // Calendar colors
    /// <inheritdoc />
    public override Color CalendarHeaderBackground => Color.FromArgb(248, 248, 248);

    /// <inheritdoc />
    public override Color CalendarTodayHighlight => Color.FromArgb(230, 240, 250);

    /// <inheritdoc />
    public override Color CalendarSelectedBorder => Color.FromArgb(10, 36, 99);

    /// <inheritdoc />
    public override Color CalendarWeekendBackground => Color.FromArgb(245, 245, 245);

    /// <inheritdoc />
    public override Color CalendarWorkHoursBackground => Color.FromArgb(250, 250, 250);

    /// <inheritdoc />
    public override Color CalendarAppointmentDefault => Color.FromArgb(0, 120, 215);

    /// <inheritdoc />
    public override Color CalendarTimeRulerText => Color.FromArgb(100, 100, 100);

    /// <inheritdoc />
    public override Color CalendarDayHeaderBackground => Color.FromArgb(242, 242, 242);

    // Diagram colors
    /// <inheritdoc />
    public override Color DiagramBackground => Color.FromArgb(255, 255, 255);

    /// <inheritdoc />
    public override Color DiagramGridLine => Color.FromArgb(220, 220, 220);

    /// <inheritdoc />
    public override Color DiagramAxisLine => Color.FromArgb(160, 160, 160);

    /// <inheritdoc />
    public override Color DiagramAxisLabel => Color.FromArgb(80, 80, 80);

    /// <inheritdoc />
    public override Color DiagramLegendBackground => Color.FromArgb(250, 250, 250);

    private static readonly Color[] _diagramPalette = new[]
    {
        Color.FromArgb(91, 155, 213),   // Excel Blau
        Color.FromArgb(237, 125, 49),   // Excel Orange
        Color.FromArgb(112, 173, 71),   // Excel Grün
        Color.FromArgb(241, 90, 96),    // Excel Rot
        Color.FromArgb(165, 105, 189),  // Excel Lila
        Color.FromArgb(68, 194, 194),   // Excel Türkis
        Color.FromArgb(237, 176, 32),   // Excel Gelb
        Color.FromArgb(158, 112, 76)    // Excel Braun
    };

    /// <inheritdoc />
    public override Color[] DiagramPalette => _diagramPalette;

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
