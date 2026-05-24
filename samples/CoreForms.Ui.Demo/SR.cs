using System.Resources;
using System.Globalization;

namespace CoreForms.Ui.Demo;

/// <summary>
/// Provides localized string resources for the demo application.
/// </summary>
internal static class SR
{
    private static readonly ResourceManager _manager =
        new("CoreForms.Ui.Demo.Resources.Localization.DemoMessages", typeof(SR).Assembly);

    /// <summary>
    /// Gets the localized string for the specified key using the current UI culture.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <returns>The localized string, or the key itself if not found.</returns>
    public static string GetString(string key)
    {
        return _manager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
    }
}
