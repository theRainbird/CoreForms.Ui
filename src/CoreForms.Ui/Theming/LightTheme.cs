using CoreForms.Ui.Core;

namespace CoreForms.Ui.Theming;

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
