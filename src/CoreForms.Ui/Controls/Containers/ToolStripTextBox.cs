using CoreForms.Ui.Core;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Containers;

/// <summary>
/// Represents an editable text box in a ToolStrip.
/// Supports full text editing including cursor, selection, copy/cut/paste, and password mode.
/// </summary>
public class ToolStripTextBox : ToolStripItem
{
    private string _text = string.Empty;
    private int _cursorPosition;
    private int _selectionAnchor;
    private int _selectionLength;
    private bool _useSystemPasswordChar;
    private bool _focused;
    private int _width = 100;

    private static readonly int CursorBlinkInterval = 530;
    private static readonly string BulletChar = "\u25CF";

    /// <summary>
    /// Initializes a new instance of ToolStripTextBox.
    /// </summary>
    public ToolStripTextBox()
    {
        DisplayStyle = ToolStripItemDisplayStyle.Text;
    }

    /// <summary>
    /// Initializes a new instance of ToolStripTextBox with the specified text.
    /// </summary>
    /// <param name="text">The initial text.</param>
    public ToolStripTextBox(string text)
    {
        _text = text;
        _cursorPosition = _text.Length;
        _selectionAnchor = _cursorPosition;
        DisplayStyle = ToolStripItemDisplayStyle.Text;
    }

    /// <summary>
    /// Gets or sets the text in the text box.
    /// </summary>
    public new string Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value;
                _cursorPosition = _text.Length;
                _selectionAnchor = _cursorPosition;
                _selectionLength = 0;
                RaiseTextChanged();
                Owner?.Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets the starting position of the selected text.
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
    /// Gets the selected text.
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
    /// Gets or sets whether the text box uses password mode.
    /// </summary>
    public bool UseSystemPasswordChar
    {
        get => _useSystemPasswordChar;
        set
        {
            if (_useSystemPasswordChar != value)
            {
                _useSystemPasswordChar = value;
                Owner?.Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the text box has focus.
    /// </summary>
    public bool Focused
    {
        get => _focused;
        set
        {
            if (_focused != value)
            {
                _focused = value;
                if (!_focused)
                {
                    _selectionLength = 0;
                }
                Owner?.Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets the width of the text box in pixels.
    /// </summary>
    public int TextBoxWidth
    {
        get => _width;
        set
        {
            if (_width != value)
            {
                _width = value;
                Owner?.Invalidate();
            }
        }
    }

    /// <summary>
    /// Occurs when the text changes.
    /// </summary>
    public new event EventHandler? TextChanged;

    private void RaiseTextChanged()
    {
        TextChanged?.Invoke(this, EventArgs.Empty);
    }

    private string GetDisplayText()
    {
        return _useSystemPasswordChar ? new string('\u25CF', _text.Length) : _text;
    }

    private int MeasureLocalTextWidth(string text, Font font, float zoom)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        var measured = Platform.Platform.MeasureText(text, font, zoom);
        return (int)(measured.width / zoom);
    }

    /// <summary>
    /// Renders the text box with its text, selection, and cursor.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
    /// <param name="x">The x-coordinate of the item bounds.</param>
    /// <param name="y">The y-coordinate of the item bounds.</param>
    /// <param name="width">The width of the item bounds.</param>
    /// <param name="height">The height of the item bounds.</param>
    /// <param name="font">The font to use for text rendering.</param>
    /// <param name="zoom">The current zoom factor.</param>
    /// <param name="hovered">Whether the item is currently hovered.</param>
    /// <param name="pressed">Whether the item is currently pressed.</param>
    public override void OnPaint(Graphics g, int x, int y, int width, int height, Font font, float zoom, bool hovered, bool pressed)
    {
        if (!Visible) return;

        int tbWidth = _width;
        int tbX = x + 2;
        int tbY = y + 2;
        int tbHeight = height - 4;

        g.FillRectangle(Color.White, tbX, tbY, tbWidth, tbHeight);

        if (_focused)
            g.DrawRectangle(Color.FromArgb(0, 120, 215), tbX, tbY, tbWidth, tbHeight, 2);
        else
            g.DrawRectangle(Color.FromArgb(128, 128, 128), tbX, tbY, tbWidth, tbHeight, 1);

        float textY = tbY + (tbHeight - font.Size * zoom) / 2f;
        float textX = tbX + 4;

        string displayText = GetDisplayText();

        if (_selectionLength > 0 && _focused)
        {
            int selStart = Math.Min(_selectionAnchor, _cursorPosition);
            int selEnd = Math.Max(_selectionAnchor, _cursorPosition);

            string beforeSel = displayText.Substring(0, selStart);
            string selStr = displayText.Substring(selStart, selEnd - selStart);
            float selX = textX + MeasureLocalTextWidth(beforeSel, font, zoom);
            float selWidth = Math.Max(MeasureLocalTextWidth(selStr, font, zoom), 2);

            g.DrawString(displayText, font, Color.Black, textX, textY);
            g.FillRectangle(SystemColors.Highlight, selX, textY, selWidth, font.Size * zoom + 2);
            g.DrawString(selStr, font, SystemColors.HighlightText, selX, textY);
        }
        else
        {
            g.DrawString(displayText, font, Color.Black, textX, textY);
        }

        if (_focused)
        {
            bool cursorVisible = (Environment.TickCount % (CursorBlinkInterval * 2)) < CursorBlinkInterval;
            if (cursorVisible)
            {
                string textBeforeCursor = displayText.Substring(0, _cursorPosition);
                float cursorX = textX + MeasureLocalTextWidth(textBeforeCursor, font, zoom);
                g.DrawLine(Color.Black, cursorX, textY, cursorX, textY + font.Size * zoom, 1);
            }
        }
    }

    /// <summary>
    /// Calculates the preferred width for layout.
    /// </summary>
    /// <param name="font">The font (unused for text box width).</param>
    /// <param name="zoom">The current zoom factor.</param>
    /// <returns>The configured text box width.</returns>
    public override int GetPreferredWidth(Font font, float zoom)
    {
        return _width + 4;
    }

    /// <summary>
    /// Handles mouse down to set focus and cursor position.
    /// </summary>
    internal void HandleMouseDown(int x, int y, int itemX, int itemY)
    {
        Focused = true;

        int xPos = x - itemX - 6;
        var font = Owner?.Font ?? Font.Default;
        float zoom = Owner?.EffectiveZoom ?? 1.0f;

        if (_useSystemPasswordChar && _text.Length > 0)
        {
            var measured = Platform.Platform.MeasureText(BulletChar, font, zoom);
            int bulletWidth = (int)(measured.width / zoom);
            _cursorPosition = Math.Min(xPos / bulletWidth + 1, _text.Length);
        }
        else
        {
            int bestPos = 0;
            int bestDist = Math.Abs(xPos);

            for (int i = 1; i <= _text.Length; i++)
            {
                int w = MeasureLocalTextWidth(_text.Substring(0, i), font, zoom);
                int dist = Math.Abs(xPos - w);
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
        Owner?.Invalidate();
    }

    /// <summary>
    /// Handles text input.
    /// </summary>
    internal void HandleTextInput(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        if (_selectionLength > 0)
            DeleteSelection();

        _text = _text.Insert(_cursorPosition, text);
        _cursorPosition += text.Length;
        _selectionAnchor = _cursorPosition;
        _selectionLength = 0;
        RaiseTextChanged();
        Owner?.Invalidate();
    }

    /// <summary>
    /// Handles key down events for editing operations.
    /// </summary>
    internal bool HandleKeyDown(KeyEventArgs e)
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
                    Cut();
                    e.Handled = true;
                    break;
                case Keys.V:
                    Paste();
                    e.Handled = true;
                    break;
            }
            Owner?.Invalidate();
            return e.Handled;
        }

        bool shift = e.Modifiers.HasFlag(ModifierKeys.Shift);

        switch (e.KeyCode)
        {
            case Keys.Back:
                if (_selectionLength > 0)
                    DeleteSelection();
                else if (_cursorPosition > 0)
                {
                    _text = _text.Remove(_cursorPosition - 1, 1);
                    _cursorPosition--;
                    _selectionAnchor = _cursorPosition;
                    _selectionLength = 0;
                }
                RaiseTextChanged();
                e.Handled = true;
                break;

            case Keys.Delete:
                if (_selectionLength > 0)
                    DeleteSelection();
                else if (_cursorPosition < _text.Length)
                    _text = _text.Remove(_cursorPosition, 1);
                RaiseTextChanged();
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
                        if (_selectionLength == 0) _selectionAnchor = _cursorPosition;
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
                        if (_selectionLength == 0) _selectionAnchor = _cursorPosition;
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
                Focused = false;
                e.Handled = true;
                break;
        }

        Owner?.Invalidate();
        return e.Handled;
    }

    private void DeleteSelection()
    {
        int start = Math.Min(_selectionAnchor, _cursorPosition);
        int end = Math.Max(_selectionAnchor, _cursorPosition);
        if (end > _text.Length) end = _text.Length;
        _text = _text.Remove(start, end - start);
        _cursorPosition = start;
        _selectionAnchor = start;
        _selectionLength = 0;
    }

    /// <summary>
    /// Copies the selected text or all text to the clipboard.
    /// </summary>
    public void CopyToClipboard()
    {
        string text = _selectionLength > 0 ? SelectedText : _text;
        if (!string.IsNullOrEmpty(text))
        {
            Clipboard.SetText(text);
        }
    }

    /// <summary>
    /// Cuts the selected text and copies it to the clipboard.
    /// </summary>
    public void Cut()
    {
        if (_selectionLength > 0)
        {
            var selected = SelectedText;
            DeleteSelection();
            Clipboard.SetText(selected);
            RaiseTextChanged();
            Owner?.Invalidate();
        }
    }

    /// <summary>
    /// Pastes the clipboard text at the current cursor position.
    /// </summary>
    public void Paste()
    {
        var text = Clipboard.GetText();
        if (string.IsNullOrEmpty(text)) return;

        if (_selectionLength > 0)
            DeleteSelection();

        _text = _text.Insert(_cursorPosition, text);
        _cursorPosition += text.Length;
        _selectionAnchor = _cursorPosition;
        _selectionLength = 0;
        RaiseTextChanged();
        Owner?.Invalidate();
    }

    /// <summary>
    /// Selects all text in the text box.
    /// </summary>
    public void SelectAll()
    {
        _selectionAnchor = 0;
        _cursorPosition = _text.Length;
        _selectionLength = _cursorPosition - _selectionAnchor;
        Owner?.Invalidate();
    }
}
