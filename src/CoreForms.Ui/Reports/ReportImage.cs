using CoreForms.Ui.Core;
using CoreForms.Ui.Rendering;

namespace CoreForms.Ui.Reports;

/// <summary>
/// Specifies how an image is sized within a <see cref="ReportImage"/> control.
/// </summary>
public enum ImageSizing
{
    /// <summary>
    /// The image is displayed at its original size. If larger than the control, it is clipped.
    /// </summary>
    Normal,

    /// <summary>
    /// The image is stretched to fill the control dimensions (may distort aspect ratio).
    /// </summary>
    Stretch,

    /// <summary>
    /// The image is uniformly scaled to fit within the control, preserving aspect ratio.
    /// </summary>
    Zoom
}

/// <summary>
/// A report control that displays an image.
/// </summary>
public class ReportImage : ReportControl
{
    private IGraphicsImage? _image;

    /// <summary>
    /// Gets or sets the image to display.
    /// </summary>
    public IGraphicsImage? Image
    {
        get => _image;
        set => _image = value;
    }

    /// <summary>
    /// Gets or sets how the image is sized within the control.
    /// </summary>
    public ImageSizing Sizing { get; set; } = ImageSizing.Zoom;

    /// <summary>
    /// Returns Height (images have fixed, non-growing content).
    /// </summary>
    public override Cm MeasureContent(ReportRenderContext context)
    {
        return Height;
    }

    /// <summary>
    /// Renders the image to the specified graphics surface.
    /// </summary>
    public override void Render(Graphics g, ReportRenderContext context, Cm actualTop, Cm actualHeight)
    {
        if (!Visible || _image == null) return;

        DrawBackground(g, context, actualTop, actualHeight);

        float x = CmToPx(Left, context);
        float y = CmToPx(actualTop, context);
        float w = CmToPx(Width, context);
        float h = CmToPx(actualHeight, context);

        switch (Sizing)
        {
            case ImageSizing.Normal:
                g.DrawImage(_image, x, y, w, h);
                break;
            case ImageSizing.Zoom:
            {
                float imgAspect = (float)_image.Width / _image.Height;
                float ctrlAspect = w / h;

                if (imgAspect > ctrlAspect)
                {
                    float scaledH = w / imgAspect;
                    g.DrawImage(_image, x, y + (h - scaledH) / 2f, w, scaledH);
                }
                else
                {
                    float scaledW = h * imgAspect;
                    g.DrawImage(_image, x + (w - scaledW) / 2f, y, scaledW, h);
                }
                break;
            }
            case ImageSizing.Stretch:
            default:
                g.DrawImage(_image, x, y, w, h);
                break;
        }
    }
}
