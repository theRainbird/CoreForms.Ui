namespace CoreForms.Ui.Core;

/// <summary>
/// Specifies the size of a sheet of paper, in hundredths of an inch.
/// </summary>
public class PaperSize
{
    /// <summary>
    /// Initializes a new PaperSize instance.
    /// </summary>
    /// <param name="name">The name of the paper size.</param>
    /// <param name="width">The width in hundredths of an inch.</param>
    /// <param name="height">The height in hundredths of an inch.</param>
    public PaperSize(string name, int width, int height)
    {
        PaperName = name;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Gets or sets the name of the paper size.
    /// </summary>
    public string PaperName { get; set; }

    /// <summary>
    /// Gets or sets the width in hundredths of an inch.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Gets or sets the height in hundredths of an inch.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Gets the width in inches.
    /// </summary>
    public double WidthInInches => Width / 100.0;

    /// <summary>
    /// Gets the height in inches.
    /// </summary>
    public double HeightInInches => Height / 100.0;

    /// <summary>
    /// A4 paper size (210mm × 297mm = 8268 × 11693 hundredths of an inch).
    /// </summary>
    public static PaperSize A4 => new("A4", 8268, 11693);

    /// <summary>
    /// Letter paper size (8.5" × 11" = 850 × 1100 hundredths of an inch).
    /// </summary>
    public static PaperSize Letter => new("Letter", 850, 1100);

    /// <summary>
    /// Legal paper size (8.5" × 14" = 850 × 1400 hundredths of an inch).
    /// </summary>
    public static PaperSize Legal => new("Legal", 850, 1400);

    public override string ToString() => $"{PaperName} ({Width}×{Height})";
}
