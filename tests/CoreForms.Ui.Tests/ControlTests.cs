using CoreForms.Ui.Core;
using CoreForms.Ui.Controls.Basic;
using Xunit;

namespace CoreForms.Ui.Tests;

public class ControlTests
{
    [Fact]
    public void Control_Bounds_ShouldInitializeCorrectly()
    {
        var control = new Label();
        Assert.Equal(0, control.X);
        Assert.Equal(0, control.Y);
        Assert.Equal(100, control.Width);
        Assert.Equal(20, control.Height);
    }

    [Fact]
    public void Control_SetBounds_ShouldTriggerEvent()
    {
        var label = new Label { Text = "Test" };
        var boundsChanged = false;

        label.BoundsChanged += (s, e) => boundsChanged = true;
        label.Bounds = new Rectangle(10, 20, 150, 40);

        Assert.True(boundsChanged);
        Assert.Equal(10, label.X);
        Assert.Equal(20, label.Y);
        Assert.Equal(150, label.Width);
        Assert.Equal(40, label.Height);
    }

    [Fact]
    public void Control_Location_ShouldWork()
    {
        var label = new Label { Location = new Point(50, 60) };
        Assert.Equal(50, label.X);
        Assert.Equal(60, label.Y);
        Assert.Equal(new Point(50, 60), label.Location);
    }

    [Fact]
    public void Control_Visible_ShouldDefaultToTrue()
    {
        var control = new Label();
        Assert.True(control.Visible);
    }

    [Fact]
    public void Control_ParentChildRelationship_ShouldWork()
    {
        var panel = new CoreForms.Ui.Controls.Containers.Panel();
        var label = new Label { Text = "Child" };

        label.Parent = panel;
        Assert.Equal(panel, label.Parent);
        Assert.Single(panel.Controls);
    }

    [Fact]
    public void Button_Click_ShouldFireEvent()
    {
        var button = new Button { Text = "Click Me" };
        var clicked = false;

        button.Click += (s, e) => clicked = true;
        button.PerformClick();

        Assert.True(clicked);
    }

    [Fact]
    public void ListBox_Items_ShouldWork()
    {
        var listBox = new ListBox();
        listBox.Items.Add("Item 1");
        listBox.Items.Add("Item 2");
        listBox.Items.Add("Item 3");

        Assert.Equal(3, listBox.Items.Count);
        Assert.Equal("Item 1", listBox.Items[0]);
    }

    [Fact]
    public void ListBox_SelectedIndex_ShouldUpdate()
    {
        var listBox = new ListBox();
        listBox.Items.Add("A");
        listBox.Items.Add("B");
        listBox.Items.Add("C");

        listBox.SelectedIndex = 1;
        Assert.Equal(1, listBox.SelectedIndex);
        Assert.Equal("B", listBox.SelectedItem);
    }

    [Fact]
    public void CheckBox_Checked_ShouldToggle()
    {
        var checkBox = new CheckBox { Text = "Agree" };
        Assert.False(checkBox.Checked);

        checkBox.Checked = true;
        Assert.True(checkBox.Checked);
    }

    [Fact]
    public void CheckBox_OnMouseUp_ShouldToggleChecked()
    {
        var checkBox = new CheckBox { Text = "Agree" };
        Assert.False(checkBox.Checked);

        checkBox.OnMouseUp(EventArgs.Empty);
        Assert.True(checkBox.Checked);

        checkBox.OnMouseUp(EventArgs.Empty);
        Assert.False(checkBox.Checked);
    }

    [Fact]
    public void CheckBox_OnMouseUp_ShouldFireCheckedChanged()
    {
        var checkBox = new CheckBox { Text = "Agree" };
        var changedCount = 0;
        checkBox.CheckedChanged += (s, e) => changedCount++;

        checkBox.OnMouseUp(EventArgs.Empty);
        Assert.Equal(1, changedCount);

        checkBox.OnMouseUp(EventArgs.Empty);
        Assert.Equal(2, changedCount);
    }

    [Fact]
    public void RadioButton_OnMouseUp_ShouldSetChecked()
    {
        var radioButton = new RadioButton { Text = "Option 1" };
        Assert.False(radioButton.Checked);

        radioButton.OnMouseUp(EventArgs.Empty);
        Assert.True(radioButton.Checked);
    }

    [Fact]
    public void Button_OnMouseUp_ShouldFireClick()
    {
        var button = new Button { Text = "Click Me" };
        var clicked = false;
        button.Click += (s, e) => clicked = true;

        button.OnMouseDown(EventArgs.Empty);
        button.OnMouseUp(EventArgs.Empty);

        Assert.True(clicked);
    }

    [Fact]
    public void RadioButton_Checked_ShouldAffectGroup()
    {
        var panel = new CoreForms.Ui.Controls.Containers.Panel();
        var radio1 = new RadioButton { Text = "Option 1", Parent = panel };
        var radio2 = new RadioButton { Text = "Option 2", Parent = panel };

        radio1.Checked = true;
        Assert.True(radio1.Checked);
        Assert.False(radio2.Checked);

        radio2.Checked = true;
        Assert.False(radio1.Checked);
        Assert.True(radio2.Checked);
    }

    [Fact]
    public void ProgressBar_Value_ShouldBeClamped()
    {
        var progressBar = new ProgressBar { Minimum = 0, Maximum = 100 };
        progressBar.Value = 50;
        Assert.Equal(50, progressBar.Value);

        progressBar.Value = 150;
        Assert.Equal(100, progressBar.Value);

        progressBar.Value = -10;
        Assert.Equal(0, progressBar.Value);
    }

    [Fact]
    public void ComboBox_Items_ShouldWork()
    {
        var comboBox = new ComboBox();
        comboBox.Items.Add("Choice 1");
        comboBox.Items.Add("Choice 2");

        Assert.Equal(2, comboBox.Items.Count);
    }
}