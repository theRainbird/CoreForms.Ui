using System.ComponentModel;
using CoreForms.Ui.Controls.Basic;
using CoreForms.Ui.Demo.Models;
using CoreForms.Ui.Controls;
using CoreForms.Ui.Controls.Advanced;
using CoreForms.Ui.Controls.Containers;
using CoreForms.Ui.Theming;
using Graphics = CoreForms.Ui.Rendering.Graphics;
using CoreForms.Ui.Core;

namespace CoreForms.Ui.Demo.UserControls;

public class DataGridPage : UserControl
{
    private BindingList<Person>? _personList;
    public event EventHandler<StatusTextChangedEventArgs>? StatusTextChanged;
    public Controls.Advanced.DataGridView DataGrid { get; private set; } = null!;

    public DataGridPage()
    {
        var dataGrid = new Controls.Advanced.DataGridView
        {
            Dock = DockStyle.Fill,
            ColumnHeadersVisible = true,
            RowHeadersVisible = true,
            ShowGroupingBar = true,
            Name = "dataGrid"
        };

        string[] statusOptions = ["Active", "Inactive", "Pending"];

        dataGrid.Columns.Add(new CoreForms.Ui.Controls.Advanced.DataGridViewColumn
            { HeaderText = SR.GetString("ColId"), Width = 60, Name = "Id", DataPropertyName = "Id",
              TextAlign = CoreForms.Ui.Controls.Advanced.DataGridViewContentAlignment.Right, FormatString = "D3",
              CellEditType = DataGridViewColumnEditType.TextBox });
        dataGrid.Columns.Add(new CoreForms.Ui.Controls.Advanced.DataGridViewColumn
            { HeaderText = SR.GetString("ColName"), Width = 150, Name = "Name", DataPropertyName = "Name",
              CellEditType = DataGridViewColumnEditType.TextBox });
        dataGrid.Columns.Add(new CoreForms.Ui.Controls.Advanced.DataGridViewColumn
            { HeaderText = SR.GetString("ColEmail"), Width = 200, Name = "Email", DataPropertyName = "Email",
              CellEditType = DataGridViewColumnEditType.TextBox, ReadOnly = true });
        dataGrid.Columns.Add(new CoreForms.Ui.Controls.Advanced.DataGridViewColumn
            { HeaderText = SR.GetString("ColStatus"), Width = 100, Name = "Status", DataPropertyName = "Status",
              TextAlign = CoreForms.Ui.Controls.Advanced.DataGridViewContentAlignment.Center,
              CellEditType = DataGridViewColumnEditType.ComboBox, Items = statusOptions.Cast<object>().ToList() });
        dataGrid.Columns.Add(new CoreForms.Ui.Controls.Advanced.DataGridViewColumn
            { HeaderText = SR.GetString("ColSalary"), Width = 110, Name = "Salary", DataPropertyName = "Salary",
              TextAlign = CoreForms.Ui.Controls.Advanced.DataGridViewContentAlignment.Right, FormatString = "C",
              CellEditType = DataGridViewColumnEditType.TextBox });
        dataGrid.Columns.Add(new CoreForms.Ui.Controls.Advanced.DataGridViewColumn
            { HeaderText = "Active", Width = 80, Name = "IsActive", DataPropertyName = "IsActive",
              TextAlign = CoreForms.Ui.Controls.Advanced.DataGridViewContentAlignment.Center,
              CellEditType = DataGridViewColumnEditType.CheckBox, TrueValue = true, FalseValue = false });
        dataGrid.Columns.Add(new CoreForms.Ui.Controls.Advanced.DataGridViewColumn
            { HeaderText = "Birth Date", Width = 120, Name = "BirthDate", DataPropertyName = "BirthDate",
              CellEditType = DataGridViewColumnEditType.DateTimePicker, PickerFormat = DateTimePickerFormat.Short, FormatString = "d"});
        dataGrid.Columns.Add(new CoreForms.Ui.Controls.Advanced.DataGridViewColumn
            { HeaderText = "", Width = 70, Name = "Actions",
              CellEditType = DataGridViewColumnEditType.Button, ButtonText = "Edit",
              ButtonIcon = CoreForms.Ui.Resources.Icons.Edit16 });

        _personList = new BindingList<Person>
        {
            new Person(1, "John Doe", "john@example.com", "Active") { Salary = 75000m, IsActive = true, BirthDate = new DateTime(1985, 3, 15) },
            new Person(2, "Jane Smith", "jane@example.com", "Active") { Salary = 82000m, IsActive = true, BirthDate = new DateTime(1990, 7, 22) },
            new Person(3, "Bob Johnson", "bob@example.com", "Inactive") { Salary = 0m, IsActive = false, BirthDate = new DateTime(1978, 11, 8) },
            new Person(4, "Alice Brown", "alice@example.com", "Active") { Salary = 91500m, IsActive = true, BirthDate = new DateTime(1995, 1, 30) },
            new Person(5, "Charlie Wilson", "charlie@example.com", "Pending") { Salary = 68000m, IsActive = false, BirthDate = new DateTime(2000, 6, 1) }
        };
        dataGrid.DataSource = _personList;

        // Bottom panel for all control buttons
        var bottomPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 80
        };

        int bx = 10;
        var addButton = new Button { Text = SR.GetString("BtnAddRow"), Location = new Point(bx, 10), Size = new Size(130, 25) };
        addButton.Click += (s, e) =>
        {
            var nextId = (_personList!.Count > 0 ? _personList.Max(p => p.Id) : 0) + 1;
            _personList.Add(new Person(nextId, "New Person", "new@example.com", "Active"));
        };

        bx += 140;
        var removeButton = new Button { Text = SR.GetString("BtnRemoveLast"), Location = new Point(bx, 10), Size = new Size(130, 25) };
        removeButton.Click += (s, e) =>
        {
            if (_personList!.Count > 0)
                _personList.RemoveAt(_personList.Count - 1);
        };

        bx += 140;
        var gbCheck = new CheckBox { Text = "Grouping Bar", Location = new Point(bx, 10), Size = new Size(130, 25), Checked = true };
        gbCheck.CheckedChanged += (s, e) => dataGrid.ShowGroupingBar = gbCheck.Checked;

        bx += 140;
        var clearBtn = new Button { Text = "Clear Grouping", Location = new Point(bx, 10), Size = new Size(130, 25) };
        clearBtn.Click += (s, e) => { dataGrid.ClearGrouping(); gbCheck.Checked = false; };

        int by2 = 40;
        int bx2 = 10;
        var gNameBtn = new Button { Text = "Group: Name", Location = new Point(bx2, by2), Size = new Size(110, 25) };
        gNameBtn.Click += (s, e) => { dataGrid.AddGroupColumn(1); dataGrid.ShowGroupingBar = true; gbCheck.Checked = true; };

        bx2 += 120;
        var gStatusBtn = new Button { Text = "Group: Status", Location = new Point(bx2, by2), Size = new Size(110, 25) };
        gStatusBtn.Click += (s, e) => { dataGrid.AddGroupColumn(3); dataGrid.ShowGroupingBar = true; gbCheck.Checked = true; };

        bx2 += 120;
        var gSalaryBtn = new Button { Text = "Group: Salary", Location = new Point(bx2, by2), Size = new Size(110, 25) };
        gSalaryBtn.Click += (s, e) => { dataGrid.AddGroupColumn(4); dataGrid.ShowGroupingBar = true; gbCheck.Checked = true; };

        bottomPanel.Controls.Add(addButton);
        bottomPanel.Controls.Add(removeButton);
        bottomPanel.Controls.Add(gbCheck);
        bottomPanel.Controls.Add(clearBtn);
        bottomPanel.Controls.Add(gNameBtn);
        bottomPanel.Controls.Add(gStatusBtn);
        bottomPanel.Controls.Add(gSalaryBtn);

        dataGrid.GroupHeaderFormatting += (s, e) =>
        {
            bool isDark = ThemeManager.CurrentTheme.Name == "Dark";
            if (e.Level == 0)
            {
                e.BackColor = isDark ? Color.FromArgb(65, 65, 70) : Color.FromArgb(200, 220, 240);
                e.Font = new Font(e.Font?.Name ?? "Arial", 14f, FontStyle.Bold);
            }
            else
            {
                e.BackColor = isDark ? Color.FromArgb(55, 55, 60) : Color.FromArgb(220, 235, 250);
                e.Font = new Font(e.Font?.Name ?? "Arial", 13f, FontStyle.Regular);
            }
        };

        dataGrid.CellButtonClick += (s, e) =>
        {
            if (_personList != null && e.RowIndex >= 0 && e.RowIndex < _personList.Count)
            {
                var person = _personList[e.RowIndex];
                var displayName = person.Name;
                var status = person.Status;
                var statusTextChanged = StatusTextChanged;
                if (statusTextChanged != null)
                {
                    var args = new StatusTextChangedEventArgs($"Edit clicked for {displayName} ({status})");
                    statusTextChanged(this, args);
                }
            }
        };

        DataGrid = dataGrid;

        Controls.Add(dataGrid);
        Controls.Add(bottomPanel);
    }
}
