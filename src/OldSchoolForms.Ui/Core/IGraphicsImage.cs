using SkiaSharp;
using System;

namespace OldSchoolForms.Ui.Core;

/// <summary>
/// Represents a graphics image that can be rendered by the graphics system.
/// Provides a common interface for both vector (SVG) and raster images.
/// </summary>
public interface IGraphicsImage : IDisposable
{
    /// <summary>
    /// Gets the width of the image in pixels.
    /// </summary>
    int Width { get; }

    /// <summary>
    /// Gets the height of the image in pixels.
    /// </summary>
    int Height { get; }

    /// <summary>
    /// Gets the native SkiaSharp image. For internal renderer use only.
    /// </summary>
    SKImage? NativeImage { get; }
}
