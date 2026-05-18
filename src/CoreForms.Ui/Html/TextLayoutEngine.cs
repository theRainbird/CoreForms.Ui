using CoreForms.Ui.Core;

namespace CoreForms.Ui.Html;

/// <summary>
/// Describes a single laid-out run within a visual line.
/// </summary>
public class LayoutRun
{
    public InlineContent Source { get; init; } = null!;
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public string DisplayText { get; set; } = string.Empty;
    public int StartOffset { get; set; }
    public int Length { get; set; }
    /// <summary>Index of the source in the block's Content list.</summary>
    public int ContentIndex { get; set; }
}

/// <summary>
/// Describes a single visual line resulting from layout.
/// </summary>
public class VisualLine
{
    public List<LayoutRun> Runs { get; } = new();
    public float Y { get; set; }
    public float Height { get; set; }
    public RichTextBlock? Block { get; set; }
    public int BlockIndex { get; set; }
}

/// <summary>
/// Computes visual lines from a <see cref="RichTextDocument"/> with word wrapping.
/// </summary>
public static class TextLayoutEngine
{
    private static (int width, int height) MeasureText(string text, string fontFamily, float fontSize, FontStyle style, float zoom)
    {
        var font = new Font(fontFamily, fontSize, style);
        return Platform.Platform.MeasureText(text, font, zoom);
    }

    /// <summary>
    /// Lays out the document into visual lines for the given width.
    /// </summary>
    /// <param name="doc">The document to lay out.</param>
    /// <param name="availableWidth">The available width in logical pixels.</param>
    /// <param name="zoom">The current zoom factor.</param>
    /// <param name="documentWidth">Receives the total document width.</param>
    /// <param name="documentHeight">Receives the total document height.</param>
    /// <returns>A list of visual lines.</returns>
    public static List<VisualLine> Layout(
        RichTextDocument doc,
        float availableWidth,
        float zoom,
        out float documentWidth,
        out float documentHeight)
    {
        var lines = new List<VisualLine>();
        float y = 0;
        float padding = 4;
        float maxLineWidth = 0;

        for (int bi = 0; bi < doc.Blocks.Count; bi++)
        {
            var block = doc.Blocks[bi];
            float blockFontSize = GetBlockFontSize(block.Type);
            float leftMargin = GetLeftMargin(block.Type);
            float blockTopY = y;

            if (bi > 0)
            {
                if (block.Type is RichTextBlockType.Heading1 or RichTextBlockType.Heading2)
                    y += 8;
                else
                    y += 4;
            }

            blockTopY = y;
            var line = NewLine(block, bi, y, leftMargin);
            bool emptyBlock = block.Content.Count == 0;

            // Marker width for list items
            float markerWidth = 0;
            int numberCounter = CountNumberBefore(doc, bi);
            if (block.Type == RichTextBlockType.BulletItem)
                markerWidth = MeasureText("• ", "Arial", blockFontSize, FontStyle.Regular, zoom).width / zoom;
            else if (block.Type == RichTextBlockType.NumberItem)
                markerWidth = MeasureText($"{numberCounter}. ", "Arial", blockFontSize, FontStyle.Regular, zoom).width / zoom;

            for (int ci = 0; ci < block.Content.Count; ci++)
            {
                var content = block.Content[ci];
                if (content is TextRun run)
                {
                    AppendTextToLine(run, ci, line, lines, block, bi, availableWidth - padding - leftMargin - markerWidth, zoom, ref maxLineWidth);
                }
                else if (content is LineBreakRun)
                {
                    line = NewLine(block, bi, y, leftMargin, markerWidth);
                }
                else if (content is ImageRun image)
                {
                    AppendImageToLine(image, ci, line, lines, block, bi, availableWidth - padding - leftMargin - markerWidth, zoom, ref maxLineWidth);
                }
                else if (content is HyperlinkRun link)
                {
                    foreach (var inner in link.InnerContent)
                    {
                        if (inner is TextRun linkText)
                            AppendTextToLine(linkText, ci, line, lines, block, bi, availableWidth - padding - leftMargin - markerWidth, zoom, ref maxLineWidth);
                        else if (inner is LineBreakRun)
                            line = NewLine(block, bi, y, leftMargin, markerWidth);
                    }
                }

                if (line.Runs.Count > 0)
                {
                    y = line.Y + line.Height;
                }
            }

            if (line.Runs.Count == 0)
            {
                y = blockTopY + blockFontSize * 1.2f;
                line.Y = blockTopY;
                line.Height = blockFontSize * 1.2f;
                lines.Add(line);
            }
        }

        documentWidth = maxLineWidth + padding;
        documentHeight = y + 4;
        return lines;
    }

    /// <summary>
    /// Convert a document position to a flat index.
    /// </summary>
    public static int ToFlatIndex(RichTextDocument doc, int blockIndex, int contentIndex, int charOffset)
    {
        int idx = 0;
        for (int bi = 0; bi < blockIndex && bi < doc.Blocks.Count; bi++)
            foreach (var c in doc.Blocks[bi].Content)
                idx += c.Length;

        if (blockIndex < doc.Blocks.Count)
        {
            var block = doc.Blocks[blockIndex];
            for (int ci = 0; ci < contentIndex && ci < block.Content.Count; ci++)
                idx += block.Content[ci].Length;
            if (contentIndex < block.Content.Count)
            {
                var content = block.Content[contentIndex];
                if (content is TextRun tr)
                    idx += Math.Min(charOffset, tr.Length);
                else if (content is HyperlinkRun link)
                    idx += Math.Min(charOffset, link.Length);
            }
        }
        return idx;
    }

    /// <summary>
    /// Convert a flat index back to a document position.
    /// </summary>
    public static DocumentPosition FromFlatIndex(RichTextDocument doc, int flatIndex)
    {
        int remaining = flatIndex;
        for (int bi = 0; bi < doc.Blocks.Count; bi++)
        {
            var block = doc.Blocks[bi];
            int blockLen = block.Content.Sum(c => c.Length);

            // Boundary between blocks: start of current block
            if (remaining == 0 && bi > 0)
                return new DocumentPosition(bi, 0, 0);

            if (remaining < blockLen || (remaining == blockLen && bi == doc.Blocks.Count - 1))
            {
                // Inside this block or at its end
                for (int ci = 0; ci < block.Content.Count; ci++)
                {
                    var content = block.Content[ci];
                    if (remaining <= content.Length)
                    {
                        int offset = 0;
                        if (content is TextRun tr)
                            offset = Math.Min(remaining, tr.Length);
                        else if (content is HyperlinkRun link)
                            offset = Math.Min(remaining, link.Length);
                        return new DocumentPosition(bi, ci, offset);
                    }
                    remaining -= content.Length;
                }
            }
            remaining -= blockLen;
        }
        // Past end: return last valid position
        if (doc.Blocks.Count > 0)
        {
            var lastBlock = doc.Blocks[^1];
            int lastCi = lastBlock.Content.Count - 1;
            return new DocumentPosition(doc.Blocks.Count - 1, Math.Max(0, lastCi),
                lastCi >= 0 && lastBlock.Content[lastCi] is TextRun tr ? tr.Length : 0);
        }
        return new DocumentPosition(0, 0, 0);
    }

    /// <summary>
    /// Hit-test: find the document position closest to the given pixel coordinates.
    /// </summary>
    public static DocumentPosition HitTest(
        RichTextDocument doc,
        List<VisualLine> lines,
        float px,
        float py,
        float zoom)
    {
        float bestDist = float.MaxValue;
        int bestBlock = 0, bestContent = 0, bestOffset = 0;

        foreach (var line in lines)
        {
            float lineTop = line.Y;
            float lineBottom = lineTop + line.Height;

            if (py >= lineTop - 4 && py < lineBottom + 4)
            {
                // Find run by x
                foreach (var run in line.Runs)
                {
                    if (px >= run.X && px < run.X + run.Width)
                    {
                        if (run.Source is TextRun tr)
                        {
                            float localX = px - run.X;
                            int offset = FindOffsetAtX(run.DisplayText, tr.FontFamily, tr.FontSize, tr.Style, zoom, localX);
                            return new DocumentPosition(line.BlockIndex,
                                line.Block!.Content.IndexOf(run.Source),
                                run.StartOffset + offset);
                        }
                        return new DocumentPosition(line.BlockIndex,
                            line.Block!.Content.IndexOf(run.Source), 0);
                    }
                }

                // After all runs in this line
                if (line.Runs.Count > 0)
                {
                    var last = line.Runs[^1];
                    if (last.Source is TextRun tr2)
                    {
                        int fullOff = last.StartOffset + last.DisplayText.Length;
                        return new DocumentPosition(line.BlockIndex,
                            line.Block!.Content.IndexOf(last.Source), fullOff);
                    }
                    return new DocumentPosition(line.BlockIndex,
                        line.Block!.Content.IndexOf(last.Source), 0);
                }

                // Empty line
                return new DocumentPosition(line.BlockIndex, 0, 0);
            }

            // Track closest line for fallback
            float dist = Math.Abs(py - (lineTop + line.Height / 2));
            if (dist < bestDist)
            {
                bestDist = dist;
                var last2 = line.Runs.Count > 0 ? line.Runs[^1] : null;
                if (last2?.Source is TextRun tr3)
                {
                    bestBlock = line.BlockIndex;
                    bestContent = line.Block!.Content.IndexOf(last2.Source);
                    bestOffset = tr3.Length;
                }
                else if (line.Runs.Count > 0)
                {
                    bestBlock = line.BlockIndex;
                    bestContent = line.Block!.Content.IndexOf(line.Runs[0].Source);
                    bestOffset = 0;
                }
            }
        }

        return new DocumentPosition(bestBlock, bestContent, bestOffset);
    }

    private static int FindOffsetAtX(string text, string fontFamily, float fontSize, FontStyle style, float zoom, float targetX)
    {
        if (string.IsNullOrEmpty(text) || targetX <= 0) return 0;
        int bestPos = 0;
        int bestDist = int.MaxValue;
        for (int i = 0; i <= text.Length; i++)
        {
            string sub = text[..i];
            int w = (int)(MeasureText(sub, fontFamily, fontSize, style, zoom).width / zoom);
            int dist = (int)Math.Abs(targetX - w);
            if (dist < bestDist) { bestDist = dist; bestPos = i; }
        }
        return bestPos;
    }

    private static VisualLine NewLine(RichTextBlock block, int blockIndex, float y, float leftMargin, float markerWidth = 0)
    {
        return new VisualLine
        {
            Block = block,
            BlockIndex = blockIndex,
            Y = y,
            Height = 0,
        };
    }

    private static void AppendTextToLine(
        TextRun run,
        int contentIndex,
        VisualLine line,
        List<VisualLine> lines,
        RichTextBlock block,
        int bi,
        float availableWidth,
        float zoom,
        ref float maxLineWidth)
    {
        string text = run.Text;
        string fontFamily = run.FontFamily;
        float fontSize = run.FontSize;
        var style = run.Style;
        int pos = 0;

        while (pos < text.Length)
        {
            int remaining = text.Length - pos;
            string segment = text.Substring(pos, Math.Min(remaining, 256));

            float segWidth = MeasureText(segment, fontFamily, fontSize, style, zoom).width / zoom;

            float currentLineWidth = 0;
            foreach (var r in line.Runs)
                currentLineWidth += r.Width;

            if (currentLineWidth + segWidth > availableWidth && currentLineWidth > 0)
            {
                // Word-wrap: find last space within width
                int breakPos = FindWordBreak(segment, fontFamily, fontSize, style, zoom, availableWidth - currentLineWidth);
                if (breakPos <= 0) breakPos = 1;

                string wrapText = segment[..breakPos];
                float wrapWidth = MeasureText(wrapText, fontFamily, fontSize, style, zoom).width / zoom;
                float wrapHeight = MeasureText("X", fontFamily, fontSize, style, zoom).height / zoom;

                AddRunToLine(line, run, contentIndex, wrapText, pos, wrapWidth, wrapHeight, ref maxLineWidth);
                lines.Add(line);

                float lineHeight = line.Height;
                line = NewLine(block, bi, line.Y + lineHeight, 0);
                pos += breakPos;
            }
            else
            {
                float runHeight = MeasureText("X", fontFamily, fontSize, style, zoom).height / zoom;
                AddRunToLine(line, run, contentIndex, segment, pos, segWidth, runHeight, ref maxLineWidth);
                pos += segment.Length;
            }
        }

        if (line.Runs.Count > 0 && !lines.Contains(line))
            lines.Add(line);
    }

    private static void AddRunToLine(
        VisualLine line,
        TextRun source,
        int contentIndex,
        string text,
        int startOffset,
        float width,
        float height,
        ref float maxLineWidth)
    {
        float x = 0;
        foreach (var r in line.Runs)
            x += r.Width;

        line.Runs.Add(new LayoutRun
        {
            Source = source,
            ContentIndex = contentIndex,
            X = x,
            Y = 0,
            Width = width,
            Height = height,
            DisplayText = text,
            StartOffset = startOffset,
            Length = text.Length,
        });
        line.Height = Math.Max(line.Height, height);
        maxLineWidth = Math.Max(maxLineWidth, x + width);
    }

    private static void AppendImageToLine(
        ImageRun image,
        int contentIndex,
        VisualLine line,
        List<VisualLine> lines,
        RichTextBlock block,
        int bi,
        float availableWidth,
        float zoom,
        ref float maxLineWidth)
    {
        float imgWidth = 32;
        float imgHeight = 32;
        float currentWidth = 0;
        foreach (var r in line.Runs)
            currentWidth += r.Width;

        if (currentWidth + imgWidth > availableWidth && currentWidth > 0)
        {
            lines.Add(line);
            line = NewLine(block, bi, line.Y + line.Height, 0);
        }

        line.Runs.Add(new LayoutRun
        {
            Source = image,
            ContentIndex = contentIndex,
            X = currentWidth,
            Y = 0,
            Width = imgWidth,
            Height = imgHeight,
            DisplayText = "",
            StartOffset = 0,
            Length = 1,
        });
        line.Height = Math.Max(line.Height, imgHeight);
        maxLineWidth = Math.Max(maxLineWidth, currentWidth + imgWidth);

        if (!lines.Contains(line))
            lines.Add(line);
    }

    private static int FindWordBreak(
        string text,
        string fontFamily,
        float fontSize,
        FontStyle style,
        float zoom,
        float maxWidth)
    {
        for (int i = text.Length; i >= 0; i--)
        {
            string sub = text[..i];
            float w = MeasureText(sub, fontFamily, fontSize, style, zoom).width / zoom;
            if (w <= maxWidth)
                return i;
        }
        return Math.Max(1, text.Length);
    }

    private static float GetBlockFontSize(RichTextBlockType type) => type switch
    {
        RichTextBlockType.Heading1 => 22,
        RichTextBlockType.Heading2 => 18,
        RichTextBlockType.Heading3 => 16,
        RichTextBlockType.Heading4 => 14,
        RichTextBlockType.Heading5 => 12,
        RichTextBlockType.Heading6 => 11,
        _ => 12
    };

    private static float GetLeftMargin(RichTextBlockType type) =>
        type is RichTextBlockType.BulletItem or RichTextBlockType.NumberItem ? 20 : 0;

    private static int CountNumberBefore(RichTextDocument doc, int blockIndex)
    {
        int count = 0;
        for (int i = 0; i < blockIndex && i < doc.Blocks.Count; i++)
        {
            if (doc.Blocks[i].Type == RichTextBlockType.NumberItem)
                count++;
            else if (doc.Blocks[i].Type != RichTextBlockType.BulletItem)
                count = 0;
        }
        return count + 1;
    }
}
