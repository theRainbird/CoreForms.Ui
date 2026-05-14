using System.Collections.Generic;
using CoreForms.Ui.Core;

namespace CoreForms.Ui.Html.Dom;

public class HtmlDomElement : HtmlDomNode
{
    public string TagName { get; set; } = string.Empty;
    public Dictionary<string, string> Attributes { get; } = new();
    public Dictionary<string, string> Styles { get; } = new();
    public Dictionary<string, string>? ComputedStyles { get; set; }
    public Rectangle Bounds { get; set; }
    public int RenderedY { get; set; }
    public int RenderedX { get; set; }
    public int RenderedWidth { get; set; }
    public int RenderedHeight { get; set; }

    private static readonly HashSet<string> BlockTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "div", "p", "h1", "h2", "h3", "h4", "h5", "h6", "ul", "ol", "li", "table", "tr", "td", "th", "thead", "tbody", "tfoot", "blockquote", "pre", "form", "section", "article", "header", "footer", "nav", "aside", "main", "figure", "figcaption", "details", "summary"
    };
    private static readonly HashSet<string> VoidTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "br", "hr", "img", "input", "meta", "link", "area", "base", "col", "embed", "param", "source", "track", "wbr"
    };
    private static readonly HashSet<string> TextContainerTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "p", "div", "span", "td", "th", "li", "label", "a"
    };

    public bool IsBlock => BlockTags.Contains(TagName);
    public bool IsVoidElement => VoidTags.Contains(TagName);
    public bool IsTextContainer => TextContainerTags.Contains(TagName);

    public HtmlDomElement() { NodeType = HtmlNodeType.Element; }
    public HtmlDomElement(string tagName) : this() { TagName = tagName.ToLowerInvariant(); }

    public string? GetAttribute(string name) { Attributes.TryGetValue(name.ToLowerInvariant(), out var v); return v; }
    public void SetAttribute(string name, string value) { Attributes[name.ToLowerInvariant()] = value; }
    public bool HasAttribute(string name) => Attributes.ContainsKey(name.ToLowerInvariant());
    public string? GetStyle(string name) { Styles.TryGetValue(name.ToLowerInvariant(), out var v); return v; }
    public void SetStyle(string name, string value) { Styles[name.ToLowerInvariant()] = value; }
    public string? GetComputedStyle(string name) { if (ComputedStyles == null) return null; ComputedStyles.TryGetValue(name.ToLowerInvariant(), out var v); return v; }

    public override string GetTextContent() => string.Join("", Children.Select(c => c.GetTextContent()));
    public override void SetTextContent(string text) { Children.Clear(); if (!string.IsNullOrEmpty(text)) AppendChild(new HtmlDomText(text)); }

    public HtmlDomElement? GetFirstChildElement() { foreach (var c in Children) if (c is HtmlDomElement e) return e; return null; }
    public IEnumerable<HtmlDomElement> GetChildElements() { foreach (var c in Children) if (c is HtmlDomElement e) yield return e; }

    public HtmlDomElement? QuerySelector(string selector)
    {
        if (MatchesSelector(selector)) return this;
        foreach (var c in Children) if (c is HtmlDomElement e) { var f = e.QuerySelector(selector); if (f != null) return f; }
        return null;
    }

    public List<HtmlDomElement> QuerySelectorAll(string selector)
    {
        var r = new List<HtmlDomElement>(); CollectElements(selector, r); return r;
    }

    private void CollectElements(string selector, List<HtmlDomElement> r)
    {
        if (MatchesSelector(selector)) r.Add(this);
        foreach (var c in Children) if (c is HtmlDomElement e) e.CollectElements(selector, r);
    }

    public bool MatchesSelector(string selector)
    {
        selector = selector.Trim();
        if (selector.StartsWith(".")) { string c = selector.Substring(1); string? c2 = GetAttribute("class"); return c2?.Split(' ').Contains(c) == true; }
        if (selector.StartsWith("#")) return GetAttribute("id") == selector.Substring(1);
        return TagName.Equals(selector, StringComparison.OrdinalIgnoreCase);
    }

    public HtmlDomElement? Closest(string selector)
    {
        var cur = this; while (cur != null) { if (cur is HtmlDomElement e && e.MatchesSelector(selector)) return e; cur = cur.Parent as HtmlDomElement; }
        return null;
    }

    public string OuterHtml
    {
        get
        {
            if (IsVoidElement)
            {
                var attrs = string.Join("", Attributes.Select(a => $" {a.Key}=\"{a.Value}\""));
                return $"<{TagName}{attrs} />";
            }

            var innerHtml = string.Join("", Children.Select(c => c is HtmlDomElement e ? e.OuterHtml : c is HtmlDomText t ? t.OuterHtml : ""));
            var attrStr = string.Join("", Attributes.Select(a => $" {a.Key}=\"{a.Value}\""));
            return $"<{TagName}{attrStr}>{innerHtml}</{TagName}>";
        }
    }
}