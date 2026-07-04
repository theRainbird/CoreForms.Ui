using OldSchoolForms.Ui.Controls.Advanced;
using OldSchoolForms.Ui.Controls.Basic;
using OldSchoolForms.Ui.Controls.Containers;
using OldSchoolForms.Ui.Demo.Models;
using OldSchoolForms.Ui.Controls;
using Graphics = OldSchoolForms.Ui.Rendering.Graphics;
using OldSchoolForms.Ui.Core;

namespace OldSchoolForms.Ui.Demo.UserControls;

/// <summary>
/// Demonstrates basic controls: text input, selection, lists, combos, progress, buttons, date/time pickers.
/// </summary>
public class BasicControlsPage : UserControl
{
    /// <summary>
    /// Occurs when the status text should be updated.
    /// </summary>
    public event EventHandler<StatusTextChangedEventArgs>? StatusTextChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="BasicControlsPage"/> class.
    /// </summary>
    public BasicControlsPage()
    {
        var groupBox1 = new GroupBox
        {
            Text = SR.GetString("GroupTextInput"),
            Location = new Point(10, 10),
            Size = new Size(300, 210)
        };

        var nameLabel = new Label { Text = SR.GetString("LabelName"), Location = new Point(10, 25), Size = new Size(70, 20) };
        var nameTextBox = new TextBox { Location = new Point(90, 25), Size = new Size(180, 25), Text = SR.GetString("DefaultName") };
        var emailLabel = new Label { Text = SR.GetString("LabelEmail"), Location = new Point(10, 55), Size = new Size(70, 20) };
        var emailTextBox = new TextBox { Location = new Point(90, 55), Size = new Size(180, 25), Text = SR.GetString("DefaultEmail") };
        var passwordLabel = new Label { Text = SR.GetString("LabelPassword"), Location = new Point(10, 85), Size = new Size(70, 20) };
        var passwordTextBox = new TextBox { Location = new Point(90, 85), Size = new Size(180, 25), Text = SR.GetString("DefaultPassword"), UseSystemPasswordChar = true };
        var multiLineLabel = new Label { Text = SR.GetString("LabelMultiLine"), Location = new Point(10, 115), Size = new Size(70, 20) };
        var multiLineTextBox = new MemoBox { Location = new Point(90, 115), Size = new Size(180, 80), Text = string.Format(SR.GetString("DefaultMultiLine"), "\n") };

        groupBox1.Controls.Add(nameLabel);
        groupBox1.Controls.Add(nameTextBox);
        groupBox1.Controls.Add(emailLabel);
        groupBox1.Controls.Add(emailTextBox);
        groupBox1.Controls.Add(passwordLabel);
        groupBox1.Controls.Add(passwordTextBox);
        groupBox1.Controls.Add(multiLineLabel);
        groupBox1.Controls.Add(multiLineTextBox);

        var groupBox2 = new GroupBox
        {
            Text = SR.GetString("GroupSelection"),
            Location = new Point(320, 10),
            Size = new Size(250, 160)
        };

        var genderLabel = new Label { Text = SR.GetString("LabelGender"), Location = new Point(10, 25), Size = new Size(100, 20) };
        var maleRadio = new RadioButton { Text = SR.GetString("RadioMale"), Location = new Point(10, 50), Size = new Size(100, 20), Checked = true };
        var femaleRadio = new RadioButton { Text = SR.GetString("RadioFemale"), Location = new Point(10, 75), Size = new Size(100, 20) };
        var otherRadio = new RadioButton { Text = SR.GetString("RadioOther"), Location = new Point(10, 100), Size = new Size(100, 20) };

        var roleLabel = new Label { Text = SR.GetString("LabelRole"), Location = new Point(120, 25), Size = new Size(80, 20) };
        var adminRadio = new RadioButton { Text = SR.GetString("RadioAdmin"), Location = new Point(120, 50), Size = new Size(80, 20) };
        var userRadio = new RadioButton { Text = SR.GetString("RadioUser"), Location = new Point(120, 75), Size = new Size(80, 20), Checked = true };
        var guestRadio = new RadioButton { Text = SR.GetString("RadioGuest"), Location = new Point(120, 100), Size = new Size(80, 20) };

        groupBox2.Controls.Add(genderLabel);
        groupBox2.Controls.Add(maleRadio);
        groupBox2.Controls.Add(femaleRadio);
        groupBox2.Controls.Add(otherRadio);
        groupBox2.Controls.Add(roleLabel);
        groupBox2.Controls.Add(adminRadio);
        groupBox2.Controls.Add(userRadio);
        groupBox2.Controls.Add(guestRadio);

        var groupBox3 = new GroupBox
        {
            Text = SR.GetString("GroupListsCombos"),
            Location = new Point(10, 230),
            Size = new Size(280, 310)
        };

        var listBoxLabel = new Label { Text = SR.GetString("LabelListBox"), Location = new Point(10, 25), Size = new Size(70, 20) };
        var listBox = new ListBox { Location = new Point(10, 45), Size = new Size(120, 150) };
        listBox.Items.Add(SR.GetString("ListItem1"));
        listBox.Items.Add(SR.GetString("ListItem2"));
        listBox.Items.Add(SR.GetString("ListItem3"));
        listBox.Items.Add(SR.GetString("ListItem4"));
        listBox.Items.Add(SR.GetString("ListItem5"));
        listBox.SelectedIndexChanged += (s, e) => OnStatusTextChanged(string.Format(SR.GetString("StatusListBoxFormat"), listBox.SelectedItem));

        var comboBoxLabel = new Label { Text = "ComboBox Styles", Location = new Point(140, 14), Size = new Size(130, 16) };

        var ddlStyleLabel = new Label { Text = "\u2014 DropDownList", Location = new Point(140, 34), Size = new Size(120, 14) };
        var ddlComboBox = new ComboBox { Location = new Point(140, 50), Size = new Size(120, 24) };
        ddlComboBox.Items.Add(SR.GetString("ComboActive"));
        ddlComboBox.Items.Add(SR.GetString("ComboInactive"));
        ddlComboBox.Items.Add(SR.GetString("ComboPending"));
        ddlComboBox.SelectedIndexChanged += (s, e) => OnStatusTextChanged("DropDownList: " + ddlComboBox.SelectedItem);

        var ddStyleLabel = new Label { Text = "\u2014 DropDown", Location = new Point(140, 80), Size = new Size(120, 14) };
        var ddComboBox = new ComboBox { Location = new Point(140, 96), Size = new Size(120, 24) };
        ddComboBox.Items.Add(SR.GetString("ComboActive"));
        ddComboBox.Items.Add(SR.GetString("ComboInactive"));
        ddComboBox.Items.Add(SR.GetString("ComboPending"));
        ddComboBox.DropDownStyle = DropDownStyle.DropDown;
        ddComboBox.SelectedIndex = 1;
        ddComboBox.SelectedIndexChanged += (s, e) => OnStatusTextChanged("DropDown: " + ddComboBox.SelectedItem);

        var simpleStyleLabel = new Label { Text = "\u2014 Simple", Location = new Point(140, 126), Size = new Size(120, 14) };
        var simpleComboBox = new ComboBox { Location = new Point(140, 142), Size = new Size(120, 96) };
        simpleComboBox.Items.Add(SR.GetString("ComboActive"));
        simpleComboBox.Items.Add(SR.GetString("ComboInactive"));
        simpleComboBox.Items.Add(SR.GetString("ComboPending"));
        simpleComboBox.DropDownStyle = DropDownStyle.Simple;
        simpleComboBox.SelectedIndexChanged += (s, e) => OnStatusTextChanged("Simple: " + simpleComboBox.SelectedItem);

        var people = new List<Person>
        {
            new(1, "Alice", "alice@example.com", "Active")    { Salary = 85000 },
            new(2, "Bob",   "bob@example.com",   "Inactive")  { Salary = 65000 },
            new(3, "Charlie", "charlie@example.com", "Active"){ Salary = 72000 },
        };
        var multiColLabel = new Label { Text = "\u2014 Multi-Column", Location = new Point(140, 248), Size = new Size(120, 14) };
        var multiColComboBox = new ComboBox { Location = new Point(140, 264), Size = new Size(120, 24) };
        multiColComboBox.DataSource = people;
        multiColComboBox.DropDownStyle = DropDownStyle.DropDown;
        multiColComboBox.DisplayMember = nameof(Person.Name);
        multiColComboBox.ColumnHeadersVisible = true;
        multiColComboBox.DropDownWidth = 0;
        multiColComboBox.Columns.Add(new ComboBoxColumn { HeaderText = "Name",   Width = 100, DataPropertyName = nameof(Person.Name) });
        multiColComboBox.Columns.Add(new ComboBoxColumn { HeaderText = "Email",  Width = 120, DataPropertyName = nameof(Person.Email) });
        multiColComboBox.Columns.Add(new ComboBoxColumn { HeaderText = "Salary", Width = 80,  DataPropertyName = nameof(Person.Salary),
                                                          TextAlign = DataGridViewContentAlignment.Right, FormatString = "N0" });
        multiColComboBox.SelectedIndexChanged += (s, e) => OnStatusTextChanged("Multi: " + multiColComboBox.SelectedItem);

        var checkBox1 = new CheckBox { Text = SR.GetString("ChkOptionA"), Location = new Point(10, 210), Size = new Size(100, 25) };
        var checkBox2 = new CheckBox { Text = SR.GetString("ChkOptionB"), Location = new Point(10, 235), Size = new Size(100, 25), Checked = true };
        var checkBox3 = new CheckBox { Text = SR.GetString("ChkOptionC"), Location = new Point(10, 260), Size = new Size(100, 25) };

        groupBox3.Controls.Add(listBoxLabel);
        groupBox3.Controls.Add(listBox);
        groupBox3.Controls.Add(comboBoxLabel);

        groupBox3.Controls.Add(ddlStyleLabel);
        groupBox3.Controls.Add(ddlComboBox);

        groupBox3.Controls.Add(ddStyleLabel);
        groupBox3.Controls.Add(ddComboBox);

        groupBox3.Controls.Add(simpleStyleLabel);
        groupBox3.Controls.Add(simpleComboBox);

        groupBox3.Controls.Add(multiColLabel);
        groupBox3.Controls.Add(multiColComboBox);
        groupBox3.Controls.Add(checkBox1);
        groupBox3.Controls.Add(checkBox2);
        groupBox3.Controls.Add(checkBox3);

        var groupBox4 = new GroupBox
        {
            Text = SR.GetString("GroupProgressButtons"),
            Location = new Point(300, 230),
            Size = new Size(270, 200)
        };

        var progressBar = new ProgressBar { Location = new Point(10, 25), Size = new Size(250, 20), Value = 60 };

        var progressButton = new Button { Text = SR.GetString("BtnProgress10"), Location = new Point(10, 55), Size = new Size(120, 30) };
        progressButton.Click += (s, e) => { if (progressBar.Value < 100) progressBar.Value += 10; };

        var resetProgressButton = new Button { Text = SR.GetString("BtnReset"), Location = new Point(140, 55), Size = new Size(120, 30) };
        resetProgressButton.Click += (s, e) => progressBar.Value = 0;

        var testButton = new Button { Text = SR.GetString("BtnTestButton"), Location = new Point(10, 95), Size = new Size(120, 30) };
        testButton.Click += (s, e) => OnStatusTextChanged(SR.GetString("StatusButtonClicked"));

        var disabledButton = new Button { Text = SR.GetString("BtnDisabled"), Location = new Point(140, 95), Size = new Size(120, 30), Enabled = false };

        var spinnerLabel = new Label { Text = SR.GetString("LabelSpinner"), Location = new Point(10, 140), Size = new Size(50, 20) };
        var spinner = new Spinner { Location = new Point(60, 132), Size = new Size(36, 36), Active = true, AutoStart = false };
        var spinnerButton = new Button { Text = SR.GetString("BtnToggle"), Location = new Point(110, 135), Size = new Size(140, 28) };
        spinnerButton.Click += (s, e) => spinner.Active = !spinner.Active;

        groupBox4.Controls.Add(progressBar);
        groupBox4.Controls.Add(progressButton);
        groupBox4.Controls.Add(resetProgressButton);
        groupBox4.Controls.Add(testButton);
        groupBox4.Controls.Add(disabledButton);
        groupBox4.Controls.Add(spinnerLabel);
        groupBox4.Controls.Add(spinner);
        groupBox4.Controls.Add(spinnerButton);

        var groupBox5 = new GroupBox
        {
            Text = SR.GetString("DtpModes"),
            Location = new Point(590, 10),
            Size = new Size(250, 200)
        };

        int y5 = 25;
        var dtpLabel1 = new Label { Text = SR.GetString("DtpShort"), Location = new Point(10, y5), Size = new Size(70, 20) };
        var dtp1 = new DateTimePicker { Location = new Point(85, y5 - 2), Size = new Size(150, 28), Format = DateTimePickerFormat.Short };
        y5 += 32;

        var dtpLabel2 = new Label { Text = SR.GetString("DtpTime"), Location = new Point(10, y5), Size = new Size(70, 20) };
        var dtp2 = new DateTimePicker { Location = new Point(85, y5 - 2), Size = new Size(150, 28), Format = DateTimePickerFormat.Time };
        y5 += 32;

        var dtpLabel3 = new Label { Text = SR.GetString("DtpLong"), Location = new Point(10, y5), Size = new Size(70, 20) };
        var dtp3 = new DateTimePicker { Location = new Point(85, y5 - 2), Size = new Size(150, 28), Format = DateTimePickerFormat.Long };
        y5 += 32;

        var dtpLabel4 = new Label { Text = SR.GetString("DtpUpDown"), Location = new Point(10, y5), Size = new Size(70, 20) };
        var dtp4 = new DateTimePicker { Location = new Point(85, y5 - 2), Size = new Size(150, 28), ShowUpDown = true };
        y5 += 32;

        var dtpLabel5 = new Label { Text = "Checked", Location = new Point(10, y5), Size = new Size(70, 20) };
        var dtp5 = new DateTimePicker { Location = new Point(85, y5 - 2), Size = new Size(150, 28), ShowCheckBox = true, Checked = true };

        groupBox5.Controls.Add(dtpLabel1);
        groupBox5.Controls.Add(dtp1);
        groupBox5.Controls.Add(dtpLabel2);
        groupBox5.Controls.Add(dtp2);
        groupBox5.Controls.Add(dtpLabel3);
        groupBox5.Controls.Add(dtp3);
        groupBox5.Controls.Add(dtpLabel4);
        groupBox5.Controls.Add(dtp4);
        groupBox5.Controls.Add(dtpLabel5);
        groupBox5.Controls.Add(dtp5);

        var groupBox6 = new GroupBox
        {
            Text = SR.GetString("GroupFontPicker"),
            Location = new Point(590, 220),
            Size = new Size(250, 100)
        };

        var fontPickerLabel = new Label { Text = SR.GetString("FontPickerLabel"), Location = new Point(10, 25), Size = new Size(100, 20) };
        var fontPicker = new FontPicker { Location = new Point(10, 50), Size = new Size(220, 30) };
        fontPicker.SelectedFontChanged += (s, e) =>
        {
            OnStatusTextChanged(string.Format("Font: {0}", fontPicker.SelectedFontFamily));
        };

        groupBox6.Controls.Add(fontPickerLabel);
        groupBox6.Controls.Add(fontPicker);

        Controls.Add(groupBox1);
        Controls.Add(groupBox2);
        Controls.Add(groupBox3);
        Controls.Add(groupBox4);
        Controls.Add(groupBox5);
        Controls.Add(groupBox6);
    }

    private void OnStatusTextChanged(string text)
    {
        StatusTextChanged?.Invoke(this, new StatusTextChangedEventArgs(text));
    }
}
