using System.Globalization;

namespace OldSchoolForms.Ui.Reports;

/// <summary>
/// Represents a measurement in centimeters. Provides arithmetic operations,
/// comparison, and conversions to pixels (screen) and points (PDF/print).
/// All report positions, widths, and heights use this type.
/// </summary>
public readonly struct Cm : IComparable<Cm>, IEquatable<Cm>
{
    /// <summary>
    /// Gets the numeric value in centimeters.
    /// </summary>
    public double Value { get; }

    /// <summary>
    /// Initializes a new Cm value.
    /// </summary>
    /// <param name="value">The measurement in centimeters.</param>
    public Cm(double value)
    {
        Value = value;
    }

    /// <summary>
    /// Represents a Cm value of zero.
    /// </summary>
    public static Cm Zero => new(0);

    /// <summary>
    /// Creates a Cm value from millimeters.
    /// </summary>
    /// <param name="mm">The measurement in millimeters.</param>
    /// <returns>A Cm value representing the specified millimeters.</returns>
    public static Cm FromMm(double mm) => new(mm / 10.0);

    /// <summary>
    /// Creates a Cm value from inches.
    /// </summary>
    /// <param name="inches">The measurement in inches.</param>
    /// <returns>A Cm value representing the specified inches.</returns>
    public static Cm FromInches(double inches) => new(inches * 2.54);

    /// <summary>
    /// Converts this measurement to pixels at the specified DPI.
    /// Screen use: 96 DPI (default). PDF use: 72 DPI.
    /// </summary>
    /// <param name="dpi">The dots-per-inch resolution. Defaults to 96 (screen).</param>
    /// <returns>The pixel value rounded to the nearest integer.</returns>
    public int ToPixels(float dpi = 96) => (int)Math.Round(Value * dpi / 2.54);

    /// <summary>
    /// Converts this measurement to a floating-point pixel value at the specified DPI.
    /// </summary>
    /// <param name="dpi">The dots-per-inch resolution.</param>
    /// <returns>The pixel value as a float.</returns>
    public float ToPixelF(float dpi = 96) => (float)(Value * dpi / 2.54);

    /// <summary>
    /// Converts this measurement to PDF points (1 point = 1/72 inch).
    /// </summary>
    /// <returns>The measurement in points.</returns>
    public float ToPoints() => (float)(Value * 72.0 / 2.54);

    /// <summary>
    /// Converts this measurement to hundredths of an inch (for PageSettings compatibility).
    /// </summary>
    public int ToHundredthsInch() => (int)Math.Round(Value / 2.54 * 100);

    public int CompareTo(Cm other) => Value.CompareTo(other.Value);

    public bool Equals(Cm other) => Value.Equals(other.Value);

    public override bool Equals(object? obj) => obj is Cm other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => $"{Value.ToString("F2", CultureInfo.CurrentCulture)} cm";

    public static implicit operator Cm(double value) => new(value);

    public static explicit operator double(Cm cm) => cm.Value;

    public static Cm operator +(Cm a, Cm b) => new(a.Value + b.Value);

    public static Cm operator -(Cm a, Cm b) => new(a.Value - b.Value);

    public static Cm operator *(Cm a, double b) => new(a.Value * b);

    public static Cm operator /(Cm a, double b) => new(a.Value / b);

    public static Cm operator -(Cm a) => new(-a.Value);

    public static bool operator ==(Cm left, Cm right) => left.Value == right.Value;

    public static bool operator !=(Cm left, Cm right) => left.Value != right.Value;

    public static bool operator <(Cm left, Cm right) => left.Value < right.Value;

    public static bool operator >(Cm left, Cm right) => left.Value > right.Value;

    public static bool operator <=(Cm left, Cm right) => left.Value <= right.Value;

    public static bool operator >=(Cm left, Cm right) => left.Value >= right.Value;

    public static Cm Max(Cm a, Cm b) => a.Value >= b.Value ? a : b;

    public static Cm Min(Cm a, Cm b) => a.Value <= b.Value ? a : b;
}
