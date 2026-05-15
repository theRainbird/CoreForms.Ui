using CoreForms.Ui.Controls.Basic;
using Xunit;

namespace CoreForms.Ui.Tests;

public class SpinnerTests
{
    [Fact]
    public void Spinner_DefaultValues_ShouldBeCorrect()
    {
        var spinner = new Spinner();

        Assert.False(spinner.Active);
        Assert.True(spinner.AutoStart);
        Assert.Equal(8, spinner.DotCount);
        Assert.Equal(4, spinner.DotRadius);
        Assert.Equal(12, spinner.SpinnerRadius);
        Assert.Equal(80, spinner.AnimationInterval);
    }

    [Fact]
    public void Spinner_Active_ShouldFireActiveChanged()
    {
        var spinner = new Spinner();
        var fired = false;

        spinner.ActiveChanged += (s, e) => fired = true;
        spinner.Active = true;

        Assert.True(fired);
        Assert.True(spinner.Active);
    }

    [Fact]
    public void Spinner_ActiveFalse_ShouldFireActiveChanged()
    {
        var spinner = new Spinner { Active = true };
        var fired = false;

        spinner.ActiveChanged += (s, e) => fired = true;
        spinner.Active = false;

        Assert.True(fired);
        Assert.False(spinner.Active);
    }

    [Fact]
    public void Spinner_ActiveSetSameValue_ShouldNotFire()
    {
        var spinner = new Spinner();
        var fired = false;

        spinner.ActiveChanged += (s, e) => fired = true;
        spinner.Active = false;

        Assert.False(fired);
    }

    [Fact]
    public void Spinner_DotCount_ShouldClampToMinimum()
    {
        var spinner = new Spinner { DotCount = 1 };
        Assert.Equal(3, spinner.DotCount);
    }

    [Fact]
    public void Spinner_DotCount_ShouldAllowValidValues()
    {
        var spinner = new Spinner { DotCount = 12 };
        Assert.Equal(12, spinner.DotCount);
    }

    [Fact]
    public void Spinner_DotRadius_ShouldClampToMinimum()
    {
        var spinner = new Spinner { DotRadius = 0 };
        Assert.Equal(1, spinner.DotRadius);
    }

    [Fact]
    public void Spinner_SpinnerRadius_ShouldClampToMinimum()
    {
        var spinner = new Spinner { SpinnerRadius = 0 };
        Assert.Equal(1, spinner.SpinnerRadius);
    }

    [Fact]
    public void Spinner_AnimationInterval_ShouldClampToMinimum()
    {
        var spinner = new Spinner { AnimationInterval = 0 };
        Assert.Equal(1, spinner.AnimationInterval);
    }

    [Fact]
    public void Spinner_AutoStart_ShouldSetActiveOnVisible()
    {
        var spinner = new Spinner { AutoStart = true, Visible = false };

        Assert.False(spinner.Active);

        spinner.Visible = true;

        Assert.True(spinner.Active);
    }

    [Fact]
    public void Spinner_AutoStart_ShouldStopOnInvisible()
    {
        var spinner = new Spinner { AutoStart = true, Visible = false };
        spinner.Visible = true;
        Assert.True(spinner.Active);

        spinner.Visible = false;

        Assert.False(spinner.Active);
    }

    [Fact]
    public void Spinner_AutoStartFalse_ShouldNotStartOnVisible()
    {
        var spinner = new Spinner { AutoStart = false, Visible = false };

        spinner.Visible = true;

        Assert.False(spinner.Active);
    }

    [Fact]
    public void Spinner_ActiveReset_ShouldRestartAnimation()
    {
        var spinner = new Spinner();

        spinner.Active = true;
        Assert.True(spinner.Active);

        spinner.Active = false;
        Assert.False(spinner.Active);
    }
}
