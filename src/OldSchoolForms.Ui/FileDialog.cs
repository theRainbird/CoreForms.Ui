using OldSchoolForms.Ui.Core;

namespace OldSchoolForms.Ui;

/// <summary>
/// Provides an abstract base class for file dialog boxes.
/// Mirrors the WinForms FileDialog API for familiarity.
/// </summary>
public abstract class FileDialog : Core.Component
{
    private string _title = string.Empty;
    private string _initialDirectory = string.Empty;
    private string _fileName = string.Empty;
    private string[] _fileNames = [];
    private string _filter = string.Empty;
    private int _filterIndex = 1;
    private string _defaultExt = string.Empty;
    private bool _addExtension = true;
    private bool _checkFileExists;
    private bool _checkPathExists = true;
    private bool _validateNames = true;
    private bool _dereferenceLinks = true;
    private bool _supportMultiDottedExtensions;

    /// <summary>
    /// Gets or sets the dialog title.
    /// </summary>
    public string Title
    {
        get => _title;
        set => _title = value ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets the initial directory displayed by the file dialog.
    /// </summary>
    public string InitialDirectory
    {
        get => _initialDirectory;
        set => _initialDirectory = value ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets the file name selected in the file dialog.
    /// </summary>
    public string FileName
    {
        get => _fileName;
        set => _fileName = value ?? string.Empty;
    }

    /// <summary>
    /// Gets the file names of all selected files in the dialog.
    /// </summary>
    public string[] FileNames => _fileNames;

    /// <summary>
    /// Gets or sets the filter string that determines the types of files shown.
    /// Format: "Text files|*.txt|All files|*.*"
    /// </summary>
    public string Filter
    {
        get => _filter;
        set => _filter = value ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets the index of the currently selected filter (1-based).
    /// </summary>
    public int FilterIndex
    {
        get => _filterIndex;
        set => _filterIndex = Math.Max(1, value);
    }

    /// <summary>
    /// Gets or sets the default file name extension.
    /// </summary>
    public string DefaultExt
    {
        get => _defaultExt;
        set => _defaultExt = value ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets whether the dialog automatically adds an extension to a file name.
    /// </summary>
    public bool AddExtension
    {
        get => _addExtension;
        set => _addExtension = value;
    }

    /// <summary>
    /// Gets or sets whether the dialog checks that the selected file exists.
    /// </summary>
    public bool CheckFileExists
    {
        get => _checkFileExists;
        set => _checkFileExists = value;
    }

    /// <summary>
    /// Gets or sets whether the dialog checks that the selected path exists.
    /// </summary>
    public bool CheckPathExists
    {
        get => _checkPathExists;
        set => _checkPathExists = value;
    }

    /// <summary>
    /// Gets or sets whether the dialog validates file names.
    /// </summary>
    public bool ValidateNames
    {
        get => _validateNames;
        set => _validateNames = value;
    }

    /// <summary>
    /// Gets or sets whether the dialog dereferences shortcut links.
    /// </summary>
    public bool DereferenceLinks
    {
        get => _dereferenceLinks;
        set => _dereferenceLinks = value;
    }

    /// <summary>
    /// Gets or sets whether the dialog supports multi-dotted extensions.
    /// </summary>
    public bool SupportMultiDottedExtensions
    {
        get => _supportMultiDottedExtensions;
        set => _supportMultiDottedExtensions = value;
    }

    /// <summary>
    /// Occurs when the user clicks the OK button in the file dialog.
    /// </summary>
    public event EventHandler? FileOk;

    /// <summary>
    /// Raises the FileOk event.
    /// </summary>
    /// <param name="e">An EventArgs that contains the event data.</param>
    protected virtual void OnFileOk(EventArgs e) => FileOk?.Invoke(this, e);

    /// <summary>
    /// Shows the file dialog with no owner window.
    /// </summary>
    /// <returns>One of the DialogResult values.</returns>
    public DialogResult ShowDialog()
    {
        return ShowDialog(owner: null);
    }

    /// <summary>
    /// Shows the file dialog with the specified owner.
    /// </summary>
    /// <param name="owner">The owner window implementing IWin32Window, or null.</param>
    /// <returns>One of the DialogResult values.</returns>
    public DialogResult ShowDialog(INativeWindow? owner)
    {
        Form? ownerForm = null;
        if (owner is Form f)
            ownerForm = f;
        else if (owner != null)
            ownerForm = FindFormFromHandle(owner.NativeHandle);

        DialogResult result;

        bool wasEnabled = ownerForm != null && ownerForm.Enabled;
        if (ownerForm != null)
            ownerForm.Enabled = false;

        try
        {
            OnFileOk(EventArgs.Empty);
            result = RunDialog(ownerForm);
        }
        finally
        {
            if (ownerForm != null)
                ownerForm.Enabled = wasEnabled;
        }

        return result;
    }

    /// <summary>
    /// When overridden in a derived class, shows the actual file dialog.
    /// </summary>
    /// <param name="owner">The owner form, or null.</param>
    /// <returns>One of the DialogResult values.</returns>
    protected abstract DialogResult RunDialog(Form? owner);

    /// <summary>
    /// Resets all dialog properties to their default values.
    /// </summary>
    public virtual void Reset()
    {
        _title = string.Empty;
        _initialDirectory = string.Empty;
        _fileName = string.Empty;
        _fileNames = [];
        _filter = string.Empty;
        _filterIndex = 1;
        _defaultExt = string.Empty;
        _addExtension = true;
        _checkFileExists = false;
        _checkPathExists = true;
        _validateNames = true;
        _dereferenceLinks = true;
        _supportMultiDottedExtensions = false;
    }

    /// <summary>
    /// Parses a WinForms filter string into named entries with patterns.
    /// </summary>
    protected static List<(string Name, List<string> Patterns)> ParseFilter(string filter)
    {
        var result = new List<(string Name, List<string> Patterns)>();
        if (string.IsNullOrEmpty(filter))
            return result;

        string[] parts = filter.Split('|');
        for (int i = 0; i + 1 < parts.Length; i += 2)
        {
            string name = parts[i].Trim();
            string patternStr = parts[i + 1].Trim();
            var patterns = new List<string>(patternStr.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            if (patterns.Count == 0)
                patterns.Add("*.*");
            result.Add((name, patterns));
        }

        if (parts.Length % 2 == 1)
        {
            result.Add((string.Empty, [parts[^1].Trim()]));
        }

        return result;
    }

    private static Form? FindFormFromHandle(nint nativeHandle)
    {
        foreach (var form in Application.Instance?.GetForms() ?? [])
        {
            if (Platform.Platform.GetNativeWindowHandle(form) == nativeHandle)
                return form;
        }
        return null;
    }
}
