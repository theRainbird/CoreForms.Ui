using CoreForms.Ui.Core;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Containers;

/// <summary>
/// Represents an editable text box in a ToolStrip.
/// Supports full text editing including cursor, selection, copy/cut/paste, and password mode.
/// </summary>
public class ToolStripTextBox : ToolStripItem
{
    private readonly TextEditorEngine _engine = new();
    private TextEditorContext? _context;
    private bool _focused;
    private int _width = 100;

    /// <summary>
    /// Initializes a new instance of ToolStripTextBox.
    /// </summary>
    public ToolStripTextBox()
    {
        DisplayStyle = ToolStripItemDisplayStyle.Text;
        _engine.TextChanged += (s, e) => { RaiseTextChanged(); Owner?.Invalidate(); };
    }

    /// <summary>
    /// Initializes a new instance of ToolStripTextBox with the specified text.
    /// </summary>
    /// <param name="text">The initial text.</param>
    public ToolStripTextBox(string text)
    {
        _engine.Text = text;
        _engine.EnsureCursorVisible(Context);
        DisplayStyle = ToolStripItemDisplayStyle.Text;
        _engine.TextChanged += (s, e) => { RaiseTextChanged(); Owner?.Invalidate(); };
    }

    private TextEditorContext Context => _context ??= new TextEditorContext(this);

    /// <summary>
    /// Gets or sets the text in the text box.
    /// </summary>
    public new string Text
    {
        get => _engine.Text;
        set
        {
            if (_engine.Text == value) return;
            _engine.Text = value;
            _engine.EnsureCursorVisible(Context);
            RaiseTextChanged();
            Owner?.Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the starting position of the selected text.
    /// </summary>
    public int SelectionStart
    {
        get => _engine.SelectionStart;
        set => _engine.SelectionStart = value;
    }

    /// <summary>
    /// Gets or sets the number of selected characters.
    /// </summary>
    public int SelectionLength
    {
        get => _engine.SelectionLength;
        set => _engine.SelectionLength = value;
    }

    /// <summary>
    /// Gets the selected text.
    /// </summary>
    public string SelectedText => _engine.SelectedText;

    /// <summary>
    /// Gets or sets whether the text box uses password mode.
    /// </summary>
    public bool UseSystemPasswordChar
    {
        get => _engine.UseSystemPasswordChar;
        set
        {
            if (_engine.UseSystemPasswordChar == value) return;
            _engine.UseSystemPasswordChar = value;
            Owner?.Invalidate();
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
                    _engine.SelectionLength = 0;
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

    /// <summary>
    /// Renders the text box with its text, selection, and cursor.
    /// </summary>
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
        float textX = tbX + 4 - _engine.ScrollOffset;

        g.SetClip(new Rectangle(tbX + 4, tbY, tbWidth - 8, tbHeight));

        string displayText = _engine.DisplayText;
        var context = Context;

        if (_engine.HasSelection && _focused)
        {
            int selStart = _engine.SelectionStartIndex;
            int selEnd = _engine.SelectionEndIndex;

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

        if (_focused && TextEditorEngine.IsCursorBlinkVisible)
        {
            string textBeforeCursor = displayText.Substring(0, _engine.CursorPosition);
            float cursorX = textX + MeasureLocalTextWidth(textBeforeCursor, font, zoom);
            g.DrawLine(Color.Black, cursorX, textY, cursorX, textY + font.Size * zoom, 1);
        }

        g.ResetClip();
    }

    private int MeasureLocalTextWidth(string text, Font font, float zoom)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        var measured = Platform.Platform.MeasureText(text, font, zoom);
        return (int)(measured.width / zoom);
    }

    /// <summary>
    /// Calculates the preferred width for layout.
    /// </summary>
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

        int logicalX = x - itemX - 6 + _engine.ScrollOffset;
        _engine.HandleMouseDown(logicalX, Context);

        Owner?.Invalidate();
    }

    /// <summary>
    /// Handles text input.
    /// </summary>
    internal void HandleTextInput(string text)
    {
        _engine.HandleTextInput(text, Context);
    }

    /// <summary>
    /// Handles key down events for editing operations.
    /// </summary>
    internal bool HandleKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            Focused = false;
            e.Handled = true;
            return true;
        }

        bool handled = _engine.HandleKeyDown(e, Context);
        if (handled)
        {
            Owner?.Invalidate();
        }
        return handled;
    }

    /// <summary>
    /// Copies the selected text or all text to the clipboard.
    /// </summary>
    public void CopyToClipboard() => _engine.CopyToClipboard();

    /// <summary>
    /// Cuts the selected text and copies it to the clipboard.
    /// </summary>
    public void Cut() => _engine.Cut(Context);

    /// <summary>
    /// Pastes the clipboard text at the current cursor position.
    /// </summary>
    public void Paste() => _engine.Paste(Context);

    /// <summary>
    /// Selects all text in the text box.
    /// </summary>
    public void SelectAll()
    {
        _engine.SelectAll(Context);
        Owner?.Invalidate();
    }

    private sealed class TextEditorContext : ITextEditorContext
    {
        private readonly ToolStripTextBox _owner;
        public TextEditorContext(ToolStripTextBox owner) => _owner = owner;
        public Font Font => _owner.Owner?.Font ?? Font.Default;
        public float Zoom => _owner.Owner?.EffectiveZoom ?? 1.0f;
        public int TextAreaWidth => _owner._width - 8;
        public void Invalidate() => _owner.Owner?.Invalidate();
    }
}
