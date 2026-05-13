using System.Collections.Generic;

namespace CoreForms.Ui.Html.Dom;

public class HtmlDomDocument : HtmlDomNode
{
    public HtmlDomElement? DocumentElement { get; set; }
    public string? RawHtml { get; set; }
    public List<string> ParseErrors { get; } = new();
    public Dictionary<string, string> StyleSheet { get; set; } = new();

    public HtmlDomDocument() { NodeType = HtmlNodeType.Document; }

    public HtmlDomElement? GetElementById(string id) => FindElementById(this, id);
    private static HtmlDomElement? FindElementById(HtmlDomNode n, string id)
    {
        if (n is HtmlDomElement e) { if (e.GetAttribute("id") == id) return e; foreach (var c in n.Children) { var f = FindElementById(c, id); if (f != null) return f; } }
        return null;
    }

    public List<HtmlDomElement> GetElementsByTagName(string tn)
    { var r = new List<HtmlDomElement>(); CollectByTagName(this, tn.ToLowerInvariant(), r); return r; }
    private static void CollectByTagName(HtmlDomNode n, string tn, List<HtmlDomElement> r)
    { if (n is HtmlDomElement e) { if (e.TagName == tn) r.Add(e); foreach (var c in n.Children) CollectByTagName(c, tn, r); } }

    public HtmlDomElement? QuerySelector(string s) => FindElement(this, s);
    public List<HtmlDomElement> QuerySelectorAll(string s) { var r = new List<HtmlDomElement>(); FindElements(this, s, r); return r; }
    private static HtmlDomElement? FindElement(HtmlDomNode n, string s)
    { if (n is HtmlDomElement e) { if (e.MatchesSelector(s)) return e; foreach (var c in n.Children) { var f = FindElement(c, s); if (f != null) return f; } } return null; }
    private static void FindElements(HtmlDomNode n, string s, List<HtmlDomElement> r)
    { if (n is HtmlDomElement e) { if (e.MatchesSelector(s)) r.Add(e); foreach (var c in n.Children) FindElements(c, s, r); } }

    public override string GetTextContent() => DocumentElement?.GetTextContent() ?? string.Empty;
    public override void SetTextContent(string text) => DocumentElement?.SetTextContent(text);

    public string GetHtml() => DocumentElement != null ? SerializeNode(DocumentElement) : string.Empty;

    private static string SerializeNode(HtmlDomNode n)
    {
        if (n is HtmlDomText t) return EscapeHtml(t.TextContent);
        if (n is HtmlDomElement e)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append('<').Append(e.TagName);
            foreach (var a in e.Attributes) sb.Append(' ').Append(a.Key).Append("=\"").Append(EscapeHtml(a.Value)).Append("\"");
            if (e.Styles.Count > 0) { sb.Append(" style=\""); foreach (var s in e.Styles) sb.Append(s.Key).Append(':').Append(s.Value).Append(';'); sb.Append('"'); }
            if (e.IsVoidElement) sb.Append(" />"); else { sb.Append('>'); foreach (var c in e.Children) sb.Append(SerializeNode(c)); sb.Append("</").Append(e.TagName).Append('>'); }
            return sb.ToString();
        }
        return string.Empty;
    }

    private static string EscapeHtml(string t) => t.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&#39;");
}