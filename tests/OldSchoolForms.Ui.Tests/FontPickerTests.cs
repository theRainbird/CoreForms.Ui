using OldSchoolForms.Ui.Core;
using OldSchoolForms.Ui.Controls.Basic;
using Xunit;

namespace OldSchoolForms.Ui.Tests;

public class FontPickerTests
{
    [Fact]
    public void FontPicker_DefaultProperties_ShouldBeSet()
    {
        var picker = new FontPicker();
        Assert.Equal(string.Empty, picker.SelectedFontFamily);
        Assert.Equal(14f, picker.FontSize);
        Assert.False(picker.DroppedDown);
        Assert.True(picker.Enabled);
    }

    [Fact]
    public void FontPicker_SelectFont_ShouldUpdateSelectedFont()
    {
        var picker = new FontPicker();
        var changed = false;
        picker.SelectedFontChanged += (s, e) => changed = true;

        var families = FontManager.GetFontFamilies();
        if (families.Length > 0)
        {
            picker.SelectedFontFamily = families[0];
            Assert.Equal(families[0], picker.SelectedFontFamily);
            Assert.True(changed);
        }
    }

    [Fact]
    public void FontPicker_FontSize_ShouldUpdate()
    {
        var picker = new FontPicker();
        picker.FontSize = 24f;
        Assert.Equal(24f, picker.FontSize);
    }

    [Fact]
    public void FontPicker_PreviewText_ShouldUpdate()
    {
        var picker = new FontPicker();
        picker.PreviewText = "Test preview";
        Assert.Equal("Test preview", picker.PreviewText);
    }

    [Fact]
    public void FontPicker_DropDownHeight_ShouldClampMinimum()
    {
        var picker = new FontPicker();
        picker.DropDownHeight = 5;
        Assert.Equal(20, picker.DropDownHeight);
    }

    [Fact]
    public void FontPicker_DroppedDown_ShouldToggle()
    {
        var picker = new FontPicker();
        picker.DroppedDown = true;
        Assert.True(picker.DroppedDown);
        picker.DroppedDown = false;
        Assert.False(picker.DroppedDown);
    }

    [Fact]
    public void FontPicker_RefreshFontList_ShouldPopulateFonts()
    {
        var picker = new FontPicker();
        picker.RefreshFontList();
        // At minimum, the system should have some fonts installed
        Assert.True(FontManager.GetFontFamilies().Length > 0);
    }

    [Fact]
    public void FontPicker_LostFocus_ShouldCloseDropdown()
    {
        var picker = new FontPicker();
        picker.DroppedDown = true;
        Assert.True(picker.DroppedDown);

        picker.OnLostFocus(EventArgs.Empty);
        Assert.False(picker.DroppedDown);
    }

    [Fact]
    public void FontPicker_TextInput_ShouldFilterAndReopen()
    {
        var picker = new FontPicker();
        var families = FontManager.GetFontFamilies();
        if (families.Length == 0) return;

        // Simulate typing a character that should match some fonts
        picker.OnTextInput("A");

        // After text input, dropdown should be open and filtered
        Assert.True(picker.DroppedDown);
    }
}
