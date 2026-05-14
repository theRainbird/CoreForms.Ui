using System.Collections.Generic;
using CoreForms.Ui.Html.Dom;

namespace CoreForms.Ui.Html.Styles;

public class HtmlStyleResolver
{
    private readonly Dictionary<string, CssStyleDeclaration> _ss = new();
    private readonly Dictionary<string, CssStyleDeclaration> _computed = new();

    public HtmlStyleResolver() { }
    public HtmlStyleResolver(Dictionary<string, CssStyleDeclaration> s) { foreach (var k in s) _ss[k.Key] = k.Value; }
    public void AddStylesheet(string css) { var p = CssParser.ParseStylesheet(css); foreach (var k in p) _ss[k.Key] = k.Value; }

    public CssStyleDeclaration ComputeStyles(HtmlDomElement el)
    {
        var key = $"{el.TagName}|{el.GetAttribute("id") ?? ""}|{el.GetAttribute("class") ?? ""}";
        if (_computed.TryGetValue(key, out var cached)) return cached;

        var c = new CssStyleDeclaration();
        ApplyUserAgentStyles(c, el.TagName);
        ApplyStylesheetRules(c, el);
        ApplyInlineStyles(c, el);
        ApplyInheritedStyles(c, el);

        el.ComputedStyles = new Dictionary<string, string>();
        foreach (var p in c.Properties) el.ComputedStyles[p.Key] = p.Value;
        _computed[key] = c;
        return c;
    }

    private static void ApplyUserAgentStyles(CssStyleDeclaration s, string tn)
    {
        switch (tn.ToLowerInvariant())
        {
            case "body": s["display"] = "block"; s["margin"] = "8px"; s["font-size"] = "12px"; break;
            case "h1": s["display"] = "block"; s["font-size"] = "2em"; s["font-weight"] = "bold"; break;
            case "h2": s["display"] = "block"; s["font-size"] = "1.5em"; s["font-weight"] = "bold"; break;
            case "h3": s["display"] = "block"; s["font-size"] = "1.17em"; s["font-weight"] = "bold"; break;
            case "p": case "div": s["display"] = "block"; break;
            case "span": s["display"] = "inline"; break;
            case "a": s["display"] = "inline"; s["color"] = "#0000ee"; s["text-decoration"] = "underline"; break;
            case "b": case "strong": s["display"] = "inline"; s["font-weight"] = "bold"; break;
            case "i": case "em": s["display"] = "inline"; s["font-style"] = "italic"; break;
            case "u": s["display"] = "inline"; s["text-decoration"] = "underline"; break;
            case "ul": case "ol": s["display"] = "block"; s["padding-left"] = "40px"; break;
            case "li": s["display"] = "list-item"; break;
            case "table": s["display"] = "table"; s["border-collapse"] = "collapse"; break;
            case "tr": s["display"] = "table-row"; break;
            case "td": case "th": s["display"] = "table-cell"; s["border"] = "1px solid #ccc"; s["padding"] = "4px"; break;
            case "br": s["display"] = "block"; break;
            case "img": s["display"] = "inline-block"; break;
            default: s["display"] = "inline"; break;
        }
    }

    private void ApplyStylesheetRules(CssStyleDeclaration s, HtmlDomElement el)
    { foreach (var r in _ss) { var sel = new CssSelector(r.Key); if (sel.Matches(el)) foreach (var p in r.Value.Properties) if (s.GetPropertyValue(p.Key) == null) s[p.Key] = p.Value; } }

    private static void ApplyInlineStyles(CssStyleDeclaration s, HtmlDomElement el)
    { var inline = CssParser.ParseInlineStyle(el.GetAttribute("style")); foreach (var p in inline.Properties) s[p.Key] = p.Value; }

    private void ApplyInheritedStyles(CssStyleDeclaration s, HtmlDomElement el)
    {
        var inherits = new[] { "color", "font-family", "font-size", "font-style", "font-weight", "text-align" };
        var p = el.Parent as HtmlDomElement; if (p == null) return;
        var ps = ComputeStyles(p);
        foreach (var i in inherits) if (s.GetPropertyValue(i) == null || s.GetPropertyValue(i) == "inherit") { var v = ps.GetPropertyValue(i); if (v != null && v != "inherit") s[i] = v; }
    }

    public void ClearCache() => _computed.Clear();

    public static int ParseLengthValue(string? v, int baseValue = 0)
    {
        if (string.IsNullOrEmpty(v)) return 0;
        v = v.Trim().ToLowerInvariant();
        if (v == "auto" || v == "inherit" || v == "0") return 0;
        if (v.EndsWith("px") && double.TryParse(v.Substring(0, v.Length - 2), out double px)) return (int)px;
        if (v.EndsWith("em") && double.TryParse(v.Substring(0, v.Length - 2), out double em)) return (int)(em * baseValue);
        if (v.EndsWith("%") && double.TryParse(v.Substring(0, v.Length - 1), out double pct)) return (int)(pct / 100 * baseValue);
        if (double.TryParse(v, out double n)) return (int)n;
        return 0;
    }

    public static string ResolveColor(string? c)
    {
        if (string.IsNullOrEmpty(c) || c == "inherit" || c == "transparent") return "transparent";
        if (c.StartsWith("#") || c.StartsWith("rgb") || c.StartsWith("hsl")) return c;
        return c.ToLowerInvariant() switch { "black" => "#000000", "white" => "#ffffff", "red" => "#ff0000", "green" => "#008000", "blue" => "#0000ff", "yellow" => "#ffff00", "gray" => "#808080", _ => c };
    }
}