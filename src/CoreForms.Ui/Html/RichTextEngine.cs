using CoreForms.Ui.Core;

namespace CoreForms.Ui.Html;

/// <summary>
/// Provides editing operations over a <see cref="RichTextDocument"/>.
/// Maintains cursor and selection state.
/// </summary>
public class RichTextEngine
{
    /// <summary>The underlying document.</summary>
    public RichTextDocument Document { get; } = new();

    public int CursorBlock { get; set; }
    public int CursorContent { get; set; }
    public int CursorOffset { get; set; }

    public int SelectionBlock { get; set; }
    public int SelectionContent { get; set; }
    public int SelectionOffset { get; set; }

    /// <summary>Convenience alias for test compatibility.</summary>
    public int CursorRun { get => CursorContent; set => CursorContent = value; }
    /// <summary>Convenience alias for test compatibility.</summary>
    public int SelectionRun { get => SelectionContent; set => SelectionContent = value; }

    public int CursorFlatIndex => TextLayoutEngine.ToFlatIndex(Document, CursorBlock, CursorContent, CursorOffset);
    public int SelectionFlatIndex => TextLayoutEngine.ToFlatIndex(Document, SelectionBlock, SelectionContent, SelectionOffset);
    public bool HasSelection => CursorFlatIndex != SelectionFlatIndex;

    /// <summary>
    /// Loads the document from an HTML string.
    /// </summary>
    public void InitFromHtml(string html)
    {
        var parsed = HtmlImport.Parse(html);
        Document.Blocks.Clear();
        foreach (var b in parsed.Blocks)
            Document.Blocks.Add(b);
        ResetCursor();
    }

    /// <summary>
    /// Serializes the document back to HTML.
    /// </summary>
    public string ToHtml() => HtmlExport.ToHtml(Document);

    public void ResetCursor()
    {
        CursorBlock = 0;
        CursorContent = 0;
        CursorOffset = 0;
        SelectionBlock = 0;
        SelectionContent = 0;
        SelectionOffset = 0;
    }

    public void InsertText(string text)
    {
        if (string.IsNullOrEmpty(text) || Document.Blocks.Count == 0) return;
        if (HasSelection) DeleteSelection();
        EnsureValidPosition();

        var block = Document.Blocks[CursorBlock];

        if (block.Content.Count == 0)
        {
            block.Content.Add(new TextRun { Text = text });
            CursorContent = 0;
            CursorOffset = text.Length;
            SyncSelection();
            return;
        }

        if (CursorContent >= block.Content.Count)
        {
            block.Content.Add(new TextRun { Text = text });
            CursorContent = block.Content.Count - 1;
            CursorOffset = block.Content[CursorContent] is TextRun tr ? tr.Length : 0;
            SyncSelection();
            return;
        }

        if (block.Content[CursorContent] is TextRun run)
        {
            run.Text = run.Text.Insert(CursorOffset, text);
            CursorOffset += text.Length;
            SyncSelection();
        }
        else
        {
            // Insert a new text run after non-text content
            var newRun = new TextRun { Text = text };
            block.Content.Insert(CursorContent + 1, newRun);
            CursorContent++;
            CursorOffset = text.Length;
            SyncSelection();
        }
    }

    public void HandleEnter()
    {
        if (Document.Blocks.Count == 0) return;
        if (HasSelection) DeleteSelection();
        EnsureValidPosition();

        var oldBlock = Document.Blocks[CursorBlock];

        // Split content at cursor
        string beforeText = "", afterText = "";
        FontStyle afterStyle = FontStyle.Regular;
        string afterFontFamily = "Arial";
        float afterFontSize = 12;
        Color afterForeColor = Color.Empty;
        int splitIndex = CursorContent;
        int splitOffset = CursorOffset;

        if (splitIndex < oldBlock.Content.Count && oldBlock.Content[splitIndex] is TextRun splitRun)
        {
            beforeText = splitRun.Text[..splitOffset];
            afterText = splitRun.Text[splitOffset..];
            afterStyle = splitRun.Style;
            afterFontFamily = splitRun.FontFamily;
            afterFontSize = splitRun.FontSize;
            afterForeColor = splitRun.ForeColor;
            splitRun.Text = beforeText;
        }

        // Build new block
        var newBlock = new RichTextBlock { Type = oldBlock.Type };

        // Move content after the split run to new block; keep the split run in old block
        int moveFrom = splitIndex;
        if (splitIndex < oldBlock.Content.Count && oldBlock.Content[splitIndex] is TextRun)
            moveFrom = splitIndex + 1;

        for (int i = moveFrom; i < oldBlock.Content.Count; i++)
            newBlock.Content.Add(oldBlock.Content[i]);
        for (int i = oldBlock.Content.Count - 1; i >= moveFrom; i--)
            oldBlock.Content.RemoveAt(i);

        if (!string.IsNullOrEmpty(afterText))
            newBlock.Content.Insert(0, new TextRun { Text = afterText, Style = afterStyle, FontFamily = afterFontFamily, FontSize = afterFontSize, ForeColor = afterForeColor });

        if (newBlock.Content.Count == 0)
            newBlock.Content.Add(new TextRun());

        // If the old block was a list item and is now empty, convert to paragraph
        if (oldBlock.Type is RichTextBlockType.BulletItem or RichTextBlockType.NumberItem
            && oldBlock.Content.Count == 1 && oldBlock.Content[0] is TextRun tr && string.IsNullOrEmpty(tr.Text))
        {
            oldBlock.Type = RichTextBlockType.Paragraph;
        }

        Document.Blocks.Insert(CursorBlock + 1, newBlock);
        CursorBlock++;
        CursorContent = 0;
        CursorOffset = 0;
        SyncSelection();
    }

    public void HandleBackspace()
    {
        if (Document.Blocks.Count == 0 || CursorFlatIndex <= 0) return;
        if (HasSelection) { DeleteSelection(); return; }
        EnsureValidPosition();

        var block = Document.Blocks[CursorBlock];

        if (CursorContent < block.Content.Count && block.Content[CursorContent] is TextRun run && CursorOffset > 0)
        {
            run.Text = run.Text.Remove(CursorOffset - 1, 1);
            CursorOffset--;
            if (string.IsNullOrEmpty(run.Text) && block.Content.Count > 1)
            {
                block.Content.RemoveAt(CursorContent);
                if (CursorContent >= block.Content.Count) CursorContent = block.Content.Count - 1;
                CursorOffset = block.Content[CursorContent] is TextRun tr2 ? tr2.Length : 0;
            }
            SyncSelection();
            return;
        }

        if (CursorBlock > 0)
        {
            var prevBlock = Document.Blocks[CursorBlock - 1];
            var curBlock = Document.Blocks[CursorBlock];
            CursorContent = prevBlock.Content.Count;
            CursorOffset = prevBlock.Content.Count > 0 && prevBlock.Content[^1] is TextRun ptr ? ptr.Length : 0;

            foreach (var c in curBlock.Content)
                prevBlock.Content.Add(c);
            Document.Blocks.RemoveAt(CursorBlock);
            CursorBlock--;
            SyncSelection();
        }
    }

    public void HandleDelete()
    {
        if (Document.Blocks.Count == 0) return;
        if (HasSelection) { DeleteSelection(); return; }

        int total = Document.TotalLength;
        if (CursorFlatIndex >= total) return;
        EnsureValidPosition();

        var block = Document.Blocks[CursorBlock];

        if (CursorContent < block.Content.Count && block.Content[CursorContent] is TextRun run && CursorOffset < run.Length)
        {
            run.Text = run.Text.Remove(CursorOffset, 1);
            if (string.IsNullOrEmpty(run.Text) && block.Content.Count > 1)
            {
                block.Content.RemoveAt(CursorContent);
                if (CursorContent >= block.Content.Count) CursorContent = block.Content.Count - 1;
                CursorOffset = block.Content[CursorContent] is TextRun tr2 ? 0 : 0;
            }
            SyncSelection();
            return;
        }

        if (CursorBlock + 1 < Document.Blocks.Count)
        {
            var nextBlock = Document.Blocks[CursorBlock + 1];
            foreach (var c in nextBlock.Content)
                block.Content.Add(c);
            Document.Blocks.RemoveAt(CursorBlock + 1);
            SyncSelection();
        }
    }

    public void MoveLeft()
    {
        if (Document.Blocks.Count == 0) return;
        EnsureValidPosition();
        var block = Document.Blocks[CursorBlock];

        if (CursorOffset > 0)
        {
            CursorOffset--;
        }
        else if (CursorContent > 0)
        {
            CursorContent--;
            CursorOffset = block.Content[CursorContent] is TextRun tr ? tr.Length : 0;
        }
        else if (CursorBlock > 0)
        {
            CursorBlock--;
            var prevBlock = Document.Blocks[CursorBlock];
            CursorContent = prevBlock.Content.Count - 1;
            if (CursorContent < 0) { CursorContent = 0; CursorOffset = 0; }
            else CursorOffset = prevBlock.Content[CursorContent] is TextRun tr2 ? tr2.Length : 0;
        }
        if (!HasSelection) SyncSelection();
    }

    public void MoveRight()
    {
        if (Document.Blocks.Count == 0) return;
        EnsureValidPosition();
        var block = Document.Blocks[CursorBlock];

        if (CursorContent < block.Content.Count && block.Content[CursorContent] is TextRun run && CursorOffset < run.Length)
        {
            CursorOffset++;
        }
        else if (CursorContent + 1 < block.Content.Count)
        {
            CursorContent++;
            CursorOffset = 0;
        }
        else if (CursorBlock + 1 < Document.Blocks.Count)
        {
            if (CursorContent < block.Content.Count)
            {
                var lastContent = block.Content[CursorContent];
                if (lastContent is TextRun tr2 && CursorOffset < tr2.Length)
                {
                    CursorOffset++;
                }
                else
                {
                    CursorBlock++;
                    CursorContent = 0;
                    CursorOffset = 0;
                }
            }
            else
            {
                CursorBlock++;
                CursorContent = 0;
                CursorOffset = 0;
            }
        }
        else if (CursorContent < block.Content.Count)
        {
            var lastContent = block.Content[CursorContent];
            if (lastContent is TextRun tr2 && CursorOffset < tr2.Length)
                CursorOffset++;
        }
        if (!HasSelection) SyncSelection();
    }

    public void MoveUp()
    {
        if (CursorBlock > 0)
        {
            CursorBlock--;
            var prevBlock = Document.Blocks[CursorBlock];
            CursorContent = prevBlock.Content.Count - 1;
            if (CursorContent < 0) { CursorContent = 0; CursorOffset = 0; }
            else CursorOffset = prevBlock.Content[CursorContent] is TextRun tr ? tr.Length : 0;
        }
        if (!HasSelection) SyncSelection();
    }

    public void MoveDown()
    {
        if (CursorBlock + 1 < Document.Blocks.Count)
        {
            CursorBlock++;
            CursorContent = 0;
            CursorOffset = 0;
        }
        if (!HasSelection) SyncSelection();
    }

    public void MoveHome()
    {
        if (CursorBlock < 0 || CursorBlock >= Document.Blocks.Count) return;
        var block = Document.Blocks[CursorBlock];
        CursorContent = block.Content.Count > 0 ? 0 : 0;
        CursorOffset = 0;
        if (!HasSelection) SyncSelection();
    }

    public void MoveEnd()
    {
        if (CursorBlock < 0 || CursorBlock >= Document.Blocks.Count) return;
        var block = Document.Blocks[CursorBlock];
        if (block.Content.Count > 0)
        {
            CursorContent = block.Content.Count - 1;
            CursorOffset = block.Content[^1] is TextRun tr ? tr.Length : 0;
        }
        else
        {
            CursorContent = 0;
            CursorOffset = 0;
        }
        if (!HasSelection) SyncSelection();
    }

    public FontStyle GetFontStyleAtCursor()
    {
        if (!IsValidPosition()) return FontStyle.Regular;
        var block = Document.Blocks[CursorBlock];
        if (CursorContent < 0 || CursorContent >= block.Content.Count) return FontStyle.Regular;
        if (block.Content[CursorContent] is TextRun run)
            return run.Style;
        return FontStyle.Regular;
    }

    public string GetFontFamilyAtCursor()
    {
        if (!IsValidPosition()) return "Arial";
        var block = Document.Blocks[CursorBlock];
        if (CursorContent < 0 || CursorContent >= block.Content.Count) return "Arial";
        if (block.Content[CursorContent] is TextRun run)
            return run.FontFamily;
        return "Arial";
    }

    public float GetFontSizeAtCursor()
    {
        if (!IsValidPosition()) return 12;
        var block = Document.Blocks[CursorBlock];
        if (CursorContent < 0 || CursorContent >= block.Content.Count) return 12;
        if (block.Content[CursorContent] is TextRun run)
            return run.FontSize;
        return 12;
    }

    public Color GetForeColorAtCursor()
    {
        if (!IsValidPosition()) return Color.Empty;
        var block = Document.Blocks[CursorBlock];
        if (CursorContent < 0 || CursorContent >= block.Content.Count) return Color.Empty;
        if (block.Content[CursorContent] is TextRun run)
            return run.ForeColor;
        return Color.Empty;
    }

    public void ToggleBold()
    {
        if (HasSelection) ToggleStyleOnSelection(FontStyle.Bold);
        else ToggleStyleAtCursor(FontStyle.Bold);
    }

    public void ToggleItalic()
    {
        if (HasSelection) ToggleStyleOnSelection(FontStyle.Italic);
        else ToggleStyleAtCursor(FontStyle.Italic);
    }

    public void ToggleUnderline()
    {
        if (HasSelection) ToggleStyleOnSelection(FontStyle.Underline);
        else ToggleStyleAtCursor(FontStyle.Underline);
    }

    public void ToggleStrikeout()
    {
        if (HasSelection) ToggleStyleOnSelection(FontStyle.Strikeout);
        else ToggleStyleAtCursor(FontStyle.Strikeout);
    }

    public void ApplyFontFamily(string fontFamily)
    {
        if (string.IsNullOrEmpty(fontFamily)) return;
        ApplyPropertyToSelectionOrCursor(run => run.FontFamily = fontFamily);
    }

    public void ApplyFontSize(float fontSize)
    {
        if (fontSize <= 0) return;
        ApplyPropertyToSelectionOrCursor(run => run.FontSize = fontSize);
    }

    public void ApplyForeColor(Color color)
    {
        ApplyPropertyToSelectionOrCursor(run => run.ForeColor = color);
    }

    private void ApplyPropertyToSelectionOrCursor(Action<TextRun> action)
    {
        if (HasSelection)
        {
            int start = Math.Min(CursorFlatIndex, SelectionFlatIndex);
            int end = Math.Max(CursorFlatIndex, SelectionFlatIndex);
            ApplyActionAcrossRange(start, end, action);
        }
        else if (IsValidPosition())
        {
            var block = Document.Blocks[CursorBlock];
            if (CursorContent < block.Content.Count && block.Content[CursorContent] is TextRun run)
                action(run);
        }
    }

    private void ToggleStyleAtCursor(FontStyle style)
    {
        if (!IsValidPosition()) return;
        var block = Document.Blocks[CursorBlock];
        if (CursorContent < block.Content.Count && block.Content[CursorContent] is TextRun run)
            run.Style ^= style;
    }

    private void ToggleStyleOnSelection(FontStyle style)
    {
        int start = Math.Min(CursorFlatIndex, SelectionFlatIndex);
        int end = Math.Max(CursorFlatIndex, SelectionFlatIndex);
        if (start >= end) return;

        ApplyActionAcrossRange(start, end, run => run.Style ^= style);
    }

    private void ApplyActionAcrossRange(int startFlat, int endFlat, Action<TextRun> action)
    {
        if (startFlat >= endFlat) return;

        int origCursorFlat = CursorFlatIndex;
        int origSelectionFlat = SelectionFlatIndex;

        var startPos = TextLayoutEngine.FromFlatIndex(Document, startFlat);

        // Split start run if selection starts mid-run
        if (startPos.CharOffset > 0)
        {
            var block = Document.Blocks[startPos.BlockIndex];
            if (startPos.ContentIndex < block.Content.Count && block.Content[startPos.ContentIndex] is TextRun)
            {
                SplitRunAt(block, startPos.ContentIndex, startPos.CharOffset);
                startPos = new DocumentPosition(startPos.BlockIndex, startPos.ContentIndex + 1, 0);
            }
        }

        var endPos = TextLayoutEngine.FromFlatIndex(Document, endFlat);

        // Split end run if selection ends mid-run
        if (endPos.CharOffset > 0)
        {
            var endBlock = Document.Blocks[endPos.BlockIndex];
            if (endPos.ContentIndex < endBlock.Content.Count && endBlock.Content[endPos.ContentIndex] is TextRun endRun && endPos.CharOffset < endRun.Text.Length)
            {
                SplitRunAt(endBlock, endPos.ContentIndex, endPos.CharOffset);
            }
        }

        // Apply action to all fully-selected runs in range
        for (int bi = startPos.BlockIndex; bi <= endPos.BlockIndex && bi < Document.Blocks.Count; bi++)
        {
            var block = Document.Blocks[bi];
            int ciStart = bi == startPos.BlockIndex ? startPos.ContentIndex : 0;
            int ciEnd = bi == endPos.BlockIndex ? endPos.ContentIndex : block.Content.Count - 1;

            for (int ci = ciStart; ci <= ciEnd && ci < block.Content.Count; ci++)
            {
                if (block.Content[ci] is TextRun run)
                    action(run);
            }
        }

        // Recompute cursor/selection from original flat indices (document text unchanged, only runs split)
        var newCursor = TextLayoutEngine.FromFlatIndex(Document, origCursorFlat);
        CursorBlock = newCursor.BlockIndex;
        CursorContent = newCursor.ContentIndex;
        CursorOffset = newCursor.CharOffset;

        var newSel = TextLayoutEngine.FromFlatIndex(Document, origSelectionFlat);
        SelectionBlock = newSel.BlockIndex;
        SelectionContent = newSel.ContentIndex;
        SelectionOffset = newSel.CharOffset;
    }

    private static void SplitRunAt(RichTextBlock block, int contentIndex, int splitOffset)
    {
        if (contentIndex < 0 || contentIndex >= block.Content.Count) return;
        if (block.Content[contentIndex] is not TextRun run) return;
        if (splitOffset <= 0 || splitOffset >= run.Text.Length) return;

        var rightRun = new TextRun
        {
            Text = run.Text[splitOffset..],
            Style = run.Style,
            FontFamily = run.FontFamily,
            FontSize = run.FontSize,
            ForeColor = run.ForeColor
        };
        run.Text = run.Text[..splitOffset];
        block.Content.Insert(contentIndex + 1, rightRun);
    }

    private void DeleteSelection()
    {
        int start = Math.Min(CursorFlatIndex, SelectionFlatIndex);
        int end = Math.Max(CursorFlatIndex, SelectionFlatIndex);
        if (start >= end) return;

        var startPos = TextLayoutEngine.FromFlatIndex(Document, start);
        var endPos = TextLayoutEngine.FromFlatIndex(Document, end);

        if (startPos.BlockIndex == endPos.BlockIndex)
        {
            var block = Document.Blocks[startPos.BlockIndex];
            if (startPos.ContentIndex == endPos.ContentIndex)
            {
                if (block.Content[startPos.ContentIndex] is TextRun run)
                {
                    run.Text = run.Text.Remove(startPos.CharOffset, endPos.CharOffset - startPos.CharOffset);
                    if (string.IsNullOrEmpty(run.Text))
                        block.Content.RemoveAt(startPos.ContentIndex);
                }
            }
            else
            {
                if (block.Content[startPos.ContentIndex] is TextRun firstRun)
                    firstRun.Text = firstRun.Text[..startPos.CharOffset];
                if (endPos.ContentIndex < block.Content.Count && block.Content[endPos.ContentIndex] is TextRun lastRun)
                    lastRun.Text = lastRun.Text[endPos.CharOffset..];

                int removeStart = startPos.ContentIndex + 1;
                int removeCount = endPos.ContentIndex - startPos.ContentIndex;
                for (int i = 0; i < removeCount && removeStart < block.Content.Count; i++)
                    block.Content.RemoveAt(removeStart);
            }
        }
        else
        {
            var firstBlock = Document.Blocks[startPos.BlockIndex];
            if (firstBlock.Content[startPos.ContentIndex] is TextRun fr)
                fr.Text = fr.Text[..startPos.CharOffset];
            for (int i = firstBlock.Content.Count - 1; i > startPos.ContentIndex; i--)
                firstBlock.Content.RemoveAt(i);

            var lastBlock = Document.Blocks[endPos.BlockIndex];
            if (endPos.ContentIndex < lastBlock.Content.Count && lastBlock.Content[endPos.ContentIndex] is TextRun lr)
                lr.Text = lr.Text[endPos.CharOffset..];
            for (int i = 0; i < endPos.ContentIndex && i < lastBlock.Content.Count; i++)
                firstBlock.Content.Add(lastBlock.Content[i]);

            for (int bi = startPos.BlockIndex + 1; bi <= endPos.BlockIndex && bi < Document.Blocks.Count; bi++)
                Document.Blocks.RemoveAt(startPos.BlockIndex + 1);
        }

        CursorBlock = startPos.BlockIndex;
        CursorContent = startPos.ContentIndex;
        CursorOffset = startPos.CharOffset;
        SyncSelection();
    }

    /// <summary>Inserts an empty paragraph at the cursor position.</summary>
    public void InsertParagraph()
    {
        if (HasSelection) DeleteSelection();
        var block = new RichTextBlock();
        block.Content.Add(new TextRun());
        Document.Blocks.Insert(CursorBlock + 1, block);
        CursorBlock++;
        CursorContent = 0;
        CursorOffset = 0;
        SyncSelection();
    }

    /// <summary>Inserts a horizontal rule at the cursor position.</summary>
    public void InsertHorizontalRule()
    {
        if (HasSelection) DeleteSelection();
        var block = new RichTextBlock { Type = RichTextBlockType.Paragraph };
        block.Content.Add(new TextRun { Text = "---" });
        Document.Blocks.Insert(CursorBlock + 1, block);
        CursorBlock++;
        CursorContent = 0;
        CursorOffset = 0;
        SyncSelection();
    }

    public void InsertLineBreak()
    {
        if (HasSelection) DeleteSelection();
        var block = Document.Blocks[CursorBlock];
        block.Content.Insert(CursorContent + 1, new LineBreakRun());
        CursorContent++;
        CursorOffset = 0;
        SyncSelection();
    }

    /// <summary>Toggles the selected block(s) between bulleted list and paragraph.</summary>
    public void ToggleUnorderedList()
        => ToggleList(RichTextBlockType.BulletItem);

    /// <summary>Toggles the selected block(s) between numbered list and paragraph.</summary>
    public void ToggleOrderedList()
        => ToggleList(RichTextBlockType.NumberItem);

    private void ToggleList(RichTextBlockType listType)
    {
        if (Document.Blocks.Count == 0) return;

        if (HasSelection)
        {
            int start = Math.Min(CursorFlatIndex, SelectionFlatIndex);
            int end = Math.Max(CursorFlatIndex, SelectionFlatIndex);
            if (start >= end) return;

            var startPos = TextLayoutEngine.FromFlatIndex(Document, start);
            var endPos = TextLayoutEngine.FromFlatIndex(Document, end);

            bool allList = true;
            for (int bi = startPos.BlockIndex; bi <= endPos.BlockIndex && bi < Document.Blocks.Count; bi++)
            {
                if (Document.Blocks[bi].Type != listType) { allList = false; break; }
            }

            var targetType = allList ? RichTextBlockType.Paragraph : listType;
            for (int bi = startPos.BlockIndex; bi <= endPos.BlockIndex && bi < Document.Blocks.Count; bi++)
            {
                if (Document.Blocks[bi].Type != targetType)
                    Document.Blocks[bi].Type = targetType;
            }
        }
        else
        {
            EnsureValidPosition();
            var block = Document.Blocks[CursorBlock];
            if (block.Type == listType)
                block.Type = RichTextBlockType.Paragraph;
            else
                block.Type = listType;
        }
        SyncSelection();
    }

    /// <summary>Wraps the selected text in a hyperlink with the given URL.</summary>
    public void CreateLink()
    {
        if (!HasSelection) return;

        int start = Math.Min(CursorFlatIndex, SelectionFlatIndex);
        int end = Math.Max(CursorFlatIndex, SelectionFlatIndex);
        if (start >= end) return;

        var startPos = TextLayoutEngine.FromFlatIndex(Document, start);
        var endPos = TextLayoutEngine.FromFlatIndex(Document, end);

        var link = new HyperlinkRun { Url = "https://" };

        if (startPos.BlockIndex == endPos.BlockIndex)
        {
            var block = Document.Blocks[startPos.BlockIndex];
            if (startPos.ContentIndex < block.Content.Count &&
                block.Content[startPos.ContentIndex] is TextRun run &&
                startPos.CharOffset > 0 && endPos.CharOffset < run.Text.Length)
            {
                SplitRunAt(block, startPos.ContentIndex, startPos.CharOffset);
                SplitRunAt(block, startPos.ContentIndex + 1, endPos.CharOffset);
                startPos = new DocumentPosition(startPos.BlockIndex, startPos.ContentIndex + 1, 0);
            }

            endPos = TextLayoutEngine.FromFlatIndex(Document, end);
            for (int ci = startPos.ContentIndex; ci <= endPos.ContentIndex && ci < block.Content.Count; ci++)
            {
                link.InnerContent.Add(block.Content[ci]);
                block.Content.RemoveAt(ci);
                ci--;
            }
            block.Content.Insert(startPos.ContentIndex, link);
        }

        SyncSelection();
    }

    /// <summary>Inserts an image placeholder at the cursor position.</summary>
    public void InsertImage()
    {
        if (HasSelection) DeleteSelection();
        EnsureValidPosition();
        var block = Document.Blocks[CursorBlock];
        block.Content.Insert(CursorContent + 1, new ImageRun { Src = "placeholder" });
        CursorContent++;
        SyncSelection();
    }

    private void SyncSelection()
    {
        SelectionBlock = CursorBlock;
        SelectionContent = CursorContent;
        SelectionOffset = CursorOffset;
    }

    private void EnsureValidPosition()
    {
        if (CursorBlock >= Document.Blocks.Count)
            CursorBlock = Document.Blocks.Count - 1;
        if (CursorBlock < 0)
            CursorBlock = 0;
    }

    private bool IsValidPosition()
    {
        if (CursorBlock < 0 || CursorBlock >= Document.Blocks.Count) return false;
        var block = Document.Blocks[CursorBlock];
        if (CursorContent < 0 || CursorContent >= block.Content.Count) return false;
        return true;
    }
}
