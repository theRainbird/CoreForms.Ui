using OldSchoolForms.Ui.Core;
using OldSchoolForms.Ui.Theming;
using Graphics = OldSchoolForms.Ui.Rendering.Graphics;

namespace OldSchoolForms.Ui.Controls.Basic;

/// <summary>
/// Provides a multi-line text editing control with word wrap and auto-show scrollbars.
/// </summary>
public class MemoBox : Control
{
    private readonly MultiLineTextEditorEngine _engine = new();
    private MemoBoxContext? _context;
    private readonly ScrollBarEngine _vScrollBar = new();
    private readonly ScrollBarEngine _hScrollBar = new();
    private MemoBoxScrollBarContext? _scrollBarContext;
    private int _textAreaX = 2;
    private int _textAreaY = 2;
    private int _textAreaWidth;
    private int _textAreaHeight;

    /// <summary>
    /// Initializes a new instance of <see cref="MemoBox"/>.
    /// </summary>
    public MemoBox()
    {
        var theme = ThemeManager.CurrentTheme;
        _backColor = theme.TextBoxBackground;
        _foreColor = theme.TextBoxText;
        Size = new Size(200, 120);
        TabStop = true;
        _engine.TextChanged += (s, e) => { OnTextChanged(); OnPropertyChanged(nameof(Text)); Invalidate(); };
        _engine.ScrollOffsetYChanged += (s, e) => Invalidate();

        _vScrollBar.Scroll += (s, e) =>
        {
            _engine.ScrollTo(_vScrollBar.Value, _textAreaHeight);
            Invalidate();
        };

        _hScrollBar.Scroll += (s, e) =>
        {
            Invalidate();
        };
    }

    /// <summary>
    /// Called when the theme changes.
    /// </summary>
    public override void OnThemeChanged(Theme newTheme)
    {
        if (!_backColorSet)
            _backColor = newTheme.TextBoxBackground;
        if (!_foreColorSet)
            _foreColor = newTheme.TextBoxText;
        Invalidate();
    }

    private MemoBoxContext Context => _context ??= new MemoBoxContext(this);
    private MemoBoxScrollBarContext ScrollBarCtx => _scrollBarContext ??= new MemoBoxScrollBarContext(this);

    /// <summary>
    /// Gets or sets the text content.
    /// </summary>
    public new string Text
    {
        get => _engine.Text;
        set
        {
            if (_engine.Text == value) return;
            _engine.Text = value;
            _engine.EnsureCursorVisible(Context);
            OnTextChanged();
            OnPropertyChanged(nameof(Text));
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets whether text wraps at the available width. Default is true.
    /// </summary>
    public bool WordWrap
    {
        get => _engine.WordWrap;
        set
        {
            if (_engine.WordWrap == value) return;
            _engine.WordWrap = value;
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the cursor position as a zero-based character index.
    /// </summary>
    public int SelectionStart
    {
        get => _engine.SelectionStart;
        set
        {
            if (_engine.SelectionStart != value)
            {
                _engine.SelectionStart = value;
                _engine.EnsureCursorVisible(Context);
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets the number of selected characters.
    /// </summary>
    public int SelectionLength
    {
        get => _engine.SelectionLength;
        set
        {
            if (_engine.SelectionLength != value)
            {
                _engine.SelectionLength = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Copies the selected text to the clipboard.
    /// </summary>
    public void CopyToClipboard() => _engine.CopyToClipboard();

    /// <summary>
    /// Cuts the selected text to the clipboard.
    /// </summary>
    public void Cut() => _engine.Cut(Context);

    /// <summary>
    /// Pastes clipboard text at the cursor position.
    /// </summary>
    public void Paste() => _engine.Paste(Context);

    /// <summary>
    /// Selects all text.
    /// </summary>
    public void SelectAll()
    {
        _engine.SelectAll(Context);
        Invalidate();
    }

    /// <inheritdoc />
    public override void Render(Graphics g)
    {
        if (!Visible) return;

        var theme = ThemeManager.CurrentTheme;
        var font = EffectiveFont;
        float zoom = EffectiveZoom;
        int lineHeight = _engine.GetLineHeight(Context);

        _textAreaX = 2;
        _textAreaY = 2;
        int innerW = Width - 4;
        int innerH = Height - 4;

        // Compute scrollbar visibility and text area
        int totalContentH = _engine.ComputeContentHeight(innerW, Context);
        bool needVScroll = _engine.WordWrap
            ? totalContentH > innerH
            : totalContentH > innerH;

        int scrollBarSize = ScrollBarEngine.DefaultScrollBarSize;
        int vScrollW = needVScroll ? scrollBarSize : 0;
        int textW = innerW - vScrollW;
        int textH = innerH;

        // Recompute with actual text width for word wrap
        totalContentH = _engine.ComputeContentHeight(textW, Context);

        bool needHScroll = false;
        if (!_engine.WordWrap)
        {
            // For non-wrap mode, compute max line width
            int maxLineWidth = ComputeMaxLineWidth(Context);
            needHScroll = maxLineWidth > textW;
        }

        int hScrollH = needHScroll ? scrollBarSize : 0;

        // If both scrollbars, adjust
        if (needVScroll && needHScroll)
        {
            textH = innerH - hScrollH;
            totalContentH = _engine.ComputeContentHeight(textW, Context);
        }
        else if (needHScroll && !needVScroll)
        {
            textH = innerH - hScrollH;
            totalContentH = _engine.ComputeContentHeight(textW, Context);
            // Check if vertical now needed
            if (totalContentH > textH)
            {
                needVScroll = true;
                vScrollW = scrollBarSize;
                textW = innerW - vScrollW;
            }
        }
        else if (needVScroll && !needHScroll)
        {
            textH = innerH;
        }

        _textAreaWidth = textW;
        _textAreaHeight = textH;

        // Update scrollbar engines
        _vScrollBar.ViewSize = textH;
        _vScrollBar.ContentSize = totalContentH;
        _vScrollBar.SmallChange = lineHeight;
        _vScrollBar.LargeChange = lineHeight * 3;

        // Sync engine viewport
        _engine.ScrollTo(_engine.ScrollOffsetY, textH);

        // Sync scrollbar value to engine (fires Scroll event if changed)
        _vScrollBar.Value = _engine.ScrollOffsetY;

        if (!_engine.WordWrap)
        {
            int maxLineWidth = ComputeMaxLineWidth(Context);
            _hScrollBar.ViewSize = textW;
            _hScrollBar.ContentSize = maxLineWidth;
            _hScrollBar.SmallChange = 10;
            _hScrollBar.LargeChange = textW;
        }

        // Background
        g.FillRectangle(BackColor, 0, 0, Width, Height);

        // Clip to text area
        g.SetClip(new Rectangle(_textAreaX, _textAreaY, _textAreaWidth, _textAreaHeight));

        // Save and translate for text rendering
        g.Save();
        g.TranslateTransform(_textAreaX - _engine.ScrollOffset, _textAreaY - _engine.ScrollOffsetY);

        var textColor = Enabled ? ForeColor : theme.GrayText;
        int visualLines = _engine.GetVisualLineCount(_textAreaWidth, Context);
        int firstVisible = _engine.ScrollOffsetY / lineHeight;
        int lastVisible = (_engine.ScrollOffsetY + _textAreaHeight) / lineHeight + 1;
        lastVisible = Math.Min(lastVisible, visualLines);

        for (int vi = firstVisible; vi < lastVisible; vi++)
        {
            var (lineText, lineStart) = _engine.GetVisualLine(vi, _textAreaWidth, Context);
            int y = vi * lineHeight;

            // Draw selection for this visual line
            if (Enabled && _engine.HasSelection && Focused)
            {
                int selStart = _engine.SelectionStartIndex;
                int selEnd = _engine.SelectionEndIndex;
                int lineEnd = lineStart + lineText.Length;

                int visStart = Math.Max(lineStart, selStart);
                int visEnd = Math.Min(lineEnd, selEnd);

                if (visStart < visEnd)
                {
                    string beforeSel = lineText.Substring(0, visStart - lineStart);
                    string selStr = lineText.Substring(visStart - lineStart, visEnd - visStart);
                    float selX = _engine.MeasureTextWidth(beforeSel, Context);
                    float selWidth = Math.Max(_engine.MeasureTextWidth(selStr, Context), 2);
                    float scaledLineHeight = font.Size * zoom + 2;

                    g.FillRectangle(theme.Highlight, selX, y, selWidth, scaledLineHeight);

                    // Draw full line then selection highlight on top
                    g.DrawString(lineText, font, textColor, 0, y);
                    g.DrawString(selStr, font, theme.HighlightText, selX, y);
                }
                else
                {
                    g.DrawString(lineText, font, textColor, 0, y);
                }
            }
            else
            {
                g.DrawString(lineText, font, textColor, 0, y);
            }
        }

        // Draw cursor
        if (Enabled && Focused && TextEditorEngine.IsCursorBlinkVisible)
        {
            int cursorVisLine = _engine.GetVisualLineFromPosition(_engine.CursorPosition, _textAreaWidth, Context);
            if (cursorVisLine >= firstVisible && cursorVisLine < lastVisible)
            {
                int colInLine = _engine.GetColumnInVisualLine(_engine.CursorPosition, _textAreaWidth, Context);
                var (cursorLineText, _) = _engine.GetVisualLine(cursorVisLine, _textAreaWidth, Context);
                float cursorX = _engine.MeasureTextWidth(
                    cursorLineText.Substring(0, Math.Min(colInLine, cursorLineText.Length)), Context);
                int cursorY = cursorVisLine * lineHeight;
                float scaledLineHeight = font.Size * zoom;

                g.DrawLine(theme.CursorLine, cursorX, cursorY, cursorX, cursorY + scaledLineHeight + 2, 1);
            }
        }

        g.Restore();
        g.ResetClip();

        // Draw scrollbars
        if (needVScroll)
        {
            var vBounds = new Rectangle(Width - scrollBarSize - 1, _textAreaY, scrollBarSize, textH);
            _vScrollBar.Render(g, vBounds, theme);
        }

        if (needHScroll)
        {
            var hBounds = new Rectangle(_textAreaX, Height - scrollBarSize - 1, textW, scrollBarSize);
            _hScrollBar.Render(g, hBounds, theme);
        }

        // Border
        if (Focused)
            g.DrawRectangle(theme.TextBoxFocusBorder, 0, 0, Width, Height, 2);
        else
            g.DrawRectangle(theme.TextBoxBorder, 0, 0, Width, Height, 1);

        base.Render(g);
    }

    private int ComputeMaxLineWidth(ITextEditorContext context)
    {
        if (string.IsNullOrEmpty(_engine.Text)) return 0;
        var lines = _engine.Text.Split('\n');
        int maxW = 0;
        foreach (var line in lines)
        {
            int w = _engine.MeasureTextWidth(line, context);
            if (w > maxW) maxW = w;
        }
        return maxW;
    }

    /// <inheritdoc />
    protected internal override void OnMouseDown(EventArgs e)
    {
        if (!Enabled) return;
        if (e is MouseEventArgs mouseArgs)
        {
            int scrollBarSize = ScrollBarEngine.DefaultScrollBarSize;

            // Check vertical scrollbar
            if (_vScrollBar.NeedsScrollbar)
            {
                var vBounds = new Rectangle(Width - scrollBarSize - 1, _textAreaY, scrollBarSize, _textAreaHeight);
                if (vBounds.Contains(mouseArgs.X, mouseArgs.Y))
                {
                    _vScrollBar.HandleMouseDown(new Point(mouseArgs.X, mouseArgs.Y), vBounds, ScrollBarCtx);
                    return;
                }
            }

            // Check horizontal scrollbar
            if (_hScrollBar.NeedsScrollbar && !_engine.WordWrap)
            {
                var hBounds = new Rectangle(_textAreaX, Height - scrollBarSize - 1, _textAreaWidth, scrollBarSize);
                if (hBounds.Contains(mouseArgs.X, mouseArgs.Y))
                {
                    _hScrollBar.HandleMouseDown(new Point(mouseArgs.X, mouseArgs.Y), hBounds, ScrollBarCtx);
                    return;
                }
            }

            // Text click
            int logicalX = mouseArgs.X - _textAreaX + _engine.ScrollOffset;
            int logicalY = mouseArgs.Y - _textAreaY + _engine.ScrollOffsetY;
            _engine.HandleMouseDown(logicalX, logicalY, Context);
        }

        Focused = true;
        base.OnMouseDown(e);
    }

    /// <inheritdoc />
    protected internal override void OnMouseUp(EventArgs e)
    {
        if (_vScrollBar.IsDragging || _vScrollBar.IsUpButtonPressed || _vScrollBar.IsDownButtonPressed)
            _vScrollBar.HandleMouseUp(ScrollBarCtx);
        if (_hScrollBar.IsDragging || _hScrollBar.IsUpButtonPressed || _hScrollBar.IsDownButtonPressed)
            _hScrollBar.HandleMouseUp(ScrollBarCtx);
        base.OnMouseUp(e);
    }

    /// <inheritdoc />
    protected internal override void OnMouseMove(EventArgs e)
    {
        if (e is MouseEventArgs mouseArgs)
        {
            int scrollBarSize = ScrollBarEngine.DefaultScrollBarSize;

            if (_vScrollBar.NeedsScrollbar)
            {
                var vBounds = new Rectangle(Width - scrollBarSize - 1, _textAreaY, scrollBarSize, _textAreaHeight);
                _vScrollBar.HandleMouseMove(new Point(mouseArgs.X, mouseArgs.Y), vBounds, ScrollBarCtx);
            }

            if (_hScrollBar.NeedsScrollbar && !_engine.WordWrap)
            {
                var hBounds = new Rectangle(_textAreaX, Height - scrollBarSize - 1, _textAreaWidth, scrollBarSize);
                _hScrollBar.HandleMouseMove(new Point(mouseArgs.X, mouseArgs.Y), hBounds, ScrollBarCtx);
            }
        }
        base.OnMouseMove(e);
    }

    /// <inheritdoc />
    protected internal override void OnMouseWheel(EventArgs e)
    {
        if (!Enabled) return;
        if (e is MouseEventArgs me)
        {
            _vScrollBar.ViewSize = _textAreaHeight;
            _vScrollBar.ContentSize = _engine.ComputeContentHeight(_textAreaWidth, Context);
            if (_vScrollBar.NeedsScrollbar && !_vScrollBar.IsDragging)
            {
                _vScrollBar.HandleMouseWheel(me.Delta, ScrollBarCtx);
            }
        }
        base.OnMouseWheel(e);
    }

    /// <inheritdoc />
    protected internal override void OnKeyDown(KeyEventArgs e)
    {
        if (!Enabled) return;
        _engine.HandleKeyDown(e, Context);
        base.OnKeyDown(e);
    }

    /// <inheritdoc />
    protected internal override void OnTextInput(string text)
    {
        if (!Enabled) return;
        _engine.HandleTextInput(text, Context);
    }

    /// <summary>
    /// Raises the TextChanged event.
    /// </summary>
    protected override void OnTextChanged()
    {
        base.OnTextChanged();
        TextChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    protected internal override void OnFocused()
    {
        base.OnFocused();
    }

    /// <summary>
    /// Occurs when the text changes.
    /// </summary>
    public event EventHandler? TextChanged;

    private sealed class MemoBoxContext : ITextEditorContext
    {
        private readonly MemoBox _owner;
        public MemoBoxContext(MemoBox owner) => _owner = owner;
        public Font Font => _owner.EffectiveFont;
        public float Zoom => _owner.EffectiveZoom;
        public int TextAreaWidth => _owner._textAreaWidth;
        public int TextAreaHeight => _owner._textAreaHeight;
        public void Invalidate() => _owner.Invalidate();
    }

    private sealed class MemoBoxScrollBarContext : IScrollBarContext
    {
        private readonly MemoBox _owner;
        public MemoBoxScrollBarContext(MemoBox owner) => _owner = owner;
        public float Zoom => _owner.EffectiveZoom;
        public void Invalidate() => _owner.Invalidate();
        public void CaptureMouse(bool capture) => _owner.CapturingMouse = capture;
    }
}
