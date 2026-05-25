using System.ComponentModel;
using CoreForms.Ui.Controls.Basic;
using CoreForms.Ui.Demo.Models;
using CoreForms.Ui.Controls;
using CoreForms.Ui.Controls.Advanced;
using Graphics = CoreForms.Ui.Rendering.Graphics;
using CoreForms.Ui.Core;

namespace CoreForms.Ui.Demo.UserControls;

/// <summary>
/// Demonstrates the DataGridView control with a BindingList of Person objects.
/// </summary>
public class DataGridPage : UserControl
{
    private BindingList<Person>? _personList;

    /// <summary>
    /// Occurs when the status text should be updated.
    /// </summary>
    public event EventHandler<StatusTextChangedEventArgs>? StatusTextChanged;

    /// <summary>
    /// Gets the DataGridView instance for external access (e.g. PopulateForm lookup).
    /// </summary>
    public Controls.Advanced.DataGridView DataGrid { get; private set; } = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataGridPage"/> class.
    /// </summary>
    public DataGridPage()
    {
        var dataGrid = new Controls.Advanced.DataGridView
        {
            Location = new Point(10, 10),
            Size = new Size(550, 250),
            ColumnHeadersVisible = true,
            RowHeadersVisible = true,
            Name = "dataGrid"
        };

        dataGrid.Columns.Add(new CoreForms.Ui.Controls.Advanced.DataGridViewColumn
            { HeaderText = SR.GetString("ColId"), Width = 60, Name = "Id", DataPropertyName = "Id",
              TextAlign = CoreForms.Ui.Controls.Advanced.DataGridViewContentAlignment.Right, FormatString = "D3" });
        dataGrid.Columns.Add(new CoreForms.Ui.Controls.Advanced.DataGridViewColumn
            { HeaderText = SR.GetString("ColName"), Width = 150, Name = "Name", DataPropertyName = "Name" });
        dataGrid.Columns.Add(new CoreForms.Ui.Controls.Advanced.DataGridViewColumn
            { HeaderText = SR.GetString("ColEmail"), Width = 200, Name = "Email", DataPropertyName = "Email" });
        dataGrid.Columns.Add(new CoreForms.Ui.Controls.Advanced.DataGridViewColumn
            { HeaderText = SR.GetString("ColStatus"), Width = 100, Name = "Status", DataPropertyName = "Status",
              TextAlign = CoreForms.Ui.Controls.Advanced.DataGridViewContentAlignment.Center });
        dataGrid.Columns.Add(new CoreForms.Ui.Controls.Advanced.DataGridViewColumn
            { HeaderText = SR.GetString("ColSalary"), Width = 110, Name = "Salary", DataPropertyName = "Salary",
              TextAlign = CoreForms.Ui.Controls.Advanced.DataGridViewContentAlignment.Right, FormatString = "C" });

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
            var nextId = (_personList!.Count > 0 ? _personList.Max(p => p.Id) : 0) + 1;
            _personList.Add(new Person(nextId, "New Person", "new@example.com", "Active"));
        };

        var removeButton = new Button { Text = SR.GetString("BtnRemoveLast"), Location = new Point(150, 270), Size = new Size(130, 30) };
        removeButton.Click += (s, e) =>
        {
            if (_personList!.Count > 0)
                _personList.RemoveAt(_personList.Count - 1);
        };

        DataGrid = dataGrid;

        Controls.Add(dataGrid);
        Controls.Add(addButton);
        Controls.Add(removeButton);
    }
}
