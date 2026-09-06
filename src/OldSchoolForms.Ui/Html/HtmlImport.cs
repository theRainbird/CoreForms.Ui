using OldSchoolForms.Ui.Core;
using HtmlAgilityPack;

namespace OldSchoolForms.Ui.Html;

/// <summary>
/// Parses HTML into a <see cref="RichTextDocument"/> using HtmlAgilityPack.
/// </summary>
public static class HtmlImport
{
    /// <summary>
    /// Parses the given HTML string into a new document.
    /// </summary>
    public static RichTextDocument Parse(string html)
    {
        var doc = new RichTextDocument();

        if (string.IsNullOrWhiteSpace(html))
        {
            EnsureOneEmptyBlock(doc);
            return doc;
        }

        var haDoc = new HtmlDocument();
        haDoc.LoadHtml(html);
        var body = haDoc.DocumentNode.SelectSingleNode("//body") ?? haDoc.DocumentNode;
        if (body == null || !body.HasChildNodes)
        {
            EnsureOneEmptyBlock(doc);
            return doc;
        }

        foreach (var child in body.ChildNodes)
        {
            if (child.NodeType == HtmlNodeType.Text)
            {
                string t = HtmlEntity.DeEntitize(child.InnerText).Trim();
                if (!string.IsNullOrEmpty(t))
                {
                    var block = new RichTextBlock();
                    block.Content.Add(new TextRun { Text = t });
                    doc.Blocks.Add(block);
                }
            }
            else if (child.NodeType == HtmlNodeType.Element)
            {
                ConvertElement(child, doc);
            }
        }

        if (doc.Blocks.Count == 0)
            EnsureOneEmptyBlock(doc);

        return doc;
    }

    private static void ConvertElement(HtmlNode node, RichTextDocument doc)
    {
        string tag = node.Name.ToLowerInvariant();

        // <ul> and <ol>: each <li> becomes its own block, not inline content
        if (tag is "ul" or "ol")
        {
            ConvertList(node, tag == "ol", doc);
            return;
        }

        // <table> handling
        if (tag == "table")
        {
            ConvertTable(node, doc);
            return;
        }

        RichTextBlockType blockType = tag switch
        {
            "h1" => RichTextBlockType.Heading1,
            "h2" => RichTextBlockType.Heading2,
            "h3" => RichTextBlockType.Heading3,
            "h4" => RichTextBlockType.Heading4,
            "h5" => RichTextBlockType.Heading5,
            "h6" => RichTextBlockType.Heading6,
            "li" when node.ParentNode?.Name is "ol" => RichTextBlockType.NumberItem,
            "li" => RichTextBlockType.BulletItem,
            _ => RichTextBlockType.Paragraph
        };

        var block = new RichTextBlock { Type = blockType };
        block.Alignment = ParseAlignment(node);
        var style = new TextStyleInfo();
        ExtractStyleFromAttributes(node, style);
        ExtractInlineContent(node, block.Content, style);

        if (block.Content.Count > 0)
            doc.Blocks.Add(block);
        else if (!IsVoidTag(tag))
        {
            block.Content.Add(new TextRun { Text = string.Empty });
            doc.Blocks.Add(block);
        }
    }

    private static void ConvertList(HtmlNode node, bool ordered, RichTextDocument doc)
    {
        foreach (var li in node.ChildNodes)
        {
            if (li.NodeType != HtmlNodeType.Element || li.Name.ToLowerInvariant() != "li")
                continue;

            var block = new RichTextBlock
            {
                Type = ordered ? RichTextBlockType.NumberItem : RichTextBlockType.BulletItem
            };
            var style = new TextStyleInfo();
            ExtractStyleFromAttributes(li, style);
            ExtractInlineContent(li, block.Content, style);

            if (block.Content.Count == 0)
                block.Content.Add(new TextRun());

            doc.Blocks.Add(block);
        }
    }

    private static void ConvertTable(HtmlNode tableNode, RichTextDocument doc)
    {
        var block = new RichTextBlock { Type = RichTextBlockType.Table };
        block.Rows = new List<TableRow>();
        int colCount = 0;

        var rowNodes = tableNode.SelectNodes(".//tr");
        if (rowNodes == null) return;

        foreach (var rowNode in rowNodes)
        {
            var row = new TableRow();
            var cellNodes = rowNode.SelectNodes("./td | ./th");
            if (cellNodes == null) continue;

            foreach (var cellNode in cellNodes)
            {
                var cell = new TableCell();
                var style = new TextStyleInfo();
                ExtractStyleFromAttributes(cellNode, style);
                ExtractInlineContent(cellNode, cell.Content, style);
                if (cell.Content.Count == 0)
                    cell.Content.Add(new TextRun());
                row.Cells.Add(cell);
            }

            if (row.Cells.Count > colCount) colCount = row.Cells.Count;
            block.Rows.Add(row);
        }

        block.ColCount = colCount;

        // Pad rows to uniform column count
        foreach (var row in block.Rows)
        {
            while (row.Cells.Count < colCount)
                row.Cells.Add(new TableCell { Content = { new TextRun() } });
        }

        if (block.Rows.Count > 0)
            doc.Blocks.Add(block);
    }

    private static void ExtractStyleFromAttributes(HtmlNode node, TextStyleInfo style)
    {
        if (node.Attributes["style"] != null)
        {
            var styles = node.Attributes["style"].Value.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var s in styles)
            {
                var parts = s.Split(':', 2);
                if (parts.Length != 2) continue;
                var prop = parts[0].Trim().ToLowerInvariant();
                var val = parts[1].Trim();

                switch (prop)
                {
                    case "font-family":
                        style.FontFamily = val.Trim('\'', '"');
                        break;
                    case "font-size":
                        if (float.TryParse(val.Replace("pt", "").Replace("px", "").Trim(), out var fs))
                            style.FontSize = fs;
                        break;
                    case "color":
                        style.ForeColor = ParseColor(val);
                        break;
                    case "background-color":
                        style.BackColor = ParseColor(val);
                        break;
                }
            }
        }
    }

    private static Color ParseColor(string val)
    {
        val = val.Trim().ToLowerInvariant();
        if (val.StartsWith('#'))
        {
            val = val.TrimStart('#');
            if (val.Length == 6 && int.TryParse(val, System.Globalization.NumberStyles.HexNumber, null, out var rgb))
                return Color.FromArgb((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
            if (val.Length == 3)
            {
                string expanded = new string(new[] { val[0], val[0], val[1], val[1], val[2], val[2] });
                if (int.TryParse(expanded, System.Globalization.NumberStyles.HexNumber, null, out var rgb3))
                    return Color.FromArgb((rgb3 >> 16) & 0xFF, (rgb3 >> 8) & 0xFF, rgb3 & 0xFF);
            }
        }
        return Color.Empty;
    }

    private static BlockAlignment ParseAlignment(HtmlNode node)
    {
        if (node.Attributes["style"] != null)
        {
            var styles = node.Attributes["style"].Value.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var s in styles)
            {
                var parts = s.Split(':', 2);
                if (parts.Length != 2) continue;
                var prop = parts[0].Trim().ToLowerInvariant();
                var val = parts[1].Trim().ToLowerInvariant();
                if (prop == "text-align")
                {
                    return val switch
                    {
                        "center" => BlockAlignment.Center,
                        "right" => BlockAlignment.Right,
                        "justify" => BlockAlignment.Justify,
                        _ => BlockAlignment.Left
                    };
                }
            }
        }
        return BlockAlignment.Left;
    }

    private static void ExtractInlineContent(HtmlNode node, List<InlineContent> content, TextStyleInfo style)
    {
        foreach (var child in node.ChildNodes)
        {
            if (child.NodeType == HtmlNodeType.Text)
            {
                string t = HtmlEntity.DeEntitize(child.InnerText);
                if (!string.IsNullOrEmpty(t))
                {
                    var run = new TextRun
                    {
                        Text = t,
                        Style = style.ToFontStyle(),
                        FontFamily = style.FontFamily,
                        FontSize = style.FontSize,
                        ForeColor = style.ForeColor,
                        BackColor = style.BackColor,
                    };
                    if (content.Count > 0 && content[^1] is TextRun last
                        && last.Style == run.Style
                        && last.FontFamily == run.FontFamily
                        && last.FontSize == run.FontSize
                        && last.ForeColor == run.ForeColor
                        && last.BackColor == run.BackColor)
                    {
                        last.Text += t;
                    }
                    else
                    {
                        content.Add(run);
                    }
                }
            }
            else if (child.NodeType == HtmlNodeType.Element)
            {
                string tag = child.Name.ToLowerInvariant();
                var childStyle = style.Clone();

                if (tag is "b" or "strong") childStyle.Bold = true;
                if (tag is "i" or "em") childStyle.Italic = true;
                if (tag is "u") childStyle.Underline = true;
                if (tag is "s" or "strike" or "del") childStyle.Strikeout = true;

                ExtractStyleFromAttributes(child, childStyle);

                if (tag is "br")
                {
                    content.Add(new LineBreakRun());
                }
                else if (tag is "img")
                {
                    var src = child.Attributes["src"]?.Value ?? "";
                    content.Add(new ImageRun { Src = src });
                }
                else if (tag is "a")
                {
                    var url = child.Attributes["href"]?.Value ?? "";
                    var link = new HyperlinkRun { Url = url };
                    ExtractInlineContent(child, link.InnerContent, childStyle);
                    if (link.InnerContent.Count > 0)
                        content.Add(link);
                }
                else if (tag is "ul" or "ol")
                {
                    // <ul>/<ol> are handled at the block level in ConvertElement.
                    // Each <li> becomes its own RichTextBlock — skip inline extraction.
                }
                else
                {
                    ExtractInlineContent(child, content, childStyle);
                }
            }
        }
    }

    private static bool IsVoidTag(string tag) => tag is "br" or "hr" or "img" or "input";

    private static void EnsureOneEmptyBlock(RichTextDocument doc)
    {
        if (doc.Blocks.Count == 0)
        {
            var b = new RichTextBlock();
            b.Content.Add(new TextRun());
            doc.Blocks.Add(b);
        }
    }

    internal class TextStyleInfo
    {
        public bool Bold;
        public bool Italic;
        public bool Underline;
        public bool Strikeout;
        public string FontFamily = "Arial";
        public float FontSize = 12;
        public Color ForeColor = Color.Empty;
        public Color BackColor = Color.Empty;

        public FontStyle ToFontStyle()
        {
            var s = FontStyle.Regular;
            if (Bold) s |= FontStyle.Bold;
            if (Italic) s |= FontStyle.Italic;
            if (Underline) s |= FontStyle.Underline;
            if (Strikeout) s |= FontStyle.Strikeout;
            return s;
        }

        public TextStyleInfo Clone() => new()
        {
            Bold = Bold,
            Italic = Italic,
            Underline = Underline,
            Strikeout = Strikeout,
            FontFamily = FontFamily,
            FontSize = FontSize,
            ForeColor = ForeColor,
            BackColor = BackColor,
        };
    }
}
