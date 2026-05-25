namespace CoreForms.Ui.Demo.UserControls;

/// <summary>
/// Provides data for the <see cref="ITabPageControl.StatusTextChanged"/> event.
/// </summary>
public class StatusTextChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StatusTextChangedEventArgs"/> class.
    /// </summary>
    /// <param name="text">The status text to display.</param>
    public StatusTextChangedEventArgs(string text)
    {
        Text = text;
    }

    /// <summary>
    /// Gets the status text to display in the status bar.
    /// </summary>
    public string Text { get; }
}
