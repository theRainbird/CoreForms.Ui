using System.Runtime.InteropServices;
using CoreForms.Ui.Core;

namespace CoreForms.Ui.Rendering;

public class FontRenderer : IDisposable
{
    private IntPtr _renderer;
    private bool _disposed;
    private readonly Dictionary<string, IntPtr> _fontCache = new();

    [DllImport("SDL2_ttf", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern int TTF_Init();

    [DllImport("SDL2_ttf", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern IntPtr TTF_OpenFont(string file, int ptsize);

    [DllImport("SDL2_ttf", CallingConvention = CallingConvention.Cdecl)]
    private static extern void TTF_CloseFont(IntPtr font);

    [DllImport("SDL2_ttf", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr TTF_RenderUTF8_Blended(IntPtr font, string text, SDLColor fg);

    [DllImport("SDL2_ttf", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SDL_DestroyTexture(IntPtr texture);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr SDL_CreateTextureFromSurface(IntPtr renderer, IntPtr surface);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SDL_FreeSurface(IntPtr surface);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern int SDL_QueryTexture(IntPtr texture, out uint format, out int access, out int w, out int h);

    [StructLayout(LayoutKind.Sequential)]
    private struct SDLColor
    {
        public byte r;
        public byte g;
        public byte b;
        public byte a;
    }

    public FontRenderer(IntPtr renderer)
    {
        _renderer = renderer;
        try
        {
            TTF_Init();
        }
        catch
        {
            Console.WriteLine("Warning: SDL_ttf not available, text rendering will be limited");
        }
    }

    public void DrawText(string text, Core.Font font, Core.Color color, float x, float y)
    {
        if (string.IsNullOrEmpty(text)) return;

        var fontPath = GetFontPath(font.Name);
        var fontPtr = GetOrLoadFont(fontPath, (int)font.Size);

        if (fontPtr == IntPtr.Zero)
        {
            return;
        }

        var sdlColor = new SDLColor { r = color.R, g = color.G, b = color.B, a = color.A };
        var surface = TTF_RenderUTF8_Blended(fontPtr, text, sdlColor);

        if (surface == IntPtr.Zero)
        {
            return;
        }

        var texture = SDL_CreateTextureFromSurface(_renderer, surface);
        SDL_FreeSurface(surface);

        if (texture != IntPtr.Zero)
        {
            SDL_QueryTexture(texture, out _, out _, out int w, out int h);
            var dstRect = new SDL_Rect { x = (int)x, y = (int)y, w = w, h = h };
            SDL_RenderCopy(_renderer, texture, IntPtr.Zero, ref dstRect);
            SDL_DestroyTexture(texture);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SDL_Rect
    {
        public int x;
        public int y;
        public int w;
        public int h;
    }

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern int SDL_RenderCopy(IntPtr renderer, IntPtr texture, IntPtr srcrect, ref SDL_Rect dstrect);

    private IntPtr GetOrLoadFont(string path, int size)
    {
        var key = $"{path}:{size}";
        if (_fontCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var font = TTF_OpenFont(path, size);
        if (font != IntPtr.Zero)
        {
            _fontCache[key] = font;
        }
        return font;
    }

    private string GetFontPath(string fontName)
    {
        var fonts = new[]
        {
            $"/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
            $"/usr/share/fonts/TTF/DejaVuSans.ttf",
            "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf",
            "/usr/share/fonts/truetype/freefont/FreeSans.ttf",
            "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"
        };

        foreach (var font in fonts)
        {
            if (File.Exists(font))
            {
                return font;
            }
        }

        return fonts[0];
    }

    [DllImport("SDL2_ttf", CallingConvention = CallingConvention.Cdecl)]
    private static extern int TTF_SizeUTF8(IntPtr font, string text, out int w, out int h);

    public (int width, int height) MeasureText(string text, Core.Font font)
    {
        if (string.IsNullOrEmpty(text))
            return (0, 0);

        var fontPath = GetFontPath(font.Name);
        var fontPtr = GetOrLoadFont(fontPath, (int)font.Size);

        if (fontPtr == IntPtr.Zero)
            return ((int)(text.Length * font.Size * 0.6f), (int)font.Size);

        TTF_SizeUTF8(fontPtr, text, out int w, out int h);
        return (w, h);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            foreach (var font in _fontCache.Values)
            {
                TTF_CloseFont(font);
            }
            _fontCache.Clear();
            _disposed = true;
        }
    }
}