using CoreForms.Ui.Core;

namespace CoreForms.Ui.Html;

public enum RichTextBlockType
{
    Paragraph,
    Heading1,
    Heading2,
    BulletItem,
    NumberItem
}

public class RichTextRun
{
    public string Text { get; set; } = string.Empty;
    public FontStyle Style { get; set; } = FontStyle.Regular;

    public int Length => Text.Length;
}

public class RichTextBlock
{
    public RichTextBlockType Type { get; set; } = RichTextBlockType.Paragraph;
    public List<RichTextRun> Runs { get; } = new();

    public int TotalLength => Runs.Sum(r => r.Length);
}

public class RichTextDocument
{
    public List<RichTextBlock> Blocks { get; } = new();

    public int TotalLength => Blocks.Sum(b => b.TotalLength);

    public (int blockIdx, int runIdx, int charOff) ToPosition(int flatIndex)
    {
        int remaining = flatIndex;
        for (int bi = 0; bi < Blocks.Count; bi++)
        {
            var block = Blocks[bi];
            int blockLen = block.TotalLength;
            if (remaining <= blockLen)
            {
                for (int ri = 0; ri < block.Runs.Count; ri++)
                {
                    var run = block.Runs[ri];
                    if (remaining <= run.Length)
                        return (bi, ri, remaining);
                    remaining -= run.Length;
                }
                return (bi, block.Runs.Count - 1, block.Runs.Count > 0 ? block.Runs[^1].Length : 0);
            }
            remaining -= blockLen;
        }
        var lastBlock = Blocks[^1];
        var lastRun = lastBlock.Runs.Count > 0 ? lastBlock.Runs[^1] : null;
        return (Blocks.Count - 1, lastBlock.Runs.Count - 1, lastRun?.Length ?? 0);
    }

    public int ToFlatIndex(int blockIdx, int runIdx, int charOff)
    {
        int idx = 0;
        for (int bi = 0; bi < blockIdx && bi < Blocks.Count; bi++)
            idx += Blocks[bi].TotalLength;
        if (blockIdx < Blocks.Count)
        {
            var block = Blocks[blockIdx];
            for (int ri = 0; ri < runIdx && ri < block.Runs.Count; ri++)
                idx += block.Runs[ri].Length;
            if (runIdx < block.Runs.Count)
                idx += Math.Min(charOff, block.Runs[runIdx].Length);
        }
        return idx;
    }
}

public class RichTextEngine
{
    public RichTextDocument Document { get; } = new();
    public int CursorBlock { get; set; }
    public int CursorRun { get; set; }
    public int CursorOffset { get; set; }
    public int SelectionBlock { get; set; }
    public int SelectionRun { get; set; }
    public int SelectionOffset { get; set; }

    public int CursorFlatIndex =>
        Document.ToFlatIndex(CursorBlock, CursorRun, CursorOffset);

    public int SelectionFlatIndex =>
        Document.ToFlatIndex(SelectionBlock, SelectionRun, SelectionOffset);

    public bool HasSelection => CursorFlatIndex != SelectionFlatIndex;

    public void InitFromHtml(string html)
    {
        Document.Blocks.Clear();

        if (string.IsNullOrWhiteSpace(html))
        {
            var b = new RichTextBlock();
            b.Runs.Add(new RichTextRun());
            Document.Blocks.Add(b);
            ResetCursor();
            return;
        }

        var haDoc = new HtmlAgilityPack.HtmlDocument();
        haDoc.LoadHtml(html);
        var body = haDoc.DocumentNode.SelectSingleNode("//body")
                   ?? haDoc.DocumentNode;

        if (body == null || !body.HasChildNodes)
        {
            var b = new RichTextBlock();
            b.Runs.Add(new RichTextRun());
            Document.Blocks.Add(b);
            ResetCursor();
            return;
        }

        foreach (var child in body.ChildNodes)
        {
            if (child.NodeType == HtmlAgilityPack.HtmlNodeType.Text)
            {
                string t = HtmlAgilityPack.HtmlEntity.DeEntitize(child.InnerText);
                if (!string.IsNullOrWhiteSpace(t))
                {
                    var block = new RichTextBlock();
                    block.Runs.Add(new RichTextRun { Text = t.Trim() });
                    Document.Blocks.Add(block);
                }
            }
            else if (child.NodeType == HtmlAgilityPack.HtmlNodeType.Element)
            {
                ConvertElement(child, Document);
            }
        }

        if (Document.Blocks.Count == 0)
        {
            var b = new RichTextBlock();
            b.Runs.Add(new RichTextRun());
            Document.Blocks.Add(b);
        }
        ResetCursor();
    }

    private static void ConvertElement(HtmlAgilityPack.HtmlNode node, RichTextDocument doc)
    {
        string tag = node.Name.ToLowerInvariant();
        RichTextBlockType blockType = tag switch
        {
            "h1" => RichTextBlockType.Heading1,
            "h2" => RichTextBlockType.Heading2,
            "li" when node.ParentNode?.Name == "ol" => RichTextBlockType.NumberItem,
            "li" => RichTextBlockType.BulletItem,
            _ => RichTextBlockType.Paragraph
        };

        var block = new RichTextBlock { Type = blockType };
        ExtractRuns(node, block.Runs, FontStyle.Regular);

        if (block.Runs.Count > 0)
            doc.Blocks.Add(block);
        else if (!IsVoidTag(tag))
        {
            block.Runs.Add(new RichTextRun());
            doc.Blocks.Add(block);
        }
    }

    private static bool IsVoidTag(string tag) => tag is "br" or "hr" or "img" or "input";

    private static void ExtractRuns(HtmlAgilityPack.HtmlNode node, List<RichTextRun> runs, FontStyle inheritStyle)
    {
        foreach (var child in node.ChildNodes)
        {
            if (child.NodeType == HtmlAgilityPack.HtmlNodeType.Text)
            {
                string t = HtmlAgilityPack.HtmlEntity.DeEntitize(child.InnerText);
                if (!string.IsNullOrWhiteSpace(t))
                    runs.Add(new RichTextRun { Text = t, Style = inheritStyle });
            }
            else if (child.NodeType == HtmlAgilityPack.HtmlNodeType.Element)
            {
                string tag = child.Name.ToLowerInvariant();
                FontStyle style = inheritStyle;
                if (tag is "b" or "strong") style |= FontStyle.Bold;
                if (tag is "i" or "em") style |= FontStyle.Italic;
                if (tag is "u") style |= FontStyle.Underline;
                if (tag is "br")
                {
                    if (runs.Count > 0)
                    {
                        var last = runs[^1];
                        runs.RemoveAt(runs.Count - 1);
                        runs.Add(new RichTextRun { Text = last.Text, Style = last.Style });
                    }
                    runs.Add(new RichTextRun { Text = "\n", Style = inheritStyle });
                }
                else if (tag is "ul" or "ol")
                {
                    foreach (var li in child.ChildNodes)
                        ExtractRuns(li, runs, style);
                }
                else
                {
                    ExtractRuns(child, runs, style);
                }
            }
        }
    }

    public string ToHtml()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("<body>");
        foreach (var block in Document.Blocks)
        {
            string tag = block.Type switch
            {
                RichTextBlockType.Heading1 => "h1",
                RichTextBlockType.Heading2 => "h2",
                RichTextBlockType.BulletItem => "li",
                RichTextBlockType.NumberItem => "li",
                _ => "p"
            };

            string wrapper = block.Type is RichTextBlockType.BulletItem or RichTextBlockType.NumberItem
                ? (block.Type == RichTextBlockType.NumberItem ? "ol" : "ul") : null;

            if (!string.IsNullOrEmpty(tag) && block.Runs.Count > 0)
            {
                string inner = RenderRuns(block.Runs);
                if (wrapper != null)
                {
                    if (!sb.ToString().EndsWith($"<{wrapper}>"))
                    {
                        if (sb.Length > 6) sb.AppendLine();
                        sb.Append($"<{wrapper}>");
                    }
                    sb.AppendLine();
                    sb.Append($"  <{tag}>{inner}</{tag}>");
                }
                else
                {
                    if (sb.Length > 6) sb.AppendLine();
                    sb.Append($"<{tag}>{inner}</{tag}>");
                }
            }

            if (block.Runs.Count == 0)
            {
                if (sb.Length > 6) sb.AppendLine();
                sb.Append($"<{tag}></{tag}>");
            }
        }
        sb.AppendLine();
        sb.Append("</body>");
        return sb.ToString();
    }

    private static string RenderRuns(List<RichTextRun> runs)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var run in runs)
        {
            string t = System.Net.WebUtility.HtmlEncode(run.Text);
            var style = run.Style;
            if (style.HasFlag(FontStyle.Bold)) t = $"<b>{t}</b>";
            if (style.HasFlag(FontStyle.Italic)) t = $"<i>{t}</i>";
            if (style.HasFlag(FontStyle.Underline)) t = $"<u>{t}</u>";
            sb.Append(t);
        }
        return sb.ToString();
    }

    public void ResetCursor()
    {
        CursorBlock = 0;
        CursorRun = 0;
        CursorOffset = 0;
        SelectionBlock = 0;
        SelectionRun = 0;
        SelectionOffset = 0;
    }

    public void InsertText(string text)
    {
        if (string.IsNullOrEmpty(text) || Document.Blocks.Count == 0) return;

        if (HasSelection) DeleteSelection();

        var block = Document.Blocks[CursorBlock];
        if (block.Runs.Count == 0)
        {
            block.Runs.Add(new RichTextRun { Text = text });
            CursorRun = 0;
            CursorOffset = text.Length;
            return;
        }

        if (CursorRun >= block.Runs.Count)
        {
            CursorRun = block.Runs.Count - 1;
            CursorOffset = block.Runs[CursorRun].Length;
        }

        var run = block.Runs[CursorRun];
        run.Text = run.Text.Insert(CursorOffset, text);
        CursorOffset += text.Length;
        SyncSelection();
    }

    public void HandleEnter()
    {
        if (Document.Blocks.Count == 0) return;

        if (HasSelection) DeleteSelection();

        var oldBlock = Document.Blocks[CursorBlock];

        string beforeText = "", afterText = "";
        FontStyle afterStyle = FontStyle.Regular;

        if (CursorRun < oldBlock.Runs.Count)
        {
            var run = oldBlock.Runs[CursorRun];
            beforeText = run.Text[..CursorOffset];
            afterText = run.Text[CursorOffset..];
            afterStyle = run.Style;
            run.Text = beforeText;

            if (string.IsNullOrEmpty(run.Text))
                oldBlock.Runs.RemoveAt(CursorRun);
        }

        var newBlock = new RichTextBlock { Type = oldBlock.Type };

        for (int i = CursorRun; i < oldBlock.Runs.Count; i++)
        {
            newBlock.Runs.Add(oldBlock.Runs[i]);
        }
        for (int i = oldBlock.Runs.Count - 1; i >= CursorRun; i--)
        {
            oldBlock.Runs.RemoveAt(i);
        }

        if (!string.IsNullOrEmpty(afterText))
        {
            newBlock.Runs.Insert(0, new RichTextRun { Text = afterText, Style = afterStyle });
        }

        if (newBlock.Runs.Count == 0)
            newBlock.Runs.Add(new RichTextRun());

        Document.Blocks.Insert(CursorBlock + 1, newBlock);
        CursorBlock++;
        CursorRun = 0;
        CursorOffset = 0;
        SyncSelection();
    }

    public void HandleBackspace()
    {
        if (Document.Blocks.Count == 0 || CursorFlatIndex <= 0) return;

        if (HasSelection) { DeleteSelection(); return; }

        if (CursorOffset > 0)
        {
            var block = Document.Blocks[CursorBlock];
            if (CursorRun < block.Runs.Count)
            {
                var run = block.Runs[CursorRun];
                run.Text = run.Text.Remove(CursorOffset - 1, 1);
                CursorOffset--;
                if (string.IsNullOrEmpty(run.Text))
                {
                    block.Runs.RemoveAt(CursorRun);
                    if (CursorRun >= block.Runs.Count) CursorRun = block.Runs.Count - 1;
                    if (CursorRun >= 0) CursorOffset = block.Runs[CursorRun].Length;
                    else CursorOffset = 0;
                }
            }
        }
        else if (CursorBlock > 0)
        {
            var prevBlock = Document.Blocks[CursorBlock - 1];
            var curBlock = Document.Blocks[CursorBlock];
            CursorOffset = prevBlock.TotalLength;
            CursorRun = prevBlock.Runs.Count;
            foreach (var run in curBlock.Runs)
                prevBlock.Runs.Add(run);
            Document.Blocks.RemoveAt(CursorBlock);
            CursorBlock--;
        }
        SyncSelection();
    }

    public void HandleDelete()
    {
        if (Document.Blocks.Count == 0) return;

        if (HasSelection) { DeleteSelection(); return; }

        int totalLen = Document.TotalLength;
        if (CursorFlatIndex >= totalLen) return;

        var block = Document.Blocks[CursorBlock];
        if (CursorRun < block.Runs.Count)
        {
            var run = block.Runs[CursorRun];
            if (CursorOffset < run.Length)
            {
                run.Text = run.Text.Remove(CursorOffset, 1);
                if (string.IsNullOrEmpty(run.Text))
                {
                    block.Runs.RemoveAt(CursorRun);
                    if (CursorRun >= block.Runs.Count) CursorRun = block.Runs.Count - 1;
                    if (CursorRun >= 0) CursorOffset = 0;
                }
            }
            else if (CursorBlock + 1 < Document.Blocks.Count)
            {
                var nextBlock = Document.Blocks[CursorBlock + 1];
                foreach (var r in nextBlock.Runs)
                    block.Runs.Add(r);
                Document.Blocks.RemoveAt(CursorBlock + 1);
            }
        }
        SyncSelection();
    }

    public void MoveLeft()
    {
        if (CursorOffset > 0)
        {
            CursorOffset--;
        }
        else if (CursorRun > 0)
        {
            CursorRun--;
            CursorOffset = Document.Blocks[CursorBlock].Runs[CursorRun].Length;
        }
        else if (CursorBlock > 0)
        {
            CursorBlock--;
            var block = Document.Blocks[CursorBlock];
            CursorRun = block.Runs.Count - 1;
            CursorOffset = CursorRun >= 0 ? block.Runs[CursorRun].Length : 0;
            if (CursorRun < 0) { CursorRun = 0; CursorOffset = 0; }
        }
        if (!HasSelection) SyncSelection();
    }

    public FontStyle GetFontStyleAtCursor()
    {
        if (CursorBlock < 0 || CursorBlock >= Document.Blocks.Count) return FontStyle.Regular;
        var block = Document.Blocks[CursorBlock];
        if (CursorRun < 0 || CursorRun >= block.Runs.Count) return FontStyle.Regular;
        return block.Runs[CursorRun].Style;
    }

    public void MoveRight()
    {
        if (Document.Blocks.Count == 0) return;
        var block = Document.Blocks[CursorBlock];
        if (CursorRun < block.Runs.Count)
        {
            var run = block.Runs[CursorRun];
            if (CursorOffset < run.Length)
            {
                CursorOffset++;
                if (!HasSelection) SyncSelection();
                return;
            }
        }

        if (CursorRun + 1 < block.Runs.Count)
        {
            CursorRun++;
            CursorOffset = 0;
        }
        else if (CursorBlock + 1 < Document.Blocks.Count)
        {
            CursorBlock++;
            CursorRun = 0;
            CursorOffset = 0;
        }
        if (!HasSelection) SyncSelection();
    }

    public void ToggleBold()
    {
        if (HasSelection) ToggleStyleOnSelection(FontStyle.Bold);
        else if (CursorRun >= 0 && CursorRun < Document.Blocks[CursorBlock].Runs.Count)
        {
            var run = Document.Blocks[CursorBlock].Runs[CursorRun];
            run.Style ^= FontStyle.Bold;
        }
    }

    public void ToggleItalic()
    {
        if (HasSelection) ToggleStyleOnSelection(FontStyle.Italic);
        else if (CursorRun >= 0 && CursorRun < Document.Blocks[CursorBlock].Runs.Count)
        {
            var run = Document.Blocks[CursorBlock].Runs[CursorRun];
            run.Style ^= FontStyle.Italic;
        }
    }

    public void ToggleUnderline()
    {
        if (HasSelection) ToggleStyleOnSelection(FontStyle.Underline);
        else if (CursorRun >= 0 && CursorRun < Document.Blocks[CursorBlock].Runs.Count)
        {
            var run = Document.Blocks[CursorBlock].Runs[CursorRun];
            run.Style ^= FontStyle.Underline;
        }
    }

    private void ToggleStyleOnSelection(FontStyle style)
    {
        int start = Math.Min(CursorFlatIndex, SelectionFlatIndex);
        int end = Math.Max(CursorFlatIndex, SelectionFlatIndex);
        if (start >= end) return;

        var (sb, sr, so) = Document.ToPosition(start);
        var (eb, er, eo) = Document.ToPosition(end);

        for (int bi = sb; bi <= eb && bi < Document.Blocks.Count; bi++)
        {
            var block = Document.Blocks[bi];
            int runStart = bi == sb ? sr : 0;
            int runEnd = bi == eb ? er : block.Runs.Count - 1;

            for (int ri = runStart; ri <= runEnd && ri < block.Runs.Count; ri++)
            {
                var run = block.Runs[ri];
                if (bi == sb && ri == sr && bi == eb && ri == er)
                {
                    if (so < eo) run.Style ^= style;
                }
                else if (bi == sb && ri == sr)
                {
                    if (so < run.Length) run.Style ^= style;
                }
                else if (bi == eb && ri == er)
                {
                    if (eo > 0) run.Style ^= style;
                }
                else
                {
                    run.Style ^= style;
                }
            }
        }
    }

    private void DeleteSelection()
    {
        int start = Math.Min(CursorFlatIndex, SelectionFlatIndex);
        int end = Math.Max(CursorFlatIndex, SelectionFlatIndex);
        if (start >= end) return;

        var (sb, sr, so) = Document.ToPosition(start);
        var (eb, er, eo) = Document.ToPosition(end);

        if (sb == eb)
        {
            var block = Document.Blocks[sb];
            if (sr == er)
            {
                block.Runs[sr].Text = block.Runs[sr].Text.Remove(so, eo - so);
                if (string.IsNullOrEmpty(block.Runs[sr].Text))
                    block.Runs.RemoveAt(sr);
            }
            else
            {
                block.Runs[sr].Text = block.Runs[sr].Text[..so];
                if (er < block.Runs.Count)
                    block.Runs[er].Text = block.Runs[er].Text[eo..];
                int removeCount = er - sr;
                if (removeCount > 0)
                {
                    for (int i = sr + 1; i <= sr + removeCount && i < block.Runs.Count; i++)
                        block.Runs.RemoveAt(sr + 1);
                }
            }
        }
        else
        {
            var firstBlock = Document.Blocks[sb];
            firstBlock.Runs[sr].Text = firstBlock.Runs[sr].Text[..so];
            for (int i = firstBlock.Runs.Count - 1; i > sr; i--)
                firstBlock.Runs.RemoveAt(i);

            var lastBlock = Document.Blocks[eb];
            if (er < lastBlock.Runs.Count)
            {
                lastBlock.Runs[er].Text = lastBlock.Runs[er].Text[eo..];
                for (int i = 0; i < er; i++)
                    firstBlock.Runs.Add(lastBlock.Runs[i]);
            }

            for (int bi = sb + 1; bi <= eb && bi < Document.Blocks.Count; bi++)
                Document.Blocks.RemoveAt(sb + 1);
        }

        CursorBlock = sb; CursorRun = sr; CursorOffset = so;
        SyncSelection();
    }

    private void SyncSelection()
    {
        SelectionBlock = CursorBlock;
        SelectionRun = CursorRun;
        SelectionOffset = CursorOffset;
    }
}
