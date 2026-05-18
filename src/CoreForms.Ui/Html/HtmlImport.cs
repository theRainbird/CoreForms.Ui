using CoreForms.Ui.Core;
using HtmlAgilityPack;

namespace CoreForms.Ui.Html;

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
                    };
                    if (content.Count > 0 && content[^1] is TextRun last
                        && last.Style == run.Style
                        && last.FontFamily == run.FontFamily
                        && last.FontSize == run.FontSize
                        && last.ForeColor == run.ForeColor)
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
                    foreach (var li in child.ChildNodes)
                        ExtractInlineContent(li, content, childStyle);
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
        };
    }
}
