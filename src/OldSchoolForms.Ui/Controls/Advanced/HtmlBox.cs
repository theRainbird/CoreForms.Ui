using OldSchoolForms.Ui.Core;
using OldSchoolForms.Ui.Theming;
using OldSchoolForms.Ui.Html;
using Graphics = OldSchoolForms.Ui.Rendering.Graphics;

namespace OldSchoolForms.Ui.Controls.Advanced;

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
    private string _lastComputedHtml = string.Empty;
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

    // Ctrl key state for link activation
    private bool _ctrlPressed;
    private bool _lastWasLink;

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
        get
        {
            var html = _engine.ToHtml();
            System.Console.WriteLine($"[HtmlBox] Html get: htmlLen={html.Length}, lastComputedLen={_lastComputedHtml.Length}, changed={html != _lastComputedHtml}");
            if (html != _lastComputedHtml)
            {
                _lastComputedHtml = html;
                System.Console.WriteLine($"[HtmlBox] Html get: firing ContentChanged");
                ContentChanged?.Invoke(this, EventArgs.Empty);
            }
            return html;
        }
        set
        {
            System.Console.WriteLine($"[HtmlBox] Html set: valueLen={value?.Length ?? 0}, backingLen={_htmlBacking.Length}, changed={value != _htmlBacking}");
            if (value != _htmlBacking)
            {
                _htmlBacking = value ?? string.Empty;
                _lastComputedHtml = string.Empty;
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
#pragma warning disable CS0067
    public event EventHandler<HtmlErrorEventArgs>? ParseError;
#pragma warning restore CS0067

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
        $"B:{_engine.CursorBlock} C:{_engine.CursorContent} O:{_engine.CursorOffset}" +
        (_engine.CursorCell >= 0 ? $" Cell:{_engine.CursorCell}" : "");

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
            case "insertUnorderedList": _engine.ToggleUnorderedList(); break;
            case "insertOrderedList": _engine.ToggleOrderedList(); break;
            case "createLink": _engine.CreateLink(); break;
            case "insertImage": _engine.InsertImage(); break;
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

    /// <summary>Inserts a table with the specified number of rows and columns at the cursor position.</summary>
    /// <param name="rows">The number of rows.</param>
    /// <param name="cols">The number of columns.</param>
    public void InsertTable(int rows, int cols)
    {
        _engine.InsertTable(rows, cols);
        InvalidateLayout();
        Invalidate();
        ContentChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Adds a new row after the current cursor row in the table.</summary>
    public void AddTableRow()
    {
        _engine.AddRow();
        InvalidateLayout();
        Invalidate();
        ContentChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Removes the row at the current cursor position in the table.</summary>
    public void RemoveTableRow()
    {
        _engine.RemoveRow();
        InvalidateLayout();
        Invalidate();
        ContentChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Adds a new column after the current cursor column in the table.</summary>
    public void AddTableColumn()
    {
        _engine.AddColumn();
        InvalidateLayout();
        Invalidate();
        ContentChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Removes the column at the current cursor position in the table.</summary>
    public void RemoveTableColumn()
    {
        _engine.RemoveColumn();
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

            RenderListMarker(g, line, li, bi, fontSize, padding);

            foreach (var run in line.Runs)
            {
                RenderRun(g, run, line, padding, leftMargin, zoom, hasSel, selStart, selEnd, cursorFlat);
            }

            if (!_cursorScreenValid && line.BlockIndex == _engine.CursorBlock && line.Runs.Count == 0)
            {
                _cursorScreenX = (int)(padding + GetLeftMargin(line.Block!.Type));
                _cursorScreenY = (int)line.Y;
                _cursorScreenValid = true;
            }
        }

        PositionEndOfDocumentCursor(padding, cursorFlat);

        DrawCursor(g);
        base.Render(g);
    }

    private void RenderListMarker(Graphics g, VisualLine line, int lineIndex, int blockIndex, float fontSize, float padding)
    {
        if (line.Block?.Type is not (RichTextBlockType.BulletItem or RichTextBlockType.NumberItem)) return;
        if (lineIndex > 0 && _cachedLines![lineIndex - 1].BlockIndex == blockIndex) return;

        float markerX = padding;
        if (line.Block.Type == RichTextBlockType.BulletItem)
        {
            float dotSize = fontSize * 0.4f;
            float dotY = line.Y + (fontSize * 0.4f);
            g.FillEllipse(ForeColor, markerX + 2, dotY, dotSize, dotSize);
        }
        else
        {
            // Count list numbers only when block changes — reuse the counter from Render loop
            int numberCounter = 0;
            for (int i = 0; i <= lineIndex; i++)
            {
                var b = _cachedLines![i].Block;
                if (b == null) continue;
                if (_cachedLines[i].BlockIndex != blockIndex)
                {
                    if (b.Type == RichTextBlockType.NumberItem)
                        numberCounter++;
                    else if (b.Type != RichTextBlockType.BulletItem)
                        numberCounter = 0;
                }
            }
            g.DrawString($"{numberCounter}.", new Font("Arial", fontSize, FontStyle.Regular), ForeColor, markerX, line.Y);
        }
    }

    private void RenderRun(Graphics g, LayoutRun run, VisualLine line, float padding, float leftMargin, float zoom,
        bool hasSel, int selStart, int selEnd, int cursorFlat)
    {
        float runX = padding + leftMargin + run.X;
        float runY = line.Y;
        var source = run.Source;

        if (run.Source is TextRun tr)
        {
            RenderTextRun(g, run, line, runX, runY, tr, zoom, hasSel, selStart, selEnd);
        }
        else if (source is ImageRun)
        {
            g.FillRectangle(ThemeManager.CurrentTheme.TextBoxText, runX, runY, 32, 32);
        }

        RenderTableGrid(g, run, line, runX, runY);
    }

    private void RenderTextRun(Graphics g, LayoutRun run, VisualLine line, float runX, float runY, TextRun tr, float zoom,
        bool hasSel, int selStart, int selEnd)
    {
        var block = line.Block;

        var effectiveStyle = tr.Style;
        if (block?.Type is RichTextBlockType.Heading1 or RichTextBlockType.Heading2
            or RichTextBlockType.Heading3 or RichTextBlockType.Heading4
            or RichTextBlockType.Heading5 or RichTextBlockType.Heading6)
            effectiveStyle |= FontStyle.Bold;

        bool isLink = run.ContentIndex >= 0 && run.ContentIndex < block!.Content.Count
            && block.Content[run.ContentIndex] is HyperlinkRun;
        if (isLink) effectiveStyle |= FontStyle.Underline;

        var font = new Font(tr.FontFamily, GetBlockFontSize(line.Block!.Type), effectiveStyle);
        var textColor = isLink ? Color.FromArgb(0, 0, 238)
            : (tr.ForeColor.Equals(Color.Empty) ? ForeColor : tr.ForeColor);
        var measured = Platform.Platform.MeasureText(run.DisplayText, font, zoom);
        float runHeight = (float)measured.height / zoom;

        g.DrawString(run.DisplayText, font, textColor, runX, runY);

        if (hasSel) RenderSelection(g, run, line, runX, runY, font, zoom, selStart, selEnd);
        if (!_cursorScreenValid) PositionCursor(run, line, runX, runY, font, zoom);
    }

    private void RenderSelection(Graphics g, LayoutRun run, VisualLine line, float runX, float runY, Font font, float zoom,
        int selStart, int selEnd)
    {
        int runStartFlat = TextLayoutEngine.ToFlatIndex(_engine.Document, line.BlockIndex,
            run.ContentIndex, run.StartOffset, run.CellIndex);
        int runEndFlat = runStartFlat + Math.Max(run.Length, 1);

        if (selStart < runEndFlat && selEnd > runStartFlat)
        {
            int localSelStart = Math.Max(0, selStart - runStartFlat);
            int localSelEnd = Math.Min(run.Length, selEnd - runStartFlat);

            string beforeSel = run.DisplayText[..Math.Min(localSelStart, run.DisplayText.Length)];
            string selText = localSelEnd > localSelStart && localSelStart < run.DisplayText.Length
                ? run.DisplayText[localSelStart..Math.Min(localSelEnd, run.DisplayText.Length)]
                : "";
            float selBeforeWidth = Platform.Platform.MeasureText(beforeSel, font, zoom).width / zoom;
            float selTextWidth = Platform.Platform.MeasureText(selText, font, zoom).width / zoom;

            int selX = (int)(runX + selBeforeWidth);
            int selW = (int)Math.Max(selTextWidth, 2);
            g.FillRectangle(ThemeManager.CurrentTheme.Highlight, selX, runY, selW, run.Height);
            if (!string.IsNullOrEmpty(selText))
                g.DrawString(selText, font, ThemeManager.CurrentTheme.HighlightText, selX, runY);
        }
    }

    private void PositionCursor(LayoutRun run, VisualLine line, float runX, float runY, Font font, float zoom)
    {
        if (line.BlockIndex != _engine.CursorBlock) return;

        bool cursorMatch = false;
        int localOff = 0;

        if (run.CellIndex >= 0 && line.Block?.Type == RichTextBlockType.Table &&
            line.Block.Rows != null && run.CellIndex == _engine.CursorCell)
        {
            int totalCols = line.Block.ColCount;
            int totalRows = line.Block.Rows.Count;
            int targetRow = run.CellIndex / totalCols;
            int targetCol = run.CellIndex % totalCols;
            if (targetRow < totalRows && targetCol < line.Block.Rows[targetRow].Cells.Count)
            {
                var targetCell = line.Block.Rows[targetRow].Cells[targetCol];
                int totalCellLen = targetCell.Content.Sum(c => c.Length);
                if (_engine.CursorOffset >= 0 && _engine.CursorOffset <= totalCellLen)
                {
                    cursorMatch = true;
                    localOff = _engine.CursorOffset;
                }
            }
        }
        else if (run.CellIndex < 0 && run.ContentIndex == _engine.CursorContent &&
            run.StartOffset <= _engine.CursorOffset &&
            _engine.CursorOffset <= run.StartOffset + run.Length)
        {
            cursorMatch = true;
            localOff = _engine.CursorOffset - run.StartOffset;
        }

        if (cursorMatch)
        {
            string beforeCursor = run.DisplayText[..Math.Min(localOff, run.DisplayText.Length)];
            float measuredWidth = Platform.Platform.MeasureText(beforeCursor, font, zoom).width;
            _cursorScreenX = (int)(runX + measuredWidth / zoom);
            _cursorScreenY = (int)runY;
            _cursorScreenValid = true;
        }
    }

    private void RenderTableGrid(Graphics g, LayoutRun run, VisualLine line, float runX, float runY)
    {
        if (run.CellIndex < 0 || line.Block?.Type != RichTextBlockType.Table || line.Block?.Rows == null) return;

        float cellLeft = runX - TextLayoutEngine.CellPadding;
        float cellTop = runY;
        float cellRight = cellLeft + run.Width;
        float cellBottom = runY + run.Height;
        int totalCols = line.Block!.ColCount;
        int totalRows = line.Block.Rows.Count;
        int colIdx = run.CellIndex % totalCols;
        int rowIdx = run.CellIndex / totalCols;

        g.DrawLine(Color.Gray, cellLeft, cellTop, cellLeft, cellBottom, 1);
        g.DrawLine(Color.Gray, cellLeft, cellTop, cellRight, cellTop, 1);
        if (colIdx == totalCols - 1)
            g.DrawLine(Color.Gray, cellRight, cellTop, cellRight, cellBottom, 1);
        if (rowIdx == totalRows - 1)
            g.DrawLine(Color.Gray, cellLeft, cellBottom, cellRight, cellBottom, 1);
    }

   private void PositionEndOfDocumentCursor(float padding, int cursorFlat)
    {
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
                    mouseArgs.X, mouseArgs.Y, zoom,
                    out var hitRun, 8);

                _engine.CursorBlock = pos.BlockIndex;
                _engine.CursorOffset = pos.CharOffset;
                _engine.SelectionBlock = _engine.CursorBlock;
                _engine.SelectionOffset = _engine.CursorOffset;

                // Table cell click: set CursorCell and CursorContent from hit run
                if (hitRun != null && hitRun.CellIndex >= 0)
                {
                    _engine.CursorCell = hitRun.CellIndex;
                    _engine.SelectionCell = hitRun.CellIndex;
                    _engine.CursorContent = 0;
                    _engine.SelectionContent = 0;
                }
                else
                {
                    _engine.CursorCell = -1;
                    _engine.SelectionCell = -1;
                    _engine.CursorContent = pos.ContentIndex;
                    _engine.SelectionContent = pos.ContentIndex;
                }

                // Double-click word selection
                if (mouseArgs.Clicks >= 2 && hitRun != null && hitRun.Source is TextRun tr)
                {
                    string text = hitRun.DisplayText;
                    int localOffset = pos.CharOffset - hitRun.StartOffset;
                    if (localOffset < 0) localOffset = 0;
                    if (localOffset > text.Length) localOffset = text.Length;

                    int start = localOffset;
                    while (start > 0 && char.IsLetterOrDigit(text[start - 1])) start--;
                    int end = localOffset;
                    while (end < text.Length && char.IsLetterOrDigit(text[end])) end++;

                    _engine.SelectionBlock = pos.BlockIndex;
                    _engine.SelectionContent = pos.ContentIndex;
                    _engine.SelectionOffset = hitRun.StartOffset + start;
                    _engine.CursorBlock = pos.BlockIndex;
                    _engine.CursorContent = pos.ContentIndex;
                    _engine.CursorOffset = hitRun.StartOffset + end;

                    if (hitRun.CellIndex >= 0)
                    {
                        _engine.SelectionCell = hitRun.CellIndex;
                        _engine.CursorCell = hitRun.CellIndex;
                    }
                }

                _preferredX = -1;

                _lastWasLink = hitRun != null && hitRun.ContentIndex >= 0 && hitRun.ContentIndex < _engine.Document.Blocks[pos.BlockIndex].Content.Count
                    && _engine.Document.Blocks[pos.BlockIndex].Content[hitRun.ContentIndex] is HyperlinkRun;
            }

            UpdateLinkCursor();

            CapturingMouse = true;
            Invalidate();
            
        }

        base.OnMouseDown(e);
        Focused = true;
    }

    /// <inheritdoc/>
    protected internal override void OnMouseMove(EventArgs e)
    {
        if (e is MouseEventArgs mouseArgs && _cachedLines != null)
        {
            float zoom = EffectiveZoom;
            EnsureLayout(Width, zoom);

            if (_cachedLines != null && _cachedLines.Count > 0)
            {
                var pos = TextLayoutEngine.HitTest(
                    _engine.Document, _cachedLines,
                    mouseArgs.X, mouseArgs.Y, zoom,
                    out var hitRun, 8);

                if (IsPressed)
                {
                    _engine.CursorBlock = pos.BlockIndex;
                    _engine.CursorOffset = pos.CharOffset;

                    // Table cell drag selection
                    if (hitRun != null && hitRun.CellIndex >= 0)
                    {
                        _engine.CursorCell = hitRun.CellIndex;
                        _engine.CursorContent = 0;
                    }
                    else
                    {
                        _engine.CursorCell = -1;
                        _engine.CursorContent = pos.ContentIndex;
                    }

                    Invalidate();
                }

                _lastWasLink = hitRun != null && hitRun.ContentIndex >= 0 && hitRun.ContentIndex < _engine.Document.Blocks[pos.BlockIndex].Content.Count
                    && _engine.Document.Blocks[pos.BlockIndex].Content[hitRun.ContentIndex] is HyperlinkRun;

                UpdateLinkCursor();
            }
        }

        base.OnMouseMove(e);
    }

     /// <inheritdoc/>
    protected internal override void OnMouseUp(EventArgs e)
    {
        CapturingMouse = false;

        if (e is MouseEventArgs mouseArgs && _cachedLines != null)
        {
            float zoom = EffectiveZoom;
            EnsureLayout(Width, zoom);

            if (_cachedLines != null && _cachedLines.Count > 0)
            {
                var pos = TextLayoutEngine.HitTest(
                    _engine.Document, _cachedLines,
                    mouseArgs.X, mouseArgs.Y, zoom,
                    out var hitRun, 8);

                if (hitRun != null && hitRun.ContentIndex >= 0 && hitRun.ContentIndex < _engine.Document.Blocks[pos.BlockIndex].Content.Count
                    && _engine.Document.Blocks[pos.BlockIndex].Content[hitRun.ContentIndex] is HyperlinkRun linkRun
                    && mouseArgs.Button == MouseButtons.Left)
                {
                    System.Console.WriteLine($"[HtmlBox] Link clicked: {linkRun.Url} (ContentIndex={hitRun.ContentIndex}, Button={mouseArgs.Button}, LinkBehavior={LinkBehavior}, _ctrlPressed={_ctrlPressed})");
                    bool ctrlOrOpen = _ctrlPressed || LinkBehavior == LinkBehavior.OpenInBrowser;
                    if (ctrlOrOpen)
                    {
                        System.Console.WriteLine($"[HtmlBox] Opening link: {linkRun.Url}");
                        OnLinkClick(linkRun.Url, linkRun);
                    }
                }
            }
        }

        Invalidate();
        

        base.OnMouseUp(e);
    }

    /// <inheritdoc/>
    protected internal override void OnKeyDown(KeyEventArgs e)
    {
        if (_readOnly) { base.OnKeyDown(e); return; }

        _ctrlPressed = e.Modifiers.HasFlag(ModifierKeys.Control);
        bool shift = e.Modifiers.HasFlag(ModifierKeys.Shift);
        bool ctrl = e.Modifiers.HasFlag(ModifierKeys.Control);

        bool handled = false;

        if (ctrl) handled = HandleCtrlShortcut(e.KeyCode);
        if (!handled) handled = HandleCharacterNavigation(e.KeyCode, shift);
        if (!handled) handled = HandleVisualNavigation(e.KeyCode, shift);
        if (!handled && e.KeyCode == Keys.Tab) handled = HandleTableTab(shift);

        if (!handled)
        {
            switch (e.KeyCode)
            {
                case Keys.Back: _engine.HandleBackspace(); handled = true; break;
                case Keys.Delete: _engine.HandleDelete(); handled = true; break;
                case Keys.Enter:
                    if (shift) _engine.InsertLineBreak();
                    else _engine.HandleEnter();
                    handled = true;
                    break;
            }
        }

        if (handled)
        {
            e.Handled = true;
            if (e.KeyCode is Keys.Back or Keys.Delete or Keys.Enter)
            {
                InvalidateLayout();
                ContentChanged?.Invoke(this, EventArgs.Empty);
            }
            Invalidate();
            
            base.OnKeyDown(e);
        }
        else
        {
            base.OnKeyDown(e);
        }
    }

    private bool HandleCtrlShortcut(Keys keyCode)
    {
        switch (keyCode)
        {
            case Keys.B: ApplyFormat("bold"); return true;
            case Keys.I: ApplyFormat("italic"); return true;
            case Keys.U: ApplyFormat("underline"); return true;
            case Keys.S: ApplyFormat("strikethrough"); return true;
            case Keys.Home: GoToDocumentBoundary(true); return true;
            case Keys.End: GoToDocumentBoundary(false); return true;
        }
        return false;
    }

    private bool HandleCharacterNavigation(Keys keyCode, bool shift)
    {
        if (keyCode != Keys.Left && keyCode != Keys.Right) return false;
        if (shift && !_engine.HasSelection) ResetSelection();
        if (keyCode == Keys.Left) _engine.MoveLeft(); else _engine.MoveRight();
        if (!shift) ResetSelection();
        return true;
    }

    private bool HandleVisualNavigation(Keys keyCode, bool shift)
    {
        if (keyCode is not (Keys.Up or Keys.Down or Keys.Home or Keys.End)) return false;
        if (shift && !_engine.HasSelection) ResetSelection();
        if (keyCode is Keys.Up) MoveVisualLine(-1);
        else if (keyCode is Keys.Down) MoveVisualLine(1);
        else MoveToVisualLineBoundary(keyCode == Keys.Home);
        if (!shift) ResetSelection();
        return true;
    }

    private bool HandleTableTab(bool shift)
    {
        var blk = _engine.CursorBlock >= 0 && _engine.CursorBlock < _engine.Document.Blocks.Count
            ? _engine.Document.Blocks[_engine.CursorBlock] : null;
        if (blk?.Type == RichTextBlockType.Table && blk.Rows != null && _engine.CursorCell >= 0)
        {
            int totalCells = blk.Rows.Count * blk.ColCount;
            if (shift)
            {
                _engine.CursorCell = _engine.CursorCell > 0 ? _engine.CursorCell - 1 : 0;
            }
            else
            {
                if (_engine.CursorCell + 1 >= totalCells)
                {
                    _engine.AddRow();
                    InvalidateLayout();
                }
                else
                {
                    _engine.CursorCell++;
                }
            }
            _engine.CursorContent = 0;
            _engine.CursorOffset = 0;
            _engine.SelectionBlock = _engine.CursorBlock;
            _engine.SelectionContent = 0;
            _engine.SelectionOffset = 0;
            _engine.SelectionCell = _engine.CursorCell;
            return true;
        }
        return false;
    }

    private void GoToDocumentBoundary(bool atStart)
    {
        if (atStart)
        {
            _engine.CursorBlock = 0;
            _engine.CursorContent = 0;
            _engine.CursorOffset = 0;
        }
        else
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
            }
        }
        _engine.SelectionBlock = _engine.CursorBlock;
        _engine.SelectionContent = _engine.CursorContent;
        _engine.SelectionOffset = _engine.CursorOffset;
        _engine.CursorCell = -1;
        _engine.SelectionCell = -1;
    }

    private void ResetSelection()
    {
        _engine.SelectionBlock = _engine.CursorBlock;
        _engine.SelectionContent = _engine.CursorContent;
        _engine.SelectionOffset = _engine.CursorOffset;
        _engine.SelectionCell = _engine.CursorCell;
    }

    /// <inheritdoc/>
    protected internal override void OnKeyUp(KeyEventArgs e)
    {
        if (_readOnly) { base.OnKeyUp(e); return; }
        _ctrlPressed = e.Modifiers.HasFlag(ModifierKeys.Control);
        base.OnKeyUp(e);
    }

    /// <summary>Handles text input from keyboard.</summary>
    protected internal override void OnTextInput(string text)
    {
        if (_readOnly || string.IsNullOrEmpty(text)) { base.OnTextInput(text); return; }
        _engine.InsertText(text);
        InvalidateLayout();
        Invalidate();
        ContentChanged?.Invoke(this, EventArgs.Empty);
        System.Console.WriteLine($"[HtmlBox] OnTextInput: text='{text}', htmlLen={_htmlBacking.Length}");
    }

    private void MoveVisualLineUp() => MoveVisualLine(-1);
    private void MoveVisualLineDown() => MoveVisualLine(1);

    private void MoveVisualLine(int direction)
    {
        EnsureLayout(Width, EffectiveZoom);
        if (_cachedLines == null || _cachedLines.Count == 0)
        {
            if (direction < 0) _engine.MoveUp(); else _engine.MoveDown();
            return;
        }

        // Table cell: move to same column in previous/next row
        var block = _engine.Document.Blocks.Count > 0 && _engine.CursorBlock >= 0 &&
            _engine.CursorBlock < _engine.Document.Blocks.Count
            ? _engine.Document.Blocks[_engine.CursorBlock] : null;
        if (block?.Type == RichTextBlockType.Table && block.Rows != null && _engine.CursorCell >= 0)
        {
            if (direction < 0)
            {
                if (_engine.CursorCell >= block.ColCount)
                {
                    _engine.CursorCell -= block.ColCount;
                    _engine.CursorContent = 0;
                    _engine.CursorOffset = 0;
                }
            }
            else
            {
                int nextRowStart = _engine.CursorCell + block.ColCount;
                int totalCells = block.Rows.Count * block.ColCount;
                if (nextRowStart < totalCells)
                {
                    _engine.CursorCell = nextRowStart;
                    _engine.CursorContent = 0;
                    _engine.CursorOffset = 0;
                }
            }
            Invalidate();
            return;
        }

        _preferredX = _cursorScreenX;
        int cursorFlat = _engine.CursorFlatIndex;

        int currentLineIndex = FindCurrentRunIndex();
        if (currentLineIndex < 0) { if (direction < 0) _engine.MoveUp(); else _engine.MoveDown(); return; }

        VisualLine? targetLine = FindTargetLine(currentLineIndex, direction);
        if (targetLine != null)
        {
            var pos = FindPositionAtXInLine(targetLine, _preferredX, EffectiveZoom);
            _engine.CursorBlock = pos.BlockIndex;
            _engine.CursorContent = pos.ContentIndex;
            _engine.CursorOffset = pos.CharOffset;

            // Set CursorCell when entering/within a table block
            if (targetLine.Block?.Type == RichTextBlockType.Table && targetLine.Block.Rows != null)
            {
                float padding = 8;
                float leftMargin = GetLeftMargin(targetLine.Block.Type);
                float contentX = _preferredX - padding - leftMargin;
                _engine.CursorCell = -1;
                foreach (var run in targetLine.Runs)
                {
                    if (contentX >= run.X && contentX <= run.X + run.Width)
                    {
                        _engine.CursorCell = run.CellIndex;
                        break;
                    }
                }
                if (_engine.CursorCell < 0 && targetLine.Runs.Count > 0)
                    _engine.CursorCell = targetLine.Runs[0].CellIndex;
            }
            else
            {
                _engine.CursorCell = -1;
            }
        }
        else
        {
            if (direction < 0) _engine.MoveUp(); else _engine.MoveDown();
        }
    }

    private int FindCurrentRunIndex()
    {
        int cursorFlat = _engine.CursorFlatIndex;
        for (int li = 0; li < _cachedLines!.Count; li++)
        {
            var line = _cachedLines[li];
            if (line.BlockIndex != _engine.CursorBlock) continue;

            bool found = false;
            foreach (var run in line.Runs)
            {
                int runStart = TextLayoutEngine.ToFlatIndex(_engine.Document,
                    line.BlockIndex, run.ContentIndex, run.StartOffset, run.CellIndex);
                int runEnd = runStart + run.Length;
                if (cursorFlat >= runStart && cursorFlat <= runEnd)
                {
                    found = true;
                    break;
                }
            }

            if (found || (line.BlockIndex == _engine.CursorBlock && line.Runs.Count == 0))
                return li;
        }
        return -1;
    }

    private VisualLine? FindTargetLine(int currentIndex, int direction)
    {
        if (direction < 0)
        {
            if (currentIndex > 0 && _cachedLines![currentIndex - 1].BlockIndex == _engine.CursorBlock)
                return _cachedLines[currentIndex - 1];
            for (int nl = currentIndex - 1; nl >= 0; nl--)
            {
                if (_cachedLines[nl].BlockIndex != _engine.CursorBlock)
                    return _cachedLines[nl];
            }
        }
        else
        {
            if (currentIndex + 1 < _cachedLines!.Count && _cachedLines[currentIndex + 1].BlockIndex == _engine.CursorBlock)
                return _cachedLines[currentIndex + 1];
            for (int nl = currentIndex + 1; nl < _cachedLines.Count; nl++)
            {
                if (_cachedLines[nl].BlockIndex != _engine.CursorBlock)
                    return _cachedLines[nl];
            }
        }
        return null;
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

                var measFont = new Font(tr.FontFamily, tr.FontSize, tr.Style);

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

    private void MoveToVisualLineStart() => MoveToVisualLineBoundary(true);
    private void MoveToVisualLineEnd() => MoveToVisualLineBoundary(false);

    private void MoveToVisualLineBoundary(bool atStart)
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
                    line.BlockIndex, run.ContentIndex, run.StartOffset, run.CellIndex);
                int runEnd = runStart + run.Length;
                if (cursorFlat >= runStart && cursorFlat <= runEnd)
                {
                    var targetRun = atStart ? line.Runs[0] : line.Runs[^1];
                    _engine.CursorBlock = line.BlockIndex;
                    _engine.CursorContent = targetRun.ContentIndex;
                    _engine.CursorOffset = atStart ? targetRun.StartOffset : targetRun.StartOffset + targetRun.Length;
                    _engine.CursorCell = targetRun.CellIndex >= 0 ? targetRun.CellIndex : -1;
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
        _engine.CursorContent = 0;
        _engine.CursorOffset = 0;
    }

    private static float GetLeftMargin(RichTextBlockType type) =>
        type is RichTextBlockType.BulletItem or RichTextBlockType.NumberItem ? 30 : 0;

    /// <summary>
    /// Updates the cursor to a hand cursor when Ctrl is held and the mouse is over a hyperlink.
    /// </summary>
    private void UpdateLinkCursor()
    {
        if (_lastWasLink)
        {
            FindForm()?.Cursor = SystemCursorType.Hand;
        }
        else
        {
            FindForm()?.Cursor = null;
        }
    }

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

    /// <summary>
    /// Raises the <see cref="LinkClick"/> event and optionally opens the URL in the default application.
    /// </summary>
    /// <param name="url">The URL of the clicked hyperlink.</param>
    /// <param name="link">The <see cref="HyperlinkRun"/> that was clicked, or null.</param>
    protected virtual void OnLinkClick(string url, HyperlinkRun? link)
    {
        var args = new HtmlLinkEventArgs(url, link);
        LinkClick?.Invoke(this, args);

        if (!args.Handled && LinkBehavior == LinkBehavior.OpenInBrowser)
        {
            OpenUrl(url);
        }
    }

    /// <summary>
    /// Opens the specified URL in the default application (e.g. web browser).
    /// Works cross-platform on Linux, Windows, and macOS.
    /// </summary>
    /// <param name="url">The URL to open.</param>
    private static void OpenUrl(string url)
    {
        System.Console.WriteLine($"[HtmlBox] OpenUrl called: {url}");
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
            System.Console.WriteLine($"[HtmlBox] OpenUrl success: {url}");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[HtmlBox] OpenUrl failed: {ex.Message}");
        }
    }
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

    /// <summary>Gets the <see cref="HyperlinkRun"/> that was clicked, or null.</summary>
    public HyperlinkRun? Link { get; }

    /// <summary>Gets or sets whether the link click has been handled.</summary>
    public bool Handled { get; set; }

    /// <summary>Initializes a new instance of <see cref="HtmlLinkEventArgs"/>.</summary>
    /// <param name="url">The URL of the clicked hyperlink.</param>
    /// <param name="link">The <see cref="HyperlinkRun"/> that was clicked, or null.</param>
    public HtmlLinkEventArgs(string url, HyperlinkRun? link)
    {
        Url = url;
        Link = link;
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
