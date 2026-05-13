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
    private bool _dirty = true;
    private int _scrollOffsetY;

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
            _dirty = false;
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

        _dirty = true;
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

    public override void Invalidate()
    {
        _dirty = true;
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