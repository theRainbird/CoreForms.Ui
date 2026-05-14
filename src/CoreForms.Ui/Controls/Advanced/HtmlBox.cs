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
    private static readonly int CursorBlinkInterval = 530;

    private string _editText = string.Empty;
    private int _cursorPos;
    private int _selectionStart;

    private bool _isMouseDown;

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
            _document = new HtmlDomDocument { DocumentElement = new HtmlDomElement("body") };
            _document.DocumentElement.OwnerDocument = _document;
            _renderer = new HtmlRenderer();
            _renderer.SetDocument(_document);
            InitEditing();
            return;
        }

        var sanitizeResult = HtmlSanitizer.Sanitize(_html);
        if (sanitizeResult.HasErrors)
            ParseError?.Invoke(this, new HtmlErrorEventArgs(sanitizeResult.Errors));

        _document = HtmlDocumentLoader.LoadHtml(sanitizeResult.SanitizedHtml);

        var styles = new Html.Styles.HtmlStyleResolver();
        foreach (var kvp in _document.StyleSheet)
            styles.AddStylesheet(kvp.Value);

        _renderer = new HtmlRenderer(styles);
        _renderer.SetDocument(_document);
        InitEditing();
        Invalidate();
    }

    private void InitEditing()
    {
        _editText = GetFlatText();
        if (_cursorPos > _editText.Length) _cursorPos = _editText.Length;
        _selectionStart = _cursorPos;
    }

    private string GetFlatText()
    {
        if (_document?.DocumentElement == null) return string.Empty;
        return string.Concat(_document.DocumentElement.Descendants()
            .OfType<HtmlDomText>().Select(n => n.TextContent));
    }

    private (HtmlDomText node, int offset) FindTextNodeAt(int flatPos)
    {
        if (_document?.DocumentElement == null)
        {
            var last = AllTextNodes().LastOrDefault();
            return (last ?? MakeBodyTextNode(), 0);
        }

        foreach (var t in AllTextNodes())
        {
            if (flatPos <= t.TextContent.Length)
                return (t, flatPos);
            flatPos -= t.TextContent.Length;
        }

        var lastNode = AllTextNodes().LastOrDefault() ?? MakeBodyTextNode();
        return (lastNode, lastNode.TextContent.Length);
    }

    private int FlatIndexOf(HtmlDomText target, int offset)
    {
        int idx = 0;
        foreach (var t in AllTextNodes())
        {
            if (t == target) return idx + offset;
            idx += t.TextContent.Length;
        }
        return idx;
    }

    private List<HtmlDomText> AllTextNodes()
    {
        if (_document?.DocumentElement == null) return new List<HtmlDomText>();
        return _document.DocumentElement.Descendants()
            .OfType<HtmlDomText>().ToList();
    }

    private HtmlDomText MakeBodyTextNode()
    {
        if (_document == null)
        {
            _document = new HtmlDomDocument { DocumentElement = new HtmlDomElement("body") };
            _document.DocumentElement.OwnerDocument = _document;
        }
        var t = new HtmlDomText("");
        _document!.DocumentElement!.AppendChild(t);
        return t;
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

        int selStart = Math.Min(_selectionStart, _cursorPos);
        int selEnd = Math.Max(_selectionStart, _cursorPos);

        if (selStart < selEnd)
        {
            var (startNode, startOff) = FindTextNodeAt(selStart);
            var (endNode, endOff) = FindTextNodeAt(selEnd);
            if (startNode == endNode)
            {
                var pos1 = _renderer?.GetTextPosition(startNode, startOff);
                var pos2 = _renderer?.GetTextPosition(endNode, endOff);
                if (pos1 != null && pos2 != null)
                {
                    int selX = Math.Min(pos1.Value.x, pos2.Value.x);
                    int selW = Math.Abs(pos2.Value.x - pos1.Value.x);
                    if (selW < 4) selW = 4;
                    g.FillRectangle(SystemColors.Highlight, selX, pos1.Value.y, selW, 16);
                }
            }
        }

        bool visible = (Environment.TickCount % (CursorBlinkInterval * 2)) < CursorBlinkInterval;
        if (!visible) return;

        var (node, offset) = FindTextNodeAt(_cursorPos);
        var pos = _renderer?.GetTextPosition(node, offset);
        if (pos == null)
        {
            int y = _renderer?.GetContentHeight() ?? 6;
            if (y < 4) y = 4;
            if (y >= Height) y = Height - 16;
            g.DrawLine(ForeColor, 6, y, 6, Math.Min(y + 16, Height), 2);
            return;
        }

        int cx = pos.Value.x;
        int cy = pos.Value.y;
        if (cy < 0 || cy >= Height) return;
        g.DrawLine(ForeColor, cx, cy, cx, Math.Min(cy + 16, Height), 2);
    }

    protected internal override void OnMouseDown(EventArgs e)
    {
        if (e is MouseEventArgs mouseArgs && _renderer != null && _document != null)
        {
            _renderer.Layout(_document, Width - 4);

            var (node, offset, _, _, _) = _renderer.HitTestTextWithPos(mouseArgs.X, mouseArgs.Y);
            if (node is HtmlDomText textNode)
            {
                _cursorPos = FlatIndexOf(textNode, offset);
                _selectionStart = _cursorPos;
            }

            var element = _renderer.HitTest(mouseArgs.X, mouseArgs.Y);
            if (element != null && element.TagName == "a")
            {
                string? href = element.GetAttribute("href");
                if (!string.IsNullOrEmpty(href))
                {
                    var linkEvent = new HtmlLinkEventArgs(href);
                    LinkClick?.Invoke(this, linkEvent);
                    if (!linkEvent.Handled && _linkBehavior == LinkBehavior.OpenInBrowser)
                    {
                        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = href, UseShellExecute = true }); }
                        catch { }
                    }
                }
            }
        }

        base.OnMouseDown(e);
        _isMouseDown = true;
        Focused = true;
    }

    protected internal override void OnMouseMove(EventArgs e)
    {
        if (_isMouseDown && e is MouseEventArgs mouseArgs && _renderer != null && _document != null)
        {
            _renderer.Layout(_document, Width - 4);
            var (node, offset, _, _, _) = _renderer.HitTestTextWithPos(mouseArgs.X, mouseArgs.Y);
            if (node is HtmlDomText textNode)
                _cursorPos = FlatIndexOf(textNode, offset);
            Invalidate();
        }
        base.OnMouseMove(e);
    }

    protected internal override void OnMouseUp(EventArgs e)
    {
        _isMouseDown = false;
        base.OnMouseUp(e);
    }

    protected internal override void OnKeyDown(KeyEventArgs e)
    {
        if (_readOnly) { base.OnKeyDown(e); return; }

        bool shift = e.Modifiers.HasFlag(ModifierKeys.Shift);

        switch (e.KeyCode)
        {
            case Keys.Back: HandleBackspace(); break;
            case Keys.Delete: HandleDelete(); break;
            case Keys.Enter: HandleEnter(); break;
            case Keys.Left: MoveLeft(shift); break;
            case Keys.Right: MoveRight(shift); break;
            case Keys.Up: case Keys.Down: break;
            default: base.OnKeyDown(e); return;
        }
        e.Handled = true;
        base.OnKeyDown(e);
    }

    protected internal override void OnTextInput(string text)
    {
        if (_readOnly || string.IsNullOrEmpty(text)) return;
        foreach (char c in text) if (c < 32) return;
        HandleTextInput(text);
    }

    private void HandleTextInput(string text)
    {
        if (_document?.DocumentElement == null) return;

        var (node, offset) = FindTextNodeAt(_cursorPos);
        node.TextContent = node.TextContent.Insert(offset, text);
        _cursorPos += text.Length;
        _editText = GetFlatText();
        Invalidate();
        ContentChanged?.Invoke(this, EventArgs.Empty);
    }

    private void HandleEnter()
    {
        if (_document?.DocumentElement == null) return;

        var (node, offset) = FindTextNodeAt(_cursorPos);
        string before = node.TextContent[..offset];
        string after = node.TextContent[offset..];

        node.TextContent = before;
        var br = new HtmlDomElement("br");
        var parent = node.Parent ?? _document.DocumentElement;
        parent.InsertAfter(br, node);

        if (after.Length > 0)
        {
            var afterNode = new HtmlDomText(after);
            parent.InsertAfter(afterNode, br);
        }

        _editText = GetFlatText();
        Invalidate();
        ContentChanged?.Invoke(this, EventArgs.Empty);
    }

    private void HandleBackspace()
    {
        if (_document?.DocumentElement == null || _cursorPos <= 0) return;

        var (node, offset) = FindTextNodeAt(_cursorPos);

        if (offset > 0)
        {
            node.TextContent = node.TextContent.Remove(offset - 1, 1);
            _cursorPos--;
        }
        else
        {
            int nodeIndex = AllTextNodes().IndexOf(node);
            if (nodeIndex > 0)
            {
                var prevNodes = AllTextNodes();
                var prevNode = prevNodes[nodeIndex - 1];
                int prevOffset = prevNode.TextContent.Length;

                prevNode.TextContent = prevNode.TextContent.Remove(prevOffset - 1, 1);
                _cursorPos--;
            }
        }

        _editText = GetFlatText();
        Invalidate();
        ContentChanged?.Invoke(this, EventArgs.Empty);
    }

    private void HandleDelete()
    {
        if (_document?.DocumentElement == null) return;

        var (node, offset) = FindTextNodeAt(_cursorPos);

        if (offset < node.TextContent.Length)
        {
            node.TextContent = node.TextContent.Remove(offset, 1);
        }
        else
        {
            var nodes = AllTextNodes();
            int idx = nodes.IndexOf(node);
            if (idx >= 0 && idx + 1 < nodes.Count)
                nodes[idx + 1].TextContent = nodes[idx + 1].TextContent.Remove(0, 1);
        }

        _editText = GetFlatText();
        Invalidate();
        ContentChanged?.Invoke(this, EventArgs.Empty);
    }

    private void MoveLeft(bool shift)
    {
        if (_cursorPos <= 0) return;
        if (!shift) _selectionStart = _cursorPos;
        _cursorPos--;
        Invalidate();
    }

    private void MoveRight(bool shift)
    {
        if (_cursorPos >= _editText.Length) return;
        if (!shift) _selectionStart = _cursorPos;
        _cursorPos++;
        Invalidate();
    }

    public override void OnThemeChanged(Theme newTheme)
    {
        if (!_backColorSet) _backColor = newTheme.TextBoxBackground;
        if (!_foreColorSet) _foreColor = newTheme.TextBoxText;
        Invalidate();
    }

    public event EventHandler<HtmlErrorEventArgs>? ParseError;
    public event EventHandler<HtmlLinkEventArgs>? LinkClick;
    public event EventHandler? ContentChanged;

    public void ApplyFormat(string formatType)
    {
        int selStart = Math.Min(_selectionStart, _cursorPos);
        int selEnd = Math.Max(_selectionStart, _cursorPos);
        if (selStart >= selEnd) return;

        string selected = _editText[selStart..selEnd];
        string replacement = formatType switch
        {
            "bold" => $"<b>{selected}</b>",
            "italic" => $"<i>{selected}</i>",
            "underline" => $"<u>{selected}</u>",
            "insertUnorderedList" => $"<ul><li>{selected}</li></ul>",
            "insertOrderedList" => $"<ol><li>{selected}</li></ol>",
            "createLink" => $"<a href=\"https://example.com\">{selected}</a>",
            "insertImage" => "<img src=\"data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAYAAABzenr0AAAABHNCSVQICAgIfAhkiAAAAAlwSFlzAAAA7AAAAOwBeShxvQAAABl0RVh0U29mdHdhcmUAd3d3Lmlua3NjYXBlLm9yZ5vuPBoAAAGJSURBVFiF7ZY9TsNAEIW/NYkUkhBKQYGgoKCgoKChoeEJPABcgCugoaGhoaGhoaGhoaGhoaGhoaGhoaGhoaGhoaGhoaGhoQGioECQAhISKSSKxMaWHUdZJCtK4pC1Zn/ezFvv2IkDfxj9+PHjZ9jb2/tmGHdJkmwBlwBToAlcAPdN0yiRSAT6/X7fMAx9IBAITk9P9yzL2gWmgUFgAhgBBoF+YBAYAIaBQaAfGAT6gAGgD+gF+oA+oA/oBfqAPqAX6AV6gB6gG+gGuoAuoAvoBLqALqAL6AQ6gQ6gA2gH2oE2oA1oAdqAZqAZaAKagWagGWgCmoBmoAloApqBJqAJaAKagCagEWgEGoFGoBFoBBqBRqAbaAbagdagNWgNWoPWoDVoDVqD1qA1aA1ag9agNWgNWoPWoDVoDVqD1qA1aA1ag9agNWgNWgM=\" />",
            _ => selected
        };

        int startIndex = _html.IndexOf(selected, StringComparison.Ordinal);
        if (startIndex < 0) return;

        _html = _html.Remove(startIndex, selected.Length).Insert(startIndex, replacement);
        ParseHtml();
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
    public HtmlLinkEventArgs(string url) { Url = url; }
}

public class HtmlErrorEventArgs : EventArgs
{
    public List<string> Errors { get; }
    public HtmlErrorEventArgs(List<string> errors) { Errors = errors; }
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
                return startText.TextContent.Substring(StartOffset, EndOffset - StartOffset);
            return startText.TextContent.Substring(StartOffset) + endText.TextContent.Substring(0, EndOffset);
        }
        return string.Empty;
    }
}
