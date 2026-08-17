using OldSchoolForms.Ui.Core;

namespace OldSchoolForms.Ui.Core;

/// <summary>
/// Extends TextEditorEngine with multi-line text editing support including word wrap,
/// vertical scrolling, and line-based cursor navigation.
/// </summary>
public class MultiLineTextEditorEngine : TextEditorEngine
{
    private int _scrollOffsetY;
    private bool _wordWrap = true;
    private string[] _logicalLines = Array.Empty<string>();

    /// <summary>
    /// Occurs when the vertical scroll offset changes.
    /// </summary>
    public event EventHandler? ScrollOffsetYChanged;

    /// <summary>
    /// Gets or sets whether text wraps at the available width.
    /// </summary>
    public bool WordWrap
    {
        get => _wordWrap;
        set
        {
            if (_wordWrap == value) return;
            _wordWrap = value;
            if (_wordWrap) _scrollOffset = 0;
            RebuildLines();
        }
    }

    /// <summary>
    /// Gets the vertical scroll offset in pixels.
    /// </summary>
    public int ScrollOffsetY => _scrollOffsetY;

    /// <summary>
    /// Resets both horizontal and vertical scroll offsets to zero.
    /// </summary>
    public void ResetScroll()
    {
        _scrollOffsetY = 0;
        _scrollOffset = 0;
    }

    /// <summary>
    /// Sets the vertical scroll offset, clamped to valid range.
    /// </summary>
    public void ScrollTo(int value, int viewHeight)
    {
        if (viewHeight <= 0) return;
        int maxScroll = int.MaxValue; // Clamping is done externally via ContentHeight
        int clamped = Math.Max(0, Math.Min(value, maxScroll));
        if (_scrollOffsetY != clamped)
        {
            _scrollOffsetY = clamped;
            ScrollOffsetYChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Gets the line height in pixels based on the current font.
    /// </summary>
    public int GetLineHeight(ITextEditorContext context)
    {
        var measured = Platform.Platform.MeasureText("Ag", context.Font, context.Zoom);
        return Math.Max(1, (int)(measured.height / context.Zoom) + 2);
    }

    /// <summary>
    /// Computes the total content height using the given context for accurate line height.
    /// </summary>
    public int ComputeContentHeight(int viewWidth, ITextEditorContext context)
    {
        int lineHeight = GetLineHeight(context);
        return GetVisualLineCount(viewWidth, context) * lineHeight;
    }

    /// <summary>
    /// Gets the number of visual lines (accounting for word wrap).
    /// </summary>
    public int GetVisualLineCount(int viewWidth, ITextEditorContext context)
    {
        if (string.IsNullOrEmpty(_text))
            return 1;

        if (!_wordWrap)
            return _logicalLines.Length > 0 ? _logicalLines.Length : 1;

        int total = 0;
        for (int i = 0; i < _logicalLines.Length; i++)
        {
            int visualCount = GetWrappedLineCount(_logicalLines[i], viewWidth, context);
            total += Math.Max(1, visualCount);
        }
        return Math.Max(1, total);
    }

    private int GetWrappedLineCount(string line, int viewWidth, ITextEditorContext context)
    {
        if (string.IsNullOrEmpty(line) || viewWidth <= 0) return 1;
        int count = 1;
        int accWidth = 0;
        for (int i = 0; i < line.Length; i++)
        {
            int cw = MeasureTextWidth(line[i].ToString(), context);
            if (accWidth + cw > viewWidth && accWidth > 0)
            {
                count++;
                accWidth = cw;
            }
            else
            {
                accWidth += cw;
            }
        }
        return count;
    }

    /// <summary>
    /// Gets the visual line text and its global start index for a given visual line index.
    /// </summary>
    public (string text, int startIndex) GetVisualLine(int visualLineIndex, int viewWidth, ITextEditorContext context)
    {
        if (string.IsNullOrEmpty(_text) || visualLineIndex < 0)
            return (string.Empty, 0);

        int globalOffset = 0;

        if (!_wordWrap)
        {
            if (visualLineIndex < _logicalLines.Length)
            {
                // Calculate global offset for this logical line
                for (int i = 0; i < visualLineIndex; i++)
                    globalOffset += _logicalLines[i].Length + 1;
                return (_logicalLines[visualLineIndex], globalOffset);
            }
            return (string.Empty, _text.Length);
        }

        int visualCounter = 0;
        globalOffset = 0;
        for (int li = 0; li < _logicalLines.Length; li++)
        {
            var line = _logicalLines[li];
            int wrappedLines = GetWrappedLineCount(line, viewWidth, context);
            for (int wi = 0; wi < wrappedLines; wi++)
            {
                if (visualCounter == visualLineIndex)
                {
                    int start = GetWrappedLineStart(line, wi, viewWidth, context);
                    int len = GetWrappedLineLength(line, wi, viewWidth, context);
                    var text = line.Substring(start, len);
                    return (text, globalOffset + start);
                }
                visualCounter++;
            }
            globalOffset += line.Length + 1;
        }

        return (string.Empty, _text.Length);
    }

    private int GetWrappedLineStart(string line, int wrapIndex, int viewWidth, ITextEditorContext context)
    {
        if (wrapIndex <= 0 || viewWidth <= 0) return 0;
        int accWidth = 0;
        int start = 0;
        int currentWrap = 0;
        for (int i = 0; i < line.Length; i++)
        {
            int cw = MeasureTextWidth(line[i].ToString(), context);
            if (accWidth + cw > viewWidth && accWidth > 0)
            {
                currentWrap++;
                if (currentWrap == wrapIndex)
                {
                    start = i;
                    break;
                }
                accWidth = cw;
            }
            else
            {
                accWidth += cw;
            }
        }
        return start;
    }

    private int GetWrappedLineLength(string line, int wrapIndex, int viewWidth, ITextEditorContext context)
    {
        int start = GetWrappedLineStart(line, wrapIndex, viewWidth, context);
        int remaining = line.Length - start;
        if (remaining <= 0) return 0;
        int accWidth = 0;
        int len = 0;
        for (int i = start; i < line.Length; i++)
        {
            int cw = MeasureTextWidth(line[i].ToString(), context);
            if (accWidth + cw > viewWidth && accWidth > 0)
                break;
            accWidth += cw;
            len++;
        }
        return len;
    }

    /// <summary>
    /// Determines the flat index into _text for a given visual line and column.
    /// </summary>
    public int GetPositionFromVisualLineCol(int visualLine, int col, int viewWidth, ITextEditorContext context)
    {
        var (_, startIndex) = GetVisualLine(visualLine, viewWidth, context);
        return Math.Min(_text.Length, startIndex + col);
    }

    private void RebuildLines()
    {
        _logicalLines = string.IsNullOrEmpty(_text) ? Array.Empty<string>() : _text.Split('\n');
    }

    /// <inheritdoc />
    public override string Text
    {
        get => base.Text;
        set
        {
            base.Text = value;
            RebuildLines();
        }
    }

    /// <inheritdoc />
    public override void HandleTextInput(string text, ITextEditorContext context)
    {
        // Convert \r to \n
        text = text.Replace("\r", "\n");
        base.HandleTextInput(text, context);
        RebuildLines();
        EnsureCursorVisible(context);
    }

    /// <inheritdoc />
    public override void EnsureCursorVisible(ITextEditorContext context)
    {
        // Horizontal: only needed when word wrap is off (long unwrapped lines)
        if (_wordWrap)
            _scrollOffset = 0;
        else
            base.EnsureCursorVisible(context);

        // Vertical: ensure cursor line is visible
        int lineHeight = GetLineHeight(context);
        int viewWidth = context.TextAreaWidth;
        int viewHeight = context.TextAreaHeight;

        if (viewHeight <= 0) return;

        int visualLine = GetVisualLineFromPosition(_cursorPosition, viewWidth, context);
        int lineY = visualLine * lineHeight;

        if (lineY < _scrollOffsetY)
            _scrollOffsetY = lineY;
        else if (lineY + lineHeight > _scrollOffsetY + viewHeight)
            _scrollOffsetY = lineY + lineHeight - viewHeight;

        int contentHeight = GetVisualLineCount(viewWidth, context) * lineHeight;
        int maxScroll = Math.Max(0, contentHeight - viewHeight);
        _scrollOffsetY = Math.Max(0, Math.Min(_scrollOffsetY, maxScroll));
        ScrollOffsetYChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public override void HandleMouseDown(int logicalX, int logicalY, ITextEditorContext context)
    {
        if (string.IsNullOrEmpty(_text))
        {
            _cursorPosition = 0;
            _selectionAnchor = 0;
            _selectionLength = 0;
            EnsureCursorVisible(context);
            context.Invalidate();
            return;
        }

        int lineHeight = GetLineHeight(context);
        int viewWidth = context.TextAreaWidth;

        int visualLine = logicalY / lineHeight;
        int visualLineCount = GetVisualLineCount(viewWidth, context);
        visualLine = Math.Max(0, Math.Min(visualLine, visualLineCount - 1));

        // Find column from x position
        var (lineText, lineStartIndex) = GetVisualLine(visualLine, viewWidth, context);
        int col = GetColumnFromX(lineText, logicalX + _scrollOffset, context);

        _cursorPosition = Math.Min(_text.Length, lineStartIndex + col);
        _selectionAnchor = _cursorPosition;
        _selectionLength = 0;
        EnsureCursorVisible(context);
        context.Invalidate();
    }

    private int GetColumnFromX(string lineText, int logicalX, ITextEditorContext context)
    {
        if (string.IsNullOrEmpty(lineText) || logicalX <= 0) return 0;
        int bestCol = 0;
        int bestDist = Math.Abs(logicalX);
        for (int i = 1; i <= lineText.Length; i++)
        {
            int w = MeasureTextWidth(lineText.Substring(0, i), context);
            int dist = Math.Abs(logicalX - w);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestCol = i;
            }
        }
        return bestCol;
    }

    /// <summary>
    /// Gets the visual line index that contains the given flat text position.
    /// </summary>
    public int GetVisualLineFromPosition(int position, int viewWidth, ITextEditorContext context)
    {
        if (position <= 0) return 0;
        if (string.IsNullOrEmpty(_text)) return 0;

        int globalOffset = 0;

        if (!_wordWrap)
        {
            for (int i = 0; i < _logicalLines.Length; i++)
            {
                int lineLen = _logicalLines[i].Length;
                if (position <= globalOffset + lineLen)
                    return i;
                globalOffset += lineLen + 1;
            }
            return _logicalLines.Length - 1;
        }

        int visualCounter = 0;
        globalOffset = 0;
        for (int li = 0; li < _logicalLines.Length; li++)
        {
            var line = _logicalLines[li];
            int wrappedLines = GetWrappedLineCount(line, viewWidth, context);
            for (int wi = 0; wi < wrappedLines; wi++)
            {
                int start = GetWrappedLineStart(line, wi, viewWidth, context);
                int len = GetWrappedLineLength(line, wi, viewWidth, context);
                if (position >= globalOffset + start && position <= globalOffset + start + len)
                    return visualCounter;
                visualCounter++;
            }
            globalOffset += line.Length + 1;
        }
        return Math.Max(0, GetVisualLineCount(viewWidth, context) - 1);
    }

    /// <summary>
    /// Gets the column offset within a visual line for a given flat position.
    /// </summary>
    public int GetColumnInVisualLine(int position, int viewWidth, ITextEditorContext context)
    {
        if (position <= 0 || string.IsNullOrEmpty(_text)) return 0;

        int globalOffset = 0;

        if (!_wordWrap)
        {
            for (int i = 0; i < _logicalLines.Length; i++)
            {
                int lineLen = _logicalLines[i].Length;
                if (position <= globalOffset + lineLen)
                    return position - globalOffset;
                globalOffset += lineLen + 1;
            }
            if (_logicalLines.Length > 0)
                return _logicalLines[^1].Length;
            return 0;
        }

        globalOffset = 0;
        for (int li = 0; li < _logicalLines.Length; li++)
        {
            var line = _logicalLines[li];
            int wrappedLines = GetWrappedLineCount(line, viewWidth, context);
            for (int wi = 0; wi < wrappedLines; wi++)
            {
                int start = GetWrappedLineStart(line, wi, viewWidth, context);
                int len = GetWrappedLineLength(line, wi, viewWidth, context);
                if (position >= globalOffset + start && position <= globalOffset + start + len)
                    return position - (globalOffset + start);
            }
            globalOffset += line.Length + 1;
        }
        return 0;
    }
    
    /// <inheritdoc />
    public override bool HandleKeyDown(KeyEventArgs e, ITextEditorContext context)
    {
        if (e.Modifiers.HasFlag(ModifierKeys.Control))
        {
            switch (e.KeyCode)
            {
                case Keys.A:
                case Keys.C:
                case Keys.X:
                case Keys.V:
                    return base.HandleKeyDown(e, context);
            }
        }

        bool shift = e.Modifiers.HasFlag(ModifierKeys.Shift);
        int viewWidth = context.TextAreaWidth;

        switch (e.KeyCode)
        {
            case Keys.Up:
                MoveCursorVertical(-1, shift, viewWidth, context);
                e.Handled = true;
                break;

            case Keys.Down:
                MoveCursorVertical(1, shift, viewWidth, context);
                e.Handled = true;
                break;

            case Keys.Left:
                if (_cursorPosition > 0)
                {
                    if (shift)
                    {
                        if (_selectionLength == 0) _selectionAnchor = _cursorPosition;
                        _cursorPosition--;
                        _selectionLength = Math.Abs(_cursorPosition - _selectionAnchor);
                    }
                    else
                    {
                        if (_selectionLength > 0)
                        {
                            _cursorPosition = Math.Min(_selectionAnchor, _cursorPosition);
                        }
                        else
                        {
                            // If at start of visual line, go to end of previous
                            int colInLine = GetColumnInVisualLine(_cursorPosition, viewWidth, context);
                            if (colInLine == 0)
                            {
                                int visLine = GetVisualLineFromPosition(_cursorPosition, viewWidth, context);
                                if (visLine > 0)
                                {
                                    var (prevText, prevStart) = GetVisualLine(visLine - 1, viewWidth, context);
                                    _cursorPosition = prevStart + prevText.Length;
                                }
                                else
                                    _cursorPosition--;
                            }
                            else
                                _cursorPosition--;
                        }
                        _selectionAnchor = _cursorPosition;
                        _selectionLength = 0;
                    }
                }
                e.Handled = true;
                break;

            case Keys.Right:
                if (_cursorPosition < _text.Length)
                {
                    if (shift)
                    {
                        if (_selectionLength == 0) _selectionAnchor = _cursorPosition;
                        _cursorPosition++;
                        _selectionLength = Math.Abs(_cursorPosition - _selectionAnchor);
                    }
                    else
                    {
                        if (_selectionLength > 0)
                        {
                            _cursorPosition = Math.Max(_selectionAnchor, _cursorPosition);
                        }
                        else
                        {
                            // If at end of visual line, go to start of next
                            int colInLine = GetColumnInVisualLine(_cursorPosition, viewWidth, context);
                            var (curLineText, curStart) = GetVisualLine(
                                GetVisualLineFromPosition(_cursorPosition, viewWidth, context),
                                viewWidth, context);
                            if (colInLine >= curLineText.Length)
                            {
                                int nextPos = curStart + curLineText.Length + 1; // skip \n or next line
                                if (nextPos <= _text.Length)
                                    _cursorPosition = nextPos;
                                else
                                    _cursorPosition++;
                            }
                            else
                                _cursorPosition++;
                        }
                        _selectionAnchor = _cursorPosition;
                        _selectionLength = 0;
                    }
                }
                e.Handled = true;
                break;

            case Keys.Home:
                if (e.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    // Ctrl+Home: go to document start (same as base)
                    return base.HandleKeyDown(e, context);
                }
                // Home: go to visual line start
                {
                    int visLine = GetVisualLineFromPosition(_cursorPosition, viewWidth, context);
                    var (_, lineStart) = GetVisualLine(visLine, viewWidth, context);
                    if (shift)
                    {
                        if (_selectionLength == 0) _selectionAnchor = _cursorPosition;
                        _cursorPosition = lineStart;
                        _selectionLength = Math.Abs(_cursorPosition - _selectionAnchor);
                    }
                    else
                    {
                        _cursorPosition = lineStart;
                        _selectionAnchor = _cursorPosition;
                        _selectionLength = 0;
                    }
                    e.Handled = true;
                }
                break;

            case Keys.End:
                if (e.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    // Ctrl+End: go to document end (same as base)
                    return base.HandleKeyDown(e, context);
                }
                // End: go to visual line end
                {
                    int visLine = GetVisualLineFromPosition(_cursorPosition, viewWidth, context);
                    var (lineText, lineStart) = GetVisualLine(visLine, viewWidth, context);
                    if (shift)
                    {
                        if (_selectionLength == 0) _selectionAnchor = _cursorPosition;
                        _cursorPosition = lineStart + lineText.Length;
                        _selectionLength = Math.Abs(_cursorPosition - _selectionAnchor);
                    }
                    else
                    {
                        _cursorPosition = lineStart + lineText.Length;
                        _selectionAnchor = _cursorPosition;
                        _selectionLength = 0;
                    }
                    e.Handled = true;
                }
                break;

            case Keys.PageUp:
                {
                    int lineHeight = GetLineHeight(context);
                    int pageLines = Math.Max(1, context.TextAreaHeight / lineHeight);
                    MoveCursorVertical(-pageLines, shift, viewWidth, context);
                    e.Handled = true;
                }
                break;

            case Keys.PageDown:
                {
                    int lineHeight = GetLineHeight(context);
                    int pageLines = Math.Max(1, context.TextAreaHeight / lineHeight);
                    MoveCursorVertical(pageLines, shift, viewWidth, context);
                    e.Handled = true;
                }
                break;

            case Keys.Back:
                if (_selectionLength > 0)
                {
                    DeleteSelection(context);
                    RebuildLines();
                    FireTextChanged();
                }
                else if (_cursorPosition > 0)
                {
                    _text = _text.Remove(_cursorPosition - 1, 1);
                    _cursorPosition--;
                    _selectionAnchor = _cursorPosition;
                    _selectionLength = 0;
                    RebuildLines();
                    FireTextChanged();
                }
                e.Handled = true;
                break;

            case Keys.Delete:
                if (_selectionLength > 0)
                {
                    DeleteSelection(context);
                    RebuildLines();
                    FireTextChanged();
                }
                else if (_cursorPosition < _text.Length)
                {
                    _text = _text.Remove(_cursorPosition, 1);
                    RebuildLines();
                    FireTextChanged();
                }
                e.Handled = true;
                break;

            case Keys.Enter:
                if (_selectionLength > 0)
                    DeleteSelection(context);
                _text = _text.Insert(_cursorPosition, "\n");
                _cursorPosition++;
                _selectionAnchor = _cursorPosition;
                _selectionLength = 0;
                RebuildLines();
                FireTextChanged();
                e.Handled = true;
                break;

            default:
                return false;
        }

        EnsureCursorVisible(context);
        context.Invalidate();
        return e.Handled;
    }

    private void MoveCursorVertical(int direction, bool shift, int viewWidth, ITextEditorContext context)
    {
        int curVisualLine = GetVisualLineFromPosition(_cursorPosition, viewWidth, context);
        int curCol = GetColumnInVisualLine(_cursorPosition, viewWidth, context);
        int newVisualLine = Math.Max(0, curVisualLine + direction);
        int totalLines = GetVisualLineCount(viewWidth, context);
        newVisualLine = Math.Min(newVisualLine, totalLines - 1);

        if (newVisualLine == curVisualLine) return;

        var (targetLineText, targetLineStart) = GetVisualLine(newVisualLine, viewWidth, context);
        int newCol = Math.Min(curCol, targetLineText.Length);
        int newPosition = targetLineStart + newCol;

        if (shift)
        {
            if (_selectionLength == 0) _selectionAnchor = _cursorPosition;
            _cursorPosition = newPosition;
            _selectionLength = Math.Abs(_cursorPosition - _selectionAnchor);
        }
        else
        {
            _cursorPosition = newPosition;
            _selectionAnchor = _cursorPosition;
            _selectionLength = 0;
        }
    }

    /// <inheritdoc />
    public override void Cut(ITextEditorContext context)
    {
        base.Cut(context);
        RebuildLines();
    }

    /// <inheritdoc />
    public override void Paste(ITextEditorContext context)
    {
        base.Paste(context);
        RebuildLines();
    }
}
