using HtmlAgilityPack;
using HAHtmlNode = HtmlAgilityPack.HtmlNode;
using HAHtmlNodeType = HtmlAgilityPack.HtmlNodeType;

namespace CoreForms.Ui.Html.Security;

public static class HtmlSanitizer
{
    private static readonly HashSet<string> AllowedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "html", "head", "body", "title", "base", "link", "meta", "h1", "h2", "h3", "h4", "h5", "h6", "p", "div", "span", "br", "hr", "b", "i", "u", "strong", "em", "del", "ins", "a", "img", "figure", "figcaption", "ul", "ol", "li", "table", "thead", "tbody", "tfoot", "tr", "th", "td", "blockquote", "pre", "code", "style", "section", "article", "header", "footer", "nav", "aside", "main", "details", "summary"
    };

    private static readonly HashSet<string> DangerousAttrPrefixes = new(StringComparer.OrdinalIgnoreCase) { "on" };

    public static SanitizeResult Sanitize(string html)
    {
        var result = new SanitizeResult();
        if (string.IsNullOrWhiteSpace(html)) { result.SanitizedHtml = string.Empty; return result; }

        try
        {
            var doc = new HtmlDocument(); doc.LoadHtml(html);
            var errors = new List<string>();
            WalkDocument(doc.DocumentNode, result, errors);
            using var w = new System.IO.StringWriter(); doc.Save(w);
            result.SanitizedHtml = w.ToString();
            if (errors.Count > 0) result.Errors = errors;
        }
        catch (Exception ex) { result.SanitizedHtml = html; result.Errors = new List<string> { $"Sanitization failed: {ex.Message}" }; }
        return result;
    }

    private static void WalkDocument(HAHtmlNode node, SanitizeResult result, List<string> errors)
    {
        if (node.NodeType == HAHtmlNodeType.Text) return;
        if (node.NodeType == HAHtmlNodeType.Element)
        {
            string tn = node.Name.ToLowerInvariant();
            if (tn == "script") { node.Remove(); errors.Add("Removed <script>"); return; }
            if (tn == "iframe" || tn == "object" || tn == "embed") { node.Remove(); errors.Add($"Removed <{tn}>"); return; }
            if (!AllowedTags.Contains(tn)) { if (node.HasChildNodes) { var cn = node.ChildNodes.ToList(); node.RemoveAllChildren(); foreach (var c in cn) node.AppendChild(c.CloneNode(true)); errors.Add($"Removed <{tn}>"); } else { node.Remove(); errors.Add($"Removed <{tn}>"); return; } }

            var toRemove = new List<HtmlAttribute>();
            foreach (var a in node.Attributes)
            {
                string an = a.Name.ToLowerInvariant();
                if (DangerousAttrPrefixes.Any(p => an.StartsWith(p))) { toRemove.Add(a); errors.Add($"Removed {a.Name}"); continue; }
                if (an == "style" && !IsSafeStyle(a.Value)) { toRemove.Add(a); errors.Add("Removed unsafe style"); continue; }
                if (an == "href" || an == "src") if (!IsSafeUrl(a.Value)) { toRemove.Add(a); errors.Add($"Removed unsafe URL"); continue; }
            }
            foreach (var a in toRemove) node.Attributes.Remove(a);
        }
        foreach (var c in node.ChildNodes.ToList()) WalkDocument(c, result, errors);
    }

    private static bool IsSafeStyle(string s) { var l = s.ToLowerInvariant(); return !l.Contains("expression") && !l.Contains("javascript") && !l.Contains("url("); }
    private static bool IsSafeUrl(string u)
    {
        if (string.IsNullOrEmpty(u)) return true;
        var l = u.ToLowerInvariant();
        if (l.StartsWith("javascript:")) return false;
        if (l.StartsWith("data:")) return l.StartsWith("data:image/");
        if (l.StartsWith("file://") || l.StartsWith("/") || l.StartsWith("http://") || l.StartsWith("https://") || l.StartsWith("mailto:") || l.StartsWith("tel:")) return true;
        return false;
    }
}

public class SanitizeResult { public string SanitizedHtml { get; set; } = string.Empty; public List<string> Errors { get; set; } = new(); public bool HasErrors => Errors.Count > 0; }