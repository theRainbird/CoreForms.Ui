using System.ComponentModel;
using System.Globalization;
using CoreForms.Ui.Controls.Advanced;
using CoreForms.Ui.Core;
using CoreForms.Ui.Controls.Basic;
using CoreForms.Ui.Controls.Containers;
using CoreForms.Ui.Controls.Separators;
using CoreForms.Ui.Controls;
using CoreForms.Ui.Data;
using CoreForms.Ui.WebBrowser.Controls;
using CoreForms.Ui.Theming;
using CoreForms.Ui.Reports;
using CoreForms.Ui.Resources;
using SkiaSharp;
using System.Collections.Generic;

namespace CoreForms.Ui.Demo;

class Program
{
    private static Label? _statusLabel;
    private static Controls.Advanced.DataGridView? _mainDataGrid;
    private static Form? _mainForm;

    [STAThread]
    static void Main()
    {
        LocalizationManager.LoadSavedCulture();

        _mainForm = new Form
        {
            Text = SR.GetString("FormTitle"),
            Width = 1200,
            Height = 900,
            Zoom = 1.25f
        };

        PopulateForm(_mainForm);

        Application.Run(_mainForm);
    }

    static void PopulateForm(Form form)
    {
        form.SuspendLayout();
        form.Controls.Clear();
        _statusLabel = null;
        _mainDataGrid = null;

        var menuStrip = CreateMenuStrip(form);
        var toolStrip = CreateToolStrip(form);
        var statusStrip = CreateStatusStrip();
        var mainTabControl = CreateMainTabControl();

        form.Controls.Add(menuStrip);
        form.Controls.Add(toolStrip);
        form.Controls.Add(statusStrip);
        form.Controls.Add(mainTabControl);

        _mainDataGrid = form.Controls.FindControl<Controls.Advanced.DataGridView>("dataGrid", recursive: true);

        form.Text = SR.GetString("FormTitle");
        form.ResumeLayout();
    }

    static MenuStrip CreateMenuStrip(Form form)
    {
        var menuStrip = new MenuStrip();
        menuStrip.Dock = DockStyle.Top;
        menuStrip.Size = new Size(900, 30);

        var fileItem = new ToolStripMenuItem(SR.GetString("MenuFile"));
        var fileNewItem = new ToolStripMenuItem(SR.GetString("MenuNew"));
        fileNewItem.Click += (s, e) => MessageBox.Show(SR.GetString("MsgTextNewFile"), SR.GetString("MsgTitleNewFile"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        var fileOpenItem = new ToolStripMenuItem(SR.GetString("MenuOpen"));
        fileOpenItem.Click += (s, e) => MessageBox.Show(SR.GetString("MsgTextOpen"), SR.GetString("MsgTitleOpen"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        var fileSaveItem = new ToolStripMenuItem(SR.GetString("MenuSave"));
        fileSaveItem.Click += (s, e) => MessageBox.Show(SR.GetString("MsgTextSaved"), SR.GetString("MsgTitleSave"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        var fileExitItem = new ToolStripMenuItem(SR.GetString("MenuExit"));
        fileExitItem.Click += (s, e) =>
        {
            var result = MessageBox.Show(SR.GetString("MsgTextExit"), SR.GetString("MsgTitleExit"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
                Application.Exit();
        };
        fileItem.DropDownItems.Add(fileNewItem);
        fileItem.DropDownItems.Add(fileOpenItem);
        fileItem.DropDownItems.Add(fileSaveItem);
        fileItem.DropDownItems.Add(fileExitItem);
        menuStrip.Items.Add(fileItem);

        var editItem = new ToolStripMenuItem(SR.GetString("MenuEdit"));
        var editUndoItem = new ToolStripMenuItem(SR.GetString("MenuUndo"));
        editUndoItem.Click += (s, e) => MessageBox.Show(SR.GetString("MsgTextUndo"), SR.GetString("MsgTitleUndo"), MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
        var editRedoItem = new ToolStripMenuItem(SR.GetString("MenuRedo"));
        editRedoItem.Click += (s, e) => MessageBox.Show(SR.GetString("MsgTextRedo"), SR.GetString("MsgTitleRedo"), MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
        var editDeleteItem = new ToolStripMenuItem(SR.GetString("MenuDelete"));
        editDeleteItem.Click += (s, e) => MessageBox.Show(SR.GetString("MsgTextDelete"), SR.GetString("MsgTitleDelete"), MessageBoxButtons.YesNoCancel, MessageBoxIcon.Error);
        var editSep1 = new ToolStripMenuItem("-");
        var editCutItem = new ToolStripMenuItem(SR.GetString("MenuCut"));
        editCutItem.Click += (s, e) => {
            if (form.ActiveControl is TextBox tb)
            {
                try { tb.Cut(); }
                catch (Exception ex) { MessageBox.Show($"Clipboard error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        };
        var editCopyItem = new ToolStripMenuItem(SR.GetString("MenuCopy"));
        editCopyItem.Click += (s, e) => {
            if (form.ActiveControl != null)
            {
                try {
                    var method = form.ActiveControl.GetType().GetMethod("CopyToClipboard");
                    method?.Invoke(form.ActiveControl, null);
                } catch (Exception ex) { MessageBox.Show(string.Format(SR.GetString("MsgTextClipboardError"), ex.Message), SR.GetString("MsgTitleError"), MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        };
        var editPasteItem = new ToolStripMenuItem(SR.GetString("MenuPaste"));
        editPasteItem.Click += (s, e) => {
            if (form.ActiveControl is TextBox tb)
            {
                try { tb.Paste(); }
                catch (Exception ex) { MessageBox.Show(string.Format(SR.GetString("MsgTextClipboardError"), ex.Message), SR.GetString("MsgTitleError"), MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        };
        var editSelectAllItem = new ToolStripMenuItem(SR.GetString("MenuSelectAll"));
        editSelectAllItem.Click += (s, e) => {
            if (form.ActiveControl is TextBox tb)
                tb.SelectAll();
        };
        editItem.DropDownItems.Add(editUndoItem);
        editItem.DropDownItems.Add(editRedoItem);
        editItem.DropDownItems.Add(editDeleteItem);
        editItem.DropDownItems.Add(editSep1);
        editItem.DropDownItems.Add(editCutItem);
        editItem.DropDownItems.Add(editCopyItem);
        editItem.DropDownItems.Add(editPasteItem);
        editItem.DropDownItems.Add(editSelectAllItem);
        menuStrip.Items.Add(editItem);

        var viewItem = new ToolStripMenuItem(SR.GetString("MenuView"));
        var viewRefreshItem = new ToolStripMenuItem(SR.GetString("MenuRefresh"));
        viewRefreshItem.Click += (s, e) => MessageBox.Show(SR.GetString("MsgTextRefreshed"), SR.GetString("MsgTitleRefresh"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        var viewFullscreenItem = new ToolStripMenuItem(SR.GetString("MenuFullscreen"));
        viewFullscreenItem.Click += (s, e) => MessageBox.Show(SR.GetString("MsgTextFullscreen"), SR.GetString("MsgTitleFullscreen"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        viewItem.DropDownItems.Add(viewRefreshItem);
        viewItem.DropDownItems.Add(viewFullscreenItem);
        menuStrip.Items.Add(viewItem);

        var helpItem = new ToolStripMenuItem(SR.GetString("MenuHelp"));
        var helpAboutItem = new ToolStripMenuItem(SR.GetString("MenuAbout"));
        helpAboutItem.Click += (s, e) => MessageBox.Show(string.Format(SR.GetString("MsgTextAbout"), "\n"), SR.GetString("MsgTitleAbout"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        var helpLicenseItem = new ToolStripMenuItem(SR.GetString("MenuLicense"));
        helpLicenseItem.Click += (s, e) => MessageBox.Show(SR.GetString("MsgTextLicense"), SR.GetString("MsgTitleLicenseError"), MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Error);
        helpItem.DropDownItems.Add(helpAboutItem);
        helpItem.DropDownItems.Add(helpLicenseItem);
        menuStrip.Items.Add(helpItem);

        var languageItem = new ToolStripMenuItem(SR.GetString("MenuLanguage"));
        languageItem.DropDownItems.Add(CreateLanguageItem("English", "en"));
        languageItem.DropDownItems.Add(CreateLanguageItem("Deutsch", "de"));
        languageItem.DropDownItems.Add(CreateLanguageItem("Français", "fr"));
        languageItem.DropDownItems.Add(CreateLanguageItem("Italiano", "it"));
        languageItem.DropDownItems.Add(CreateLanguageItem("Español", "es"));
        languageItem.DropDownItems.Add(CreateLanguageItem("Русский", "ru"));
        menuStrip.Items.Add(languageItem);

        return menuStrip;
    }

    static ToolStrip CreateToolStrip(Form form)
    {
        var toolStrip = new ToolStrip();
        toolStrip.Dock = DockStyle.Top;

        var newButton = new ToolStripButton(SR.GetString("ToolNew"), Icons.DocumentAdd24!);
        newButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
        newButton.Click += (s, e) => _statusLabel!.Text = SR.GetString("StatusNewClicked");
        toolStrip.Items.Add(newButton);

        var openButton = new ToolStripButton(SR.GetString("ToolOpen"), Icons.Document24!);
        openButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
        openButton.Click += (s, e) => _statusLabel!.Text = SR.GetString("StatusOpenClicked");
        toolStrip.Items.Add(openButton);

        var saveButton = new ToolStripButton(SR.GetString("ToolSave"), Icons.DocumentEdit24!);
        saveButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
        saveButton.Click += (s, e) => _statusLabel!.Text = SR.GetString("StatusSaveClicked");
        toolStrip.Items.Add(saveButton);

        toolStrip.Items.Add(new ToolStripSeparator());

        var boldButton = new ToolStripButton(SR.GetString("ToolBold"));
        boldButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        boldButton.CheckOnClick = true;
        boldButton.CheckedChanged += (s, e) => _statusLabel!.Text = string.Format(SR.GetString("StatusBoldFormat"), boldButton.Checked);
        toolStrip.Items.Add(boldButton);

        var italicButton = new ToolStripButton(SR.GetString("ToolItalic"));
        italicButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        italicButton.CheckOnClick = true;
        italicButton.CheckedChanged += (s, e) => _statusLabel!.Text = string.Format(SR.GetString("StatusItalicFormat"), italicButton.Checked);
        toolStrip.Items.Add(italicButton);

        toolStrip.Items.Add(new ToolStripSeparator());

        var searchBox = new ToolStripTextBox();
        searchBox.TextBoxWidth = 120;
        searchBox.TextChanged += (s, e) => _statusLabel!.Text = string.Format(SR.GetString("StatusSearchFormat"), searchBox.Text);
        toolStrip.Items.Add(searchBox);

        var searchButton = new ToolStripButton(SR.GetString("ToolSearch"), Icons.SearchSparkle24!);
        searchButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
        searchButton.Click += (s, e) => _statusLabel!.Text = string.Format(SR.GetString("StatusSearchingFormat"), searchBox.Text);
        toolStrip.Items.Add(searchButton);

        toolStrip.Items.Add(new ToolStripSeparator());

        var zoomLabel = new ToolStripLabel(SR.GetString("ToolZoomLabel"));
        toolStrip.Items.Add(zoomLabel);

        var zoomComboBox = new ToolStripLabel(SR.GetString("ToolZoom100"));
        zoomComboBox.IsLink = true;
        zoomComboBox.Click += (s, e) =>
        {
            form.Zoom = form.Zoom == 1.0f ? 1.5f : 1.0f;
            zoomComboBox.Text = $"{(int)(form.Zoom * 100)}%";
            _statusLabel!.Text = string.Format(SR.GetString("StatusZoomFormat"), zoomComboBox.Text);
        };
        toolStrip.Items.Add(zoomComboBox);

        toolStrip.Items.Add(new ToolStripSeparator());

        ToolStripButton? lightButton = null;
        ToolStripButton? darkButton = null;

        lightButton = new ToolStripButton(SR.GetString("ToolLight"), Icons.WeatherSunnyLow24!);
        lightButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
        lightButton.Checked = true;
        lightButton.CheckOnClick = true;
        lightButton.CheckedChanged += (s, e) =>
        {
            if (lightButton.Checked)
            {
                ThemeManager.SetTheme(new LightTheme());
                darkButton!.Checked = false;
                _statusLabel!.Text = SR.GetString("StatusThemeLight");
            }
        };
        toolStrip.Items.Add(lightButton);

        darkButton = new ToolStripButton(SR.GetString("ToolDark"), Icons.WeatherSnowflake24!);
        darkButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
        darkButton.CheckOnClick = true;
        darkButton.CheckedChanged += (s, e) =>
        {
            if (darkButton.Checked)
            {
                ThemeManager.SetTheme(new DarkTheme());
                lightButton!.Checked = false;
                _statusLabel!.Text = SR.GetString("StatusThemeDark");
            }
        };
        toolStrip.Items.Add(darkButton);

        toolStrip.Items.Add(new ToolStripSeparator());

        var toggleEnabledButton = new ToolStripButton(SR.GetString("ToolToggleEnabled"));
        toggleEnabledButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
        toggleEnabledButton.Click += (s, e) =>
        {
            ToggleControlsEnabled(form, toolStrip);
            _statusLabel!.Text = SR.GetString("StatusToggled");
        };
        toolStrip.Items.Add(toggleEnabledButton);

        toolStrip.Items.Add(new ToolStripSeparator());

        var helpButton = new ToolStripButton(SR.GetString("ToolHelp"), Icons.QuestionCircle24!);
        helpButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
        helpButton.Click += (s, e) => MessageBox.Show(string.Format(SR.GetString("MsgTextToolStripDemo"), "\n"), SR.GetString("MsgTitleHelp"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        toolStrip.Items.Add(helpButton);

        return toolStrip;
    }

    static ToolStripMenuItem CreateLanguageItem(string displayName, string cultureCode)
    {
        var item = new ToolStripMenuItem(displayName);
        item.Click += (s, e) =>
        {
            LocalizationManager.SetCulture(new CultureInfo(cultureCode));
            LocalizationManager.SaveCurrentCulture();
            if (_mainForm != null)
                PopulateForm(_mainForm);
        };
        return item;
    }

    static void ToggleControlsEnabled(Control parent, Control exclude)
    {
        foreach (var control in parent.Controls)
        {
            if (control == exclude)
                continue;
            control.Enabled = !control.Enabled;
            if (control.Controls.Count > 0)
                ToggleControlsEnabled(control, exclude);
        }
    }

    static Panel CreateStatusStrip()
    {
        _statusLabel = new Label { Text = SR.GetString("StatusReady") };

        var statusStrip = new Panel
        {
            Size = new Size(900, 24)
        };
        statusStrip.Dock = DockStyle.Bottom;
        statusStrip.Padding = new Padding(5, 2, 5, 2);
        _statusLabel.Location = new Point(5, 2);
        _statusLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        statusStrip.Controls.Add(_statusLabel);

        return statusStrip;
    }

    static TabControl CreateMainTabControl()
    {
        var tabControl = new TabControl();
        tabControl.Dock = DockStyle.Fill;

        tabControl.AddTabPage(CreateBasicControlsPage());
        tabControl.AddTabPage(CreateDataGridPage());
        tabControl.AddTabPage(CreateDataBindingPage());
        tabControl.AddTabPage(CreateMessageBoxPage());
        tabControl.AddTabPage(CreateFileDialogPage());
        tabControl.AddTabPage(CreatePrintDialogPage());
        tabControl.AddTabPage(CreateDockAnchorPage());
        tabControl.AddTabPage(CreateTreeViewPage());
        tabControl.AddTabPage(CreateUserControlPage());
        tabControl.AddTabPage(CreateImagesPage());
        tabControl.AddTabPage(CreateSplitPanelPage());
        tabControl.AddTabPage(CreateWebBrowserPage());
        tabControl.AddTabPage(CreateHtmlEditorPage());
        tabControl.AddTabPage(CreateCalendarPage());
        tabControl.AddTabPage(CreateMultiWindowPage());
        tabControl.AddTabPage(CreateReportPage());

        return tabControl;
    }

    static TabPage CreateBasicControlsPage()
    {
        var page = new TabPage { Text = SR.GetString("TabBasicControls") };

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
        listBox.SelectedIndexChanged += (s, e) => _statusLabel!.Text = string.Format(SR.GetString("StatusListBoxFormat"), listBox.SelectedItem);

        var comboBoxLabel = new Label { Text = "ComboBox Styles", Location = new Point(140, 14), Size = new Size(130, 16) };

        var ddlStyleLabel = new Label { Text = "\u2014 DropDownList", Location = new Point(140, 34), Size = new Size(120, 14) };
        var ddlComboBox = new ComboBox { Location = new Point(140, 50), Size = new Size(120, 24) };
        ddlComboBox.Items.Add(SR.GetString("ComboActive"));
        ddlComboBox.Items.Add(SR.GetString("ComboInactive"));
        ddlComboBox.Items.Add(SR.GetString("ComboPending"));
        ddlComboBox.SelectedIndexChanged += (s, e) => _statusLabel!.Text = "DropDownList: " + ddlComboBox.SelectedItem;

        var ddStyleLabel = new Label { Text = "\u2014 DropDown", Location = new Point(140, 80), Size = new Size(120, 14) };
        var ddComboBox = new ComboBox { Location = new Point(140, 96), Size = new Size(120, 24) };
        ddComboBox.Items.Add(SR.GetString("ComboActive"));
        ddComboBox.Items.Add(SR.GetString("ComboInactive"));
        ddComboBox.Items.Add(SR.GetString("ComboPending"));
        ddComboBox.DropDownStyle = DropDownStyle.DropDown;
        ddComboBox.SelectedIndex = 1;
        ddComboBox.SelectedIndexChanged += (s, e) => _statusLabel!.Text = "DropDown: " + ddComboBox.SelectedItem;

        var simpleStyleLabel = new Label { Text = "\u2014 Simple", Location = new Point(140, 126), Size = new Size(120, 14) };
        var simpleComboBox = new ComboBox { Location = new Point(140, 142), Size = new Size(120, 96) };
        simpleComboBox.Items.Add(SR.GetString("ComboActive"));
        simpleComboBox.Items.Add(SR.GetString("ComboInactive"));
        simpleComboBox.Items.Add(SR.GetString("ComboPending"));
        simpleComboBox.DropDownStyle = DropDownStyle.Simple;
        simpleComboBox.SelectedIndexChanged += (s, e) => _statusLabel!.Text = "Simple: " + simpleComboBox.SelectedItem;

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
        multiColComboBox.SelectedIndexChanged += (s, e) => _statusLabel!.Text = "Multi: " + multiColComboBox.SelectedItem;

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
        testButton.Click += (s, e) => _statusLabel!.Text = SR.GetString("StatusButtonClicked");

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

        page.Controls.Add(groupBox1);
        page.Controls.Add(groupBox2);
        page.Controls.Add(groupBox3);
        page.Controls.Add(groupBox4);
        page.Controls.Add(groupBox5);

        return page;
    }

    private static BindingList<Person>? _personList;

    static TabPage CreateDataGridPage()
    {
        var page = new TabPage { Text = SR.GetString("TabDataGrid") };

        var dataGrid = new Controls.Advanced.DataGridView
        {
            Location = new Point(10, 10),
            Size = new Size(550, 250),
            ColumnHeadersVisible = true,
            RowHeadersVisible = true,
            Name = "dataGrid"
        };

        dataGrid.Columns.Add(new Controls.Advanced.DataGridViewColumn
            { HeaderText = SR.GetString("ColId"), Width = 60, Name = "Id", DataPropertyName = "Id",
              TextAlign = DataGridViewContentAlignment.Right, FormatString = "D3" });
        dataGrid.Columns.Add(new Controls.Advanced.DataGridViewColumn
            { HeaderText = SR.GetString("ColName"), Width = 150, Name = "Name", DataPropertyName = "Name" });
        dataGrid.Columns.Add(new Controls.Advanced.DataGridViewColumn
            { HeaderText = SR.GetString("ColEmail"), Width = 200, Name = "Email", DataPropertyName = "Email" });
        dataGrid.Columns.Add(new Controls.Advanced.DataGridViewColumn
            { HeaderText = SR.GetString("ColStatus"), Width = 100, Name = "Status", DataPropertyName = "Status",
              TextAlign = DataGridViewContentAlignment.Center });
        dataGrid.Columns.Add(new Controls.Advanced.DataGridViewColumn
            { HeaderText = SR.GetString("ColSalary"), Width = 110, Name = "Salary", DataPropertyName = "Salary",
              TextAlign = DataGridViewContentAlignment.Right, FormatString = "C" });

        _personList = new BindingList<Person>
        {
            new Person(1, "John Doe", "john@example.com", "Active") { Salary = 75000m },
            new Person(2, "Jane Smith", "jane@example.com", "Active") { Salary = 82000m },
            new Person(3, "Bob Johnson", "bob@example.com", "Inactive") { Salary = 0m },
            new Person(4, "Alice Brown", "alice@example.com", "Active") { Salary = 91500m },
            new Person(5, "Charlie Wilson", "charlie@example.com", "Pending") { Salary = 68000m }
        };
        dataGrid.DataSource = _personList;

        var addButton = new Button { Text = SR.GetString("BtnAddRow"), Location = new Point(10, 270), Size = new Size(130, 30), Name = "addButton" };
        addButton.Click += (s, e) =>
        {
            var nextId = (_personList.Count > 0 ? _personList.Max(p => p.Id) : 0) + 1;
            _personList.Add(new Person(nextId, "New Person", "new@example.com", "Active"));
        };

        var removeButton = new Button { Text = SR.GetString("BtnRemoveLast"), Location = new Point(150, 270), Size = new Size(130, 30) };
        removeButton.Click += (s, e) =>
        {
            if (_personList.Count > 0)
                _personList.RemoveAt(_personList.Count - 1);
        };

        page.Controls.Add(dataGrid);
        page.Controls.Add(addButton);
        page.Controls.Add(removeButton);

        return page;
    }

    static TabPage CreateDataBindingPage()
    {
        var page = new TabPage { Text = SR.GetString("TabDataBinding") };

        #region Data source
        var contacts = new BindingList<Person>
        {
            new Person(1, "Alice Wonder", "alice@example.com", "Active"),
            new Person(2, "Bob Builder", "bob@example.com", "Active"),
            new Person(3, "Charlie Brown", "charlie@example.com", "Inactive")
        };
        var bindingSource = new BindingSource(contacts);

        #endregion

        #region Left: List + Navigation
        var listGroup = new GroupBox
        {
            Text = SR.GetString("GroupContactList"),
            Location = new Point(10, 10),
            Size = new Size(220, 350)
        };

        var contactListBox = new ListBox
        {
            Location = new Point(10, 25),
            Size = new Size(195, 200),
            TabStop = true
        };
        contactListBox.DataSource = bindingSource;
        contactListBox.DisplayMember = "Name";

        var navPanel = new Panel { Location = new Point(10, 235), Size = new Size(195, 100) };

        var firstButton = new Button { Text = SR.GetString("BtnFirst"), Location = new Point(0, 0), Size = new Size(45, 28) };
        var prevButton = new Button { Text = SR.GetString("BtnPrev"), Location = new Point(50, 0), Size = new Size(45, 28) };
        var nextButton = new Button { Text = SR.GetString("BtnNext"), Location = new Point(100, 0), Size = new Size(45, 28) };
        var lastButton = new Button { Text = SR.GetString("BtnLast"), Location = new Point(150, 0), Size = new Size(45, 28) };

        firstButton.Click += (s, e) => bindingSource.MoveFirst();
        prevButton.Click += (s, e) => bindingSource.MovePrevious();
        nextButton.Click += (s, e) => bindingSource.MoveNext();
        lastButton.Click += (s, e) => bindingSource.MoveLast();

        var addPersonButton = new Button
        {
            Text = SR.GetString("BtnAddContact"),
            Location = new Point(0, 35),
            Size = new Size(95, 28)
        };
        addPersonButton.Click += (s, e) =>
        {
            var nextId = contacts.Count > 0 ? contacts[^1].Id + 1 : 1;
            contacts.Add(new Person(nextId, "New Contact", "new@example.com", "Active"));
            bindingSource.MoveLast();
        };

        var removePersonButton = new Button
        {
            Text = SR.GetString("BtnRemove"),
            Location = new Point(100, 35),
            Size = new Size(95, 28)
        };
        removePersonButton.Click += (s, e) =>
        {
            if (bindingSource.Position >= 0 && bindingSource.Count > 0)
            {
                contacts.RemoveAt(bindingSource.Position);
            }
        };

        var positionLabel = new Label
        {
            Text = string.Format(SR.GetString("StatusPositionFormat"), 0, 0),
            Location = new Point(0, 70),
            Size = new Size(195, 20)
        };

        bindingSource.PositionChanged += (s, e) =>
        {
            positionLabel.Text = string.Format(SR.GetString("StatusPositionFormat"), bindingSource.Position + 1, bindingSource.Count);
        };
        positionLabel.Text = string.Format(SR.GetString("StatusPositionFormat"), 1, contacts.Count);

        navPanel.Controls.Add(firstButton);
        navPanel.Controls.Add(prevButton);
        navPanel.Controls.Add(nextButton);
        navPanel.Controls.Add(lastButton);
        navPanel.Controls.Add(addPersonButton);
        navPanel.Controls.Add(removePersonButton);
        navPanel.Controls.Add(positionLabel);

        listGroup.Controls.Add(contactListBox);
        listGroup.Controls.Add(navPanel);

        #endregion

        #region Right: Detail editing with Bindings
        var detailGroup = new GroupBox
        {
            Text = SR.GetString("GroupContactDetails"),
            Location = new Point(240, 10),
            Size = new Size(350, 350)
        };

        var idLabel = new Label { Text = SR.GetString("LabelId"), Location = new Point(10, 25), Size = new Size(60, 20) };
        var idValue = new Label { Text = "", Location = new Point(80, 25), Size = new Size(60, 20) };

        var nameLabel = new Label { Text = SR.GetString("LabelName"), Location = new Point(10, 55), Size = new Size(60, 20) };
        var nameTextBox = new TextBox { Location = new Point(80, 55), Size = new Size(250, 25) };

        var emailLabel = new Label { Text = SR.GetString("LabelEmail"), Location = new Point(10, 90), Size = new Size(60, 20) };
        var emailTextBox = new TextBox { Location = new Point(80, 90), Size = new Size(250, 25) };

        var statusLabel_ = new Label { Text = SR.GetString("LabelStatus"), Location = new Point(10, 125), Size = new Size(60, 20) };
        var statusComboBox = new ComboBox { Location = new Point(80, 125), Size = new Size(150, 25) };
        statusComboBox.Items.Add(SR.GetString("ComboActive"));
        statusComboBox.Items.Add(SR.GetString("ComboInactive"));
        statusComboBox.Items.Add(SR.GetString("ComboPending"));

        var isActiveCheckBox = new CheckBox
        {
            Text = SR.GetString("ChkIsActive"),
            Location = new Point(10, 165),
            Size = new Size(120, 25)
        };

        var feedbackLabel = new Label
        {
            Text = SR.GetString("LabelBindingFeedback"),
            Location = new Point(10, 210),
            Size = new Size(320, 40)
        };

        detailGroup.Controls.Add(idLabel);
        detailGroup.Controls.Add(idValue);
        detailGroup.Controls.Add(nameLabel);
        detailGroup.Controls.Add(nameTextBox);
        detailGroup.Controls.Add(emailLabel);
        detailGroup.Controls.Add(emailTextBox);
        detailGroup.Controls.Add(statusLabel_);
        detailGroup.Controls.Add(statusComboBox);
        detailGroup.Controls.Add(isActiveCheckBox);
        detailGroup.Controls.Add(feedbackLabel);

        #endregion

        #region Bottom: ComboBox + Live Preview
        var comboGroup = new GroupBox
        {
            Text = SR.GetString("GroupComboDataSource"),
            Location = new Point(10, 370),
            Size = new Size(250, 100)
        };
        var contactCombo = new ComboBox
        {
            Location = new Point(10, 25),
            Size = new Size(225, 25)
        };
        contactCombo.DataSource = bindingSource;
        contactCombo.DisplayMember = "Name";
        contactCombo.ValueMember = "Id";

        var selectedValueLabel = new Label
        {
            Text = string.Format(SR.GetString("StatusSelectedValueFormat"), "-"),
            Location = new Point(10, 60),
            Size = new Size(225, 20)
        };
        contactCombo.SelectedIndexChanged += (s, e) =>
        {
            selectedValueLabel.Text = string.Format(SR.GetString("StatusSelectedValueFormat"), contactCombo.SelectedValue);
        };

        comboGroup.Controls.Add(contactCombo);
        comboGroup.Controls.Add(selectedValueLabel);

        #endregion

        #region Bindings (after controls created)
        // Bind detail controls to bindingSource's current item
        // We bind to the BindingSource itself — it forwards change events
        nameTextBox.DataBindings.Add("Text", bindingSource, "Name");
        emailTextBox.DataBindings.Add("Text", bindingSource, "Email");
        isActiveCheckBox.DataBindings.Add("Checked", bindingSource, "IsActive");

        // Update ID label and status when current changes
        bindingSource.CurrentChanged += (s, e) =>
        {
            var current = bindingSource.Current as Person;
            if (current != null)
            {
                idValue.Text = current.Id.ToString();
                statusComboBox.Text = current.Status;
            }
            else
            {
                idValue.Text = "";
                statusComboBox.Text = "";
            }
        };
        // Initial display
        if (bindingSource.Current is Person firstPerson)
        {
            idValue.Text = firstPerson.Id.ToString();
            statusComboBox.Text = firstPerson.Status;
        }

        // Sync status back to person when ComboBox changes
        statusComboBox.SelectedIndexChanged += (s, e) =>
        {
            if (bindingSource.Current is Person p)
            {
                p.Status = statusComboBox.Text;
            }
        };

        #endregion

        page.Controls.Add(listGroup);
        page.Controls.Add(detailGroup);
        page.Controls.Add(comboGroup);

        return page;
    }

    static TabPage CreateMessageBoxPage()
    {
        var page = new TabPage { Text = SR.GetString("TabMessageBoxes") };

        var infoButton = new Button { Text = SR.GetString("BtnInformation"), Location = new Point(10, 10), Size = new Size(150, 35) };
        infoButton.Click += (s, e) =>
        {
            var result = MessageBox.Show(SR.GetString("MsgTextInformation"), SR.GetString("MsgTitleInformation"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            _statusLabel!.Text = string.Format(SR.GetString("StatusResultFormat"), result);
        };

        var warningButton = new Button { Text = SR.GetString("BtnWarning"), Location = new Point(170, 10), Size = new Size(150, 35) };
        warningButton.Click += (s, e) =>
        {
            var result = MessageBox.Show(SR.GetString("MsgTextWarning"), SR.GetString("MsgTitleWarning"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _statusLabel!.Text = string.Format(SR.GetString("StatusResultFormat"), result);
        };

        var errorButton = new Button { Text = SR.GetString("BtnError"), Location = new Point(330, 10), Size = new Size(150, 35) };
        errorButton.Click += (s, e) =>
        {
            var result = MessageBox.Show(SR.GetString("MsgTextError"), SR.GetString("MsgTitleError"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            _statusLabel!.Text = string.Format(SR.GetString("StatusResultFormat"), result);
        };

        var questionButton = new Button { Text = SR.GetString("BtnQuestionYesNo"), Location = new Point(10, 55), Size = new Size(150, 35) };
        questionButton.Click += (s, e) =>
        {
            var result = MessageBox.Show(SR.GetString("MsgTextQuestion"), SR.GetString("MsgTitleQuestion"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            _statusLabel!.Text = string.Format(SR.GetString("StatusResultFormat"), result);
        };

        var okCancelButton = new Button { Text = SR.GetString("BtnOKCancel"), Location = new Point(170, 55), Size = new Size(150, 35) };
        okCancelButton.Click += (s, e) =>
        {
            var result = MessageBox.Show(SR.GetString("MsgTextConfirmCancel"), SR.GetString("MsgTitleConfirmation"), MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
            _statusLabel!.Text = string.Format(SR.GetString("StatusResultFormat"), result);
        };

        var yesNoCancelButton = new Button { Text = SR.GetString("BtnYesNoCancel"), Location = new Point(330, 55), Size = new Size(180, 35) };
        yesNoCancelButton.Click += (s, e) =>
        {
            var result = MessageBox.Show(SR.GetString("MsgTextSaveChanges"), SR.GetString("MsgTitleSave"), MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
            _statusLabel!.Text = string.Format(SR.GetString("StatusResultFormat"), result);
        };

        var retryButton = new Button { Text = SR.GetString("BtnRetryCancel"), Location = new Point(10, 100), Size = new Size(180, 35) };
        retryButton.Click += (s, e) =>
        {
            var result = MessageBox.Show(SR.GetString("MsgTextConnectionFailed"), SR.GetString("MsgTitleConnection"), MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
            _statusLabel!.Text = string.Format(SR.GetString("StatusResultFormat"), result);
        };

        var abortButton = new Button { Text = SR.GetString("BtnAbortRetryIgnore"), Location = new Point(200, 100), Size = new Size(250, 35) };
        abortButton.Click += (s, e) =>
        {
            var result = MessageBox.Show(SR.GetString("MsgTextOperation"), SR.GetString("MsgTitleOperation"), MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Error);
            _statusLabel!.Text = string.Format(SR.GetString("StatusResultFormat"), result);
        };

        page.Controls.Add(infoButton);
        page.Controls.Add(warningButton);
        page.Controls.Add(errorButton);
        page.Controls.Add(questionButton);
        page.Controls.Add(okCancelButton);
        page.Controls.Add(yesNoCancelButton);
        page.Controls.Add(retryButton);
        page.Controls.Add(abortButton);

        return page;
    }

    static TabPage CreateFileDialogPage()
    {
        var page = new TabPage { Text = SR.GetString("TabFileDialogs") };

        // D-Bus diagnostic button (Linux only)
        var dbusTestButton = new Button
        {
            Text = SR.GetString("BtnTestDBus"),
            Location = new Point(10, 10),
            Size = new Size(180, 30)
        };
        dbusTestButton.Click += (s, e) =>
        {
            _statusLabel!.Text = SR.GetString("StatusTestDBus");
        };

        var groupBox = new GroupBox
        {
            Text = SR.GetString("GroupOpenFileDialog"),
            Location = new Point(10, 50),
            Size = new Size(350, 250)
        };

        var openSingleButton = new Button { Text = SR.GetString("BtnOpenFile"), Location = new Point(10, 25), Size = new Size(150, 35) };
        openSingleButton.Click += (s, e) =>
        {
            var dlg = new OpenFileDialog
            {
                Title = SR.GetString("DlgTitleSelectTextFile"),
                Filter = SR.GetString("DlgFilterTextAndAll"),
                FilterIndex = 1,
                CheckFileExists = true
            };
            var result = dlg.ShowDialog();
            _statusLabel!.Text = result == DialogResult.OK
                ? string.Format(SR.GetString("StatusOpenFormat"), dlg.FileName)
                : SR.GetString("StatusOpenCancelled");
        };

        var openMultiButton = new Button { Text = SR.GetString("BtnOpenFilesMulti"), Location = new Point(10, 70), Size = new Size(150, 35) };
        openMultiButton.Click += (s, e) =>
        {
            var dlg = new OpenFileDialog
            {
                Title = SR.GetString("DlgTitleSelectMulti"),
                Filter = SR.GetString("DlgFilterAllCSharpText"),
                Multiselect = true,
                CheckFileExists = true
            };
            var result = dlg.ShowDialog();
            if (result == DialogResult.OK)
            {
                _statusLabel!.Text = string.Format(SR.GetString("StatusOpenFormat"), string.Join("; ", dlg.FileNames));
            }
            else
            {
                _statusLabel!.Text = SR.GetString("StatusOpenCancelled");
            }
        };

        var openWithInitialDirButton = new Button { Text = SR.GetString("BtnOpenFromTemp"), Location = new Point(10, 115), Size = new Size(150, 35) };
        openWithInitialDirButton.Click += (s, e) =>
        {
            var dlg = new OpenFileDialog
            {
                Title = SR.GetString("DlgTitleOpenTemp"),
                InitialDirectory = "/tmp",
                Filter = SR.GetString("DlgFilterAll"),
                DefaultExt = "txt",
                AddExtension = true
            };
            var result = dlg.ShowDialog();
            _statusLabel!.Text = result == DialogResult.OK
                ? string.Format(SR.GetString("StatusOpenFormat"), dlg.FileName)
                : SR.GetString("StatusOpenCancelled");
        };

        var resultLabel = new Label
        {
            Text = SR.GetString("LabelResultInStatusBar"),
            Location = new Point(10, 170),
            Size = new Size(320, 50)
        };

        groupBox.Controls.Add(openSingleButton);
        groupBox.Controls.Add(openMultiButton);
        groupBox.Controls.Add(openWithInitialDirButton);
        groupBox.Controls.Add(resultLabel);

        var saveGroupBox = new GroupBox
        {
            Text = SR.GetString("GroupSaveFileDialog"),
            Location = new Point(370, 50),
            Size = new Size(350, 250)
        };

        var saveButton = new Button { Text = SR.GetString("BtnSaveFile"), Location = new Point(10, 25), Size = new Size(150, 35) };
        saveButton.Click += (s, e) =>
        {
            var dlg = new SaveFileDialog
            {
                Title = SR.GetString("DlgTitleSaveAs"),
                Filter = SR.GetString("DlgFilterTextAndAll"),
                DefaultExt = "txt",
                AddExtension = true,
                OverwritePrompt = true,
                FileName = SR.GetString("DefaultSaveFileName")
            };
            var result = dlg.ShowDialog();
            _statusLabel!.Text = result == DialogResult.OK
                ? string.Format(SR.GetString("StatusSaveFormat"), dlg.FileName)
                : SR.GetString("StatusSaveCancelled");
        };

        var saveWithDirButton = new Button { Text = SR.GetString("BtnSaveToTemp"), Location = new Point(10, 70), Size = new Size(150, 35) };
        saveWithDirButton.Click += (s, e) =>
        {
            var dlg = new SaveFileDialog
            {
                Title = SR.GetString("DlgTitleSaveTemp"),
                InitialDirectory = "/tmp",
                Filter = SR.GetString("DlgFilterCSharpAll"),
                DefaultExt = "cs",
                AddExtension = true,
                OverwritePrompt = true,
                FileName = SR.GetString("DefaultSaveFileNameCs")
            };
            var result = dlg.ShowDialog();
            _statusLabel!.Text = result == DialogResult.OK
                ? string.Format(SR.GetString("StatusSaveFormat"), dlg.FileName)
                : SR.GetString("StatusSaveCancelled");
        };

        saveGroupBox.Controls.Add(saveButton);
        saveGroupBox.Controls.Add(saveWithDirButton);

        page.Controls.Add(dbusTestButton);
        page.Controls.Add(groupBox);
        page.Controls.Add(saveGroupBox);

        return page;
    }

    static TabPage CreatePrintDialogPage()
    {
        var page = new TabPage { Text = SR.GetString("TabPrintDialog") };

        var printerListBox = new ListBox
        {
            Location = new Point(170, 10),
            Size = new Size(250, 200)
        };

        var setupButton = new Button { Text = SR.GetString("BtnPrintSetup"), Location = new Point(10, 10), Size = new Size(150, 35) };
        setupButton.Click += (s, e) =>
        {
            var dlg = new PrintDialog
            {
                AllowSomePages = true,
                AllowSelection = true,
                AllowCurrentPage = true
            };
            var result = dlg.ShowDialog();
            if (result == DialogResult.OK)
            {
                var ps = dlg.PrinterSettings!;
                _statusLabel!.Text = string.Format(SR.GetString("StatusPrintSetupFormat"), ps.PrinterName, ps.Copies, ps.PrintRange);
            }
            else
            {
                _statusLabel!.Text = SR.GetString("StatusPrintCancelled");
            }
        };

        var listButton = new Button { Text = SR.GetString("BtnPrinterList"), Location = new Point(10, 55), Size = new Size(150, 35) };
        listButton.Click += (s, e) =>
        {
            printerListBox.Items.Clear();
            var printers = PrinterSettings.InstalledPrinters;
            foreach (var p in printers)
                printerListBox.Items.Add(p);
            _statusLabel!.Text = string.Format(SR.GetString("StatusPrintersFoundFormat"), printers.Length);
        };

        var testPrintButton = new Button { Text = SR.GetString("BtnQuickPrint"), Location = new Point(10, 100), Size = new Size(150, 35) };
        testPrintButton.Click += (s, e) =>
        {
            var doc = new PrintDocument
            {
                DocumentName = SR.GetString("PrintDocName"),
                PrinterSettings = new PrinterSettings()
            };
            doc.DefaultPageSettings = new PageSettings
            {
                PaperSize = Core.PaperSize.A4,
                Landscape = false
            };
            doc.PrintPage += (sender, args) =>
            {
                var g = args.Graphics;
                if (g == null) return;

                int margin = 100;
                var bounds = args.MarginBounds;

                using (var paint = new SkiaSharp.SKPaint
                {
                    Color = SkiaSharp.SKColors.Black,
                    Style = SkiaSharp.SKPaintStyle.Stroke,
                    StrokeWidth = 2
                })
                {
                    g.DrawRectangle(Core.Color.Black, bounds.X, bounds.Y, bounds.Width, bounds.Height);
                }

                g.DrawString(SR.GetString("PrintTestPageTitle"), new Core.Font("Arial", 24), Core.Color.Black,
                    bounds.X + 10, bounds.Y + 10);

                g.DrawString(string.Format(SR.GetString("PrintDateFormat"), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")), new Core.Font("Arial", 12), Core.Color.Black,
                    bounds.X + 10, bounds.Y + 60);

                g.DrawString(SR.GetString("PrintTestPageBody"), new Core.Font("Arial", 12), Core.Color.Black,
                    bounds.X + 10, bounds.Y + 100);

                args.HasMorePages = false;
            };
            try
            {
                doc.Print();
                _statusLabel!.Text = SR.GetString("StatusPrintSent");
            }
            catch (Exception ex)
            {
                _statusLabel!.Text = string.Format(SR.GetString("StatusPrintErrorFormat"), ex.Message);
                Console.WriteLine($"[Print] Error: {ex}");
            }
        };

        page.Controls.Add(setupButton);
        page.Controls.Add(listButton);
        page.Controls.Add(testPrintButton);
        page.Controls.Add(printerListBox);

        return page;
    }

    static TabPage CreateDockAnchorPage()
    {
        var page = new TabPage { Text = SR.GetString("TabDockAnchor") };

        var dockPanel = new Panel
        {
            Location = new Point(10, 10),
            Size = new Size(250, 300),
            BorderStyle = BorderStyle.FixedSingle
        };

        var dockedTopLabel = new Label { Text = SR.GetString("LabelDockTop"), Size = new Size(250, 22) };
        dockedTopLabel.Dock = DockStyle.Top;

        var hSep = new SeperatorControl
        {
            Orientation = SeperatorOrientation.Horizontal,
            Size = new Size(250, 2),
            Dock = DockStyle.Top
        };

        var dockedBottomLabel = new Label { Text = SR.GetString("LabelDockBottom"), Size = new Size(250, 22) };
        dockedBottomLabel.Dock = DockStyle.Bottom;

        var dockedLeftLabel = new Label { Text = SR.GetString("LabelDockLeft"), Size = new Size(30, 176) };
        dockedLeftLabel.Dock = DockStyle.Left;

        var vSep = new SeperatorControl
        {
            Orientation = SeperatorOrientation.Vertical,
            Size = new Size(2, 176),
            Dock = DockStyle.Left
        };

        var centerLabel = new Label { Text = SR.GetString("LabelDockFill"), Size = new Size(50, 50) };
        centerLabel.Dock = DockStyle.Fill;

        dockPanel.Controls.Add(dockedTopLabel);
        dockPanel.Controls.Add(hSep);
        dockPanel.Controls.Add(dockedBottomLabel);
        dockPanel.Controls.Add(dockedLeftLabel);
        dockPanel.Controls.Add(vSep);
        dockPanel.Controls.Add(centerLabel);

        var anchorPanel = new Panel
        {
            Location = new Point(270, 10),
            Size = new Size(300, 300),
            BorderStyle = BorderStyle.FixedSingle
        };

        var anchorLabel = new Label
        {
            Text = SR.GetString("LabelAnchorAll"),
            Location = new Point(10, 10),
            Size = new Size(280, 280)
        };
        anchorLabel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        anchorPanel.Controls.Add(anchorLabel);

        page.Controls.Add(dockPanel);
        page.Controls.Add(anchorPanel);

        return page;
    }

    static TabPage CreateTreeViewPage()
    {
        var page = new TabPage { Text = SR.GetString("TabTreeView") };

        // Create an ImageList with some icons
        var imageList = new ImageList();
        // Using existing icons for folder and file representations
        imageList.Add(Icons.DocumentFolder24 ?? Icons.Document24!);
        imageList.Add(Icons.DocumentFolder24 ?? Icons.Document24!);
        imageList.Add(Icons.Document24!);

        // Create TreeView
        var treeView = new TreeView
        {
            Location = new Point(10, 10),
            Size = new Size(400, 300),
            ImageList = imageList
        };

        // Build sample nodes
        var root1 = new TreeNode(SR.GetString("TreeNodeRoot1")) { ImageIndex = 0 };
        var child1 = new TreeNode(SR.GetString("TreeNodeChild1")) { ImageIndex = 1 };
        var child2 = new TreeNode(SR.GetString("TreeNodeChild2")) { ImageIndex = 2 };
        root1.Add(child1);
        root1.Add(child2);

        var root2 = new TreeNode(SR.GetString("TreeNodeRoot2")) { ImageIndex = 0 };
        var subRoot = new TreeNode(SR.GetString("TreeNodeSubRoot")) { ImageIndex = 1 };
        subRoot.Add(new TreeNode(SR.GetString("TreeNodeLeafA")) { ImageIndex = 2 });
        subRoot.Add(new TreeNode(SR.GetString("TreeNodeLeafB")) { ImageIndex = 2 });
        root2.Add(subRoot);

        treeView.Nodes.Add(root1);
        treeView.Nodes.Add(root2);

        page.Controls.Add(treeView);
        return page;
    }

    static TabPage CreateUserControlPage()
    {
        var page = new TabPage { Text = SR.GetString("TabUserControl") };

        var loginControl = new LoginUserControl
        {
            Location = new Point(10, 10),
            Size = new Size(350, 200)
        };
        loginControl.LoginClicked += (s, e) =>
        {
            _statusLabel!.Text = string.Format(SR.GetString("StatusLoginFormat"), loginControl.Username);
        };

        page.Controls.Add(loginControl);
        return page;
    }

    static TabPage CreateImagesPage()
    {
        var page = new TabPage { Text = SR.GetString("TabImages") };

        var svgGroup = new GroupBox
        {
            Text = SR.GetString("GroupSvgImages"),
            Location = new Point(10, 10),
            Size = new Size(250, 200)
        };

        var svgPictureBox = new PictureBox
        {
            Location = new Point(15, 25),
            Size = new Size(80, 80),
            SizeMode = PictureBoxSizeMode.Zoom
        };
        svgPictureBox.Image = Icons.CheckmarkCircle24;

        var svgStretchBox = new PictureBox
        {
            Location = new Point(105, 25),
            Size = new Size(120, 80),
            SizeMode = PictureBoxSizeMode.StretchImage
        };
        svgStretchBox.Image = Icons.Image24;

        var svgCenterBox = new PictureBox
        {
            Location = new Point(15, 115),
            Size = new Size(210, 60),
            SizeMode = PictureBoxSizeMode.CenterImage
        };
        svgCenterBox.Image = Icons.CheckmarkCircle24;

        var svgLabel = new Label { Text = SR.GetString("LabelSvgModes"), Location = new Point(15, 175), Size = new Size(210, 20) };

        svgGroup.Controls.Add(svgPictureBox);
        svgGroup.Controls.Add(svgStretchBox);
        svgGroup.Controls.Add(svgCenterBox);
        svgGroup.Controls.Add(svgLabel);

        var rasterGroup = new GroupBox
        {
            Text = SR.GetString("GroupRasterImages"),
            Location = new Point(270, 10),
            Size = new Size(250, 200)
        };

        var pngBytes = CreateDemoPng();
        var rasterImage = RasterImage.FromBytes(pngBytes);

        var rasterNormalBox = new PictureBox
        {
            Location = new Point(15, 25),
            Size = new Size(60, 60),
            SizeMode = PictureBoxSizeMode.Normal
        };
        rasterNormalBox.Image = rasterImage;

        var rasterStretchBox = new PictureBox
        {
            Location = new Point(85, 25),
            Size = new Size(150, 60),
            SizeMode = PictureBoxSizeMode.StretchImage
        };
        rasterStretchBox.Image = rasterImage;

        var rasterZoomBox = new PictureBox
        {
            Location = new Point(15, 95),
            Size = new Size(220, 80),
            SizeMode = PictureBoxSizeMode.Zoom
        };
        rasterZoomBox.Image = rasterImage;

        var rasterLabel = new Label { Text = SR.GetString("LabelRasterModes"), Location = new Point(15, 175), Size = new Size(210, 20) };

        rasterGroup.Controls.Add(rasterNormalBox);
        rasterGroup.Controls.Add(rasterStretchBox);
        rasterGroup.Controls.Add(rasterZoomBox);
        rasterGroup.Controls.Add(rasterLabel);

        page.Controls.Add(svgGroup);
        page.Controls.Add(rasterGroup);
        return page;
    }

    private static byte[] CreateDemoPng()
    {
        using var bitmap = new SKBitmap(32, 32, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(new SKColor(65, 105, 225));

        using var paint = new SKPaint { Color = SKColors.White, IsAntialias = true };
        canvas.DrawCircle(16, 16, 10, paint);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    static TabPage CreateSplitPanelPage()
    {
        var page = new TabPage { Text = SR.GetString("TabSplitPanel") };

        var verticalSplit = new SplitPanel
        {
            Location = new Point(10, 10),
            Size = new Size(300, 250),
            Orientation = SplitOrientation.Vertical,
            SplitterDistance = 120,
            BorderStyle = BorderStyle.FixedSingle
        };

        var topLabel = new Label
        {
            Text = SR.GetString("LabelTopPanel"),
            Location = new Point(10, 10),
            Size = new Size(100, 30)
        };
        var topButton = new Button
        {
            Text = SR.GetString("BtnButtonInTop"),
            Location = new Point(10, 50),
            Size = new Size(120, 25)
        };
        topButton.Click += (s, e) => _statusLabel!.Text = SR.GetString("StatusTopButtonClicked");
        verticalSplit.Panel1.Controls.Add(topLabel);
        verticalSplit.Panel1.Controls.Add(topButton);

        var bottomTextBox = new TextBox
        {
            Text = SR.GetString("LabelBottomPanel"),
            Location = new Point(10, 10),
            Size = new Size(260, 25)
        };
        verticalSplit.Panel2.Controls.Add(bottomTextBox);

        var horizontalSplit = new SplitPanel
        {
            Location = new Point(320, 10),
            Size = new Size(300, 250),
            Orientation = SplitOrientation.Horizontal,
            SplitterDistance = 120
        };

        var leftLabel = new Label
        {
            Text = SR.GetString("LabelLeftPanel"),
            Location = new Point(10, 10),
            Size = new Size(100, 30)
        };
        horizontalSplit.Panel1.Controls.Add(leftLabel);

        var rightLabel = new Label
        {
            Text = SR.GetString("LabelRightPanel"),
            Location = new Point(10, 10),
            Size = new Size(140, 30)
        };
        var rightButton = new Button
        {
            Text = SR.GetString("BtnButtonInRight"),
            Location = new Point(10, 50),
            Size = new Size(120, 25)
        };
        rightButton.Click += (s, e) => _statusLabel!.Text = SR.GetString("StatusRightButtonClicked");
        horizontalSplit.Panel2.Controls.Add(rightLabel);
        horizontalSplit.Panel2.Controls.Add(rightButton);

        var distanceLabel = new Label
        {
            Text = string.Format(SR.GetString("StatusSplitterDistFormat"), verticalSplit.SplitterDistance),
            Location = new Point(10, 275),
            Size = new Size(200, 20)
        };

        var set200Button = new Button
        {
            Text = SR.GetString("BtnSetDist200"),
            Location = new Point(10, 300),
            Size = new Size(120, 25)
        };
        set200Button.Click += (s, e) =>
        {
            verticalSplit.SplitterDistance = 200;
            distanceLabel.Text = string.Format(SR.GetString("StatusSplitterDistFormat"), verticalSplit.SplitterDistance);
            _statusLabel!.Text = SR.GetString("StatusSplitterSet200");
        };

        var toggleOrientationButton = new Button
        {
            Text = SR.GetString("BtnToggleOrientation"),
            Location = new Point(140, 300),
            Size = new Size(150, 25)
        };
        toggleOrientationButton.Click += (s, e) =>
        {
            horizontalSplit.Orientation = horizontalSplit.Orientation == SplitOrientation.Horizontal
                ? SplitOrientation.Vertical
                : SplitOrientation.Horizontal;
            _statusLabel!.Text = string.Format(SR.GetString("StatusOrientationFormat"), horizontalSplit.Orientation);
        };

        var incMinSizeButton = new Button
        {
            Text = SR.GetString("BtnPanel1Min10"),
            Location = new Point(10, 335),
            Size = new Size(120, 25)
        };
        incMinSizeButton.Click += (s, e) =>
        {
            verticalSplit.Panel1MinSize += 10;
            _statusLabel!.Text = string.Format(SR.GetString("StatusPanel1MinSizeFormat"), verticalSplit.Panel1MinSize);
        };

        verticalSplit.SplitterMoved += (s, e) =>
        {
            distanceLabel.Text = string.Format(SR.GetString("StatusSplitterDistFormat"), verticalSplit.SplitterDistance);
            _statusLabel!.Text = string.Format(SR.GetString("StatusSplitterMovedFormat"), verticalSplit.SplitterDistance);
        };

        page.Controls.Add(verticalSplit);
        page.Controls.Add(horizontalSplit);
        page.Controls.Add(distanceLabel);
        page.Controls.Add(set200Button);
        page.Controls.Add(toggleOrientationButton);
        page.Controls.Add(incMinSizeButton);

        return page;
    }

    static TabPage CreateWebBrowserPage()
    {
        var page = new TabPage { Text = SR.GetString("TabWebBrowser") };

        var toolStrip = new ToolStrip { Dock = DockStyle.Top };

        // Navigation buttons
        var backButton = new ToolStripButton(SR.GetString("BtnWebBack"));
        var forwardButton = new ToolStripButton(SR.GetString("BtnWebForward"));
        var refreshButton = new ToolStripButton("", Icons.ArrowSync24!) { DisplayStyle = ToolStripItemDisplayStyle.Image };
        var stopButton = new ToolStripButton("", Icons.DismissCircle24!) { DisplayStyle = ToolStripItemDisplayStyle.Image };

        // URL bar
        var urlTextBox = new ToolStripTextBox { TextBoxWidth = 400 };
        var goButton = new ToolStripButton("", Icons.SearchSparkle24!) { DisplayStyle = ToolStripItemDisplayStyle.Image };

        // Zoom buttons
        var zoomInButton = new ToolStripButton("", Icons.AddCircle24!) { DisplayStyle = ToolStripItemDisplayStyle.Image };
        var zoomOutButton = new ToolStripButton("", Icons.DismissCircle24!) { DisplayStyle = ToolStripItemDisplayStyle.Image };
        var zoomResetButton = new ToolStripButton("100%");

        var webView = new WebView
        {
            Dock = DockStyle.Fill
        };

        backButton.Click += (s, e) => webView.GoBack();
        forwardButton.Click += (s, e) => webView.GoForward();
        refreshButton.Click += (s, e) => webView.Refresh();
        stopButton.Click += (s, e) => webView.Stop();
        zoomInButton.Click += (s, e) => webView.ZoomIn();
        zoomOutButton.Click += (s, e) => webView.ZoomOut();
        zoomResetButton.Click += (s, e) => webView.ResetZoom();

        void NavigateToUrl()
        {
            var url = urlTextBox.Text;
            if (!string.IsNullOrWhiteSpace(url))
                webView.Navigate(url);
        }

        goButton.Click += (s, e) => NavigateToUrl();

        // Enter in URL box triggers navigation (via ToolStrip.KeyDown)
        toolStrip.KeyDown += (s, e) =>
        {
            if (e is CoreForms.Ui.Core.KeyEventArgs ke && ke.KeyCode == CoreForms.Ui.Core.Keys.Enter)
            {
                NavigateToUrl();
                ke.Handled = true;
            }
        };

        // Ctrl+0 for reset zoom
        webView.KeyDown += (s, e) =>
        {
            if (e is CoreForms.Ui.Core.KeyEventArgs ke && ke.Modifiers.HasFlag(CoreForms.Ui.Core.ModifierKeys.Control))
            {
                switch (ke.KeyCode)
                {
                    case CoreForms.Ui.Core.Keys.D0:
                        webView.ResetZoom();
                        ke.Handled = true;
                        break;
                }
            }
        };

        webView.Navigated += (s, e) =>
        {
            urlTextBox.Text = e.Url ?? string.Empty;
        };

        toolStrip.Items.AddRange(new ToolStripItem[]
        {
            backButton, forwardButton, refreshButton, stopButton,
            new ToolStripSeparator(),
            urlTextBox, goButton,
            new ToolStripSeparator(),
            zoomInButton, zoomOutButton, zoomResetButton
        });

        page.Controls.Add(toolStrip);
        page.Controls.Add(webView);

        return page;
    }

    static TabPage CreateHtmlEditorPage()
    {
        var page = new TabPage { Text = SR.GetString("TabHtmlEditor") };

        var toolStrip = new ToolStrip { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden };

        var boldButton = new ToolStripButton(SR.GetString("ToolBold"), Icons.TextEditStyle24!) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText };
        var italicButton = new ToolStripButton(SR.GetString("ToolItalic")) { DisplayStyle = ToolStripItemDisplayStyle.Text };
        var underlineButton = new ToolStripButton(SR.GetString("ToolUnderline")) { DisplayStyle = ToolStripItemDisplayStyle.Text };
        var bulletListButton = new ToolStripButton("", Icons.TextBulletListSquare24!) { DisplayStyle = ToolStripItemDisplayStyle.Image };
        var numberListButton = new ToolStripButton("", Icons.NumberSymbolSquare24!) { DisplayStyle = ToolStripItemDisplayStyle.Image };
        var linkButton = new ToolStripButton("", Icons.Link24!) { DisplayStyle = ToolStripItemDisplayStyle.Image };
        var imageButton = new ToolStripButton("", Icons.Image24!) { DisplayStyle = ToolStripItemDisplayStyle.Image };

        var htmlBox = new HtmlBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = false,
            Html = @"<h1>HTML Editor</h1>
<p>Welcome to the <b>CoreForms</b> HTML editor!</p>
<p>This is a <a href=""https://example.com"">link</a> example.</p>
<ul>
<li>First item</li>
<li>Second item</li>
<li>Third item</li>
</ul>
<ol>
<li>Number one</li>
<li>Number two</li>
<li>Number three</li>
</ol>"
        };

        void UpdateFormatButtons()
        {
            boldButton.Checked = htmlBox.IsBold;
            italicButton.Checked = htmlBox.IsItalic;
            underlineButton.Checked = htmlBox.IsUnderline;
        }

        boldButton.Click += (s, e) => { htmlBox.ApplyFormat("bold"); UpdateFormatButtons(); };
        italicButton.Click += (s, e) => { htmlBox.ApplyFormat("italic"); UpdateFormatButtons(); };
        underlineButton.Click += (s, e) => { htmlBox.ApplyFormat("underline"); UpdateFormatButtons(); };
        bulletListButton.Click += (s, e) => { htmlBox.ApplyFormat("insertUnorderedList"); };
        numberListButton.Click += (s, e) => { htmlBox.ApplyFormat("insertOrderedList"); };
        linkButton.Click += (s, e) => { htmlBox.ApplyFormat("createLink"); };
        imageButton.Click += (s, e) => { htmlBox.ApplyFormat("insertImage"); };
        htmlBox.ContentChanged += (s, e) => { UpdateFormatButtons(); _statusLabel!.Text = htmlBox.CursorDebug; };

        toolStrip.Items.Add(boldButton);
        toolStrip.Items.Add(italicButton);
        toolStrip.Items.Add(underlineButton);
        toolStrip.Items.Add(new ToolStripSeparator());
        toolStrip.Items.Add(bulletListButton);
        toolStrip.Items.Add(numberListButton);
        toolStrip.Items.Add(new ToolStripSeparator());
        toolStrip.Items.Add(linkButton);
        toolStrip.Items.Add(imageButton);

        page.Controls.Add(toolStrip);
        page.Controls.Add(htmlBox);

        return page;
    }

    static TabPage CreateCalendarPage()
    {
        var page = new TabPage { Text = SR.GetString("TabCalendar") };

        var calendar = new CalendarView
        {
            Dock = DockStyle.Fill,
            ViewType = CalendarViewType.Month
        };

        // Add some sample appointments
        var today = DateTime.Today;
        calendar.Appointments.Add(new CalendarAppointment
        {
            Subject = SR.GetString("AppointmentTeamMeeting"),
            StartTime = today.AddHours(10),
            EndTime = today.AddHours(11),
            Location = SR.GetString("AppointmentRoom101"),
            CategoryColor = CoreForms.Ui.Controls.Advanced.CalendarView.CategoryColors[0]
        });

        calendar.Appointments.Add(new CalendarAppointment
        {
            Subject = SR.GetString("AppointmentLunch"),
            StartTime = today.AddHours(12),
            EndTime = today.AddHours(13),
            Location = SR.GetString("AppointmentCafeteria"),
            CategoryColor = CoreForms.Ui.Controls.Advanced.CalendarView.CategoryColors[2]
        });

        calendar.Appointments.Add(new CalendarAppointment
        {
            Subject = SR.GetString("AppointmentProjectReview"),
            StartTime = today.AddHours(14).AddMinutes(30),
            EndTime = today.AddHours(16),
            Location = SR.GetString("AppointmentConfRoom"),
            CategoryColor = CoreForms.Ui.Controls.Advanced.CalendarView.CategoryColors[1]
        });

        calendar.Appointments.Add(new CalendarAppointment
        {
            Subject = SR.GetString("AppointmentWorkshop"),
            StartTime = today.AddDays(1).AddHours(9),
            EndTime = today.AddDays(1).AddHours(12),
            Location = SR.GetString("AppointmentTrainingRoom"),
            CategoryColor = CoreForms.Ui.Controls.Advanced.CalendarView.CategoryColors[3]
        });

        calendar.Appointments.Add(new CalendarAppointment
        {
            Subject = SR.GetString("AppointmentSprintPlanning"),
            StartTime = today.AddDays(3).AddHours(10),
            EndTime = today.AddDays(3).AddHours(12),
            Location = SR.GetString("AppointmentRoom204"),
            CategoryColor = CoreForms.Ui.Controls.Advanced.CalendarView.CategoryColors[0]
        });

        calendar.Appointments.Add(new CalendarAppointment
        {
            Subject = SR.GetString("AppointmentConference"),
            StartTime = today.AddDays(10),
            EndTime = today.AddDays(12),
            IsAllDay = true,
            CategoryColor = CoreForms.Ui.Controls.Advanced.CalendarView.CategoryColors[4]
        });

        // Event handlers
        calendar.DateSelected += (s, e) =>
        {
            _statusLabel!.Text = string.Format(SR.GetString("StatusSelectedDateFormat"), e.Date.ToString("dddd, MMMM dd, yyyy"));
        };

        calendar.ViewChanged += (s, e) =>
        {
            _statusLabel!.Text = string.Format(SR.GetString("StatusViewFormat"), calendar.ViewType);
        };

        page.Controls.Add(calendar);
        return page;
    }

    static TabPage CreateMultiWindowPage()
    {
        var page = new TabPage { Text = SR.GetString("TabMultiWindows") };
        var modelessGroup = new GroupBox
        {
            Text = SR.GetString("GroupModeless"),
            Location = new Point(10, 10),
            Size = new Size(280, 120)
        };

        var btnSimpleWindow = new Button
        {
            Text = SR.GetString("BtnOpenSimpleWindow"),
            Location = new Point(10, 25),
            Size = new Size(250, 35)
        };
        btnSimpleWindow.Click += (s, e) =>
        {
            var win = new Form
            {
                Title = SR.GetString("SimpleWindowTitle"),
                Text = SR.GetString("SimpleWindowTitle"),
                Width = 400,
                Height = 300
            };
            var label = new Label
            {
                Text = SR.GetString("SimpleWindowLabel"),
                Location = new Point(20, 20),
                Size = new Size(350, 30)
            };
            win.Controls.Add(label);
            win.Show();
            _statusLabel!.Text = SR.GetString("StatusSimpleWindowOpened");
        };

        var btnWindowControls = new Button
        {
            Text = SR.GetString("BtnOpenWindowWithControls"),
            Location = new Point(10, 70),
            Size = new Size(250, 35)
        };
        btnWindowControls.Click += (s, e) =>
        {
            int clickCount = 0;
            var win = new Form
            {
                Title = SR.GetString("WindowControlsTitle"),
                Text = SR.GetString("WindowControlsTitle"),
                Width = 400,
                Height = 250,

            };
            var clickLabel = new Label
            {
                Text = string.Format(SR.GetString("LabelClickCount"), 0),
                Location = new Point(20, 20),
                Size = new Size(350, 30)
            };
            var countButton = new Button
            {
                Text = SR.GetString("BtnTestButton"),
                Location = new Point(20, 60),
                Size = new Size(120, 30)
            };
            countButton.Click += (_, _) =>
            {
                clickCount++;
                clickLabel.Text = string.Format(SR.GetString("LabelClickCount"), clickCount);
            };
            win.Controls.Add(clickLabel);
            win.Controls.Add(countButton);
            win.Show();
            _statusLabel!.Text = SR.GetString("StatusWindowWithControlsOpened");
        };

        modelessGroup.Controls.Add(btnSimpleWindow);
        modelessGroup.Controls.Add(btnWindowControls);

        var modalGroup = new GroupBox
        {
            Text = SR.GetString("GroupModal"),
            Location = new Point(300, 10),
            Size = new Size(280, 120)
        };

        var btnModalDialog = new Button
        {
            Text = SR.GetString("BtnOpenModalDialog"),
            Location = new Point(10, 25),
            Size = new Size(250, 35)
        };
        btnModalDialog.Click += (s, e) =>
        {
            var dlg = new Form
            {
                Title = SR.GetString("ModalWindowTitle"),
                Text = SR.GetString("ModalWindowTitle"),
                Width = 350,
                Height = 200,
                FormBorderStyle = FormBorderStyle.FixedDialog,

            };
            var label = new Label
            {
                Text = SR.GetString("ModalWindowLabel"),
                Location = new Point(20, 20),
                Size = new Size(300, 60)
            };
            dlg.Controls.Add(label);
            _statusLabel!.Text = SR.GetString("StatusModalDialogOpened");
            var result = dlg.ShowDialog(_mainForm);
            _statusLabel!.Text = SR.GetString("StatusModalClosed");
        };

        var btnModalConfirm = new Button
        {
            Text = SR.GetString("BtnOpenModalConfirm"),
            Location = new Point(10, 70),
            Size = new Size(250, 35)
        };
        btnModalConfirm.Click += (s, e) =>
        {
            var dlg = new Form
            {
                Title = SR.GetString("ConfirmTitle"),
                Text = SR.GetString("ConfirmTitle"),
                Width = 300,
                Height = 180,
                FormBorderStyle = FormBorderStyle.FixedDialog,

            };
            var label = new Label
            {
                Text = SR.GetString("ModalWindowLabel"),
                Location = new Point(20, 20),
                Size = new Size(250, 50)
            };
            var btnOK = new Button
            {
                Text = SR.GetString("OK"),
                Location = new Point(60, 90),
                Size = new Size(80, 30)
            };
            btnOK.Click += (_, _) => dlg.DialogResult = DialogResult.OK;
            var btnCancel = new Button
            {
                Text = SR.GetString("Cancel"),
                Location = new Point(150, 90),
                Size = new Size(80, 30)
            };
            btnCancel.Click += (_, _) => dlg.DialogResult = DialogResult.Cancel;
            dlg.Controls.Add(label);
            dlg.Controls.Add(btnOK);
            dlg.Controls.Add(btnCancel);
            var result = dlg.ShowDialog(_mainForm);
            if (result == DialogResult.OK)
                _statusLabel!.Text = SR.GetString("StatusModalConfirmed");
            else
                _statusLabel!.Text = SR.GetString("StatusModalCancelled");
        };

        modalGroup.Controls.Add(btnModalDialog);
        modalGroup.Controls.Add(btnModalConfirm);

        var styleGroup = new GroupBox
        {
            Text = SR.GetString("GroupWindowStyles"),
            Location = new Point(10, 140),
            Size = new Size(570, 80)
        };

        var btnFixedDialog = new Button
        {
            Text = SR.GetString("BtnOpenFixedDialog"),
            Location = new Point(10, 30),
            Size = new Size(250, 35)
        };
        btnFixedDialog.Click += (s, e) =>
        {
            var dlg = new Form
            {
                Title = SR.GetString("FixedDialogTitle"),
                Text = SR.GetString("FixedDialogTitle"),
                Width = 350,
                Height = 200,
                FormBorderStyle = FormBorderStyle.FixedDialog,

            };
            var label = new Label
            {
                Text = SR.GetString("ModalWindowLabel"),
                Location = new Point(20, 20),
                Size = new Size(300, 80)
            };
            dlg.Controls.Add(label);
            _statusLabel!.Text = SR.GetString("StatusFixedDialogOpened");
            dlg.ShowDialog(_mainForm);
            _statusLabel!.Text = SR.GetString("StatusModalClosed");
        };

        styleGroup.Controls.Add(btnFixedDialog);

        page.Controls.Add(modelessGroup);
        page.Controls.Add(modalGroup);
        page.Controls.Add(styleGroup);

        return page;
    }

    static TabPage CreateReportPage()
    {
        var page = new TabPage { Text = "Reports" };

        var report = new Report("Employee Report");
        report.PageSetup.SetPaperSize(ReportPaperSize.A4, landscape: true);
        report.PageSetup.SetMargins(1.5);

        var employees = new List<Person>
        {
            new(1, "Alice Wonder", "alice@example.com", "Active") { Salary = 85000m },
            new(2, "Bob Builder", "bob@example.com", "Active") { Salary = 72000m },
            new(3, "Charlie Brown", "charlie@example.com", "Inactive") { Salary = 0m },
            new(4, "Diana Prince", "diana@example.com", "Active") { Salary = 95000m },
            new(5, "Eve Adams", "eve@example.com", "Pending") { Salary = 68000m },
            new(6, "Frank Castle", "frank@example.com", "Active") { Salary = 78000m },
            new(7, "Grace Hopper", "grace@example.com", "Active") { Salary = 110000m },
            new(8, "Henry Ford", "henry@example.com", "Inactive") { Salary = 0m },
            new(9, "Ivy League", "ivy@example.com", "Pending") { Salary = 62000m },
            new(10, "Jack Sparrow", "jack@example.com", "Active") { Salary = 88000m },
            new(11, "Kate Bishop", "kate@example.com", "Active") { Salary = 74000m },
            new(12, "Leo Messi", "leo@example.com", "Active") { Salary = 120000m },
        };
        report.DataSource = employees;

        var titleFont = new Font("Arial", 18, FontStyle.Bold);
        var headerFont = new Font("Arial", 10, FontStyle.Bold);
        var dataFont = new Font("Arial", 9);

        report.PageHeader.Height = 2.0;
        report.PageHeader.BackColor = Color.FromArgb(230, 240, 255);
        report.PageHeader.Controls.Add(new ReportLabel
        {
            Text = "Employee Report",
            Left = 0, Top = 0.2, Width = 26.7, Height = 1.2,
            Font = titleFont, TextAlign = TextAlignment.Center
        });
        report.PageHeader.Controls.Add(new ReportLine
        {
            Left = 0, Top = 1.6, X2 = 26.7, Y2 = 1.6,
            LineWidth = 0.04, LineColor = Color.FromArgb(50, 80, 180)
        });

        var group = new ReportGroup("Status");
        group.Header.Height = 0.7;
        group.Header.BackColor = Color.FromArgb(200, 220, 255);
        group.Header.RepeatOnNewPage = true;
        group.Header.Controls.Add(new ReportTextBox
        {
            Expression = "Status: {Status}",
            Left = 0.5, Top = 0.1, Width = 10, Height = 0.5,
            Font = headerFont, ForeColor = Color.FromArgb(30, 60, 150)
        });
        group.Header.Controls.Add(new ReportLine
        {
            Left = 0.5, Top = 0.65, X2 = 26.2, Y2 = 0.65,
            LineWidth = 0.02, LineColor = Color.FromArgb(150, 180, 220)
        });
        group.Footer.Height = 0.4;
        group.Footer.Controls.Add(new ReportLine
        {
            Left = 0.5, Top = 0.2, X2 = 26.2, Y2 = 0.2,
            LineWidth = 0.02, LineColor = Color.FromArgb(150, 180, 220)
        });
        report.Groups.Add(group);

        report.Detail.Height = 0.55;
        report.Detail.Controls.Add(new ReportTextBox
        {
            DataField = "Id", Left = 0.3, Top = 0.05,
            Width = 2, Height = 0.45, Font = dataFont,
            TextAlign = TextAlignment.Right, Format = "{0:D3}"
        });
        report.Detail.Controls.Add(new ReportTextBox
        {
            DataField = "Name", Left = 3.0, Top = 0.05,
            Width = 7, Height = 0.45, Font = dataFont
        });
        report.Detail.Controls.Add(new ReportTextBox
        {
            DataField = "Email", Left = 10.5, Top = 0.05,
            Width = 8, Height = 0.45, Font = dataFont
        });
        report.Detail.Controls.Add(new ReportTextBox
        {
            DataField = "Salary", Left = 19.0, Top = 0.05,
            Width = 3.5, Height = 0.45, Font = dataFont,
            TextAlign = TextAlignment.Right, Format = "{0:N0} EUR"
        });
        report.Detail.Controls.Add(new ReportCheckBox
        {
            DataField = "IsActive", Left = 23.0, Top = 0.05,
            Width = 3, Height = 0.45
        });

        report.PageFooter.Height = 0.6;
        report.PageFooter.Controls.Add(new ReportLine
        {
            Left = 0, Top = 0.1, X2 = 26.7, Y2 = 0.1,
            LineWidth = 0.02, LineColor = Color.FromArgb(180, 180, 180)
        });
        report.PageFooter.Controls.Add(new ReportLabel
        {
            Text = "Confidential",
            Left = 0, Top = 0.2, Width = 8, Height = 0.35,
            Font = new Font("Arial", 7), ForeColor = Color.FromArgb(128, 128, 128)
        });
        report.PageFooter.Controls.Add(new ReportTextBox
        {
            Expression = "Page {PageNumber} / {TotalPages}",
            Left = 18, Top = 0.2, Width = 8.7, Height = 0.35,
            Font = new Font("Arial", 7), ForeColor = Color.FromArgb(128, 128, 128),
            TextAlign = TextAlignment.Right
        });

        var viewer = new ReportViewer
        {
            Dock = DockStyle.Fill,
            Report = report
        };
        viewer.RefreshReport();

        var refreshBtn = new Button
        {
            Text = "Refresh Report",
            Location = new Point(10, 10),
            Size = new Size(140, 30)
        };
        refreshBtn.Click += (_, _) =>
        {
            viewer.Report = report;
            viewer.RefreshReport();
            _statusLabel!.Text = "Report refreshed";
        };

        page.Controls.Add(viewer);

        return page;
    }
}

public class Person : INotifyPropertyChanged
{
    private int _id;
    private string _name;
    private string _email;
    private string _status;
    private bool _isActive;
    private DateTime _birthDate;
    private DateTime _startTime;

    public Person(int id, string name, string email, string status)
    {
        _id = id;
        _name = name;
        _email = email;
        _status = status;
        _isActive = status == "Active";
        _birthDate = new DateTime(1990, 1, 1);
        _startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 9, 0, 0);
    }

    public int Id
    {
        get => _id;
        set
        {
            if (_id != value) { _id = value; OnPropertyChanged(nameof(Id)); }
        }
    }

    public string Name
    {
        get => _name;
        set
        {
            if (_name != value) { _name = value; OnPropertyChanged(nameof(Name)); }
        }
    }

    public string Email
    {
        get => _email;
        set
        {
            if (_email != value) { _email = value; OnPropertyChanged(nameof(Email)); }
        }
    }

    public string Status
    {
        get => _status;
        set
        {
            if (_status != value) { _status = value; OnPropertyChanged(nameof(Status)); }
        }
    }

    public decimal Salary { get; set; }

    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive != value) { _isActive = value; OnPropertyChanged(nameof(IsActive)); }
        }
    }

    public DateTime BirthDate
    {
        get => _birthDate;
        set
        {
            if (_birthDate != value) { _birthDate = value; OnPropertyChanged(nameof(BirthDate)); }
        }
    }

    public DateTime StartTime
    {
        get => _startTime;
        set
        {
            if (_startTime != value) { _startTime = value; OnPropertyChanged(nameof(StartTime)); }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class LoginUserControl : UserControl
{
    private Label _usernameLabel = null!;
    private TextBox _usernameTextBox = null!;
    private Label _passwordLabel = null!;
    private TextBox _passwordTextBox = null!;
    private CheckBox _rememberCheckBox = null!;
    private Button _loginButton = null!;

    public string Username => _usernameTextBox.Text;
    public string Password => _passwordTextBox.Text;

    public event EventHandler? LoginClicked;

    public LoginUserControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        BackColor = ThemeManager.CurrentTheme.ControlBackground;
        BorderStyle = BorderStyle.FixedSingle;

        _usernameLabel = new Label { Text = SR.GetString("LabelUsername"), Location = new Point(10, 15), Size = new Size(80, 20) };
        _usernameTextBox = new TextBox { Location = new Point(100, 15), Size = new Size(230, 25), Text = SR.GetString("DefaultLoginUsername") };

        _passwordLabel = new Label { Text = SR.GetString("LabelPassword"), Location = new Point(10, 50), Size = new Size(80, 20) };
        _passwordTextBox = new TextBox { Location = new Point(100, 50), Size = new Size(230, 25), Text = SR.GetString("DefaultLoginPassword"), UseSystemPasswordChar = true };

        _rememberCheckBox = new CheckBox { Text = SR.GetString("ChkRememberMe"), Location = new Point(100, 80), Size = new Size(150, 20), Checked = true };

        _loginButton = new Button { Text = SR.GetString("BtnLogin"), Location = new Point(100, 115), Size = new Size(100, 30) };
        _loginButton.Click += (s, e) => LoginClicked?.Invoke(this, EventArgs.Empty);

        Controls.Add(_usernameLabel);
        Controls.Add(_usernameTextBox);
        Controls.Add(_passwordLabel);
        Controls.Add(_passwordTextBox);
        Controls.Add(_rememberCheckBox);
        Controls.Add(_loginButton);
    }
}
