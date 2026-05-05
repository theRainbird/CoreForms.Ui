namespace CoreForms.Ui.Core;

public readonly struct Color
{
    public byte R { get; }
    public byte G { get; }
    public byte B { get; }
    public byte A { get; }

    private Color(byte r, byte g, byte b, byte a = 255)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public static Color FromArgb(int r, int g, int b, int a = 255)
        => new((byte)r, (byte)g, (byte)b, (byte)a);

    public static Color FromArgb(int argb)
        => new((byte)(argb >> 16), (byte)(argb >> 8), (byte)argb, (byte)(argb >> 24));

    public static readonly Color Empty = new(0, 0, 0, 0);
    public static readonly Color White = new(255, 255, 255);
    public static readonly Color Black = new(0, 0, 0);
    public static readonly Color Red = new(255, 0, 0);
    public static readonly Color Green = new(0, 255, 0);
    public static readonly Color Blue = new(0, 0, 255);
    public static readonly Color Yellow = new(255, 255, 0);
    public static readonly Color Transparent = new(0, 0, 0, 0);

    public int ToArgb() => (A << 24) | (R << 16) | (G << 8) | B;
}

public readonly struct Size
{
    public int Width { get; }
    public int Height { get; }

    public Size(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public static readonly Size Empty = new(0, 0);

    public override string ToString() => $"[{Width}, {Height}]";
}

public readonly struct Point
{
    public int X { get; }
    public int Y { get; }

    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }

    public static readonly Point Empty = new(0, 0);

    public override string ToString() => $"[{X}, {Y}]";
}

public readonly struct Rectangle
{
    public int X { get; }
    public int Y { get; }
    public int Width { get; }
    public int Height { get; }

    public Rectangle(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public int Left => X;
    public int Top => Y;
    public int Right => X + Width;
    public int Bottom => Y + Height;

    public Point Location
    {
        get => new Point(X, Y);
    }

    public Size Size
    {
        get => new Size(Width, Height);
    }

    public bool Contains(int x, int y)
        => x >= X && x < X + Width && y >= Y && y < Y + Height;

    public bool Contains(Point point)
        => Contains(point.X, point.Y);

    public bool IntersectsWith(Rectangle rect)
        => X < rect.X + rect.Width && X + Width > rect.X &&
           Y < rect.Y + rect.Height && Y + Height > rect.Y;

    public static readonly Rectangle Empty = new(0, 0, 0, 0);

    public override string ToString() => $"[{X}, {Y}, {Width}, {Height}]";
}

public class Font
{
    public string Name { get; }
    public float Size { get; }
    public FontStyle Style { get; }

    public Font(string name, float size, FontStyle style = FontStyle.Regular)
    {
        Name = name;
        Size = size;
        Style = style;
    }

    public static readonly Font Default = new("Arial", 14f);
}

[Flags]
public enum FontStyle
{
    Regular = 0,
    Bold = 1,
    Italic = 2,
    Underline = 4,
    Strikeout = 8
}

public static class SystemColors
{
    public static readonly Color Control = Color.FromArgb(212, 208, 200);
    public static readonly Color ControlText = Color.FromArgb(0, 0, 0);
    public static readonly Color Window = Color.FromArgb(255, 255, 255);
    public static readonly Color WindowText = Color.FromArgb(0, 0, 0);
    public static readonly Color Highlight = Color.FromArgb(10, 36, 99);
    public static readonly Color HighlightText = Color.FromArgb(255, 255, 255);
    public static readonly Color ActiveCaption = Color.FromArgb(10, 36, 99);
    public static readonly Color InactiveCaption = Color.FromArgb(128, 128, 128);
}

public enum MouseButtons
{
    None,
    Left,
    Right,
    Middle,
    XButton1,
    XButton2
}

public class MouseEventArgs : EventArgs
{
    public MouseButtons Button { get; }
    public int Clicks { get; }
    public int X { get; }
    public int Y { get; }
    public int Delta { get; }

    public MouseEventArgs(MouseButtons button, int clicks, int x, int y, int delta)
    {
        Button = button;
        Clicks = clicks;
        X = x;
        Y = y;
        Delta = delta;
    }
}

public enum Keys
{
    None = 0,
    Back = 8,
    Tab = 9,
    Enter = 13,
    Escape = 27,
    Space = 32,
    PageUp = 33,
    PageDown = 34,
    End = 35,
    Home = 36,
    Left = 37,
    Up = 38,
    Right = 39,
    Down = 40,
    Insert = 45,
    Delete = 46,
    D0 = 48,
    D1 = 49,
    D2 = 50,
    D3 = 51,
    D4 = 52,
    D5 = 53,
    D6 = 54,
    D7 = 55,
    D8 = 56,
    D9 = 57,
    A = 65,
    B = 66,
    C = 67,
    D = 68,
    E = 69,
    F = 70,
    G = 71,
    H = 72,
    I = 73,
    J = 74,
    K = 75,
    L = 76,
    M = 77,
    N = 78,
    O = 79,
    P = 80,
    Q = 81,
    R = 82,
    S = 83,
    T = 84,
    U = 85,
    V = 86,
    W = 87,
    X = 88,
    Y = 89,
    Z = 90,
    F1 = 112,
    F2 = 113,
    F3 = 114,
    F4 = 115,
    F5 = 116,
    F6 = 117,
    F7 = 118,
    F8 = 119,
    F9 = 120,
    F10 = 121,
    F11 = 122,
    F12 = 123
}

[Flags]
public enum ModifierKeys
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Win = 8
}

public class KeyEventArgs : EventArgs
{
    public Keys KeyCode { get; set; }
    public ModifierKeys Modifiers { get; set; }
    public bool Handled { get; set; }
}

public class KeyPressEventArgs : EventArgs
{
    public char KeyChar { get; set; }
    public bool Handled { get; set; }
}

public class CancelEventArgs : EventArgs
{
    public bool Cancel { get; set; }

    public CancelEventArgs(bool cancel = false)
    {
        Cancel = cancel;
    }
}

public class TextInputEventArgs : EventArgs
{
    public string Text { get; }

    public TextInputEventArgs(string text)
    {
        Text = text;
    }
}