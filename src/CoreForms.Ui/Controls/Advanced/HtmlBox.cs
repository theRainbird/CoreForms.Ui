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
    private HtmlDomText? _cursorDomNode;
    private int _cursorDomOffset;
    private static readonly int CursorBlinkInterval = 530;
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
            _renderer.SetDocument(_document);
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
        _renderer.SetDocument(_document);

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

        DrawCursor(g);

        base.Render(g);
    }

    private void DrawCursor(Graphics g)
    {
        if (!Focused || _readOnly) return;

        bool visible = (Environment.TickCount % (CursorBlinkInterval * 2)) < CursorBlinkInterval;
        if (!visible) return;

        var cursorPos = GetCursorScreenPos();
        if (cursorPos == null) return;

        var (cx, cy) = cursorPos.Value;
        if (cy < 0 || cy >= Height) return;
        g.DrawLine(ForeColor, cx, cy, cx, Math.Min(cy + 16, Height), 2);
    }

    private (int x, int y)? GetCursorScreenPos()
    {
        if (_renderer == null || _document == null) return null;

        var pos = _renderer.GetTextPosition(_cursorDomNode, _cursorDomOffset);
        if (pos != null) return (pos.Value.x, pos.Value.y - _scrollOffsetY);

        int contentY = _renderer.GetContentHeight();
        if (contentY < 4) contentY = 4;
        return (6, Math.Min(contentY, Height - 16) - _scrollOffsetY);
    }

    protected internal override void OnMouseDown(EventArgs e)
    {
        var mouseArgs = e as MouseEventArgs;
        if (mouseArgs != null && _renderer != null)
        {
            int testX = mouseArgs.X;
            int testY = mouseArgs.Y;

            var (node, offset, _, _, _) = _renderer.HitTestTextWithPos(testX, testY);
            if (node is HtmlDomText textNode)
            {
                _cursorDomNode = textNode;
                _cursorDomOffset = offset;
                Selection.StartNode = node;
                Selection.StartOffset = offset;
                Selection.EndNode = node;
                Selection.EndOffset = offset;
            }

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

        bool shift = e.Modifiers.HasFlag(ModifierKeys.Shift);

        switch (e.KeyCode)
        {
            case Keys.Back:
                HandleBackspace();
                e.Handled = true;
                break;
            case Keys.Delete:
                HandleDelete();
                e.Handled = true;
                break;
            case Keys.Enter:
                HandleEnter();
                e.Handled = true;
                break;
            case Keys.Left:
                MoveCursorLeft(shift);
                e.Handled = true;
                break;
            case Keys.Right:
                MoveCursorRight(shift);
                e.Handled = true;
                break;
            case Keys.Up:
            case Keys.Down:
                e.Handled = true;
                break;
        }

        base.OnKeyDown(e);
    }

    protected internal override void OnTextInput(string text)
    {
        if (_readOnly || string.IsNullOrEmpty(text)) return;
        foreach (char c in text)
            if (c < 32) return;
        HandleTextInput(text);
    }

    private void HandleTextInput(string text)
    {
        if (_document?.DocumentElement == null) return;
        EnsureCursorValid();

        if (_cursorDomNode != null)
        {
            _cursorDomNode.TextContent = _cursorDomNode.TextContent.Insert(_cursorDomOffset, text);
            _cursorDomOffset += text.Length;
        }
        else
        {
            var newNode = new HtmlDomText(text);
            _document.DocumentElement.AppendChild(newNode);
            _cursorDomNode = newNode;
            _cursorDomOffset = text.Length;
        }

        SyncHtmlAndInvalidate();
    }

    private void HandleEnter()
    {
        if (_document?.DocumentElement == null) return;
        EnsureCursorValid();

        var body = _document.DocumentElement;
        var br = new HtmlDomElement("br");
        if (_cursorDomNode != null)
        {
            body.InsertAfter(br, _cursorDomNode);
            _cursorDomNode = null;
            _cursorDomOffset = 0;
        }
        else
        {
            body.AppendChild(br);
        }

        SyncHtmlAndInvalidate();
    }

    private void HandleBackspace()
    {
        if (_document?.DocumentElement == null) return;
        EnsureCursorValid();

        if (_cursorDomNode != null && _cursorDomOffset > 0)
        {
            _cursorDomNode.TextContent = _cursorDomNode.TextContent.Remove(_cursorDomOffset - 1, 1);
            _cursorDomOffset--;
            if (string.IsNullOrEmpty(_cursorDomNode.TextContent))
            {
                var parent = _cursorDomNode.Parent;
                parent?.RemoveChild(_cursorDomNode);
                _cursorDomNode = null;
                _cursorDomOffset = 0;
            }
        }
        else if (_cursorDomNode != null)
        {
            var sibling = _cursorDomNode.PreviousSibling;
            if (sibling is HtmlDomText prevText && prevText.TextContent.Length > 0)
            {
                _cursorDomOffset = prevText.TextContent.Length;
                _cursorDomNode = prevText;
                _cursorDomNode.TextContent = _cursorDomNode.TextContent.Remove(_cursorDomOffset - 1, 1);
                _cursorDomOffset--;
            }
        }

        SyncHtmlAndInvalidate();
    }

    private void HandleDelete()
    {
        if (_document?.DocumentElement == null) return;
        EnsureCursorValid();

        if (_cursorDomNode != null)
        {
            int len = _cursorDomNode.TextContent.Length;
            if (_cursorDomOffset < len)
            {
                _cursorDomNode.TextContent = _cursorDomNode.TextContent.Remove(_cursorDomOffset, 1);
                if (string.IsNullOrEmpty(_cursorDomNode.TextContent))
                {
                    var parent = _cursorDomNode.Parent;
                    parent?.RemoveChild(_cursorDomNode);
                    _cursorDomNode = null;
                    _cursorDomOffset = 0;
                }
            }
            else
            {
                var parent = _cursorDomNode.Parent;
                if (parent != null)
                {
                    int idx = parent.Children.IndexOf(_cursorDomNode);
                    if (idx >= 0 && idx + 1 < parent.Children.Count)
                    {
                        var next = parent.Children[idx + 1];
                        if (next is HtmlDomElement nextElem && nextElem.TagName == "br")
                            parent.RemoveChild(nextElem);
                        else if (next is HtmlDomText nextText && nextText.TextContent.Length > 0)
                            nextText.TextContent = nextText.TextContent[1..];
                    }
                }
            }
        }

        SyncHtmlAndInvalidate();
    }

    private void MoveCursorLeft(bool shift)
    {
        if (_document?.DocumentElement == null) return;
        EnsureCursorValid();

        if (_cursorDomNode != null && _cursorDomOffset > 0)
        {
            _cursorDomOffset--;
        }
        else if (_cursorDomNode != null)
        {
            var sibling = _cursorDomNode.PreviousSibling;
            if (sibling is HtmlDomText prevText)
            {
                _cursorDomNode = prevText;
                _cursorDomOffset = prevText.TextContent.Length;
            }
            else if (sibling is HtmlDomElement { TagName: "br" })
            {
                _cursorDomNode = null;
                _cursorDomOffset = 0;
            }
        }

        if (!shift)
        {
            Selection.StartNode = _cursorDomNode;
            Selection.StartOffset = _cursorDomOffset;
            Selection.EndNode = _cursorDomNode;
            Selection.EndOffset = _cursorDomOffset;
        }
        else
        {
            Selection.EndNode = _cursorDomNode;
            Selection.EndOffset = _cursorDomOffset;
        }
    }

    private void MoveCursorRight(bool shift)
    {
        if (_document?.DocumentElement == null) return;
        EnsureCursorValid();

        if (_cursorDomNode != null && _cursorDomOffset < _cursorDomNode.TextContent.Length)
        {
            _cursorDomOffset++;
        }
        else if (_cursorDomNode != null)
        {
            var sibling = _cursorDomNode.NextSibling;
            if (sibling is HtmlDomText nextText)
            {
                _cursorDomNode = nextText;
                _cursorDomOffset = 0;
            }
            else if (sibling is HtmlDomElement { TagName: "br" })
            {
                _cursorDomNode = null;
                _cursorDomOffset = 0;
            }
        }

        if (!shift)
        {
            Selection.StartNode = _cursorDomNode;
            Selection.StartOffset = _cursorDomOffset;
            Selection.EndNode = _cursorDomNode;
            Selection.EndOffset = _cursorDomOffset;
        }
        else
        {
            Selection.EndNode = _cursorDomNode;
            Selection.EndOffset = _cursorDomOffset;
        }
    }

    private void EnsureCursorValid()
    {
        if (_document?.DocumentElement == null) return;

        if (_cursorDomNode != null)
        {
            if (!_document.DocumentElement.Descendants().Contains(_cursorDomNode))
            {
                _cursorDomNode = null;
                _cursorDomOffset = 0;
            }
            return;
        }

        var lastText = _document.DocumentElement.Descendants().OfType<HtmlDomText>().LastOrDefault();
        if (lastText != null)
        {
            _cursorDomNode = lastText;
            _cursorDomOffset = lastText.TextContent.Length;
        }
    }

    private void SyncHtmlAndInvalidate()
    {
        _html = _document!.DocumentElement!.OuterHtml;
        Invalidate();
        ContentChanged?.Invoke(this, EventArgs.Empty);
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
                InsertHtmlForSelection("<img src=\"data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAYAAABzenr0AAAABHNCSVQICAgIfAhkiAAAAAlwSFlzAAAA7AAAAOwBeShxvQAAABl0RVh0U29mdHdhcmUAd3d3Lmlua3NjYXBlLm9yZ5vuPBoAAAGJSURBVFiF7ZY9TsNAEIW/NYkUkhBKQYGgoKCgoKChoeEJPABcgCugoaGhoaGhoaGhoaGhoaGhoaGhoaGhoaGhoaGhoaGhoQGioECQAhISKSSKxMaWHUdZJCtK4pC1Zn/ezFvv2IkDfxj9+PHjZ9jb2/tmGHdJkmwBlwBToAlcAPdN0yiRSAT6/X7fMAx9IBAITk9P9yzL2gWmgUFgAhgBBoF+YBAYAIaBQaAfGAT6gAGgD+gF+oA+oA/oBfqAPqAX6AV6gB6gG+gGuoAuoAvoBLqALqAL6AQ6gQ6gA2gH2oE2oA1oAdqAZqAZaAKagWagGWgCmoBmoAloApqBJqAJaAKagCagEWgEGoFGoBFoBBqBRqAbaAbagdagNWgNWoPWoDVoDVqD1qA1aA1ag9agNWgNWoPWoDVoDVqD1qA1aA1ag9agNWgNWgM=\" />");
                break;
        }
    }

    private void WrapSelectionWithTag(string tagName, string? attributes = null)
    {
        var selectedText = Selection.GetSelectedText();
        if (string.IsNullOrEmpty(selectedText)) return;

        var attrStr = string.IsNullOrEmpty(attributes) ? "" : " " + attributes;
        var replacement = $"<{tagName}{attrStr}>{selectedText}</{tagName}>";

        int startIndex = _html.IndexOf(selectedText, StringComparison.Ordinal);
        if (startIndex < 0) return;

        _html = _html.Remove(startIndex, selectedText.Length).Insert(startIndex, replacement);
        Html = _html;
        ContentChanged?.Invoke(this, EventArgs.Empty);
    }

    private void InsertHtmlForSelection(string html)
    {
        var selectedText = Selection.GetSelectedText();
        if (string.IsNullOrEmpty(selectedText)) return;

        int startIndex = _html.IndexOf(selectedText, StringComparison.Ordinal);
        if (startIndex < 0) return;

        _html = _html.Remove(startIndex, selectedText.Length).Insert(startIndex, html);
        Html = _html;
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
