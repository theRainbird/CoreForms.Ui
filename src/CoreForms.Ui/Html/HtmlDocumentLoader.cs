using CoreForms.Ui.Html.Dom;
using CoreForms.Ui.Html.Styles;
using HtmlAgilityPack;
using HAHtmlNode = HtmlAgilityPack.HtmlNode;
using HAHtmlNodeType = HtmlAgilityPack.HtmlNodeType;

namespace CoreForms.Ui.Html;

public static class HtmlDocumentLoader
{
    public static HtmlDomDocument LoadHtml(string html)
    {
        var doc = new HtmlDomDocument();
        if (string.IsNullOrWhiteSpace(html)) { doc.DocumentElement = new HtmlDomElement("body"); return doc; }

        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);
        doc.RawHtml = html;

        var styleRules = CssParser.ExtractStyleRules(html);
        foreach (var css in styleRules)
        {
            var parsed = CssParser.ParseStylesheet(css);
            foreach (var kvp in parsed) doc.StyleSheet[kvp.Key] = kvp.Value.ToString();
        }

        var root = ConvertNode(htmlDoc.DocumentNode, doc);
        if (root is HtmlDomElement converted)
        {
            if (converted.TagName == "#document" && converted.Children.Count > 0)
            {
                var firstChild = converted.Children[0] as HtmlDomElement;
                if (firstChild != null)
                {
                    doc.DocumentElement = firstChild;
                    doc.DocumentElement.OwnerDocument = doc;
                    WalkTree(doc.DocumentElement, n => n.OwnerDocument = doc);
                }
                else
                {
                    var body = new HtmlDomElement("body");
                    body.OwnerDocument = doc;
                    foreach (var child in converted.Children)
                        body.AppendChild(child);
                    doc.DocumentElement = body;
                    WalkTree(doc.DocumentElement, n => n.OwnerDocument = doc);
                }
            }
            else
            {
                doc.DocumentElement = converted;
                doc.DocumentElement.OwnerDocument = doc;
                WalkTree(doc.DocumentElement, n => n.OwnerDocument = doc);
            }
        }

        if (htmlDoc.ParseErrors.Count() > 0) foreach (var e in htmlDoc.ParseErrors) doc.ParseErrors.Add($"Line {e.Line}: {e.Reason}");
        return doc;
    }

    private static HtmlDomNode? ConvertNode(HAHtmlNode node, HtmlDomDocument doc)
    {
        if (node.NodeType == HAHtmlNodeType.Text) { string t = node.InnerText; if (string.IsNullOrWhiteSpace(t)) return null; var txt = new HtmlDomText(t); txt.OwnerDocument = doc; return txt; }
        if (node.NodeType == HAHtmlNodeType.Comment) return null;

        var el = new HtmlDomElement(node.Name);
        el.OwnerDocument = doc;
        if (node.HasAttributes) foreach (var a in node.Attributes) el.SetAttribute(a.Name, a.Value);
        if (node.HasChildNodes) foreach (var c in node.ChildNodes) { var conv = ConvertNode(c, doc); if (conv != null) el.AppendChild(conv); }
        return el;
    }

    private static void WalkTree(HtmlDomNode n, Action<HtmlDomNode> a) { a(n); foreach (var c in n.Children) WalkTree(c, a); }
}