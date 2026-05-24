namespace CoreForms.Ui.Reports;

/// <summary>
/// Configures the page layout for a <see cref="Report"/>: paper size, orientation,
/// and margins. All measurements are in centimeters (<see cref="Cm"/>).
/// Defaults are A4 portrait with 2.0 cm margins (common German standard).
/// </summary>
public class ReportPageSetup
{
    private Cm _portraitWidth = 21.0;
    private Cm _portraitHeight = 29.7;
    private bool _landscape;

    /// <summary>
    /// Gets or sets the paper width in cm, accounting for orientation.
    /// In portrait mode this is the shorter side; in landscape mode the longer side.
    /// </summary>
    public Cm PaperWidth => _landscape ? _portraitHeight : _portraitWidth;

    /// <summary>
    /// Gets or sets the paper height in cm, accounting for orientation.
    /// In portrait mode this is the longer side; in landscape mode the shorter side.
    /// </summary>
    public Cm PaperHeight => _landscape ? _portraitWidth : _portraitHeight;

    /// <summary>
    /// Gets the page width without orientation swapping (always the portrait width).
    /// </summary>
    public Cm PortraitWidth => _portraitWidth;

    /// <summary>
    /// Gets the page height without orientation swapping (always the portrait height).
    /// </summary>
    public Cm PortraitHeight => _portraitHeight;

    /// <summary>
    /// Gets or sets whether the page is in landscape orientation.
    /// When true, <see cref="PaperWidth"/> and <see cref="PaperHeight"/> swap.
    /// </summary>
    public bool Landscape
    {
        get => _landscape;
        set => _landscape = value;
    }

    /// <summary>
    /// Gets or sets the left margin in cm (default 2.0).
    /// </summary>
    public Cm LeftMargin { get; set; } = 2.0;

    /// <summary>
    /// Gets or sets the right margin in cm (default 2.0).
    /// </summary>
    public Cm RightMargin { get; set; } = 2.0;

    /// <summary>
    /// Gets or sets the top margin in cm (default 2.0).
    /// </summary>
    public Cm TopMargin { get; set; } = 2.0;

    /// <summary>
    /// Gets or sets the bottom margin in cm (default 2.0).
    /// </summary>
    public Cm BottomMargin { get; set; } = 2.0;

    /// <summary>
    /// Gets the printable width (paper width minus left and right margins).
    /// </summary>
    public Cm PrintableWidth => PaperWidth - LeftMargin - RightMargin;

    /// <summary>
    /// Gets the printable height (paper height minus top and bottom margins).
    /// </summary>
    public Cm PrintableHeight => PaperHeight - TopMargin - BottomMargin;

    /// <summary>
    /// Sets the paper size using portrait dimensions.
    /// </summary>
    /// <param name="portraitWidth">Width in portrait orientation (cm).</param>
    /// <param name="portraitHeight">Height in portrait orientation (cm).</param>
    public void SetPaperSize(Cm portraitWidth, Cm portraitHeight)
    {
        _portraitWidth = portraitWidth;
        _portraitHeight = portraitHeight;
    }

    /// <summary>
    /// Sets the paper size from a <see cref="ReportPaperSize"/> tuple.
    /// </summary>
    public void SetPaperSize((Cm Width, Cm Height) paperSize)
    {
        _portraitWidth = paperSize.Width;
        _portraitHeight = paperSize.Height;
    }

    /// <summary>
    /// Sets the paper size to a standard size and optionally sets orientation.
    /// </summary>
    public void SetPaperSize((Cm Width, Cm Height) paperSize, bool landscape)
    {
        _portraitWidth = paperSize.Width;
        _portraitHeight = paperSize.Height;
        _landscape = landscape;
    }

    /// <summary>
    /// Sets uniform margins on all four sides.
    /// </summary>
    public void SetMargins(Cm all)
    {
        LeftMargin = RightMargin = TopMargin = BottomMargin = all;
    }

    /// <summary>
    /// Sets margins individually.
    /// </summary>
    public void SetMargins(Cm left, Cm right, Cm top, Cm bottom)
    {
        LeftMargin = left;
        RightMargin = right;
        TopMargin = top;
        BottomMargin = bottom;
    }

    /// <summary>
    /// Returns a clone of this setup.
    /// </summary>
    public ReportPageSetup Clone() => new()
    {
        _portraitWidth = _portraitWidth,
        _portraitHeight = _portraitHeight,
        _landscape = _landscape,
        LeftMargin = LeftMargin,
        RightMargin = RightMargin,
        TopMargin = TopMargin,
        BottomMargin = BottomMargin
    };
}
