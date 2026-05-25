using CoreForms.Ui.Controls.Basic;
using CoreForms.Ui.Controls.Containers;
using CoreForms.Ui.Controls;
using CoreForms.Ui.Resources;
using SkiaSharp;
using Graphics = CoreForms.Ui.Rendering.Graphics;
using CoreForms.Ui.Core;

namespace CoreForms.Ui.Demo.UserControls;

/// <summary>
/// Demonstrates PictureBox with SVG and raster images in various SizeMode settings.
/// </summary>
public class ImagesPage : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImagesPage"/> class.
    /// </summary>
    public ImagesPage()
    {
        var svgGroup = new GroupBox
        {
            Text = SR.GetString("GroupSvgImages"),
            Location = new Point(10, 10),
            Size = new Size(250, 200)
        };

        var svgPictureBox = new PictureBox
        {
            Location = new Point(15, 25),
            Size = new Size(80, 80),
            SizeMode = PictureBoxSizeMode.Zoom
        };
        svgPictureBox.Image = Icons.CheckmarkCircle24;

        var svgStretchBox = new PictureBox
        {
            Location = new Point(105, 25),
            Size = new Size(120, 80),
            SizeMode = PictureBoxSizeMode.StretchImage
        };
        svgStretchBox.Image = Icons.Image24;

        var svgCenterBox = new PictureBox
        {
            Location = new Point(15, 115),
            Size = new Size(210, 60),
            SizeMode = PictureBoxSizeMode.CenterImage
        };
        svgCenterBox.Image = Icons.CheckmarkCircle24;

        var svgLabel = new Label { Text = SR.GetString("LabelSvgModes"), Location = new Point(15, 175), Size = new Size(210, 20) };

        svgGroup.Controls.Add(svgPictureBox);
        svgGroup.Controls.Add(svgStretchBox);
        svgGroup.Controls.Add(svgCenterBox);
        svgGroup.Controls.Add(svgLabel);

        var rasterGroup = new GroupBox
        {
            Text = SR.GetString("GroupRasterImages"),
            Location = new Point(270, 10),
            Size = new Size(250, 200)
        };

        var pngBytes = CreateDemoPng();
        var rasterImage = RasterImage.FromBytes(pngBytes);

        var rasterNormalBox = new PictureBox
        {
            Location = new Point(15, 25),
            Size = new Size(60, 60),
            SizeMode = PictureBoxSizeMode.Normal
        };
        rasterNormalBox.Image = rasterImage;

        var rasterStretchBox = new PictureBox
        {
            Location = new Point(85, 25),
            Size = new Size(150, 60),
            SizeMode = PictureBoxSizeMode.StretchImage
        };
        rasterStretchBox.Image = rasterImage;

        var rasterZoomBox = new PictureBox
        {
            Location = new Point(15, 95),
            Size = new Size(220, 80),
            SizeMode = PictureBoxSizeMode.Zoom
        };
        rasterZoomBox.Image = rasterImage;

        var rasterLabel = new Label { Text = SR.GetString("LabelRasterModes"), Location = new Point(15, 175), Size = new Size(210, 20) };

        rasterGroup.Controls.Add(rasterNormalBox);
        rasterGroup.Controls.Add(rasterStretchBox);
        rasterGroup.Controls.Add(rasterZoomBox);
        rasterGroup.Controls.Add(rasterLabel);

        Controls.Add(svgGroup);
        Controls.Add(rasterGroup);
    }

    private static byte[] CreateDemoPng()
    {
        using var bitmap = new SKBitmap(32, 32, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(new SKColor(65, 105, 225));

        using var paint = new SKPaint { Color = SKColors.White, IsAntialias = true };
        canvas.DrawCircle(16, 16, 10, paint);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
