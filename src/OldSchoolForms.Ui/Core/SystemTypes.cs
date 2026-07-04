namespace OldSchoolForms.Ui.Core;

/// <summary>
/// Represents a 32-bit ARGB color value.
/// </summary>
public readonly struct Color
{
    /// <summary>
    /// Gets the red component value (0-255).
    /// </summary>
    public byte R { get; }

    /// <summary>
    /// Gets the green component value (0-255).
    /// </summary>
    public byte G { get; }

    /// <summary>
    /// Gets the blue component value (0-255).
    /// </summary>
    public byte B { get; }

    /// <summary>
    /// Gets the alpha component value (0-255).
    /// </summary>
    public byte A { get; }

    private Color(byte r, byte g, byte b, byte a = 255)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    /// <summary>
    /// Creates a Color from the specified red, green, blue, and alpha component values.
    /// </summary>
    /// <param name="r">The red component (0-255).</param>
    /// <param name="g">The green component (0-255).</param>
    /// <param name="b">The blue component (0-255).</param>
    /// <param name="a">The alpha component (0-255). Defaults to 255 (fully opaque).</param>
    /// <returns>A Color with the specified component values.</returns>
    public static Color FromArgb(int r, int g, int b, int a = 255)
        => new((byte)r, (byte)g, (byte)b, (byte)a);

    /// <summary>
    /// Creates a Color from a 32-bit ARGB integer value.
    /// </summary>
    /// <param name="argb">A 32-bit ARGB integer value.</param>
    /// <returns>A Color from the specified ARGB value.</returns>
    public static Color FromArgb(int argb)
        => new((byte)(argb >> 16), (byte)(argb >> 8), (byte)argb, (byte)(argb >> 24));

    /// <summary>
    /// Represents a color that has no value (transparent black).
    /// </summary>
    public static readonly Color Empty = new(0, 0, 0, 0);

    /// <summary>
    /// Determines whether two Color instances are equal.
    /// </summary>
    public static bool operator ==(Color left, Color right)
        => left.R == right.R && left.G == right.G && left.B == right.B && left.A == right.A;

    /// <summary>
    /// Determines whether two Color instances are not equal.
    /// </summary>
    public static bool operator !=(Color left, Color right)
        => !(left == right);

    /// <summary>
    /// Determines whether this Color is equal to the specified object.
    /// </summary>
    public override bool Equals(object? obj)
        => obj is Color other && this == other;

    /// <summary>
    /// Returns the hash code for this Color.
    /// </summary>
    public override int GetHashCode()
        => HashCode.Combine(R, G, B, A);

    /// <summary>
    /// Represents the color white.
    /// </summary>
    public static readonly Color White = new(255, 255, 255);

    /// <summary>
    /// Represents the color black.
    /// </summary>
    public static readonly Color Black = new(0, 0, 0);

    /// <summary>
    /// Represents the color red.
    /// </summary>
    public static readonly Color Red = new(255, 0, 0);

    /// <summary>
    /// Represents the color green.
    /// </summary>
    public static readonly Color Green = new(0, 255, 0);

    /// <summary>
    /// Represents the color blue.
    /// </summary>
    public static readonly Color Blue = new(0, 0, 255);

    /// <summary>
    /// Represents the color yellow.
    /// </summary>
    public static readonly Color Yellow = new(255, 255, 0);

    /// <summary>
    /// Represents a transparent color.
    /// </summary>
    public static readonly Color Transparent = new(0, 0, 0, 0);

    /// <summary>AliceBlue (240,248,255)</summary>
    public static readonly Color AliceBlue = new(240, 248, 255);
    /// <summary>AntiqueWhite (250,235,215)</summary>
    public static readonly Color AntiqueWhite = new(250, 235, 215);
    /// <summary>Aqua (0,255,255)</summary>
    public static readonly Color Aqua = new(0, 255, 255);
    /// <summary>Aquamarine (127,255,212)</summary>
    public static readonly Color Aquamarine = new(127, 255, 212);
    /// <summary>Azure (240,255,255)</summary>
    public static readonly Color Azure = new(240, 255, 255);
    /// <summary>Beige (245,245,220)</summary>
    public static readonly Color Beige = new(245, 245, 220);
    /// <summary>Bisque (255,228,196)</summary>
    public static readonly Color Bisque = new(255, 228, 196);
    /// <summary>BlanchedAlmond (255,235,205)</summary>
    public static readonly Color BlanchedAlmond = new(255, 235, 205);
    /// <summary>BlueViolet (138,43,226)</summary>
    public static readonly Color BlueViolet = new(138, 43, 226);
    /// <summary>Brown (165,42,42)</summary>
    public static readonly Color Brown = new(165, 42, 42);
    /// <summary>BurlyWood (222,184,135)</summary>
    public static readonly Color BurlyWood = new(222, 184, 135);
    /// <summary>CadetBlue (95,158,160)</summary>
    public static readonly Color CadetBlue = new(95, 158, 160);
    /// <summary>Chartreuse (127,255,0)</summary>
    public static readonly Color Chartreuse = new(127, 255, 0);
    /// <summary>Chocolate (210,105,30)</summary>
    public static readonly Color Chocolate = new(210, 105, 30);
    /// <summary>Coral (255,127,80)</summary>
    public static readonly Color Coral = new(255, 127, 80);
    /// <summary>CornflowerBlue (100,149,237)</summary>
    public static readonly Color CornflowerBlue = new(100, 149, 237);
    /// <summary>Cornsilk (255,248,220)</summary>
    public static readonly Color Cornsilk = new(255, 248, 220);
    /// <summary>Crimson (220,20,60)</summary>
    public static readonly Color Crimson = new(220, 20, 60);
    /// <summary>Cyan (0,255,255)</summary>
    public static readonly Color Cyan = new(0, 255, 255);
    /// <summary>DarkBlue (0,0,139)</summary>
    public static readonly Color DarkBlue = new(0, 0, 139);
    /// <summary>DarkCyan (0,139,139)</summary>
    public static readonly Color DarkCyan = new(0, 139, 139);
    /// <summary>DarkGoldenrod (184,134,11)</summary>
    public static readonly Color DarkGoldenrod = new(184, 134, 11);
    /// <summary>DarkGray (169,169,169)</summary>
    public static readonly Color DarkGray = new(169, 169, 169);
    /// <summary>DarkGreen (0,100,0)</summary>
    public static readonly Color DarkGreen = new(0, 100, 0);
    /// <summary>DarkKhaki (189,183,107)</summary>
    public static readonly Color DarkKhaki = new(189, 183, 107);
    /// <summary>DarkMagenta (139,0,139)</summary>
    public static readonly Color DarkMagenta = new(139, 0, 139);
    /// <summary>DarkOliveGreen (85,107,47)</summary>
    public static readonly Color DarkOliveGreen = new(85, 107, 47);
    /// <summary>DarkOrange (255,140,0)</summary>
    public static readonly Color DarkOrange = new(255, 140, 0);
    /// <summary>DarkOrchid (153,50,204)</summary>
    public static readonly Color DarkOrchid = new(153, 50, 204);
    /// <summary>DarkRed (139,0,0)</summary>
    public static readonly Color DarkRed = new(139, 0, 0);
    /// <summary>DarkSalmon (233,150,122)</summary>
    public static readonly Color DarkSalmon = new(233, 150, 122);
    /// <summary>DarkSeaGreen (143,188,143)</summary>
    public static readonly Color DarkSeaGreen = new(143, 188, 143);
    /// <summary>DarkSlateBlue (72,61,139)</summary>
    public static readonly Color DarkSlateBlue = new(72, 61, 139);
    /// <summary>DarkSlateGray (47,79,79)</summary>
    public static readonly Color DarkSlateGray = new(47, 79, 79);
    /// <summary>DarkTurquoise (0,206,209)</summary>
    public static readonly Color DarkTurquoise = new(0, 206, 209);
    /// <summary>DarkViolet (148,0,211)</summary>
    public static readonly Color DarkViolet = new(148, 0, 211);
    /// <summary>DeepPink (255,20,147)</summary>
    public static readonly Color DeepPink = new(255, 20, 147);
    /// <summary>DeepSkyBlue (0,191,255)</summary>
    public static readonly Color DeepSkyBlue = new(0, 191, 255);
    /// <summary>DimGray (105,105,105)</summary>
    public static readonly Color DimGray = new(105, 105, 105);
    /// <summary>DodgerBlue (30,144,255)</summary>
    public static readonly Color DodgerBlue = new(30, 144, 255);
    /// <summary>Firebrick (178,34,34)</summary>
    public static readonly Color Firebrick = new(178, 34, 34);
    /// <summary>FloralWhite (255,250,240)</summary>
    public static readonly Color FloralWhite = new(255, 250, 240);
    /// <summary>ForestGreen (34,139,34)</summary>
    public static readonly Color ForestGreen = new(34, 139, 34);
    /// <summary>Fuchsia (255,0,255)</summary>
    public static readonly Color Fuchsia = new(255, 0, 255);
    /// <summary>Gainsboro (220,220,220)</summary>
    public static readonly Color Gainsboro = new(220, 220, 220);
    /// <summary>GhostWhite (248,248,255)</summary>
    public static readonly Color GhostWhite = new(248, 248, 255);
    /// <summary>Gold (255,215,0)</summary>
    public static readonly Color Gold = new(255, 215, 0);
    /// <summary>Goldenrod (218,165,32)</summary>
    public static readonly Color Goldenrod = new(218, 165, 32);
    /// <summary>Gray (128,128,128)</summary>
    public static readonly Color Gray = new(128, 128, 128);
    /// <summary>GreenYellow (173,255,47)</summary>
    public static readonly Color GreenYellow = new(173, 255, 47);
    /// <summary>Honeydew (240,255,240)</summary>
    public static readonly Color Honeydew = new(240, 255, 240);
    /// <summary>HotPink (255,105,180)</summary>
    public static readonly Color HotPink = new(255, 105, 180);
    /// <summary>IndianRed (205,92,92)</summary>
    public static readonly Color IndianRed = new(205, 92, 92);
    /// <summary>Indigo (75,0,130)</summary>
    public static readonly Color Indigo = new(75, 0, 130);
    /// <summary>Ivory (255,255,240)</summary>
    public static readonly Color Ivory = new(255, 255, 240);
    /// <summary>Khaki (240,230,140)</summary>
    public static readonly Color Khaki = new(240, 230, 140);
    /// <summary>Lavender (230,230,250)</summary>
    public static readonly Color Lavender = new(230, 230, 250);
    /// <summary>LavenderBlush (255,240,245)</summary>
    public static readonly Color LavenderBlush = new(255, 240, 245);
    /// <summary>LawnGreen (124,252,0)</summary>
    public static readonly Color LawnGreen = new(124, 252, 0);
    /// <summary>LemonChiffon (255,250,205)</summary>
    public static readonly Color LemonChiffon = new(255, 250, 205);
    /// <summary>LightBlue (173,216,230)</summary>
    public static readonly Color LightBlue = new(173, 216, 230);
    /// <summary>LightCoral (240,128,128)</summary>
    public static readonly Color LightCoral = new(240, 128, 128);
    /// <summary>LightCyan (224,255,255)</summary>
    public static readonly Color LightCyan = new(224, 255, 255);
    /// <summary>LightGoldenrodYellow (250,250,210)</summary>
    public static readonly Color LightGoldenrodYellow = new(250, 250, 210);
    /// <summary>LightGray (211,211,211)</summary>
    public static readonly Color LightGray = new(211, 211, 211);
    /// <summary>LightGreen (144,238,144)</summary>
    public static readonly Color LightGreen = new(144, 238, 144);
    /// <summary>LightPink (255,182,193)</summary>
    public static readonly Color LightPink = new(255, 182, 193);
    /// <summary>LightSalmon (255,160,122)</summary>
    public static readonly Color LightSalmon = new(255, 160, 122);
    /// <summary>LightSeaGreen (32,178,170)</summary>
    public static readonly Color LightSeaGreen = new(32, 178, 170);
    /// <summary>LightSkyBlue (135,206,250)</summary>
    public static readonly Color LightSkyBlue = new(135, 206, 250);
    /// <summary>LightSlateGray (119,136,153)</summary>
    public static readonly Color LightSlateGray = new(119, 136, 153);
    /// <summary>LightSteelBlue (176,196,222)</summary>
    public static readonly Color LightSteelBlue = new(176, 196, 222);
    /// <summary>LightYellow (255,255,224)</summary>
    public static readonly Color LightYellow = new(255, 255, 224);
    /// <summary>Lime (0,255,0)</summary>
    public static readonly Color Lime = new(0, 255, 0);
    /// <summary>LimeGreen (50,205,50)</summary>
    public static readonly Color LimeGreen = new(50, 205, 50);
    /// <summary>Linen (250,240,230)</summary>
    public static readonly Color Linen = new(250, 240, 230);
    /// <summary>Magenta (255,0,255)</summary>
    public static readonly Color Magenta = new(255, 0, 255);
    /// <summary>Maroon (128,0,0)</summary>
    public static readonly Color Maroon = new(128, 0, 0);
    /// <summary>MediumAquamarine (102,205,170)</summary>
    public static readonly Color MediumAquamarine = new(102, 205, 170);
    /// <summary>MediumBlue (0,0,205)</summary>
    public static readonly Color MediumBlue = new(0, 0, 205);
    /// <summary>MediumOrchid (186,85,211)</summary>
    public static readonly Color MediumOrchid = new(186, 85, 211);
    /// <summary>MediumPurple (147,112,219)</summary>
    public static readonly Color MediumPurple = new(147, 112, 219);
    /// <summary>MediumSeaGreen (60,179,113)</summary>
    public static readonly Color MediumSeaGreen = new(60, 179, 113);
    /// <summary>MediumSlateBlue (123,104,238)</summary>
    public static readonly Color MediumSlateBlue = new(123, 104, 238);
    /// <summary>MediumSpringGreen (0,250,154)</summary>
    public static readonly Color MediumSpringGreen = new(0, 250, 154);
    /// <summary>MediumTurquoise (72,209,204)</summary>
    public static readonly Color MediumTurquoise = new(72, 209, 204);
    /// <summary>MediumVioletRed (199,21,133)</summary>
    public static readonly Color MediumVioletRed = new(199, 21, 133);
    /// <summary>MidnightBlue (25,25,112)</summary>
    public static readonly Color MidnightBlue = new(25, 25, 112);
    /// <summary>MintCream (245,255,250)</summary>
    public static readonly Color MintCream = new(245, 255, 250);
    /// <summary>MistyRose (255,228,225)</summary>
    public static readonly Color MistyRose = new(255, 228, 225);
    /// <summary>Moccasin (255,228,181)</summary>
    public static readonly Color Moccasin = new(255, 228, 181);
    /// <summary>NavajoWhite (255,222,173)</summary>
    public static readonly Color NavajoWhite = new(255, 222, 173);
    /// <summary>Navy (0,0,128)</summary>
    public static readonly Color Navy = new(0, 0, 128);
    /// <summary>OldLace (253,245,230)</summary>
    public static readonly Color OldLace = new(253, 245, 230);
    /// <summary>Olive (128,128,0)</summary>
    public static readonly Color Olive = new(128, 128, 0);
    /// <summary>OliveDrab (107,142,35)</summary>
    public static readonly Color OliveDrab = new(107, 142, 35);
    /// <summary>Orange (255,165,0)</summary>
    public static readonly Color Orange = new(255, 165, 0);
    /// <summary>OrangeRed (255,69,0)</summary>
    public static readonly Color OrangeRed = new(255, 69, 0);
    /// <summary>Orchid (218,112,214)</summary>
    public static readonly Color Orchid = new(218, 112, 214);
    /// <summary>PaleGoldenrod (238,232,170)</summary>
    public static readonly Color PaleGoldenrod = new(238, 232, 170);
    /// <summary>PaleGreen (152,251,152)</summary>
    public static readonly Color PaleGreen = new(152, 251, 152);
    /// <summary>PaleTurquoise (175,238,238)</summary>
    public static readonly Color PaleTurquoise = new(175, 238, 238);
    /// <summary>PaleVioletRed (219,112,147)</summary>
    public static readonly Color PaleVioletRed = new(219, 112, 147);
    /// <summary>PapayaWhip (255,239,213)</summary>
    public static readonly Color PapayaWhip = new(255, 239, 213);
    /// <summary>PeachPuff (255,218,185)</summary>
    public static readonly Color PeachPuff = new(255, 218, 185);
    /// <summary>Peru (205,133,63)</summary>
    public static readonly Color Peru = new(205, 133, 63);
    /// <summary>Pink (255,192,203)</summary>
    public static readonly Color Pink = new(255, 192, 203);
    /// <summary>Plum (221,160,221)</summary>
    public static readonly Color Plum = new(221, 160, 221);
    /// <summary>PowderBlue (176,224,230)</summary>
    public static readonly Color PowderBlue = new(176, 224, 230);
    /// <summary>Purple (128,0,128)</summary>
    public static readonly Color Purple = new(128, 0, 128);
    /// <summary>RebeccaPurple (102,51,153)</summary>
    public static readonly Color RebeccaPurple = new(102, 51, 153);
    /// <summary>RosyBrown (188,143,143)</summary>
    public static readonly Color RosyBrown = new(188, 143, 143);
    /// <summary>RoyalBlue (65,105,225)</summary>
    public static readonly Color RoyalBlue = new(65, 105, 225);
    /// <summary>SaddleBrown (139,69,19)</summary>
    public static readonly Color SaddleBrown = new(139, 69, 19);
    /// <summary>Salmon (250,128,114)</summary>
    public static readonly Color Salmon = new(250, 128, 114);
    /// <summary>SandyBrown (244,164,96)</summary>
    public static readonly Color SandyBrown = new(244, 164, 96);
    /// <summary>SeaGreen (46,139,87)</summary>
    public static readonly Color SeaGreen = new(46, 139, 87);
    /// <summary>SeaShell (255,245,238)</summary>
    public static readonly Color SeaShell = new(255, 245, 238);
    /// <summary>Sienna (160,82,45)</summary>
    public static readonly Color Sienna = new(160, 82, 45);
    /// <summary>Silver (192,192,192)</summary>
    public static readonly Color Silver = new(192, 192, 192);
    /// <summary>SkyBlue (135,206,235)</summary>
    public static readonly Color SkyBlue = new(135, 206, 235);
    /// <summary>SlateBlue (106,90,205)</summary>
    public static readonly Color SlateBlue = new(106, 90, 205);
    /// <summary>SlateGray (112,128,144)</summary>
    public static readonly Color SlateGray = new(112, 128, 144);
    /// <summary>Snow (255,250,250)</summary>
    public static readonly Color Snow = new(255, 250, 250);
    /// <summary>SpringGreen (0,255,127)</summary>
    public static readonly Color SpringGreen = new(0, 255, 127);
    /// <summary>SteelBlue (70,130,180)</summary>
    public static readonly Color SteelBlue = new(70, 130, 180);
    /// <summary>Tan (210,180,140)</summary>
    public static readonly Color Tan = new(210, 180, 140);
    /// <summary>Teal (0,128,128)</summary>
    public static readonly Color Teal = new(0, 128, 128);
    /// <summary>Thistle (216,191,216)</summary>
    public static readonly Color Thistle = new(216, 191, 216);
    /// <summary>Tomato (255,99,71)</summary>
    public static readonly Color Tomato = new(255, 99, 71);
    /// <summary>Turquoise (64,224,208)</summary>
    public static readonly Color Turquoise = new(64, 224, 208);
    /// <summary>Violet (238,130,238)</summary>
    public static readonly Color Violet = new(238, 130, 238);
    /// <summary>Wheat (245,222,179)</summary>
    public static readonly Color Wheat = new(245, 222, 179);
    /// <summary>WhiteSmoke (245,245,245)</summary>
    public static readonly Color WhiteSmoke = new(245, 245, 245);
    /// <summary>YellowGreen (154,205,50)</summary>
    public static readonly Color YellowGreen = new(154, 205, 50);

    /// <summary>
    /// Converts this Color to a 32-bit ARGB integer.
    /// </summary>
    /// <returns>A 32-bit integer representing the color.</returns>
    public int ToArgb() => (A << 24) | (R << 16) | (G << 8) | B;
}

/// <summary>
/// Represents an ordered pair of integers, typically representing width and height.
/// </summary>
public readonly struct Size
{
    /// <summary>
    /// Gets the width component of the Size.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets the height component of the Size.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Initializes a new instance of the Size structure with the specified width and height.
    /// </summary>
    /// <param name="width">The width component.</param>
    /// <param name="height">The height component.</param>
    public Size(int width, int height)
    {
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Represents a Size with zero width and height.
    /// </summary>
    public static readonly Size Empty = new(0, 0);

    /// <summary>
    /// Returns a string representation of this Size in the format "[Width, Height]".
    /// </summary>
    /// <returns>A string representation of this Size.</returns>
    public override string ToString() => $"[{Width}, {Height}]";
}

/// <summary>
/// Represents the padding (inner margin) of a control.
/// </summary>
public struct Padding
{
    /// <summary>
    /// Gets the left padding value.
    /// </summary>
    public int Left { get; }

    /// <summary>
    /// Gets the top padding value.
    /// </summary>
    public int Top { get; }

    /// <summary>
    /// Gets the right padding value.
    /// </summary>
    public int Right { get; }

    /// <summary>
    /// Gets the bottom padding value.
    /// </summary>
    public int Bottom { get; }

    /// <summary>
    /// Initializes a new Padding with the same value for all sides.
    /// </summary>
    /// <param name="all">The padding value for all sides.</param>
    public Padding(int all)
    {
        Left = Top = Right = Bottom = all;
    }

    /// <summary>
    /// Initializes a new Padding with the specified values for each side.
    /// </summary>
    /// <param name="left">The left padding value.</param>
    /// <param name="top">The top padding value.</param>
    /// <param name="right">The right padding value.</param>
    /// <param name="bottom">The bottom padding value.</param>
    public Padding(int left, int top, int right, int bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    /// <summary>
    /// Gets the total horizontal padding (left + right).
    /// </summary>
    public int Horizontal => Left + Right;

    /// <summary>
    /// Gets the total vertical padding (top + bottom).
    /// </summary>
    public int Vertical => Top + Bottom;

    /// <summary>
    /// Represents a Padding with all values set to zero.
    /// </summary>
    public static readonly Padding Empty = new Padding(0);
}

/// <summary>
/// Represents an ordered pair of integer x and y coordinates.
/// </summary>
public readonly struct Point
{
    /// <summary>
    /// Gets the x-coordinate.
    /// </summary>
    public int X { get; }

    /// <summary>
    /// Gets the y-coordinate.
    /// </summary>
    public int Y { get; }

    /// <summary>
    /// Initializes a new instance of the Point structure with the specified coordinates.
    /// </summary>
    /// <param name="x">The x-coordinate.</param>
    /// <param name="y">The y-coordinate.</param>
    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Represents a Point at coordinates (0, 0).
    /// </summary>
    public static readonly Point Empty = new(0, 0);

    /// <summary>
    /// Returns a string representation of this Point in the format "[X, Y]".
    /// </summary>
    /// <returns>A string representation of this Point.</returns>
    public override string ToString() => $"[{X}, {Y}]";
}

/// <summary>
/// Represents the position and size of a rectangle.
/// </summary>
public readonly struct Rectangle
{
    /// <summary>
    /// Gets the x-coordinate of the rectangle's left edge.
    /// </summary>
    public int X { get; }

    /// <summary>
    /// Gets the y-coordinate of the rectangle's top edge.
    /// </summary>
    public int Y { get; }

    /// <summary>
    /// Gets the width of the rectangle.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets the height of the rectangle.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Initializes a new instance of the Rectangle structure with the specified location and size.
    /// </summary>
    /// <param name="x">The x-coordinate of the rectangle's left edge.</param>
    /// <param name="y">The y-coordinate of the rectangle's top edge.</param>
    /// <param name="width">The width of the rectangle.</param>
    /// <param name="height">The height of the rectangle.</param>
    public Rectangle(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Gets the x-coordinate of the left edge of the rectangle.
    /// </summary>
    public int Left => X;

    /// <summary>
    /// Gets the y-coordinate of the top edge of the rectangle.
    /// </summary>
    public int Top => Y;

    /// <summary>
    /// Gets the x-coordinate of the right edge of the rectangle (X + Width).
    /// </summary>
    public int Right => X + Width;

    /// <summary>
    /// Gets the y-coordinate of the bottom edge of the rectangle (Y + Height).
    /// </summary>
    public int Bottom => Y + Height;

    /// <summary>
    /// Gets the coordinates of the upper-left corner of the rectangle.
    /// </summary>
    public Point Location
    {
        get => new Point(X, Y);
    }

    /// <summary>
    /// Gets the size of the rectangle.
    /// </summary>
    public Size Size
    {
        get => new Size(Width, Height);
    }

    /// <summary>
    /// Determines whether the specified point (x, y) is contained within this rectangle.
    /// </summary>
    /// <param name="x">The x-coordinate of the point.</param>
    /// <param name="y">The y-coordinate of the point.</param>
    /// <returns>True if the point is contained within the rectangle; otherwise, false.</returns>
    public bool Contains(int x, int y)
        => x >= X && x < X + Width && y >= Y && y < Y + Height;

    /// <summary>
    /// Determines whether the specified Point is contained within this rectangle.
    /// </summary>
    /// <param name="point">The Point to test.</param>
    /// <returns>True if the point is contained within the rectangle; otherwise, false.</returns>
    public bool Contains(Point point)
        => Contains(point.X, point.Y);

    /// <summary>
    /// Determines whether the specified rectangle is entirely contained within this rectangle.
    /// </summary>
    /// <param name="rect">The rectangle to test.</param>
    /// <returns>True if the rectangle is entirely contained; otherwise, false.</returns>
    public bool Contains(Rectangle rect)
        => rect.X >= X && rect.Right <= Right &&
           rect.Y >= Y && rect.Bottom <= Bottom;

    /// <summary>
    /// Determines whether this rectangle intersects with the specified rectangle.
    /// </summary>
    /// <param name="rect">The rectangle to test.</param>
    /// <returns>True if the rectangles intersect; otherwise, false.</returns>
    public bool IntersectsWith(Rectangle rect)
        => X < rect.X + rect.Width && X + Width > rect.X &&
           Y < rect.Y + rect.Height && Y + Height > rect.Y;

    /// <summary>
    /// Represents a Rectangle with all values set to zero.
    /// </summary>
    public static readonly Rectangle Empty = new(0, 0, 0, 0);

    /// <summary>
    /// Returns a string representation of this Rectangle.
    /// </summary>
    /// <returns>A string representation of this Rectangle.</returns>
    public override string ToString() => $"[{X}, {Y}, {Width}, {Height}]";
}

/// <summary>
/// Defines the font used to display text.
/// </summary>
public class Font
{
    /// <summary>
    /// Gets the font family name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the font size in points.
    /// </summary>
    public float Size { get; }

    /// <summary>
    /// Gets the font style.
    /// </summary>
    public FontStyle Style { get; }

    /// <summary>
    /// Initializes a new instance of the Font class with the specified name, size, and style.
    /// </summary>
    /// <param name="name">The font family name.</param>
    /// <param name="size">The font size in points.</param>
    /// <param name="style">The font style. Defaults to regular.</param>
    public Font(string name, float size, FontStyle style = FontStyle.Regular)
    {
        Name = name;
        Size = size;
        Style = style;
    }

    /// <summary>
    /// Represents the default font (Arial, 14pt, Regular).
    /// </summary>
    public static readonly Font Default = new("Arial", 14f);
}

/// <summary>
/// Specifies the style of the font.
/// </summary>
[Flags]
public enum FontStyle
{
    /// <summary>
    /// Regular font style.
    /// </summary>
    Regular = 0,

    /// <summary>
    /// Bold font style.
    /// </summary>
    Bold = 1,

    /// <summary>
    /// Italic font style.
    /// </summary>
    Italic = 2,

    /// <summary>
    /// Underlined font style.
    /// </summary>
    Underline = 4,

    /// <summary>
    /// Strikethrough font style.
    /// </summary>
    Strikeout = 8
}

/// <summary>
/// Provides predefined system colors.
/// </summary>
public static class SystemColors
{
    /// <summary>
    /// Gets the default control background color.
    /// </summary>
    public static readonly Color Control = Color.FromArgb(212, 208, 200);

    /// <summary>
    /// Gets the default control text color.
    /// </summary>
    public static readonly Color ControlText = Color.FromArgb(0, 0, 0);

    /// <summary>
    /// Gets the default window background color.
    /// </summary>
    public static readonly Color Window = Color.FromArgb(255, 255, 255);

    /// <summary>
    /// Gets the default window text color.
    /// </summary>
    public static readonly Color WindowText = Color.FromArgb(0, 0, 0);

    /// <summary>
    /// Gets the highlight color for selected items.
    /// </summary>
    public static readonly Color Highlight = Color.FromArgb(10, 36, 99);

    /// <summary>
    /// Gets the text color for highlighted items.
    /// </summary>
    public static readonly Color HighlightText = Color.FromArgb(255, 255, 255);

    /// <summary>
    /// Gets the active caption bar color.
    /// </summary>
    public static readonly Color ActiveCaption = Color.FromArgb(10, 36, 99);

    /// <summary>
    /// Gets the text color for active caption bars.
    /// </summary>
    public static readonly Color ActiveCaptionText = Color.FromArgb(255, 255, 255);

    /// <summary>
    /// Gets the inactive caption bar color.
    /// </summary>
    public static readonly Color InactiveCaption = Color.FromArgb(128, 128, 128);

    /// <summary>
    /// Gets the lighter control background color.
    /// </summary>
    public static readonly Color ControlLight = Color.FromArgb(240, 240, 240);

    /// <summary>
    /// Gets the darker control border color.
    /// </summary>
    public static readonly Color ControlDark = Color.FromArgb(160, 160, 160);

    /// <summary>
    /// Gets the disabled text color.
    /// </summary>
    public static readonly Color GrayText = Color.FromArgb(128, 128, 128);
}

/// <summary>
/// Specifies which mouse button was pressed.
/// </summary>
public enum MouseButtons
{
    /// <summary>
    /// No mouse button was pressed.
    /// </summary>
    None,

    /// <summary>
    /// The left mouse button was pressed.
    /// </summary>
    Left,

    /// <summary>
    /// The right mouse button was pressed.
    /// </summary>
    Right,

    /// <summary>
    /// The middle mouse button was pressed.
    /// </summary>
    Middle,

    /// <summary>
    /// The first X button was pressed.
    /// </summary>
    XButton1,

    /// <summary>
    /// The second X button was pressed.
    /// </summary>
    XButton2
}

/// <summary>
/// Provides data for mouse events.
/// </summary>
public class MouseEventArgs : EventArgs
{
    /// <summary>
    /// Gets which mouse button was pressed.
    /// </summary>
    public MouseButtons Button { get; }

    /// <summary>
    /// Gets the number of times the button was clicked.
    /// </summary>
    public int Clicks { get; }

    /// <summary>
    /// Gets the x-coordinate of the mouse cursor relative to the control.
    /// </summary>
    public int X { get; }

    /// <summary>
    /// Gets the y-coordinate of the mouse cursor relative to the control.
    /// </summary>
    public int Y { get; }

    /// <summary>
    /// Gets the wheel delta (positive for scrolling up, negative for scrolling down).
    /// Each notch is typically ±1; this accumulates fractional values from smooth-scroll devices.
    /// </summary>
    public float Delta { get; }

    /// <summary>
    /// Initializes a new instance of MouseEventArgs.
    /// </summary>
    /// <param name="button">The mouse button that was pressed.</param>
    /// <param name="clicks">The number of times the button was clicked.</param>
    /// <param name="x">The x-coordinate relative to the control.</param>
    /// <param name="y">The y-coordinate relative to the control.</param>
    /// <param name="delta">The wheel delta (float, e.g. ±1 per notch).</param>
    public MouseEventArgs(MouseButtons button, int clicks, int x, int y, float delta)
    {
        Button = button;
        Clicks = clicks;
        X = x;
        Y = y;
        Delta = delta;
    }
}

/// <summary>
/// Specifies a key on the keyboard.
/// </summary>
public enum Keys
{
    /// <summary>
    /// No key.
    /// </summary>
    None = 0,

    /// <summary>
    /// The BACKSPACE key.
    /// </summary>
    Back = 8,

    /// <summary>
    /// The TAB key.
    /// </summary>
    Tab = 9,

    /// <summary>
    /// The ENTER key.
    /// </summary>
    Enter = 13,

    /// <summary>
    /// The ESCAPE key.
    /// </summary>
    Escape = 27,

    /// <summary>
    /// The SPACEBAR key.
    /// </summary>
    Space = 32,

    /// <summary>
    /// The MENU key (Alt key).
    /// </summary>
    Menu = 18,

    /// <summary>
    /// The PAGE UP key.
    /// </summary>
    PageUp = 33,

    /// <summary>
    /// The PAGE DOWN key.
    /// </summary>
    PageDown = 34,

    /// <summary>
    /// The END key.
    /// </summary>
    End = 35,

    /// <summary>
    /// The HOME key.
    /// </summary>
    Home = 36,

    /// <summary>
    /// The LEFT ARROW key.
    /// </summary>
    Left = 37,

    /// <summary>
    /// The UP ARROW key.
    /// </summary>
    Up = 38,

    /// <summary>
    /// The RIGHT ARROW key.
    /// </summary>
    Right = 39,

    /// <summary>
    /// The DOWN ARROW key.
    /// </summary>
    Down = 40,

    /// <summary>
    /// The INSERT key.
    /// </summary>
    Insert = 45,

    /// <summary>
    /// The DELETE key.
    /// </summary>
    Delete = 46,

    /// <summary>
    /// The '0' key.
    /// </summary>
    D0 = 48,

    /// <summary>
    /// The '1' key.
    /// </summary>
    D1 = 49,

    /// <summary>
    /// The '2' key.
    /// </summary>
    D2 = 50,

    /// <summary>
    /// The '3' key.
    /// </summary>
    D3 = 51,

    /// <summary>
    /// The '4' key.
    /// </summary>
    D4 = 52,

    /// <summary>
    /// The '5' key.
    /// </summary>
    D5 = 53,

    /// <summary>
    /// The '6' key.
    /// </summary>
    D6 = 54,

    /// <summary>
    /// The '7' key.
    /// </summary>
    D7 = 55,

    /// <summary>
    /// The '8' key.
    /// </summary>
    D8 = 56,

    /// <summary>
    /// The '9' key.
    /// </summary>
    D9 = 57,

    /// <summary>
    /// The 'A' key.
    /// </summary>
    A = 65,

    /// <summary>
    /// The 'B' key.
    /// </summary>
    B = 66,

    /// <summary>
    /// The 'C' key.
    /// </summary>
    C = 67,

    /// <summary>
    /// The 'D' key.
    /// </summary>
    D = 68,

    /// <summary>
    /// The 'E' key.
    /// </summary>
    E = 69,

    /// <summary>
    /// The 'F' key.
    /// </summary>
    F = 70,

    /// <summary>
    /// The 'G' key.
    /// </summary>
    G = 71,

    /// <summary>
    /// The 'H' key.
    /// </summary>
    H = 72,

    /// <summary>
    /// The 'I' key.
    /// </summary>
    I = 73,

    /// <summary>
    /// The 'J' key.
    /// </summary>
    J = 74,

    /// <summary>
    /// The 'K' key.
    /// </summary>
    K = 75,

    /// <summary>
    /// The 'L' key.
    /// </summary>
    L = 76,

    /// <summary>
    /// The 'M' key.
    /// </summary>
    M = 77,

    /// <summary>
    /// The 'N' key.
    /// </summary>
    N = 78,

    /// <summary>
    /// The 'O' key.
    /// </summary>
    O = 79,

    /// <summary>
    /// The 'P' key.
    /// </summary>
    P = 80,

    /// <summary>
    /// The 'Q' key.
    /// </summary>
    Q = 81,

    /// <summary>
    /// The 'R' key.
    /// </summary>
    R = 82,

    /// <summary>
    /// The 'S' key.
    /// </summary>
    S = 83,

    /// <summary>
    /// The 'T' key.
    /// </summary>
    T = 84,

    /// <summary>
    /// The 'U' key.
    /// </summary>
    U = 85,

    /// <summary>
    /// The 'V' key.
    /// </summary>
    V = 86,

    /// <summary>
    /// The 'W' key.
    /// </summary>
    W = 87,

    /// <summary>
    /// The 'X' key.
    /// </summary>
    X = 88,

    /// <summary>
    /// The 'Y' key.
    /// </summary>
    Y = 89,

    /// <summary>
    /// The 'Z' key.
    /// </summary>
    Z = 90,

    /// <summary>
    /// The F1 function key.
    /// </summary>
    F1 = 112,

    /// <summary>
    /// The F2 function key.
    /// </summary>
    F2 = 113,

    /// <summary>
    /// The F3 function key.
    /// </summary>
    F3 = 114,

    /// <summary>
    /// The F4 function key.
    /// </summary>
    F4 = 115,

    /// <summary>
    /// The F5 function key.
    /// </summary>
    F5 = 116,

    /// <summary>
    /// The F6 function key.
    /// </summary>
    F6 = 117,

    /// <summary>
    /// The F7 function key.
    /// </summary>
    F7 = 118,

    /// <summary>
    /// The F8 function key.
    /// </summary>
    F8 = 119,

    /// <summary>
    /// The F9 function key.
    /// </summary>
    F9 = 120,

    /// <summary>
    /// The F10 function key.
    /// </summary>
    F10 = 121,

    /// <summary>
    /// The F11 function key.
    /// </summary>
    F11 = 122,

    /// <summary>
    /// The F12 function key.
    /// </summary>
    F12 = 123
}

/// <summary>
/// Specifies modifier keys that are pressed.
/// </summary>
[Flags]
public enum ModifierKeys
{
    /// <summary>
    /// No modifier keys.
    /// </summary>
    None = 0,

    /// <summary>
    /// The ALT key.
    /// </summary>
    Alt = 1,

    /// <summary>
    /// The CTRL key.
    /// </summary>
    Control = 2,

    /// <summary>
    /// The SHIFT key.
    /// </summary>
    Shift = 4,

    /// <summary>
    /// The Windows logo key.
    /// </summary>
    Win = 8
}

/// <summary>
/// Provides data for keyboard events.
/// </summary>
public class KeyEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the key code of the pressed key.
    /// </summary>
    public Keys KeyCode { get; set; }

    /// <summary>
    /// Gets or sets the modifier keys that are pressed.
    /// </summary>
    public ModifierKeys Modifiers { get; set; }

    /// <summary>
    /// Gets or sets whether the event has been handled.
    /// </summary>
    public bool Handled { get; set; }
}

/// <summary>
/// Provides data for key press events.
/// </summary>
public class KeyPressEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the character that was pressed.
    /// </summary>
    public char KeyChar { get; set; }

    /// <summary>
    /// Gets or sets whether the event has been handled.
    /// </summary>
    public bool Handled { get; set; }
}

/// <summary>
/// Provides data for events that can be cancelled.
/// </summary>
public class CancelEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets whether the operation should be cancelled.
    /// </summary>
    public bool Cancel { get; set; }

    /// <summary>
    /// Initializes a new instance of CancelEventArgs with the specified cancel value.
    /// </summary>
    /// <param name="cancel">Whether to cancel the operation.</param>
    public CancelEventArgs(bool cancel = false)
    {
        Cancel = cancel;
    }
}

/// <summary>
/// Provides data for text input events.
/// </summary>
public class TextInputEventArgs : EventArgs
{
    /// <summary>
    /// Gets the input text.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Initializes a new instance of TextInputEventArgs with the specified text.
    /// </summary>
    /// <param name="text">The input text.</param>
    public TextInputEventArgs(string text)
    {
        Text = text;
    }
}