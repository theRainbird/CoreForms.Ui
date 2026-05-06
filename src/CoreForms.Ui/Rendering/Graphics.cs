using CoreForms.Ui.Core;

namespace CoreForms.Ui.Rendering;

public class Graphics : IDisposable
{
    private float _offsetX;
    private float _offsetY;
    private readonly Stack<Matrix> _transforms = new();
    private readonly Stack<Rectangle> _clipStack = new();
    private readonly List<DrawCommand> _commands = new();
    private bool _disposed;

    public float OffsetX => _offsetX;
    public float OffsetY => _offsetY;
    public Rectangle? ClipBounds => _clipStack.Count > 0 ? _clipStack.Peek() : null;

    public void SetClip(Rectangle rect)
    {
        _clipStack.Push(new Rectangle(
            (int)(rect.X + _offsetX),
            (int)(rect.Y + _offsetY),
            rect.Width,
            rect.Height));
    }

    public void ResetClip()
    {
        if (_clipStack.Count > 0)
            _clipStack.Pop();
    }

    public void TranslateTransform(float dx, float dy)
    {
        _offsetX += dx;
        _offsetY += dy;
    }

    public void Save()
    {
        _transforms.Push(new Matrix(_offsetX, _offsetY));
    }

    public void Restore()
    {
        if (_transforms.Count > 0)
        {
            var matrix = _transforms.Pop();
            _offsetX = matrix.OffsetX;
            _offsetY = matrix.OffsetY;
        }
    }

    public void Clear(Color color)
    {
        _commands.Add(new DrawCommand { Type = DrawCommandType.Clear, Color = color });
    }

    public void FillRectangle(Color color, float x, float y, float width, float height)
    {
        _commands.Add(new DrawCommand
        {
            Type = DrawCommandType.FillRectangle,
            Color = color,
            X = x + _offsetX,
            Y = y + _offsetY,
            Width = width,
            Height = height,
            ClipBounds = ClipBounds
        });
    }

    public void DrawRectangle(Color color, float x, float y, float width, float height, float lineWidth = 1f)
    {
        _commands.Add(new DrawCommand
        {
            Type = DrawCommandType.DrawRectangle,
            Color = color,
            X = x + _offsetX,
            Y = y + _offsetY,
            Width = width,
            Height = height,
            LineWidth = lineWidth,
            ClipBounds = ClipBounds
        });
    }

    public void DrawString(string text, Font font, Color color, float x, float y)
    {
        _commands.Add(new DrawCommand
        {
            Type = DrawCommandType.DrawString,
            Text = text,
            Font = font,
            Color = color,
            X = x + _offsetX,
            Y = y + _offsetY,
            ClipBounds = ClipBounds
        });
    }

    public void FillEllipse(Color color, float x, float y, float width, float height)
    {
        _commands.Add(new DrawCommand
        {
            Type = DrawCommandType.FillEllipse,
            Color = color,
            X = x + _offsetX,
            Y = y + _offsetY,
            Width = width,
            Height = height,
            ClipBounds = ClipBounds
        });
    }

    public void DrawLine(Color color, float x1, float y1, float x2, float y2, float lineWidth = 1f)
    {
        _commands.Add(new DrawCommand
        {
            Type = DrawCommandType.DrawLine,
            Color = color,
            X = x1 + _offsetX,
            Y = y1 + _offsetY,
            X2 = x2 + _offsetX,
            Y2 = y2 + _offsetY,
            LineWidth = lineWidth,
            ClipBounds = ClipBounds
        });
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }

    public List<DrawCommand> GetCommands() => new(_commands);
}

public class Matrix
{
    public float OffsetX { get; }
    public float OffsetY { get; }

    public Matrix(float offsetX, float offsetY)
    {
        OffsetX = offsetX;
        OffsetY = offsetY;
    }
}

public enum DrawCommandType
{
    Clear,
    FillRectangle,
    DrawRectangle,
    DrawString,
    FillEllipse,
    DrawLine,
    DrawImage
}

public class DrawCommand
{
    public DrawCommandType Type { get; set; }
    public Color Color { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public float X2 { get; set; }
    public float Y2 { get; set; }
    public float LineWidth { get; set; } = 1f;
    public string? Text { get; set; }
    public Font? Font { get; set; }
    public object? Image { get; set; }
    public Rectangle? ClipBounds { get; set; }
}