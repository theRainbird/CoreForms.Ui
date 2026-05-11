using CoreForms.Ui.Core;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Containers;

/// <summary>
/// Specifies how a ToolStripItem displays its content.
/// </summary>
public enum ToolStripItemDisplayStyle
{
    /// <summary>
    /// Only the image is displayed.
    /// </summary>
    Image,

    /// <summary>
    /// Only the text is displayed.
    /// </summary>
    Text,

    /// <summary>
    /// Both image and text are displayed.
    /// </summary>
    ImageAndText
}

/// <summary>
/// Specifies how an image is scaled within a ToolStripItem.
/// </summary>
public enum ToolStripItemImageScaling
{
    /// <summary>
    /// The image is not scaled.
    /// </summary>
    None,

    /// <summary>
    /// The image is scaled to fit the item size, maintaining aspect ratio.
    /// </summary>
    SizeToFit,

    /// <summary>
    /// The image is scaled to fit the item size, stretching if necessary.
    /// </summary>
    ScaleToFit
}

/// <summary>
/// Specifies the text alignment within a ToolStripItem.
/// </summary>
public enum ToolStripItemTextAlign
{
    /// <summary>
    /// Text is aligned to the left.
    /// </summary>
    Left,

    /// <summary>
    /// Text is centered.
    /// </summary>
    Center,

    /// <summary>
    /// Text is aligned to the right.
    /// </summary>
    Right
}

/// <summary>
/// Base class for all items that can be hosted in a ToolStrip or MenuStrip.
/// Provides common properties and rendering infrastructure.
/// </summary>
public abstract class ToolStripItem : Component
{
    private string _text = string.Empty;
    private bool _enabled = true;
    private bool _visible = true;
    private SvgImage? _image;
    private ToolStripItemDisplayStyle _displayStyle = ToolStripItemDisplayStyle.ImageAndText;
    private ToolStripItemImageScaling _imageScaling = ToolStripItemImageScaling.SizeToFit;
    private ToolStripItemTextAlign _textAlign = ToolStripItemTextAlign.Left;
    private Padding _padding;
    private bool _isHovered;
    private bool _isPressed;

    /// <summary>
    /// Gets or sets the text displayed on the item.
    /// </summary>
    public string Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value;
                OnTextChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the item can respond to user interaction.
    /// </summary>
    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled != value)
            {
                _enabled = value;
                OnEnabledChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the item is visible.
    /// </summary>
    public bool Visible
    {
        get => _visible;
        set
        {
            if (_visible != value)
            {
                _visible = value;
                OnVisibleChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the image displayed on the item.
    /// </summary>
    public SvgImage? Image
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
    /// Gets or sets how the item displays its content (image, text, or both).
    /// </summary>
    public ToolStripItemDisplayStyle DisplayStyle
    {
        get => _displayStyle;
        set
        {
            if (_displayStyle != value)
            {
                _displayStyle = value;
                OnDisplayStyleChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets how the image is scaled within the item.
    /// </summary>
    public ToolStripItemImageScaling ImageScaling
    {
        get => _imageScaling;
        set
        {
            if (_imageScaling != value)
            {
                _imageScaling = value;
                OnImageScalingChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the text alignment within the item.
    /// </summary>
    public ToolStripItemTextAlign TextAlign
    {
        get => _textAlign;
        set
        {
            if (_textAlign != value)
            {
                _textAlign = value;
                OnTextAlignChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the internal spacing within the item.
    /// </summary>
    public Padding Padding
    {
        get => _padding;
        set
        {
            if (_padding.Left != value.Left || _padding.Top != value.Top ||
                _padding.Right != value.Right || _padding.Bottom != value.Bottom)
            {
                _padding = value;
                OnPaddingChanged();
            }
        }
    }

    /// <summary>
    /// Gets whether the item is currently hovered by the mouse.
    /// </summary>
    public bool IsHovered
    {
        get => _isHovered;
        internal set
        {
            if (_isHovered != value)
            {
                _isHovered = value;
                OnHoverChanged();
            }
        }
    }

    /// <summary>
    /// Gets whether the item currently has a mouse button pressed.
    /// </summary>
    public bool IsPressed
    {
        get => _isPressed;
        internal set
        {
            if (_isPressed != value)
            {
                _isPressed = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the parent ToolStrip that owns this item.
    /// </summary>
    internal ToolStrip? Owner { get; set; }

    /// <summary>
    /// Occurs when the item is clicked.
    /// </summary>
    public event EventHandler? Click;

    /// <summary>
    /// Occurs when the text changes.
    /// </summary>
    public event EventHandler? TextChanged;

    /// <summary>
    /// Raises the Click event.
    /// </summary>
    public void PerformClick()
    {
        if (_enabled)
        {
            OnClick();
        }
    }

    /// <summary>
    /// Raises the Click event. Override to add custom click behavior.
    /// </summary>
    protected virtual void OnClick()
    {
        Click?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Gets the display text with the mnemonic ampersand removed.
    /// </summary>
    public string DisplayText => StripMnemonic(_text);

    /// <summary>
    /// Gets the mnemonic key character, or null if none is specified.
    /// </summary>
    public char? Mnemonic => GetMnemonicChar(_text);

    /// <summary>
    /// Returns the index of the mnemonic character in the display text.
    /// </summary>
    internal int MnemonicIndex => GetMnemonicIndex(_text);

    /// <summary>
    /// Calculates the preferred width of the item for layout purposes.
    /// </summary>
    /// <param name="font">The font to use for text measurement.</param>
    /// <param name="zoom">The current zoom factor.</param>
    /// <returns>The preferred width in pixels.</returns>
    public abstract int GetPreferredWidth(Font font, float zoom);

    /// <summary>
    /// Renders the item within the specified bounds.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
    /// <param name="x">The x-coordinate of the item bounds.</param>
    /// <param name="y">The y-coordinate of the item bounds.</param>
    /// <param name="width">The width of the item bounds.</param>
    /// <param name="height">The height of the item bounds.</param>
    /// <param name="font">The font to use for text rendering.</param>
    /// <param name="zoom">The current zoom factor.</param>
    /// <param name="hovered">Whether the item is currently hovered.</param>
    /// <param name="pressed">Whether the item is currently pressed.</param>
    public virtual void OnPaint(Graphics g, int x, int y, int width, int height, Font font, float zoom, bool hovered, bool pressed)
    {
        if (!_visible) return;

        var textColor = _enabled ? Color.Black : SystemColors.GrayText;
        int contentX = x + _padding.Left;
        int contentY = y + _padding.Top;
        int contentWidth = width - _padding.Horizontal;
        int contentHeight = height - _padding.Vertical;

        bool showImage = _displayStyle == ToolStripItemDisplayStyle.Image ||
                         _displayStyle == ToolStripItemDisplayStyle.ImageAndText;
        bool showText = _displayStyle == ToolStripItemDisplayStyle.Text ||
                        _displayStyle == ToolStripItemDisplayStyle.ImageAndText;

        int imageWidth = 0;
        int imageHeight = 0;

        if (showImage && _image != null)
        {
            imageWidth = CalculateImageWidth(_image, contentHeight);
            imageHeight = contentHeight;

            int imageX = CalculateImageX(contentX, contentWidth, imageWidth, showText);
            int imageY = contentY + (contentHeight - imageHeight) / 2;

            g.DrawImage(_image, imageX, imageY, imageWidth, imageHeight);
        }

        if (showText && !string.IsNullOrEmpty(DisplayText))
        {
            int textX = CalculateTextX(contentX, contentWidth, imageWidth, showImage);
            int textY = contentY + (contentHeight - (int)font.Size) / 2;

            RenderTextWithMnemonic(g, DisplayText, font, textColor, textX, textY, zoom);
        }
    }

    /// <summary>
    /// Tests whether the specified point is within the item bounds.
    /// </summary>
    /// <param name="x">The x-coordinate relative to the parent ToolStrip.</param>
    /// <param name="y">The y-coordinate relative to the parent ToolStrip.</param>
    /// <param name="itemX">The x-coordinate of the item bounds.</param>
    /// <param name="itemY">The y-coordinate of the item bounds.</param>
    /// <param name="itemWidth">The width of the item bounds.</param>
    /// <param name="itemHeight">The height of the item bounds.</param>
    /// <returns>True if the point is within the item bounds; otherwise, false.</returns>
    public virtual bool HitTest(int x, int y, int itemX, int itemY, int itemWidth, int itemHeight)
    {
        return x >= itemX && x < itemX + itemWidth && y >= itemY && y < itemY + itemHeight;
    }

    /// <summary>
    /// Called when the text changes.
    /// </summary>
    protected virtual void OnTextChanged()
    {
        TextChanged?.Invoke(this, EventArgs.Empty);
        Owner?.Invalidate();
    }

    /// <summary>
    /// Called when the enabled state changes.
    /// </summary>
    protected virtual void OnEnabledChanged()
    {
        Owner?.Invalidate();
    }

    /// <summary>
    /// Called when the visibility changes.
    /// </summary>
    protected virtual void OnVisibleChanged()
    {
        Owner?.Invalidate();
    }

    /// <summary>
    /// Called when the image changes.
    /// </summary>
    protected virtual void OnImageChanged()
    {
        Owner?.Invalidate();
    }

    /// <summary>
    /// Called when the display style changes.
    /// </summary>
    protected virtual void OnDisplayStyleChanged()
    {
        Owner?.Invalidate();
    }

    /// <summary>
    /// Called when the image scaling changes.
    /// </summary>
    protected virtual void OnImageScalingChanged()
    {
        Owner?.Invalidate();
    }

    /// <summary>
    /// Called when the text alignment changes.
    /// </summary>
    protected virtual void OnTextAlignChanged()
    {
        Owner?.Invalidate();
    }

    /// <summary>
    /// Called when the padding changes.
    /// </summary>
    protected virtual void OnPaddingChanged()
    {
        Owner?.Invalidate();
    }

    /// <summary>
    /// Called when the hover state changes.
    /// </summary>
    protected virtual void OnHoverChanged()
    {
        Owner?.Invalidate();
    }

    /// <summary>
    /// Calculates the scaled image width based on the ImageScaling setting.
    /// </summary>
    protected int CalculateImageWidth(SvgImage image, int availableHeight)
    {
        return _imageScaling switch
        {
            ToolStripItemImageScaling.None => image.Width,
            ToolStripItemImageScaling.SizeToFit => Math.Min(image.Width, availableHeight),
            ToolStripItemImageScaling.ScaleToFit => availableHeight,
            _ => image.Width
        };
    }

    /// <summary>
    /// Measures the width of text using the platform text measurement.
    /// </summary>
    protected int MeasureTextWidth(string text, Font font, float zoom)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        var measured = Platform.Platform.MeasureText(text, font, zoom);
        return (int)(measured.width / zoom);
    }

    private int CalculateImageX(int contentX, int contentWidth, int imageWidth, bool showText)
    {
        if (!showText)
        {
            return _textAlign switch
            {
                ToolStripItemTextAlign.Center => contentX + (contentWidth - imageWidth) / 2,
                ToolStripItemTextAlign.Right => contentX + contentWidth - imageWidth - 4,
                _ => contentX + 4
            };
        }
        return contentX + 4;
    }

    private int CalculateTextX(int contentX, int contentWidth, int imageWidth, bool showImage)
    {
        int baseX = showImage ? contentX + imageWidth + 6 : contentX + 4;
        return _textAlign switch
        {
            ToolStripItemTextAlign.Center => contentX + (contentWidth - MeasureTextWidth(DisplayText, Font.Default, 1.0f)) / 2,
            ToolStripItemTextAlign.Right => contentX + contentWidth - MeasureTextWidth(DisplayText, Font.Default, 1.0f) - 4,
            _ => baseX
        };
    }

    private void RenderTextWithMnemonic(Graphics g, string text, Font font, Color color, int x, int y, float zoom)
    {
        int mnemonicIdx = MnemonicIndex;
        g.DrawString(text, font, color, x, y);

        if (mnemonicIdx >= 0 && Owner != null)
        {
            float scaledFontSize = font.Size * zoom;
            int charWidth = (int)(scaledFontSize / 2);
            int underlineX = x + mnemonicIdx * charWidth;
            int underlineY = y + (int)scaledFontSize;
            g.DrawLine(color, underlineX, underlineY, underlineX + charWidth, underlineY);
        }
    }

    private static string StripMnemonic(string text)
    {
        if (text == null) return string.Empty;
        int idx = text.IndexOf('&');
        if (idx >= 0 && idx < text.Length - 1)
            return text.Substring(0, idx) + text.Substring(idx + 1);
        if (idx >= 0 && idx == text.Length - 1)
            return text.Substring(0, idx);
        return text;
    }

    private static char? GetMnemonicChar(string text)
    {
        if (text == null) return null;
        int idx = text.IndexOf('&');
        if (idx >= 0 && idx < text.Length - 1)
            return char.ToUpperInvariant(text[idx + 1]);
        return null;
    }

    private static int GetMnemonicIndex(string text)
    {
        if (text == null) return -1;
        int idx = text.IndexOf('&');
        if (idx >= 0 && idx < text.Length - 1)
            return idx;
        return -1;
    }
}
