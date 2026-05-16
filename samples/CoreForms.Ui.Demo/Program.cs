using System.ComponentModel;
using CoreForms.Ui.Controls.Advanced;
using CoreForms.Ui.Core;
using CoreForms.Ui.Controls.Basic;
using CoreForms.Ui.Controls.Containers;
using CoreForms.Ui.Controls.Separators;
using CoreForms.Ui.Controls;
using CoreForms.Ui.Data;
using CoreForms.Ui.WebBrowser.Controls;
using CoreForms.Ui.Theming;
using CoreForms.Ui.Resources;
using SkiaSharp;
using System.Collections.Generic;

namespace CoreForms.Ui.Demo;

class Program
{
    private static Label? _statusLabel;
    private static Controls.Advanced.DataGridView? _mainDataGrid;

    [STAThread]
    static void Main()
    {
        var form = new Form
        {
            Text = "CoreForms.Ui Demo",
            Width = 1200,
            Height = 900,
            Zoom = 1.25f
        };

        var menuStrip = CreateMenuStrip(form);
        var toolStrip = CreateToolStrip(form);
        var statusStrip = CreateStatusStrip();
        var mainTabControl = CreateMainTabControl();

        form.Controls.Add(menuStrip);
        form.Controls.Add(toolStrip);
        form.Controls.Add(statusStrip);
        form.Controls.Add(mainTabControl);

        _mainDataGrid = form.Controls.FindControl<Controls.Advanced.DataGridView>("dataGrid", recursive: true);
        var addButton = form.Controls.FindControl<Button>("addButton", recursive: true);
        addButton?.Click += (sender, args) =>
        {
            _mainDataGrid?.AddRow(_mainDataGrid.Rows.Count + 1, "John Doe", "john@example.com", "Active");
        };

        Application.Run(form);
    }

    static MenuStrip CreateMenuStrip(Form form)
    {
        var menuStrip = new MenuStrip();
        menuStrip.Dock = DockStyle.Top;
        menuStrip.Size = new Size(900, 30);

        var fileItem = new ToolStripMenuItem("&File");
        var fileNewItem = new ToolStripMenuItem("&New");
        fileNewItem.Click += (s, e) => MessageBox.Show("Create a new file?", "New File", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        var fileOpenItem = new ToolStripMenuItem("&Open");
        fileOpenItem.Click += (s, e) => MessageBox.Show("Open an existing file.", "Open", MessageBoxButtons.OK, MessageBoxIcon.Information);
        var fileSaveItem = new ToolStripMenuItem("&Save");
        fileSaveItem.Click += (s, e) => MessageBox.Show("File saved successfully!", "Save", MessageBoxButtons.OK, MessageBoxIcon.Information);
        var fileExitItem = new ToolStripMenuItem("E&xit");
        fileExitItem.Click += (s, e) =>
        {
            var result = MessageBox.Show("Are you sure you want to exit?", "Exit", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
                Application.Exit();
        };
        fileItem.DropDownItems.Add(fileNewItem);
        fileItem.DropDownItems.Add(fileOpenItem);
        fileItem.DropDownItems.Add(fileSaveItem);
        fileItem.DropDownItems.Add(fileExitItem);
        menuStrip.Items.Add(fileItem);

        var editItem = new ToolStripMenuItem("&Edit");
        var editUndoItem = new ToolStripMenuItem("&Undo");
        editUndoItem.Click += (s, e) => MessageBox.Show("Undo last action?", "Undo", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
        var editRedoItem = new ToolStripMenuItem("&Redo");
        editRedoItem.Click += (s, e) => MessageBox.Show("Redo last action?", "Redo", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
        var editDeleteItem = new ToolStripMenuItem("&Delete");
        editDeleteItem.Click += (s, e) => MessageBox.Show("Delete this item? This cannot be undone.", "Delete", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Error);
        var editSep1 = new ToolStripMenuItem("-");
        var editCutItem = new ToolStripMenuItem("Cu&t");
        editCutItem.Click += (s, e) => {
            if (form.ActiveControl is TextBox tb)
            {
                try { tb.Cut(); }
                catch (Exception ex) { MessageBox.Show($"Clipboard error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        };
        var editCopyItem = new ToolStripMenuItem("&Copy");
        editCopyItem.Click += (s, e) => {
            if (form.ActiveControl != null)
            {
                try {
                    var method = form.ActiveControl.GetType().GetMethod("CopyToClipboard");
                    method?.Invoke(form.ActiveControl, null);
                } catch (Exception ex) { MessageBox.Show($"Clipboard error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        };
        var editPasteItem = new ToolStripMenuItem("&Paste");
        editPasteItem.Click += (s, e) => {
            if (form.ActiveControl is TextBox tb)
            {
                try { tb.Paste(); }
                catch (Exception ex) { MessageBox.Show($"Clipboard error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        };
        var editSelectAllItem = new ToolStripMenuItem("Select &All");
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

        var viewItem = new ToolStripMenuItem("&View");
        var viewRefreshItem = new ToolStripMenuItem("&Refresh");
        viewRefreshItem.Click += (s, e) => MessageBox.Show("View refreshed.", "Refresh", MessageBoxButtons.OK, MessageBoxIcon.Information);
        var viewFullscreenItem = new ToolStripMenuItem("Fullscreen");
        viewFullscreenItem.Click += (s, e) => MessageBox.Show("Toggle fullscreen mode is not yet implemented.", "Fullscreen", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        viewItem.DropDownItems.Add(viewRefreshItem);
        viewItem.DropDownItems.Add(viewFullscreenItem);
        menuStrip.Items.Add(viewItem);

        var helpItem = new ToolStripMenuItem("&Help");
        var helpAboutItem = new ToolStripMenuItem("&About");
        helpAboutItem.Click += (s, e) => MessageBox.Show("CoreForms.Ui Demo\nVersion 1.0\n\nA cross-platform UI framework.", "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        var helpLicenseItem = new ToolStripMenuItem("License");
        helpLicenseItem.Click += (s, e) => MessageBox.Show("Retry loading the license?", "License Error", MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Error);
        helpItem.DropDownItems.Add(helpAboutItem);
        helpItem.DropDownItems.Add(helpLicenseItem);
        menuStrip.Items.Add(helpItem);

        return menuStrip;
    }

    static ToolStrip CreateToolStrip(Form form)
    {
        var toolStrip = new ToolStrip();
        toolStrip.Dock = DockStyle.Top;

        var newButton = new ToolStripButton("New", Icons.DocumentAdd24!);
        newButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
        newButton.Click += (s, e) => _statusLabel!.Text = "New clicked";
        toolStrip.Items.Add(newButton);

        var openButton = new ToolStripButton("Open", Icons.Document24!);
        openButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
        openButton.Click += (s, e) => _statusLabel!.Text = "Open clicked";
        toolStrip.Items.Add(openButton);

        var saveButton = new ToolStripButton("Save", Icons.DocumentEdit24!);
        saveButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
        saveButton.Click += (s, e) => _statusLabel!.Text = "Save clicked";
        toolStrip.Items.Add(saveButton);

        toolStrip.Items.Add(new ToolStripSeparator());

        var boldButton = new ToolStripButton("B");
        boldButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        boldButton.CheckOnClick = true;
        boldButton.CheckedChanged += (s, e) => _statusLabel!.Text = $"Bold: {boldButton.Checked}";
        toolStrip.Items.Add(boldButton);

        var italicButton = new ToolStripButton("I");
        italicButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        italicButton.CheckOnClick = true;
        italicButton.CheckedChanged += (s, e) => _statusLabel!.Text = $"Italic: {italicButton.Checked}";
        toolStrip.Items.Add(italicButton);

        toolStrip.Items.Add(new ToolStripSeparator());

        var searchBox = new ToolStripTextBox();
        searchBox.TextBoxWidth = 120;
        searchBox.TextChanged += (s, e) => _statusLabel!.Text = $"Search: {searchBox.Text}";
        toolStrip.Items.Add(searchBox);

        var searchButton = new ToolStripButton("Search", Icons.SearchSparkle24!);
        searchButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
        searchButton.Click += (s, e) => _statusLabel!.Text = $"Searching for: {searchBox.Text}";
        toolStrip.Items.Add(searchButton);

        toolStrip.Items.Add(new ToolStripSeparator());

        var zoomLabel = new ToolStripLabel("Zoom:");
        toolStrip.Items.Add(zoomLabel);

        var zoomComboBox = new ToolStripLabel("100%");
        zoomComboBox.IsLink = true;
        zoomComboBox.Click += (s, e) =>
        {
            form.Zoom = form.Zoom == 1.0f ? 1.5f : 1.0f;
            zoomComboBox.Text = $"{(int)(form.Zoom * 100)}%";
            _statusLabel!.Text = $"Zoom: {zoomComboBox.Text}";
        };
        toolStrip.Items.Add(zoomComboBox);

        toolStrip.Items.Add(new ToolStripSeparator());

        ToolStripButton? lightButton = null;
        ToolStripButton? darkButton = null;

        lightButton = new ToolStripButton("Light", Icons.WeatherSunnyLow24!);
        lightButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
        lightButton.Checked = true;
        lightButton.CheckOnClick = true;
        lightButton.CheckedChanged += (s, e) =>
        {
            if (lightButton.Checked)
            {
                ThemeManager.SetTheme(new LightTheme());
                darkButton!.Checked = false;
                _statusLabel!.Text = "Theme: Light";
            }
        };
        toolStrip.Items.Add(lightButton);

        darkButton = new ToolStripButton("Dark", Icons.WeatherSnowflake24!);
        darkButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
        darkButton.CheckOnClick = true;
        darkButton.CheckedChanged += (s, e) =>
        {
            if (darkButton.Checked)
            {
                ThemeManager.SetTheme(new DarkTheme());
                lightButton!.Checked = false;
                _statusLabel!.Text = "Theme: Dark";
            }
        };
        toolStrip.Items.Add(darkButton);

        toolStrip.Items.Add(new ToolStripSeparator());

        var toggleEnabledButton = new ToolStripButton("Toggle Enabled");
        toggleEnabledButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
        toggleEnabledButton.Click += (s, e) =>
        {
            ToggleControlsEnabled(form, toolStrip);
            _statusLabel!.Text = "Controls enabled state toggled";
        };
        toolStrip.Items.Add(toggleEnabledButton);

        toolStrip.Items.Add(new ToolStripSeparator());

        var helpButton = new ToolStripButton("Help", Icons.QuestionCircle24!);
        helpButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
        helpButton.Click += (s, e) => MessageBox.Show("CoreForms.Ui ToolStrip Demo\n\nDemonstrates all ToolStrip item types.", "Help", MessageBoxButtons.OK, MessageBoxIcon.Information);
        toolStrip.Items.Add(helpButton);

        return toolStrip;
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
        _statusLabel = new Label { Text = "Ready" };

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
        tabControl.AddTabPage(CreateSplitPanelPage());
        tabControl.AddTabPage(CreateWebBrowserPage());
        tabControl.AddTabPage(CreateHtmlEditorPage());

        return tabControl;
    }

    static TabPage CreateBasicControlsPage()
    {
        var page = new TabPage { Text = "Basic Controls" };

        var groupBox1 = new GroupBox
        {
            Text = "Text Input",
            Location = new Point(10, 10),
            Size = new Size(300, 160)
        };

        var nameLabel = new Label { Text = "Name:", Location = new Point(10, 25), Size = new Size(70, 20) };
        var nameTextBox = new TextBox { Location = new Point(90, 25), Size = new Size(180, 25), Text = "John Doe" };
        var emailLabel = new Label { Text = "Email:", Location = new Point(10, 55), Size = new Size(70, 20) };
        var emailTextBox = new TextBox { Location = new Point(90, 55), Size = new Size(180, 25), Text = "john@example.com" };
        var passwordLabel = new Label { Text = "Password:", Location = new Point(10, 85), Size = new Size(70, 20) };
        var passwordTextBox = new TextBox { Location = new Point(90, 85), Size = new Size(180, 25), Text = "secret", UseSystemPasswordChar = true };
        var multiLineLabel = new Label { Text = "Multi-line:", Location = new Point(10, 115), Size = new Size(70, 20) };
        var multiLineTextBox = new TextBox { Location = new Point(90, 115), Size = new Size(180, 35), Text = "Multi-line text" };

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
            Text = "Selection",
            Location = new Point(320, 10),
            Size = new Size(250, 160)
        };

        var genderLabel = new Label { Text = "Gender:", Location = new Point(10, 25), Size = new Size(100, 20) };
        var maleRadio = new RadioButton { Text = "Male", Location = new Point(10, 50), Size = new Size(100, 20), Checked = true };
        var femaleRadio = new RadioButton { Text = "Female", Location = new Point(10, 75), Size = new Size(100, 20) };
        var otherRadio = new RadioButton { Text = "Other", Location = new Point(10, 100), Size = new Size(100, 20) };

        var roleLabel = new Label { Text = "Role:", Location = new Point(120, 25), Size = new Size(80, 20) };
        var adminRadio = new RadioButton { Text = "Admin", Location = new Point(120, 50), Size = new Size(80, 20) };
        var userRadio = new RadioButton { Text = "User", Location = new Point(120, 75), Size = new Size(80, 20), Checked = true };
        var guestRadio = new RadioButton { Text = "Guest", Location = new Point(120, 100), Size = new Size(80, 20) };

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
            Text = "Lists & Combo Boxes",
            Location = new Point(10, 180),
            Size = new Size(280, 200)
        };

        var listBoxLabel = new Label { Text = "ListBox:", Location = new Point(10, 25), Size = new Size(70, 20) };
        var listBox = new ListBox { Location = new Point(10, 45), Size = new Size(120, 120) };
        listBox.Items.Add("Item 1");
        listBox.Items.Add("Item 2");
        listBox.Items.Add("Item 3");
        listBox.Items.Add("Item 4");
        listBox.Items.Add("Item 5");
        listBox.SelectedIndexChanged += (s, e) => _statusLabel!.Text = $"ListBox: {listBox.SelectedItem}";

        var comboBoxLabel = new Label { Text = "ComboBox:", Location = new Point(140, 25), Size = new Size(80, 20) };
        var comboBox = new ComboBox { Location = new Point(140, 45), Size = new Size(120, 24) };
        comboBox.Items.Add("Active");
        comboBox.Items.Add("Inactive");
        comboBox.Items.Add("Pending");
        comboBox.SelectedIndexChanged += (s, e) => _statusLabel!.Text = $"ComboBox: {comboBox.SelectedItem}";

        var checkBox1 = new CheckBox { Text = "Option A", Location = new Point(140, 80), Size = new Size(100, 25) };
        var checkBox2 = new CheckBox { Text = "Option B", Location = new Point(140, 105), Size = new Size(100, 25), Checked = true };
        var checkBox3 = new CheckBox { Text = "Option C", Location = new Point(140, 130), Size = new Size(100, 25) };

        groupBox3.Controls.Add(listBoxLabel);
        groupBox3.Controls.Add(listBox);
        groupBox3.Controls.Add(comboBoxLabel);
        groupBox3.Controls.Add(comboBox);
        groupBox3.Controls.Add(checkBox1);
        groupBox3.Controls.Add(checkBox2);
        groupBox3.Controls.Add(checkBox3);

        var groupBox4 = new GroupBox
        {
            Text = "Progress & Buttons",
            Location = new Point(300, 180),
            Size = new Size(270, 200)
        };

        var progressBar = new ProgressBar { Location = new Point(10, 25), Size = new Size(250, 20), Value = 60 };

        var progressButton = new Button { Text = "Progress +10", Location = new Point(10, 55), Size = new Size(120, 30) };
        progressButton.Click += (s, e) => { if (progressBar.Value < 100) progressBar.Value += 10; };

        var resetProgressButton = new Button { Text = "Reset", Location = new Point(140, 55), Size = new Size(120, 30) };
        resetProgressButton.Click += (s, e) => progressBar.Value = 0;

        var testButton = new Button { Text = "Test Button", Location = new Point(10, 95), Size = new Size(120, 30) };
        testButton.Click += (s, e) => _statusLabel!.Text = "Button clicked!";

        var disabledButton = new Button { Text = "Disabled", Location = new Point(140, 95), Size = new Size(120, 30), Enabled = false };

        var spinnerLabel = new Label { Text = "Spinner:", Location = new Point(10, 140), Size = new Size(50, 20) };
        var spinner = new Spinner { Location = new Point(60, 132), Size = new Size(36, 36), Active = true, AutoStart = false };
        var spinnerButton = new Button { Text = "Toggle", Location = new Point(110, 135), Size = new Size(140, 28) };
        spinnerButton.Click += (s, e) => spinner.Active = !spinner.Active;

        groupBox4.Controls.Add(progressBar);
        groupBox4.Controls.Add(progressButton);
        groupBox4.Controls.Add(resetProgressButton);
        groupBox4.Controls.Add(testButton);
        groupBox4.Controls.Add(disabledButton);
        groupBox4.Controls.Add(spinnerLabel);
        groupBox4.Controls.Add(spinner);
        groupBox4.Controls.Add(spinnerButton);

        page.Controls.Add(groupBox1);
        page.Controls.Add(groupBox2);
        page.Controls.Add(groupBox3);
        page.Controls.Add(groupBox4);

        return page;
    }

    private static BindingList<Person>? _personList;

    static TabPage CreateDataGridPage()
    {
        var page = new TabPage { Text = "Data Grid" };

        var dataGrid = new Controls.Advanced.DataGridView
        {
            Location = new Point(10, 10),
            Size = new Size(550, 250),
            ColumnHeadersVisible = true,
            RowHeadersVisible = true,
            Name = "dataGrid"
        };

        dataGrid.Columns.Add(new Controls.Advanced.DataGridViewColumn
            { HeaderText = "ID", Width = 50, Name = "Id", DataPropertyName = "Id" });
        dataGrid.Columns.Add(new Controls.Advanced.DataGridViewColumn
            { HeaderText = "Name", Width = 150, Name = "Name", DataPropertyName = "Name" });
        dataGrid.Columns.Add(new Controls.Advanced.DataGridViewColumn
            { HeaderText = "Email", Width = 200, Name = "Email", DataPropertyName = "Email" });
        dataGrid.Columns.Add(new Controls.Advanced.DataGridViewColumn
            { HeaderText = "Status", Width = 100, Name = "Status", DataPropertyName = "Status" });

        _personList = new BindingList<Person>
        {
            new Person(1, "John Doe", "john@example.com", "Active"),
            new Person(2, "Jane Smith", "jane@example.com", "Active"),
            new Person(3, "Bob Johnson", "bob@example.com", "Inactive"),
            new Person(4, "Alice Brown", "alice@example.com", "Active"),
            new Person(5, "Charlie Wilson", "charlie@example.com", "Pending")
        };
        dataGrid.DataSource = _personList;

        var addButton = new Button { Text = "Add Row", Location = new Point(10, 270), Size = new Size(130, 30), Name = "addButton" };
        addButton.Click += (s, e) =>
        {
            var nextId = (_personList.Count > 0 ? _personList[^1].Id : 0) + 1;
            _personList.Add(new Person(nextId, "New Person", "new@example.com", "Active"));
        };

        var removeButton = new Button { Text = "Remove Last", Location = new Point(150, 270), Size = new Size(130, 30) };
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
        var page = new TabPage { Text = "Data Binding" };

        // ===== Data source =====
        var contacts = new BindingList<Person>
        {
            new Person(1, "Alice Wonder", "alice@example.com", "Active"),
            new Person(2, "Bob Builder", "bob@example.com", "Active"),
            new Person(3, "Charlie Brown", "charlie@example.com", "Inactive")
        };
        var bindingSource = new BindingSource(contacts);

        // ===== Left: List + Navigation =====
        var listGroup = new GroupBox
        {
            Text = "Contact List (BindingSource)",
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

        var firstButton = new Button { Text = "|<", Location = new Point(0, 0), Size = new Size(45, 28) };
        var prevButton = new Button { Text = "<", Location = new Point(50, 0), Size = new Size(45, 28) };
        var nextButton = new Button { Text = ">", Location = new Point(100, 0), Size = new Size(45, 28) };
        var lastButton = new Button { Text = ">|", Location = new Point(150, 0), Size = new Size(45, 28) };

        firstButton.Click += (s, e) => bindingSource.MoveFirst();
        prevButton.Click += (s, e) => bindingSource.MovePrevious();
        nextButton.Click += (s, e) => bindingSource.MoveNext();
        lastButton.Click += (s, e) => bindingSource.MoveLast();

        var addPersonButton = new Button
        {
            Text = "Add Contact",
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
            Text = "Remove",
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
            Text = "Position: 0 / 0",
            Location = new Point(0, 70),
            Size = new Size(195, 20)
        };

        bindingSource.PositionChanged += (s, e) =>
        {
            positionLabel.Text = $"Position: {bindingSource.Position + 1} / {bindingSource.Count}";
        };
        positionLabel.Text = $"Position: 1 / {contacts.Count}";

        navPanel.Controls.Add(firstButton);
        navPanel.Controls.Add(prevButton);
        navPanel.Controls.Add(nextButton);
        navPanel.Controls.Add(lastButton);
        navPanel.Controls.Add(addPersonButton);
        navPanel.Controls.Add(removePersonButton);
        navPanel.Controls.Add(positionLabel);

        listGroup.Controls.Add(contactListBox);
        listGroup.Controls.Add(navPanel);

        // ===== Right: Detail editing with Bindings =====
        var detailGroup = new GroupBox
        {
            Text = "Contact Details (DataBindings)",
            Location = new Point(240, 10),
            Size = new Size(350, 350)
        };

        var idLabel = new Label { Text = "ID:", Location = new Point(10, 25), Size = new Size(60, 20) };
        var idValue = new Label { Text = "", Location = new Point(80, 25), Size = new Size(60, 20) };

        var nameLabel = new Label { Text = "Name:", Location = new Point(10, 55), Size = new Size(60, 20) };
        var nameTextBox = new TextBox { Location = new Point(80, 55), Size = new Size(250, 25) };

        var emailLabel = new Label { Text = "Email:", Location = new Point(10, 90), Size = new Size(60, 20) };
        var emailTextBox = new TextBox { Location = new Point(80, 90), Size = new Size(250, 25) };

        var statusLabel_ = new Label { Text = "Status:", Location = new Point(10, 125), Size = new Size(60, 20) };
        var statusComboBox = new ComboBox { Location = new Point(80, 125), Size = new Size(150, 25) };
        statusComboBox.Items.Add("Active");
        statusComboBox.Items.Add("Inactive");
        statusComboBox.Items.Add("Pending");

        var isActiveCheckBox = new CheckBox
        {
            Text = "Is Active",
            Location = new Point(10, 165),
            Size = new Size(120, 25)
        };

        var feedbackLabel = new Label
        {
            Text = "Bindings push changes automatically.",
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

        // ===== Bottom: ComboBox + Live Preview =====
        var comboGroup = new GroupBox
        {
            Text = "ComboBox DataSource",
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
            Text = "SelectedValue: -",
            Location = new Point(10, 60),
            Size = new Size(225, 20)
        };
        contactCombo.SelectedIndexChanged += (s, e) =>
        {
            selectedValueLabel.Text = $"SelectedValue: {contactCombo.SelectedValue}";
        };

        comboGroup.Controls.Add(contactCombo);
        comboGroup.Controls.Add(selectedValueLabel);

        // ===== Bindings (after controls created) =====
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

        page.Controls.Add(listGroup);
        page.Controls.Add(detailGroup);
        page.Controls.Add(comboGroup);

        return page;
    }

    static TabPage CreateMessageBoxPage()
    {
        var page = new TabPage { Text = "Message Boxes" };

        var infoButton = new Button { Text = "Information", Location = new Point(10, 10), Size = new Size(150, 35) };
        infoButton.Click += (s, e) =>
        {
            var result = MessageBox.Show("This is an information message.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _statusLabel!.Text = $"Result: {result}";
        };

        var warningButton = new Button { Text = "Warning", Location = new Point(170, 10), Size = new Size(150, 35) };
        warningButton.Click += (s, e) =>
        {
            var result = MessageBox.Show("Attention! A warning has been triggered.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _statusLabel!.Text = $"Result: {result}";
        };

        var errorButton = new Button { Text = "Error", Location = new Point(330, 10), Size = new Size(150, 35) };
        errorButton.Click += (s, e) =>
        {
            var result = MessageBox.Show("An error has occurred!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _statusLabel!.Text = $"Result: {result}";
        };

        var questionButton = new Button { Text = "Question (Yes/No)", Location = new Point(10, 55), Size = new Size(150, 35) };
        questionButton.Click += (s, e) =>
        {
            var result = MessageBox.Show("Do you want to continue?", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            _statusLabel!.Text = $"Result: {result}";
        };

        var okCancelButton = new Button { Text = "OK / Cancel", Location = new Point(170, 55), Size = new Size(150, 35) };
        okCancelButton.Click += (s, e) =>
        {
            var result = MessageBox.Show("Confirm or cancel the operation.", "Confirmation", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
            _statusLabel!.Text = $"Result: {result}";
        };

        var yesNoCancelButton = new Button { Text = "Yes / No / Cancel", Location = new Point(330, 55), Size = new Size(180, 35) };
        yesNoCancelButton.Click += (s, e) =>
        {
            var result = MessageBox.Show("Save changes?", "Save", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
            _statusLabel!.Text = $"Result: {result}";
        };

        var retryButton = new Button { Text = "Retry / Cancel", Location = new Point(10, 100), Size = new Size(180, 35) };
        retryButton.Click += (s, e) =>
        {
            var result = MessageBox.Show("Connection failed. Try again?", "Connection", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
            _statusLabel!.Text = $"Result: {result}";
        };

        var abortButton = new Button { Text = "Abort / Retry / Ignore", Location = new Point(200, 100), Size = new Size(250, 35) };
        abortButton.Click += (s, e) =>
        {
            var result = MessageBox.Show("The operation can be aborted, retried, or ignored.", "Operation", MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Error);
            _statusLabel!.Text = $"Result: {result}";
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
        var page = new TabPage { Text = "File Dialogs" };

        // D-Bus diagnostic button (Linux only)
        var dbusTestButton = new Button
        {
            Text = "Test D-Bus Connection",
            Location = new Point(10, 10),
            Size = new Size(180, 30)
        };
        dbusTestButton.Click += (s, e) =>
        {
            _statusLabel!.Text = "Click Open File to test D-Bus portal dialog";
        };

        var groupBox = new GroupBox
        {
            Text = "OpenFileDialog",
            Location = new Point(10, 50),
            Size = new Size(350, 250)
        };

        var openSingleButton = new Button { Text = "Open File...", Location = new Point(10, 25), Size = new Size(150, 35) };
        openSingleButton.Click += (s, e) =>
        {
            var dlg = new OpenFileDialog
            {
                Title = "Select a text file",
                Filter = "Text files|*.txt|All files|*.*",
                FilterIndex = 1,
                CheckFileExists = true
            };
            var result = dlg.ShowDialog();
            _statusLabel!.Text = result == DialogResult.OK
                ? $"Open: {dlg.FileName}"
                : "Open cancelled";
        };

        var openMultiButton = new Button { Text = "Open Files (Multi)...", Location = new Point(10, 70), Size = new Size(150, 35) };
        openMultiButton.Click += (s, e) =>
        {
            var dlg = new OpenFileDialog
            {
                Title = "Select one or more files",
                Filter = "All files|*.*|C# files|*.cs|Text files|*.txt",
                Multiselect = true,
                CheckFileExists = true
            };
            var result = dlg.ShowDialog();
            if (result == DialogResult.OK)
            {
                _statusLabel!.Text = $"Open: {string.Join("; ", dlg.FileNames)}";
            }
            else
            {
                _statusLabel!.Text = "Open cancelled";
            }
        };

        var openWithInitialDirButton = new Button { Text = "Open from /tmp...", Location = new Point(10, 115), Size = new Size(150, 35) };
        openWithInitialDirButton.Click += (s, e) =>
        {
            var dlg = new OpenFileDialog
            {
                Title = "Open from temp directory",
                InitialDirectory = "/tmp",
                Filter = "All files|*.*",
                DefaultExt = "txt",
                AddExtension = true
            };
            var result = dlg.ShowDialog();
            _statusLabel!.Text = result == DialogResult.OK
                ? $"Open: {dlg.FileName}"
                : "Open cancelled";
        };

        var resultLabel = new Label
        {
            Text = "Result will appear in the status bar.",
            Location = new Point(10, 170),
            Size = new Size(320, 50)
        };

        groupBox.Controls.Add(openSingleButton);
        groupBox.Controls.Add(openMultiButton);
        groupBox.Controls.Add(openWithInitialDirButton);
        groupBox.Controls.Add(resultLabel);

        var saveGroupBox = new GroupBox
        {
            Text = "SaveFileDialog",
            Location = new Point(370, 50),
            Size = new Size(350, 250)
        };

        var saveButton = new Button { Text = "Save File...", Location = new Point(10, 25), Size = new Size(150, 35) };
        saveButton.Click += (s, e) =>
        {
            var dlg = new SaveFileDialog
            {
                Title = "Save file as",
                Filter = "Text files|*.txt|All files|*.*",
                DefaultExt = "txt",
                AddExtension = true,
                OverwritePrompt = true,
                FileName = "document.txt"
            };
            var result = dlg.ShowDialog();
            _statusLabel!.Text = result == DialogResult.OK
                ? $"Save: {dlg.FileName}"
                : "Save cancelled";
        };

        var saveWithDirButton = new Button { Text = "Save to /tmp...", Location = new Point(10, 70), Size = new Size(150, 35) };
        saveWithDirButton.Click += (s, e) =>
        {
            var dlg = new SaveFileDialog
            {
                Title = "Save to temp directory",
                InitialDirectory = "/tmp",
                Filter = "C# files|*.cs|All files|*.*",
                DefaultExt = "cs",
                AddExtension = true,
                OverwritePrompt = true,
                FileName = "output.cs"
            };
            var result = dlg.ShowDialog();
            _statusLabel!.Text = result == DialogResult.OK
                ? $"Save: {dlg.FileName}"
                : "Save cancelled";
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
        var page = new TabPage { Text = "Print Dialog" };

        var printerListBox = new ListBox
        {
            Location = new Point(170, 10),
            Size = new Size(250, 200)
        };

        var setupButton = new Button { Text = "Print Setup...", Location = new Point(10, 10), Size = new Size(150, 35) };
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
                _statusLabel!.Text = $"Printer: {ps.PrinterName}, Copies: {ps.Copies}, Range: {ps.PrintRange}";
            }
            else
            {
                _statusLabel!.Text = "Print cancelled";
            }
        };

        var listButton = new Button { Text = "Printer List", Location = new Point(10, 55), Size = new Size(150, 35) };
        listButton.Click += (s, e) =>
        {
            printerListBox.Items.Clear();
            var printers = PrinterSettings.InstalledPrinters;
            foreach (var p in printers)
                printerListBox.Items.Add(p);
            _statusLabel!.Text = $"{printers.Length} printers found";
        };

        var testPrintButton = new Button { Text = "Quick Print Test", Location = new Point(10, 100), Size = new Size(150, 35) };
        testPrintButton.Click += (s, e) =>
        {
            var doc = new PrintDocument
            {
                DocumentName = "Test Page",
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

                g.DrawString("CoreForms.Ui Test Page", new Core.Font("Arial", 24), Core.Color.Black,
                    bounds.X + 10, bounds.Y + 10);

                g.DrawString($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", new Core.Font("Arial", 12), Core.Color.Black,
                    bounds.X + 10, bounds.Y + 60);

                g.DrawString("This is a test print page.", new Core.Font("Arial", 12), Core.Color.Black,
                    bounds.X + 10, bounds.Y + 100);

                args.HasMorePages = false;
            };
            try
            {
                doc.Print();
                _statusLabel!.Text = "Test page sent to printer";
            }
            catch (Exception ex)
            {
                _statusLabel!.Text = $"Print error: {ex.Message}";
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
        var page = new TabPage { Text = "Dock & Anchor" };

        var dockPanel = new Panel
        {
            Location = new Point(10, 10),
            Size = new Size(250, 300),
            BorderStyle = BorderStyle.FixedSingle
        };

        var dockedTopLabel = new Label { Text = "Dock=Top", Size = new Size(250, 22) };
        dockedTopLabel.Dock = DockStyle.Top;

        var hSep = new SeperatorControl
        {
            Orientation = SeperatorOrientation.Horizontal,
            Size = new Size(250, 2),
            Dock = DockStyle.Top
        };

        var dockedBottomLabel = new Label { Text = "Dock=Bottom", Size = new Size(250, 22) };
        dockedBottomLabel.Dock = DockStyle.Bottom;

        var dockedLeftLabel = new Label { Text = "L", Size = new Size(30, 176) };
        dockedLeftLabel.Dock = DockStyle.Left;

        var vSep = new SeperatorControl
        {
            Orientation = SeperatorOrientation.Vertical,
            Size = new Size(2, 176),
            Dock = DockStyle.Left
        };

        var centerLabel = new Label { Text = "Dock=Fill", Size = new Size(50, 50) };
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
            Text = "Anchored to all sides (Anchor=All)",
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
        var page = new TabPage { Text = "Tree View" };

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
        var root1 = new TreeNode("Root 1") { ImageIndex = 0 };
        var child1 = new TreeNode("Child 1") { ImageIndex = 1 };
        var child2 = new TreeNode("Child 2") { ImageIndex = 2 };
        root1.Add(child1);
        root1.Add(child2);

        var root2 = new TreeNode("Root 2") { ImageIndex = 0 };
        var subRoot = new TreeNode("Sub Root") { ImageIndex = 1 };
        subRoot.Add(new TreeNode("Leaf A") { ImageIndex = 2 });
        subRoot.Add(new TreeNode("Leaf B") { ImageIndex = 2 });
        root2.Add(subRoot);

        treeView.Nodes.Add(root1);
        treeView.Nodes.Add(root2);

        page.Controls.Add(treeView);
        return page;
    }

    static TabPage CreateUserControlPage()
    {
        var page = new TabPage { Text = "User Control" };

        var loginControl = new LoginUserControl
        {
            Location = new Point(10, 10),
            Size = new Size(350, 200)
        };
        loginControl.LoginClicked += (s, e) =>
        {
            _statusLabel!.Text = $"Login: {loginControl.Username}";
        };

        page.Controls.Add(loginControl);
        return page;
    }

    static TabPage CreateImagesPage()
    {
        var page = new TabPage { Text = "Images" };

        var svgGroup = new GroupBox
        {
            Text = "SVG Images",
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

        var svgLabel = new Label { Text = "Zoom | Stretch | Center", Location = new Point(15, 175), Size = new Size(210, 20) };

        svgGroup.Controls.Add(svgPictureBox);
        svgGroup.Controls.Add(svgStretchBox);
        svgGroup.Controls.Add(svgCenterBox);
        svgGroup.Controls.Add(svgLabel);

        var rasterGroup = new GroupBox
        {
            Text = "Raster Images (PNG)",
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

        var rasterLabel = new Label { Text = "Normal | Stretch | Zoom", Location = new Point(15, 175), Size = new Size(210, 20) };

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
        var page = new TabPage { Text = "Split Panel" };

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
            Text = "Top Panel (Panel1)",
            Location = new Point(10, 10),
            Size = new Size(100, 30)
        };
        var topButton = new Button
        {
            Text = "Button in Top",
            Location = new Point(10, 50),
            Size = new Size(120, 25)
        };
        topButton.Click += (s, e) => _statusLabel!.Text = "Top button clicked!";
        verticalSplit.Panel1.Controls.Add(topLabel);
        verticalSplit.Panel1.Controls.Add(topButton);

        var bottomTextBox = new TextBox
        {
            Text = "Bottom Panel (Panel2)",
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
            Text = "Left Panel (Panel1)",
            Location = new Point(10, 10),
            Size = new Size(100, 30)
        };
        horizontalSplit.Panel1.Controls.Add(leftLabel);

        var rightLabel = new Label
        {
            Text = "Right Panel (Panel2)",
            Location = new Point(10, 10),
            Size = new Size(140, 30)
        };
        var rightButton = new Button
        {
            Text = "Button in Right",
            Location = new Point(10, 50),
            Size = new Size(120, 25)
        };
        rightButton.Click += (s, e) => _statusLabel!.Text = "Right button clicked!";
        horizontalSplit.Panel2.Controls.Add(rightLabel);
        horizontalSplit.Panel2.Controls.Add(rightButton);

        var distanceLabel = new Label
        {
            Text = $"SplitterDist: {verticalSplit.SplitterDistance}",
            Location = new Point(10, 275),
            Size = new Size(200, 20)
        };

        var set200Button = new Button
        {
            Text = "Set Dist 200",
            Location = new Point(10, 300),
            Size = new Size(120, 25)
        };
        set200Button.Click += (s, e) =>
        {
            verticalSplit.SplitterDistance = 200;
            distanceLabel.Text = $"SplitterDist: {verticalSplit.SplitterDistance}";
            _statusLabel!.Text = "Splitter distance set to 200";
        };

        var toggleOrientationButton = new Button
        {
            Text = "Toggle Orientation",
            Location = new Point(140, 300),
            Size = new Size(150, 25)
        };
        toggleOrientationButton.Click += (s, e) =>
        {
            horizontalSplit.Orientation = horizontalSplit.Orientation == SplitOrientation.Horizontal
                ? SplitOrientation.Vertical
                : SplitOrientation.Horizontal;
            _statusLabel!.Text = $"Orientation: {horizontalSplit.Orientation}";
        };

        var incMinSizeButton = new Button
        {
            Text = "Panel1 Min +10",
            Location = new Point(10, 335),
            Size = new Size(120, 25)
        };
        incMinSizeButton.Click += (s, e) =>
        {
            verticalSplit.Panel1MinSize += 10;
            _statusLabel!.Text = $"Panel1MinSize: {verticalSplit.Panel1MinSize}";
        };

        verticalSplit.SplitterMoved += (s, e) =>
        {
            distanceLabel.Text = $"SplitterDist: {verticalSplit.SplitterDistance}";
            _statusLabel!.Text = $"Splitter moved to {verticalSplit.SplitterDistance}";
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
        var page = new TabPage { Text = "Web Browser" };

        var toolStrip = new ToolStrip { Dock = DockStyle.Top };

        // Navigation buttons
        var backButton = new ToolStripButton("← Back");
        var forwardButton = new ToolStripButton("Forward →");
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
        var page = new TabPage { Text = "HTML Editor" };

        var toolStrip = new ToolStrip { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden };

        var boldButton = new ToolStripButton("B", Icons.TextEditStyle24!) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText };
        var italicButton = new ToolStripButton("I") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        var underlineButton = new ToolStripButton("U") { DisplayStyle = ToolStripItemDisplayStyle.Text };
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
        htmlBox.ContentChanged += (s, e) => UpdateFormatButtons();

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
}

public class Person : INotifyPropertyChanged
{
    private int _id;
    private string _name;
    private string _email;
    private string _status;
    private bool _isActive;

    public Person(int id, string name, string email, string status)
    {
        _id = id;
        _name = name;
        _email = email;
        _status = status;
        _isActive = status == "Active";
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

    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive != value) { _isActive = value; OnPropertyChanged(nameof(IsActive)); }
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

        _usernameLabel = new Label { Text = "Username:", Location = new Point(10, 15), Size = new Size(80, 20) };
        _usernameTextBox = new TextBox { Location = new Point(100, 15), Size = new Size(230, 25), Text = "admin" };

        _passwordLabel = new Label { Text = "Password:", Location = new Point(10, 50), Size = new Size(80, 20) };
        _passwordTextBox = new TextBox { Location = new Point(100, 50), Size = new Size(230, 25), Text = "password", UseSystemPasswordChar = true };

        _rememberCheckBox = new CheckBox { Text = "Remember me", Location = new Point(100, 80), Size = new Size(150, 20), Checked = true };

        _loginButton = new Button { Text = "Login", Location = new Point(100, 115), Size = new Size(100, 30) };
        _loginButton.Click += (s, e) => LoginClicked?.Invoke(this, EventArgs.Empty);

        Controls.Add(_usernameLabel);
        Controls.Add(_usernameTextBox);
        Controls.Add(_passwordLabel);
        Controls.Add(_passwordTextBox);
        Controls.Add(_rememberCheckBox);
        Controls.Add(_loginButton);
    }
}
