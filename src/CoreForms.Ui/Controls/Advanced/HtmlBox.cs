using CoreForms.Ui.Core;
using CoreForms.Ui.Theming;
using CoreForms.Ui.Html;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Advanced;

public class HtmlBox : Control
{
    private readonly RichTextEngine _engine = new();
    private string _htmlBacking = string.Empty;
    private bool _readOnly = true;
    private LinkBehavior _linkBehavior = LinkBehavior.RaiseEvent;
    private static readonly int CursorBlinkInterval = 530;

    private int _cursorScreenX;
    private int _cursorScreenY;
    private bool _cursorScreenValid;

    public HtmlBox()
    {
        var theme = ThemeManager.CurrentTheme;
        _backColor = theme.TextBoxBackground;
        _foreColor = theme.TextBoxText;
        Size = new Size(300, 200);
        TabStop = true;
        _engine.InitFromHtml(string.Empty);
    }

    public string Html
    {
        get => _engine.ToHtml();
        set
        {
            if (value != _htmlBacking)
            {
                _htmlBacking = value ?? string.Empty;
                _engine.InitFromHtml(_htmlBacking);
                Invalidate();
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

    public override void OnThemeChanged(Theme newTheme)
    {
        if (!_backColorSet) _backColor = newTheme.TextBoxBackground;
        if (!_foreColorSet) _foreColor = newTheme.TextBoxText;
        Invalidate();
    }

    public event EventHandler<HtmlErrorEventArgs>? ParseError;
    public event EventHandler<HtmlLinkEventArgs>? LinkClick;
    public event EventHandler? ContentChanged;

    public bool IsBold => _engine.GetFontStyleAtCursor().HasFlag(FontStyle.Bold);
    public bool IsItalic => _engine.GetFontStyleAtCursor().HasFlag(FontStyle.Italic);
    public bool IsUnderline => _engine.GetFontStyleAtCursor().HasFlag(FontStyle.Underline);

    public void ApplyFormat(string formatType)
    {
        switch (formatType)
        {
            case "bold": _engine.ToggleBold(); break;
            case "italic": _engine.ToggleItalic(); break;
            case "underline": _engine.ToggleUnderline(); break;
        }

        Invalidate();
        ContentChanged?.Invoke(this, EventArgs.Empty);
    }

    public override void Render(Graphics g)
    {
        if (!Visible) return;

        g.FillRectangle(BackColor, 0, 0, Width, Height);

        if (Focused)
            g.DrawRectangle(ThemeManager.CurrentTheme.TextBoxFocusBorder, 0, 0, Width, Height, 2);
        else
            g.DrawRectangle(ThemeManager.CurrentTheme.TextBoxBorder, 0, 0, Width, Height, 1);

        _cursorScreenValid = false;

        int y = 4;
        float zoom = g.Zoom;
        var doc = _engine.Document;
        int numberCounter = 0;

        for (int bi = 0; bi < doc.Blocks.Count; bi++)
        {
            var block = doc.Blocks[bi];
            int blockStartFlat = doc.ToFlatIndex(bi, 0, 0);
            int blockEndFlat = blockStartFlat + block.TotalLength;

            int blockTop = y;
            int textStartX = GetTextStartX(block.Type);
            int markerX = 4;
            float fontSize = GetBlockFontSize(block.Type);

            if (block.Type == RichTextBlockType.NumberItem)
            {
                numberCounter++;
            }
            else if (block.Type != RichTextBlockType.BulletItem)
            {
                numberCounter = 0;
            }

            foreach (var run in block.Runs)
            {
                if (string.IsNullOrEmpty(run.Text)) continue;

                var font = new Font("Arial", fontSize, run.Style);
                var measured = Platform.Platform.MeasureText(run.Text, font, zoom);
                int runHeightLogical = (int)(measured.height / zoom);

                if (run == block.Runs[0])
                {
                    if (block.Type == RichTextBlockType.BulletItem)
                    {
                        g.DrawString("•", new Font("Arial", fontSize, FontStyle.Regular), ForeColor, markerX, y);
                    }
                    else if (block.Type == RichTextBlockType.NumberItem)
                    {
                        g.DrawString($"{numberCounter}.", new Font("Arial", fontSize, FontStyle.Regular), ForeColor,
                            markerX, y);
                    }
                }

                g.DrawString(run.Text, font, ForeColor, textStartX, y);

                int runStartFlat = blockStartFlat + GetRunStartOffset(block, run);
                int runEndFlat = runStartFlat + run.Length;

                int selStart = Math.Min(_engine.CursorFlatIndex, _engine.SelectionFlatIndex);
                int selEnd = Math.Max(_engine.CursorFlatIndex, _engine.SelectionFlatIndex);

                if (selStart < runEndFlat && selEnd > runStartFlat)
                {
                    int localSelStart = Math.Max(0, selStart - runStartFlat);
                    int localSelEnd = Math.Min(run.Length, selEnd - runStartFlat);

                    string beforeSel = run.Text[..localSelStart];
                    string selText = run.Text[localSelStart..localSelEnd];
                    float selBeforeWidth = Platform.Platform.MeasureText(beforeSel, font, zoom).width / zoom;
                    float selTextWidth = Platform.Platform.MeasureText(selText, font, zoom).width / zoom;
                    int selX = textStartX + (int)selBeforeWidth;
                    int selW = (int)selTextWidth;
                    if (selW < 2) selW = 2;
                    g.FillRectangle(ThemeManager.CurrentTheme.Highlight, selX, y, selW, runHeightLogical);
                    g.DrawString(selText, font, ThemeManager.CurrentTheme.HighlightText, selX, y);
                }

                int cursorFlat = _engine.CursorFlatIndex;
                if (!_cursorScreenValid && cursorFlat >= runStartFlat && cursorFlat <= runEndFlat)
                {
                    int localOff = cursorFlat - runStartFlat;
                    string beforeCursor = run.Text[..localOff];
                    float measuredWidth = Platform.Platform.MeasureText(beforeCursor, font, zoom).width;
                    _cursorScreenX = (int)(textStartX + measuredWidth / zoom);
                    _cursorScreenY = y;
                    _cursorScreenValid = true;
                }

                y += runHeightLogical;
            }

            if (y == blockTop)
                y += (int)fontSize + 4;
        }

        DrawCursor(g);
        base.Render(g);
    }

    private static int GetTextStartX(RichTextBlockType type)
    {
        if (type is RichTextBlockType.BulletItem or RichTextBlockType.NumberItem)
            return 24;
        return 4;
    }

    private static float GetBlockFontSize(RichTextBlockType type) => type switch
    {
        RichTextBlockType.Heading1 => 22,
        RichTextBlockType.Heading2 => 18,
        _ => 12
    };

    private static int GetRunStartOffset(RichTextBlock block, RichTextRun target)
    {
        int offset = 0;
        foreach (var r in block.Runs)
        {
            if (r == target) return offset;
            offset += r.Length;
        }

        return offset;
    }

    private void DrawCursor(Graphics g)
    {
        if (!Focused || _readOnly) return;

        bool visible = (Environment.TickCount % (CursorBlinkInterval * 2)) < CursorBlinkInterval;
        if (!visible) return;

        if (!_cursorScreenValid) return;

        int cy = _cursorScreenY;
        if (cy < 0 || cy >= Height) return;
        if (cy + 16 > Height) cy = Height - 16;

        g.DrawLine(ForeColor, _cursorScreenX, cy, _cursorScreenX, cy + 14, 1);
    }

    protected internal override void OnMouseDown(EventArgs e)
    {
        if (e is MouseEventArgs mouseArgs)
        {
            int mx = mouseArgs.X;
            float zoom = EffectiveZoom;
            int y = 4;
            var doc = _engine.Document;
            int lastBlock = -1, lastRun = -1, lastOffset = 0;

            for (int bi = 0; bi < doc.Blocks.Count; bi++)
            {
                var block = doc.Blocks[bi];
                float fontSize = GetBlockFontSize(block.Type);
                int textStartX = GetTextStartX(block.Type);

                foreach (var run in block.Runs)
                {
                    if (string.IsNullOrEmpty(run.Text)) continue;

                    var font = new Font("Arial", fontSize, run.Style);
                    var measured = Platform.Platform.MeasureText(run.Text, font, zoom);
                    int runHeightLogical = (int)(measured.height / zoom);

                    lastBlock = bi;
                    lastRun = doc.Blocks[bi].Runs.IndexOf(run);
                    lastOffset = run.Length;

                    if (mouseArgs.Y >= y && mouseArgs.Y < y + runHeightLogical)
                    {
                        int charOffset = FindCharAtX(run.Text, font, zoom, mx - (int)(textStartX * zoom));
                        _engine.CursorBlock = bi;
                        _engine.CursorRun = doc.Blocks[bi].Runs.IndexOf(run);
                        _engine.CursorOffset = charOffset;
                        _engine.SelectionBlock = _engine.CursorBlock;
                        _engine.SelectionRun = _engine.CursorRun;
                        _engine.SelectionOffset = _engine.CursorOffset;
                        Invalidate();
                        ContentChanged?.Invoke(this, EventArgs.Empty);
                        base.OnMouseDown(e);
                        Focused = true;
                        return;
                    }

                    y += runHeightLogical;
                }

                if (block.Runs.Count == 0)
                    y += (int)fontSize + 4;
            }

            if (lastBlock >= 0 && lastRun >= 0)
            {
                _engine.CursorBlock = lastBlock;
                _engine.CursorRun = lastRun;
                _engine.CursorOffset = lastOffset;
                _engine.SelectionBlock = _engine.CursorBlock;
                _engine.SelectionRun = _engine.CursorRun;
                _engine.SelectionOffset = _engine.CursorOffset;
                Invalidate();
                ContentChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        base.OnMouseDown(e);
        Focused = true;
    }

    private static int FindCharAtX(string text, Font font, float zoom, int targetX)
    {
        if (string.IsNullOrEmpty(text) || targetX <= 0) return 0;
        int bestPos = 0;
        int bestDist = int.MaxValue;

        for (int i = 0; i <= text.Length; i++)
        {
            string sub = text[..i];
            int w = Platform.Platform.MeasureText(sub, font, zoom).width;
            int dist = Math.Abs(targetX - w);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestPos = i;
            }
        }

        return bestPos;
    }

    protected internal override void OnMouseMove(EventArgs e)
    {
        base.OnMouseMove(e);
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
            case Keys.Back: _engine.HandleBackspace(); break;
            case Keys.Delete: _engine.HandleDelete(); break;
            case Keys.Enter: _engine.HandleEnter(); break;
            case Keys.Left:
                if (shift && !_engine.HasSelection)
                {
                    _engine.SelectionBlock = _engine.CursorBlock;
                    _engine.SelectionRun = _engine.CursorRun;
                    _engine.SelectionOffset = _engine.CursorOffset;
                }

                _engine.MoveLeft();
                if (!shift)
                {
                    _engine.SelectionBlock = _engine.CursorBlock;
                    _engine.SelectionRun = _engine.CursorRun;
                    _engine.SelectionOffset = _engine.CursorOffset;
                }

                break;
            case Keys.Right:
                if (shift && !_engine.HasSelection)
                {
                    _engine.SelectionBlock = _engine.CursorBlock;
                    _engine.SelectionRun = _engine.CursorRun;
                    _engine.SelectionOffset = _engine.CursorOffset;
                }

                _engine.MoveRight();
                if (!shift)
                {
                    _engine.SelectionBlock = _engine.CursorBlock;
                    _engine.SelectionRun = _engine.CursorRun;
                    _engine.SelectionOffset = _engine.CursorOffset;
                }

                break;
            case Keys.Up:
                if (shift && !_engine.HasSelection)
                {
                    _engine.SelectionBlock = _engine.CursorBlock;
                    _engine.SelectionRun = _engine.CursorRun;
                    _engine.SelectionOffset = _engine.CursorOffset;
                }

                if (_engine.CursorBlock > 0)
                {
                    _engine.CursorBlock--;
                    var prevBlock = _engine.Document.Blocks[_engine.CursorBlock];
                    _engine.CursorRun = prevBlock.Runs.Count - 1;
                    _engine.CursorOffset = _engine.CursorRun >= 0 ? prevBlock.Runs[_engine.CursorRun].Length : 0;
                    if (_engine.CursorRun < 0)
                    {
                        _engine.CursorRun = 0;
                        _engine.CursorOffset = 0;
                    }

                    if (!shift)
                    {
                        _engine.SelectionBlock = _engine.CursorBlock;
                        _engine.SelectionRun = _engine.CursorRun;
                        _engine.SelectionOffset = _engine.CursorOffset;
                    }
                }

                break;
            case Keys.Down:
                if (shift && !_engine.HasSelection)
                {
                    _engine.SelectionBlock = _engine.CursorBlock;
                    _engine.SelectionRun = _engine.CursorRun;
                    _engine.SelectionOffset = _engine.CursorOffset;
                }

                if (_engine.CursorBlock + 1 < _engine.Document.Blocks.Count)
                {
                    _engine.CursorBlock++;
                    _engine.CursorRun = 0;
                    _engine.CursorOffset = 0;
                    if (!shift)
                    {
                        _engine.SelectionBlock = _engine.CursorBlock;
                        _engine.SelectionRun = _engine.CursorRun;
                        _engine.SelectionOffset = _engine.CursorOffset;
                    }
                }

                break;
            default:
                base.OnKeyDown(e);
                return;
        }

        e.Handled = true;
        Invalidate();
        ContentChanged?.Invoke(this, EventArgs.Empty);
        base.OnKeyDown(e);
    }

    protected internal override void OnTextInput(string text)
    {
        if (_readOnly || string.IsNullOrEmpty(text)) return;
        foreach (char c in text)
            if (c < 32)
                return;
        _engine.InsertText(text);
        Invalidate();
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