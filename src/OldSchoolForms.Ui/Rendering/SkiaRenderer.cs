using System.Runtime.InteropServices;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SkiaSharp;

namespace OldSchoolForms.Ui.Rendering;

/// <summary>
/// Provides SkiaSharp-based rendering for the graphics system.
/// Each window has its own SkiaRenderer instance.
/// Attempts GPU-accelerated rendering via GRGlInterface first; falls back to
/// offscreen SKSurface + OpenGL texture blit if GPU context creation fails
/// (e.g., no GPU available, RDP session, headless environments).
/// </summary>
public class SkiaRenderer : IDisposable
{
    private readonly IWindow _window;
    private GL? _gl;
    private SKSurface _surface = null!;
    private GRContext? _grContext;
    private GRBackendRenderTarget? _renderTarget;
    private SkiaRendererFallback? _fallback;
    private int _width;
    private int _height;
    private bool _disposed;
    private bool _useGpuRendering;
    private Core.Rectangle? _lastClipRect;

    /// <summary>
    /// Gets the SkiaSharp canvas for direct drawing operations.
    /// </summary>
    public SKCanvas Canvas => _surface?.Canvas!;

    /// <summary>
    /// Gets whether this renderer is using GPU-accelerated rendering.
    /// </summary>
    public bool IsGpuAccelerated => _useGpuRendering;

    /// <summary>
    /// Initializes a new SkiaRenderer for the specified window.
    /// Attempts GPU-accelerated rendering first; falls back to software if GPU init fails.
    /// </summary>
    public SkiaRenderer(IWindow window)
        : this(window, window.Size.X, window.Size.Y)
    {
    }

    /// <summary>
    /// Initializes a new SkiaRenderer for the specified window with explicit dimensions.
    /// </summary>
    public SkiaRenderer(IWindow window, int width, int height)
    {
        _window = window;
        _width = width > 0 ? width : Math.Max(1, window.Size.X);
        _height = height > 0 ? height : Math.Max(1, window.Size.Y);

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

        if (_gl != null)
        {
            _useGpuRendering = TryInitGpuRendering();
        }

        if (!_useGpuRendering)
        {
            Console.WriteLine("[SkiaRenderer] Using software rendering (offscreen SKSurface + GL blit)");
            _fallback = new SkiaRendererFallback(_gl, _window);
            var info = new SKImageInfo(_width, _height, SKColorType.Bgra8888, SKAlphaType.Premul);
            _surface = SKSurface.Create(info);

            if (_surface == null)
            {
                throw new InvalidOperationException("Failed to create SkiaSharp rendering surface.");
            }
        }

        window.Resize += OnResize;
    }

    private bool TryInitGpuRendering()
    {
        if (_gl == null) return false;

        try
        {
            var glInterface = GRGlInterface.Create((name) =>
            {
                var procAddress = GetProcAddress(name);
                return procAddress != IntPtr.Zero ? procAddress : IntPtr.Zero;
            });

            if (glInterface == null)
            {
                Console.WriteLine("[SkiaRenderer] GRGlInterface.Create returned null, falling back to software");
                return false;
            }

            _grContext = GRContext.CreateGl(glInterface);
            if (_grContext == null)
            {
                Console.WriteLine("[SkiaRenderer] GRContext.CreateGl returned null, falling back to software");
                return false;
            }

            CreateGpuSurface();

            Console.WriteLine("[SkiaRenderer] GPU-accelerated rendering initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SkiaRenderer] GPU init failed, falling back to software: {ex.Message}");
            _grContext?.Dispose();
            _grContext = null;
            _renderTarget?.Dispose();
            _renderTarget = null;
            return false;
        }
    }

    private void CreateGpuSurface()
    {
        _renderTarget?.Dispose();
        _surface?.Dispose();

        int fboid;
        unsafe
        {
            if (_gl != null) { _gl.GetInteger(GLEnum.FramebufferBinding, out fboid); } else { fboid = 0; }
        }

        _renderTarget = new GRBackendRenderTarget(_width, _height, 0, 8,
            new GRGlFramebufferInfo((uint)fboid, (uint)GLEnum.Rgba8));

        _surface = SKSurface.Create(_grContext, _renderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Bgra8888);

        if (_surface == null)
        {
            throw new InvalidOperationException("Failed to create GPU-accelerated SkiaSharp surface.");
        }

        _grContext?.ResetContext();
    }

    private IntPtr GetProcAddress(string name)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return GlxGetProcAddress(name);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return WglGetProcAddress(name);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return NsGlGetProcAddress(name);
        }
        return IntPtr.Zero;
    }

    [DllImport("libGL.so.1", EntryPoint = "glXGetProcAddressARB", CharSet = CharSet.Ansi)]
    private static extern IntPtr GlxGetProcAddress(string procName);

    [DllImport("opengl32.dll", EntryPoint = "wglGetProcAddress", CharSet = CharSet.Ansi)]
    private static extern IntPtr WglGetProcAddress(string procName);

    [DllImport("/System/Library/Frameworks/OpenGL.framework/OpenGL", EntryPoint = "NSGLGetProcAddress", CharSet = CharSet.Ansi)]
    private static extern IntPtr NsGlGetProcAddress(string procName);

    private void OnResize(Vector2D<int> size)
    {
        if (size.X > 0 && size.Y > 0 && (size.X != _width || size.Y != _height))
        {
            CreateSurface(size.X, size.Y);
        }
    }

    private void CreateSurface(int width, int height)
    {
        _width = width;
        _height = height;

        if (_useGpuRendering && _grContext != null)
        {
            CreateGpuSurface();
        }
        else
        {
            _surface?.Dispose();
            var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
            _surface = SKSurface.Create(info);

            if (_surface == null)
            {
                throw new InvalidOperationException("Failed to create SkiaSharp rendering surface.");
            }
        }

        _fallback?.OnResize(width, height);
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
    /// Draws an image at the specified location and size with bilinear filtering
    /// for smooth rendering at non-integer zoom levels.
    /// </summary>
    public void DrawImage(SKImage image, float x, float y, float width, float height)
    {
        using var paint = new SKPaint { FilterQuality = SKFilterQuality.Low };
        _surface.Canvas.DrawImage(image, new SKRect(x, y, x + width, y + height), paint);
    }

    /// <summary>
    /// Sets the clipping rectangle. Skips Save/Restore when the clip has not changed
    /// since the last call to avoid unnecessary canvas state changes.
    /// </summary>
    public void SetClipRect(Core.Rectangle? rect)
    {
        if (!rect.HasValue && !_lastClipRect.HasValue)
            return;

        if (rect.HasValue && _lastClipRect.HasValue &&
            rect.Value.X == _lastClipRect.Value.X &&
            rect.Value.Y == _lastClipRect.Value.Y &&
            rect.Value.Width == _lastClipRect.Value.Width &&
            rect.Value.Height == _lastClipRect.Value.Height)
        {
            return;
        }

        _lastClipRect = rect;
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
    /// For GPU rendering, flushes the SkiaSharp context and swaps buffers.
    /// For software rendering, blits the offscreen surface via OpenGL texture.
    /// </summary>
    public void Present()
    {
        if (_disposed) return;

        if (_useGpuRendering)
        {
            _surface.Canvas.Flush();
            _grContext?.Flush();
        }
        else
        {
            _surface.Canvas.Flush();
            _fallback?.BlitToScreen(_surface, _width, _height);
        }
    }

    /// <summary>
    /// Draws a filled pie wedge.
    /// </summary>
    public void FillPie(Core.Color color, float x, float y, float w, float h, float startAngle, float sweepAngle)
    {
        using var paint = new SKPaint { Color = new SKColor(color.R, color.G, color.B, color.A), IsAntialias = true };
        _surface.Canvas.DrawArc(new SKRect(x, y, x + w, y + h), startAngle, sweepAngle, true, paint);
    }

    /// <summary>
    /// Draws an arc outline.
    /// </summary>
    public void DrawArc(Core.Color color, float x, float y, float w, float h, float startAngle, float sweepAngle, float lineWidth)
    {
        using var paint = new SKPaint
        {
            Color = new SKColor(color.R, color.G, color.B, color.A),
            IsAntialias = true,
            StrokeWidth = lineWidth,
            Style = SKPaintStyle.Stroke
        };
        _surface.Canvas.DrawArc(new SKRect(x, y, x + w, y + h), startAngle, sweepAngle, false, paint);
    }

    /// <summary>
    /// Draws a filled polygon from a flat vertex array [x1,y1,x2,y2,...].
    /// </summary>
    public void FillPolygon(Core.Color color, float[] points)
    {
        using var paint = new SKPaint { Color = new SKColor(color.R, color.G, color.B, color.A), IsAntialias = true };
        using var path = new SKPath();
        if (points.Length < 2) return;
        path.MoveTo(points[0], points[1]);
        for (int i = 2; i < points.Length; i += 2)
            path.LineTo(points[i], points[i + 1]);
        path.Close();
        _surface.Canvas.DrawPath(path, paint);
    }

    /// <summary>
    /// Draws a polygon outline from a flat vertex array [x1,y1,x2,y2,...].
    /// </summary>
    public void DrawPolygon(Core.Color color, float[] points, float lineWidth)
    {
        using var paint = new SKPaint
        {
            Color = new SKColor(color.R, color.G, color.B, color.A),
            IsAntialias = true,
            StrokeWidth = lineWidth,
            Style = SKPaintStyle.Stroke
        };
        using var path = new SKPath();
        if (points.Length < 2) return;
        path.MoveTo(points[0], points[1]);
        for (int i = 2; i < points.Length; i += 2)
            path.LineTo(points[i], points[i + 1]);
        _surface.Canvas.DrawPath(path, paint);
    }

    /// <summary>
    /// Cleans up OpenGL resources while the GL context is still valid.
    /// Called before the window is closed to prevent resource leaks.
    /// </summary>
    public void CleanupGLResources()
    {
        if (_useGpuRendering)
        {
            // GPU resources are cleaned up in Dispose
            return;
        }

        _fallback?.CleanupGLResources();
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
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _window.Resize -= OnResize;

        _surface?.Dispose();
        _surface = null!;

        _renderTarget?.Dispose();
        _renderTarget = null;

        _grContext?.Dispose();
        _grContext = null;

        _fallback?.Dispose();
        _fallback = null;

        _gl = null;
    }

    /// <summary>
    /// Handles software rendering fallback: offscreen SKSurface + OpenGL texture blit.
    /// Used when GPU-accelerated SkiaSharp rendering is not available.
    /// </summary>
    private class SkiaRendererFallback : IDisposable
    {
        private GL? _gl;
        private uint _textureId;
        private bool _textureInitialized;
        private uint _shaderProgram;
        private uint _vao;
        private bool _blitInitialized;

        public SkiaRendererFallback(GL? gl, IWindow window)
        {
            _gl = gl;
        }

        public void OnResize(int width, int height)
        {
            _textureInitialized = false;
            _blitInitialized = false;
        }

        public unsafe void BlitToScreen(SKSurface surface, int width, int height)
        {
            if (_gl == null) return;

            try
            {
                using var snapshot = surface.Snapshot();
                using var pixels = snapshot.PeekPixels();
                if (pixels == null) return;

                var pixelData = pixels.GetPixelSpan();
                if (pixelData.IsEmpty) return;

                _gl.Viewport(0, 0, (uint)width, (uint)height);
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
                    _gl.TexImage2D(GLEnum.Texture2D, 0, (int)GLEnum.Rgba8, (uint)width, (uint)height, 0,
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

        public void Dispose()
        {
            CleanupGLResources();
            _gl = null;
        }
    }
}