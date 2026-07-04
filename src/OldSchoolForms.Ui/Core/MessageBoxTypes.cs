namespace OldSchoolForms.Ui.Core;

/// <summary>
/// Specifies the buttons to display on a MessageBox.
/// </summary>
public enum MessageBoxButtons
{
    /// <summary>
    /// The message box contains an OK button.
    /// </summary>
    OK,

    /// <summary>
    /// The message box contains OK and Cancel buttons.
    /// </summary>
    OKCancel,

    /// <summary>
    /// The message box contains Yes and No buttons.
    /// </summary>
    YesNo,

    /// <summary>
    /// The message box contains Yes, No, and Cancel buttons.
    /// </summary>
    YesNoCancel,

    /// <summary>
    /// The message box contains Retry and Cancel buttons.
    /// </summary>
    RetryCancel,

    /// <summary>
    /// The message box contains Abort, Retry, and Ignore buttons.
    /// </summary>
    AbortRetryIgnore
}

/// <summary>
/// Specifies the icon to display on a MessageBox.
/// </summary>
public enum MessageBoxIcon
{
    /// <summary>
    /// The message box contains no icon.
    /// </summary>
    None,

    /// <summary>
    /// The message box contains an information icon.
    /// </summary>
    Information,

    /// <summary>
    /// The message box contains a warning icon.
    /// </summary>
    Warning,

    /// <summary>
    /// The message box contains an error icon.
    /// </summary>
    Error,

    /// <summary>
    /// The message box contains a question mark icon.
    /// </summary>
    Question
}

/// <summary>
/// Specifies the default button on a MessageBox.
/// </summary>
public enum MessageBoxDefaultButton
{
    /// <summary>
    /// The first button is the default.
    /// </summary>
    Button1,

    /// <summary>
    /// The second button is the default.
    /// </summary>
    Button2,

    /// <summary>
    /// The third button is the default.
    /// </summary>
    Button3
}

/// <summary>
/// Specifies the result of a dialog box operation.
/// </summary>
public enum DialogResult
{
    /// <summary>
    /// No result from the dialog box.
    /// </summary>
    None,

    /// <summary>
    /// The dialog box return value is OK.
    /// </summary>
    OK,

    /// <summary>
    /// The dialog box return value is Cancel.
    /// </summary>
    Cancel,

    /// <summary>
    /// The dialog box return value is Yes.
    /// </summary>
    Yes,

    /// <summary>
    /// The dialog box return value is No.
    /// </summary>
    No,

    /// <summary>
    /// The dialog box return value is Abort.
    /// </summary>
    Abort,

    /// <summary>
    /// The dialog box return value is Retry.
    /// </summary>
    Retry,

    /// <summary>
    /// The dialog box return value is Ignore.
    /// </summary>
    Ignore
}