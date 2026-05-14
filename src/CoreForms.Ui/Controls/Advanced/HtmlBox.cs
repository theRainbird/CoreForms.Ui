using CoreForms.Ui.Core;
using CoreForms.Ui.Theming;
using CoreForms.Ui.Html;
using CoreForms.Ui.Html.Dom;
using CoreForms.Ui.Html.Security;
using CoreForms.Ui.Rendering;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Advanced;

public class HtmlBox : Control
{
    private string _html = string.Empty;
    private HtmlDomDocument? _document;
    private HtmlRenderer? _renderer;
    private bool _readOnly = true;
    private LinkBehavior _linkBehavior = LinkBehavior.RaiseEvent;
    #pragma warning disable CS0414, CS0649
    private bool _needsLayout;
    private int _scrollOffsetY;
    #pragma warning restore CS0414, CS0649

    public HtmlBox()
    {
        var theme = ThemeManager.CurrentTheme;
        _backColor = theme.TextBoxBackground;
        _foreColor = theme.TextBoxText;
        Size = new Size(300, 200);
        TabStop = true;
    }

    public string Html
    {
        get => _html;
        set
        {
            if (_html != value)
            {
                _html = value;
                ParseHtml();
            }
        }
    }

    public bool ReadOnly
    {
        get => _readOnly;
        set
        {
            if (_readOnly != value)
            {
                _readOnly = value;
                Invalidate();
            }
        }
    }

    public LinkBehavior LinkBehavior
    {
        get => _linkBehavior;
        set => _linkBehavior = value;
    }

    public HtmlSelection Selection { get; } = new();

    private void ParseHtml()
    {
        if (string.IsNullOrWhiteSpace(_html))
        {
            _document = new HtmlDomDocument();
            _renderer = new HtmlRenderer();
            _needsLayout = false;
            return;
        }

        var sanitizeResult = HtmlSanitizer.Sanitize(_html);

        if (sanitizeResult.HasErrors)
        {
            ParseError?.Invoke(this, new HtmlErrorEventArgs(sanitizeResult.Errors));
        }

        _document = HtmlDocumentLoader.LoadHtml(sanitizeResult.SanitizedHtml);

        var styles = new Html.Styles.HtmlStyleResolver();
        foreach (var kvp in _document.StyleSheet)
        {
            styles.AddStylesheet(kvp.Value);
        }
        _renderer = new HtmlRenderer(styles);

        _needsLayout = true;
        Invalidate();
    }

    public override void Render(Graphics g)
    {
        if (!Visible) return;

        g.FillRectangle(BackColor, 0, 0, Width, Height);

        if (Focused)
            g.DrawRectangle(ThemeManager.CurrentTheme.TextBoxFocusBorder, 0, 0, Width, Height, 2);
        else
            g.DrawRectangle(ThemeManager.CurrentTheme.TextBoxBorder, 0, 0, Width, Height, 1);

        if (_renderer != null && _document != null)
        {
            g.Save();
            g.TranslateTransform(0, -_scrollOffsetY);

            _renderer.Layout(_document, Width - 4);
            _renderer.Render(g, _document);

            g.Restore();
        }

        base.Render(g);
    }

    protected internal override void OnMouseDown(EventArgs e)
    {
        var mouseArgs = e as MouseEventArgs;
        if (mouseArgs != null && _renderer != null)
        {
            int testX = mouseArgs.X;
            int testY = mouseArgs.Y + _scrollOffsetY;

            var element = _renderer.HitTest(testX, testY);
            if (element != null && element.TagName == "a")
            {
                string? href = element.GetAttribute("href");
                if (!string.IsNullOrEmpty(href))
                {
                    var linkEvent = new HtmlLinkEventArgs(href);
                    LinkClick?.Invoke(this, linkEvent);

                    if (!linkEvent.Handled && _linkBehavior == LinkBehavior.OpenInBrowser)
                    {
                        try
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = href,
                                UseShellExecute = true
                            });
                        }
                        catch { }
                    }
                }
            }
        }

        base.OnMouseDown(e);
        Focused = true;
    }

    protected internal override void OnKeyDown(KeyEventArgs e)
    {
        if (_readOnly)
        {
            base.OnKeyDown(e);
            return;
        }

        switch (e.KeyCode)
        {
            case Keys.Back:
                DeleteText(-1);
                e.Handled = true;
                break;
            case Keys.Delete:
                DeleteText(0);
                e.Handled = true;
                break;
            case Keys.Enter:
                InsertText("\n");
                e.Handled = true;
                break;
            case Keys.Left:
                MoveCursor(-1);
                e.Handled = true;
                break;
            case Keys.Right:
                MoveCursor(1);
                e.Handled = true;
                break;
            case Keys.Up:
                MoveCursorLine(-1);
                e.Handled = true;
                break;
            case Keys.Down:
                MoveCursorLine(1);
                e.Handled = true;
                break;
        }

        base.OnKeyDown(e);
    }

    protected internal override void OnTextInput(string text)
    {
        if (_readOnly || string.IsNullOrEmpty(text)) return;
        InsertText(text);
    }

    private void InsertText(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        if (_document?.DocumentElement == null) return;

        var body = _document.DocumentElement;
        var textNode = new HtmlDomText(text);
        body.AppendChild(textNode);

        _html = _document.DocumentElement.OuterHtml;
        ParseHtml();
        ContentChanged?.Invoke(this, EventArgs.Empty);
    }

    private void DeleteText(int direction)
    {
        if (_document?.DocumentElement == null) return;

        var children = _document.DocumentElement.Children;
        if (children.Count == 0) return;

        if (direction < 0)
        {
            var lastChild = children[^1];
            if (lastChild is HtmlDomText textNode)
            {
                if (textNode.TextContent.Length > 0)
                {
                    textNode.TextContent = textNode.TextContent[..^1];
                    if (string.IsNullOrEmpty(textNode.TextContent))
                        _document.DocumentElement.RemoveChild(textNode);
                }
                else
                {
                    _document.DocumentElement.RemoveChild(lastChild);
                }
            }
            else
            {
                _document.DocumentElement.RemoveChild(lastChild);
            }
        }
        else
        {
            var firstChild = children[0];
            if (firstChild is HtmlDomText textNode)
            {
                if (textNode.TextContent.Length > 0)
                {
                    textNode.TextContent = textNode.TextContent[1..];
                    if (string.IsNullOrEmpty(textNode.TextContent))
                        _document.DocumentElement.RemoveChild(textNode);
                }
                else
                {
                    _document.DocumentElement.RemoveChild(firstChild);
                }
            }
            else
            {
                _document.DocumentElement.RemoveChild(firstChild);
            }
        }

        _html = _document.DocumentElement.OuterHtml;
        ParseHtml();
        ContentChanged?.Invoke(this, EventArgs.Empty);
    }

    private void MoveCursor(int direction)
    {
    }

    private void MoveCursorLine(int direction)
    {
    }

    public override void Invalidate()
    {
        _needsLayout = true;
        base.Invalidate();
    }

    public override void OnThemeChanged(Theme newTheme)
    {
        if (!_backColorSet)
            _backColor = newTheme.TextBoxBackground;
        if (!_foreColorSet)
            _foreColor = newTheme.TextBoxText;
        Invalidate();
    }

    public event EventHandler<HtmlErrorEventArgs>? ParseError;
    public event EventHandler<HtmlLinkEventArgs>? LinkClick;
    public event EventHandler? ContentChanged;

    public void ApplyFormat(string formatType)
    {
        switch (formatType)
        {
            case "bold":
                WrapSelectionWithTag("b");
                break;
            case "italic":
                WrapSelectionWithTag("i");
                break;
            case "underline":
                WrapSelectionWithTag("u");
                break;
            case "insertUnorderedList":
                WrapSelectionWithTag("ul");
                break;
            case "insertOrderedList":
                WrapSelectionWithTag("ol");
                break;
            case "createLink":
                WrapSelectionWithTag("a", "href=\"https://example.com\"");
                break;
            case "insertImage":
                InsertHtml("<img src=\"data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAYAAABzenr0AAAABHNCSVQICAgIfAhkiAAAAAlwSFlzAAAA7AAAAOwBeShxvQAAABl0RVh0U29mdHdhcmUAd3d3Lmlua3NjYXBlLm9yZ5vuPBoAAAGJSURBVFiF7ZY9TsNAEIW/NYkUkhBKQYGgoKCgoKChoeEJPABcgCugoaGhoaGhoaGhoaGhoaGhoaGhoaGhoaGhoaGhoaGhoQGioECQAhISKSSKxMaWHUdZJCtK4pC1Zn/ezFvv2IkDfxj9+PHjZ9jb2/tmGHdJkmwBlwBToAlcAPdN0yiRSAT6/X7fMAx9IBAITk9P9yzL2gWmgUFgAhgBBoF+YBAYAIaBQaAfGAT6gAGgD+gF+oA+oA/oBfqAPqAX6AV6gB6gG+gGuoAuoAvoBLqALqAL6AQ6gQ6gA2gH2oE2oA1oAdqAZqAZaAKagWagGWgCmoBmoAloApqBJqAJaAKagCagEWgEGoFGoBFoBBqBRqAbaAbagdagNWgNWoPWoDVoDVqD1qA1aA1ag9agNWgNWoPWoDVoDVqD1qA1aA1ag9agNWgNWgM=\" />");
                break;
        }
    }

    private void WrapSelectionWithTag(string tagName, string? attributes = null)
    {
        var selectedText = Selection.GetSelectedText();
        if (string.IsNullOrEmpty(selectedText)) return;

        var attrStr = string.IsNullOrEmpty(attributes) ? "" : " " + attributes;
        var html = $"<{tagName}{attrStr}>{selectedText}</{tagName}>";
        InsertHtml(html);
    }

    private void InsertHtml(string html)
    {
        var selectedText = Selection.GetSelectedText();
        var newHtml = _html.Insert(_html.IndexOf(selectedText, StringComparison.Ordinal), html);
        Html = newHtml;
        ContentChanged?.Invoke(this, EventArgs.Empty);
    }
}

public enum LinkBehavior
{
    None,
    RaiseEvent,
    OpenInBrowser
}

public class HtmlLinkEventArgs : EventArgs
{
    public string Url { get; }
    public bool Handled { get; set; }

    public HtmlLinkEventArgs(string url)
    {
        Url = url;
    }
}

public class HtmlErrorEventArgs : EventArgs
{
    public List<string> Errors { get; }

    public HtmlErrorEventArgs(List<string> errors)
    {
        Errors = errors;
    }
}

public class HtmlSelection
{
    public HtmlDomNode? StartNode { get; set; }
    public int StartOffset { get; set; }
    public HtmlDomNode? EndNode { get; set; }
    public int EndOffset { get; set; }

    public bool IsCollapsed => StartNode == EndNode && StartOffset == EndOffset;

    public string GetSelectedText()
    {
        if (StartNode is HtmlDomText startText && EndNode is HtmlDomText endText)
        {
            if (StartNode == EndNode)
            {
                return startText.TextContent.Substring(StartOffset, EndOffset - StartOffset);
            }
            return startText.TextContent.Substring(StartOffset) + endText.TextContent.Substring(0, EndOffset);
        }
        return string.Empty;
    }
}