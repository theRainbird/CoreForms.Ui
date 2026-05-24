using System.Globalization;
using System.Text.Json;

namespace CoreForms.Ui.Core;

/// <summary>
/// Provides runtime culture switching for the framework.
/// Fires <see cref="CultureChanged"/> when the UI culture is changed,
/// allowing open forms and controls to refresh their localized text.
/// </summary>
public static class LocalizationManager
{
    private static readonly string _settingsPath;

    /// <summary>
    /// Raised after <see cref="CurrentCulture"/> has changed.
    /// Subscribers should re-apply localized strings and invalidate.
    /// </summary>
    public static event EventHandler<CultureInfo>? CultureChanged;

    /// <summary>
    /// Gets the current UI culture used by the framework.
    /// </summary>
    public static CultureInfo CurrentCulture { get; private set; } = CultureInfo.CurrentUICulture;

    static LocalizationManager()
    {
        var basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrEmpty(basePath))
            basePath = Environment.GetEnvironmentVariable("HOME") ?? ".";
        _settingsPath = Path.Combine(basePath, "CoreForms.Ui", "settings.json");
    }

    /// <summary>
    /// Sets the UI culture for the entire application.
    /// Updates <see cref="CultureInfo.CurrentUICulture"/>,
    /// <see cref="CultureInfo.DefaultThreadCurrentUICulture"/>, and raises
    /// <see cref="CultureChanged"/>.
    /// </summary>
    /// <param name="culture">The culture to switch to (e.g. <c>new CultureInfo("de")</c>).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="culture"/> is null.</exception>
    public static void SetCulture(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);

        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CurrentCulture = culture;
        CultureChanged?.Invoke(null, culture);
    }

    /// <summary>
    /// Persists the current culture to the application settings file.
    /// </summary>
    public static void SaveCurrentCulture()
    {
        try
        {
            var dir = Path.GetDirectoryName(_settingsPath);
            if (dir != null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var data = new { culture = CurrentCulture.Name };
            File.WriteAllText(_settingsPath, JsonSerializer.Serialize(data));
        }
        catch
        {
            // Silently ignore – settings persistence is best-effort
        }
    }

    /// <summary>
    /// Loads a previously persisted culture from the application settings file
    /// and applies it via <see cref="SetCulture"/>.
    /// </summary>
    public static void LoadSavedCulture()
    {
        try
        {
            if (!File.Exists(_settingsPath))
                return;

            var json = File.ReadAllText(_settingsPath);
            using var doc = JsonDocument.Parse(json);
            var cultureName = doc.RootElement.GetProperty("culture").GetString();
            if (!string.IsNullOrEmpty(cultureName))
                SetCulture(new CultureInfo(cultureName));
        }
        catch
        {
            // Silently ignore – corrupted settings fall back to default
        }
    }
}
