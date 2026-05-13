using System.Collections.Generic;
using CoreForms.Ui.Html.Dom;
using CoreForms.Ui.Html.Styles;
using CoreForms.Ui.Core;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Rendering;

public class HtmlRenderer
{
    private readonly HtmlStyleResolver _styleResolver;
    private readonly Dictionary<HtmlDomElement, List<RenderedLine>> _layoutCache = new();
    private int _renderWidth;
    private int _renderHeight;

    public HtmlRenderer()
    {
        _styleResolver = new HtmlStyleResolver();
    }

    public HtmlRenderer(HtmlStyleResolver styleResolver)
    {
        _styleResolver = styleResolver;
    }

    public void SetStylesheet(string css)
    {
        _styleResolver.AddStylesheet(css);
    }

    public void Layout(HtmlDomDocument document, int width)
    {
        _renderWidth = width;
        _layoutCache.Clear();

        if (document.DocumentElement == null)
            return;

        int y = 0;
        LayoutElement(document.DocumentElement, 0, ref y, width);
        _renderHeight = y;
    }

    private void LayoutElement(HtmlDomElement element, int x, ref int y, int availableWidth)
    {
        var styles = _styleResolver.ComputeStyles(element);
        string display = styles.Display;

        int marginLeft = HtmlStyleResolver.ParseLengthValue(styles.MarginLeft, availableWidth);
        int marginRight = HtmlStyleResolver.ParseLengthValue(styles.MarginRight, availableWidth);
        int marginTop = HtmlStyleResolver.ParseLengthValue(styles.MarginTop, availableWidth);
        int marginBottom = HtmlStyleResolver.ParseLengthValue(styles.MarginBottom, availableWidth);
        int paddingLeft = HtmlStyleResolver.ParseLengthValue(styles.PaddingLeft, availableWidth);
        int paddingRight = HtmlStyleResolver.ParseLengthValue(styles.PaddingRight, availableWidth);
        int paddingTop = HtmlStyleResolver.ParseLengthValue(styles.PaddingTop, availableWidth);
        int paddingBottom = HtmlStyleResolver.ParseLengthValue(styles.PaddingBottom, availableWidth);

        int contentWidth = availableWidth - marginLeft - marginRight - paddingLeft - paddingRight;
        if (contentWidth < 10) contentWidth = 10;

        if (display == "none")
            return;

        int startX = x + marginLeft;
        int startY = y + marginTop;

        if (display == "block" || display == "list-item")
        {
            var lines = new List<RenderedLine>();
            int lineY = startY + paddingTop;
            int lineHeight = 20;

            foreach (var child in element.Children)
            {
                if (child is HtmlDomElement childElement)
                {
                    LayoutElement(childElement, startX + paddingLeft, ref lineY, contentWidth - paddingLeft - paddingRight);
                    if (childElement.RenderedHeight > 0)
                    {
                        lineHeight = Math.Max(lineHeight, childElement.RenderedHeight);
                        lineY += childElement.RenderedHeight;
                    }
                }
                else if (child is HtmlDomText textNode)
                {
                    var textLines = LayoutText(textNode, startX + paddingLeft, lineY, contentWidth - paddingLeft - paddingRight, styles);
                    foreach (var textLine in textLines)
                    {
                        lines.Add(textLine);
                        lineHeight = Math.Max(lineHeight, textLine.Height);
                    }
                }
            }

            int elementHeight = marginTop + paddingTop + lineY - startY + paddingBottom + marginBottom;
            element.RenderedX = startX;
            element.RenderedY = startY;
            element.RenderedWidth = availableWidth;
            element.RenderedHeight = elementHeight > 0 ? elementHeight : lineHeight + paddingTop + paddingBottom;

            _layoutCache[element] = lines;
            y = startY + element.RenderedHeight + marginBottom;
        }
        else if (display == "inline" || display == "inline-block")
        {
            int currentX = startX + paddingLeft;
            int lineY = startY + paddingTop;
            int lineHeight = 16;

            var lines = new List<RenderedLine>();

            foreach (var child in element.Children)
            {
                if (child is HtmlDomText textNode)
                {
                    var textLines = LayoutText(textNode, currentX, lineY, contentWidth, styles);
                    lines.AddRange(textLines);
                    if (textLines.Count > 0)
                    {
                        lineHeight = Math.Max(lineHeight, textLines[0].Height);
                        currentX = textLines[^1].X + textLines[^1].Width;
                    }
                }
            }

            int elementWidth = currentX - startX - paddingLeft + paddingRight;
            int elementHeight = marginTop + paddingTop + lineHeight + paddingBottom + marginBottom;

            element.RenderedX = startX;
            element.RenderedY = startY;
            element.RenderedWidth = elementWidth > 0 ? elementWidth : 10;
            element.RenderedHeight = elementHeight > 0 ? elementHeight : lineHeight;

            _layoutCache[element] = lines;
            y = startY + element.RenderedHeight + marginBottom;
        }
        else if (display == "table")
        {
            LayoutTable(element, startX, ref y, availableWidth, marginLeft, marginTop, marginRight, marginBottom, paddingLeft, paddingRight, paddingTop, paddingBottom);
        }
        else
        {
            var lines = new List<RenderedLine>();
            int lineY = startY + paddingTop;

            foreach (var child in element.Children)
            {
                if (child is HtmlDomElement childElement)
                {
                    LayoutElement(childElement, startX + paddingLeft, ref lineY, contentWidth - paddingLeft - paddingRight);
                }
                else if (child is HtmlDomText textNode)
                {
                    var textLines = LayoutText(textNode, startX + paddingLeft, lineY, contentWidth - paddingLeft - paddingRight, styles);
                    lines.AddRange(textLines);
                }
            }

            element.RenderedX = startX;
            element.RenderedY = startY;
            element.RenderedWidth = availableWidth;
            element.RenderedHeight = lineY - startY + paddingTop + paddingBottom;

            _layoutCache[element] = lines;
            y = startY + element.RenderedHeight + marginBottom;
        }
    }

    private List<RenderedLine> LayoutText(HtmlDomText textNode, int x, int y, int maxWidth, CssStyleDeclaration parentStyles)
    {
        var lines = new List<RenderedLine>();
        if (string.IsNullOrEmpty(textNode.TextContent))
            return lines;

        textNode.RenderedX = x;
        textNode.RenderedY = y;
        textNode.RenderedWidth = 0;
        textNode.RenderedHeight = 16;

        lines.Add(new RenderedLine
        {
            Element = null,
            TextNode = textNode,
            X = x,
            Y = y,
            Width = 0,
            Height = 16,
            Text = textNode.TextContent
        });

        return lines;
    }

    private void LayoutTable(HtmlDomElement element, int x, ref int y, int availableWidth, int marginLeft, int marginTop, int marginRight, int marginBottom, int paddingLeft, int paddingRight, int paddingTop, int paddingBottom)
    {
        int startX = x + marginLeft + paddingLeft;
        int startY = y + marginTop + paddingTop;

        int tableHeight = paddingTop + paddingBottom;
        var rowElements = new List<HtmlDomElement>();

        foreach (var child in element.GetChildElements())
        {
            if (child.TagName == "tr")
            {
                rowElements.Add(child);
            }
        }

        foreach (var row in rowElements)
        {
            int rowHeight = 20;
            foreach (var cell in row.GetChildElements())
            {
                LayoutElement(cell, 0, ref startY, 100);
                rowHeight = Math.Max(rowHeight, cell.RenderedHeight);
            }

            int rowY = startY;
            startY += rowHeight;
            tableHeight += rowHeight;
        }

        element.RenderedX = x + marginLeft;
        element.RenderedY = y + marginTop;
        element.RenderedWidth = availableWidth;
        element.RenderedHeight = tableHeight + marginBottom;

        y = startY + paddingBottom + marginBottom;
    }

    public void Render(Graphics g, HtmlDomDocument document)
    {
        if (document.DocumentElement == null)
            return;

        RenderElement(g, document.DocumentElement);
    }

    private void RenderElement(Graphics g, HtmlDomElement element)
    {
        var styles = _styleResolver.ComputeStyles(element);
        string display = styles.Display;

        if (display == "none")
            return;

        int x = element.RenderedX;
        int y = element.RenderedY;
        int w = element.RenderedWidth;
        int h = element.RenderedHeight;

        string bgColor = HtmlStyleResolver.ResolveColor(styles.BackgroundColor);
        if (bgColor != "transparent")
        {
            g.FillRectangle(ParseColor(bgColor), x, y, w, h);
        }

        int marginLeft = HtmlStyleResolver.ParseLengthValue(styles.MarginLeft, w);
        int marginTop = HtmlStyleResolver.ParseLengthValue(styles.MarginTop, h);
        int paddingLeft = HtmlStyleResolver.ParseLengthValue(styles.PaddingLeft, w);
        int paddingTop = HtmlStyleResolver.ParseLengthValue(styles.PaddingTop, h);

        int contentX = x + marginLeft + paddingLeft;
        int contentY = y + marginTop + paddingTop;
        int contentWidth = w - marginLeft - paddingLeft - HtmlStyleResolver.ParseLengthValue(styles.MarginRight, w) - HtmlStyleResolver.ParseLengthValue(styles.PaddingRight, w);

        string tagName = element.TagName.ToLowerInvariant();
        bool isLink = tagName == "a";
        Color linkColor = ParseColor("#0000ee");

        foreach (var child in element.Children)
        {
            if (child is HtmlDomElement childElement)
            {
                RenderElement(g, childElement);
            }
            else if (child is HtmlDomText textNode)
            {
                string text = textNode.TextContent;
                if (!string.IsNullOrEmpty(text))
                {
                    Color textColor;
                    if (isLink)
                    {
                        textColor = linkColor;
                    }
                    else
                    {
                        string colorVal = HtmlStyleResolver.ResolveColor(styles.Color);
                        textColor = ParseColor(colorVal);
                    }

                    var font = CreateFont(styles);
                    float fontSize = font.Size * g.Zoom;

                    g.DrawString(text, font, textColor, contentX, contentY);

                    textNode.RenderedX = contentX;
                    textNode.RenderedY = contentY;
                    textNode.RenderedWidth = (int)(text.Length * fontSize * 0.6);
                    textNode.RenderedHeight = (int)fontSize;
                }
            }
        }

        if (element.TagName == "br")
        {
        }
    }

    private static Font CreateFont(CssStyleDeclaration styles)
    {
        string fontFamily = styles.FontFamily;
        if (fontFamily == "inherit" || string.IsNullOrEmpty(fontFamily))
            fontFamily = "Arial";

        float fontSize = 12;
        string sizeStr = styles.FontSize;
        if (!string.IsNullOrEmpty(sizeStr) && sizeStr != "inherit")
        {
            fontSize = HtmlStyleResolver.ParseLengthValue(sizeStr, 16);
            if (fontSize == 0) fontSize = 12;
        }

        return new Font(fontFamily, fontSize);
    }

    private static Color ParseColor(string colorStr)
    {
        if (string.IsNullOrEmpty(colorStr) || colorStr == "transparent" || colorStr == "inherit")
            return Color.FromArgb(0, 0, 0);

        if (colorStr.StartsWith("#"))
        {
            if (colorStr.Length == 7)
            {
                int r = Convert.ToInt32(colorStr.Substring(1, 2), 16);
                int g = Convert.ToInt32(colorStr.Substring(3, 2), 16);
                int b = Convert.ToInt32(colorStr.Substring(5, 2), 16);
                return Color.FromArgb(r, g, b);
            }
            if (colorStr.Length == 4)
            {
                int r = Convert.ToInt32(colorStr.Substring(1, 1) + colorStr.Substring(1, 1), 16);
                int g = Convert.ToInt32(colorStr.Substring(2, 1) + colorStr.Substring(2, 1), 16);
                int b = Convert.ToInt32(colorStr.Substring(3, 1) + colorStr.Substring(3, 1), 16);
                return Color.FromArgb(r, g, b);
            }
        }

        return Color.FromArgb(0, 0, 0);
    }

    public HtmlDomElement? HitTest(int x, int y)
    {
        foreach (var kvp in _layoutCache)
        {
            var element = kvp.Key;
            if (x >= element.RenderedX && x <= element.RenderedX + element.RenderedWidth &&
                y >= element.RenderedY && y <= element.RenderedY + element.RenderedHeight)
            {
                return element;
            }
        }
        return null;
    }

    public (HtmlDomNode? node, int offset) HitTestText(int x, int y)
    {
        foreach (var kvp in _layoutCache)
        {
            foreach (var line in kvp.Value)
            {
                if (line.TextNode != null)
                {
                    if (x >= line.X && x <= line.X + line.Width &&
                        y >= line.Y && y <= line.Y + line.Height)
                    {
                        int offset = (int)((x - line.X) / (line.Width / (double)line.Text.Length));
                        offset = Math.Max(0, Math.Min(line.Text.Length, offset));
                        return (line.TextNode, offset);
                    }
                }
            }
        }
        return (null, 0);
    }

    public int TotalHeight => _renderHeight;
}

public class RenderedLine
{
    public HtmlDomElement? Element { get; set; }
    public HtmlDomText? TextNode { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string Text { get; set; } = string.Empty;
}