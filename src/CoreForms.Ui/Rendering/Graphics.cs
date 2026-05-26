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
    private readonly List<Rectangle> _clipStack = new();
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
    /// Gets the current clip bounds as the intersection of all active clip regions.
    /// Returns null if no clip is active.
    /// </summary>
    public Rectangle? ClipBounds
    {
        get
        {
            if (_clipStack.Count == 0) return null;
            Rectangle result = _clipStack[0];
            for (int i = 1; i < _clipStack.Count; i++)
            {
                var next = _clipStack[i];
                int x = Math.Max(result.X, next.X);
                int y = Math.Max(result.Y, next.Y);
                int right = Math.Min(result.Right, next.Right);
                int bottom = Math.Min(result.Bottom, next.Bottom);
                if (right <= x || bottom <= y)
                    return Rectangle.Empty;
                result = new Rectangle(x, y, right - x, bottom - y);
            }
            return result;
        }
    }

    /// <summary>
    /// Sets the clipping region to the specified rectangle.
    /// </summary>
    /// <param name="rect">The clipping rectangle.</param>
    public void SetClip(Rectangle rect)
    {
        _clipStack.Add(new Rectangle(
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
            _clipStack.RemoveAt(_clipStack.Count - 1);
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
    /// Draws a filled pie wedge.
    /// </summary>
    /// <param name="color">The fill color.</param>
    /// <param name="x">The x-coordinate of the bounding rectangle.</param>
    /// <param name="y">The y-coordinate of the bounding rectangle.</param>
    /// <param name="width">The width of the bounding rectangle.</param>
    /// <param name="height">The height of the bounding rectangle.</param>
    /// <param name="startAngle">The start angle in degrees from the x-axis.</param>
    /// <param name="sweepAngle">The sweep angle in degrees (positive clockwise).</param>
    public void FillPie(Color color, float x, float y, float width, float height, float startAngle, float sweepAngle)
    {
        _commands.Add(new DrawCommand
        {
            Type = DrawCommandType.FillPie,
            Color = color,
            X = (x + _offsetX) * _zoom,
            Y = (y + _offsetY) * _zoom,
            Width = width * _zoom,
            Height = height * _zoom,
            StartAngle = startAngle,
            SweepAngle = sweepAngle,
            ClipBounds = ClipBounds
        });
    }

    /// <summary>
    /// Draws an arc outline.
    /// </summary>
    /// <param name="color">The outline color.</param>
    /// <param name="x">The x-coordinate of the bounding rectangle.</param>
    /// <param name="y">The y-coordinate of the bounding rectangle.</param>
    /// <param name="width">The width of the bounding rectangle.</param>
    /// <param name="height">The height of the bounding rectangle.</param>
    /// <param name="startAngle">The start angle in degrees from the x-axis.</param>
    /// <param name="sweepAngle">The sweep angle in degrees (positive clockwise).</param>
    /// <param name="lineWidth">The line width.</param>
    public void DrawArc(Color color, float x, float y, float width, float height, float startAngle, float sweepAngle, float lineWidth = 1f)
    {
        _commands.Add(new DrawCommand
        {
            Type = DrawCommandType.DrawArc,
            Color = color,
            X = (x + _offsetX) * _zoom,
            Y = (y + _offsetY) * _zoom,
            Width = width * _zoom,
            Height = height * _zoom,
            StartAngle = startAngle,
            SweepAngle = sweepAngle,
            LineWidth = lineWidth * _zoom,
            ClipBounds = ClipBounds
        });
    }

    /// <summary>
    /// Draws a filled polygon from a flat vertex array.
    /// </summary>
    /// <param name="color">The fill color.</param>
    /// <param name="points">Flat float array [x1,y1,x2,y2,...] of vertex coordinates.</param>
    public void FillPolygon(Color color, float[] points)
    {
        var transformed = new float[points.Length];
        for (int i = 0; i < points.Length; i += 2)
        {
            transformed[i] = (points[i] + _offsetX) * _zoom;
            transformed[i + 1] = (points[i + 1] + _offsetY) * _zoom;
        }
        _commands.Add(new DrawCommand
        {
            Type = DrawCommandType.FillPolygon,
            Color = color,
            Points = transformed,
            ClipBounds = ClipBounds
        });
    }

    /// <summary>
    /// Draws a polygon outline from a flat vertex array.
    /// </summary>
    /// <param name="color">The outline color.</param>
    /// <param name="points">Flat float array [x1,y1,x2,y2,...] of vertex coordinates.</param>
    /// <param name="lineWidth">The line width.</param>
    public void DrawPolygon(Color color, float[] points, float lineWidth = 1f)
    {
        var transformed = new float[points.Length];
        for (int i = 0; i < points.Length; i += 2)
        {
            transformed[i] = (points[i] + _offsetX) * _zoom;
            transformed[i + 1] = (points[i + 1] + _offsetY) * _zoom;
        }
        _commands.Add(new DrawCommand
        {
            Type = DrawCommandType.DrawPolygon,
            Color = color,
            Points = transformed,
            LineWidth = lineWidth * _zoom,
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
    public List<DrawCommand> GetCommands() => _commands;

    /// <summary>
    /// Imports draw commands from a source list, applying both a source scale factor
    /// and the Graphics object's current zoom and offset transforms.
    /// The formula used is: <c>(sourceCoord * sourceScale + addOffset + gfxOffset) * gfxZoom</c>.
    /// This ensures the imported content stays aligned with Graphics-based fills/lines
    /// drawn at the same position, regardless of the current Graphics zoom level.
    /// </summary>
    /// <param name="source">The list of draw commands to import.</param>
    /// <param name="sourceScale">Scale factor applied to source coordinates (e.g. report preview zoom).</param>
    /// <param name="addOffsetX">Additional horizontal offset (e.g. paper corner position).</param>
    /// <param name="addOffsetY">Additional vertical offset.</param>
    public void ImportCommands(List<DrawCommand> source, float sourceScale = 1f, float addOffsetX = 0, float addOffsetY = 0)
    {
        var currentClip = ClipBounds;
        foreach (var cmd in source)
        {
            Rectangle? finalClip = currentClip;
            if (cmd.ClipBounds.HasValue && currentClip.HasValue)
                finalClip = Intersect(cmd.ClipBounds.Value, currentClip.Value);
            else if (cmd.ClipBounds.HasValue)
                finalClip = cmd.ClipBounds;

            float[]? transformedPoints = null;
            if (cmd.Points != null)
            {
                transformedPoints = new float[cmd.Points.Length];
                for (int i = 0; i < cmd.Points.Length; i += 2)
                {
                    transformedPoints[i] = (cmd.Points[i] * sourceScale + addOffsetX + _offsetX) * _zoom;
                    transformedPoints[i + 1] = (cmd.Points[i + 1] * sourceScale + addOffsetY + _offsetY) * _zoom;
                }
            }

            var newCmd = new DrawCommand
            {
                Type = cmd.Type,
                Color = cmd.Color,
                X = (cmd.X * sourceScale + addOffsetX + _offsetX) * _zoom,
                Y = (cmd.Y * sourceScale + addOffsetY + _offsetY) * _zoom,
                Width = cmd.Width * sourceScale * _zoom,
                Height = cmd.Height * sourceScale * _zoom,
                X2 = (cmd.X2 * sourceScale + addOffsetX + _offsetX) * _zoom,
                Y2 = (cmd.Y2 * sourceScale + addOffsetY + _offsetY) * _zoom,
                X3 = (cmd.X3 * sourceScale + addOffsetX + _offsetX) * _zoom,
                Y3 = (cmd.Y3 * sourceScale + addOffsetY + _offsetY) * _zoom,
                LineWidth = cmd.LineWidth * sourceScale * _zoom,
                Text = cmd.Text,
                Font = cmd.Font,
                Image = cmd.Image,
                Zoom = cmd.Zoom * sourceScale * _zoom,
                StartAngle = cmd.StartAngle,
                SweepAngle = cmd.SweepAngle,
                Points = transformedPoints,
                ClipBounds = finalClip
            };
            _commands.Add(newCmd);
        }
    }

    private static Rectangle Intersect(Rectangle a, Rectangle b)
    {
        int x = Math.Max(a.X, b.X);
        int y = Math.Max(a.Y, b.Y);
        int right = Math.Min(a.Right, b.Right);
        int bottom = Math.Min(a.Bottom, b.Bottom);
        if (right <= x || bottom <= y)
            return Rectangle.Empty;
        return new Rectangle(x, y, right - x, bottom - y);
    }
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
    FillTriangle,

    /// <summary>
    /// Draw a filled pie wedge.
    /// </summary>
    FillPie,

    /// <summary>
    /// Draw an arc.
    /// </summary>
    DrawArc,

    /// <summary>
    /// Draw a filled polygon.
    /// </summary>
    FillPolygon,

    /// <summary>
    /// Draw a polygon outline.
    /// </summary>
    DrawPolygon
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

    /// <summary>
    /// Gets or sets the start angle in degrees (for pie and arc commands).
    /// </summary>
    public float StartAngle { get; set; }

    /// <summary>
    /// Gets or sets the sweep angle in degrees (for pie and arc commands).
    /// Positive values sweep clockwise.
    /// </summary>
    public float SweepAngle { get; set; }

    /// <summary>
    /// Gets or sets the polygon vertex data as a flat float array [x1,y1,x2,y2,...].
    /// Used by FillPolygon and DrawPolygon commands.
    /// </summary>
    public float[]? Points { get; set; }
}