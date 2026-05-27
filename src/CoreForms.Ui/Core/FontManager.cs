using SkiaSharp;

namespace CoreForms.Ui.Core;

/// <summary>
/// Provides centralized, memory-efficient font enumeration and typeface management.
/// Uses SKFontManager for lazy-loaded font family names instead of filesystem scanning.
/// </summary>
public static class FontManager
{
    private static string[]? _cachedFamilies;
    private static readonly Dictionary<string, SKTypeface> TypefaceCache = new();

    /// <summary>
    /// Gets all available font family names from the system.
    /// Results are cached; no font files are loaded into memory during enumeration.
    /// </summary>
    /// <returns>An array of font family names, sorted alphabetically.</returns>
    public static string[] GetFontFamilies()
    {
        if (_cachedFamilies != null)
            return _cachedFamilies;

        var families = SKFontManager.Default.GetFontFamilies();
        Array.Sort(families, StringComparer.OrdinalIgnoreCase);
        _cachedFamilies = families;
        return families;
    }

    /// <summary>
    /// Gets an SKTypeface for the specified font family name.
    /// The typeface is lazily loaded and cached for subsequent requests.
    /// No font file is accessed until the first call per family name.
    /// </summary>
    /// <param name="familyName">The font family name.</param>
    /// <returns>An SKTypeface instance, or null if the font cannot be found.</returns>
    public static SKTypeface? GetTypeface(string familyName)
    {
        if (TypefaceCache.TryGetValue(familyName, out var cached))
            return cached;

        try
        {
            var typeface = SKTypeface.FromFamilyName(familyName);
            if (typeface != null)
            {
                TypefaceCache[familyName] = typeface;
                return typeface;
            }
        }
        catch
        {
            // Ignore lookup failures
        }

        return null;
    }

    /// <summary>
    /// Filters font families by the given search text (case-insensitive start-with match).
    /// </summary>
    /// <param name="searchText">The text to filter by.</param>
    /// <returns>A filtered array of matching family names.</returns>
    public static string[] SearchFontFamilies(string searchText)
    {
        var families = GetFontFamilies();
        if (string.IsNullOrEmpty(searchText))
            return families;

        return families
            .Where(f => f.StartsWith(searchText, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    /// <summary>
    /// Clears all cached typefaces. Call this to free memory when fonts are no longer needed.
    /// </summary>
    public static void ClearCache()
    {
        foreach (var typeface in TypefaceCache.Values)
            typeface.Dispose();
        TypefaceCache.Clear();
    }

    /// <summary>
    /// Invalidates the family name cache so the next call to <see cref="GetFontFamilies"/> re-enumerates.
    /// </summary>
    public static void RefreshFontList()
    {
        _cachedFamilies = null;
    }
}
