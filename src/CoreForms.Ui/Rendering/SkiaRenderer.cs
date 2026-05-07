using System.Runtime.InteropServices;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SkiaSharp;

namespace CoreForms.Ui.Rendering;

/// <summary>
/// Provides SkiaSharp-based rendering for the graphics system.
/// Each window has its own SkiaRenderer instance.
/// Renders to an offscreen SKSurface and blits to the OpenGL framebuffer
/// via a texture and fullscreen quad, avoiding Y-axis issues and GPU context problems.
/// </summary>
public class SkiaRenderer : IDisposable
{
    private readonly IWindow _window;
    private GL? _gl;
    private SKSurface _surface = null!;
    private int _width;
    private int _height;
    private bool _disposed;
    private uint _textureId;
    private bool _textureInitialized;
    private uint _shaderProgram;
    private uint _vao;
    private bool _blitInitialized;

    /// <summary>
    /// Gets the SkiaSharp canvas for direct drawing operations.
    /// </summary>
    public SKCanvas Canvas => _surface?.Canvas!;

    /// <summary>
    /// Initializes a new SkiaRenderer for the specified window.
    /// </summary>
    public SkiaRenderer(IWindow window)
    {
        _window = window;
        _width = window.Size.X;
        _height = window.Size.Y;

        window.MakeCurrent();

        try
        {
            _gl = window.CreateOpenGL();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SkiaRenderer] OpenGL init failed: {ex.Message}");
            _gl = null;
        }

        var info = new SKImageInfo(_width, _height, SKColorType.Bgra8888, SKAlphaType.Premul);
        _surface = SKSurface.Create(info);

        if (_surface == null)
        {
            throw new InvalidOperationException("Failed to create SkiaSharp rendering surface.");
        }

        window.Resize += OnResize;
    }

    private void OnResize(Vector2D<int> size)
    {
        if (size.X > 0 && size.Y > 0 && (size.X != _width || size.Y != _height))
        {
            CreateSurface(size.X, size.Y);
        }
    }

    private void CreateSurface(int width, int height)
    {
        _surface?.Dispose();
        _width = width;
        _height = height;

        var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        _surface = SKSurface.Create(info);

        if (_surface == null)
        {
            throw new InvalidOperationException("Failed to create SkiaSharp rendering surface.");
        }

        _textureInitialized = false;
        _blitInitialized = false;
    }

    /// <summary>
    /// Clears the renderer with the specified color.
    /// </summary>
    public void Clear(Core.Color color)
    {
        _surface.Canvas.Clear(new SKColor(color.R, color.G, color.B, color.A));
    }

    /// <summary>
    /// Fills a rectangle with the specified color.
    /// </summary>
    public void FillRectangle(Core.Color color, float x, float y, float width, float height)
    {
        using var paint = new SKPaint { Color = new SKColor(color.R, color.G, color.B, color.A), IsAntialias = true };
        _surface.Canvas.DrawRect(x, y, width, height, paint);
    }

    /// <summary>
    /// Draws a rectangle outline with the specified line width.
    /// </summary>
    public void DrawRectangle(Core.Color color, float x, float y, float width, float height, float lineWidth = 1f)
    {
        using var paint = new SKPaint
        {
            Color = new SKColor(color.R, color.G, color.B, color.A),
            IsAntialias = true,
            StrokeWidth = lineWidth,
            Style = SKPaintStyle.Stroke
        };
        _surface.Canvas.DrawRect(x, y, width, height, paint);
    }

    /// <summary>
    /// Draws a line between two points.
    /// </summary>
    public void DrawLine(Core.Color color, float x1, float y1, float x2, float y2, float lineWidth = 1f)
    {
        using var paint = new SKPaint
        {
            Color = new SKColor(color.R, color.G, color.B, color.A),
            IsAntialias = true,
            StrokeWidth = lineWidth,
            Style = SKPaintStyle.Stroke,
            StrokeCap = lineWidth >= 3f ? SKStrokeCap.Round : SKStrokeCap.Butt
        };
        _surface.Canvas.DrawLine(x1, y1, x2, y2, paint);
    }

    /// <summary>
    /// Draws a filled triangle.
    /// </summary>
    public void FillTriangle(Core.Color color, float x1, float y1, float x2, float y2, float x3, float y3)
    {
        using var paint = new SKPaint { Color = new SKColor(color.R, color.G, color.B, color.A), IsAntialias = true };
        using var path = new SKPath();
        path.MoveTo(x1, y1);
        path.LineTo(x2, y2);
        path.LineTo(x3, y3);
        path.Close();
        _surface.Canvas.DrawPath(path, paint);
    }

    /// <summary>
    /// Fills an ellipse.
    /// </summary>
    public void FillEllipse(Core.Color color, float x, float y, float width, float height)
    {
        using var paint = new SKPaint { Color = new SKColor(color.R, color.G, color.B, color.A), IsAntialias = true };
        _surface.Canvas.DrawOval(new SKRect(x, y, x + width, y + height), paint);
    }

    /// <summary>
    /// Draws an ellipse outline.
    /// </summary>
    public void DrawEllipse(Core.Color color, float x, float y, float width, float height, float lineWidth = 1f)
    {
        using var paint = new SKPaint
        {
            Color = new SKColor(color.R, color.G, color.B, color.A),
            IsAntialias = true,
            StrokeWidth = lineWidth,
            Style = SKPaintStyle.Stroke
        };
        _surface.Canvas.DrawOval(new SKRect(x, y, x + width, y + height), paint);
    }

    /// <summary>
    /// Draws an image at the specified location and size.
    /// </summary>
    public void DrawImage(SKImage image, float x, float y, float width, float height)
    {
        _surface.Canvas.DrawImage(image, new SKRect(x, y, x + width, y + height));
    }

    /// <summary>
    /// Sets the clipping rectangle.
    /// </summary>
    public void SetClipRect(Core.Rectangle? rect)
    {
        _surface.Canvas.Restore();
        _surface.Canvas.Save();

        if (rect.HasValue)
        {
            var r = rect.Value;
            _surface.Canvas.ClipRect(new SKRect(r.X, r.Y, r.X + r.Width, r.Y + r.Height));
        }
    }

    /// <summary>
    /// Presents the rendered frame to the display.
    /// Flushes SkiaSharp rendering, then blits the offscreen surface to the OpenGL framebuffer.
    /// </summary>
    public void Present()
    {
        if (_disposed) return;
        _surface.Canvas.Flush();

        if (_gl != null)
        {
            BlitToScreen();
        }
    }

    private unsafe void BlitToScreen()
    {
        if (_gl == null) return;

        try
        {
            using var snapshot = _surface.Snapshot();
            using var pixels = snapshot.PeekPixels();
            if (pixels == null) return;

            var pixelData = pixels.GetPixelSpan();
            if (pixelData.IsEmpty) return;

            _gl.Viewport(0, 0, (uint)_width, (uint)_height);
            _gl.Clear(ClearBufferMask.ColorBufferBit);

            if (!_textureInitialized)
            {
                _textureId = _gl.GenTexture();
                _textureInitialized = true;
            }

            _gl.BindTexture(GLEnum.Texture2D, _textureId);
            _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Linear);
            _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Linear);
            _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.ClampToEdge);
            _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)GLEnum.ClampToEdge);

            fixed (void* ptr = pixelData)
            {
                _gl.TexImage2D(GLEnum.Texture2D, 0, (int)GLEnum.Rgba8, (uint)_width, (uint)_height, 0,
                    GLEnum.Bgra, GLEnum.UnsignedByte, ptr);
            }

            if (!_blitInitialized)
            {
                InitBlitShader();
                _blitInitialized = true;
            }

            _gl.UseProgram(_shaderProgram);
            _gl.BindVertexArray(_vao);
            _gl.BindTexture(GLEnum.Texture2D, _textureId);
            _gl.DrawArrays(GLEnum.TriangleStrip, 0, 4);
            _gl.BindVertexArray(0);
        }
        catch { }
    }

    private void InitBlitShader()
    {
        if (_gl == null) return;

        string vertexShaderSource =
            @"#version 330 core
            out vec2 vTexCoord;
            void main() {
                vec2 pos = vec2((gl_VertexID & 2) * 2 - 1, (gl_VertexID & 1) * 2 - 1);
                vTexCoord = vec2((gl_VertexID & 2), 1.0 - (gl_VertexID & 1));
                gl_Position = vec4(pos, 0.0, 1.0);
            }";

        string fragmentShaderSource =
            @"#version 330 core
            in vec2 vTexCoord;
            out vec4 FragColor;
            uniform sampler2D uTexture;
            void main() {
                FragColor = texture(uTexture, vTexCoord);
            }";

        uint vs = _gl.CreateShader(GLEnum.VertexShader);
        _gl.ShaderSource(vs, vertexShaderSource);
        _gl.CompileShader(vs);

        uint fs = _gl.CreateShader(GLEnum.FragmentShader);
        _gl.ShaderSource(fs, fragmentShaderSource);
        _gl.CompileShader(fs);

        _shaderProgram = _gl.CreateProgram();
        _gl.AttachShader(_shaderProgram, vs);
        _gl.AttachShader(_shaderProgram, fs);
        _gl.LinkProgram(_shaderProgram);

        _gl.DeleteShader(vs);
        _gl.DeleteShader(fs);

        _vao = _gl.GenVertexArray();
        _gl.BindVertexArray(_vao);
        _gl.BindVertexArray(0);
    }

    /// <summary>
    /// Cleans up OpenGL resources (texture, shader, VAO) while the GL context is still valid.
    /// Called before the window is closed to prevent resource leaks.
    /// </summary>
    public void CleanupGLResources()
    {
        if (_gl == null) return;

        try
        {
            if (_textureInitialized)
            {
                _gl.DeleteTexture(_textureId);
                _textureInitialized = false;
            }

            if (_blitInitialized)
            {
                _gl.DeleteProgram(_shaderProgram);
                _gl.DeleteVertexArray(_vao);
                _blitInitialized = false;
            }
        }
        catch { }
    }

    /// <summary>
    /// Notifies the renderer that the GL context is being destroyed.
    /// Sets the GL reference to null to prevent any future GL calls.
    /// </summary>
    public void MarkContextLost()
    {
        _gl = null;
    }

    /// <summary>
    /// Releases all resources used by this SkiaRenderer.
    /// Disposes the SkiaSharp surface. GL resources must be cleaned up
    /// via CleanupGLResources() before the GL context is destroyed.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _window.Resize -= OnResize;
        _surface?.Dispose();
        _surface = null!;
        _gl = null;
    }
}