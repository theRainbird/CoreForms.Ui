using CoreForms.Ui.Core;
using CoreForms.Ui.Controls.Basic;
using CoreForms.Ui.Controls.Advanced;

namespace CoreForms.Ui.Demo;

class Program
{
    [STAThread]
    static void Main()
    {
        var form = new Form
        {
            Text = "CoreForms.Ui Demo",
            Width = 900,
            Height = 650,
            BackColor = SystemColors.Window
        };

        var label = new Label
        {
            Text = "DataGrid Example:",
            Location = new Point(20, 20),
            Size = new Size(200, 25),
            BackColor = Color.White,
            ForeColor = Color.Black
        };

        var dataGrid = new DataGridView
        {
            Location = new Point(20, 50),
            Size = new Size(600, 200),
            ColumnHeadersVisible = true,
            RowHeadersVisible = true
        };

        dataGrid.Columns.Add(new DataGridViewColumn { HeaderText = "ID", Width = 60, Name = "Id" });
        dataGrid.Columns.Add(new DataGridViewColumn { HeaderText = "Name", Width = 150, Name = "Name" });
        dataGrid.Columns.Add(new DataGridViewColumn { HeaderText = "Email", Width = 200, Name = "Email" });
        dataGrid.Columns.Add(new DataGridViewColumn { HeaderText = "Status", Width = 100, Name = "Status" });

        dataGrid.AddRow(1, "John Doe", "john@example.com", "Active");
        dataGrid.AddRow(2, "Jane Smith", "jane@example.com", "Active");
        dataGrid.AddRow(3, "Bob Johnson", "bob@example.com", "Inactive");
        dataGrid.AddRow(4, "Alice Brown", "alice@example.com", "Active");
        dataGrid.AddRow(5, "Charlie Wilson", "charlie@example.com", "Pending");

        dataGrid.SelectionChanged += (s, e) =>
        {
            label.Text = dataGrid.SelectedValue?.ToString() ?? "No selection";
        };

        // Note: CellClick event example commented out due to generic event type issue
        // dataGrid.CellClick += (s, args) => { ... }

        var comboBox = new ComboBox
        {
            Location = new Point(20, 270),
            Size = new Size(200, 24)
        };

        comboBox.Items.Add("Item 1");
        comboBox.Items.Add("Item 2");
        comboBox.Items.Add("Item 3");
        comboBox.Items.Add("Item 4");
        comboBox.Items.Add("Item 5");

        var checkBox = new CheckBox
        {
            Text = "Show Grid Lines",
            Location = new Point(250, 270),
            Size = new Size(150, 25),
            Checked = true
        };

        checkBox.CheckedChanged += (s, e) =>
        {
            dataGrid.ShowGridLines = checkBox.Checked;
        };

        var progressBar = new ProgressBar
        {
            Location = new Point(20, 310),
            Size = new Size(200, 20),
            Value = 50
        };

        var button = new Button
        {
            Text = "Add Row",
            Location = new Point(250, 305),
            Size = new Size(100, 30)
        };

        button.Click += (s, e) =>
        {
            var random = new Random();
            var newId = dataGrid.Rows.Count + 1;
            dataGrid.AddRow(newId, $"New User {newId}", $"user{newId}@example.com", "Active");
        };

        var listBox = new ListBox
        {
            Location = new Point(20, 350),
            Size = new Size(200, 120)
        };

        listBox.Items.Add("Item 1");
        listBox.Items.Add("Item 2");
        listBox.Items.Add("Item 3");
        listBox.Items.Add("Item 4");
        listBox.Items.Add("Item 5");

        listBox.SelectedIndexChanged += (s, e) =>
        {
            label.Text = listBox.SelectedItem?.ToString() ?? "";
        };

        form.Controls.Add(label);
        form.Controls.Add(dataGrid);
        form.Controls.Add(comboBox);
        form.Controls.Add(checkBox);
        form.Controls.Add(progressBar);
        form.Controls.Add(button);
        form.Controls.Add(listBox);

        Application.Run(form);
    }
}