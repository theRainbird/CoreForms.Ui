namespace CoreForms.Ui.Core;

/// <summary>
/// Specifies the margins of a printed page, in hundredths of an inch.
/// </summary>
public class Margins
{
    /// <summary>
    /// Initializes a new instance with zero margins.
    /// </summary>
    public Margins() : this(0, 0, 0, 0) { }

    /// <summary>
    /// Initializes a new instance with the specified margins.
    /// </summary>
    public Margins(int left, int right, int top, int bottom)
    {
        Left = left;
        Right = right;
        Top = top;
        Bottom = bottom;
    }

    /// <summary>
    /// Gets or sets the left margin, in hundredths of an inch.
    /// </summary>
    public int Left { get; set; }

    /// <summary>
    /// Gets or sets the right margin, in hundredths of an inch.
    /// </summary>
    public int Right { get; set; }

    /// <summary>
    /// Gets or sets the top margin, in hundredths of an inch.
    /// </summary>
    public int Top { get; set; }

    /// <summary>
    /// Gets or sets the bottom margin, in hundredths of an inch.
    /// </summary>
    public int Bottom { get; set; }

    /// <summary>
    /// Returns a clone of this Margins instance.
    /// </summary>
    public Margins Clone() => new(Left, Right, Top, Bottom);

    public override bool Equals(object? obj) =>
        obj is Margins m && m.Left == Left && m.Right == Right && m.Top == Top && m.Bottom == Bottom;

    public override int GetHashCode() => HashCode.Combine(Left, Right, Top, Bottom);

    public static bool operator ==(Margins? a, Margins? b) =>
        a?.Left == b?.Left && a?.Right == b?.Right && a?.Top == b?.Top && a?.Bottom == b?.Bottom;

    public static bool operator !=(Margins? a, Margins? b) => !(a == b);
}
