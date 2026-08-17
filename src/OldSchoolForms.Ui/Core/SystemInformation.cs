namespace OldSchoolForms.Ui.Core;

/// <summary>
/// Provides system-wide information and metrics, such as double-click timing and distance.
/// </summary>
public static class SystemInformation
{
    /// <summary>
    /// Gets the maximum time interval in milliseconds between two clicks to be considered a double-click.
    /// </summary>
    public static int DoubleClickTime => 500;

    /// <summary>
    /// Gets the maximum movement distance in pixels between two clicks to be considered a double-click.
    /// </summary>
    public static int DoubleClickSize => 4;
}
