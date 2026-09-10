using OldSchoolForms.Ui.Core;
using OldSchoolForms.Ui.Controls.Basic;
using Graphics = OldSchoolForms.Ui.Rendering.Graphics;
using OldSchoolForms.Ui.Controls.Containers;

namespace OldSchoolForms.Ui.Designer;

/// <summary>
/// Identifies the kind of document that a designer should create as the surface root.
/// </summary>
public enum NewDocumentType
{
    /// <summary>A <see cref="OldSchoolForms.Ui.Designer.DesignForm"/> with a title bar.</summary>
    Form,

    /// <summary>A <see cref="OldSchoolForms.Ui.Designer.DesignUserControl"/> without a title bar.</summary>
    UserControl
}

/// <summary>
/// A modal dialog that lets the user choose whether a new design should start from a
/// <see cref="DesignForm"/> or a <see cref="DesignUserControl"/>.
/// </summary>
public class NewDocumentDialog : Form
{
    private RadioButton _formRadio = null!;
    private RadioButton _userControlRadio = null!;

    private NewDocumentType _selectedType;

    /// <summary>
    /// Gets the document type currently selected in the dialog.
    /// </summary>
    public NewDocumentType SelectedType => _selectedType;

    /// <summary>
    /// Shows the new-document dialog and blocks until the user confirms or cancels.
    /// </summary>
    /// <param name="initial">The document type preselected when the dialog opens.</param>
    /// <param name="owner">The owner form, or null.</param>
    /// <returns>The selected document type when confirmed; <see langword="null"/> when cancelled.</returns>
    public static NewDocumentType? ShowDialog(NewDocumentType initial = NewDocumentType.Form, Form? owner = null)
    {
        var dialog = new NewDocumentDialog(initial);
        var result = dialog.ShowDialog(owner);
        return result == DialogResult.OK ? dialog._selectedType : null;
    }

    private NewDocumentDialog(NewDocumentType initial)
    {
        _selectedType = initial;
        Text = "Neues Element";
        Width = 360;
        Height = 220;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        Zoom = 1f;

        var instruction = new Label
        {
            Text = "Welches Element soll erstellt werden?",
            X = 24,
            Y = 28,
            Width = 310,
            Height = 20
        };
        Controls.Add(instruction);

        _formRadio = new RadioButton
        {
            Text = "Form",
            X = 40,
            Y = 72,
            Width = 280,
            Height = 28,
            Checked = initial == NewDocumentType.Form
        };
        _userControlRadio = new RadioButton
        {
            Text = "UserControl",
            X = 40,
            Y = 104,
            Width = 280,
            Height = 28,
            Checked = initial != NewDocumentType.Form
        };
        _formRadio.CheckedChanged += (_, _) =>
        {
            if (_formRadio.Checked)
                _selectedType = NewDocumentType.Form;
        };
        _userControlRadio.CheckedChanged += (_, _) =>
        {
            if (_userControlRadio.Checked)
                _selectedType = NewDocumentType.UserControl;
        };
        Controls.Add(_formRadio);
        Controls.Add(_userControlRadio);

        var buttonPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 50,
            BackColor = Core.Color.Transparent
        };
        var okButton = new Button
        {
            Text = "OK",
            Width = 75,
            Height = 30,
            X = 112,
            Y = 10
        };
        okButton.Click += (_, _) => CloseDialog(true);

        var cancelButton = new Button
        {
            Text = "Abbrechen",
            Width = 75,
            Height = 30,
            X = 197,
            Y = 10
        };
        cancelButton.Click += (_, _) => CloseDialog(false);

        buttonPanel.Controls.AddRange([okButton, cancelButton]);
        Controls.Add(buttonPanel);
    }

    private void CloseDialog(bool ok)
    {
        DialogResult = ok ? DialogResult.OK : DialogResult.Cancel;
        _modal = false;
    }
}
