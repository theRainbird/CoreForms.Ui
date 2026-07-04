using System.Resources;
using System.Globalization;
using System.Reflection;

namespace OldSchoolForms.Ui.Core;

/// <summary>
/// Provides localized string resources for the framework.
/// </summary>
internal static class LangRes
{
    private static readonly ResourceManager _manager =
        new("OldSchoolForms.Ui.Resources.Localization.Messages", typeof(LangRes).Assembly);

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