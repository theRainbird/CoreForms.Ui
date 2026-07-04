namespace OldSchoolForms.Ui.Data;

/// <summary>
/// Indicates the completion state of a binding operation.
/// </summary>
public enum BindingCompleteState
{
    /// <summary>The binding operation completed successfully.</summary>
    Success,
    /// <summary>The binding operation raised an exception.</summary>
    Exception,
    /// <summary>The binding operation encountered a data error.</summary>
    DataError
}
