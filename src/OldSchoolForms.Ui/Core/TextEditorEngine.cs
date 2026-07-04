namespace OldSchoolForms.Ui.Core;

/// <summary>
/// Provides reusable text editing logic including cursor movement, selection, clipboard operations,
/// and horizontal scrolling. Designed to be shared by any control requiring text input.
/// Requires an ITextEditorContext for font/zoom/invalidation access.
/// </summary>
public class TextEditorEngine
{
    private bool _useSystemPasswordChar;
    /// <summary>
    /// The full text content.
    /// </summary>
    protected string _text = string.Empty;
    /// <summary>
    /// The current cursor position as an index into <see cref="_text"/>.
    /// </summary>
    protected int _cursorPosition;
    /// <summary>
    /// The anchor position for selection (other end of the selection range).
    /// </summary>
    protected int _selectionAnchor;
    /// <summary>
    /// The length of the current selection in characters.
    /// </summary>
    protected int _selectionLength;
    /// <summary>
    /// The horizontal scroll offset in pixels.
    /// </summary>
    protected int _scrollOffset;

    /// <summary>
    /// The bullet character used in password mode.
    /// </summary>
    public const string BulletChar = "\u25CF";

    /// <summary>
    /// Interval in milliseconds for cursor blinking.
    /// </summary>
    public const int CursorBlinkInterval = 530;

    /// <summary>
    /// Occurs when the text content changes.
    /// </summary>
    public event EventHandler? TextChanged;

    /// <summary>
    /// Raises the <see cref="TextChanged"/> event. Can be called from derived classes.
    /// </summary>
    protected void FireTextChanged() => TextChanged?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Gets or sets the text content. When set, the cursor is moved to the end and selection is cleared.
    /// The caller should call EnsureCursorVisible and fire TextChanged after setting.
    /// </summary>
    public virtual string Text
    {
        get => _text;
        set
        {
            if (_text == value) return;
            _text = value ?? string.Empty;
            _cursorPosition = _text.Length;
            _selectionAnchor = _cursorPosition;
            _selectionLength = 0;
        }
    }

    /// <summary>
    /// Gets the display text, replacing characters with bullets in password mode.
    /// </summary>
    public string DisplayText => _useSystemPasswordChar ? new string('\u25CF', _text.Length) : _text;

    /// <summary>
    /// Gets whether the UseSystemPasswordChar option is active.
    /// </summary>
    protected bool UseSystemPasswordCharActive => _useSystemPasswordChar;

    /// <summary>
    /// Gets or sets whether password mode is active.
    /// </summary>
    public bool UseSystemPasswordChar
    {
        get => _useSystemPasswordChar;
        set
        {
            if (_useSystemPasswordChar != value)
                _useSystemPasswordChar = value;
        }
    }

    /// <summary>
    /// Gets or sets the starting position of the selection.
    /// </summary>
    public int SelectionStart
    {
        get => _selectionLength > 0 ? Math.Min(_selectionAnchor, _cursorPosition) : _cursorPosition;
        set => _selectionAnchor = value;
    }

    /// <summary>
    /// Gets or sets the number of selected characters.
    /// </summary>
    public int SelectionLength
    {
        get => _selectionLength;
        set => _selectionLength = value;
    }

    /// <summary>
    /// Gets the currently selected text.
    /// </summary>
    public string SelectedText
    {
        get
        {
            if (_selectionLength <= 0) return string.Empty;
            int start = Math.Min(_selectionAnchor, _cursorPosition);
            int len = Math.Abs(_selectionLength);
            if (start + len > _text.Length) len = _text.Length - start;
            return _text.Substring(start, len);
        }
    }

    /// <summary>
    /// Gets the current cursor position.
    /// </summary>
    public int CursorPosition => _cursorPosition;

    /// <summary>
    /// Gets the current horizontal scroll offset in pixels.
    /// </summary>
    public int ScrollOffset => _scrollOffset;

    /// <summary>
    /// Gets whether any text is currently selected.
    /// </summary>
    public bool HasSelection => _selectionLength > 0;

    /// <summary>
    /// Gets the start index of the selection (the lower of anchor and cursor).
    /// </summary>
    public int SelectionStartIndex => Math.Min(_selectionAnchor, _cursorPosition);

    /// <summary>
    /// Gets the end index of the selection (the higher of anchor and cursor).
    /// </summary>
    public int SelectionEndIndex => Math.Max(_selectionAnchor, _cursorPosition);

    /// <summary>
    /// Gets whether the cursor should be drawn as visible based on the blink timer.
    /// </summary>
    public static bool IsCursorBlinkVisible =>
        (Environment.TickCount % (CursorBlinkInterval * 2)) < CursorBlinkInterval;

    /// <summary>
    /// Measures the width of the specified text using the given context's font and zoom.
    /// </summary>
    public int MeasureTextWidth(string text, ITextEditorContext context)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        var measured = Platform.Platform.MeasureText(text, context.Font, context.Zoom);
        return (int)(measured.width / context.Zoom);
    }

    /// <summary>
    /// Measures the total display width of the current text.
    /// </summary>
    public virtual int MeasureDisplayWidth(ITextEditorContext context)
    {
        if (_text.Length == 0) return 0;
        if (_useSystemPasswordChar)
        {
            var measured = Platform.Platform.MeasureText(BulletChar, context.Font, context.Zoom);
            int bulletWidth = (int)(measured.width / context.Zoom);
            return bulletWidth * _text.Length;
        }
        return MeasureTextWidth(_text, context);
    }

    /// <summary>
    /// Adjusts the scroll offset so the cursor is visible within the text area.
    /// </summary>
    public virtual void EnsureCursorVisible(ITextEditorContext context)
    {
        if (string.IsNullOrEmpty(_text))
        {
            _scrollOffset = 0;
            return;
        }

        int textAreaWidth = context.TextAreaWidth;
        if (textAreaWidth <= 0)
        {
            _scrollOffset = 0;
            return;
        }

        string displayText = DisplayText;
        int cursorLogicalX;

        if (_useSystemPasswordChar)
        {
            var measured = Platform.Platform.MeasureText(BulletChar, context.Font, context.Zoom);
            int bulletWidth = (int)(measured.width / context.Zoom);
            cursorLogicalX = _cursorPosition * bulletWidth;
        }
        else
        {
            string textBeforeCursor = displayText.Substring(0, _cursorPosition);
            cursorLogicalX = MeasureTextWidth(textBeforeCursor, context);
        }

        int cursorVisualX = 4 + cursorLogicalX - _scrollOffset;

        if (cursorVisualX < 4)
            _scrollOffset = cursorLogicalX;
        else if (cursorVisualX > 4 + textAreaWidth)
            _scrollOffset = cursorLogicalX - textAreaWidth;

        int totalTextWidth = MeasureDisplayWidth(context);
        int maxScroll = Math.Max(0, totalTextWidth - textAreaWidth);
        _scrollOffset = Math.Max(0, Math.Min(_scrollOffset, maxScroll));
    }

    /// <summary>
    /// Handles mouse down to set the cursor position based on a logical x-coordinate
    /// (pixels from the text origin, including scroll offset). The y-coordinate is ignored
    /// in single-line mode; override for multi-line support.
    /// </summary>
    /// <param name="logicalX">The x-coordinate relative to the text start, adjusted for scroll offset.</param>
    /// <param name="context">The editor context.</param>
    public virtual void HandleMouseDown(int logicalX, ITextEditorContext context)
    {
        if (_useSystemPasswordChar && _text.Length > 0)
        {
            var measured = Platform.Platform.MeasureText(BulletChar, context.Font, context.Zoom);
            int bulletWidth = (int)(measured.width / context.Zoom);
            _cursorPosition = Math.Min(logicalX / bulletWidth + 1, _text.Length);
        }
        else
        {
            int bestPos = 0;
            int bestDist = Math.Abs(logicalX);

            for (int i = 1; i <= _text.Length; i++)
            {
                int w = MeasureTextWidth(_text.Substring(0, i), context);
                int dist = Math.Abs(logicalX - w);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestPos = i;
                }
            }

            _cursorPosition = bestPos;
        }

        _selectionAnchor = _cursorPosition;
        _selectionLength = 0;
        EnsureCursorVisible(context);
        context.Invalidate();
    }

    /// <summary>
    /// Handles mouse down with both x and y logical coordinates. By default ignores y
    /// and delegates to <see cref="HandleMouseDown(int, ITextEditorContext)"/>.
    /// Override this method for multi-line text editors that need line-based positioning.
    /// </summary>
    /// <param name="logicalX">The x-coordinate relative to the text start, adjusted for scroll offset.</param>
    /// <param name="logicalY">The y-coordinate relative to the text start, adjusted for scroll offset.</param>
    /// <param name="context">The editor context.</param>
    public virtual void HandleMouseDown(int logicalX, int logicalY, ITextEditorContext context)
    {
        HandleMouseDown(logicalX, context);
    }

    /// <summary>
    /// Handles a key down event. Returns true if the key was handled.
    /// </summary>
    public virtual bool HandleKeyDown(KeyEventArgs e, ITextEditorContext context)
    {
        if (e.Modifiers.HasFlag(ModifierKeys.Control))
        {
            switch (e.KeyCode)
            {
                case Keys.A:
                    _selectionAnchor = 0;
                    _cursorPosition = _text.Length;
                    _selectionLength = _cursorPosition - _selectionAnchor;
                    e.Handled = true;
                    break;
                case Keys.C:
                    CopyToClipboard();
                    e.Handled = true;
                    break;
                case Keys.X:
                    Cut(context);
                    e.Handled = true;
                    break;
                case Keys.V:
                    Paste(context);
                    e.Handled = true;
                    break;
            }
            EnsureCursorVisible(context);
            return e.Handled;
        }

        bool shift = e.Modifiers.HasFlag(ModifierKeys.Shift);

        switch (e.KeyCode)
        {
            case Keys.Back:
                if (_selectionLength > 0)
                {
                    DeleteSelection(context);
                    TextChanged?.Invoke(this, EventArgs.Empty);
                }
                else if (_cursorPosition > 0)
                {
                    _text = _text.Remove(_cursorPosition - 1, 1);
                    _cursorPosition--;
                    _selectionAnchor = _cursorPosition;
                    _selectionLength = 0;
                    TextChanged?.Invoke(this, EventArgs.Empty);
                }
                e.Handled = true;
                break;

            case Keys.Delete:
                if (_selectionLength > 0)
                {
                    DeleteSelection(context);
                    TextChanged?.Invoke(this, EventArgs.Empty);
                }
                else if (_cursorPosition < _text.Length)
                {
                    _text = _text.Remove(_cursorPosition, 1);
                    TextChanged?.Invoke(this, EventArgs.Empty);
                }
                e.Handled = true;
                break;

            case Keys.Left:
                if (_selectionLength > 0 && !shift)
                {
                    _cursorPosition = Math.Min(_selectionAnchor, _cursorPosition);
                    _selectionLength = 0;
                    _selectionAnchor = _cursorPosition;
                }
                else if (_cursorPosition > 0)
                {
                    if (shift)
                    {
                        if (_selectionLength == 0)
                            _selectionAnchor = _cursorPosition;
                        _cursorPosition--;
                        _selectionLength = Math.Abs(_cursorPosition - _selectionAnchor);
                    }
                    else
                    {
                        _cursorPosition--;
                        _selectionAnchor = _cursorPosition;
                        _selectionLength = 0;
                    }
                }
                e.Handled = true;
                break;

            case Keys.Right:
                if (_selectionLength > 0 && !shift)
                {
                    _cursorPosition = Math.Max(_selectionAnchor, _cursorPosition);
                    _selectionLength = 0;
                    _selectionAnchor = _cursorPosition;
                }
                else if (_cursorPosition < _text.Length)
                {
                    if (shift)
                    {
                        if (_selectionLength == 0)
                            _selectionAnchor = _cursorPosition;
                        _cursorPosition++;
                        _selectionLength = Math.Abs(_cursorPosition - _selectionAnchor);
                    }
                    else
                    {
                        _cursorPosition++;
                        _selectionAnchor = _cursorPosition;
                        _selectionLength = 0;
                    }
                }
                e.Handled = true;
                break;

            case Keys.Home:
                if (shift)
                {
                    if (_selectionLength == 0) _selectionAnchor = _cursorPosition;
                    _cursorPosition = 0;
                    _selectionLength = Math.Abs(_cursorPosition - _selectionAnchor);
                }
                else
                {
                    _cursorPosition = 0;
                    _selectionAnchor = 0;
                    _selectionLength = 0;
                }
                e.Handled = true;
                break;

            case Keys.End:
                if (shift)
                {
                    if (_selectionLength == 0) _selectionAnchor = _cursorPosition;
                    _cursorPosition = _text.Length;
                    _selectionLength = Math.Abs(_cursorPosition - _selectionAnchor);
                }
                else
                {
                    _cursorPosition = _text.Length;
                    _selectionAnchor = _cursorPosition;
                    _selectionLength = 0;
                }
                e.Handled = true;
                break;

            case Keys.Escape:
                return false;
        }

        EnsureCursorVisible(context);
        context.Invalidate();
        return e.Handled;
    }

    /// <summary>
    /// Handles text input from the user, inserting at the cursor position.
    /// </summary>
    public virtual void HandleTextInput(string text, ITextEditorContext context)
    {
        if (string.IsNullOrEmpty(text)) return;

        if (_selectionLength > 0)
            DeleteSelection(context);

        _text = _text.Insert(_cursorPosition, text);
        _cursorPosition += text.Length;
        _selectionAnchor = _cursorPosition;
        _selectionLength = 0;
        EnsureCursorVisible(context);
        TextChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Copies the selected text (or all text if no selection) to the clipboard.
    /// </summary>
    public virtual void CopyToClipboard()
    {
        string text = _selectionLength > 0 ? SelectedText : _text;
        if (!string.IsNullOrEmpty(text))
        {
            Core.Clipboard.SetText(text);
        }
    }

    /// <summary>
    /// Cuts the selected text to the clipboard.
    /// </summary>
    public virtual void Cut(ITextEditorContext context)
    {
        if (_selectionLength > 0)
        {
            var selected = SelectedText;
            DeleteSelection(context);
            Core.Clipboard.SetText(selected);
            TextChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Pastes text from the clipboard at the cursor position.
    /// </summary>
    public virtual void Paste(ITextEditorContext context)
    {
        var text = Core.Clipboard.GetText();
        if (string.IsNullOrEmpty(text)) return;

        if (_selectionLength > 0)
            DeleteSelection(context);

        _text = _text.Insert(_cursorPosition, text);
        _cursorPosition += text.Length;
        _selectionAnchor = _cursorPosition;
        _selectionLength = 0;
        EnsureCursorVisible(context);
        TextChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Sets the cursor position and selection range. The selection starts at <paramref name="start"/>
    /// and extends <paramref name="length"/> characters to the right. The cursor is placed at <paramref name="start"/>.
    /// </summary>
    /// <param name="start">The start position of the selection (and cursor position).</param>
    /// <param name="length">The length of the selection. Must be non-negative.</param>
    public void SetSelectionRange(int start, int length)
    {
        int clampedStart = Math.Max(0, Math.Min(start, _text.Length));
        int clampedEnd = Math.Min(_text.Length, start + Math.Max(0, length));
        _cursorPosition = clampedStart;
        _selectionAnchor = clampedEnd;
        _selectionLength = Math.Abs(clampedEnd - clampedStart);
    }

    /// <summary>
    /// Selects all text.
    /// </summary>
    public virtual void SelectAll(ITextEditorContext context)
    {
        _selectionAnchor = 0;
        _cursorPosition = _text.Length;
        _selectionLength = _cursorPosition - _selectionAnchor;
        EnsureCursorVisible(context);
    }

    /// <summary>
    /// Deletes the currently selected text and moves the cursor to the selection start.
    /// </summary>
    /// <param name="context">The editor context for invalidation and scrolling.</param>
    protected virtual void DeleteSelection(ITextEditorContext context)
    {
        int start = Math.Min(_selectionAnchor, _cursorPosition);
        int end = Math.Max(_selectionAnchor, _cursorPosition);
        if (end > _text.Length) end = _text.Length;
        _text = _text.Remove(start, end - start);
        _cursorPosition = start;
        _selectionAnchor = start;
        _selectionLength = 0;
        EnsureCursorVisible(context);
    }
}
