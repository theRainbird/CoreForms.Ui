using SkiaSharp;
using Svg.Skia;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace CoreForms.Ui.Core;

/// <summary>
/// Represents an SVG image that can be rendered by the graphics system.
/// Encapsulates SkiaSharp types to keep the public API renderer-agnostic.
/// Supports resolution-independent rendering by caching rasterizations at multiple sizes.
/// The raster cache uses LRU eviction to prevent unbounded memory growth during zoom changes.
/// </summary>
public class SvgImage : IGraphicsImage
{
    private const int MaxCacheEntries = 32;

    private SKImage? _nativeImage;
    private byte[]? _svgData;
    private Dictionary<(int, int), SKImage>? _rasterCache;
    private LinkedList<(int, int)>? _cacheOrder;
    private bool _disposed;

    /// <summary>
    /// Gets the native SkiaSharp image at the default size. For internal renderer use only.
    /// </summary>
    public SKImage? NativeImage => _nativeImage;

    /// <summary>
    /// Gets the width of the image in pixels at the default size.
    /// </summary>
    public int Width { get; private set; }

    /// <summary>
    /// Gets the height of the image in pixels at the default size.
    /// </summary>
    public int Height { get; private set; }

    private SvgImage() { }

    /// <summary>
    /// Loads an SVG image from an embedded resource.
    /// </summary>
    /// <param name="resourceName">The fully qualified manifest resource name.</param>
    /// <param name="size">The desired default width/height in pixels.</param>
    /// <returns>The loaded SvgImage, or null if loading failed.</returns>
    public static SvgImage? FromSvgResource(string resourceName, int size)
    {
        try
        {
            var assembly = typeof(SvgImage).Assembly;
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                return null;
            return FromSvgStream(stream, size);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SvgImage] Failed to load resource '{resourceName}': {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Loads an SVG image from a stream.
    /// </summary>
    /// <param name="stream">The stream containing SVG data.</param>
    /// <param name="size">The desired default width/height in pixels.</param>
    /// <returns>The loaded SvgImage, or null if loading failed.</returns>
    public static SvgImage? FromSvgStream(Stream stream, int size)
    {
        try
        {
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            var svgData = ms.ToArray();

            using var svg = new SKSvg();
            using var svgStream = new MemoryStream(svgData);
            if (svg.Load(svgStream) == null)
                return null;

            var picture = svg.Picture;
            if (picture == null)
                return null;

            var image = CreateFromPicture(picture, size);
            if (image == null)
                return null;

            image._svgData = svgData;
            return image;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SvgImage] Failed to load from stream: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Loads an SVG image from a string.
    /// </summary>
    /// <param name="svgContent">The SVG content as a string.</param>
    /// <param name="size">The desired width/height in pixels.</param>
    /// <returns>The loaded SvgImage, or null if loading failed.</returns>
    public static SvgImage? FromSvgString(string svgContent, int size)
    {
        try
        {
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(svgContent));
            return FromSvgStream(stream, size);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SvgImage] Failed to load from string: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets a rasterized version of this SVG image at the specified size.
    /// Results are cached with LRU eviction so subsequent requests for the same size are fast.
    /// </summary>
    /// <param name="width">The desired width in pixels.</param>
    /// <param name="height">The desired height in pixels.</param>
    /// <returns>The rasterized SKImage at the requested size, or null if rasterization failed.</returns>
    public SKImage? GetRasterized(int width, int height)
    {
        if (_disposed)
            return null;

        if (width <= 0 || height <= 0)
            return null;

        if (width == Width && height == Height)
            return _nativeImage;

        if (_rasterCache != null && _rasterCache.TryGetValue((width, height), out var cached))
        {
            TouchCacheEntry((width, height));
            return cached;
        }

        if (_svgData == null)
            return null;

        try
        {
            using var svg = new SKSvg();
            using var svgStream = new MemoryStream(_svgData);
            if (svg.Load(svgStream) == null)
                return null;

            var picture = svg.Picture;
            if (picture == null)
                return null;

            var rasterized = RasterizePicture(picture, width, height);
            if (rasterized == null)
                return null;

            AddToCache((width, height), rasterized);
            return rasterized;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SvgImage] Failed to rasterize at {width}x{height}: {ex.Message}");
            return null;
        }
    }

    private void TouchCacheEntry((int, int) key)
    {
        if (_cacheOrder != null)
        {
            _cacheOrder.Remove(key);
            _cacheOrder.AddLast(key);
        }
    }

    private void AddToCache((int, int) key, SKImage image)
    {
        _rasterCache ??= new Dictionary<(int, int), SKImage>();
        _cacheOrder ??= new LinkedList<(int, int)>();

        if (_rasterCache.Count >= MaxCacheEntries && _cacheOrder.First != null)
        {
            var oldest = _cacheOrder.First.Value;
            _cacheOrder.RemoveFirst();
            if (_rasterCache.TryGetValue(oldest, out var evicted))
            {
                evicted.Dispose();
                _rasterCache.Remove(oldest);
            }
        }

        _rasterCache[key] = image;
        _cacheOrder.AddLast(key);
    }

    private static SKImage? RasterizePicture(SKPicture picture, int width, int height)
    {
        if (picture.CullRect.Width <= 0 || picture.CullRect.Height <= 0)
            return null;

        using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul));
        if (surface == null)
            return null;

        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        float scaleX = width / picture.CullRect.Width;
        float scaleY = height / picture.CullRect.Height;
        float scale = Math.Min(scaleX, scaleY);
        float offsetX = (width - picture.CullRect.Width * scale) / 2f;
        float offsetY = (height - picture.CullRect.Height * scale) / 2f;
        canvas.Translate(offsetX, offsetY);
        canvas.Scale(scale, scale);

        canvas.DrawPicture(picture);
        canvas.Flush();

        return surface.Snapshot();
    }

    private static SvgImage? CreateFromPicture(SKPicture picture, int size)
    {
        var rasterized = RasterizePicture(picture, size, size);
        if (rasterized == null)
            return null;

        return new SvgImage
        {
            _nativeImage = rasterized,
            Width = size,
            Height = size
        };
    }

    /// <summary>
    /// Releases all resources used by this SvgImage.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _nativeImage?.Dispose();
        _nativeImage = null;
        if (_rasterCache != null)
        {
            foreach (var kvp in _rasterCache)
                kvp.Value.Dispose();
            _rasterCache.Clear();
        }
        _cacheOrder?.Clear();
    }
}
