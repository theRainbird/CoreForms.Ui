using CoreForms.Ui.Core;
using CoreForms.Ui.Controls.Containers;
using CoreForms.Ui.Theming;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls;

/// <summary>
/// A user-defined control that serves as a container for other controls, allowing
/// developers to compose reusable composite controls from existing controls.
/// </summary>
public class UserControl : ContainerControl
{
    private BorderStyle _borderStyle = BorderStyle.None;

    /// <summary>
    /// Initializes a new instance of the UserControl class.
    /// </summary>
    public UserControl()
    {
        _backColor = ThemeManager.CurrentTheme.ControlBackground;
        Size = new Size(150, 100);
        TabStop = true;
    }

    /// <summary>
    /// Called when the theme changes. Updates user control-specific colors.
    /// </summary>
    /// <param name="newTheme">The new theme that was activated.</param>
    public override void OnThemeChanged(Theme newTheme)
    {
        if (!_backColorSet)
            _backColor = newTheme.ControlBackground;
        Invalidate();
    }

    /// <summary>
    /// Gets or sets the border style of the user control.
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
    /// Renders the user control with its background and optional border.
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