using OldSchoolForms.Ui.Core;

namespace OldSchoolForms.Ui;

/// <summary>
/// Provides centralized coordinate transformation utilities for zoom-aware rendering and hit testing.
/// All control bounds use logical coordinates; this class handles conversions between logical and pixel space.
/// </summary>
public static class CoordinateTransform
{
    /// <summary>
    /// Gets the effective font size scaled by zoom for layout calculations.
    /// </summary>
    /// <param name="font">The font to scale.</param>
    /// <param name="zoom">The zoom factor.</param>
    /// <returns>The scaled font size in logical coordinate units.</returns>
    /// <exception cref="ArgumentNullException">Thrown when font is null.</exception>
    public static float GetScaledFontSize(Font font, float zoom)
    {
        return font.Size * zoom;
    }

    /// <summary>
    /// Calculates the height of a list item based on font size and zoom.
    /// </summary>
    /// <param name="font">The font used for the item.</param>
    /// <param name="zoom">The zoom factor.</param>
    /// <param name="padding">Additional vertical padding. Default is 4.</param>
    /// <returns>The item height in logical coordinate units.</returns>
    public static int GetItemHeight(Font font, float zoom, int padding = 4)
    {
        return (int)(font.Size * zoom) + padding;
    }

    /// <summary>
    /// Calculates the Y position for vertically centering text within a container.
    /// </summary>
    /// <param name="containerHeight">The height of the container in logical units.</param>
    /// <param name="font">The font to center.</param>
    /// <param name="zoom">The zoom factor.</param>
    /// <returns>The Y position for the text baseline in logical coordinate units.</returns>
    public static float CenterVertically(int containerHeight, Font font, float zoom)
    {
        float scaledSize = font.Size * zoom;
        return (containerHeight - scaledSize) / 2f;
    }

    /// <summary>
    /// Calculates the Y position for vertically centering text within a sub-region.
    /// </summary>
    /// <param name="regionY">The Y position of the region in logical units.</param>
    /// <param name="regionHeight">The height of the region in logical units.</param>
    /// <param name="font">The font to center.</param>
    /// <param name="zoom">The zoom factor.</param>
    /// <returns>The Y position for the text baseline in logical coordinate units.</returns>
    public static float CenterVertically(int regionY, int regionHeight, Font font, float zoom)
    {
        float scaledSize = font.Size * zoom;
        return regionY + (regionHeight - scaledSize) / 2f;
    }

    /// <summary>
    /// Measures text dimensions using the platform's font renderer with proper zoom scaling.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="font">The font to use.</param>
    /// <param name="zoom">The zoom factor.</param>
    /// <returns>A tuple containing the width and height in logical coordinate units.</returns>
    public static (int width, int height) MeasureText(string text, Font font, float zoom)
    {
        if (string.IsNullOrEmpty(text))
            return (0, 0);
        return Platform.Platform.MeasureText(text, font, zoom);
    }

    /// <summary>
    /// Converts a logical Y coordinate to the corresponding pixel Y for a dropdown region.
    /// </summary>
    /// <param name="controlBottom">The bottom edge of the control in logical units.</param>
    /// <param name="zoom">The zoom factor.</param>
    /// <returns>The pixel Y coordinate where the dropdown starts.</returns>
    public static int LogicalToPixelY(int controlBottom, float zoom)
    {
        return (int)(controlBottom * zoom);
    }

    /// <summary>
    /// Converts a logical height to pixel height.
    /// </summary>
    /// <param name="logicalHeight">The height in logical units.</param>
    /// <param name="zoom">The zoom factor.</param>
    /// <returns>The height in pixels.</returns>
    public static int LogicalToPixelHeight(int logicalHeight, float zoom)
    {
        return (int)(logicalHeight * zoom);
    }

    /// <summary>
    /// Determines if a scrollbar is needed based on content vs. available space.
    /// </summary>
    /// <param name="contentHeight">The total content height in logical units.</param>
    /// <param name="availableHeight">The available display height in logical units.</param>
    /// <returns>True if a scrollbar is needed; otherwise, false.</returns>
    public static bool NeedsScrollbar(int contentHeight, int availableHeight)
    {
        return contentHeight > availableHeight;
    }

    /// <summary>
    /// Calculates scrollbar thumb position and size.
    /// </summary>
    /// <param name="scrollOffset">The current scroll offset in logical units.</param>
    /// <param name="contentHeight">The total content height in logical units.</param>
    /// <param name="viewHeight">The visible area height in logical units.</param>
    /// <param name="scrollBarHeight">The scrollbar track height in logical units.</param>
    /// <param name="minThumbHeight">Minimum thumb height. Default is 20.</param>
    /// <returns>A tuple containing thumb Y position and thumb height in logical units.</returns>
    public static (int thumbY, int thumbHeight) CalculateScrollbarThumb(
        int scrollOffset, int contentHeight, int viewHeight, int scrollBarHeight, int minThumbHeight = 20)
    {
        int maxScroll = Math.Max(0, contentHeight - viewHeight);
        if (maxScroll <= 0)
            return (0, scrollBarHeight);

        float thumbHeightRatio = (float)viewHeight / contentHeight;
        int thumbHeight = Math.Max(minThumbHeight, (int)(scrollBarHeight * thumbHeightRatio));
        int thumbY = (int)((float)scrollOffset / maxScroll * (scrollBarHeight - thumbHeight));

        return (thumbY, thumbHeight);
    }
}
