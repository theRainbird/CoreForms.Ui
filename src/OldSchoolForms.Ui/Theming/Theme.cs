using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OldSchoolForms.Ui.Core;

namespace OldSchoolForms.Ui.Theming;

/// <summary>
/// Abstract base class for UI themes, defining colors and fonts for all controls.
/// </summary>
public abstract class Theme
{
    /// <summary>
    /// Gets the name of the theme.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of Theme with the specified name.
    /// </summary>
    /// <param name="name">The name of the theme.</param>
    protected Theme(string name)
    {
        Name = name;
    }

    // Core system colors
    /// <summary>
    /// Gets the default control background color.
    /// </summary>
    public abstract Color ControlBackground { get; }

    /// <summary>
    /// Gets the default control text color.
    /// </summary>
    public abstract Color ControlText { get; }

    /// <summary>
    /// Gets the default window background color.
    /// </summary>
    public abstract Color WindowBackground { get; }

    /// <summary>
    /// Gets the default window text color.
    /// </summary>
    public abstract Color WindowText { get; }

    /// <summary>
    /// Gets the highlight color for selected items.
    /// </summary>
    public abstract Color Highlight { get; }

    /// <summary>
    /// Gets the text color for highlighted items.
    /// </summary>
    public abstract Color HighlightText { get; }

    /// <summary>
    /// Gets the active caption bar color.
    /// </summary>
    public abstract Color ActiveCaption { get; }

    /// <summary>
    /// Gets the text color for active caption bars.
    /// </summary>
    public abstract Color ActiveCaptionText { get; }

    /// <summary>
    /// Gets the inactive caption bar color.
    /// </summary>
    public abstract Color InactiveCaption { get; }

    /// <summary>
    /// Gets the lighter control background color.
    /// </summary>
    public abstract Color ControlLight { get; }

    /// <summary>
    /// Gets the darker control border color.
    /// </summary>
    public abstract Color ControlDark { get; }

    /// <summary>
    /// Gets the disabled text color.
    /// </summary>
    public abstract Color GrayText { get; }

    // Extended theme colors
    /// <summary>
    /// Gets the default border color.
    /// </summary>
    public abstract Color Border { get; }

    /// <summary>
    /// Gets the focus indicator color.
    /// </summary>
    public abstract Color FocusIndicator { get; }

    /// <summary>
    /// Gets the progress bar fill color.
    /// </summary>
    public abstract Color ProgressBarFill { get; }

    /// <summary>
    /// Gets the scrollbar track color.
    /// </summary>
    public abstract Color ScrollbarTrack { get; }

    /// <summary>
    /// Gets the scrollbar thumb color.
    /// </summary>
    public abstract Color ScrollbarThumb { get; }

    /// <summary>
    /// Gets the scrollbar thumb border color.
    /// </summary>
    public abstract Color ScrollbarThumbBorder { get; }

    /// <summary>
    /// Gets the scrollbar border color.
    /// </summary>
    public abstract Color ScrollbarBorder { get; }

    /// <summary>
    /// Gets the horizontal grid line color.
    /// </summary>
    public abstract Color GridLine { get; }

    /// <summary>
    /// Gets the vertical grid line color.
    /// </summary>
    public abstract Color GridLineVertical { get; }

    /// <summary>
    /// Gets the alternate row background color.
    /// </summary>
    public abstract Color AlternateRow { get; }

    /// <summary>
    /// Gets the hover highlight color.
    /// </summary>
    public abstract Color HoverHighlight { get; }

    /// <summary>
    /// Gets the checkbox background color.
    /// </summary>
    public abstract Color CheckboxBackground { get; }

    /// <summary>
    /// Gets the checkbox border color.
    /// </summary>
    public abstract Color CheckboxBorder { get; }

    /// <summary>
    /// Gets the checkbox checkmark color.
    /// </summary>
    public abstract Color CheckboxCheck { get; }

    /// <summary>
    /// Gets the textbox background color.
    /// </summary>
    public abstract Color TextBoxBackground { get; }

    /// <summary>
    /// Gets the textbox text color.
    /// </summary>
    public abstract Color TextBoxText { get; }

    /// <summary>
    /// Gets the textbox border color.
    /// </summary>
    public abstract Color TextBoxBorder { get; }

    /// <summary>
    /// Gets the textbox focus border color.
    /// </summary>
    public abstract Color TextBoxFocusBorder { get; }

    /// <summary>
    /// Gets the content area background color (e.g., TreeView, ListBox).
    /// </summary>
    public abstract Color ContentBackground { get; }

    /// <summary>
    /// Gets the tab header background color.
    /// </summary>
    public abstract Color TabHeaderBackground { get; }

    /// <summary>
    /// Gets the selected tab background color.
    /// </summary>
    public abstract Color TabSelectedBackground { get; }

    /// <summary>
    /// Gets the selected tab text color.
    /// </summary>
    public abstract Color TabSelectedText { get; }

    /// <summary>
    /// Gets the unselected tab background color.
    /// </summary>
    public abstract Color TabUnselectedBackground { get; }

    /// <summary>
    /// Gets the unselected tab text color.
    /// </summary>
    public abstract Color TabUnselectedText { get; }

    /// <summary>
    /// Gets the tab separator color.
    /// </summary>
    public abstract Color TabSeparator { get; }

    /// <summary>
    /// Gets the selected tab border color.
    /// </summary>
    public abstract Color TabSelectedBorder { get; }

    /// <summary>
    /// Gets the tab content area background color.
    /// </summary>
    public abstract Color TabContentBackground { get; }

    /// <summary>
    /// Gets the menu separator color.
    /// </summary>
    public abstract Color MenuSeparator { get; }

    /// <summary>
    /// Gets the menu hover background color.
    /// </summary>
    public abstract Color MenuHover { get; }

    /// <summary>
    /// Gets the menu dropdown background color.
    /// </summary>
    public abstract Color MenuDropdownBackground { get; }

    /// <summary>
    /// Gets the menu dropdown border color.
    /// </summary>
    public abstract Color MenuDropdownBorder { get; }

    /// <summary>
    /// Gets the menu dropdown hover color.
    /// </summary>
    public abstract Color MenuDropdownHover { get; }

    /// <summary>
    /// Gets the DataGridView border color.
    /// </summary>
    public abstract Color DataGridViewBorder { get; }

    /// <summary>
    /// Gets the DataGridView header separator color.
    /// </summary>
    public abstract Color DataGridViewHeaderSeparator { get; }

    /// <summary>
    /// Gets the DataGridView header text color.
    /// </summary>
    public abstract Color DataGridViewHeaderText { get; }

    /// <summary>
    /// Gets the DataGridView row header text color.
    /// </summary>
    public abstract Color DataGridViewRowHeaderText { get; }

    /// <summary>
    /// Gets the DataGridView cell text color.
    /// </summary>
    public abstract Color DataGridViewCellText { get; }

    /// <summary>
    /// Gets the DataGridView add-new-row background color.
    /// </summary>
    public abstract Color DataGridViewAddNewRowBackground { get; }

    /// <summary>
    /// Gets the DataGridView add-new-row separator color.
    /// </summary>
    public abstract Color DataGridViewAddNewRowSeparator { get; }

    /// <summary>
    /// Gets the DataGridView add-new-row asterisk color.
    /// </summary>
    public abstract Color DataGridViewAddNewRowAsterisk { get; }

    /// <summary>
    /// Gets the color for the DataGridView sort arrow glyph in column headers.
    /// </summary>
    public abstract Color DataGridViewSortArrow { get; }
    public abstract Color DataGridViewGroupHeaderBackground { get; }
    public abstract Color DataGridViewGroupHeaderText { get; }
    public abstract Color DataGridViewGroupingBarBackground { get; }
    public abstract Color DataGridViewGroupingBarText { get; }
    public abstract Color DataGridViewGroupingBarPillBackground { get; }

    /// <summary>
    /// Gets the PivotTable header background color.
    /// </summary>
    public abstract Color PivotTableHeaderBackground { get; }

    /// <summary>
    /// Gets the PivotTable header text color.
    /// </summary>
    public abstract Color PivotTableHeaderText { get; }

    /// <summary>
    /// Gets the PivotTable total row/column background color.
    /// </summary>
    public abstract Color PivotTableTotalBackground { get; }

    /// <summary>
    /// Gets the PivotTable alternating row background color.
    /// </summary>
    public abstract Color PivotTableAlternateRow { get; }

    /// <summary>
    /// Gets the PivotTable grid line color.
    /// </summary>
    public abstract Color PivotTableGridLine { get; }

    /// <summary>
    /// Gets the PivotTable selected cell background color.
    /// </summary>
    public abstract Color PivotTableSelection { get; }

    /// <summary>
    /// Gets the TableLayoutPanel grid line color.
    /// </summary>
    public abstract Color TableLayoutGridLine { get; }

    /// <summary>
    /// Gets the panel border color.
    /// </summary>
    public abstract Color PanelBorder { get; }

    /// <summary>
    /// Gets the message box overlay dimming color.
    /// </summary>
    public abstract Color MessageBoxOverlay { get; }

    /// <summary>
    /// Gets the message box border color.
    /// </summary>
    public abstract Color MessageBoxBorder { get; }

    /// <summary>
    /// Gets the button hover background color.
    /// </summary>
    public abstract Color ButtonHoverBackground { get; }

    /// <summary>
    /// Gets the button pressed background color.
    /// </summary>
    public abstract Color ButtonPressedBackground { get; }

    /// <summary>
    /// Gets the button border color.
    /// </summary>
    public abstract Color ButtonBorder { get; }

    /// <summary>
    /// Gets the combobox dropdown arrow color.
    /// </summary>
    public abstract Color ComboBoxDropdownArrow { get; }

    /// <summary>
    /// Gets the combobox dropdown button separator color.
    /// </summary>
    public abstract Color ComboBoxDropdownButtonSeparator { get; }

    /// <summary>
    /// Gets the ToolStripItem default text color.
    /// </summary>
    public abstract Color ToolStripItemText { get; }

    /// <summary>
    /// Gets the StatusStrip top line color.
    /// </summary>
    public abstract Color StatusStripTopLine { get; }

    /// <summary>
    /// Gets the cursor line color.
    /// </summary>
    public abstract Color CursorLine { get; }

    /// <summary>
    /// Gets the dark separator line color used by SeperatorControl.
    /// </summary>
    public abstract Color SeparatorDark { get; }

    /// <summary>
    /// Gets the light separator line color used by SeperatorControl.
    /// </summary>
    public abstract Color SeparatorLight { get; }

    // Calendar colors
    /// <summary>
    /// Gets the calendar header background color.
    /// </summary>
    public abstract Color CalendarHeaderBackground { get; }

    /// <summary>
    /// Gets the background color for today's date cell.
    /// </summary>
    public abstract Color CalendarTodayHighlight { get; }

    /// <summary>
    /// Gets the border color for the selected date cell.
    /// </summary>
    public abstract Color CalendarSelectedBorder { get; }

    /// <summary>
    /// Gets the background color for weekend day cells.
    /// </summary>
    public abstract Color CalendarWeekendBackground { get; }

    /// <summary>
    /// Gets the background color for work hours in week/day views.
    /// </summary>
    public abstract Color CalendarWorkHoursBackground { get; }

    /// <summary>
    /// Gets the default color for appointment bars.
    /// </summary>
    public abstract Color CalendarAppointmentDefault { get; }

    /// <summary>
    /// Gets the time ruler text color.
    /// </summary>
    public abstract Color CalendarTimeRulerText { get; }

    /// <summary>
    /// Gets the day header background color.
    /// </summary>
    public abstract Color CalendarDayHeaderBackground { get; }

    // Diagram colors
    /// <summary>
    /// Gets the diagram chart area background color.
    /// </summary>
    public abstract Color DiagramBackground { get; }

    /// <summary>
    /// Gets the diagram grid line color.
    /// </summary>
    public abstract Color DiagramGridLine { get; }

    /// <summary>
    /// Gets the diagram axis line color.
    /// </summary>
    public abstract Color DiagramAxisLine { get; }

    /// <summary>
    /// Gets the diagram axis label text color.
    /// </summary>
    public abstract Color DiagramAxisLabel { get; }

    /// <summary>
    /// Gets the diagram legend background color.
    /// </summary>
    public abstract Color DiagramLegendBackground { get; }

    /// <summary>
    /// Gets the default color palette for diagram series fills (pastel/muted colors).
    /// </summary>
    public abstract Color[] DiagramPalette { get; }

    // Fonts
    /// <summary>
    /// Gets the default font for controls.
    /// </summary>
    public abstract Font DefaultFont { get; }

    /// <summary>
    /// Gets the font for headings and titles.
    /// </summary>
    public abstract Font HeadingFont { get; }

    /// <summary>
    /// Gets the font for small text.
    /// </summary>
    public abstract Font SmallFont { get; }

    /// <summary>
    /// Gets the monospace font for code/terminal.
    /// </summary>
    public abstract Font MonospaceFont { get; }

    /// <summary>
    /// Resolves the first available font from the specified preference list.
    /// </summary>
    /// <param name="preferredNames">Ordered list of preferred font family names.</param>
    /// <param name="size">The font size in points.</param>
    /// <param name="style">The font style.</param>
    /// <returns>A Font with the first available family name, or Font.Default if none found.</returns>
    protected static Font ResolveFont(IEnumerable<string> preferredNames, float size, FontStyle style = FontStyle.Regular)
    {
        var availableFonts = GetAvailableFontFamilies();
        foreach (var name in preferredNames)
        {
            if (availableFonts.Contains(name, StringComparer.OrdinalIgnoreCase))
                return new Font(name, size, style);
        }
        return Font.Default;
    }

    private static HashSet<string> GetAvailableFontFamilies()
    {
        var families = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var dirs = GetFontDirectories();
        foreach (var dir in dirs)
        {
            if (!Directory.Exists(dir)) continue;
            try
            {
                foreach (var file in Directory.EnumerateFiles(dir, "*.ttf"))
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    families.Add(name);
                }
                foreach (var file in Directory.EnumerateFiles(dir, "*.otf"))
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    families.Add(name);
                }
            }
            catch
            {
                // Ignore inaccessible directories
            }
        }
        return families;
    }

    private static IEnumerable<string> GetFontDirectories()
    {
        if (OperatingSystem.IsWindows())
        {
            yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts");
        }
        else if (OperatingSystem.IsLinux())
        {
            yield return "/usr/share/fonts";
            yield return "/usr/local/share/fonts";
            yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".fonts");
            yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "fonts");
        }
        else
        {
            yield return "/usr/share/fonts";
            yield return "/System/Library/Fonts";
            yield return "/Library/Fonts";
        }
    }
}
