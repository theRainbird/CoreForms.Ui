namespace CoreForms.Ui.Core;

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
    /// Gets the inactive caption bar color.
    /// </summary>
    public static readonly Color InactiveCaption = Color.FromArgb(128, 128, 128);
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
    /// </summary>
    public int Delta { get; }

    /// <summary>
    /// Initializes a new instance of MouseEventArgs.
    /// </summary>
    /// <param name="button">The mouse button that was pressed.</param>
    /// <param name="clicks">The number of times the button was clicked.</param>
    /// <param name="x">The x-coordinate relative to the control.</param>
    /// <param name="y">The y-coordinate relative to the control.</param>
    /// <param name="delta">The wheel delta.</param>
    public MouseEventArgs(MouseButtons button, int clicks, int x, int y, int delta)
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