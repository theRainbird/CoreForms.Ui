using CoreForms.Ui.Core;
using CoreForms.Ui.Platform;

namespace CoreForms.Ui;

/// <summary>
/// Displays a standard dialog that prompts the user to save a file.
/// Mirrors the WinForms SaveFileDialog API.
/// </summary>
public class SaveFileDialog : FileDialog
{
    private bool _createPrompt;
    private bool _overwritePrompt = true;

    /// <summary>
    /// Gets or sets whether the dialog prompts for creation if the file does not exist.
    /// </summary>
    public bool CreatePrompt
    {
        get => _createPrompt;
        set => _createPrompt = value;
    }

    /// <summary>
    /// Gets or sets whether the dialog prompts for overwrite confirmation
    /// if the selected file already exists.
    /// </summary>
    public bool OverwritePrompt
    {
        get => _overwritePrompt;
        set => _overwritePrompt = value;
    }

    /// <summary>
    /// Opens the selected file with read/write access.
    /// </summary>
    /// <returns>A Stream for the selected file.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no file was selected.</exception>
    public Stream OpenFile()
    {
        string fileName = FileName;
        if (string.IsNullOrEmpty(fileName))
            throw new InvalidOperationException("No file was selected.");

        return new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite);
    }

    /// <summary>
    /// Runs the Save File dialog using the native OS file picker.
    /// </summary>
    /// <param name="owner">The owner form, or null.</param>
    /// <returns>One of the DialogResult values.</returns>
    protected override DialogResult RunDialog(Form? owner)
    {
        var result = FileDialogImpl.ShowSaveFile(
            owner, Title, InitialDirectory, FileName, Filter, FilterIndex,
            DefaultExt, AddExtension, _overwritePrompt, _createPrompt,
            out string file);

        if (!string.IsNullOrEmpty(file))
        {
            FileName = file;
        }

        return result;
    }
}
