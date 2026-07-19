using OldSchoolForms.Ui.Core;

namespace OldSchoolForms.Ui.Html;

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
    /// <summary>For table cells, the cell index (row*colCount+col). -1 for non-table runs.</summary>
    public int CellIndex { get; set; } = -1;
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
    /// <summary>Horizontal padding inside table cells, in pixels.</summary>
    public const float CellPadding = 4;

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

            // --- Table block layout ---
            if (block.Type == RichTextBlockType.Table && block.Rows != null && block.Rows.Count > 0)
            {
                float tablePadding = 4;
                float availForTable = availableWidth - padding;

                // Compute column widths (auto-fit)
                int colCount = block.ColCount;
                float[] colWidths = new float[colCount];
                float totalContentWidth = 0;

                for (int c = 0; c < colCount; c++)
                {
                    float maxColWidth = 0;
                    foreach (var row in block.Rows)
                    {
                        if (c < row.Cells.Count)
                        {
                            var cell = row.Cells[c];
                            float cellWidth = 0;
                            foreach (var ci in cell.Content)
                            {
                                if (ci is TextRun tr)
                                    cellWidth += MeasureText(tr.Text, tr.FontFamily, tr.FontSize, tr.Style, zoom).width / zoom;
                                else if (ci is ImageRun)
                                    cellWidth += 24;
                            }
                            if (cellWidth > maxColWidth) maxColWidth = cellWidth;
                        }
                    }
                    colWidths[c] = Math.Max(maxColWidth, 20);
                    totalContentWidth += colWidths[c];
                }

                // Distribute remaining space proportionally
                float gridLineWidth = colCount + 1; // 1px per grid line
                float totalNeeded = totalContentWidth + gridLineWidth;
                if (totalNeeded > availForTable)
                {
                    float scale = availForTable / totalNeeded;
                    for (int c = 0; c < colCount; c++)
                        colWidths[c] = Math.Max(colWidths[c] * scale, 10);
                }
                else if (totalNeeded < availForTable)
                {
                    float extra = (availForTable - totalNeeded) / colCount;
                    for (int c = 0; c < colCount; c++)
                        colWidths[c] += extra;
                }

                // Layout each row
                for (int ri = 0; ri < block.Rows.Count; ri++)
                {
                    var row = block.Rows[ri];
                    float rowHeight = 0;
                    float[] cellHeights = new float[colCount];

                    // Layout each cell's content to determine height
                    for (int c = 0; c < colCount && c < row.Cells.Count; c++)
                    {
                        var cell = row.Cells[c];
                        float cellWidth = colWidths[c];
                        float cellContentHeight = 0;

                        foreach (var ci in cell.Content)
                        {
                            if (ci is TextRun tr)
                            {
                                float textWidth = MeasureText(tr.Text, tr.FontFamily, tr.FontSize, tr.Style, zoom).width / zoom;
                                float lineCount = Math.Max(1, textWidth <= cellWidth ? 1 : (float)Math.Ceiling(textWidth / cellWidth));
                                cellContentHeight += lineCount * (tr.FontSize * 1.4f);
                            }
                            else if (ci is LineBreakRun)
                            {
                                cellContentHeight += 12 * 1.4f;
                            }
                            else if (ci is ImageRun)
                            {
                                cellContentHeight += 24;
                            }
                            else if (ci is HyperlinkRun link)
                            {
                                foreach (var inner in link.InnerContent)
                                {
                                    if (inner is TextRun lt)
                                    {
                                        float tw = MeasureText(lt.Text, lt.FontFamily, lt.FontSize, lt.Style, zoom).width / zoom;
                                        float lc = Math.Max(1, tw <= cellWidth ? 1 : (float)Math.Ceiling(tw / cellWidth));
                                        cellContentHeight += lc * (lt.FontSize * 1.4f);
                                    }
                                }
                            }
                        }
                        cellHeights[c] = Math.Max(cellContentHeight, 20);
                        if (cellHeights[c] > rowHeight) rowHeight = cellHeights[c];
                    }

                    // Create visual line for this row
                    var rowLine = new VisualLine
                    {
                        Block = block,
                        BlockIndex = bi,
                        Y = y,
                        Height = rowHeight + 2
                    };

                    float cellX = 0;
                    for (int c = 0; c < colCount && c < row.Cells.Count; c++)
                    {
                        var cell = row.Cells[c];
                        int cellIdx = ri * colCount + c;

                        // Build display text from cell content
                        string cellText = "";
                        foreach (var ci in cell.Content)
                        {
                            if (ci is TextRun tr)
                                cellText += tr.Text;
                            else if (ci is LineBreakRun)
                                cellText += "\n";
                            else if (ci is HyperlinkRun link)
                                foreach (var inner in link.InnerContent)
                                    if (inner is TextRun linkText)
                                        cellText += linkText.Text;
                        }

                        // Add a layout run for the cell (for hit-testing and rendering)
                        rowLine.Runs.Add(new LayoutRun
                        {
                            Source = cell.Content.Count > 0 ? cell.Content[0] : new TextRun(),
                            ContentIndex = 0,
                            CellIndex = cellIdx,
                            X = cellX + CellPadding,
                            Y = 0,
                            Width = colWidths[c],
                            Height = rowHeight,
                            DisplayText = cellText,
                            StartOffset = 0,
                            Length = cellText.Length
                        });

                        cellX += colWidths[c] + 1; // 1px gap for grid line
                    }

                    lines.Add(rowLine);
                    y += rowLine.Height;

                    float rowWidth = cellX;
                    if (rowWidth > maxLineWidth) maxLineWidth = rowWidth;
                }

                // Add spacing after table
                y += 6;
                continue;
            }
            // --- End table layout ---

            // Add spacing between previous block and this block BEFORE creating the line,
            // so line.Y includes the gap and y = line.Y + line.Height preserves it.
            if (bi > 0)
            {
                var prev = doc.Blocks[bi - 1];
                bool prevIsList = prev.Type is RichTextBlockType.BulletItem or RichTextBlockType.NumberItem;
                bool currIsList = block.Type is RichTextBlockType.BulletItem or RichTextBlockType.NumberItem;

                if (prevIsList && currIsList)
                {
                    y += 0; // between list items: no gap
                }
                else if (prev.Type is RichTextBlockType.Heading1 or RichTextBlockType.Heading2)
                {
                    y += 14; // after heading
                }
                else if (prevIsList || currIsList)
                {
                    y += 10; // before or after a list (but not between items)
                }
                else
                {
                    y += 6;
                }
            }

            blockTopY = y;
            var line = NewLine(block, bi, y, leftMargin);
            bool emptyBlock = block.Content.Count == 0;

            // Marker width for list items
            float markerWidth = 0;
            int numberCounter = CountNumberBefore(doc, bi);
            if (block.Type == RichTextBlockType.BulletItem)
                markerWidth = MeasureText("* ", "Arial", blockFontSize, FontStyle.Regular, zoom).width / zoom;
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
                y = blockTopY + blockFontSize * 1.4f;
                line.Y = blockTopY;
                line.Height = blockFontSize * 1.4f;
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
    /// <param name="doc">The document.</param>
    /// <param name="blockIndex">Block index.</param>
    /// <param name="contentIndex">Index into block content list (or cell content list for table cells).</param>
    /// <param name="charOffset">Character offset within a TextRun.</param>
    /// <param name="cellIndex">For table blocks, the cell index (row*colCount+col). -1 for non-table or block-level queries.</param>
    public static int ToFlatIndex(RichTextDocument doc, int blockIndex, int contentIndex, int charOffset, int cellIndex = -1)
    {
        if (blockIndex < 0 || blockIndex >= doc.Blocks.Count) return 0;
        var block = doc.Blocks[blockIndex];

        int idx = 0;
        for (int bi = 0; bi < blockIndex; bi++)
        {
            var b = doc.Blocks[bi];
            if (b.Type == RichTextBlockType.Table && b.Rows != null)
            {
                foreach (var row in b.Rows)
                    foreach (var cell in row.Cells)
                        idx += cell.Content.Sum(c => c.Length);
            }
            else
            {
                foreach (var c in b.Content)
                    idx += c.Length;
            }
        }

        if (block.Type == RichTextBlockType.Table && block.Rows != null)
        {
            if (cellIndex >= 0)
            {
                // Sum content of all cells before the target cell
                int totalCells = block.Rows.Count * block.ColCount;
                for (int i = 0; i < cellIndex && i < totalCells; i++)
                {
                    int r = i / block.ColCount;
                    int c = i % block.ColCount;
                    if (r < block.Rows.Count && c < block.Rows[r].Cells.Count)
                        idx += block.Rows[r].Cells[c].Content.Sum(ci => ci.Length);
                }

                // Add content offset within the target cell
                int targetRow = cellIndex / block.ColCount;
                int tc = cellIndex % block.ColCount;
                if (targetRow < block.Rows.Count && tc < block.Rows[targetRow].Cells.Count)
                {
                    var targetCell = block.Rows[targetRow].Cells[tc];
                    for (int ci = 0; ci < contentIndex && ci < targetCell.Content.Count; ci++)
                        idx += targetCell.Content[ci].Length;
                    if (contentIndex >= 0 && contentIndex < targetCell.Content.Count && targetCell.Content[contentIndex] is TextRun tr2)
                        idx += Math.Max(0, Math.Min(charOffset, tr2.Length));
                }
                return idx;
            }

            // Block-level query without cellIndex: return index before this table
            return idx;
        }

        if (contentIndex < 0 || contentIndex >= block.Content.Count) return idx;

        for (int ci = 0; ci < contentIndex; ci++)
            idx += block.Content[ci].Length;

        var content = block.Content[contentIndex];
        if (content is TextRun tr)
            idx += Math.Max(0, Math.Min(charOffset, tr.Length));
        else if (content is HyperlinkRun link)
        {
            int accumulated = 0;
            for (int i = 0; i < link.InnerContent.Count; i++)
            {
                var inner = link.InnerContent[i];
                if (charOffset <= accumulated + inner.Length)
                {
                    idx += Math.Max(0, charOffset - accumulated);
                    break;
                }
                accumulated += inner.Length;
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
            int blockLen;

            if (block.Type == RichTextBlockType.Table && block.Rows != null)
            {
                blockLen = 0;
                foreach (var row in block.Rows)
                    foreach (var cell in row.Cells)
                        blockLen += cell.Content.Sum(c => c.Length);
            }
            else
            {
                blockLen = block.Content.Sum(c => c.Length);
            }

            // Boundary between blocks: start of current block
            if (remaining == 0 && bi > 0)
                return new DocumentPosition(bi, 0, 0);

            if (remaining < blockLen || (remaining == blockLen && bi == doc.Blocks.Count - 1))
            {
                // Inside this block or at its end
                if (block.Type == RichTextBlockType.Table && block.Rows != null)
                {
                    // Find the cell containing this flat index
                    int totalCells = block.Rows.Count * block.ColCount;
                    for (int i = 0; i < totalCells; i++)
                    {
                        int r = i / block.ColCount;
                        int c = i % block.ColCount;
                        if (r < block.Rows.Count && c < block.Rows[r].Cells.Count)
                        {
                            var cell = block.Rows[r].Cells[c];
                            int cellLen = cell.Content.Sum(ci => ci.Length);
                            if (remaining < cellLen)
                            {
                                for (int ci = 0; ci < cell.Content.Count; ci++)
                                {
                                    var content = cell.Content[ci];
                                    if (remaining <= content.Length)
                                    {
                                        int offset = 0;
                                        if (content is TextRun tr)
                                            offset = Math.Min(remaining, tr.Length);
                                        return new DocumentPosition(bi, ci, offset);
                                    }
                                    remaining -= content.Length;
                                }
                                return new DocumentPosition(bi, 0, 0);
                            }
                            remaining -= cellLen;
                        }
                    }
                    return new DocumentPosition(bi, 0, 0);
                }

                for (int ci = 0; ci < block.Content.Count; ci++)
                {
                    var content = block.Content[ci];
                    if (remaining <= content.Length)
                    {
                        int offset = 0;
                        if (content is TextRun tr)
                            offset = Math.Min(remaining, tr.Length);
                        else if (content is HyperlinkRun link)
                        {
                            int acc = 0;
                            for (int i = 0; i < link.InnerContent.Count; i++)
                            {
                                var inner = link.InnerContent[i];
                                if (remaining <= acc + inner.Length)
                                {
                                    offset = Math.Max(0, remaining - acc);
                                    break;
                                }
                                acc += inner.Length;
                            }
                        }
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
    /// <param name="doc">The document being hit-tested.</param>
    /// <param name="lines">The laid-out visual lines.</param>
    /// <param name="px">The mouse X coordinate in control client area.</param>
    /// <param name="py">The mouse Y coordinate in control client area.</param>
    /// <param name="zoom">The current zoom factor.</param>
    /// <param name="padding">The content area padding (typically 8).</param>
    /// <returns>The document position closest to the given coordinates.</returns>
    public static DocumentPosition HitTest(
        RichTextDocument doc,
        List<VisualLine> lines,
        float px,
        float py,
        float zoom,
        out LayoutRun? hitRun,
        float padding = 8)
    {
        hitRun = null;
        float bestDist = float.MaxValue;
        int bestBlock = 0, bestContent = 0, bestOffset = 0;

 // Convert control X coordinates to content-area coordinates
        // (layout stores run X positions relative to content area, not control origin)
        foreach (var line in lines)
        {
            float lineLeftMargin = GetLeftMargin(line.Block?.Type ?? RichTextBlockType.Paragraph);
            float cx = px - padding - lineLeftMargin;
            float lineTop = line.Y;
            float lineBottom = lineTop + line.Height;

            if (py >= lineTop - 4 && py < lineBottom + 4)
            {
                // Find run by x
                foreach (var run in line.Runs)
                {
                    if (cx >= run.X && cx < run.X + run.Width)
                    {
                        hitRun = run;

                        // Table cell hit — ContentIndex = 0, charOff = offset within cell
                        if (run.CellIndex >= 0 && line.Block?.Type == RichTextBlockType.Table && line.Block.Rows != null)
                        {
                            int totalCols = line.Block.ColCount;
                            int totalRows = line.Block.Rows.Count;
                            int targetRow = run.CellIndex / totalCols;
                            int targetCol = run.CellIndex % totalCols;
                            if (targetRow < totalRows && targetCol < line.Block.Rows[targetRow].Cells.Count)
                            {
                                int charOff = run.StartOffset;
                                if (run.Source is TextRun trCell)
                                {
                                    float localX = cx - run.X;
                                    int offset = FindOffsetAtX(run.DisplayText, trCell.FontFamily, trCell.FontSize, trCell.Style, zoom, localX);
                                    charOff = run.StartOffset + offset;
                                }
                                return new DocumentPosition(line.BlockIndex, 0, charOff);
                            }
                            return new DocumentPosition(line.BlockIndex, 0, 0);
                        }

                        if (run.Source is TextRun tr)
                        {
                            float localX = cx - run.X;
                            int offset = FindOffsetAtX(run.DisplayText, tr.FontFamily, tr.FontSize, tr.Style, zoom, localX);
                            return new DocumentPosition(line.BlockIndex,
                                line.Block!.Content.IndexOf(run.Source),
                                run.StartOffset + offset);
                        }
                        hitRun = run;
                        return new DocumentPosition(line.BlockIndex,
                            line.Block!.Content.IndexOf(run.Source), 0);
                    }
                }

                // After all runs in this line
                if (line.Runs.Count > 0)
                {
                    var last = line.Runs[^1];
                    hitRun = last;
                    if (last.CellIndex >= 0 && line.Block?.Type == RichTextBlockType.Table && line.Block.Rows != null)
                    {
                        int fullOff = last.StartOffset + last.DisplayText.Length;
                        return new DocumentPosition(line.BlockIndex, 0, fullOff);
                    }
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
                if (last2 != null && last2.CellIndex >= 0 && line.Block?.Type == RichTextBlockType.Table && line.Block.Rows != null)
                {
                    bestBlock = line.BlockIndex;
                    bestContent = 0;
                    if (last2.Source is TextRun trfb) bestOffset = trfb.Length;
                    else bestOffset = 0;
                    hitRun = last2;
                }
                else if (last2?.Source is TextRun tr3)
                {
                    bestBlock = line.BlockIndex;
                    bestContent = line.Block!.Content.IndexOf(last2.Source);
                    bestOffset = tr3.Length;
                    hitRun = last2;
                }
                else if (line.Runs.Count > 0)
                {
                    var first = line.Runs[0];
                    if (first.CellIndex >= 0 && line.Block?.Type == RichTextBlockType.Table && line.Block.Rows != null)
                    {
                        bestBlock = line.BlockIndex;
                        bestContent = 0;
                        bestOffset = 0;
                        hitRun = first;
                    }
                    else
                    {
                        bestBlock = line.BlockIndex;
                        bestContent = line.Block!.Content.IndexOf(line.Runs[0].Source);
                        bestOffset = 0;
                        hitRun = line.Runs[0];
                    }
                }
            }
        }

        return new DocumentPosition(bestBlock, bestContent, bestOffset);
    }

   private static int FindOffsetAtX(string text, string fontFamily, float fontSize, FontStyle style, float zoom, float targetX)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        if (targetX <= 0) return 0;

        for (int i = 1; i <= text.Length; i++)
        {
            string sub = text[..i];
            float w = MeasureText(sub, fontFamily, fontSize, style, zoom).width / zoom;
            if (w >= targetX)
            {
                return i;
            }
        }
        return text.Length;
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
                float wrapHeight = fontSize * 1.4f;

                AddRunToLine(line, run, contentIndex, wrapText, pos, wrapWidth, wrapHeight, ref maxLineWidth);
                lines.Add(line);

                float lineHeight = line.Height;
                line = NewLine(block, bi, line.Y + lineHeight, 0);
                pos += breakPos;
            }
            else
            {
                float runHeight = fontSize * 1.4f;
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
        float imgWidth = 24;
        float imgHeight = 24;
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
        RichTextBlockType.Heading1 => 24,
        RichTextBlockType.Heading2 => 20,
        RichTextBlockType.Heading3 => 18,
        RichTextBlockType.Heading4 => 16,
        RichTextBlockType.Heading5 => 14,
        RichTextBlockType.Heading6 => 12,
        _ => 12
    };

    private static float GetLeftMargin(RichTextBlockType type) =>
        type is RichTextBlockType.BulletItem or RichTextBlockType.NumberItem ? 30 : 0;

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
