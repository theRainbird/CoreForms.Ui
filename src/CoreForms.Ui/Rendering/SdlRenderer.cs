using System.Runtime.InteropServices;

namespace CoreForms.Ui.Rendering;

/// <summary>
/// Provides SDL2-based rendering implementation for the graphics system.
/// Each window has its own SdlRenderer instance tied to its native window.
/// </summary>
public class SdlRenderer : IDisposable
{
    private IntPtr _renderer;
    private IntPtr _window;
    private bool _disposed;

    /// <summary>
    /// Gets the native SDL renderer handle.
    /// </summary>
    public IntPtr Handle => _renderer;

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr SDL_CreateRenderer(IntPtr window, int index, uint flags);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SDL_DestroyRenderer(IntPtr renderer);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern int SDL_SetRenderDrawColor(IntPtr renderer, byte r, byte g, byte b, byte a);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern int SDL_RenderClear(IntPtr renderer);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SDL_RenderPresent(IntPtr renderer);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern int SDL_RenderFillRect(IntPtr renderer, ref SDL_Rect rect);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern int SDL_RenderDrawRect(IntPtr renderer, ref SDL_Rect rect);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern int SDL_RenderDrawLine(IntPtr renderer, int x1, int y1, int x2, int y2);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern int SDL_RenderDrawLineF(IntPtr renderer, float x1, float y1, float x2, float y2);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern bool SDL_SetHint(string name, string value);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern int SDL_RenderSetClipRect(IntPtr renderer, ref SDL_Rect rect);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern int SDL_RenderSetClipRect(IntPtr renderer, IntPtr rect);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern int SDL_SetRenderDrawBlendMode(IntPtr renderer, BlendMode blendMode);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr SDL_CreateFont(string path);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern int SDL_RenderGeometry(IntPtr renderer, IntPtr texture, SDL_Vertex[] vertices, int num_vertices, int[] indices, int num_indices);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern int SDL_RenderCopy(IntPtr renderer, IntPtr texture, IntPtr srcrect, ref SDL_Rect dstrect);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SDL_DestroyTexture(IntPtr texture);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern int SDL_QueryTexture(IntPtr texture, out uint format, out int access, out int w, out int h);

    /// <summary>
    /// SDL blend modes for alpha compositing.
    /// </summary>
    private enum BlendMode : uint
    {
        SDL_BLENDMODE_NONE = 0,
        SDL_BLENDMODE_BLEND = 1,
        SDL_BLENDMODE_ADD = 2,
        SDL_BLENDMODE_MOD = 4,
        SDL_BLENDMODE_MUL = 8
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SDL_Rect
    {
        public int x;
        public int y;
        public int w;
        public int h;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SDL_FPoint
    {
        public float x;
        public float y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SDL_Color
    {
        public byte r;
        public byte g;
        public byte b;
        public byte a;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SDL_Vertex
    {
        public SDL_FPoint position;
        public SDL_Color color;
        public SDL_FPoint tex_coord;
    }

    /// <summary>
    /// Initializes a new SdlRenderer for the specified window.
    /// </summary>
    /// <param name="window">The native window handle.</param>
    /// <exception cref="InvalidOperationException">Thrown when SDL renderer creation fails.</exception>
    public SdlRenderer(IntPtr window)
    {
        SDL_SetHint("SDL_RENDER_LINE_METHOD", "2");
        _window = window;
        _renderer = SDL_CreateRenderer(window, -1, 0);

        if (_renderer == IntPtr.Zero)
        {
            var errorPtr = SDL_GetError();
            var errorMsg = errorPtr == IntPtr.Zero ? "Unknown error" : Marshal.PtrToStringAnsi(errorPtr);
            throw new InvalidOperationException("SDL_CreateRenderer failed: " + errorMsg);
        }
    }

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr SDL_GetError();

    /// <summary>
    /// Clears the renderer with the specified color.
    /// </summary>
    /// <param name="color">The clear color.</param>
    public void Clear(Core.Color color)
    {
        SDL_SetRenderDrawBlendMode(_renderer, BlendMode.SDL_BLENDMODE_BLEND);
        SDL_SetRenderDrawColor(_renderer, color.R, color.G, color.B, color.A);
        SDL_RenderClear(_renderer);
    }

    /// <summary>
    /// Fills a rectangle with the specified color.
    /// Supports alpha blending when the color has transparency.
    /// </summary>
    /// <param name="color">The fill color.</param>
    /// <param name="x">The x-coordinate.</param>
    /// <param name="y">The y-coordinate.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    public void FillRectangle(Core.Color color, float x, float y, float width, float height)
    {
        if (color.A < 255)
        {
            SDL_SetRenderDrawBlendMode(_renderer, BlendMode.SDL_BLENDMODE_BLEND);
        }
        SDL_SetRenderDrawColor(_renderer, color.R, color.G, color.B, color.A);
        var rect = new SDL_Rect { x = (int)x, y = (int)y, w = (int)width, h = (int)height };
        SDL_RenderFillRect(_renderer, ref rect);
    }

    /// <summary>
    /// Draws a rectangle outline.
    /// </summary>
    /// <param name="color">The outline color.</param>
    /// <param name="x">The x-coordinate.</param>
    /// <param name="y">The y-coordinate.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    public void DrawRectangle(Core.Color color, float x, float y, float width, float height)
    {
        SDL_SetRenderDrawColor(_renderer, color.R, color.G, color.B, color.A);
        var rect = new SDL_Rect { x = (int)x, y = (int)y, w = (int)width, h = (int)height };
        SDL_RenderDrawRect(_renderer, ref rect);
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
    public void DrawLine(Core.Color color, float x1, float y1, float x2, float y2, float lineWidth = 1f)
    {
        SDL_SetRenderDrawColor(_renderer, color.R, color.G, color.B, color.A);

        if (lineWidth <= 1f)
        {
            SDL_RenderDrawLineF(_renderer, x1, y1, x2, y2);
            return;
        }

        float dx = x2 - x1;
        float dy = y2 - y1;
        float length = MathF.Sqrt(dx * dx + dy * dy);
        if (length < 0.001f)
        {
            SDL_RenderDrawLineF(_renderer, x1, y1, x2, y2);
            return;
        }

        float hw = lineWidth / 2f;
        float nx = (-dy / length) * hw;
        float ny = (dx / length) * hw;

        var sdlColor = new SDL_Color { r = color.R, g = color.G, b = color.B, a = color.A };
        var tc = new SDL_FPoint { x = 0, y = 0 };

        var vertices = new SDL_Vertex[]
        {
            new() { position = new SDL_FPoint { x = x1 + nx, y = y1 + ny }, color = sdlColor, tex_coord = tc },
            new() { position = new SDL_FPoint { x = x1 - nx, y = y1 - ny }, color = sdlColor, tex_coord = tc },
            new() { position = new SDL_FPoint { x = x2 + nx, y = y2 + ny }, color = sdlColor, tex_coord = tc },
            new() { position = new SDL_FPoint { x = x2 - nx, y = y2 - ny }, color = sdlColor, tex_coord = tc },
        };

        var indices = new int[] { 0, 1, 2, 1, 2, 3 };

        SDL_RenderGeometry(_renderer, IntPtr.Zero, vertices, vertices.Length, indices, indices.Length);

        if (lineWidth >= 3f)
        {
            var endColor = sdlColor;
            var cap1 = new SDL_Vertex[]
            {
                new() { position = new SDL_FPoint { x = x1 + nx, y = y1 + ny }, color = endColor, tex_coord = tc },
                new() { position = new SDL_FPoint { x = x1 - nx, y = y1 - ny }, color = endColor, tex_coord = tc },
                new() { position = new SDL_FPoint { x = x1, y = y1 }, color = endColor, tex_coord = tc },
            };
            var cap2 = new SDL_Vertex[]
            {
                new() { position = new SDL_FPoint { x = x2 + nx, y = y2 + ny }, color = endColor, tex_coord = tc },
                new() { position = new SDL_FPoint { x = x2 - nx, y = y2 - ny }, color = endColor, tex_coord = tc },
                new() { position = new SDL_FPoint { x = x2, y = y2 }, color = endColor, tex_coord = tc },
            };
            var capIndices = new int[] { 0, 1, 2 };
            SDL_RenderGeometry(_renderer, IntPtr.Zero, cap1, cap1.Length, capIndices, capIndices.Length);
            SDL_RenderGeometry(_renderer, IntPtr.Zero, cap2, cap2.Length, capIndices, capIndices.Length);
        }
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
    public void FillTriangle(Core.Color color, float x1, float y1, float x2, float y2, float x3, float y3)
    {
        var sdlColor = new SDL_Color { r = color.R, g = color.G, b = color.B, a = color.A };
        var tc = new SDL_FPoint { x = 0, y = 0 };

        var vertices = new SDL_Vertex[]
        {
            new() { position = new SDL_FPoint { x = x1, y = y1 }, color = sdlColor, tex_coord = tc },
            new() { position = new SDL_FPoint { x = x2, y = y2 }, color = sdlColor, tex_coord = tc },
            new() { position = new SDL_FPoint { x = x3, y = y3 }, color = sdlColor, tex_coord = tc },
        };

        var indices = new int[] { 0, 1, 2 };

        SDL_SetRenderDrawColor(_renderer, color.R, color.G, color.B, color.A);
        SDL_RenderGeometry(_renderer, IntPtr.Zero, vertices, vertices.Length, indices, indices.Length);
    }

    /// <summary>
    /// Fills an ellipse within the specified bounding rectangle using triangle fan approximation.
    /// </summary>
    /// <param name="color">The fill color.</param>
    /// <param name="x">The x-coordinate of the bounding rectangle.</param>
    /// <param name="y">The y-coordinate of the bounding rectangle.</param>
    /// <param name="width">The width of the bounding rectangle.</param>
    /// <param name="height">The height of the bounding rectangle.</param>
    public void FillEllipse(Core.Color color, float x, float y, float width, float height)
    {
        SDL_SetRenderDrawBlendMode(_renderer, color.A < 255 ? BlendMode.SDL_BLENDMODE_BLEND : BlendMode.SDL_BLENDMODE_NONE);
        SDL_SetRenderDrawColor(_renderer, color.R, color.G, color.B, color.A);

        float cx = x + width / 2f;
        float cy = y + height / 2f;
        float rx = width / 2f;
        float ry = height / 2f;

        const int segments = 32;
        var sdlColor = new SDL_Color { r = color.R, g = color.G, b = color.B, a = color.A };
        var tc = new SDL_FPoint { x = 0, y = 0 };
        var center = new SDL_Vertex { position = new SDL_FPoint { x = cx, y = cy }, color = sdlColor, tex_coord = tc };

        for (int i = 0; i < segments; i++)
        {
            float angle1 = 2.0f * MathF.PI * i / segments;
            float angle2 = 2.0f * MathF.PI * (i + 1) / segments;

            var v1 = new SDL_Vertex
            {
                position = new SDL_FPoint { x = cx + rx * MathF.Cos(angle1), y = cy + ry * MathF.Sin(angle1) },
                color = sdlColor,
                tex_coord = tc
            };
            var v2 = new SDL_Vertex
            {
                position = new SDL_FPoint { x = cx + rx * MathF.Cos(angle2), y = cy + ry * MathF.Sin(angle2) },
                color = sdlColor,
                tex_coord = tc
            };

            var triVertices = new SDL_Vertex[] { center, v1, v2 };
            var triIndices = new int[] { 0, 1, 2 };
            SDL_RenderGeometry(_renderer, IntPtr.Zero, triVertices, 3, triIndices, 3);
        }
    }

    /// <summary>
    /// Draws an image (SDL texture) at the specified location and size.
    /// </summary>
    /// <param name="image">The image object (IntPtr SDL texture, or other supported type).</param>
    /// <param name="x">The x-coordinate.</param>
    /// <param name="y">The y-coordinate.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    public void DrawImage(object image, float x, float y, float width, float height)
    {
        if (image is IntPtr texturePtr && texturePtr != IntPtr.Zero)
        {
            var dstRect = new SDL_Rect { x = (int)x, y = (int)y, w = (int)width, h = (int)height };
            SDL_RenderCopy(_renderer, texturePtr, IntPtr.Zero, ref dstRect);
        }
    }

    /// <summary>
    /// Sets the clipping rectangle.
    /// </summary>
    /// <param name="rect">The clipping rectangle, or null to disable clipping.</param>
    public void SetClipRect(Core.Rectangle? rect)
    {
        if (rect.HasValue)
        {
            var sdlRect = new SDL_Rect { x = (int)rect.Value.X, y = (int)rect.Value.Y, w = (int)rect.Value.Width, h = (int)rect.Value.Height };
            SDL_RenderSetClipRect(_renderer, ref sdlRect);
        }
        else
        {
            SDL_RenderSetClipRect(_renderer, IntPtr.Zero);
        }
    }

    /// <summary>
    /// Presents the rendered frame to the display.
    /// </summary>
    public void Present()
    {
        SDL_RenderPresent(_renderer);
    }

    /// <summary>
    /// Releases all resources used by this SdlRenderer.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            if (_renderer != IntPtr.Zero)
            {
                SDL_DestroyRenderer(_renderer);
                _renderer = IntPtr.Zero;
            }
            _disposed = true;
        }
    }
}