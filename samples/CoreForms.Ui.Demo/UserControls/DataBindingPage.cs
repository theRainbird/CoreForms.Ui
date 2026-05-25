using System.ComponentModel;
using CoreForms.Ui.Controls.Basic;
using CoreForms.Ui.Controls.Containers;
using CoreForms.Ui.Data;
using CoreForms.Ui.Demo.Models;
using CoreForms.Ui.Controls;
using Graphics = CoreForms.Ui.Rendering.Graphics;
using CoreForms.Ui.Core;

namespace CoreForms.Ui.Demo.UserControls;

/// <summary>
/// Demonstrates data binding with BindingSource, ListBox, detail controls, and ComboBox data source.
/// </summary>
public class DataBindingPage : UserControl
{
    /// <summary>
    /// Occurs when the status text should be updated.
    /// </summary>
    public event EventHandler<StatusTextChangedEventArgs>? StatusTextChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataBindingPage"/> class.
    /// </summary>
    public DataBindingPage()
    {
        var contacts = new BindingList<Person>
        {
            new Person(1, "Alice Wonder", "alice@example.com", "Active"),
            new Person(2, "Bob Builder", "bob@example.com", "Active"),
            new Person(3, "Charlie Brown", "charlie@example.com", "Inactive")
        };
        var bindingSource = new BindingSource(contacts);

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
            Text = string.Format(SR.GetString("StatusPositionFormat"), 1, contacts.Count),
            Location = new Point(0, 70),
            Size = new Size(195, 20)
        };

        bindingSource.PositionChanged += (s, e) =>
        {
            positionLabel.Text = string.Format(SR.GetString("StatusPositionFormat"), bindingSource.Position + 1, bindingSource.Count);
        };

        navPanel.Controls.Add(firstButton);
        navPanel.Controls.Add(prevButton);
        navPanel.Controls.Add(nextButton);
        navPanel.Controls.Add(lastButton);
        navPanel.Controls.Add(addPersonButton);
        navPanel.Controls.Add(removePersonButton);
        navPanel.Controls.Add(positionLabel);

        listGroup.Controls.Add(contactListBox);
        listGroup.Controls.Add(navPanel);

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

        nameTextBox.DataBindings.Add("Text", bindingSource, "Name");
        emailTextBox.DataBindings.Add("Text", bindingSource, "Email");
        isActiveCheckBox.DataBindings.Add("Checked", bindingSource, "IsActive");

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

        if (bindingSource.Current is Person firstPerson)
        {
            idValue.Text = firstPerson.Id.ToString();
            statusComboBox.Text = firstPerson.Status;
        }

        statusComboBox.SelectedIndexChanged += (s, e) =>
        {
            if (bindingSource.Current is Person p)
            {
                p.Status = statusComboBox.Text;
            }
        };

        Controls.Add(listGroup);
        Controls.Add(detailGroup);
        Controls.Add(comboGroup);
    }
}
