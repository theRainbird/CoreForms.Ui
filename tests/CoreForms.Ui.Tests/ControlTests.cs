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
        Assert.Single(engine.Document.Blocks[0].Content);
        var run = Assert.IsType<TextRun>(engine.Document.Blocks[0].Content[0]);
        Assert.Equal("Hello World", run.Text);
        Assert.Equal(FontStyle.Regular, run.Style);

        engine.CursorBlock = 0;
        engine.CursorRun = 0;
        engine.CursorOffset = 0;
        engine.SelectionBlock = 0;
        engine.SelectionRun = 0;
        engine.SelectionOffset = 5;

        Assert.True(engine.HasSelection);

        engine.ToggleBold();
        Assert.Equal(FontStyle.Bold, ((TextRun)engine.Document.Blocks[0].Content[0]).Style);

        engine.ToggleBold();
        Assert.Equal(FontStyle.Regular, ((TextRun)engine.Document.Blocks[0].Content[0]).Style);
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
        // content: "Hello " (Regular), "World" (Bold), "!" (Regular)

        Assert.Equal(3, engine.Document.Blocks[0].Content.Count);

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

        // Run 0 ("Hel"): not selected, should stay Regular
        Assert.Equal(FontStyle.Regular, ((TextRun)engine.Document.Blocks[0].Content[0]).Style);
        // Run 1 ("lo "): selected portion, should become Bold
        Assert.Equal(FontStyle.Bold, ((TextRun)engine.Document.Blocks[0].Content[1]).Style);
        // Run 2 ("World"): entirely selected, toggles from Bold back to Regular
        Assert.Equal(FontStyle.Regular, ((TextRun)engine.Document.Blocks[0].Content[2]).Style);
        // Run 3 ("!"): not selected
        Assert.Equal(FontStyle.Regular, ((TextRun)engine.Document.Blocks[0].Content[3]).Style);
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
    public void DataGridView_TextBoxCellEdit_BeginEdit_CreatesTextBoxChild()
    {
        var grid = new CoreForms.Ui.Controls.Advanced.DataGridView();
        var col = new CoreForms.Ui.Controls.Advanced.DataGridViewColumn { HeaderText = "Col1", CellEditType = DataGridViewColumnEditType.TextBox };
        grid.Columns.Add(col);
        grid.AddRow("A1");
        grid.SelectedRowIndex = 0;
        grid.SelectedColumnIndex = 0;

        grid.BeginEdit();

        Assert.True(grid.IsCurrentCellInEditMode);
        Assert.Single(grid.Controls);
        Assert.IsType<TextBox>(grid.Controls[0]);
    }

    [Fact]
    public void DataGridView_TextBoxCellEdit_CommitViaEnter_UpdatesCellValue()
    {
        var grid = new CoreForms.Ui.Controls.Advanced.DataGridView();
        var col = new CoreForms.Ui.Controls.Advanced.DataGridViewColumn { HeaderText = "Col1", CellEditType = DataGridViewColumnEditType.TextBox };
        grid.Columns.Add(col);
        grid.AddRow("A1");
        grid.SelectedRowIndex = 0;
        grid.SelectedColumnIndex = 0;
        grid.BeginEdit();

        var tb = Assert.IsType<TextBox>(grid.Controls[0]);
        tb.Text = "NewValue";

        // Simulate Enter key on the editing control
        tb.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Enter, Modifiers = ModifierKeys.None });

        Assert.Equal("NewValue", grid.Rows[0].Cells[0].Value);
        Assert.False(grid.IsCurrentCellInEditMode);
    }

    [Fact]
    public void DataGridView_TextBoxCellEdit_CancelViaEscape_RestoresOriginalValue()
    {
        var grid = new CoreForms.Ui.Controls.Advanced.DataGridView();
        var col = new CoreForms.Ui.Controls.Advanced.DataGridViewColumn { HeaderText = "Col1", CellEditType = DataGridViewColumnEditType.TextBox };
        grid.Columns.Add(col);
        grid.AddRow("Original");
        grid.SelectedRowIndex = 0;
        grid.SelectedColumnIndex = 0;
        grid.BeginEdit();

        var tb = Assert.IsType<TextBox>(grid.Controls[0]);
        tb.Text = "Modified";

        // Simulate Escape key
        tb.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Escape, Modifiers = ModifierKeys.None });

        Assert.Equal("Original", grid.Rows[0].Cells[0].Value);
        Assert.False(grid.IsCurrentCellInEditMode);
    }

    [Fact]
    public void DataGridView_ComboBoxCellEdit_SelectItem_UpdatesCellValue()
    {
        var grid = new CoreForms.Ui.Controls.Advanced.DataGridView();
        var col = new CoreForms.Ui.Controls.Advanced.DataGridViewColumn
        {
            HeaderText = "Col1",
            CellEditType = DataGridViewColumnEditType.ComboBox,
            Items = new List<object> { "Option1", "Option2", "Option3" }
        };
        grid.Columns.Add(col);
        grid.AddRow("Option1");
        grid.SelectedRowIndex = 0;
        grid.SelectedColumnIndex = 0;
        grid.BeginEdit();

        var cb = Assert.IsType<ComboBox>(grid.Controls[0]);
        cb.SelectedIndex = 1;

        // Value is NOT updated until commit; editor stays open
        Assert.Equal("Option1", grid.Rows[0].Cells[0].Value);
        Assert.True(grid.IsCurrentCellInEditMode);

        // Commit via Enter
        cb.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Enter, Modifiers = ModifierKeys.None });
        Assert.False(grid.IsCurrentCellInEditMode);
        Assert.Equal("Option2", grid.Rows[0].Cells[0].Value);
    }

    [Fact]
    public void DataGridView_ComboBoxEscapeThenNavigate_PreservesOriginalValue()
    {
        var grid = new CoreForms.Ui.Controls.Advanced.DataGridView();
        var col = new CoreForms.Ui.Controls.Advanced.DataGridViewColumn
        {
            HeaderText = "Status",
            CellEditType = DataGridViewColumnEditType.ComboBox,
            Items = new List<object> { "Active", "Inactive", "Pending" }
        };
        grid.Columns.Add(col);
        grid.AddRow("Active");
        grid.SelectedRowIndex = 0;
        grid.SelectedColumnIndex = 0;
        grid.BeginEdit();

        var cb = Assert.IsType<ComboBox>(grid.Controls[0]);
        cb.SelectedIndex = 2; // "Pending"

        // Escape cancels the edit
        cb.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Escape, Modifiers = ModifierKeys.None });
        Assert.False(grid.IsCurrentCellInEditMode);
        Assert.Equal("Active", grid.Rows[0].Cells[0].Value);

        // Navigate to another cell – original value must still be intact
        grid.SelectedColumnIndex = 1; // doesn't exist but simulates navigation
        grid.SelectedColumnIndex = 0;
        Assert.Equal("Active", grid.Rows[0].Cells[0].Value);
    }

    [Fact]
    public void DataGridView_CheckBoxCellEdit_TogglesValueDirectly()
    {
        var grid = new CoreForms.Ui.Controls.Advanced.DataGridView();
        var col = new CoreForms.Ui.Controls.Advanced.DataGridViewColumn
        {
            HeaderText = "Col1",
            CellEditType = DataGridViewColumnEditType.CheckBox,
            TrueValue = true,
            FalseValue = false
        };
        grid.Columns.Add(col);
        grid.AddRow(false);
        grid.SelectedRowIndex = 0;
        grid.SelectedColumnIndex = 0;

        // BeginEdit for CheckBox toggles inline (no persistent editor)
        grid.BeginEdit();

        Assert.Equal(true, grid.Rows[0].Cells[0].Value);
        Assert.False(grid.IsCurrentCellInEditMode);

        // Toggle again
        grid.BeginEdit();
        Assert.Equal(false, grid.Rows[0].Cells[0].Value);
    }

    [Fact]
    public void DataGridView_CheckBoxCellEdit_WithCustomTrueFalseValues()
    {
        var grid = new CoreForms.Ui.Controls.Advanced.DataGridView();
        var col = new CoreForms.Ui.Controls.Advanced.DataGridViewColumn
        {
            HeaderText = "Col1",
            CellEditType = DataGridViewColumnEditType.CheckBox,
            TrueValue = "yes",
            FalseValue = "no"
        };
        grid.Columns.Add(col);
        grid.AddRow("no");
        grid.SelectedRowIndex = 0;
        grid.SelectedColumnIndex = 0;

        grid.BeginEdit();
        Assert.Equal("yes", grid.Rows[0].Cells[0].Value);

        grid.BeginEdit();
        Assert.Equal("no", grid.Rows[0].Cells[0].Value);
    }

    [Fact]
    public void DataGridView_ReadOnlyColumn_DoesNotEnterEditMode()
    {
        var grid = new CoreForms.Ui.Controls.Advanced.DataGridView();
        var col = new CoreForms.Ui.Controls.Advanced.DataGridViewColumn
        {
            HeaderText = "Col1",
            CellEditType = DataGridViewColumnEditType.TextBox,
            ReadOnly = true
        };
        grid.Columns.Add(col);
        grid.AddRow("A1");
        grid.SelectedRowIndex = 0;
        grid.SelectedColumnIndex = 0;

        grid.BeginEdit();

        Assert.False(grid.IsCurrentCellInEditMode);
        Assert.Empty(grid.Controls);
    }

    [Fact]
    public void DataGridView_GlobalReadOnly_DoesNotEnterEditMode()
    {
        var grid = new CoreForms.Ui.Controls.Advanced.DataGridView();
        var col = new CoreForms.Ui.Controls.Advanced.DataGridViewColumn
        {
            HeaderText = "Col1",
            CellEditType = DataGridViewColumnEditType.TextBox
        };
        grid.Columns.Add(col);
        grid.AddRow("A1");
        grid.ReadOnly = true;
        grid.SelectedRowIndex = 0;
        grid.SelectedColumnIndex = 0;

        grid.BeginEdit();

        Assert.False(grid.IsCurrentCellInEditMode);
        Assert.Empty(grid.Controls);
    }

    [Fact]
    public void DataGridView_ColumnWithNoEditType_DoesNotEnterEditMode()
    {
        var grid = new CoreForms.Ui.Controls.Advanced.DataGridView();
        var col = new CoreForms.Ui.Controls.Advanced.DataGridViewColumn
        {
            HeaderText = "Col1",
            CellEditType = DataGridViewColumnEditType.None
        };
        grid.Columns.Add(col);
        grid.AddRow("A1");
        grid.SelectedRowIndex = 0;
        grid.SelectedColumnIndex = 0;

        grid.BeginEdit();

        Assert.False(grid.IsCurrentCellInEditMode);
        Assert.Empty(grid.Controls);
    }

    [Fact]
    public void DataGridView_F2Key_StartsEditingOnEditableCell()
    {
        var grid = new CoreForms.Ui.Controls.Advanced.DataGridView();
        var col = new CoreForms.Ui.Controls.Advanced.DataGridViewColumn { HeaderText = "Col1", CellEditType = DataGridViewColumnEditType.TextBox };
        grid.Columns.Add(col);
        grid.AddRow("A1");
        grid.SelectedRowIndex = 0;
        grid.SelectedColumnIndex = 0;

        grid.OnKeyDown(new KeyEventArgs { KeyCode = Keys.F2, Modifiers = ModifierKeys.None });

        Assert.True(grid.IsCurrentCellInEditMode);
    }

    [Fact]
    public void DataGridView_OnTextInput_StartsEditingAndForwardsText()
    {
        var grid = new CoreForms.Ui.Controls.Advanced.DataGridView();
        var col = new CoreForms.Ui.Controls.Advanced.DataGridViewColumn { HeaderText = "Col1", CellEditType = DataGridViewColumnEditType.TextBox };
        grid.Columns.Add(col);
        grid.AddRow("");
        grid.SelectedRowIndex = 0;
        grid.SelectedColumnIndex = 0;

        grid.OnTextInput("H");

        Assert.True(grid.IsCurrentCellInEditMode);
        var tb = Assert.IsType<TextBox>(grid.Controls[0]);
        Assert.Equal("H", tb.Text);
    }

    [Fact]
    public void DataGridView_EscapeKey_CancelsEditing()
    {
        var grid = new CoreForms.Ui.Controls.Advanced.DataGridView();
        var col = new CoreForms.Ui.Controls.Advanced.DataGridViewColumn { HeaderText = "Col1", CellEditType = DataGridViewColumnEditType.TextBox };
        grid.Columns.Add(col);
        grid.AddRow("Original");
        grid.SelectedRowIndex = 0;
        grid.SelectedColumnIndex = 0;
        grid.BeginEdit();

        grid.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Escape, Modifiers = ModifierKeys.None });

        Assert.False(grid.IsCurrentCellInEditMode);
        Assert.Equal("Original", grid.Rows[0].Cells[0].Value);
    }

    [Fact]
    public void DataGridView_MouseClickOnNonEditableCell_DoesNotStartEdit()
    {
        var grid = new CoreForms.Ui.Controls.Advanced.DataGridView();
        var col = new CoreForms.Ui.Controls.Advanced.DataGridViewColumn { HeaderText = "Col1", CellEditType = DataGridViewColumnEditType.None };
        grid.Columns.Add(col);
        grid.AddRow("A1");
        grid.Size = new Size(200, 200);

        // Click on cell (0, 0) - should select but not edit
        grid.OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, 50, 50, 0));

        Assert.Equal(0, grid.SelectedRowIndex);
        Assert.False(grid.IsCurrentCellInEditMode);
    }

    [Fact]
    public void DataGridView_MouseClickOnEditableCell_StartsEditing()
    {
        var grid = new CoreForms.Ui.Controls.Advanced.DataGridView();
        var col = new CoreForms.Ui.Controls.Advanced.DataGridViewColumn { HeaderText = "Col1", CellEditType = DataGridViewColumnEditType.TextBox };
        grid.Columns.Add(col);
        grid.AddRow("A1");
        grid.Size = new Size(200, 200);

        // Click on cell (0, 0) - should select and start editing
        grid.EditMode = DataGridViewEditMode.EditOnEnter;
        grid.OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, 50, 50, 0));

        Assert.Equal(0, grid.SelectedRowIndex);
        Assert.True(grid.IsCurrentCellInEditMode);
    }

    [Fact]
    public void DataGridView_DemoConfig_EditableCellClick_StartsEditing()
    {
        var grid = new CoreForms.Ui.Controls.Advanced.DataGridView();
        grid.ShowGroupingBar = true;
        grid.Size = new Size(600, 300);

        grid.Columns.Add(new CoreForms.Ui.Controls.Advanced.DataGridViewColumn
            { HeaderText = "Id", Width = 60, CellEditType = DataGridViewColumnEditType.TextBox });
        grid.Columns.Add(new CoreForms.Ui.Controls.Advanced.DataGridViewColumn
            { HeaderText = "Name", Width = 150, CellEditType = DataGridViewColumnEditType.TextBox });
        grid.Columns.Add(new CoreForms.Ui.Controls.Advanced.DataGridViewColumn
            { HeaderText = "Email", Width = 200, CellEditType = DataGridViewColumnEditType.TextBox, ReadOnly = true });
        grid.AddRow(1, "John", "john@test.com");
        grid.EditMode = DataGridViewEditMode.EditOnEnter;

        // Click on Name cell (row 0, col 1) - daY = 30+30 = 60, col1 x = 40+60 = 100
        grid.OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, 120, 75, 0));

        Assert.Equal(0, grid.SelectedRowIndex);
        Assert.Equal(1, grid.SelectedColumnIndex);
        Assert.True(grid.IsCurrentCellInEditMode, "Editing should start when clicking an editable cell");
    }

    [Fact]
    public void DataGridView_DemoConfig_ReadOnlyCellClick_DoesNotStartEditing()
    {
        var grid = new CoreForms.Ui.Controls.Advanced.DataGridView();
        grid.ShowGroupingBar = true;
        grid.Size = new Size(600, 300);

        grid.Columns.Add(new CoreForms.Ui.Controls.Advanced.DataGridViewColumn
            { HeaderText = "Id", Width = 60, CellEditType = DataGridViewColumnEditType.TextBox });
        grid.Columns.Add(new CoreForms.Ui.Controls.Advanced.DataGridViewColumn
            { HeaderText = "Name", Width = 150, CellEditType = DataGridViewColumnEditType.TextBox });
        grid.Columns.Add(new CoreForms.Ui.Controls.Advanced.DataGridViewColumn
            { HeaderText = "Email", Width = 200, CellEditType = DataGridViewColumnEditType.TextBox, ReadOnly = true });
        grid.AddRow(1, "John", "john@test.com");

        // Click on Email cell (row 0, col 2) - daY = 60, col2 x = 40+60+150 = 250
        grid.OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, 280, 75, 0));

        Assert.Equal(0, grid.SelectedRowIndex);
        Assert.Equal(2, grid.SelectedColumnIndex);
        Assert.False(grid.IsCurrentCellInEditMode, "Editing should NOT start when clicking a ReadOnly cell");
    }

    [Fact]
    public void DataGridView_TabNavigation_MovesToNextCell()
    {
        var grid = new CoreForms.Ui.Controls.Advanced.DataGridView();
        grid.Columns.Add(new CoreForms.Ui.Controls.Advanced.DataGridViewColumn { HeaderText = "Col1", CellEditType = DataGridViewColumnEditType.TextBox });
        grid.Columns.Add(new CoreForms.Ui.Controls.Advanced.DataGridViewColumn { HeaderText = "Col2", CellEditType = DataGridViewColumnEditType.TextBox });
        grid.AddRow("A1", "B1");
        grid.AddRow("A2", "B2");
        grid.SelectedRowIndex = 0;
        grid.SelectedColumnIndex = 0;

        grid.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Tab, Modifiers = ModifierKeys.None });
        Assert.Equal(1, grid.SelectedColumnIndex);
        Assert.Equal(0, grid.SelectedRowIndex);
    }

    [Fact]
    public void DataGridView_TabNavigationAtLastColumn_MovesToNextRow()
    {
        var grid = new CoreForms.Ui.Controls.Advanced.DataGridView();
        grid.Columns.Add(new CoreForms.Ui.Controls.Advanced.DataGridViewColumn { HeaderText = "Col1" });
        grid.Columns.Add(new CoreForms.Ui.Controls.Advanced.DataGridViewColumn { HeaderText = "Col2" });
        grid.AddRow("A1", "B1");
        grid.AddRow("A2", "B2");
        grid.SelectedRowIndex = 0;
        grid.SelectedColumnIndex = 1;

        grid.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Tab, Modifiers = ModifierKeys.None });
        Assert.Equal(1, grid.SelectedRowIndex);
        Assert.Equal(0, grid.SelectedColumnIndex);
    }

    [Fact]
    public void DataGridView_ShiftTab_MovesToPreviousCell()
    {
        var grid = new CoreForms.Ui.Controls.Advanced.DataGridView();
        grid.Columns.Add(new CoreForms.Ui.Controls.Advanced.DataGridViewColumn { HeaderText = "Col1" });
        grid.Columns.Add(new CoreForms.Ui.Controls.Advanced.DataGridViewColumn { HeaderText = "Col2" });
        grid.AddRow("A1", "B1");
        grid.SelectedRowIndex = 0;
        grid.SelectedColumnIndex = 1;

        grid.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Tab, Modifiers = ModifierKeys.Shift });
        Assert.Equal(0, grid.SelectedColumnIndex);
        Assert.Equal(0, grid.SelectedRowIndex);
    }

    [Fact]
    public void DataGridView_ArrowKeyNavigation_SelectsFirstRowFromUnselected()
    {
        var grid = new CoreForms.Ui.Controls.Advanced.DataGridView();
        grid.Columns.Add(new CoreForms.Ui.Controls.Advanced.DataGridViewColumn { HeaderText = "Col1" });
        grid.AddRow("A1");
        grid.AddRow("A2");

        // No selection initially
        Assert.Equal(-1, grid.SelectedRowIndex);

        grid.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Down, Modifiers = ModifierKeys.None });
        Assert.Equal(0, grid.SelectedRowIndex);
    }

    [Fact]
    public void DataGridView_ConvertValueToType_StringToDecimal_Succeeds()
    {
        var grid = new CoreForms.Ui.Controls.Advanced.DataGridView();
        // This tests the value conversion path via EndEdit
        grid.Columns.Add(new CoreForms.Ui.Controls.Advanced.DataGridViewColumn
            { HeaderText = "Salary", CellEditType = DataGridViewColumnEditType.TextBox });
        grid.AddRow("0");
        grid.SelectedRowIndex = 0;
        grid.SelectedColumnIndex = 0;
        grid.BeginEdit();

        // Simulate typing a decimal value
        var tb = Assert.IsType<TextBox>(grid.Controls[0]);
        tb.Text = "75000.50";
        tb.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Enter, Modifiers = ModifierKeys.None });

        Assert.Equal("75000.50", grid.Rows[0].Cells[0].Value);
        Assert.False(grid.IsCurrentCellInEditMode);
    }

    [Fact]
    public void DataGridView_DateTimePickerCellEdit_CreatesEditor()
    {
        var grid = new CoreForms.Ui.Controls.Advanced.DataGridView();
        var col = new CoreForms.Ui.Controls.Advanced.DataGridViewColumn
        {
            HeaderText = "Col1",
            CellEditType = DataGridViewColumnEditType.DateTimePicker
        };
        grid.Columns.Add(col);
        grid.AddRow(new DateTime(2025, 6, 15));
        grid.SelectedRowIndex = 0;
        grid.SelectedColumnIndex = 0;

        grid.BeginEdit();

        Assert.True(grid.IsCurrentCellInEditMode);
        Assert.Single(grid.Controls);
        var dtp = Assert.IsType<DateTimePicker>(grid.Controls[0]);
        Assert.Equal(new DateTime(2025, 6, 15), dtp.Value);
    }

    [Fact]
    public void DataGridView_CellValueChangedEvent_FiresOnEditCommit()
    {
        var grid = new CoreForms.Ui.Controls.Advanced.DataGridView();
        var col = new CoreForms.Ui.Controls.Advanced.DataGridViewColumn { HeaderText = "Col1", CellEditType = DataGridViewColumnEditType.TextBox };
        grid.Columns.Add(col);
        grid.AddRow("A1");
        grid.SelectedRowIndex = 0;
        grid.SelectedColumnIndex = 0;

        DataGridViewCellEventArgs? firedArgs = null;
        grid.CellValueChanged += (s, e) => firedArgs = e;

        grid.BeginEdit();
        var tb = Assert.IsType<TextBox>(grid.Controls[0]);
        tb.Text = "NewValue";
        tb.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Enter, Modifiers = ModifierKeys.None });

        Assert.NotNull(firedArgs);
        Assert.Equal(0, firedArgs.ColumnIndex);
        Assert.Equal(0, firedArgs.RowIndex);
    }

    [Fact]
    public void DataGridView_EndEdit_RemovesEditingControl()
    {
        var grid = new CoreForms.Ui.Controls.Advanced.DataGridView();
        var col = new CoreForms.Ui.Controls.Advanced.DataGridViewColumn { HeaderText = "Col1", CellEditType = DataGridViewColumnEditType.TextBox };
        grid.Columns.Add(col);
        grid.AddRow("A1");
        grid.SelectedRowIndex = 0;
        grid.SelectedColumnIndex = 0;
        grid.BeginEdit();

        Assert.True(grid.IsCurrentCellInEditMode);

        grid.EndEdit(true);

        Assert.False(grid.IsCurrentCellInEditMode);
        Assert.Empty(grid.Controls);
    }

    [Fact]
    public void DataGridView_LostFocus_CommitsEditing()
    {
        var grid = new CoreForms.Ui.Controls.Advanced.DataGridView();
        var col = new CoreForms.Ui.Controls.Advanced.DataGridViewColumn { HeaderText = "Col1", CellEditType = DataGridViewColumnEditType.TextBox };
        grid.Columns.Add(col);
        grid.AddRow("A1");
        grid.SelectedRowIndex = 0;
        grid.SelectedColumnIndex = 0;
        grid.BeginEdit();

        var tb = Assert.IsType<TextBox>(grid.Controls[0]);
        tb.Text = "Committed";

        // Simulate LostFocus on the DataGridView
        grid.OnLostFocus(EventArgs.Empty);

        Assert.Equal("Committed", grid.Rows[0].Cells[0].Value);
        Assert.False(grid.IsCurrentCellInEditMode);
    }

    [Fact]
    public void DataGridView_TabDuringEdit_CommitsAndNavigatesToNextColumn()
    {
        var grid = new CoreForms.Ui.Controls.Advanced.DataGridView();
        var col1 = new CoreForms.Ui.Controls.Advanced.DataGridViewColumn { HeaderText = "Col1", CellEditType = DataGridViewColumnEditType.TextBox };
        var col2 = new CoreForms.Ui.Controls.Advanced.DataGridViewColumn { HeaderText = "Col2", CellEditType = DataGridViewColumnEditType.TextBox };
        grid.Columns.Add(col1);
        grid.Columns.Add(col2);
        grid.AddRow("A1", "B1");
        grid.SelectedRowIndex = 0;
        grid.SelectedColumnIndex = 0;
        grid.BeginEdit();

        var tb = Assert.IsType<TextBox>(grid.Controls[0]);
        tb.Text = "Modified";

        grid.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Tab, Modifiers = ModifierKeys.None });

        Assert.Equal("Modified", grid.Rows[0].Cells[0].Value);
        Assert.Equal(1, grid.SelectedColumnIndex);
        Assert.False(grid.IsCurrentCellInEditMode);
    }

    [Fact]
    public void DataGridView_OnKeyDown_ArrowKeys_NavigateThroughReadOnlyCells()
    {
        var grid = new CoreForms.Ui.Controls.Advanced.DataGridView();
        var col1 = new CoreForms.Ui.Controls.Advanced.DataGridViewColumn { HeaderText = "Col1", CellEditType = DataGridViewColumnEditType.TextBox, ReadOnly = true };
        var col2 = new CoreForms.Ui.Controls.Advanced.DataGridViewColumn { HeaderText = "Col2", CellEditType = DataGridViewColumnEditType.TextBox };
        grid.Columns.Add(col1);
        grid.Columns.Add(col2);
        grid.AddRow("ReadOnly", "Editable");
        grid.SelectedRowIndex = 0;
        grid.SelectedColumnIndex = 0;

        // Right arrow -> moves to col1 (should not skip readonly)
        grid.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Right, Modifiers = ModifierKeys.None });
        Assert.Equal(1, grid.SelectedColumnIndex);

        // Left arrow -> moves back to col0 (readonly)
        grid.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Left, Modifiers = ModifierKeys.None });
        Assert.Equal(0, grid.SelectedColumnIndex);
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
    public void ComboBox_DropDownStyle_Default_IsDropDownList()
    {
        var comboBox = new ComboBox();
        Assert.Equal(DropDownStyle.DropDownList, comboBox.DropDownStyle);
    }

    [Fact]
    public void ComboBox_DropDownStyle_CanSetAndGet()
    {
        var comboBox = new ComboBox();
        comboBox.DropDownStyle = DropDownStyle.DropDown;
        Assert.Equal(DropDownStyle.DropDown, comboBox.DropDownStyle);

        comboBox.DropDownStyle = DropDownStyle.Simple;
        Assert.Equal(DropDownStyle.Simple, comboBox.DropDownStyle);

        comboBox.DropDownStyle = DropDownStyle.DropDownList;
        Assert.Equal(DropDownStyle.DropDownList, comboBox.DropDownStyle);
    }

    [Fact]
    public void ComboBox_DropDownStyle_DropDownList_TextIsReadOnly()
    {
        var comboBox = new ComboBox();
        comboBox.Items.Add("Item 1");
        comboBox.Items.Add("Item 2");
        comboBox.SelectedIndex = 0;

        comboBox.DropDownStyle = DropDownStyle.DropDownList;
        Assert.Equal("Item 1", comboBox.Text);

        comboBox.Text = "Custom Text";
        Assert.NotEqual("Custom Text", comboBox.Text);
        Assert.Equal("Item 1", comboBox.Text);
    }

    [Fact]
    public void ComboBox_DropDownStyle_DropDown_AllowsTextEditing()
    {
        var comboBox = new ComboBox();
        comboBox.Items.Add("Item 1");
        comboBox.Items.Add("Item 2");

        comboBox.DropDownStyle = DropDownStyle.DropDown;
        comboBox.Text = "Custom Text";
        Assert.Equal("Custom Text", comboBox.Text);
    }

    [Fact]
    public void ComboBox_DropDownStyle_Simple_AllowsTextEditing()
    {
        var comboBox = new ComboBox();
        comboBox.Items.Add("Item 1");
        comboBox.Items.Add("Item 2");

        comboBox.DropDownStyle = DropDownStyle.Simple;
        comboBox.Text = "Custom Text";
        Assert.Equal("Custom Text", comboBox.Text);
    }

    [Fact]
    public void ComboBox_DropDownStyle_DropDown_TextChanged_FiresOnTextInput()
    {
        var comboBox = new ComboBox();
        comboBox.DropDownStyle = DropDownStyle.DropDown;
        var fired = false;
        comboBox.TextChanged += (s, e) => fired = true;

        comboBox.OnTextInput("H");
        Assert.True(fired);
    }

    [Fact]
    public void ComboBox_DropDownStyle_DropDownList_TextInputDoesNothing()
    {
        var comboBox = new ComboBox();
        comboBox.Items.Add("Item 1");
        comboBox.DropDownStyle = DropDownStyle.DropDownList;

        comboBox.OnTextInput("H");
        Assert.Equal(string.Empty, comboBox.Text);
    }

    [Fact]
    public void ComboBox_DropDownStyle_DropDown_SelectedIndexSyncsText()
    {
        var comboBox = new ComboBox();
        comboBox.Items.Add("Choice A");
        comboBox.Items.Add("Choice B");
        comboBox.DropDownStyle = DropDownStyle.DropDown;

        comboBox.SelectedIndex = 1;
        Assert.Equal("Choice B", comboBox.Text);
    }

    [Fact]
    public void ComboBox_DropDownStyle_Simple_SelectedIndexSyncsText()
    {
        var comboBox = new ComboBox();
        comboBox.Items.Add("Choice A");
        comboBox.Items.Add("Choice B");
        comboBox.DropDownStyle = DropDownStyle.Simple;

        comboBox.SelectedIndex = 1;
        Assert.Equal("Choice B", comboBox.Text);
    }

    [Fact]
    public void ComboBox_DropDownStyle_DropDown_KeyboardEditingWorks()
    {
        var comboBox = new ComboBox();
        comboBox.DropDownStyle = DropDownStyle.DropDown;
        comboBox.Focused = true;

        comboBox.OnTextInput("Hello");
        Assert.Equal("Hello", comboBox.Text);

        // Backspace should remove last character
        comboBox.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Back, Modifiers = ModifierKeys.None });
        Assert.Equal("Hell", comboBox.Text);
    }

    [Fact]
    public void ComboBox_DropDownStyle_DropDownList_OnKeyDown_UpDown_ChangesSelection()
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
    public void ComboBox_DropDown_AutoComplete_AppendsMatchingItem()
    {
        var comboBox = new ComboBox();
        comboBox.DropDownStyle = DropDownStyle.DropDown;
        comboBox.Items.Add("Active");
        comboBox.Items.Add("Inactive");
        comboBox.Items.Add("Pending");

        comboBox.OnTextInput("Ac");
        Assert.Equal("Active", comboBox.Text);
        Assert.Equal(0, comboBox.SelectedIndex);
    }

    [Fact]
    public void ComboBox_DropDown_AutoComplete_CaseInsensitive()
    {
        var comboBox = new ComboBox();
        comboBox.DropDownStyle = DropDownStyle.DropDown;
        comboBox.Items.Add("Active");
        comboBox.Items.Add("Inactive");
        comboBox.Items.Add("Pending");

        comboBox.OnTextInput("ac");
        Assert.Equal("Active", comboBox.Text);
        Assert.Equal(0, comboBox.SelectedIndex);
    }

    [Fact]
    public void ComboBox_DropDown_AutoComplete_NoMatch_NoChange()
    {
        var comboBox = new ComboBox();
        comboBox.DropDownStyle = DropDownStyle.DropDown;
        comboBox.Items.Add("Active");
        comboBox.Items.Add("Inactive");
        comboBox.Items.Add("Pending");

        comboBox.OnTextInput("Xyz");
        Assert.Equal("Xyz", comboBox.Text);
    }

    [Fact]
    public void ComboBox_DropDown_AutoComplete_SelectsDifferentIndexOnNewMatch()
    {
        var comboBox = new ComboBox();
        comboBox.DropDownStyle = DropDownStyle.DropDown;
        comboBox.Items.Add("Alpha");
        comboBox.Items.Add("Active");
        comboBox.Items.Add("Ace");

        comboBox.OnTextInput("Ac");
        Assert.Equal("Active", comboBox.Text);
        Assert.Equal(1, comboBox.SelectedIndex);
    }

    [Fact]
    public void ComboBox_DropDown_AutoComplete_Backspace_RevertsSelection()
    {
        var comboBox = new ComboBox();
        comboBox.DropDownStyle = DropDownStyle.DropDown;
        comboBox.Items.Add("Active");
        comboBox.Items.Add("Inactive");
        comboBox.Items.Add("Pending");

        comboBox.OnTextInput("Ac");        // Auto-completes to "Active", "tive" selected
        Assert.Equal("Active", comboBox.Text);

        comboBox.OnKeyDown(new KeyEventArgs { KeyCode = Keys.Back, Modifiers = ModifierKeys.None });
        Assert.Equal("Ac", comboBox.Text); // Backspace should delete the auto-completed selection
    }

    [Fact]
    public void ComboBox_DropDown_AutoComplete_ExactMatch_NoChange()
    {
        var comboBox = new ComboBox();
        comboBox.DropDownStyle = DropDownStyle.DropDown;
        comboBox.Items.Add("Active");
        comboBox.Items.Add("Inactive");

        comboBox.OnTextInput("Active");
        Assert.Equal("Active", comboBox.Text); // Should stay exactly as typed
    }

    [Fact]
    public void ComboBox_DropDownList_TextSet_WithMatchingItem_SelectsIt()
    {
        var comboBox = new ComboBox();
        comboBox.DropDownStyle = DropDownStyle.DropDownList;
        comboBox.Items.Add("Active");
        comboBox.Items.Add("Inactive");
        comboBox.Items.Add("Pending");

        comboBox.Text = "Inactive";
        Assert.Equal(1, comboBox.SelectedIndex);
        Assert.Equal("Inactive", comboBox.Text);
    }

    [Fact]
    public void ComboBox_DropDownList_TextSet_WithNonMatchingItem_Ignored()
    {
        var comboBox = new ComboBox();
        comboBox.DropDownStyle = DropDownStyle.DropDownList;
        comboBox.Items.Add("Active");
        comboBox.Items.Add("Inactive");
        comboBox.Items.Add("Pending");
        comboBox.SelectedIndex = 0;

        comboBox.Text = "NonExistent";
        Assert.Equal(0, comboBox.SelectedIndex); // Selection should not change
        Assert.Equal("Active", comboBox.Text);   // Text should still show current selection
    }

    [Fact]
    public void ComboBox_Simple_AutoComplete_AppendsMatchingItem()
    {
        var comboBox = new ComboBox();
        comboBox.DropDownStyle = DropDownStyle.Simple;
        comboBox.Items.Add("Active");
        comboBox.Items.Add("Inactive");
        comboBox.Items.Add("Pending");

        comboBox.OnTextInput("Pen");
        Assert.Equal("Pending", comboBox.Text);
    }

    [Fact]
    public void ComboBox_DropDownList_TextSet_CaseInsensitiveMatch()
    {
        var comboBox = new ComboBox();
        comboBox.DropDownStyle = DropDownStyle.DropDownList;
        comboBox.Items.Add("Active");
        comboBox.Items.Add("Inactive");

        comboBox.Text = "active";
        Assert.Equal(0, comboBox.SelectedIndex);
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

    private class TestPerson
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public int Salary { get; set; }
    }

    [Fact]
    public void ComboBox_DataSource_WithDisplayMember_ShowsPropertyValue()
    {
        var comboBox = new ComboBox();
        comboBox.DataSource = new List<TestPerson> { new TestPerson { Name = "Alice", Email = "a@a.com", Salary = 50000 } };
        comboBox.DisplayMember = nameof(TestPerson.Name);
        Assert.Equal("Alice", comboBox.Text);
    }

    [Fact]
    public void ComboBox_DataSource_WithDisplayMember_AndColumns_DisplaysCorrectly()
    {
        var comboBox = new ComboBox();
        comboBox.DataSource = new List<TestPerson>
        {
            new TestPerson { Name = "Alice", Email = "a@a.com", Salary = 50000 },
        };
        comboBox.DisplayMember = nameof(TestPerson.Name);
        comboBox.ColumnHeadersVisible = true;
        comboBox.Columns.Add(new ComboBoxColumn { HeaderText = "Name", Width = 100, DataPropertyName = nameof(TestPerson.Name) });
        Assert.Equal("Alice", comboBox.Text);
    }

    [Fact]
    public void TabControl_MouseClick_ShouldReachChildNearBottomOfContentArea()
    {
        const int tabHeaderHeight = 26;
        var tabControl = new TabControl();
        tabControl.Size = new Size(400, tabHeaderHeight + 200);
        var tabPage = new TabPage();
        var button = new Button { Location = new Point(10, 175), Size = new Size(100, 25) };
        tabPage.Controls.Add(button);
        tabControl.AddTabPage(tabPage);
        tabControl.PerformLayout();

        Assert.Equal(tabHeaderHeight + 200, tabControl.Height);
        Assert.Equal(200, tabPage.Height);

        var clicked = false;
        button.Click += (s, e) => clicked = true;

        var mouseDownArgs = new MouseEventArgs(MouseButtons.Left, 1, 60, tabHeaderHeight + 190, 0);
        var mouseUpArgs = new MouseEventArgs(MouseButtons.Left, 1, 60, tabHeaderHeight + 190, 0);
        tabControl.OnMouseDown(mouseDownArgs);
        tabControl.OnMouseUp(mouseUpArgs);

        Assert.True(clicked, "Click at bottom of content area should reach button");
    }

    [Fact]
    public void TabControl_MouseClick_ShouldReachChildAtExactBottomOfContentArea()
    {
        const int tabHeaderHeight = 26;
        var tabControl = new TabControl();
        tabControl.Size = new Size(400, tabHeaderHeight + 200);
        var tabPage = new TabPage();
        var button = new Button { Location = new Point(10, 175), Size = new Size(100, 25) };
        tabPage.Controls.Add(button);
        tabControl.AddTabPage(tabPage);
        tabControl.PerformLayout();

        var clicked = false;
        button.Click += (s, e) => clicked = true;

        var mouseDownArgs = new MouseEventArgs(MouseButtons.Left, 1, 60, tabHeaderHeight + 199, 0);
        var mouseUpArgs = new MouseEventArgs(MouseButtons.Left, 1, 60, tabHeaderHeight + 199, 0);
        tabControl.OnMouseDown(mouseDownArgs);
        tabControl.OnMouseUp(mouseUpArgs);

        Assert.True(clicked, "Click at exact bottom edge of content area should reach button");
    }

    [Fact]
    public void DataGridView_UngroupedInsideTabControl_SelectsCorrectRow()
    {
        var tabControl = new CoreForms.Ui.Controls.Advanced.TabControl();
        tabControl.Size = new Size(450, 450);
        var tabPage = new CoreForms.Ui.Controls.Advanced.TabPage();
        var grid = new CoreForms.Ui.Controls.Advanced.DataGridView();
        grid.Dock = DockStyle.Fill;
        grid.ColumnHeadersVisible = true;
        grid.AllowUserToAddRows = false;
        grid.Columns.Add(new CoreForms.Ui.Controls.Advanced.DataGridViewColumn { HeaderText = "Name", Width = 150 });
        grid.Columns.Add(new CoreForms.Ui.Controls.Advanced.DataGridViewColumn { HeaderText = "Value", Width = 120 });
        for (int i = 0; i < 12; i++)
            grid.AddRow($"Item {i}", i);
        tabPage.Controls.Add(grid);
        tabControl.AddTabPage(tabPage);
        tabControl.PerformLayout();
        tabPage.PerformLayout();

        int daY = grid.ColumnHeadersVisible ? 30 : 0;
        // Row 0 renders at Y = daY + 0*30 = daY
        grid.OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, 50, daY + 15, 0));
        grid.OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, 50, daY + 15, 0));
        Assert.Equal(0, grid.SelectedRowIndex);

        // Row 5 renders at Y = daY + 5*30 = daY + 150
        grid.OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, 50, daY + 150 + 15, 0));
        grid.OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, 50, daY + 150 + 15, 0));
        Assert.Equal(5, grid.SelectedRowIndex);

        // Row 11 (last, 0-indexed) renders at Y = daY + 11*30 = daY + 330
        grid.OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, 50, daY + 330 + 15, 0));
        grid.OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, 50, daY + 330 + 15, 0));
        Assert.Equal(11, grid.SelectedRowIndex);
    }

    [Fact]
    public void DataGridView_UngroupedInsideTabControl_ClickThroughTabControl_SelectsCorrectRow()
    {
        var tabControl = new CoreForms.Ui.Controls.Advanced.TabControl();
        tabControl.Size = new Size(450, 450);
        var tabPage = new CoreForms.Ui.Controls.Advanced.TabPage();
        var grid = new CoreForms.Ui.Controls.Advanced.DataGridView();
        grid.Dock = DockStyle.Fill;
        grid.ColumnHeadersVisible = true;
        grid.AllowUserToAddRows = false;
        grid.Columns.Add(new CoreForms.Ui.Controls.Advanced.DataGridViewColumn { HeaderText = "Name", Width = 150 });
        grid.Columns.Add(new CoreForms.Ui.Controls.Advanced.DataGridViewColumn { HeaderText = "Value", Width = 120 });
        for (int i = 0; i < 12; i++)
            grid.AddRow($"Item {i}", i);
        tabPage.Controls.Add(grid);
        tabControl.AddTabPage(tabPage);
        tabControl.PerformLayout();

        int tabHeaderHeight = 26;
        int daY = grid.ColumnHeadersVisible ? 30 : 0;

        // Click through TabControl at Y = daY + header + 15 (middle of row 0)
        tabControl.OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, 50, daY + tabHeaderHeight + 15, 0));
        tabControl.OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, 50, daY + tabHeaderHeight + 15, 0));
        Assert.Equal(0, grid.SelectedRowIndex);

        // Row 5: Y = daY + header + 5*30 + 15
        tabControl.OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, 50, daY + tabHeaderHeight + 150 + 15, 0));
        tabControl.OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, 50, daY + tabHeaderHeight + 150 + 15, 0));
        Assert.Equal(5, grid.SelectedRowIndex);

        // Row 11 (last): Y = daY + header + 11*30 + 15
        tabControl.OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, 50, daY + tabHeaderHeight + 330 + 15, 0));
        tabControl.OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, 50, daY + tabHeaderHeight + 330 + 15, 0));
        Assert.Equal(11, grid.SelectedRowIndex);
    }

    [Fact]
    public void DataGridView_InsideDialogLayout_SelectsCorrectRow()
    {
        var container = new CoreForms.Ui.Controls.Containers.Panel();
        container.Size = new Size(450, 500);

        var tabControl = new CoreForms.Ui.Controls.Advanced.TabControl();
        tabControl.Dock = DockStyle.Fill;

        var tabPage = new CoreForms.Ui.Controls.Advanced.TabPage();
        var grid = new CoreForms.Ui.Controls.Advanced.DataGridView();
        grid.Dock = DockStyle.Fill;
        grid.ColumnHeadersVisible = true;
        grid.AllowUserToAddRows = false;
        grid.Columns.Add(new CoreForms.Ui.Controls.Advanced.DataGridViewColumn { HeaderText = "Name", Width = 150 });
        grid.Columns.Add(new CoreForms.Ui.Controls.Advanced.DataGridViewColumn { HeaderText = "Value", Width = 120 });
        for (int i = 0; i < 12; i++)
            grid.AddRow($"Item {i}", i);
        tabPage.Controls.Add(grid);
        tabControl.AddTabPage(tabPage);
        tabControl.AddTabPage(new CoreForms.Ui.Controls.Advanced.TabPage { Text = "Tab 2" });

        container.Controls.Add(tabControl);

        var buttonPanel = new CoreForms.Ui.Controls.Containers.Panel { Dock = DockStyle.Bottom, Height = 50 };
        container.Controls.Add(buttonPanel);

        container.PerformLayout();

        Assert.Equal(0, tabControl.X);
        Assert.Equal(0, tabControl.Y);
        Assert.Equal(450, tabControl.Width);
        Assert.Equal(450, tabControl.Height);

        int tabHeaderHeight = 26;
        int daY = grid.ColumnHeadersVisible ? 30 : 0;

        // Row 0: Y = daY + 15
        grid.OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, 50, daY + 15, 0));
        Assert.Equal(0, grid.SelectedRowIndex);

        // Row 5: Y = daY + 150 + 15
        grid.OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, 50, daY + 150 + 15, 0));
        Assert.Equal(5, grid.SelectedRowIndex);

        // Row 11: Y = daY + 330 + 15
        grid.OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, 50, daY + 330 + 15, 0));
        Assert.Equal(11, grid.SelectedRowIndex);

        // Now dispatch through the container (panel) as Form would:
        grid.SelectedRowIndex = -1;

        // Row 0 at container Y = daY + header + 15
        container.OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, 50, daY + tabHeaderHeight + 15, 0));
        container.OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, 50, daY + tabHeaderHeight + 15, 0));
        Assert.Equal(0, grid.SelectedRowIndex);

        // Row 5 at container Y = daY + header + 5*30 + 15
        grid.SelectedRowIndex = -1;
        container.OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, 50, daY + tabHeaderHeight + 150 + 15, 0));
        container.OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, 50, daY + tabHeaderHeight + 150 + 15, 0));
        Assert.Equal(5, grid.SelectedRowIndex);

        // Row 11 at container Y = daY + header + 11*30 + 15
        grid.SelectedRowIndex = -1;
        container.OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, 50, daY + tabHeaderHeight + 330 + 15, 0));
        container.OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, 50, daY + tabHeaderHeight + 330 + 15, 0));
        Assert.Equal(11, grid.SelectedRowIndex);
    }

    [Fact]
    public void DataGridView_GroupedMode_MultiGroupHitTest_SelectsCorrectRow()
    {
        var grid = new CoreForms.Ui.Controls.Advanced.DataGridView();
        grid.ShowGroupingBar = false;
        grid.ColumnHeadersVisible = false;
        grid.Size = new Size(400, 400);
        grid.Columns.Add(new CoreForms.Ui.Controls.Advanced.DataGridViewColumn { HeaderText = "Category", Groupable = true });
        grid.Columns.Add(new CoreForms.Ui.Controls.Advanced.DataGridViewColumn { HeaderText = "Type", Groupable = true });
        grid.AddRow("A", "X");
        grid.AddRow("A", "Y");
        grid.AddRow("B", "X");
        grid.AddRow("B", "Y");
        grid.AddGroupColumn(0);
        grid.AddGroupColumn(1);

        // With 2 grouping levels (Category then Type) and no grouping bar/column headers:
        // daY = 0
        // Content area starts at Y=0
        // Cat A header: Y=0..29, Type X header: Y=30..59, Row 0 (A,X): Y=60..89
        // Type Y header: Y=90..119, Row 1 (A,Y): Y=120..149
        // Cat B header: Y=150..179, Type X header: Y=180..209, Row 2 (B,X): Y=210..239
        // Type Y header: Y=240..269, Row 3 (B,Y): Y=270..299

        // Click on Row 0 (A,X) at Y=75
        grid.OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, 50, 75, 0));
        grid.OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, 50, 75, 0));
        Assert.Equal(0, grid.SelectedRowIndex);

        // Click on Row 1 (A,Y) at Y=135
        grid.OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, 50, 135, 0));
        grid.OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, 50, 135, 0));
        Assert.Equal(1, grid.SelectedRowIndex);

        // Click on Row 2 (B,X) at Y=225
        grid.OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, 50, 225, 0));
        grid.OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, 50, 225, 0));
        Assert.Equal(2, grid.SelectedRowIndex);

        // Click on Row 3 (B,Y) at Y=285
        grid.OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, 50, 285, 0));
        grid.OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, 50, 285, 0));
        Assert.Equal(3, grid.SelectedRowIndex);
    }
}