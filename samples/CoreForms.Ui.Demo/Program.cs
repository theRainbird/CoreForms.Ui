using CoreForms.Ui.Controls.Advanced;
using CoreForms.Ui.Core;
using CoreForms.Ui.Controls.Basic;
using CoreForms.Ui.Controls.Containers;
using CoreForms.Ui.Layout;
using CoreForms.Ui.Theming;

namespace CoreForms.Ui.Demo;

class Program
{
    [STAThread]
    static void Main()
    {
        var form = new Form
        {
            Text = "CoreForms.Ui Demo - Dock & Anchor",
            Width = 1200,
            Height = 900,
            BackColor = SystemColors.Window,
            Zoom = 1.25f
        };

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
            if (form.ActiveControl is Controls.Basic.TextBox tb)
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
            if (form.ActiveControl is Controls.Basic.TextBox tb)
            {
                try { tb.Paste(); }
                catch (Exception ex) { MessageBox.Show($"Clipboard error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        };
        var editSelectAllItem = new ToolStripMenuItem("Select &All");
        editSelectAllItem.Click += (s, e) => {
            if (form.ActiveControl is Controls.Basic.TextBox tb)
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

        var statusLabel = new Label
        {
            Text = "Ready",
            ForeColor = SystemColors.ControlText
        };

        var toolStrip = new ToolStrip();
        toolStrip.Dock = DockStyle.Top;

        var newIcon = SvgImage.FromSvgResource("CoreForms.Ui.Resources.Icons.file-plus.svg", 32);
        var newButton = new ToolStripButton("New", newIcon);
        newButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
        newButton.Click += (s, e) => statusLabel.Text = "New clicked";
        toolStrip.Items.Add(newButton);

        var openIcon = SvgImage.FromSvgResource("CoreForms.Ui.Resources.Icons.folder-open.svg", 32);
        var openButton = new ToolStripButton("Open", openIcon);
        openButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
        openButton.Click += (s, e) => statusLabel.Text = "Open clicked";
        toolStrip.Items.Add(openButton);

        var saveIcon = SvgImage.FromSvgResource("CoreForms.Ui.Resources.Icons.device-floppy.svg", 32);
        var saveButton = new ToolStripButton("Save", saveIcon);
        saveButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
        saveButton.Click += (s, e) => statusLabel.Text = "Save clicked";
        toolStrip.Items.Add(saveButton);

        toolStrip.Items.Add(new ToolStripSeparator());

        var boldIcon = SvgImage.FromSvgResource("CoreForms.Ui.Resources.Icons.bold.svg", 32);
        var boldButton = new ToolStripButton("B", boldIcon);
        boldButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
        boldButton.CheckOnClick = true;
        boldButton.CheckedChanged += (s, e) => statusLabel.Text = $"Bold: {boldButton.Checked}";
        toolStrip.Items.Add(boldButton);

        var italicIcon = SvgImage.FromSvgResource("CoreForms.Ui.Resources.Icons.italic.svg", 32);
        var italicButton = new ToolStripButton("I", italicIcon);
        italicButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
        italicButton.CheckOnClick = true;
        italicButton.CheckedChanged += (s, e) => statusLabel.Text = $"Italic: {italicButton.Checked}";
        toolStrip.Items.Add(italicButton);

        toolStrip.Items.Add(new ToolStripSeparator());

        var searchBox = new ToolStripTextBox();
        searchBox.TextBoxWidth = 120;
        searchBox.TextChanged += (s, e) => statusLabel.Text = $"Search: {searchBox.Text}";
        toolStrip.Items.Add(searchBox);

        var searchIcon = SvgImage.FromSvgResource("CoreForms.Ui.Resources.Icons.search.svg", 32);
        var searchButton = new ToolStripButton("Search", searchIcon);
        searchButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
        searchButton.Click += (s, e) => statusLabel.Text = $"Searching for: {searchBox.Text}";
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
            statusLabel.Text = $"Zoom: {zoomComboBox.Text}";
        };
        toolStrip.Items.Add(zoomComboBox);

        toolStrip.Items.Add(new ToolStripSeparator());

        ToolStripButton? lightButton = null;
        ToolStripButton? darkButton = null;

        var lightIcon = SvgImage.FromSvgResource("CoreForms.Ui.Resources.Icons.sun.svg", 32);
        lightButton = new ToolStripButton("Light", lightIcon);
        lightButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
        lightButton.Checked = true;
        lightButton.CheckOnClick = true;
        lightButton.CheckedChanged += (s, e) =>
        {
            if (lightButton.Checked)
            {
                ThemeManager.SetTheme(new LightTheme());
                darkButton!.Checked = false;
                statusLabel.Text = "Theme: Light";
            }
        };
        toolStrip.Items.Add(lightButton);

        var darkIcon = SvgImage.FromSvgResource("CoreForms.Ui.Resources.Icons.moon.svg", 32);
        darkButton = new ToolStripButton("Dark", darkIcon);
        darkButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
        darkButton.CheckOnClick = true;
        darkButton.CheckedChanged += (s, e) =>
        {
            if (darkButton.Checked)
            {
                ThemeManager.SetTheme(new DarkTheme());
                lightButton!.Checked = false;
                statusLabel.Text = "Theme: Dark";
            }
        };
        toolStrip.Items.Add(darkButton);

        toolStrip.Items.Add(new ToolStripSeparator());

        var helpIcon = SvgImage.FromSvgResource("CoreForms.Ui.Resources.Icons.help.svg", 32);
        var helpButton = new ToolStripButton("Help", helpIcon);
        helpButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
        helpButton.Click += (s, e) => MessageBox.Show("CoreForms.Ui ToolStrip Demo\n\nDemonstrates all ToolStrip item types.", "Help", MessageBoxButtons.OK, MessageBoxIcon.Information);
        toolStrip.Items.Add(helpButton);

        var statusStrip = new Panel
        {
            BackColor = SystemColors.Control,
            Size = new Size(900, 24)
        };
        statusStrip.Dock = DockStyle.Bottom;
        statusStrip.Padding = new Padding(5, 2, 5, 2);
        statusLabel.Location = new Point(5, 2);
        statusLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        statusStrip.Controls.Add(statusLabel);

        var leftPanel = new Panel
        {
            BackColor = Color.FromArgb(240, 240, 240),
            Size = new Size(180, 500)
        };
        leftPanel.Dock = DockStyle.Left;
        leftPanel.BorderStyle = BorderStyle.FixedSingle;

        var navLabel = new Label
        {
            Text = "Navigation",
            Location = new Point(10, 10),
            Size = new Size(150, 20),
            BackColor = Color.FromArgb(240, 240, 240),
            ForeColor = Color.Black
        };

        var navListBox = new ListBox
        {
            Location = new Point(10, 35),
            Size = new Size(150, 300)
        };
        navListBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        navListBox.Items.Add("Home");
        navListBox.Items.Add("Dashboard");
        navListBox.Items.Add("Settings");
        navListBox.Items.Add("Profile");
        navListBox.Items.Add("Reports");
        navListBox.Items.Add("Analytics");

        leftPanel.Controls.Add(navLabel);
        leftPanel.Controls.Add(navListBox);

        var mainPanel = new Panel
        {
            BackColor = Color.White,
            Size = new Size(700, 600)
        };
        mainPanel.Dock = DockStyle.Fill;

        mainPanel.Controls.Add(CreateHeaderLabel());
        mainPanel.Controls.Add(CreateTabControl());
        mainPanel.Controls.Add(CreateDataGrid());
        mainPanel.Controls.Add(CreateBottomLeftPanel(statusLabel));
        mainPanel.Controls.Add(CreateBottomRightPanel());

        navListBox.SelectedIndexChanged += (s, e) =>
        {
            statusLabel.Text = navListBox.SelectedItem?.ToString() ?? "Ready";
        };

        form.Controls.Add(menuStrip);
        form.Controls.Add(toolStrip);
        form.Controls.Add(statusStrip);
        form.Controls.Add(leftPanel);
        form.Controls.Add(mainPanel);
        
        var addButton = form.Controls.FindControl<Button>("addButton", recursive: true);
        addButton?.Click += (sender, args) =>
        {
            var dataGrid = form.Controls.FindControl<DataGridView>("dataGrid", recursive: true);
            dataGrid?.AddRow(dataGrid.Rows.Count + 1, "John Doe", "john@example.com", "Active");
        };
        
        Application.Run(form);
    }

    static Label CreateHeaderLabel()
    {
        var label = new Label
        {
            Text = "Dock & Anchor Demo",
            Location = new Point(15, 10),
            Size = new Size(300, 25),
            BackColor = Color.White,
            ForeColor = Color.FromArgb(10, 36, 99)
        };
        label.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        return label;
    }

    static TabControl CreateTabControl()
    {
        var tabControl = new TabControl
        {
            Location = new Point(15, 45),
            Size = new Size(400, 160)
        };
        tabControl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        var textBoxPage = new TabPage { Text = "Text Boxes" };

        var nameLabel = new Label
        {
            Text = "Name:",
            Location = new Point(10, 10),
            Size = new Size(70, 20),
            BackColor = Color.White
        };

        var nameTextBox = new TextBox
        {
            Location = new Point(80, 10),
            Size = new Size(200, 25),
            Text = "John Doe",
            Name = "nameTextBox"
        };

        var emailLabel = new Label
        {
            Text = "Email:",
            Location = new Point(10, 40),
            Size = new Size(70, 20),
            BackColor = Color.White
        };

        var emailTextBox = new TextBox
        {
            Location = new Point(80, 40),
            Size = new Size(200, 25),
            Text = "john@example.com",
            Name = "emailTextBox"
        };

        var passwordLabel = new Label
        {
            Text = "Password:",
            Location = new Point(10, 70),
            Size = new Size(70, 20),
            BackColor = Color.White
        };

        var passwordTextBox = new TextBox
        {
            Location = new Point(80, 70),
            Size = new Size(200, 25),
            Text = "MySecretPassword",
            Name = "passwordTextBox",
            UseSystemPasswordChar = true
        };

        textBoxPage.Controls.Add(nameLabel);
        textBoxPage.Controls.Add(nameTextBox);
        textBoxPage.Controls.Add(emailLabel);
        textBoxPage.Controls.Add(emailTextBox);
        textBoxPage.Controls.Add(passwordLabel);
        textBoxPage.Controls.Add(passwordTextBox);

        var radioPage = new TabPage { Text = "Radio Buttons" };

        var genderLabel = new Label
        {
            Text = "Gender:",
            Location = new Point(10, 10),
            Size = new Size(80, 20),
            BackColor = Color.White
        };

        var maleRadio = new RadioButton
        {
            Text = "Male",
            Location = new Point(10, 35),
            Size = new Size(100, 20),
            Checked = true
        };

        var femaleRadio = new RadioButton
        {
            Text = "Female",
            Location = new Point(10, 60),
            Size = new Size(100, 20)
        };

        var otherRadio = new RadioButton
        {
            Text = "Other",
            Location = new Point(10, 85),
            Size = new Size(100, 20)
        };

        var roleLabel = new Label
        {
            Text = "Role:",
            Location = new Point(120, 10),
            Size = new Size(80, 20),
            BackColor = Color.White
        };

        var adminRadio = new RadioButton
        {
            Text = "Admin",
            Location = new Point(120, 35),
            Size = new Size(100, 20)
        };

        var userRadio = new RadioButton
        {
            Text = "User",
            Location = new Point(120, 60),
            Size = new Size(100, 20),
            Checked = true
        };

        var guestRadio = new RadioButton
        {
            Text = "Guest",
            Location = new Point(120, 85),
            Size = new Size(100, 20)
        };

        radioPage.Controls.Add(genderLabel);
        radioPage.Controls.Add(maleRadio);
        radioPage.Controls.Add(femaleRadio);
        radioPage.Controls.Add(otherRadio);
        radioPage.Controls.Add(roleLabel);
        radioPage.Controls.Add(adminRadio);
        radioPage.Controls.Add(userRadio);
        radioPage.Controls.Add(guestRadio);

        tabControl.AddTabPage(textBoxPage);
        tabControl.AddTabPage(radioPage);

        return tabControl;
    }

    static Controls.Advanced.DataGridView CreateDataGrid()
    {
        var dataGrid = new Controls.Advanced.DataGridView
        {
            Location = new Point(15, 215),
            Size = new Size(400, 180),
            ColumnHeadersVisible = true,
            RowHeadersVisible = true,
            Name = "dataGrid"
        };
        dataGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
        dataGrid.Columns.Add(new Controls.Advanced.DataGridViewColumn { HeaderText = "ID", Width = 50, Name = "Id" });
        dataGrid.Columns.Add(new Controls.Advanced.DataGridViewColumn { HeaderText = "Name", Width = 150, Name = "Name" });
        dataGrid.Columns.Add(new Controls.Advanced.DataGridViewColumn { HeaderText = "Email", Width = 200, Name = "Email" });
        dataGrid.Columns.Add(new Controls.Advanced.DataGridViewColumn { HeaderText = "Status", Width = 80, Name = "Status" });
        dataGrid.AddRow(1, "John Doe", "john@example.com", "Active");
        dataGrid.AddRow(2, "Jane Smith", "jane@example.com", "Active");
        dataGrid.AddRow(3, "Bob Johnson", "bob@example.com", "Inactive");
        return dataGrid;
    }

    static Panel CreateBottomLeftPanel(Label statusLabel)
    {
        var panel = new Panel
        {
            Location = new Point(15, 410),
            Size = new Size(400, 160)
        };
        panel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

        var comboBox = new ComboBox
        {
            Location = new Point(5, 5),
            Size = new Size(200, 24)
        };
        comboBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        comboBox.Items.Add("Active");
        comboBox.Items.Add("Inactive");
        comboBox.Items.Add("Pending");

        var checkBox = new CheckBox
        {
            Text = "Show Grid Lines",
            Location = new Point(5, 35),
            Size = new Size(150, 25),
            BackColor = SystemColors.Control,
            Checked = true
        };

        var progressBar = new ProgressBar
        {
            Location = new Point(5, 65),
            Size = new Size(200, 20),
            Value = 50
        };
        progressBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        var addButton = new Button
        {
            Text = "Add Row",
            Location = new Point(5, 95),
            Size = new Size(100, 30),
            Name = "addButton"
        };

        var progressButton = new Button
        {
            Text = "Progress +",
            Location = new Point(115, 95),
            Size = new Size(100, 30)
        };
        progressButton.Click += (s, e) =>
        {
            if (progressBar.Value < 100)
                progressBar.Value += 10;
        };

        var msgBoxButton = new Button
        {
            Text = "MessageBox",
            Location = new Point(5, 130),
            Size = new Size(210, 30)
        };
        msgBoxButton.Click += (s, e) =>
        {
            var result = MessageBox.Show(
                "This is a test of the MessageBox.\nAll button combinations are available in the menu.\n\nClick Yes to continue, No to cancel.",
                "MessageBox Test",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);
            statusLabel.Text = $"MessageBox result: {result}";
        };

      
        panel.Controls.Add(comboBox);
        panel.Controls.Add(checkBox);
        panel.Controls.Add(progressBar);
        panel.Controls.Add(addButton);
        panel.Controls.Add(progressButton);
        panel.Controls.Add(msgBoxButton);
      
        return panel;
    }

    static Panel CreateBottomRightPanel()
    {
        var panel = new Panel
        {
            BackColor = Color.FromArgb(245, 245, 255),
            Location = new Point(430, 45),
            Size = new Size(250, 365)
        };
        panel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

        var dockedTopLabel = new Label
        {
            Text = "Dock=Top",
            BackColor = Color.FromArgb(200, 220, 255),
            ForeColor = Color.Black,
            Size = new Size(250, 22)
        };
        dockedTopLabel.Dock = DockStyle.Top;

        var dockedBottomLabel = new Label
        {
            Text = "Dock=Bottom",
            BackColor = Color.FromArgb(255, 220, 200),
            ForeColor = Color.Black,
            Size = new Size(250, 22)
        };
        dockedBottomLabel.Dock = DockStyle.Bottom;

        var dockedLeftLabel = new Label
        {
            Text = "L",
            BackColor = Color.FromArgb(200, 255, 200),
            ForeColor = Color.Black,
            Size = new Size(30, 176)
        };
        dockedLeftLabel.Dock = DockStyle.Left;

        var centerLabel = new Label
        {
            Text = "Dock=Fill",
            BackColor = Color.FromArgb(255, 255, 220),
            ForeColor = Color.Black,
            Size = new Size(50, 50)
        };
        centerLabel.Dock = DockStyle.Fill;

        panel.Controls.Add(dockedTopLabel);
        panel.Controls.Add(dockedBottomLabel);
        panel.Controls.Add(dockedLeftLabel);
        panel.Controls.Add(centerLabel);

        return panel;
    }
}