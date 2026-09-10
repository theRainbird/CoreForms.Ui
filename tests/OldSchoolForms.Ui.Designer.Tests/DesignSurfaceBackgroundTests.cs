using System.Linq;
using OldSchoolForms.Ui.Core;
using OldSchoolForms.Ui.Rendering;
using Xunit;

namespace OldSchoolForms.Ui.Designer.Tests;

/// <summary>
/// Verifies that the design-time form and user control paint their base area in their own
/// background color instead of a hardcoded white, so changes made in the PropertyGrid are
/// visible on the design surface.
/// </summary>
public class DesignSurfaceBackgroundTests
{
    private static DrawCommand? FindBackgroundFill(Graphics g, Control control)
        => g.GetCommands()
            .FirstOrDefault(c => c.Type == DrawCommandType.FillRectangle
                && Math.Abs(c.X - control.X) < 0.5f
                && Math.Abs(c.Y - control.Y) < 0.5f
                && Math.Abs(c.Width - control.Width) < 0.5f
                && Math.Abs(c.Height - control.Height) < 0.5f);

    [Fact]
    public void DesignForm_RendersBackgroundWithBackColor_NotWhite()
    {
        var form = new DesignForm();
        var backColor = Color.FromArgb(10, 20, 30);
        form.BackColor = backColor;

        using var g = new Graphics();
        form.Render(g);

        var bg = FindBackgroundFill(g, form);

        Assert.NotNull(bg);
        Assert.NotEqual(Color.White, bg!.Color);
        Assert.Equal(backColor.R, bg!.Color.R);
        Assert.Equal(backColor.G, bg!.Color.G);
        Assert.Equal(backColor.B, bg!.Color.B);
    }

    [Fact]
    public void DesignForm_BackColor_Change_IsReflectedInRender()
    {
        var form = new DesignForm();

        using var g1 = new Graphics();
        form.Render(g1);
        var original = FindBackgroundFill(g1, form)!;

        form.BackColor = Color.FromArgb(40, 50, 60);

        using var g2 = new Graphics();
        form.Render(g2);
        var updated = FindBackgroundFill(g2, form)!;

        Assert.NotEqual(original!.Color, updated.Color);
        Assert.Equal(40, updated.Color.R);
        Assert.Equal(50, updated.Color.G);
        Assert.Equal(60, updated.Color.B);
    }

    [Fact]
    public void DesignUserControl_RendersBackgroundWithBackColor_NotWhite()
    {
        var control = new DesignUserControl();
        var backColor = Color.FromArgb(11, 22, 33);
        control.BackColor = backColor;

        using var g = new Graphics();
        control.Render(g);

        var bg = FindBackgroundFill(g, control);

        Assert.NotNull(bg);
        Assert.NotEqual(Color.White, bg!.Color);
        Assert.Equal(backColor.R, bg!.Color.R);
        Assert.Equal(backColor.G, bg!.Color.G);
        Assert.Equal(backColor.B, bg!.Color.B);
    }
}
