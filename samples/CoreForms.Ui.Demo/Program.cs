using CoreForms.Ui.Controls.Advanced;
using CoreForms.Ui.Core;
using CoreForms.Ui.Controls.Basic;
using CoreForms.Ui.Controls.Containers;
using CoreForms.Ui.Layout;

namespace CoreForms.Ui.Demo;

class Program
{
    [STAThread]
    static void Main()
    {
        var form = new Form
        {
            Text = "CoreForms.Ui Demo - Dock & Anchor",
            Width = 900,
            Height = 650,
            BackColor = SystemColors.Window
        };

        var menuStrip = new CoreForms.Ui.Controls.Containers.MenuStrip();
        menuStrip.Dock = DockStyle.Top;
        menuStrip.Size = new Size(900, 30);
        menuStrip.Items.Add(new CoreForms.Ui.Controls.Containers.ToolStripMenuItem("File"));
        menuStrip.Items.Add(new CoreForms.Ui.Controls.Containers.ToolStripMenuItem("Edit"));
        menuStrip.Items.Add(new CoreForms.Ui.Controls.Containers.ToolStripMenuItem("View"));

        var statusLabel = new Label
        {
            Text = "Ready",
            ForeColor = SystemColors.ControlText
        };

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
            Size = new Size(700, 500)
        };
        mainPanel.Dock = DockStyle.Fill;

        mainPanel.Controls.Add(CreateHeaderLabel());
        mainPanel.Controls.Add(CreateDataGrid());
        mainPanel.Controls.Add(CreateBottomLeftPanel());
        mainPanel.Controls.Add(CreateBottomRightPanel());

        navListBox.SelectedIndexChanged += (s, e) =>
        {
            statusLabel.Text = navListBox.SelectedItem?.ToString() ?? "Ready";
        };

        form.Controls.Add(menuStrip);
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

    static Controls.Advanced.DataGridView CreateDataGrid()
    {
        var dataGrid = new Controls.Advanced.DataGridView
        {
            Location = new Point(15, 40),
            Size = new Size(400, 200),
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

    static Panel CreateBottomLeftPanel()
    {
        var panel = new Panel
        {
            BackColor = SystemColors.Control,
            Location = new Point(15, 250),
            Size = new Size(250, 220)
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

        var infoLabel = new Label
        {
            Text = "Dock: Left, Right, Top, Bottom, Fill\nAnchor: TopLeft, TopLeftRight, All\nResize window to see effects!",
            Location = new Point(5, 135),
            Size = new Size(200, 80),
            BackColor = SystemColors.Control,
            ForeColor = Color.FromArgb(80, 80, 80)
        };

        panel.Controls.Add(comboBox);
        panel.Controls.Add(checkBox);
        panel.Controls.Add(progressBar);
        panel.Controls.Add(addButton);
        panel.Controls.Add(progressButton);
        panel.Controls.Add(infoLabel);

        return panel;
    }

    static Panel CreateBottomRightPanel()
    {
        var panel = new Panel
        {
            BackColor = Color.FromArgb(245, 245, 255),
            Location = new Point(275, 250),
            Size = new Size(250, 220)
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