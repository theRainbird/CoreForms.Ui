namespace OldSchoolForms.Ui.Core;

/// <summary>
/// Provides static methods for accessing the system clipboard.
/// </summary>
public static class Clipboard
{
    /// <summary>
    /// Gets the text content from the clipboard.
    /// </summary>
    /// <returns>The clipboard text, or null if the clipboard is empty or contains non-text data.</returns>
    public static string? GetText() => Platform.Platform.GetClipboardText();

    /// <summary>
    /// Sets the text content of the clipboard.
    /// </summary>
    /// <param name="text">The text to copy to the clipboard.</param>
    /// <exception cref="ArgumentNullException">Thrown when text is null.</exception>
    public static void SetText(string? text)
    {
        if (text == null)
            throw new ArgumentNullException(nameof(text));
        Platform.Platform.SetClipboardText(text);
    }

    /// <summary>
    /// Determines whether the clipboard contains text data.
    /// </summary>
    /// <returns>True if the clipboard contains text; otherwise, false.</returns>
    public static bool ContainsText() => !string.IsNullOrEmpty(GetText());
}