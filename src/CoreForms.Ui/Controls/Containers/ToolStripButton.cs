using CoreForms.Ui.Core;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Containers;

/// <summary>
/// Represents a button in a ToolStrip that can display text, an image, or both.
/// Supports dropdown menus, check-on-click behavior, and standard button states.
/// </summary>
public class ToolStripButton : ToolStripItem
{
    private bool _checkOnClick;
    private bool _checked;
    private readonly List<ToolStripItem> _dropDownItems = new();

    /// <summary>
    /// Initializes a new instance of ToolStripButton.
    /// </summary>
    public ToolStripButton() { }

    /// <summary>
    /// Initializes a new instance of ToolStripButton with the specified text.
    /// </summary>
    /// <param name="text">The button text.</param>
    public ToolStripButton(string text)
    {
        Text = text;
    }

    /// <summary>
    /// Initializes a new instance of ToolStripButton with the specified image.
    /// </summary>
    /// <param name="image">The button image.</param>
    public ToolStripButton(SvgImage image)
    {
        Image = image;
    }

    /// <summary>
    /// Initializes a new instance of ToolStripButton with the specified text and image.
    /// </summary>
    /// <param name="text">The button text.</param>
    /// <param name="image">The button image.</param>
    public ToolStripButton(string text, SvgImage image)
    {
        Text = text;
        Image = image;
    }

    /// <summary>
    /// Gets or sets whether the button toggles its checked state when clicked.
    /// </summary>
    public bool CheckOnClick
    {
        get => _checkOnClick;
        set => _checkOnClick = value;
    }

    /// <summary>
    /// Gets or sets whether the button is in the checked state.
    /// </summary>
    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked != value)
            {
                _checked = value;
                OnCheckedChanged();
            }
        }
    }

    /// <summary>
    /// Gets the collection of dropdown items. When non-empty, a dropdown arrow is displayed.
    /// </summary>
    public List<ToolStripItem> DropDownItems => _dropDownItems;

    /// <summary>
    /// Occurs when the checked state changes.
    /// </summary>
    public event EventHandler? CheckedChanged;

    /// <summary>
    /// Occurs when the dropdown is opened.
    /// </summary>
    public event EventHandler? DropDownOpened;

    /// <summary>
    /// Occurs when the dropdown is closed.
    /// </summary>
    public event EventHandler? DropDownClosed;

    /// <summary>
    /// Raises the Click event and toggles the checked state if CheckOnClick is enabled.
    /// </summary>
    protected override void OnClick()
    {
        if (_checkOnClick)
        {
            _checked = !_checked;
            OnCheckedChanged();
        }
        base.OnClick();
    }

    /// <summary>
    /// Raises the CheckedChanged event.
    /// </summary>
    protected virtual void OnCheckedChanged()
    {
        CheckedChanged?.Invoke(this, EventArgs.Empty);
        Owner?.Invalidate();
    }

    /// <summary>
    /// Raises the DropDownOpened event.
    /// </summary>
    internal void OnDropDownOpened()
    {
        DropDownOpened?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Raises the DropDownClosed event.
    /// </summary>
    internal void OnDropDownClosed()
    {
        DropDownClosed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Renders the button with its text, image, and state indicators.
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
    public override void OnPaint(Graphics g, int x, int y, int width, int height, Font font, float zoom, bool hovered, bool pressed)
    {
        if (!Visible) return;

        bool isActive = hovered || pressed || _checked;
        if (isActive)
        {
            Color bgColor = _checked ? Color.FromArgb(180, 200, 230) : Color.FromArgb(200, 200, 200);
            g.FillRectangle(bgColor, x, y, width, height);
        }

        if (_checked)
        {
            g.DrawRectangle(Color.FromArgb(100, 140, 200), x, y, width - 1, height - 1, 1);
        }

        bool showImage = DisplayStyle == ToolStripItemDisplayStyle.Image ||
                         DisplayStyle == ToolStripItemDisplayStyle.ImageAndText;
        bool showText = DisplayStyle == ToolStripItemDisplayStyle.Text ||
                        DisplayStyle == ToolStripItemDisplayStyle.ImageAndText;

        int contentX = x + Padding.Left + 4;
        int imageWidth = 0;

        if (showImage && Image != null)
        {
            imageWidth = CalculateImageWidth(Image, height - Padding.Vertical - 2);
            int imageY = y + Padding.Top + (height - Padding.Vertical - imageWidth) / 2;
            g.DrawImage(Image, contentX, imageY, imageWidth, imageWidth);
        }

        if (showText && !string.IsNullOrEmpty(DisplayText))
        {
            int textX = showImage ? contentX + imageWidth + 4 : contentX;
            var textColor = Enabled ? Color.Black : SystemColors.GrayText;
            g.DrawString(DisplayText, font, textColor, textX, y + (height - (int)font.Size) / 2);
        }

        if (_dropDownItems.Count > 0)
        {
            int arrowX = x + width - 12;
            int arrowY = y + height / 2;
            g.FillTriangle(Color.FromArgb(100, 100, 100),
                arrowX - 3, arrowY - 2,
                arrowX + 3, arrowY - 2,
                arrowX, arrowY + 2);
        }
    }

    /// <summary>
    /// Calculates the preferred width for layout.
    /// </summary>
    /// <param name="font">The font to use for text measurement.</param>
    /// <param name="zoom">The current zoom factor.</param>
    /// <returns>The preferred width in pixels.</returns>
    public override int GetPreferredWidth(Font font, float zoom)
    {
        int width = 8;

        if (DisplayStyle == ToolStripItemDisplayStyle.Text ||
            DisplayStyle == ToolStripItemDisplayStyle.ImageAndText)
        {
            width += MeasureTextWidth(DisplayText, font, zoom) + 4;
        }

        if ((DisplayStyle == ToolStripItemDisplayStyle.Image ||
             DisplayStyle == ToolStripItemDisplayStyle.ImageAndText) && Image != null)
        {
            width += Image.Width + 4;
        }

        if (_dropDownItems.Count > 0)
        {
            width += 12;
        }

        return width;
    }
}
