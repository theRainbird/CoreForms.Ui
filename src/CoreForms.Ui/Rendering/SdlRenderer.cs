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
    private static extern int SDL_RenderSetClipRect(IntPtr renderer, ref SDL_Rect rect);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern int SDL_RenderSetClipRect(IntPtr renderer, IntPtr rect);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr SDL_CreateFont(string path);

    [StructLayout(LayoutKind.Sequential)]
    private struct SDL_Rect
    {
        public int x;
        public int y;
        public int w;
        public int h;
    }

    public SdlRenderer(IntPtr window)
    {
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

    public void DrawLine(Core.Color color, float x1, float y1, float x2, float y2)
    {
        SDL_SetRenderDrawColor(_renderer, color.R, color.G, color.B, color.A);
        SDL_RenderDrawLine(_renderer, (int)x1, (int)y1, (int)x2, (int)y2);
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