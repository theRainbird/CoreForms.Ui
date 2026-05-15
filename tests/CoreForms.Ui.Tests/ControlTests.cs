using CoreForms.Ui.Core;
using CoreForms.Ui.Controls.Basic;
using CoreForms.Ui.Controls.Advanced;
using CoreForms.Ui.Controls.Containers;
using CoreForms.Ui.Html;
using Xunit;

namespace CoreForms.Ui.Tests;

public class ControlTests
{
    [Fact]
    public void RichTextEngine_ToggleBold_OnSelection_TogglesRunStyle()
    {
        var engine = new RichTextEngine();
        engine.InitFromHtml("<p>Hello World</p>");

        Assert.Single(engine.Document.Blocks);
        Assert.Single(engine.Document.Blocks[0].Runs);
        Assert.Equal("Hello World", engine.Document.Blocks[0].Runs[0].Text);
        Assert.Equal(FontStyle.Regular, engine.Document.Blocks[0].Runs[0].Style);

        engine.CursorBlock = 0;
        engine.CursorRun = 0;
        engine.CursorOffset = 0;
        engine.SelectionBlock = 0;
        engine.SelectionRun = 0;
        engine.SelectionOffset = 5;

        Assert.True(engine.HasSelection);

        engine.ToggleBold();
        Assert.Equal(FontStyle.Bold, engine.Document.Blocks[0].Runs[0].Style);

        engine.ToggleBold();
        Assert.Equal(FontStyle.Regular, engine.Document.Blocks[0].Runs[0].Style);
    }

    [Fact]
    public void RichTextEngine_GetFontStyleAtCursor_ReturnsCorrectStyle()
    {
        var engine = new RichTextEngine();
        engine.InitFromHtml("<p>Hello <b>World</b></p>");

        engine.CursorBlock = 0;
        engine.CursorRun = 0;
        engine.CursorOffset = 0;
        Assert.Equal(FontStyle.Regular, engine.GetFontStyleAtCursor());

        engine.CursorBlock = 0;
        engine.CursorRun = 1;
        engine.CursorOffset = 0;
        Assert.Equal(FontStyle.Bold, engine.GetFontStyleAtCursor());
    }

    [Fact]
    public void RichTextEngine_ToggleBold_OnCrossRunSelection_TogglesBothRuns()
    {
        var engine = new RichTextEngine();
        engine.InitFromHtml("<p>Hello <b>World</b>!</p>");
        // runs: "Hello " (Regular), "World" (Bold), "!" (Regular)

        Assert.Equal(3, engine.Document.Blocks[0].Runs.Count);

        // Select "lo World" (cross-run: end of run 0, all of run 1)
        // Flat index: "Hello " = 6 chars, "World" = 5 chars
        // "lo World" starts at flat index 3, ends at flat index 11
        engine.CursorBlock = 0;
        engine.CursorRun = 0;
        engine.CursorOffset = 3;
        engine.SelectionBlock = 0;
        engine.SelectionRun = 1;
        engine.SelectionOffset = 5; // end of "World"

        Assert.True(engine.HasSelection);
        Assert.Equal(3, engine.CursorFlatIndex);
        Assert.Equal(11, engine.SelectionFlatIndex);

        engine.ToggleBold();

        // Run 0 ("Hello "): only "lo " was selected, so the entire run toggles
        Assert.Equal(FontStyle.Bold, engine.Document.Blocks[0].Runs[0].Style);
        // Run 1 ("World"): entirely selected + toggle
        Assert.Equal(FontStyle.Regular, engine.Document.Blocks[0].Runs[1].Style);
        // Run 2 ("!"): not selected
        Assert.Equal(FontStyle.Regular, engine.Document.Blocks[0].Runs[2].Style);
    }
    [Fact]
    public void Control_Bounds_ShouldInitializeCorrectly()
    {
        var control = new Label();
        Assert.Equal(0, control.X);
        Assert.Equal(0, control.Y);
        Assert.Equal(150, control.Width);
        Assert.Equal(28, control.Height);
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
    public void TextBox_OnTextInput_ShouldInsertChar()
    {
        var textBox = new TextBox();
        textBox.Text = "Hllo";
        Assert.Equal(4, textBox.SelectionStart);

        textBox.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Home, Modifiers = ModifierKeys.None });
        Assert.Equal(0, textBox.SelectionStart);

        textBox.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Right, Modifiers = ModifierKeys.None });
        Assert.Equal(1, textBox.SelectionStart);

        textBox.OnTextInput("e");
        Assert.Equal("Hello", textBox.Text);
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

    [Fact]
    public void CheckBox_OnKeyDown_Space_ShouldToggleChecked()
    {
        var checkBox = new CheckBox { Text = "Agree" };
        Assert.False(checkBox.Checked);

        checkBox.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Space, Modifiers = ModifierKeys.None });
        Assert.True(checkBox.Checked);

        checkBox.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Space, Modifiers = ModifierKeys.None });
        Assert.False(checkBox.Checked);
    }

    [Fact]
    public void CheckBox_OnKeyDown_Space_ShouldSetHandled()
    {
        var checkBox = new CheckBox();
        var args = new KeyEventArgs { KeyCode = Keys.Space, Modifiers = ModifierKeys.None };
        checkBox.OnKeyDown(args);
        Assert.True(args.Handled);
    }

    [Fact]
    public void Button_OnKeyDown_Enter_ShouldFireClick()
    {
        var button = new Button { Text = "Click Me" };
        var clicked = false;
        button.Click += (s, e) => clicked = true;

        button.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Enter, Modifiers = ModifierKeys.None });
        Assert.True(clicked);
    }

    [Fact]
    public void Button_OnKeyDown_Space_ShouldFireClick()
    {
        var button = new Button { Text = "Click Me" };
        var clicked = false;
        button.Click += (s, e) => clicked = true;

        button.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Space, Modifiers = ModifierKeys.None });
        Assert.True(clicked);
    }

    [Fact]
    public void Button_OnKeyDown_Enter_ShouldSetHandled()
    {
        var button = new Button();
        var args = new KeyEventArgs { KeyCode = Keys.Enter, Modifiers = ModifierKeys.None };
        button.OnKeyDown(args);
        Assert.True(args.Handled);
    }

    [Fact]
    public void RadioButton_OnKeyDown_Space_ShouldSetChecked()
    {
        var radioButton = new RadioButton { Text = "Option 1" };
        Assert.False(radioButton.Checked);

        radioButton.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Space, Modifiers = ModifierKeys.None });
        Assert.True(radioButton.Checked);
    }

    [Fact]
    public void ListBox_OnKeyDown_Down_ShouldMoveSelection()
    {
        var listBox = new ListBox();
        listBox.Items.Add("A");
        listBox.Items.Add("B");
        listBox.Items.Add("C");
        listBox.SelectedIndex = 0;

        listBox.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Down, Modifiers = ModifierKeys.None });
        Assert.Equal(1, listBox.SelectedIndex);
    }

    [Fact]
    public void ListBox_OnKeyDown_Up_ShouldMoveSelection()
    {
        var listBox = new ListBox();
        listBox.Items.Add("A");
        listBox.Items.Add("B");
        listBox.Items.Add("C");
        listBox.SelectedIndex = 2;

        listBox.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Up, Modifiers = ModifierKeys.None });
        Assert.Equal(1, listBox.SelectedIndex);
    }

    [Fact]
    public void ListBox_OnKeyDown_Home_ShouldMoveToFirst()
    {
        var listBox = new ListBox();
        listBox.Items.Add("A");
        listBox.Items.Add("B");
        listBox.Items.Add("C");
        listBox.SelectedIndex = 2;

        listBox.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Home, Modifiers = ModifierKeys.None });
        Assert.Equal(0, listBox.SelectedIndex);
    }

    [Fact]
    public void ListBox_OnKeyDown_End_ShouldMoveToLast()
    {
        var listBox = new ListBox();
        listBox.Items.Add("A");
        listBox.Items.Add("B");
        listBox.Items.Add("C");
        listBox.SelectedIndex = 0;

        listBox.OnKeyDown(new KeyEventArgs { KeyCode = Keys.End, Modifiers = ModifierKeys.None });
        Assert.Equal(2, listBox.SelectedIndex);
    }

    [Fact]
    public void Control_Focused_ShouldFireGotFocusEvent()
    {
        var control = new Label();
        var gotFocus = false;
        control.GotFocus += (s, e) => gotFocus = true;

        control.Focused = true;
        Assert.True(gotFocus);
    }

    [Fact]
    public void Control_Focused_ShouldFireLostFocusEvent()
    {
        var control = new Label();
        control.Focused = true;
        var lostFocus = false;
        control.LostFocus += (s, e) => lostFocus = true;

        control.Focused = false;
        Assert.True(lostFocus);
    }

    [Fact]
    public void ContainerControl_ActiveControl_ShouldSetFocused()
    {
        var panel = new CoreForms.Ui.Controls.Containers.Panel();
        var label = new Label();
        panel.Controls.Add(label);

        panel.ActiveControl = label;
        Assert.True(label.Focused);

        panel.ActiveControl = null;
        Assert.False(label.Focused);
    }

    [Fact]
    public void ContainerControl_ActiveControl_ChangeShouldUnfocusOld()
    {
        var panel = new CoreForms.Ui.Controls.Containers.Panel();
        var label1 = new Label();
        var label2 = new Label();
        panel.Controls.Add(label1);
        panel.Controls.Add(label2);

        panel.ActiveControl = label1;
        Assert.True(label1.Focused);

        panel.ActiveControl = label2;
        Assert.False(label1.Focused);
        Assert.True(label2.Focused);
    }

    [Fact]
    public void DataGridView_OnKeyDown_Down_ShouldMoveSelection()
    {
        var grid = new CoreForms.Ui.Controls.Advanced.DataGridView();
        grid.Columns.Add(new CoreForms.Ui.Controls.Advanced.DataGridViewColumn { HeaderText = "Col1" });
        grid.AddRow("A1");
        grid.AddRow("A2");
        grid.AddRow("A3");
        grid.SelectedRowIndex = 0;

        grid.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Down, Modifiers = ModifierKeys.None });
        Assert.Equal(1, grid.SelectedRowIndex);
    }

    [Fact]
    public void DataGridView_OnKeyDown_Up_ShouldMoveSelection()
    {
        var grid = new CoreForms.Ui.Controls.Advanced.DataGridView();
        grid.Columns.Add(new CoreForms.Ui.Controls.Advanced.DataGridViewColumn { HeaderText = "Col1" });
        grid.AddRow("A1");
        grid.AddRow("A2");
        grid.AddRow("A3");
        grid.SelectedRowIndex = 2;

        grid.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Up, Modifiers = ModifierKeys.None });
        Assert.Equal(1, grid.SelectedRowIndex);
    }

    [Fact]
    public void TabControl_OnKeyDown_Right_ShouldSwitchTab()
    {
        var tabControl = new CoreForms.Ui.Controls.Advanced.TabControl();
        tabControl.TabPages.Add(new CoreForms.Ui.Controls.Advanced.TabPage { Text = "Tab1" });
        tabControl.TabPages.Add(new CoreForms.Ui.Controls.Advanced.TabPage { Text = "Tab2" });

        Assert.Equal(0, tabControl.SelectedIndex);

        tabControl.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Right, Modifiers = ModifierKeys.None });
        Assert.Equal(1, tabControl.SelectedIndex);
    }

    [Fact]
    public void TabControl_OnKeyDown_Left_ShouldSwitchTab()
    {
        var tabControl = new CoreForms.Ui.Controls.Advanced.TabControl();
        tabControl.TabPages.Add(new CoreForms.Ui.Controls.Advanced.TabPage { Text = "Tab1" });
        tabControl.TabPages.Add(new CoreForms.Ui.Controls.Advanced.TabPage { Text = "Tab2" });
        tabControl.SelectedIndex = 1;

        tabControl.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Left, Modifiers = ModifierKeys.None });
        Assert.Equal(0, tabControl.SelectedIndex);
    }

    [Fact]
    public void ComboBox_OnKeyDown_Down_ShouldMoveSelection()
    {
        var comboBox = new ComboBox();
        comboBox.Items.Add("Item 1");
        comboBox.Items.Add("Item 2");
        comboBox.Items.Add("Item 3");
        comboBox.SelectedIndex = 0;

        comboBox.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Down, Modifiers = ModifierKeys.None });
        Assert.Equal(1, comboBox.SelectedIndex);
    }

    [Fact]
    public void ComboBox_OnKeyDown_Up_ShouldMoveSelection()
    {
        var comboBox = new ComboBox();
        comboBox.Items.Add("Item 1");
        comboBox.Items.Add("Item 2");
        comboBox.Items.Add("Item 3");
        comboBox.SelectedIndex = 2;

        comboBox.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Up, Modifiers = ModifierKeys.None });
        Assert.Equal(1, comboBox.SelectedIndex);
    }

    [Fact]
    public void Form_Tab_ShouldMoveFocusToNextControl()
    {
        var form = new Form();
        var button1 = new Button { TabIndex = 0 };
        var button2 = new Button { TabIndex = 1 };
        form.Controls.Add(button1);
        form.Controls.Add(button2);

        form.ActiveControl = button1;
        form.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Tab, Modifiers = ModifierKeys.None });

        Assert.Equal(button2, form.ActiveControl);
    }

    [Fact]
    public void Form_ShiftTab_ShouldMoveFocusToPreviousControl()
    {
        var form = new Form();
        var button1 = new Button { TabIndex = 0 };
        var button2 = new Button { TabIndex = 1 };
        form.Controls.Add(button1);
        form.Controls.Add(button2);

        form.ActiveControl = button2;
        form.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Tab, Modifiers = ModifierKeys.Shift });

        Assert.Equal(button1, form.ActiveControl);
    }

[Fact]
    public void ContainerControl_MouseDispatch_ShouldReachNestedChild()
    {
        var panel = new CoreForms.Ui.Controls.Containers.Panel();
        panel.Size = new Size(200, 200);
        var button = new Button { Location = new Point(10, 10), Size = new Size(100, 30) };
        panel.Controls.Add(button);

        var clicked = false;
        button.Click += (s, e) => clicked = true;

        var mouseDownArgs = new MouseEventArgs(MouseButtons.Left, 1, 50, 20, 0);
        var mouseUpArgs = new MouseEventArgs(MouseButtons.Left, 1, 50, 20, 0);
        panel.OnMouseDown(mouseDownArgs);
        panel.OnMouseUp(mouseUpArgs);

        Assert.True(clicked);
    }

    

[Fact]
    public void ContainerControl_MouseDispatch_ShouldReachDeeplyNestedChild()
    {
        var outerPanel = new CoreForms.Ui.Controls.Containers.Panel();
        outerPanel.Size = new Size(400, 400);
        var innerPanel = new CoreForms.Ui.Controls.Containers.Panel();
        innerPanel.Location = new Point(50, 50);
        innerPanel.Size = new Size(200, 200);
        var button = new Button { Location = new Point(10, 10), Size = new Size(100, 30) };
        innerPanel.Controls.Add(button);
        outerPanel.Controls.Add(innerPanel);

        Assert.True(outerPanel.Controls.Count == 1);
        Assert.True(innerPanel.Controls.Count == 1);
        Assert.True(innerPanel.Visible);
        Assert.True(button.Visible);
        Assert.True(innerPanel.HitTest(new Point(65, 70)));
        Assert.True(button.HitTest(new Point(15, 20)));

        var clicked = false;
        button.Click += (s, e) => clicked = true;

        var mouseDownArgs = new MouseEventArgs(MouseButtons.Left, 1, 65, 70, 0);
        var mouseUpArgs = new MouseEventArgs(MouseButtons.Left, 1, 65, 70, 0);
        outerPanel.OnMouseDown(mouseDownArgs);
        outerPanel.OnMouseUp(mouseUpArgs);

        Assert.True(clicked, "Button click should fire through nested dispatch");
    }

    [Fact]
    public void ContainerControl_KeyboardDispatch_ShouldReachActiveControl()
    {
        var panel = new CoreForms.Ui.Controls.Containers.Panel();
        panel.Size = new Size(200, 200);
        var textBox = new TextBox { TabStop = true, TabIndex = 0 };
        panel.Controls.Add(textBox);

        panel.ActiveControl = textBox;

        var keyPressed = false;
        textBox.KeyDown += (s, e) => keyPressed = true;

        panel.OnKeyDown(new KeyEventArgs { KeyCode = Keys.A, Modifiers = ModifierKeys.None });
        Assert.True(keyPressed);
    }

    [Fact]
    public void Form_Tab_ShouldWrapAround()
    {
        var form = new Form();
        var button1 = new Button { TabIndex = 0 };
        var button2 = new Button { TabIndex = 1 };
        form.Controls.Add(button1);
        form.Controls.Add(button2);

        form.ActiveControl = button2;
        form.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Tab, Modifiers = ModifierKeys.None });

        Assert.Equal(button1, form.ActiveControl);
    }

    [Fact]
    public void Form_ShiftTab_ShouldWrapAround()
    {
        var form = new Form();
        var button1 = new Button { TabIndex = 0 };
        var button2 = new Button { TabIndex = 1 };
        form.Controls.Add(button1);
        form.Controls.Add(button2);

        form.ActiveControl = button1;
        form.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Tab, Modifiers = ModifierKeys.Shift });

        Assert.Equal(button2, form.ActiveControl);
    }

    [Fact]
    public void Form_Tab_ShouldSkipNonTabStopControls()
    {
        var form = new Form();
        var button = new Button { TabIndex = 0 };
        var label = new Label { TabIndex = 1 };
        var textBox = new TextBox { TabIndex = 2 };
        form.Controls.Add(button);
        form.Controls.Add(label);
        form.Controls.Add(textBox);

        form.ActiveControl = button;
        form.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Tab, Modifiers = ModifierKeys.None });

        Assert.Equal(textBox, form.ActiveControl);
    }

    [Fact]
    public void Form_Tab_ShouldSelectFirstControlWhenNoneFocused()
    {
        var form = new Form();
        var button = new Button { TabIndex = 0 };
        var textBox = new TextBox { TabIndex = 1 };
        form.Controls.Add(button);
        form.Controls.Add(textBox);

        Assert.Null(form.ActiveControl);
        form.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Tab, Modifiers = ModifierKeys.None });

        Assert.Equal(button, form.ActiveControl);
    }

    [Fact]
    public void Form_Tab_ShouldRespectTabIndex()
    {
        var form = new Form();
        var textBox = new TextBox { TabIndex = 2 };
        var button = new Button { TabIndex = 0 };
        var comboBox = new ComboBox { TabIndex = 1 };
        form.Controls.Add(textBox);
        form.Controls.Add(button);
        form.Controls.Add(comboBox);

        form.ActiveControl = button;
        form.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Tab, Modifiers = ModifierKeys.None });

        Assert.Equal(comboBox, form.ActiveControl);

        form.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Tab, Modifiers = ModifierKeys.None });

        Assert.Equal(textBox, form.ActiveControl);
    }

    [Fact]
    public void Form_Tab_ShouldNavigateNestedContainers()
    {
        var form = new Form();
        var panel = new Panel();
        var button1 = new Button { TabIndex = 0 };
        var button2 = new Button { TabIndex = 1 };
        var button3 = new Button { TabIndex = 2 };
        panel.Controls.Add(button1);
        panel.Controls.Add(button2);
        form.Controls.Add(panel);
        form.Controls.Add(button3);

        form.ActiveControl = button1;
        Assert.True(button1.Focused);
        Assert.False(button2.Focused);
        Assert.False(button3.Focused);

        form.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Tab, Modifiers = ModifierKeys.None });
        Assert.True(button2.Focused);
        Assert.False(button1.Focused);

        form.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Tab, Modifiers = ModifierKeys.None });
        Assert.True(button3.Focused);
        Assert.False(button2.Focused);
    }

[Fact]
    public void Form_Tab_ShouldNavigateWithinSingleTabPage()
    {
        var form = new Form();
        var tabControl = new TabControl();
        var tabPage = new TabPage();
        var textBox1 = new TextBox();
        var textBox2 = new TextBox();
        var textBox3 = new TextBox();
        tabPage.Controls.Add(textBox1);
        tabPage.Controls.Add(textBox2);
        tabPage.Controls.Add(textBox3);
        tabControl.AddTabPage(tabPage);
        form.Controls.Add(tabControl);

        form.ActiveControl = textBox1;
        Assert.True(textBox1.Focused);
        Assert.False(textBox2.Focused);
        Assert.False(textBox3.Focused);

        form.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Tab, Modifiers = ModifierKeys.None });
        Assert.True(textBox2.Focused);
        Assert.False(textBox1.Focused);
        Assert.False(textBox3.Focused);

        form.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Tab, Modifiers = ModifierKeys.None });
        Assert.True(textBox3.Focused);
        Assert.False(textBox2.Focused);

        form.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Tab, Modifiers = ModifierKeys.None });
        Assert.True(textBox1.Focused);
        Assert.False(textBox3.Focused);

        form.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Tab, Modifiers = ModifierKeys.Shift });
        Assert.True(textBox3.Focused);
        Assert.False(textBox1.Focused);

        form.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Tab, Modifiers = ModifierKeys.Shift });
        Assert.True(textBox2.Focused);
        Assert.False(textBox3.Focused);
    }

    [Fact]
    public void Form_Tab_ShouldNavigateWithinTabPage_NestedInPanel()
    {
        var form = new Form();
        var panel = new Panel();
        var tabControl = new TabControl();
        var tabPage = new TabPage();
        var textBox1 = new TextBox();
        var textBox2 = new TextBox();
        tabPage.Controls.Add(textBox1);
        tabPage.Controls.Add(textBox2);
        tabControl.AddTabPage(tabPage);
        panel.Controls.Add(tabControl);
        form.Controls.Add(panel);

        form.ActiveControl = textBox1;
        Assert.True(textBox1.Focused);
        Assert.False(textBox2.Focused);

        form.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Tab, Modifiers = ModifierKeys.None });
        Assert.True(textBox2.Focused);
        Assert.False(textBox1.Focused);

        form.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Tab, Modifiers = ModifierKeys.None });
        Assert.True(textBox1.Focused);
        Assert.False(textBox2.Focused);

        form.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Tab, Modifiers = ModifierKeys.Shift });
        Assert.True(textBox2.Focused);
    }

    [Fact]
    public void Form_Focus_ShouldOnlyOneControlFocused()
    {
        var form = new Form();
        var button1 = new Button();
        var button2 = new Button();
        form.Controls.Add(button1);
        form.Controls.Add(button2);

        button1.Focused = true;
        Assert.True(button1.Focused);
        Assert.False(button2.Focused);

        button2.Focused = true;
        Assert.True(button2.Focused);
        Assert.False(button1.Focused);
    }
}