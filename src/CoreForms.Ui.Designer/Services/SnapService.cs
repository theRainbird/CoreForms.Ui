using System;
using System.Collections.Generic;
using CoreForms.Ui.Core;

namespace CoreForms.Ui.Designer.Services;

/// <summary>
/// Describes a single smart guide line to render on the design surface.
/// </summary>
public class GuideLine
{
    /// <summary>
    /// Gets the start point of the guide line (in surface coordinates).
    /// </summary>
    public Point Start { get; }

    /// <summary>
    /// Gets the end point of the guide line.
    /// </summary>
    public Point End { get; }

    /// <summary>
    /// Initializes a new guide line.
    /// </summary>
    public GuideLine(Point start, Point end)
    {
        Start = start;
        End = end;
    }
}

/// <summary>
/// Smart guide alignment data returned by the snap engine.
/// Contains offset deltas to apply and guide lines to render.
/// </summary>
public class SnapResult
{
    /// <summary>
    /// Gets the X offset to apply for snapping.
    /// </summary>
    public int SnapX { get; set; }

    /// <summary>
    /// Gets the Y offset to apply for snapping.
    /// </summary>
    public int SnapY { get; set; }

    /// <summary>
    /// Gets the list of guide lines to render.
    /// </summary>
    public List<GuideLine> GuideLines { get; } = new();

    /// <summary>
    /// Gets whether any snap was applied.
    /// </summary>
    public bool HasSnap => SnapX != 0 || SnapY != 0;
}

/// <summary>
/// Detects alignment between a dragged control and its siblings.
/// Produces snap offsets and guide lines for the design surface.
/// </summary>
public class SnapService
{
    private const int SnapThreshold = 6;

    /// <summary>
    /// Computes snap alignment for the given control bounds against a list of sibling bounds.
    /// </summary>
    /// <param name="movingBounds">The current bounds of the control being moved/resized.</param>
    /// <param name="originalBounds">The bounds of the control before movement started.</param>
    /// <param name="siblingBounds">Bounds of all other controls on the surface.</param>
    /// <returns>A SnapResult with offsets and guide lines.</returns>
    public SnapResult ComputeSnap(Rectangle movingBounds, Rectangle originalBounds, IReadOnlyList<Rectangle> siblingBounds)
    {
        var result = new SnapResult();

        if (siblingBounds.Count == 0)
            return result;

        int bestSnapX = 0, bestSnapY = 0;
        int bestDistX = SnapThreshold, bestDistY = SnapThreshold;

        foreach (var sibling in siblingBounds)
        {
            if (AreEqual(sibling, movingBounds)) continue;

            // Horizontal alignments (snap X)
            TrySnap(movingBounds.X, sibling.X, ref bestSnapX, ref bestDistX);
            TrySnap(movingBounds.X, sibling.Right, ref bestSnapX, ref bestDistX);
            TrySnap(movingBounds.Right, sibling.X, ref bestSnapX, ref bestDistX);
            TrySnap(movingBounds.Right, sibling.Right, ref bestSnapX, ref bestDistX);

            // Horizontal center
            int mc = movingBounds.X + movingBounds.Width / 2;
            int sc = sibling.X + sibling.Width / 2;
            TrySnap(mc, sc, ref bestSnapX, ref bestDistX);

            // Vertical alignments (snap Y)
            TrySnap(movingBounds.Y, sibling.Y, ref bestSnapY, ref bestDistY);
            TrySnap(movingBounds.Y, sibling.Bottom, ref bestSnapY, ref bestDistY);
            TrySnap(movingBounds.Bottom, sibling.Y, ref bestSnapY, ref bestDistY);
            TrySnap(movingBounds.Bottom, sibling.Bottom, ref bestSnapY, ref bestDistY);

            // Vertical center
            int mcy = movingBounds.Y + movingBounds.Height / 2;
            int scy = sibling.Y + sibling.Height / 2;
            TrySnap(mcy, scy, ref bestSnapY, ref bestDistY);
        }

        if (bestSnapX != 0)
        {
            result.SnapX = bestSnapX;
            AddHorizontalGuides(result, movingBounds, bestSnapX, siblingBounds);
        }

        if (bestSnapY != 0)
        {
            result.SnapY = bestSnapY;
            AddVerticalGuides(result, movingBounds, bestSnapY, siblingBounds);
        }

        return result;
    }

    private static void TrySnap(int movingCoord, int siblingCoord, ref int bestSnap, ref int bestDist)
    {
        int diff = siblingCoord - movingCoord;
        int absDiff = Math.Abs(diff);

        if (absDiff < bestDist)
        {
            bestDist = absDiff;
            bestSnap = diff;
        }
    }

    private static void AddHorizontalGuides(SnapResult result, Rectangle bounds, int snapX, IReadOnlyList<Rectangle> siblings)
    {
        int snappedX = bounds.X + snapX;
        int snappedRight = snappedX + bounds.Width;

        foreach (var sibling in siblings)
        {
            if (AreEqual(sibling, bounds)) continue;

            // Left edge match
            if (Math.Abs(snappedX - sibling.X) <= 1)
            {
                int lineY = Math.Min(bounds.Y, sibling.Y);
                int lineH = Math.Max(bounds.Bottom, sibling.Bottom) - lineY;
                result.GuideLines.Add(new GuideLine(
                    new Point(snappedX, lineY),
                    new Point(snappedX, lineY + lineH)));
            }

            // Right edge match
            if (Math.Abs(snappedRight - sibling.Right) <= 1)
            {
                int lineY = Math.Min(bounds.Y, sibling.Y);
                int lineH = Math.Max(bounds.Bottom, sibling.Bottom) - lineY;
                result.GuideLines.Add(new GuideLine(
                    new Point(snappedRight, lineY),
                    new Point(snappedRight, lineY + lineH)));
            }

            // Left vs right
            if (Math.Abs(snappedX - sibling.Right) <= 1)
            {
                result.GuideLines.Add(new GuideLine(
                    new Point(snappedX, bounds.Y),
                    new Point(snappedX, bounds.Bottom)));
            }

            if (Math.Abs(snappedRight - sibling.X) <= 1)
            {
                result.GuideLines.Add(new GuideLine(
                    new Point(snappedRight, bounds.Y),
                    new Point(snappedRight, bounds.Bottom)));
            }

            // Center match
            int mc = snappedX + bounds.Width / 2;
            int sc = sibling.X + sibling.Width / 2;
            if (Math.Abs(mc - sc) <= 1)
            {
                int lineY = Math.Min(bounds.Y, sibling.Y);
                int lineH = Math.Max(bounds.Bottom, sibling.Bottom) - lineY;
                result.GuideLines.Add(new GuideLine(
                    new Point(mc, lineY),
                    new Point(mc, lineY + lineH)));
            }
        }
    }

    private static bool AreEqual(Rectangle a, Rectangle b)
    {
        return a.X == b.X && a.Y == b.Y && a.Width == b.Width && a.Height == b.Height;
    }

    private static void AddVerticalGuides(SnapResult result, Rectangle bounds, int snapY, IReadOnlyList<Rectangle> siblings)
    {
        int snappedY = bounds.Y + snapY;
        int snappedBottom = snappedY + bounds.Height;

        foreach (var sibling in siblings)
        {
            if (AreEqual(sibling, bounds)) continue;

            if (Math.Abs(snappedY - sibling.Y) <= 1)
            {
                int lineX = Math.Min(bounds.X, sibling.X);
                int lineW = Math.Max(bounds.Right, sibling.Right) - lineX;
                result.GuideLines.Add(new GuideLine(
                    new Point(lineX, snappedY),
                    new Point(lineX + lineW, snappedY)));
            }

            if (Math.Abs(snappedBottom - sibling.Bottom) <= 1)
            {
                int lineX = Math.Min(bounds.X, sibling.X);
                int lineW = Math.Max(bounds.Right, sibling.Right) - lineX;
                result.GuideLines.Add(new GuideLine(
                    new Point(lineX, snappedBottom),
                    new Point(lineX + lineW, snappedBottom)));
            }

            if (Math.Abs(snappedY - sibling.Bottom) <= 1)
            {
                result.GuideLines.Add(new GuideLine(
                    new Point(bounds.X, snappedY),
                    new Point(bounds.Right, snappedY)));
            }

            if (Math.Abs(snappedBottom - sibling.Y) <= 1)
            {
                result.GuideLines.Add(new GuideLine(
                    new Point(bounds.X, snappedBottom),
                    new Point(bounds.Right, snappedBottom)));
            }

            int mcy = snappedY + bounds.Height / 2;
            int scy = sibling.Y + sibling.Height / 2;
            if (Math.Abs(mcy - scy) <= 1)
            {
                int lineX = Math.Min(bounds.X, sibling.X);
                int lineW = Math.Max(bounds.Right, sibling.Right) - lineX;
                result.GuideLines.Add(new GuideLine(
                    new Point(lineX, mcy),
                    new Point(lineX + lineW, mcy)));
            }
        }
    }
}
