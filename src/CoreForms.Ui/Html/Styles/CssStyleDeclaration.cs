using System.Collections.Generic;

namespace CoreForms.Ui.Html.Styles;

public class CssStyleDeclaration
{
    private readonly Dictionary<string, string> _p = new();
    public string? this[string n] { get { _p.TryGetValue(n.ToLowerInvariant(), out var v); return v; } set { if (value == null) _p.Remove(n.ToLowerInvariant()); else _p[n.ToLowerInvariant()] = value; } }
    public string? GetPropertyValue(string n) => this[n];
    public void SetProperty(string n, string v) => this[n] = v;
    public bool HasProperty(string n) => _p.ContainsKey(n.ToLowerInvariant());
    public void RemoveProperty(string n) => _p.Remove(n.ToLowerInvariant());
    public IReadOnlyDictionary<string, string> Properties => _p;

    public string Display => this["display"] ?? "inline";
    public string Color => this["color"] ?? "inherit";
    public string BackgroundColor => this["background-color"] ?? "transparent";
    public string FontSize => this["font-size"] ?? "inherit";
    public string FontWeight => this["font-weight"] ?? "normal";
    public string FontStyle => this["font-style"] ?? "normal";
    public string TextDecoration => this["text-decoration"] ?? "none";
    public string TextAlign => this["text-align"] ?? "left";
    public string FontFamily => this["font-family"] ?? "inherit";
    public string MarginTop => this["margin-top"] ?? "0";
    public string MarginRight => this["margin-right"] ?? "0";
    public string MarginBottom => this["margin-bottom"] ?? "0";
    public string MarginLeft => this["margin-left"] ?? "0";
    public string PaddingTop => this["padding-top"] ?? "0";
    public string PaddingRight => this["padding-right"] ?? "0";
    public string PaddingBottom => this["padding-bottom"] ?? "0";
    public string PaddingLeft => this["padding-left"] ?? "0";
    public string Width => this["width"] ?? "auto";
    public string Height => this["height"] ?? "auto";

    public string GetMargin(string d) => d.ToLowerInvariant() switch { "top" => MarginTop, "right" => MarginRight, "bottom" => MarginBottom, "left" => MarginLeft, _ => "0" };
    public string GetPadding(string d) => d.ToLowerInvariant() switch { "top" => PaddingTop, "right" => PaddingRight, "bottom" => PaddingBottom, "left" => PaddingLeft, _ => "0" };

    public override string ToString() => string.Join("; ", _p.Select(kv => $"{kv.Key}: {kv.Value}"));

    public CssStyleDeclaration Clone() { var c = new CssStyleDeclaration(); foreach (var kv in _p) c._p[kv.Key] = kv.Value; return c; }
}