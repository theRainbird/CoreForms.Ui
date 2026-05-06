using CoreForms.Ui.Core;
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
        BackColor = SystemColors.Control;
        Size = new Size(200, 150);
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
    /// Renders the panel with its background and border.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
    public override void Render(Graphics g)
    {
        if (!Visible) return;

        g.FillRectangle(BackColor, 0, 0, Width, Height);

        if (_borderStyle == BorderStyle.FixedSingle)
        {
            g.DrawRectangle(Color.FromArgb(128, 128, 128), 0, 0, Width, Height, 1);
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