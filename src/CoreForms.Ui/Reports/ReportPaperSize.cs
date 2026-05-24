namespace CoreForms.Ui.Reports;

/// <summary>
/// Provides predefined paper sizes in centimeters.
/// Dimensions are always in portrait orientation; swap with <see cref="ReportPageSetup.Landscape"/>.
/// </summary>
public static class ReportPaperSize
{
    /// <summary>A3: 29.7 × 42.0 cm</summary>
    public static (Cm Width, Cm Height) A3 => (29.7, 42.0);

    /// <summary>A4: 21.0 × 29.7 cm (German default)</summary>
    public static (Cm Width, Cm Height) A4 => (21.0, 29.7);

    /// <summary>A5: 14.8 × 21.0 cm</summary>
    public static (Cm Width, Cm Height) A5 => (14.8, 21.0);

    /// <summary>Letter: 21.59 × 27.94 cm (US standard)</summary>
    public static (Cm Width, Cm Height) Letter => (21.59, 27.94);

    /// <summary>Legal: 21.59 × 35.56 cm</summary>
    public static (Cm Width, Cm Height) Legal => (21.59, 35.56);
}
