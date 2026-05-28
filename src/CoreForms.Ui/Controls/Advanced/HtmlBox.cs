using CoreForms.Ui.Core;
using CoreForms.Ui.Theming;
using CoreForms.Ui.Html;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Advanced;

/// <summary>
/// A WYSIWYG HTML editor control with full formatting support.
/// Supports bold, italic, underline, strikethrough, font families, font sizes,
/// text colors, lists, tables, images, hyperlinks, and horizontal rules.
/// Uses SkiaSharp for rendering and HtmlAgilityPack for HTML import/export.
/// </summary>
public class HtmlBox : Control
{
    private readonly RichTextEngine _engine = new();
    private string _htmlBacking = string.Empty;
    private bool _readOnly;
    private LinkBehavior _linkBehavior = LinkBehavior.RaiseEvent;
    private static readonly int CursorBlinkInterval = 530;

    // Cached layout
    private List<VisualLine>? _cachedLines;
    private float _cachedAvailableWidth;
    private float _cachedZoom;
    private float _documentWidth;
    private float _documentHeight;
    private bool _layoutDirty = true;

    // Cursor screen position
    private int _cursorScreenX;
    private int _cursorScreenY;
    private bool _cursorScreenValid;
    private int _preferredX = -1;

    /// <summary>
    /// Initializes a new instance of the <see cref="HtmlBox"/> class.
    /// </summary>
    public HtmlBox()
    {
        var theme = ThemeManager.CurrentTheme;
        _backColor = theme.TextBoxBackground;
        _foreColor = theme.TextBoxText;
        Size = new Size(300, 200);
        TabStop = true;
        _engine.InitFromHtml(string.Empty);
    }

    /// <summary>
    /// Gets or sets the HTML content of the editor.
    /// </summary>
    public string Html
    {
        get => _engine.ToHtml();
        set
        {
            if (value != _htmlBacking)
            {
                _htmlBacking = value ?? string.Empty;
                _engine.InitFromHtml(_htmlBacking);
                InvalidateLayout();
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the editor is read-only.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the link behavior when a link is clicked.
    /// </summary>
    public LinkBehavior LinkBehavior
    {
        get => _linkBehavior;
        set => _linkBehavior = value;
    }

    /// <summary>Occurs when the HTML content changes.</summary>
    public event EventHandler? ContentChanged;

    /// <summary>Occurs when a link is clicked.</summary>
    public event EventHandler<HtmlLinkEventArgs>? LinkClick;

    /// <summary>Occurs when HTML parsing encounters errors.</summary>
    public event EventHandler<HtmlErrorEventArgs>? ParseError;

    // --- Formatting state queries ---

    /// <summary>Gets whether the text at the cursor is bold.</summary>
    public bool IsBold => _engine.GetFontStyleAtCursor().HasFlag(FontStyle.Bold);

    /// <summary>Gets whether the text at the cursor is italic.</summary>
    public bool IsItalic => _engine.GetFontStyleAtCursor().HasFlag(FontStyle.Italic);

    /// <summary>Gets whether the text at the cursor is underlined.</summary>
    public bool IsUnderline => _engine.GetFontStyleAtCursor().HasFlag(FontStyle.Underline);

    /// <summary>Gets whether the text at the cursor is struck through.</summary>
    public bool IsStrikeout => _engine.GetFontStyleAtCursor().HasFlag(FontStyle.Strikeout);

    /// <summary>Gets the font family at the cursor.</summary>
    public string FontName => _engine.GetFontFamilyAtCursor();

    /// <summary>Gets the font size at the cursor.</summary>
    public float FontSizeValue => _engine.GetFontSizeAtCursor();

    /// <summary>Gets debug info about the cursor position.</summary>
    public string CursorDebug =>
        $"B:{_engine.CursorBlock} C:{_engine.CursorContent} O:{_engine.CursorOffset}";

    /// <summary>
    /// Applies a formatting command.
    /// </summary>
    public void ApplyFormat(string formatType)
    {
        switch (formatType)
        {
            case "bold": _engine.ToggleBold(); break;
            case "italic": _engine.ToggleItalic(); break;
            case "underline": _engine.ToggleUnderline(); break;
            case "strikethrough": _engine.ToggleStrikeout(); break;
            case "insertParagraph": _engine.InsertParagraph(); break;
            case "insertLineBreak": _engine.InsertLineBreak(); break;
            case "insertHorizontalRule": _engine.InsertHorizontalRule(); break;
        }

        InvalidateLayout();
        Invalidate();
        ContentChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Applies a formatting command with a value.
    /// </summary>
    public void ApplyFormat(string command, string value)
    {
        switch (command)
        {
            case "fontName":
                _engine.ApplyFontFamily(value);
                break;
            case "fontSize":
                if (float.TryParse(value, out var fs))
                    _engine.ApplyFontSize(fs);
                break;
            case "foreColor":
                var color = ParseColor(value);
                if (!color.Equals(Color.Empty))
                    _engine.ApplyForeColor(color);
                break;
        }

        InvalidateLayout();
        Invalidate();
        ContentChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Draws a cursor at the current cursor position.</summary>
    public void ShowCursor() => Invalidate();

    private static Color ParseColor(string val)
    {
        val = val.Trim().TrimStart('#');
        if (val.Length == 6 && int.TryParse(val, System.Globalization.NumberStyles.HexNumber, null, out var rgb))
            return Color.FromArgb((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
        return Color.Empty;
    }

    /// <inheritdoc/>
    public override void OnThemeChanged(Theme newTheme)
    {
        if (!_backColorSet) _backColor = newTheme.TextBoxBackground;
        if (!_foreColorSet) _foreColor = newTheme.TextBoxText;
        InvalidateLayout();
        Invalidate();
    }

    /// <inheritdoc/>
    public override void Render(Graphics g)
    {
        if (!Visible) return;

        g.FillRectangle(BackColor, 0, 0, Width, Height);

        if (Focused)
            g.DrawRectangle(ThemeManager.CurrentTheme.TextBoxFocusBorder, 0, 0, Width, Height, 2);
        else
            g.DrawRectangle(ThemeManager.CurrentTheme.TextBoxBorder, 0, 0, Width, Height, 1);

        float zoom = g.Zoom;
        EnsureLayout(Width, zoom);

        if (_cachedLines == null) return;

        _cursorScreenValid = false;
        int cursorFlat = _engine.CursorFlatIndex;
        int selStart = Math.Min(_engine.CursorFlatIndex, _engine.SelectionFlatIndex);
        int selEnd = Math.Max(_engine.CursorFlatIndex, _engine.SelectionFlatIndex);
        bool hasSel = _engine.HasSelection;

        float padding = 8;
        int numberCounter = 0;
        int lastBlockIndex = -1;

        for (int li = 0; li < _cachedLines.Count; li++)
        {
            var line = _cachedLines[li];
            var block = line.Block;
            if (block == null) continue;

            // Count list numbers only when block changes
            int bi = line.BlockIndex;
            if (bi != lastBlockIndex)
            {
                lastBlockIndex = bi;
                if (bi >= 0 && bi < _engine.Document.Blocks.Count)
                {
                    var b = _engine.Document.Blocks[bi];
                    if (b.Type == RichTextBlockType.NumberItem)
                        numberCounter++;
                    else if (b.Type != RichTextBlockType.BulletItem)
                        numberCounter = 0;
                }
            }

            float leftMargin = GetLeftMargin(line.Block!.Type);
            float fontSize = GetBlockFontSize(line.Block.Type);
            float markerX = padding;

            // Draw list marker on first line of each list item
            if (block.Type is RichTextBlockType.BulletItem or RichTextBlockType.NumberItem
                && (li == 0 || _cachedLines[li - 1].BlockIndex != bi))
            {
                if (block.Type == RichTextBlockType.BulletItem)
                {
                    float dotSize = fontSize * 0.4f;
                    float dotY = line.Y + (fontSize * 0.4f);
                    g.FillEllipse(ForeColor, markerX + 2, dotY, dotSize, dotSize);
                }
                else
                {
                    g.DrawString($"{numberCounter}.", new Font("Arial", fontSize, FontStyle.Regular), ForeColor, markerX, line.Y);
                }
            }

            foreach (var run in line.Runs)
            {
                float runX = padding + leftMargin + run.X;
                float runY = line.Y;
                var source = run.Source;

                if (run.Source is TextRun tr)
                {
                    // Compute effective style: add bold for headings and underline for hyperlinks
                    var effectiveStyle = tr.Style;
                    if (block.Type is RichTextBlockType.Heading1 or RichTextBlockType.Heading2
                        or RichTextBlockType.Heading3 or RichTextBlockType.Heading4
                        or RichTextBlockType.Heading5 or RichTextBlockType.Heading6)
                        effectiveStyle |= FontStyle.Bold;

                    // Check if this run is inside a HyperlinkRun
                    bool isLink = false;
                    if (run.ContentIndex >= 0 && run.ContentIndex < block.Content.Count
                        && block.Content[run.ContentIndex] is HyperlinkRun)
                    {
                        isLink = true;
                        effectiveStyle |= FontStyle.Underline;
                    }

                    var font = new Font(tr.FontFamily, fontSize, effectiveStyle);
                    var textColor = isLink ? Color.FromArgb(0, 0, 238)
                        : (tr.ForeColor.Equals(Color.Empty) ? ForeColor : tr.ForeColor);
                    var measured = Platform.Platform.MeasureText(run.DisplayText, font, zoom);
                    float runHeight = (float)measured.height / zoom;

                    // Draw text
                    g.DrawString(run.DisplayText, font, textColor, runX, runY);

                    // Draw selection
                    if (hasSel)
                    {
                        int runStartFlat = TextLayoutEngine.ToFlatIndex(_engine.Document, line.BlockIndex,
                            run.ContentIndex, run.StartOffset);
                        int runEndFlat = runStartFlat + run.Length;

                        if (selStart < runEndFlat && selEnd > runStartFlat)
                        {
                            int localSelStart = Math.Max(0, selStart - runStartFlat);
                            int localSelEnd = Math.Min(run.Length, selEnd - runStartFlat);

                            string beforeSel = run.DisplayText[..localSelStart];
                            string selText = run.DisplayText[localSelStart..localSelEnd];
                            float selBeforeWidth = Platform.Platform.MeasureText(beforeSel, font, zoom).width / zoom;
                            float selTextWidth = Platform.Platform.MeasureText(selText, font, zoom).width / zoom;

                            int selX = (int)(runX + selBeforeWidth);
                            int selW = (int)Math.Max(selTextWidth, 2);
                            g.FillRectangle(ThemeManager.CurrentTheme.Highlight, selX, runY, selW, runHeight);
                            g.DrawString(selText, font, ThemeManager.CurrentTheme.HighlightText, selX, runY);
                        }
                    }

                    // Cursor positioning — use model position, not flat index
                    // This avoids ambiguity at block boundaries (both blocks have the same flat index)
                    if (!_cursorScreenValid)
                    {
                        if (line.BlockIndex == _engine.CursorBlock &&
                            run.ContentIndex == _engine.CursorContent &&
                            run.StartOffset <= _engine.CursorOffset &&
                            _engine.CursorOffset <= run.StartOffset + run.Length)
                        {
                            int localOff = _engine.CursorOffset - run.StartOffset;
                            string beforeCursor = run.DisplayText[..Math.Min(localOff, run.DisplayText.Length)];
                            float measuredWidth = Platform.Platform.MeasureText(beforeCursor, font, zoom).width;
                            _cursorScreenX = (int)(runX + measuredWidth / zoom);
                            _cursorScreenY = (int)runY;
                            _cursorScreenValid = true;
                        }
                    }
                }
                else if (source is ImageRun image)
                {
                    // Draw image placeholder
                    g.FillRectangle(ThemeManager.CurrentTheme.TextBoxText, runX, runY, 32, 32);
                }
            }

            if (!_cursorScreenValid && line.BlockIndex == _engine.CursorBlock && line.Runs.Count == 0)
            {
                _cursorScreenX = (int)(padding + GetLeftMargin(line.Block!.Type));
                _cursorScreenY = (int)line.Y;
                _cursorScreenValid = true;
            }
        }

        // End-of-document fallback: cursor at total length
        if (!_cursorScreenValid && _cachedLines != null && _cachedLines.Count > 0)
        {
            int totalLen = _engine.Document.TotalLength;
            if (cursorFlat >= totalLen && totalLen > 0)
            {
                var lastLine = _cachedLines[^1];
                if (lastLine.Runs.Count > 0)
                {
                    var last = lastLine.Runs[^1];
                    float lx = padding + GetLeftMargin(lastLine.Block?.Type ?? 0) + last.X + last.Width;
                    _cursorScreenX = (int)lx;
                    _cursorScreenY = (int)lastLine.Y;
                    _cursorScreenValid = true;
                }
            }
        }

        DrawCursor(g);
        base.Render(g);
    }

    private void InvalidateLayout()
    {
        _layoutDirty = true;
        _preferredX = -1;
    }

    private void EnsureLayout(float availableWidth, float zoom)
    {
        if (!_layoutDirty && _cachedLines != null &&
            Math.Abs(_cachedAvailableWidth - availableWidth) < 0.5f &&
            Math.Abs(_cachedZoom - zoom) < 0.01f)
            return;

        _cachedLines = TextLayoutEngine.Layout(
            _engine.Document, Math.Max(availableWidth - 8, 50), zoom,
            out _documentWidth, out _documentHeight);
        _cachedAvailableWidth = availableWidth;
        _cachedZoom = zoom;
        _layoutDirty = false;
    }

    private void DrawCursor(Graphics g)
    {
        if (!Focused || _readOnly) return;

        bool visible = (Environment.TickCount % (CursorBlinkInterval * 2)) < CursorBlinkInterval;
        if (!visible) return;
        if (!_cursorScreenValid)
        {
            _cursorScreenX = 4;
            _cursorScreenY = 4;
        }

        int cy = _cursorScreenY;
        if (cy < 0 || cy >= Height) return;
        if (cy + 16 > Height) cy = Height - 16;

        g.DrawLine(ForeColor, _cursorScreenX, cy, _cursorScreenX, cy + 14, 1);
    }

    /// <inheritdoc/>
    protected internal override void OnMouseDown(EventArgs e)
    {
        if (e is MouseEventArgs mouseArgs)
        {
            float zoom = EffectiveZoom;
            EnsureLayout(Width, zoom);

            if (_cachedLines != null && _cachedLines.Count > 0)
            {
                var pos = TextLayoutEngine.HitTest(
                    _engine.Document, _cachedLines,
                    mouseArgs.X, mouseArgs.Y, zoom);

                _engine.CursorBlock = pos.BlockIndex;
                _engine.CursorContent = pos.ContentIndex;
                _engine.CursorOffset = pos.CharOffset;
                _engine.SelectionBlock = _engine.CursorBlock;
                _engine.SelectionContent = _engine.CursorContent;
                _engine.SelectionOffset = _engine.CursorOffset;
                _preferredX = -1;
            }

            CapturingMouse = true;
            Invalidate();
            ContentChanged?.Invoke(this, EventArgs.Empty);
        }

        base.OnMouseDown(e);
        Focused = true;
    }

    /// <inheritdoc/>
    protected internal override void OnMouseMove(EventArgs e)
    {
        if (e is MouseEventArgs mouseArgs && IsPressed && _cachedLines != null)
        {
            float zoom = EffectiveZoom;
            EnsureLayout(Width, zoom);

            if (_cachedLines != null && _cachedLines.Count > 0)
            {
                var pos = TextLayoutEngine.HitTest(
                    _engine.Document, _cachedLines,
                    mouseArgs.X, mouseArgs.Y, zoom);

                _engine.CursorBlock = pos.BlockIndex;
                _engine.CursorContent = pos.ContentIndex;
                _engine.CursorOffset = pos.CharOffset;
            }

            Invalidate();
        }

        base.OnMouseMove(e);
    }

    /// <inheritdoc/>
    protected internal override void OnMouseUp(EventArgs e)
    {
        CapturingMouse = false;
        Invalidate();
        ContentChanged?.Invoke(this, EventArgs.Empty);

        base.OnMouseUp(e);
    }

    /// <inheritdoc/>
    protected internal override void OnKeyDown(KeyEventArgs e)
    {
        if (_readOnly) { base.OnKeyDown(e); return; }

        bool shift = e.Modifiers.HasFlag(ModifierKeys.Shift);
        bool ctrl = e.Modifiers.HasFlag(ModifierKeys.Control);

        // Ctrl+ shortcuts
        if (ctrl)
        {
            switch (e.KeyCode)
            {
                case Keys.B: ApplyFormat("bold"); e.Handled = true; return;
                case Keys.I: ApplyFormat("italic"); e.Handled = true; return;
                case Keys.U: ApplyFormat("underline"); e.Handled = true; return;
                case Keys.S: ApplyFormat("strikethrough"); e.Handled = true; return;
                case Keys.Home:
                    _engine.CursorBlock = 0;
                    _engine.CursorContent = 0;
                    _engine.CursorOffset = 0;
                    _engine.SelectionBlock = 0;
                    _engine.SelectionContent = 0;
                    _engine.SelectionOffset = 0;
                    e.Handled = true; return;
                case Keys.End:
                    {
                        int lastB = _engine.Document.Blocks.Count - 1;
                        if (lastB >= 0)
                        {
                            var blk = _engine.Document.Blocks[lastB];
                            int lastC = blk.Content.Count - 1;
                            int lastOff = lastC >= 0 && blk.Content[lastC] is TextRun tr ? tr.Length : 0;
                            _engine.CursorBlock = lastB;
                            _engine.CursorContent = Math.Max(0, lastC);
                            _engine.CursorOffset = lastOff;
                            _engine.SelectionBlock = lastB;
                            _engine.SelectionContent = Math.Max(0, lastC);
                            _engine.SelectionOffset = lastOff;
                        }
                        e.Handled = true; return;
                    }
            }
        }

        switch (e.KeyCode)
        {
            case Keys.Back:
                _engine.HandleBackspace();
                break;
            case Keys.Delete:
                _engine.HandleDelete();
                break;
            case Keys.Enter:
                if (shift) _engine.InsertLineBreak();
                else _engine.HandleEnter();
                break;
            case Keys.Left:
                if (shift && !_engine.HasSelection)
                    SyncSelectionAnchor();
                _engine.MoveLeft();
                if (!shift)
                {
                    _engine.SelectionBlock = _engine.CursorBlock;
                    _engine.SelectionContent = _engine.CursorContent;
                    _engine.SelectionOffset = _engine.CursorOffset;
                }
                break;
            case Keys.Right:
                if (shift && !_engine.HasSelection)
                    SyncSelectionAnchor();
                _engine.MoveRight();
                if (!shift)
                {
                    _engine.SelectionBlock = _engine.CursorBlock;
                    _engine.SelectionContent = _engine.CursorContent;
                    _engine.SelectionOffset = _engine.CursorOffset;
                }
                break;
            case Keys.Up:
                if (shift && !_engine.HasSelection)
                    SyncSelectionAnchor();
                MoveVisualLineUp();
                if (!shift)
                {
                    _engine.SelectionBlock = _engine.CursorBlock;
                    _engine.SelectionContent = _engine.CursorContent;
                    _engine.SelectionOffset = _engine.CursorOffset;
                }
                break;
            case Keys.Down:
                if (shift && !_engine.HasSelection)
                    SyncSelectionAnchor();
                MoveVisualLineDown();
                if (!shift)
                {
                    _engine.SelectionBlock = _engine.CursorBlock;
                    _engine.SelectionContent = _engine.CursorContent;
                    _engine.SelectionOffset = _engine.CursorOffset;
                }
                break;
            case Keys.Home:
                if (shift && !_engine.HasSelection)
                    SyncSelectionAnchor();
                MoveToVisualLineStart();
                if (!shift)
                {
                    _engine.SelectionBlock = _engine.CursorBlock;
                    _engine.SelectionContent = _engine.CursorContent;
                    _engine.SelectionOffset = _engine.CursorOffset;
                }
                break;
            case Keys.End:
                if (shift && !_engine.HasSelection)
                    SyncSelectionAnchor();
                MoveToVisualLineEnd();
                if (!shift)
                {
                    _engine.SelectionBlock = _engine.CursorBlock;
                    _engine.SelectionContent = _engine.CursorContent;
                    _engine.SelectionOffset = _engine.CursorOffset;
                }
                break;
            default:
                base.OnKeyDown(e);
                return;
        }

        e.Handled = true;

        bool textModified = e.KeyCode is Keys.Back or Keys.Delete or Keys.Enter;
        if (textModified)
            InvalidateLayout();
        Invalidate();
        ContentChanged?.Invoke(this, EventArgs.Empty);
        base.OnKeyDown(e);
    }

    /// <summary>Handles text input from keyboard.</summary>
    protected internal override void OnTextInput(string text)
    {
        if (_readOnly || string.IsNullOrEmpty(text)) { base.OnTextInput(text); return; }
        _engine.InsertText(text);
        InvalidateLayout();
        Invalidate();
        ContentChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SyncSelectionAnchor()
    {
        _engine.SelectionBlock = _engine.CursorBlock;
        _engine.SelectionContent = _engine.CursorContent;
        _engine.SelectionOffset = _engine.CursorOffset;
    }

    private void MoveVisualLineUp()
    {
        EnsureLayout(Width, EffectiveZoom);
        if (_cachedLines == null || _cachedLines.Count == 0)
        {
            _engine.MoveUp();
            return;
        }

        _preferredX = _cursorScreenX;
        int cursorFlat = _engine.CursorFlatIndex;

        for (int li = 0; li < _cachedLines.Count; li++)
        {
            var line = _cachedLines[li];
            if (line.BlockIndex != _engine.CursorBlock) continue;

            bool found = false;
            foreach (var run in line.Runs)
            {
                int runStart = TextLayoutEngine.ToFlatIndex(_engine.Document,
                    line.BlockIndex, run.ContentIndex, run.StartOffset);
                int runEnd = runStart + run.Length;
                if (cursorFlat >= runStart && cursorFlat <= runEnd)
                {
                    found = true;
                    break;
                }
            }

            if (!found && !(line.BlockIndex == _engine.CursorBlock && line.Runs.Count == 0))
                continue;

            // Find target visual line (previous in same block, or last of previous block)
            VisualLine? targetLine = null;
            if (li > 0 && _cachedLines[li - 1].BlockIndex == _engine.CursorBlock)
            {
                targetLine = _cachedLines[li - 1];
            }
            else
            {
                for (int nl = li - 1; nl >= 0; nl--)
                {
                    if (_cachedLines[nl].BlockIndex != _engine.CursorBlock)
                    {
                        targetLine = _cachedLines[nl];
                        break;
                    }
                }
            }

            if (targetLine != null)
            {
                var pos = FindPositionAtXInLine(targetLine, _preferredX, EffectiveZoom);
                _engine.CursorBlock = pos.BlockIndex;
                _engine.CursorContent = pos.ContentIndex;
                _engine.CursorOffset = pos.CharOffset;
            }
            else
            {
                _engine.MoveUp();
            }
            return;
        }
        // Fallback: no visual line found in layout — use block-level navigation
        _engine.MoveUp();
    }

    private void MoveVisualLineDown()
    {
        EnsureLayout(Width, EffectiveZoom);
        if (_cachedLines == null || _cachedLines.Count == 0)
        {
            _engine.MoveDown();
            return;
        }

        _preferredX = _cursorScreenX;
        int cursorFlat = _engine.CursorFlatIndex;

        for (int li = 0; li < _cachedLines.Count; li++)
        {
            var line = _cachedLines[li];
            if (line.BlockIndex != _engine.CursorBlock) continue;

            bool found = false;
            foreach (var run in line.Runs)
            {
                int runStart = TextLayoutEngine.ToFlatIndex(_engine.Document,
                    line.BlockIndex, run.ContentIndex, run.StartOffset);
                int runEnd = runStart + run.Length;
                if (cursorFlat >= runStart && cursorFlat <= runEnd)
                {
                    found = true;
                    break;
                }
            }

            if (!found && !(line.BlockIndex == _engine.CursorBlock && line.Runs.Count == 0))
                continue;

            // Find target visual line (next in same block, or first of next block)
            VisualLine? targetLine = null;
            if (li + 1 < _cachedLines.Count && _cachedLines[li + 1].BlockIndex == _engine.CursorBlock)
            {
                targetLine = _cachedLines[li + 1];
            }
            else
            {
                for (int nl = li + 1; nl < _cachedLines.Count; nl++)
                {
                    if (_cachedLines[nl].BlockIndex != _engine.CursorBlock)
                    {
                        targetLine = _cachedLines[nl];
                        break;
                    }
                }
            }

            if (targetLine != null)
            {
                var pos = FindPositionAtXInLine(targetLine, _preferredX, EffectiveZoom);
                _engine.CursorBlock = pos.BlockIndex;
                _engine.CursorContent = pos.ContentIndex;
                _engine.CursorOffset = pos.CharOffset;
            }
            else
            {
                _engine.MoveDown();
            }
            return;
        }
        // Fallback: no visual line found in layout — use block-level navigation
        _engine.MoveDown();
    }

    private DocumentPosition FindPositionAtXInLine(VisualLine line, int prefX, float zoom)
    {
        float padding = 8;
        float leftMargin = GetLeftMargin(line.Block?.Type ?? 0);
        float lineStartX = padding + leftMargin;

        foreach (var run in line.Runs)
        {
            float runX = lineStartX + run.X;

            if (run.Source is TextRun tr)
            {
                string text = run.DisplayText;
                if (string.IsNullOrEmpty(text))
                {
                    if (prefX <= runX)
                        return new DocumentPosition(line.BlockIndex, run.ContentIndex, run.StartOffset);
                    continue;
                }

                float blockFontSize = GetBlockFontSize(line.Block?.Type ?? RichTextBlockType.Paragraph);
                var measFont = new Font(tr.FontFamily, blockFontSize, tr.Style);

                if (prefX <= runX)
                {
                    return new DocumentPosition(line.BlockIndex, run.ContentIndex, run.StartOffset);
                }

                for (int i = 1; i <= text.Length; i++)
                {
                    string sub = text[..i];
                    float measured = Platform.Platform.MeasureText(sub, measFont, zoom).width / zoom;
                    float charX = runX + measured;

                    if (charX >= prefX)
                    {
                        return new DocumentPosition(line.BlockIndex, run.ContentIndex, run.StartOffset + i);
                    }
                }
            }
            else
            {
                if (prefX <= runX)
                    return new DocumentPosition(line.BlockIndex, run.ContentIndex, run.StartOffset);

                float runEndX = runX + run.Width;
                if (prefX <= runEndX)
                    return new DocumentPosition(line.BlockIndex, run.ContentIndex, run.StartOffset + Math.Min(1, run.Length));
            }
        }

        if (line.Runs.Count > 0)
        {
            var lastRun = line.Runs[^1];
            if (lastRun.Source is TextRun)
            {
                return new DocumentPosition(line.BlockIndex, lastRun.ContentIndex,
                    lastRun.StartOffset + lastRun.Length);
            }
            return new DocumentPosition(line.BlockIndex, lastRun.ContentIndex,
                lastRun.StartOffset + Math.Min(1, lastRun.Length));
        }

        return new DocumentPosition(line.BlockIndex, 0, 0);
    }

    private void MoveToVisualLineStart()
    {
        EnsureLayout(Width, EffectiveZoom);
        if (_cachedLines == null || _cachedLines.Count == 0) return;

        int cursorFlat = _engine.CursorFlatIndex;

        foreach (var line in _cachedLines)
        {
            if (line.BlockIndex != _engine.CursorBlock) continue;
            foreach (var run in line.Runs)
            {
                int runStart = TextLayoutEngine.ToFlatIndex(_engine.Document,
                    line.BlockIndex, run.ContentIndex, run.StartOffset);
                int runEnd = runStart + run.Length;
                if (cursorFlat >= runStart && cursorFlat <= runEnd)
                {
                    var first = line.Runs[0];
                    _engine.CursorBlock = line.BlockIndex;
                    _engine.CursorContent = first.ContentIndex;
                    _engine.CursorOffset = first.StartOffset;
                    return;
                }
            }
            if (line.BlockIndex == _engine.CursorBlock && line.Runs.Count == 0)
            {
                _engine.CursorContent = 0;
                _engine.CursorOffset = 0;
                return;
            }
        }
        // No matching line found — fallback: go to block start
        var b = _engine.Document.Blocks[_engine.CursorBlock];
        _engine.CursorContent = 0;
        _engine.CursorOffset = 0;
    }

    private void MoveToVisualLineEnd()
    {
        EnsureLayout(Width, EffectiveZoom);
        if (_cachedLines == null || _cachedLines.Count == 0) return;

        int cursorFlat = _engine.CursorFlatIndex;

        foreach (var line in _cachedLines)
        {
            if (line.BlockIndex != _engine.CursorBlock) continue;
            foreach (var run in line.Runs)
            {
                int runStart = TextLayoutEngine.ToFlatIndex(_engine.Document,
                    line.BlockIndex, run.ContentIndex, run.StartOffset);
                int runEnd = runStart + run.Length;
                if (cursorFlat >= runStart && cursorFlat <= runEnd)
                {
                    var last = line.Runs[^1];
                    _engine.CursorBlock = line.BlockIndex;
                    _engine.CursorContent = last.ContentIndex;
                    _engine.CursorOffset = last.StartOffset + last.Length;
                    return;
                }
            }
            if (line.BlockIndex == _engine.CursorBlock && line.Runs.Count == 0)
            {
                _engine.CursorContent = 0;
                _engine.CursorOffset = 0;
                return;
            }
        }
    }

    private static float GetLeftMargin(RichTextBlockType type) =>
        type is RichTextBlockType.BulletItem or RichTextBlockType.NumberItem ? 30 : 0;

    private static float GetBlockFontSize(RichTextBlockType type) => type switch
    {
        RichTextBlockType.Heading1 => 24,
        RichTextBlockType.Heading2 => 20,
        RichTextBlockType.Heading3 => 18,
        RichTextBlockType.Heading4 => 16,
        RichTextBlockType.Heading5 => 14,
        RichTextBlockType.Heading6 => 12,
        _ => 12
    };
}

/// <summary>
/// Defines how hyperlinks are handled when clicked.
/// </summary>
public enum LinkBehavior
{
    /// <summary>No action on link click.</summary>
    None,
    /// <summary>Raise the LinkClick event.</summary>
    RaiseEvent,
    /// <summary>Open the link in the default browser.</summary>
    OpenInBrowser
}

/// <summary>
/// Provides data for the <see cref="HtmlBox.LinkClick"/> event.
/// </summary>
public class HtmlLinkEventArgs : EventArgs
{
    /// <summary>The URL of the clicked link.</summary>
    public string Url { get; }

    /// <summary>Gets or sets whether the link click has been handled.</summary>
    public bool Handled { get; set; }

    /// <summary>Initializes a new instance of <see cref="HtmlLinkEventArgs"/>.</summary>
    public HtmlLinkEventArgs(string url)
    {
        Url = url;
    }
}

/// <summary>
/// Provides data for HTML parsing errors.
/// </summary>
public class HtmlErrorEventArgs : EventArgs
{
    /// <summary>Gets the list of error messages.</summary>
    public List<string> Errors { get; }

    /// <summary>Initializes a new instance of <see cref="HtmlErrorEventArgs"/>.</summary>
    public HtmlErrorEventArgs(List<string> errors)
    {
        Errors = errors;
    }
}
