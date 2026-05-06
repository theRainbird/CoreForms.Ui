using System.Runtime.InteropServices;

namespace CoreForms.Ui.Rendering;

public class SdlRenderer : IDisposable
{
    private IntPtr _renderer;
    private IntPtr _window;
    private bool _disposed;

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
    private static extern IntPtr SDL_CreateFont(string path);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern int SDL_RenderGeometry(IntPtr renderer, IntPtr texture, SDL_Vertex[] vertices, int num_vertices, int[] indices, int num_indices);

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

    public void Clear(Core.Color color)
    {
        SDL_SetRenderDrawColor(_renderer, color.R, color.G, color.B, color.A);
        SDL_RenderClear(_renderer);
    }

    public void FillRectangle(Core.Color color, float x, float y, float width, float height)
    {
        SDL_SetRenderDrawColor(_renderer, color.R, color.G, color.B, color.A);
        var rect = new SDL_Rect { x = (int)x, y = (int)y, w = (int)width, h = (int)height };
        SDL_RenderFillRect(_renderer, ref rect);
    }

    public void DrawRectangle(Core.Color color, float x, float y, float width, float height)
    {
        SDL_SetRenderDrawColor(_renderer, color.R, color.G, color.B, color.A);
        var rect = new SDL_Rect { x = (int)x, y = (int)y, w = (int)width, h = (int)height };
        SDL_RenderDrawRect(_renderer, ref rect);
    }

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
            var endR = (float)x1; var endRY = (float)y1;
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

    public void Present()
    {
        SDL_RenderPresent(_renderer);
    }

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