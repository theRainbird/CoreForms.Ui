using CoreForms.Ui.Core;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Basic;

/// <summary>
/// A control that allows the user to enter text.
/// </summary>
public class TextBox : Control
{
    private string _text = string.Empty;
    private int _cursorPosition;
    private int _selectionAnchor;
    private int _selectionLength;
    private static readonly int CursorBlinkInterval = 530;

    /// <summary>
    /// Initializes a new instance of TextBox.
    /// </summary>
    public TextBox()
    {
        BackColor = Color.White;
        ForeColor = Color.Black;
        Size = new Size(200, 32);
        TabStop = true;
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
                OnTextChanged();
                Invalidate();
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

    private bool _useSystemPasswordChar;

    /// <summary>
    /// Gets or sets whether the text box uses password mode, hiding characters with a bullet symbol.
    /// </summary>
    public bool UseSystemPasswordChar
    {
        get => _useSystemPasswordChar;
        set
        {
            if (_useSystemPasswordChar != value)
            {
                _useSystemPasswordChar = value;
                Invalidate();
            }
        }
    }

    private static readonly string BulletChar = "\u25CF";

    private string GetDisplayText()
    {
        return _useSystemPasswordChar ? new string('\u25CF', _text.Length) : _text;
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

    private int MeasureTextWidth(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;
        var font = Font ?? Font.Default;
        var measured = Platform.Platform.MeasureText(text, font);
        return measured.width;
    }

    private int MeasureDisplayWidth()
    {
        if (_text.Length == 0)
            return 0;
        if (_useSystemPasswordChar)
        {
            var font = Font ?? Font.Default;
            var measured = Platform.Platform.MeasureText(BulletChar, font);
            return measured.width * _text.Length;
        }
        return MeasureTextWidth(_text);
    }

    /// <summary>
    /// Renders the text box with its text, selection, and cursor.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
    public override void Render(Rendering.Graphics g)
    {
        if (!Visible) return;

g.FillRectangle(BackColor, 0, 0, Width, Height);

        if (Focused)
            g.DrawRectangle(Color.FromArgb(0, 120, 215), 0, 0, Width, Height, 2);
        else
            g.DrawRectangle(Color.FromArgb(128, 128, 128), 0, 0, Width, Height, 1);

        var font = Font ?? Font.Default;
        float textY = (Height - font.Size) / 2f;
        float textX = 4;

        string displayText = GetDisplayText();

        if (_selectionLength > 0 && Focused)
        {
            int selStart = Math.Min(_selectionAnchor, _cursorPosition);
            int selEnd = Math.Max(_selectionAnchor, _cursorPosition);

            string beforeSel = displayText.Substring(0, selStart);
            string selStr = displayText.Substring(selStart, selEnd - selStart);
            float selX = textX + MeasureTextWidth(beforeSel);
            float selWidth = Math.Max(MeasureTextWidth(selStr), 2);

            g.DrawString(displayText, font, ForeColor, textX, textY);
            g.FillRectangle(SystemColors.Highlight, selX, textY, selWidth, font.Size + 2);
            g.DrawString(selStr, font, SystemColors.HighlightText, selX, textY);
        }
        else
        {
            g.DrawString(displayText, font, ForeColor, textX, textY);
        }

        if (Focused)
        {
            bool cursorVisible = (Environment.TickCount % (CursorBlinkInterval * 2)) < CursorBlinkInterval;
            if (cursorVisible)
            {
                string textBeforeCursor = displayText.Substring(0, _cursorPosition);
                float cursorX = textX + MeasureTextWidth(textBeforeCursor);
                g.DrawLine(Color.Black, cursorX, textY, cursorX, textY + font.Size, 1);
            }
        }

        base.Render(g);
    }

    /// <summary>
    /// Raises the TextChanged event.
    /// </summary>
    protected override void OnTextChanged()
    {
        base.OnTextChanged();
        TextChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Gets the value to copy to clipboard.
    /// </summary>
    protected string? GetClipboardValue() => _selectionLength > 0 ? SelectedText : _text;

    /// <summary>
    /// Copies the selected text or all text to the clipboard.
    /// </summary>
    public void CopyToClipboard()
    {
        string text = _selectionLength > 0 ? SelectedText : _text;
        if (!string.IsNullOrEmpty(text))
        {
            Core.Clipboard.SetText(text);
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
            Core.Clipboard.SetText(selected);
        }
    }

    /// <summary>
    /// Pastes the clipboard text at the current cursor position,
    /// replacing any selected text.
    /// </summary>
    public void Paste()
    {
        var text = Core.Clipboard.GetText();
        if (string.IsNullOrEmpty(text))
            return;

        if (_selectionLength > 0)
            DeleteSelection();

        _text = _text.Insert(_cursorPosition, text);
        _cursorPosition += text.Length;
        _selectionAnchor = _cursorPosition;
        _selectionLength = 0;
        OnTextChanged();
    }

    /// <summary>
    /// Selects all text in the text box.
    /// </summary>
    public void SelectAll()
    {
        _selectionAnchor = 0;
        _cursorPosition = _text.Length;
        _selectionLength = _cursorPosition - _selectionAnchor;
    }

    /// <summary>
    /// Raises the MouseDown event and sets cursor position.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseDown(EventArgs e)
    {
        var mouseArgs = e as MouseEventArgs;
        if (mouseArgs != null)
        {
            int xPos = mouseArgs.X - 4;

            if (_useSystemPasswordChar && _text.Length > 0)
            {
                var font = Font ?? Font.Default;
                var bulletMeasured = Platform.Platform.MeasureText(BulletChar, font);
                int bulletWidth = bulletMeasured.width;
                _cursorPosition = Math.Min(xPos / bulletWidth + 1, _text.Length);
            }
            else
            {
                int bestPos = 0;
                int bestDist = Math.Abs(xPos);

                for (int i = 1; i <= _text.Length; i++)
                {
                    int w = MeasureTextWidth(_text.Substring(0, i));
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
        }

        Focused = true;
        base.OnMouseDown(e);
    }

    /// <summary>
    /// Raises the KeyDown event to handle text editing.
    /// </summary>
    /// <param name="e">A KeyEventArgs that contains the event data.</param>
    protected internal override void OnKeyDown(KeyEventArgs e)
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
            base.OnKeyDown(e);
            return;
        }

        bool shift = e.Modifiers.HasFlag(ModifierKeys.Shift);

        switch (e.KeyCode)
        {
            case Keys.Back:
                if (_selectionLength > 0)
                {
                    DeleteSelection();
                }
                else if (_cursorPosition > 0)
                {
                    _text = _text.Remove(_cursorPosition - 1, 1);
                    _cursorPosition--;
                    _selectionAnchor = _cursorPosition;
                    _selectionLength = 0;
                }
                OnTextChanged();
                e.Handled = true;
                break;
            case Keys.Delete:
                if (_selectionLength > 0)
                {
                    DeleteSelection();
                }
                else if (_cursorPosition < _text.Length)
                {
                    _text = _text.Remove(_cursorPosition, 1);
                }
                OnTextChanged();
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
        }
        base.OnKeyDown(e);
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
    /// Raises the TextInput event to insert typed text.
    /// </summary>
    /// <param name="text">The input text.</param>
    protected internal override void OnTextInput(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        if (_selectionLength > 0)
            DeleteSelection();

        _text = _text.Insert(_cursorPosition, text);
        _cursorPosition += text.Length;
        _selectionAnchor = _cursorPosition;
        _selectionLength = 0;
        OnTextChanged();
    }

    /// <summary>
    /// Occurs when the text changes.
    /// </summary>
    public event EventHandler? TextChanged;
}