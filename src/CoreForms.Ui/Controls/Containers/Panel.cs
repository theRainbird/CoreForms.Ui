using CoreForms.Ui.Core;
using CoreForms.Ui.Theming;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Containers;

/// <summary>
/// A control that serves as a container for other controls.
/// </summary>
public class Panel : ContainerControl
{
    private BorderStyle _borderStyle = BorderStyle.None;

    /// <summary>
    /// Initializes a new instance of Panel.
    /// </summary>
    public Panel()
    {
        _backColor = Color.Transparent;
        _backColorSet = true;
        Size = new Size(200, 150);
        TabStop = false;
    }

    /// <summary>
    /// Gets or sets the border style of the panel.
    /// </summary>
    public BorderStyle BorderStyle
    {
        get => _borderStyle;
        set
        {
            _borderStyle = value;
            Invalidate();
        }
    }

    /// <summary>
    /// Gets the clipping rectangle excluding the border when FixedSingle.
    /// </summary>
    protected override Rectangle GetChildClipRectangle()
    {
        if (_borderStyle == BorderStyle.FixedSingle)
            return new Rectangle(1, 1, Width - 2, Height - 2);
        return base.GetChildClipRectangle();
    }

    /// <summary>
    /// Renders the panel with its background and border.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
    public override void Render(Graphics g)
    {
        if (!Visible) return;

        var theme = ThemeManager.CurrentTheme;

        g.FillRectangle(BackColor, 0, 0, Width, Height);

        if (_borderStyle == BorderStyle.FixedSingle)
        {
            g.DrawRectangle(theme.PanelBorder, 0, 0, Width, Height, 1);
        }

        base.Render(g);
    }
}

/// <summary>
/// Specifies the border style of a Panel.
/// </summary>
public enum BorderStyle
{
    /// <summary>
    /// No border.
    /// </summary>
    None,

    /// <summary>
    /// A single-line border.
    /// </summary>
    FixedSingle,

    /// <summary>
    /// A 3D-style border.
    /// </summary>
    Fixed3D
}