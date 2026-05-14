using SkiaSharp;
using CoreForms.Ui.Core;

namespace CoreForms.Ui.WebBrowser.Rendering;

internal sealed class PixelImage : IGraphicsImage
{
    private SKImage? _image;
    private SKBitmap? _bitmap;

    public int Width { get; }
    public int Height { get; }
    public SKImage? NativeImage => _image;

    public PixelImage(byte[] bgraData, int width, int height)
    {
        Width = width;
        Height = height;

        var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        _bitmap = new SKBitmap(info);
        var ptr = _bitmap.GetPixels();
        System.Runtime.InteropServices.Marshal.Copy(bgraData, 0, ptr, bgraData.Length);
        _image = SKImage.FromBitmap(_bitmap);
        // Keep _bitmap alive as long as _image exists (SKImage.FromBitmap may reference it)
    }

    public void Dispose()
    {
        _image?.Dispose();
        _image = null;
        _bitmap?.Dispose();
        _bitmap = null;
    }
}
