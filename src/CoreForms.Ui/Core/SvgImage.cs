using SkiaSharp;
using Svg.Skia;
using System;
using System.IO;
using System.Reflection;

namespace CoreForms.Ui.Core;

/// <summary>
/// Represents an SVG image that can be rendered by the graphics system.
/// Encapsulates SkiaSharp types to keep the public API renderer-agnostic.
/// </summary>
public class SvgImage : IDisposable
{
    private SKImage? _nativeImage;
    private bool _disposed;

    /// <summary>
    /// Gets the native SkiaSharp image. For internal renderer use only.
    /// </summary>
    internal SKImage? NativeImage => _nativeImage;

    /// <summary>
    /// Gets the width of the image in pixels.
    /// </summary>
    public int Width { get; private set; }

    /// <summary>
    /// Gets the height of the image in pixels.
    /// </summary>
    public int Height { get; private set; }

    private SvgImage() { }

    /// <summary>
    /// Loads an SVG image from an embedded resource.
    /// </summary>
    /// <param name="resourceName">The fully qualified manifest resource name.</param>
    /// <param name="size">The desired width/height in pixels.</param>
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
    /// <param name="size">The desired width/height in pixels.</param>
    /// <returns>The loaded SvgImage, or null if loading failed.</returns>
    public static SvgImage? FromSvgStream(Stream stream, int size)
    {
        try
        {
            using var svg = new SKSvg();
            if (svg.Load(stream) == null)
                return null;

            var picture = svg.Picture;
            if (picture == null)
                return null;

            return CreateFromPicture(picture, size);
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

    private static SvgImage? CreateFromPicture(SKPicture picture, int size)
    {
        if (picture.CullRect.Width <= 0 || picture.CullRect.Height <= 0)
            return null;

        using var surface = SKSurface.Create(new SKImageInfo(size, size, SKColorType.Bgra8888, SKAlphaType.Premul));
        if (surface == null)
            return null;

        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        float scaleX = size / picture.CullRect.Width;
        float scaleY = size / picture.CullRect.Height;
        float scale = Math.Min(scaleX, scaleY);
        float offsetX = (size - picture.CullRect.Width * scale) / 2f;
        float offsetY = (size - picture.CullRect.Height * scale) / 2f;
        canvas.Translate(offsetX, offsetY);
        canvas.Scale(scale, scale);

        canvas.DrawPicture(picture);
        canvas.Flush();

        var snapshot = surface.Snapshot();
        if (snapshot == null)
            return null;

        return new SvgImage
        {
            _nativeImage = snapshot,
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
    }
}
