namespace CoreForms.Ui.Data;

/// <summary>
/// Provides data for the BindingComplete event.
/// </summary>
public class BindingCompleteEventArgs : EventArgs
{
    /// <summary>
    /// Gets the binding associated with this event.
    /// </summary>
    public Binding Binding { get; }

    /// <summary>
    /// Gets the completion state.
    /// </summary>
    public BindingCompleteState BindingCompleteState { get; }

    /// <summary>
    /// Gets the exception that occurred, if any.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Gets the error text.
    /// </summary>
    public string ErrorText { get; }

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public BindingCompleteEventArgs(Binding binding, BindingCompleteState state, Exception? exception = null, string errorText = "")
    {
        Binding = binding;
        BindingCompleteState = state;
        Exception = exception;
        ErrorText = errorText;
    }
}
