using System.Runtime.InteropServices;
using CoreForms.Ui.Core;
using SkiaSharp;

namespace CoreForms.Ui.Rendering;

/// <summary>
/// Provides font rendering using SkiaSharp's SKPaint and SKFont APIs.
/// Supports cross-platform font discovery and caches typefaces for performance.
/// Text is rendered directly to the SKCanvas, eliminating per-frame texture creation.
/// </summary>
public class SkiaFontRenderer : IDisposable
{
    private readonly Dictionary<string, SKTypeface> _typefaceCache = new();
    private readonly Dictionary<string, SKFont> _fontCache = new();
    private bool _disposed;

    /// <summary>
    /// Draws text at the specified location on the given canvas.
    /// </summary>
    /// <param name="text">The text to draw.</param>
    /// <param name="font">The font to use.</param>
    /// <param name="color">The text color.</param>
    /// <param name="x">The x-coordinate.</param>
    /// <param name="y">The y-coordinate.</param>
    /// <param name="canvas">The SkiaSharp canvas to draw on.</param>
    public void DrawText(string text, Core.Font font, Core.Color color, float x, float y, SKCanvas canvas)
    {
        if (string.IsNullOrEmpty(text)) return;

        var skFont = GetOrCreateFont(font);
        if (skFont == null) return;

        using var paint = new SKPaint
        {
            Color = new SKColor(color.R, color.G, color.B, color.A),
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            TextSize = font.Size
        };

        canvas.DrawText(text, x, y + font.Size, skFont, paint);
    }

    /// <summary>
    /// Measures the dimensions of the specified text.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="font">The font to use.</param>
    /// <returns>A tuple containing the width and height.</returns>
    public (int width, int height) MeasureText(string text, Core.Font font)
    {
        if (string.IsNullOrEmpty(text))
            return (0, 0);

        var skFont = GetOrCreateFont(font);
        if (skFont == null)
            return ((int)(text.Length * font.Size * 0.6f), (int)font.Size);

        var metrics = skFont.Metrics;
        ushort[] glyphs = new ushort[text.Length];
        skFont.GetGlyphs(text, glyphs);
        float width = skFont.MeasureText(glyphs);
        return ((int)width, (int)(metrics.Descent - metrics.Ascent));
    }

    private SKFont? GetOrCreateFont(Core.Font font)
    {
        var key = $"{font.Name}:{font.Size}:{font.Style}";
        if (_fontCache.TryGetValue(key, out var cached))
            return cached;

        var typeface = GetOrLoadTypeface(font.Name, font.Style);
        if (typeface == null) return null;

        var skFont = new SKFont(typeface, font.Size);
        _fontCache[key] = skFont;
        return skFont;
    }

    private SKTypeface? GetOrLoadTypeface(string fontName, FontStyle style)
    {
        var styleKey = $"{fontName}:{style}";
        if (_typefaceCache.TryGetValue(styleKey, out var cached))
            return cached;

        foreach (var dir in GetFontDirectories())
        {
            if (!Directory.Exists(dir)) continue;

            var candidates = Directory.GetFiles(dir, "*.ttf", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(dir, "*.otf", SearchOption.AllDirectories));

            foreach (var file in candidates)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                var isBold = style.HasFlag(FontStyle.Bold);
                var isItalic = style.HasFlag(FontStyle.Italic);

                if (IsFontMatch(fileName, fontName, isBold, isItalic))
                {
                    try
                    {
                        var typeface = SKTypeface.FromFile(file);
                        if (typeface != null)
                        {
                            _typefaceCache[styleKey] = typeface;
                            return typeface;
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
        }

        foreach (var dir in GetFontDirectories())
        {
            if (!Directory.Exists(dir)) continue;

            var candidates = Directory.GetFiles(dir, "*.ttf", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(dir, "*.otf", SearchOption.AllDirectories));

            foreach (var file in candidates)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (IsFallbackFontMatch(fileName))
                {
                    try
                    {
                        var typeface = SKTypeface.FromFile(file);
                        if (typeface != null)
                        {
                            _typefaceCache[styleKey] = typeface;
                            return typeface;
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
        }

        try
        {
            var defaultTypeface = SKTypeface.FromFamilyName(fontName);
            if (defaultTypeface != null)
            {
                _typefaceCache[styleKey] = defaultTypeface;
                return defaultTypeface;
            }
        }
        catch { }

        return null;
    }

    private static IEnumerable<string> GetFontDirectories()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            return new[] { Path.Combine(windir, "Fonts") };
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return new[] { "/Library/Fonts", "/System/Library/Fonts", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library", "Fonts") };
        }

        return new[] { "/usr/share/fonts", "/usr/local/share/fonts", "/run/current-system/sw/share/fonts" };
    }

    private static bool IsFontMatch(string fileName, string fontName, bool bold, bool italic)
    {
        var nameLower = fontName.ToLowerInvariant();
        var fileLower = fileName.ToLowerInvariant();

        var baseNames = new[] { nameLower };
        foreach (var baseName in baseNames)
        {
            if (!fileLower.Contains(baseName.Replace(" ", ""))) continue;

            if (bold && italic && fileLower.Contains("bolditalic")) return true;
            if (bold && fileLower.Contains("bold") && !fileLower.Contains("italic")) return true;
            if (italic && fileLower.Contains("italic") && !fileLower.Contains("bold")) return true;
            if (!bold && !italic && !fileLower.Contains("bold") && !fileLower.Contains("italic")) return true;
        }

        return false;
    }

    private static bool IsFallbackFontMatch(string fileName)
    {
        var fileLower = fileName.ToLowerInvariant();
        return fileLower.Contains("dejavusans") && !fileLower.Contains("bold") && !fileLower.Contains("italic")
            || fileLower.Contains("liberationsans") && !fileLower.Contains("bold") && !fileLower.Contains("italic")
            || fileLower.Contains("freesans") && !fileLower.Contains("bold") && !fileLower.Contains("italic")
            || fileLower.Contains("arial") && !fileLower.Contains("bold") && !fileLower.Contains("italic");
    }

    /// <summary>
    /// Releases all resources used by this SkiaFontRenderer.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            foreach (var font in _fontCache.Values)
                font.Dispose();
            foreach (var typeface in _typefaceCache.Values)
                typeface.Dispose();
            _fontCache.Clear();
            _typefaceCache.Clear();
            _disposed = true;
        }
    }
}