using CoreForms.Ui.Core;

namespace CoreForms.Ui.Html.Dom;

public class HtmlDomText : HtmlDomNode
{
    public string TextContent { get; set; } = string.Empty;
    public Rectangle Bounds { get; set; }
    public int RenderedX, RenderedY, RenderedWidth, RenderedHeight;

    public HtmlDomText() { NodeType = HtmlNodeType.Text; }
    public HtmlDomText(string text) : this() { TextContent = text ?? string.Empty; }
    public override string GetTextContent() => TextContent;
    public override void SetTextContent(string text) { TextContent = text ?? string.Empty; }
    public string GetWhitespaceNormalizedText() => System.Text.RegularExpressions.Regex.Replace(TextContent, @"\s+", " ").Trim();
    public bool IsWhitespaceOnly() => string.IsNullOrWhiteSpace(TextContent);

    public string OuterHtml => TextContent;
}