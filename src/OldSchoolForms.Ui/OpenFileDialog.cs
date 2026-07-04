using OldSchoolForms.Ui.Core;
using OldSchoolForms.Ui.Platform;

namespace OldSchoolForms.Ui;

/// <summary>
/// Displays a standard dialog that prompts the user to open a file.
/// Mirrors the WinForms OpenFileDialog API.
/// </summary>
public class OpenFileDialog : FileDialog
{
    private bool _multiSelect;
    private bool _readOnlyChecked;
    private bool _showReadOnly;

    /// <summary>
    /// Gets or sets whether the dialog allows multiple files to be selected.
    /// </summary>
    public bool Multiselect
    {
        get => _multiSelect;
        set => _multiSelect = value;
    }

    /// <summary>
    /// Gets or sets whether the read-only check box is selected.
    /// </summary>
    public bool ReadOnlyChecked
    {
        get => _readOnlyChecked;
        set => _readOnlyChecked = value;
    }

    /// <summary>
    /// Gets or sets whether the dialog includes a read-only check box.
    /// </summary>
    public bool ShowReadOnly
    {
        get => _showReadOnly;
        set => _showReadOnly = value;
    }

    /// <summary>
    /// Opens the selected file with read-only access.
    /// </summary>
    /// <returns>A read-only Stream for the selected file.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no file was selected.</exception>
    public Stream OpenFile()
    {
        string fileName = FileName;
        if (string.IsNullOrEmpty(fileName))
            throw new InvalidOperationException("No file was selected.");

        return new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    /// <summary>
    /// Runs the Open File dialog using the native OS file picker.
    /// </summary>
    /// <param name="owner">The owner form, or null.</param>
    /// <returns>One of the DialogResult values.</returns>
    protected override DialogResult RunDialog(Form? owner)
    {
        var result = FileDialogImpl.ShowOpenFile(
            owner, Title, InitialDirectory, FileName, Filter, FilterIndex,
            DefaultExt, AddExtension, CheckFileExists, CheckPathExists,
            DereferenceLinks, _multiSelect, out string[] files);

        if (files.Length > 0)
        {
            FileName = files[0];
        }

        return result;
    }
}
