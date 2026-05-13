using System.Collections.Generic;

namespace CoreForms.Ui.Html.Styles;

public static class CssParser
{
    public static Dictionary<string, CssStyleDeclaration> ParseStylesheet(string css)
    {
        var r = new Dictionary<string, CssStyleDeclaration>();
        if (string.IsNullOrWhiteSpace(css)) return r;
        var rules = ExtractRules(css);
        foreach (var (sel, decls) in rules)
        {
            if (string.IsNullOrWhiteSpace(sel) || decls.Count == 0) continue;
            var s = new CssStyleDeclaration();
            foreach (var (p, v) in decls) s.SetProperty(p, v);
            if (!r.ContainsKey(sel)) r[sel] = s;
            else foreach (var p in s.Properties) r[sel][p.Key] = p.Value;
        }
        return r;
    }

    private static List<(string, List<(string, string)>)> ExtractRules(string css)
    {
        var r = new List<(string, List<(string, string)>)>();
        int bc = 0; bool inSel = true; string curSel = ""; List<(string, string)> curDecls = new();
        for (int i = 0; i < css.Length; i++)
        {
            char c = css[i];
            if (c == '{') { if (bc == 0) inSel = false; bc++; }
            else if (c == '}') { bc--; if (bc == 0) { r.Add((curSel.Trim(), curDecls)); curSel = ""; curDecls = new(); inSel = true; } }
            else if (inSel && c != '\n' && c != '\r' && c != '\t' && c != ' ') curSel += c;
        }
        return r;
    }

    private static List<(string, string)> ParseDeclarations(string decls)
    {
        var r = new List<(string, string)>();
        foreach (var p in decls.Split(';'))
        {
            var t = p.Trim(); if (string.IsNullOrEmpty(t)) continue;
            int col = t.IndexOf(':'); if (col > 0) r.Add((t.Substring(0, col).Trim().ToLowerInvariant(), t.Substring(col + 1).Trim()));
        }
        return r;
    }

    public static CssStyleDeclaration ParseInlineStyle(string? style)
    {
        var s = new CssStyleDeclaration();
        if (string.IsNullOrWhiteSpace(style)) return s;
        foreach (var p in style.Split(';'))
        {
            var t = p.Trim(); if (string.IsNullOrEmpty(t)) continue;
            int col = t.IndexOf(':'); if (col > 0)
            {
                string n = t.Substring(0, col).Trim().ToLowerInvariant();
                string v = t.Substring(col + 1).Trim();
                ExpandShorthand(s, n, v);
            }
        }
        return s;
    }

    private static void ExpandShorthand(CssStyleDeclaration s, string n, string v)
    {
        var p = v.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (n == "margin" || n == "padding")
        {
            string tp = n + "-top", rp = n + "-right", bp = n + "-bottom", lp = n + "-left";
            switch (p.Length)
            {
                case 1: s[tp] = s[rp] = s[bp] = s[lp] = p[0]; break;
                case 2: s[tp] = s[bp] = p[0]; s[rp] = s[lp] = p[1]; break;
                case 3: s[tp] = p[0]; s[rp] = s[lp] = p[1]; s[bp] = p[2]; break;
                case 4: s[tp] = p[0]; s[rp] = p[1]; s[bp] = p[2]; s[lp] = p[3]; break;
                default: s[n] = v; break;
            }
        }
        else s[n] = v;
    }

    public static List<string> ExtractStyleRules(string html)
    {
        var r = new List<string>();
        int i = 0;
        while ((i = html.IndexOf("<style", i, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            int s = html.IndexOf(">", i) + 1;
            int e = html.IndexOf("</style>", s, StringComparison.OrdinalIgnoreCase);
            if (e > s) { r.Add(html.Substring(s, e - s)); i = e; }
            else i++;
        }
        return r;
    }
}