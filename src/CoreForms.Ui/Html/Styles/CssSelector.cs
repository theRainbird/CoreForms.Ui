using CoreForms.Ui.Html.Dom;

namespace CoreForms.Ui.Html.Styles;

public class CssSelector
{
    public string SelectorText { get; }
    public SelectorType Type { get; set; }
    public string? TagName { get; set; }
    public string? ClassName { get; set; }
    public string? Id { get; set; }
    public string? AttributeName { get; set; }
    public string? AttributeValue { get; set; }
    public CssSelector? Parent { get; set; }

    public CssSelector(string s) { SelectorText = s.Trim(); Parse(s); }

    private void Parse(string s)
    {
        s = s.Trim();
        if (s.StartsWith(".")) { Type = SelectorType.Class; ClassName = s.Substring(1); }
        else if (s.StartsWith("#")) { Type = SelectorType.Id; Id = s.Substring(1); }
        else if (s.StartsWith("[")) { Type = SelectorType.Attribute; int s2 = 1, e = s.IndexOf(']'); if (e > 0) { string c = s.Substring(s2, e - s2); int eq = c.IndexOf('='); if (eq > 0) { AttributeName = c.Substring(0, eq); string op = c.Substring(eq + 1); if ((op.StartsWith("\"") && op.EndsWith("\"")) || (op.StartsWith("'") && op.EndsWith("'"))) AttributeValue = op.Substring(1, op.Length - 2); else AttributeValue = op; } else AttributeName = c; } }
        else if (s.Contains(" ")) { Type = SelectorType.Descendant; var p = s.Split(' ', 2); TagName = p[0]; if (p.Length > 1) Parent = new CssSelector(p[1]); }
        else if (s.Contains(">")) { Type = SelectorType.Child; var p = s.Split('>'); TagName = p[0]; if (p.Length > 1) Parent = new CssSelector(p[1]); }
        else { Type = SelectorType.Tag; TagName = s.ToLowerInvariant(); }
    }

    public bool Matches(HtmlDomElement el) => Type switch
    {
        SelectorType.Tag => el.TagName.Equals(TagName, StringComparison.OrdinalIgnoreCase),
        SelectorType.Class => HasClass(el, ClassName!),
        SelectorType.Id => el.GetAttribute("id") == Id,
        SelectorType.Attribute => MatchesAttribute(el),
        SelectorType.Descendant => MatchesDescendant(el),
        SelectorType.Child => MatchesChild(el),
        _ => false
    };

    private static bool HasClass(HtmlDomElement el, string cn) { string? c = el.GetAttribute("class"); return c?.Split(' ').Contains(cn) == true; }
    private bool MatchesAttribute(HtmlDomElement el) { if (AttributeName == null) return false; string? v = el.GetAttribute(AttributeName); if (v == null) return false; if (AttributeValue == null) return true; return v == AttributeValue; }
    private bool MatchesDescendant(HtmlDomElement el) { if (Parent == null) return Matches(el); var p = el.Parent as HtmlDomElement; if (p == null) return false; return Parent.Matches(p) && Matches(el); }
    private bool MatchesChild(HtmlDomElement el) { if (Parent == null) return Matches(el); var p = el.Parent as HtmlDomElement; if (p == null) return false; return Parent.Matches(p) && TagName != null && el.TagName.Equals(TagName, StringComparison.OrdinalIgnoreCase); }
}

public enum SelectorType { Tag, Class, Id, Attribute, PseudoClass, Descendant, Child, Universal }