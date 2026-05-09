using System.Runtime.InteropServices;

namespace CoreForms.Ui.Core;

/// <summary>
/// Provides system DPI detection and zoom-related utilities.
/// </summary>
public static class Dpi
{
    private static float? _cachedDpi;

    /// <summary>
    /// Gets the system DPI value. Returns 96 if detection fails.
    /// </summary>
    public static float GetSystemDpi()
    {
        if (_cachedDpi.HasValue)
            return _cachedDpi.Value;

        float dpi = 96f;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                dpi = GetWindowsDpi();
            }
            catch
            {
                dpi = 96f;
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            try
            {
                dpi = GetLinuxDpi();
            }
            catch
            {
                dpi = 96f;
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            dpi = GetOsxDpi();
        }

        _cachedDpi = dpi;
        return dpi;
    }

    /// <summary>
    /// Gets the default zoom factor based on system DPI. 96 DPI = 1.0 (100%).
    /// </summary>
    public static float GetDefaultZoom() => GetSystemDpi() / 96f;

    /// <summary>
    /// Minimum allowed zoom value.
    /// </summary>
    public const float MinZoom = 0.25f;

    /// <summary>
    /// Maximum allowed zoom value.
    /// </summary>
    public const float MaxZoom = 4.0f;

    /// <summary>
    /// Clamps a zoom value to the allowed range.
    /// </summary>
    /// <param name="zoom">The zoom value to clamp.</param>
    /// <returns>The clamped zoom value.</returns>
    public static float ClampZoom(float zoom) => Math.Clamp(zoom, MinZoom, MaxZoom);

    private static float GetWindowsDpi()
    {
        return 96f;
    }

    private static float GetLinuxDpi()
    {
        try
        {
            var result = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "xrandr",
                Arguments = "--current",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (result != null)
            {
                using var reader = result.StandardOutput;
                var output = reader.ReadToEnd();
                result.WaitForExit();

                var lines = output.Split('\n');
                foreach (var line in lines)
                {
                    if (line.Contains("connected") && line.Contains("x"))
                    {
                        var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var part in parts)
                        {
                            if (part.Contains("x"))
                            {
                                var dims = part.Split('x');
                                if (dims.Length == 2 && int.TryParse(dims[0], out int width) && int.TryParse(dims[1], out int height))
                                {
                                    float dpi = width * 25.4f / GetMonitorWidthInMm(line);
                                    if (dpi >= 50 && dpi <= 500)
                                        return dpi;
                                }
                            }
                        }
                    }
                }
            }
        }
        catch { }

        var gdkScale = Environment.GetEnvironmentVariable("GDK_DPI_SCALE");
        if (!string.IsNullOrEmpty(gdkScale) && float.TryParse(gdkScale, out float scale))
            return 96f * scale;

        var gtkScale = Environment.GetEnvironmentVariable("GTK_SCALE");
        if (!string.IsNullOrEmpty(gtkScale) && float.TryParse(gtkScale, out float gtk))
            return 96f * gtk;

        return 96f;
    }

    private static float GetMonitorWidthInMm(string xrandrLine)
    {
        var match = System.Text.RegularExpressions.Regex.Match(xrandrLine, @"(\d+)mm");
        if (match.Success && int.TryParse(match.Groups[1].Value, out int widthMm) && widthMm > 0)
            return widthMm;
        return 400f;
    }

    private static float GetOsxDpi()
    {
        return 96f;
    }
}