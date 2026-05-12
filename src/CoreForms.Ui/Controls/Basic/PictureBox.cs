using CoreForms.Ui.Core;
using CoreForms.Ui.Theming;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Basic;

/// <summary>
/// Specifies how an image is displayed within a PictureBox.
/// </summary>
public enum PictureBoxSizeMode
{
    /// <summary>
    /// The image is displayed at its original size.
    /// </summary>
    Normal,

    /// <summary>
    /// The image is stretched to fill the entire PictureBox.
    /// </summary>
    StretchImage,

    /// <summary>
    /// The image is centered within the PictureBox at its original size.
    /// </summary>
    CenterImage,

    /// <summary>
    /// The image is scaled to fit within the PictureBox while maintaining aspect ratio.
    /// </summary>
    Zoom
}

/// <summary>
/// Displays an image in a Windows Forms-style picture box control.
/// Supports both vector (SVG) and raster images with various sizing modes.
/// </summary>
public class PictureBox : Control
{
    private IGraphicsImage? _image;
    private PictureBoxSizeMode _sizeMode = PictureBoxSizeMode.Normal;

    /// <summary>
    /// Initializes a new instance of PictureBox.
    /// </summary>
    public PictureBox()
    {
        _backColor = Color.Transparent;
        Size = new Size(100, 100);
    }

    /// <summary>
    /// Gets or sets the image displayed in the PictureBox.
    /// </summary>
    public IGraphicsImage? Image
    {
        get => _image;
        set
        {
            if (_image != value)
            {
                _image = value;
                OnImageChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets how the image is sized and positioned within the PictureBox.
    /// </summary>
    public PictureBoxSizeMode SizeMode
    {
        get => _sizeMode;
        set
        {
            if (_sizeMode != value)
            {
                _sizeMode = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Occurs when the Image property changes.
    /// </summary>
    public event EventHandler? ImageChanged;

    /// <summary>
    /// Renders the PictureBox with its image.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
    public override void Render(Graphics g)
    {
        if (!Visible) return;

        if (BackColor.A > 0)
        {
            g.FillRectangle(BackColor, 0, 0, Width, Height);
        }

        if (_image != null)
        {
            DrawImageWithSizeMode(g, _image);
        }

        base.Render(g);
    }

    private void DrawImageWithSizeMode(Graphics g, IGraphicsImage image)
    {
        int drawX, drawY, drawWidth, drawHeight;

        switch (_sizeMode)
        {
            case PictureBoxSizeMode.Normal:
                drawX = 0;
                drawY = 0;
                drawWidth = image.Width;
                drawHeight = image.Height;
                break;

            case PictureBoxSizeMode.StretchImage:
                drawX = 0;
                drawY = 0;
                drawWidth = Width;
                drawHeight = Height;
                break;

            case PictureBoxSizeMode.CenterImage:
                drawWidth = image.Width;
                drawHeight = image.Height;
                drawX = (Width - drawWidth) / 2;
                drawY = (Height - drawHeight) / 2;
                break;

            case PictureBoxSizeMode.Zoom:
                float scaleX = (float)Width / image.Width;
                float scaleY = (float)Height / image.Height;
                float scale = Math.Min(scaleX, scaleY);
                drawWidth = (int)(image.Width * scale);
                drawHeight = (int)(image.Height * scale);
                drawX = (Width - drawWidth) / 2;
                drawY = (Height - drawHeight) / 2;
                break;

            default:
                drawX = 0;
                drawY = 0;
                drawWidth = image.Width;
                drawHeight = image.Height;
                break;
        }

        g.DrawImage(image, drawX, drawY, drawWidth, drawHeight);
    }

    /// <summary>
    /// Called when the Image property changes.
    /// </summary>
    protected virtual void OnImageChanged()
    {
        ImageChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }
}
