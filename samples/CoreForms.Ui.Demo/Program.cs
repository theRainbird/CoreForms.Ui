using CoreForms.Ui.Controls.Advanced;
using CoreForms.Ui.Core;
using CoreForms.Ui.Controls.Basic;
using CoreForms.Ui.Controls.Containers;
using CoreForms.Ui.Controls;
using CoreForms.Ui.Resources;
using CoreForms.Ui.Theming;
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

        var helpButton = new ToolStripButton("Help", Icons.QuestionCircle24!);
        helpButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
        helpButton.Click += (s, e) => MessageBox.Show("CoreForms.Ui ToolStrip Demo\n\nDemonstrates all ToolStrip item types.", "Help", MessageBoxButtons.OK, MessageBoxIcon.Information);
        toolStrip.Items.Add(helpButton);

        return toolStrip;
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
        tabControl.AddTabPage(CreateMessageBoxPage());
        tabControl.AddTabPage(CreateDockAnchorPage());
        tabControl.AddTabPage(CreateTreeViewPage());

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

        groupBox4.Controls.Add(progressBar);
        groupBox4.Controls.Add(progressButton);
        groupBox4.Controls.Add(resetProgressButton);
        groupBox4.Controls.Add(testButton);
        groupBox4.Controls.Add(disabledButton);

        page.Controls.Add(groupBox1);
        page.Controls.Add(groupBox2);
        page.Controls.Add(groupBox3);
        page.Controls.Add(groupBox4);

        return page;
    }

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
        dataGrid.Columns.Add(new Controls.Advanced.DataGridViewColumn { HeaderText = "ID", Width = 50, Name = "Id" });
        dataGrid.Columns.Add(new Controls.Advanced.DataGridViewColumn { HeaderText = "Name", Width = 150, Name = "Name" });
        dataGrid.Columns.Add(new Controls.Advanced.DataGridViewColumn { HeaderText = "Email", Width = 200, Name = "Email" });
        dataGrid.Columns.Add(new Controls.Advanced.DataGridViewColumn { HeaderText = "Status", Width = 100, Name = "Status" });
        dataGrid.AddRow(1, "John Doe", "john@example.com", "Active");
        dataGrid.AddRow(2, "Jane Smith", "jane@example.com", "Active");
        dataGrid.AddRow(3, "Bob Johnson", "bob@example.com", "Inactive");
        dataGrid.AddRow(4, "Alice Brown", "alice@example.com", "Active");
        dataGrid.AddRow(5, "Charlie Wilson", "charlie@example.com", "Pending");

        var addButton = new Button { Text = "Add Row", Location = new Point(10, 270), Size = new Size(130, 30), Name = "addButton" };
        var removeButton = new Button { Text = "Remove Row", Location = new Point(150, 270), Size = new Size(130, 30) };
        removeButton.Click += (s, e) =>
        {
            if (dataGrid.Rows.Count > 0)
                dataGrid.Rows.Remove(dataGrid.Rows[dataGrid.Rows.Count - 1]);
        };

        page.Controls.Add(dataGrid);
        page.Controls.Add(addButton);
        page.Controls.Add(removeButton);

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

        var dockedBottomLabel = new Label { Text = "Dock=Bottom", Size = new Size(250, 22) };
        dockedBottomLabel.Dock = DockStyle.Bottom;

        var dockedLeftLabel = new Label { Text = "L", Size = new Size(30, 176) };
        dockedLeftLabel.Dock = DockStyle.Left;

        var centerLabel = new Label { Text = "Dock=Fill", Size = new Size(50, 50) };
        centerLabel.Dock = DockStyle.Fill;

        dockPanel.Controls.Add(dockedTopLabel);
        dockPanel.Controls.Add(dockedBottomLabel);
        dockPanel.Controls.Add(dockedLeftLabel);
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
}
