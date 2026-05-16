using CoreForms.Ui.Core;

namespace CoreForms.Ui.Rendering;

/// <summary>
    /// Provides methods for rendering graphics primitives using a command-list pattern.
    /// </summary>
    public class Graphics : IDisposable
    {
        private float _offsetX;
        private float _offsetY;
        private float _zoom = 1.0f;
    private readonly Stack<Matrix> _transforms = new();
    private readonly Stack<Rectangle> _clipStack = new();
    private readonly List<DrawCommand> _commands = new();
    private bool _disposed;

    /// <summary>
    /// Gets the current x-offset for transformations.
    /// </summary>
    public float OffsetX => _offsetX;

    /// <summary>
    /// Gets the current y-offset for transformations.
    /// </summary>
    public float OffsetY => _offsetY;

    /// <summary>
    /// Gets or sets the zoom factor for scaling all drawing operations.
    /// </summary>
    public float Zoom
    {
        get => _zoom;
        set => _zoom = value > 0 ? value : 1.0f;
    }

    /// <summary>
    /// Delegate for measuring text dimensions.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="font">The font to use.</param>
    /// <param name="zoom">The zoom factor.</param>
    /// <returns>A tuple with width and height.</returns>
    public delegate (int width, int height) MeasureTextCallback(string text, Font font, float zoom);

    /// <summary>
    /// Gets or sets the callback for measuring text dimensions.
    /// Set by the platform layer to enable accurate text measurement.
    /// </summary>
    public MeasureTextCallback? MeasureText { get; set; }

    /// <summary>
    /// Measures the specified text using the current font and zoom.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="font">The font to use.</param>
    /// <param name="zoom">The zoom factor. Defaults to the current graphics zoom if not specified.</param>
    /// <returns>A tuple containing (width, height) in pixels.</returns>
    public (int width, int height) MeasureString(string text, Font font, float zoom = 1.0f)
    {
        if (MeasureText != null)
            return MeasureText(text, font, zoom);

        float scaledSize = font.Size * zoom;
        return ((int)(text.Length * scaledSize * 0.6f), (int)scaledSize);
    }

    /// <summary>
    /// Gets the current clip bounds, if any.
    /// </summary>
    public Rectangle? ClipBounds => _clipStack.Count > 0 ? _clipStack.Peek() : null;

    /// <summary>
    /// Sets the clipping region to the specified rectangle.
    /// </summary>
    /// <param name="rect">The clipping rectangle.</param>
    public void SetClip(Rectangle rect)
    {
        _clipStack.Push(new Rectangle(
            (int)((rect.X + _offsetX) * _zoom),
            (int)((rect.Y + _offsetY) * _zoom),
            (int)(rect.Width * _zoom),
            (int)(rect.Height * _zoom)));
    }

    /// <summary>
    /// Removes the current clipping region.
    /// </summary>
    public void ResetClip()
    {
        if (_clipStack.Count > 0)
            _clipStack.Pop();
    }

    /// <summary>
    /// Translates the coordinate system by the specified offset.
    /// </summary>
    /// <param name="dx">The x-offset.</param>
    /// <param name="dy">The y-offset.</param>
    public void TranslateTransform(float dx, float dy)
    {
        _offsetX += dx;
        _offsetY += dy;
    }

    /// <summary>
    /// Saves the current graphics state (transform and offset).
    /// </summary>
    public void Save()
    {
        _transforms.Push(new Matrix(_offsetX, _offsetY));
    }

    /// <summary>
    /// Restores the graphics state to the previously saved state.
    /// </summary>
    public void Restore()
    {
        if (_transforms.Count > 0)
        {
            var matrix = _transforms.Pop();
            _offsetX = matrix.OffsetX;
            _offsetY = matrix.OffsetY;
        }
    }

    /// <summary>
    /// Clears the rendering surface with the specified color.
    /// </summary>
    /// <param name="color">The color to use for clearing.</param>
    public void Clear(Color color)
    {
        _commands.Add(new DrawCommand { Type = DrawCommandType.Clear, Color = color });
    }

    /// <summary>
    /// Draws a filled rectangle.
    /// </summary>
    /// <param name="color">The fill color.</param>
    /// <param name="x">The x-coordinate.</param>
    /// <param name="y">The y-coordinate.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    public void FillRectangle(Color color, float x, float y, float width, float height)
    {
        _commands.Add(new DrawCommand
        {
            Type = DrawCommandType.FillRectangle,
            Color = color,
            X = (x + _offsetX) * _zoom,
            Y = (y + _offsetY) * _zoom,
            Width = width * _zoom,
            Height = height * _zoom,
            ClipBounds = ClipBounds
        });
    }

    /// <summary>
    /// Draws a rectangle outline.
    /// </summary>
    /// <param name="color">The outline color.</param>
    /// <param name="x">The x-coordinate.</param>
    /// <param name="y">The y-coordinate.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <param name="lineWidth">The line width.</param>
    public void DrawRectangle(Color color, float x, float y, float width, float height, float lineWidth = 1f)
    {
        _commands.Add(new DrawCommand
        {
            Type = DrawCommandType.DrawRectangle,
            Color = color,
            X = (x + _offsetX) * _zoom,
            Y = (y + _offsetY) * _zoom,
            Width = width * _zoom,
            Height = height * _zoom,
            LineWidth = lineWidth * _zoom,
            ClipBounds = ClipBounds
        });
    }

    /// <summary>
    /// Draws a text string.
    /// </summary>
    /// <param name="text">The text to draw.</param>
    /// <param name="font">The font to use.</param>
    /// <param name="color">The text color.</param>
    /// <param name="x">The x-coordinate.</param>
    /// <param name="y">The y-coordinate.</param>
    public void DrawString(string text, Font font, Color color, float x, float y)
    {
        _commands.Add(new DrawCommand
        {
            Type = DrawCommandType.DrawString,
            Text = text,
            Font = font,
            Color = color,
            X = (x + _offsetX) * _zoom,
            Y = (y + _offsetY) * _zoom,
            Zoom = _zoom,
            ClipBounds = ClipBounds
        });
    }

    /// <summary>
    /// Draws a filled ellipse.
    /// </summary>
    /// <param name="color">The fill color.</param>
    /// <param name="x">The x-coordinate of the bounding rectangle.</param>
    /// <param name="y">The y-coordinate of the bounding rectangle.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    public void FillEllipse(Color color, float x, float y, float width, float height)
    {
        _commands.Add(new DrawCommand
        {
            Type = DrawCommandType.FillEllipse,
            Color = color,
            X = (x + _offsetX) * _zoom,
            Y = (y + _offsetY) * _zoom,
            Width = width * _zoom,
            Height = height * _zoom,
            ClipBounds = ClipBounds
        });
    }

    /// <summary>
    /// Draws a line between two points.
    /// </summary>
    /// <param name="color">The line color.</param>
    /// <param name="x1">The x-coordinate of the start point.</param>
    /// <param name="y1">The y-coordinate of the start point.</param>
    /// <param name="x2">The x-coordinate of the end point.</param>
    /// <param name="y2">The y-coordinate of the end point.</param>
    /// <param name="lineWidth">The line width.</param>
    public void DrawLine(Color color, float x1, float y1, float x2, float y2, float lineWidth = 1f)
    {
        _commands.Add(new DrawCommand
        {
            Type = DrawCommandType.DrawLine,
            Color = color,
            X = (x1 + _offsetX) * _zoom,
            Y = (y1 + _offsetY) * _zoom,
            X2 = (x2 + _offsetX) * _zoom,
            Y2 = (y2 + _offsetY) * _zoom,
            LineWidth = lineWidth * _zoom,
            ClipBounds = ClipBounds
        });
    }

    /// <summary>
    /// Draws an ellipse outline at the specified location and size.
    /// </summary>
    /// <param name="color">The outline color.</param>
    /// <param name="x">The x-coordinate of the bounding rectangle.</param>
    /// <param name="y">The y-coordinate of the bounding rectangle.</param>
    /// <param name="width">The width of the bounding rectangle.</param>
    /// <param name="height">The height of the bounding rectangle.</param>
    /// <param name="lineWidth">The line width. Defaults to 1.</param>
    public void DrawEllipse(Color color, float x, float y, float width, float height, float lineWidth = 1f)
    {
        _commands.Add(new DrawCommand
        {
            Type = DrawCommandType.DrawEllipse,
            Color = color,
            X = (x + _offsetX) * _zoom,
            Y = (y + _offsetY) * _zoom,
            Width = width * _zoom,
            Height = height * _zoom,
            LineWidth = lineWidth * _zoom,
            ClipBounds = ClipBounds
        });
    }

    /// <summary>
    /// Draws an image at the specified location and size.
    /// </summary>
    /// <param name="image">The IGraphicsImage to draw.</param>
    /// <param name="x">The x-coordinate.</param>
    /// <param name="y">The y-coordinate.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    public void DrawImage(IGraphicsImage image, float x, float y, float width, float height)
    {
        _commands.Add(new DrawCommand
        {
            Type = DrawCommandType.DrawImage,
            Image = image,
            X = (x + _offsetX) * _zoom,
            Y = (y + _offsetY) * _zoom,
            Width = width * _zoom,
            Height = height * _zoom,
            ClipBounds = ClipBounds
        });
    }

    /// <summary>
    /// Draws a filled triangle.
    /// </summary>
    /// <param name="color">The fill color.</param>
    /// <param name="x1">The x-coordinate of the first vertex.</param>
    /// <param name="y1">The y-coordinate of the first vertex.</param>
    /// <param name="x2">The x-coordinate of the second vertex.</param>
    /// <param name="y2">The y-coordinate of the second vertex.</param>
    /// <param name="x3">The x-coordinate of the third vertex.</param>
    /// <param name="y3">The y-coordinate of the third vertex.</param>
    public void FillTriangle(Color color, float x1, float y1, float x2, float y2, float x3, float y3)
    {
        _commands.Add(new DrawCommand
        {
            Type = DrawCommandType.FillTriangle,
            Color = color,
            X = (x1 + _offsetX) * _zoom,
            Y = (y1 + _offsetY) * _zoom,
            X2 = (x2 + _offsetX) * _zoom,
            Y2 = (y2 + _offsetY) * _zoom,
            X3 = (x3 + _offsetX) * _zoom,
            Y3 = (y3 + _offsetY) * _zoom,
            ClipBounds = ClipBounds
        });
    }

    /// <summary>
    /// Releases all resources used by this Graphics object.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }

    /// <summary>
    /// Gets the list of draw commands accumulated by this Graphics object.
    /// </summary>
    /// <returns>A list of draw commands.</returns>
    public List<DrawCommand> GetCommands() => new(_commands);
}

/// <summary>
/// Represents a 2D transformation matrix with x and y offsets.
/// </summary>
public class Matrix
{
    /// <summary>
    /// Gets the x-offset.
    /// </summary>
    public float OffsetX { get; }

    /// <summary>
    /// Gets the y-offset.
    /// </summary>
    public float OffsetY { get; }

    /// <summary>
    /// Initializes a new instance of Matrix with the specified offsets.
    /// </summary>
    /// <param name="offsetX">The x-offset.</param>
    /// <param name="offsetY">The y-offset.</param>
    public Matrix(float offsetX, float offsetY)
    {
        OffsetX = offsetX;
        OffsetY = offsetY;
    }
}

/// <summary>
/// Specifies the type of draw command.
/// </summary>
public enum DrawCommandType
{
    /// <summary>
    /// Clear the rendering surface.
    /// </summary>
    Clear,

    /// <summary>
    /// Draw a filled rectangle.
    /// </summary>
    FillRectangle,

    /// <summary>
    /// Draw a rectangle outline.
    /// </summary>
    DrawRectangle,

    /// <summary>
    /// Draw text.
    /// </summary>
    DrawString,

    /// <summary>
    /// Draw a filled ellipse.
    /// </summary>
    FillEllipse,

    /// <summary>
    /// Draw a line.
    /// </summary>
    DrawLine,

    /// <summary>
    /// Draw an image.
    /// </summary>
    DrawImage,

    /// <summary>
    /// Draw an ellipse outline.
    /// </summary>
    DrawEllipse,

    /// <summary>
    /// Draw a filled triangle.
    /// </summary>
    FillTriangle
}

/// <summary>
/// Represents a single draw command for rendering.
/// </summary>
public class DrawCommand
{
    /// <summary>
    /// Gets or sets the type of draw command.
    /// </summary>
    public DrawCommandType Type { get; set; }

    /// <summary>
    /// Gets or sets the color for the command.
    /// </summary>
    public Color Color { get; set; }

    /// <summary>
    /// Gets or sets the x-coordinate.
    /// </summary>
    public float X { get; set; }

    /// <summary>
    /// Gets or sets the y-coordinate.
    /// </summary>
    public float Y { get; set; }

    /// <summary>
    /// Gets or sets the width.
    /// </summary>
    public float Width { get; set; }

    /// <summary>
    /// Gets or sets the height.
    /// </summary>
    public float Height { get; set; }

    /// <summary>
    /// Gets or sets the second x-coordinate (for lines and triangles).
    /// </summary>
    public float X2 { get; set; }

    /// <summary>
    /// Gets or sets the second y-coordinate (for lines and triangles).
    /// </summary>
    public float Y2 { get; set; }

    /// <summary>
    /// Gets or sets the third x-coordinate (for triangles).
    /// </summary>
    public float X3 { get; set; }

    /// <summary>
    /// Gets or sets the third y-coordinate (for triangles).
    /// </summary>
    public float Y3 { get; set; }

    /// <summary>
    /// Gets or sets the line width.
    /// </summary>
    public float LineWidth { get; set; } = 1f;

    /// <summary>
    /// Gets or sets the text (for DrawString commands).
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the font (for DrawString commands).
    /// </summary>
    public Font? Font { get; set; }

    /// <summary>
    /// Gets or sets the image (for DrawImage commands).
    /// </summary>
    public IGraphicsImage? Image { get; set; }

    /// <summary>
    /// Gets or sets the clipping bounds.
    /// </summary>
    public Rectangle? ClipBounds { get; set; }

    /// <summary>
    /// Gets or sets the zoom factor for this command (used for font scaling).
    /// </summary>
    public float Zoom { get; set; } = 1.0f;
}