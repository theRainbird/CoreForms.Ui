using CoreForms.Ui.Core;
using CoreForms.Ui.Theming;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Basic;

/// <summary>
/// A control that allows the user to enter text.
/// </summary>
public class TextBox : Control
{
    private readonly TextEditorEngine _engine = new();
    private TextEditorContext? _context;

    /// <summary>
    /// Initializes a new instance of TextBox.
    /// </summary>
    public TextBox()
    {
        var theme = ThemeManager.CurrentTheme;
        _backColor = theme.TextBoxBackground;
        _foreColor = theme.TextBoxText;
        Size = new Size(200, 32);
        TabStop = true;
        _engine.TextChanged += (s, e) => { OnTextChanged(); Invalidate(); };
    }

    private TextEditorContext Context => _context ??= new TextEditorContext(this);

    /// <summary>
    /// Called when the theme changes. Updates textbox-specific colors.
    /// </summary>
    public override void OnThemeChanged(Theme newTheme)
    {
        if (!_backColorSet)
            _backColor = newTheme.TextBoxBackground;
        if (!_foreColorSet)
            _foreColor = newTheme.TextBoxText;
        Invalidate();
    }

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
            OnTextChanged();
            OnPropertyChanged(nameof(Text));
            Invalidate();
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
    /// Gets or sets whether the text box uses password mode, hiding characters with a bullet symbol.
    /// </summary>
    public bool UseSystemPasswordChar
    {
        get => _engine.UseSystemPasswordChar;
        set
        {
            if (_engine.UseSystemPasswordChar == value) return;
            _engine.UseSystemPasswordChar = value;
            Invalidate();
        }
    }

    /// <summary>
    /// Gets the selected text.
    /// </summary>
    public string SelectedText => _engine.SelectedText;

    /// <summary>
    /// Renders the text box with its text, selection, and cursor.
    /// </summary>
    public override void Render(Rendering.Graphics g)
    {
        if (!Visible) return;

        var theme = ThemeManager.CurrentTheme;

        g.FillRectangle(BackColor, 0, 0, Width, Height);

        if (Focused)
            g.DrawRectangle(theme.TextBoxFocusBorder, 0, 0, Width, Height, 2);
        else
            g.DrawRectangle(theme.TextBoxBorder, 0, 0, Width, Height, 1);

        var font = EffectiveFont;
        float zoom = EffectiveZoom;
        float scaledFontSize = font.Size * zoom;
        float textY = CoordinateTransform.CenterVertically(Height, font, zoom);
        float textX = 4 - _engine.ScrollOffset;

        g.SetClip(new Rectangle(4, 0, Width - 8, Height));

        var textColor = Enabled ? ForeColor : theme.GrayText;
        string displayText = _engine.DisplayText;
        var context = Context;

        if (Enabled && _engine.HasSelection && Focused)
        {
            int selStart = _engine.SelectionStartIndex;
            int selEnd = _engine.SelectionEndIndex;

            string beforeSel = displayText.Substring(0, selStart);
            string selStr = displayText.Substring(selStart, selEnd - selStart);
            float selX = textX + _engine.MeasureTextWidth(beforeSel, context);
            float selWidth = Math.Max(_engine.MeasureTextWidth(selStr, context), 2);

            g.DrawString(displayText, font, textColor, textX, textY);
            g.FillRectangle(theme.Highlight, selX, textY, selWidth, scaledFontSize + 2);
            g.DrawString(selStr, font, theme.HighlightText, selX, textY);
        }
        else
        {
            g.DrawString(displayText, font, textColor, textX, textY);
        }

        if (Enabled && Focused && TextEditorEngine.IsCursorBlinkVisible)
        {
            string textBeforeCursor = displayText.Substring(0, _engine.CursorPosition);
            float cursorX = textX + _engine.MeasureTextWidth(textBeforeCursor, context);
            g.DrawLine(theme.CursorLine, cursorX, textY, cursorX, textY + scaledFontSize, 1);
        }

        g.ResetClip();

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
    protected string? GetClipboardValue() => _engine.HasSelection ? _engine.SelectedText : _engine.Text;

    /// <summary>
    /// Copies the selected text or all text to the clipboard.
    /// </summary>
    public void CopyToClipboard() => _engine.CopyToClipboard();

    /// <summary>
    /// Cuts the selected text and copies it to the clipboard.
    /// </summary>
    public void Cut() => _engine.Cut(Context);

    /// <summary>
    /// Pastes the clipboard text at the current cursor position, replacing any selected text.
    /// </summary>
    public void Paste() => _engine.Paste(Context);

    /// <summary>
    /// Selects all text in the text box.
    /// </summary>
    public void SelectAll() => _engine.SelectAll(Context);

    /// <summary>
    /// Raises the MouseDown event and sets cursor position.
    /// </summary>
    protected internal override void OnMouseDown(EventArgs e)
    {
        if (!Enabled) return;
        if (e is MouseEventArgs mouseArgs)
        {
            int logicalX = mouseArgs.X - 4 + _engine.ScrollOffset;
            _engine.HandleMouseDown(logicalX, Context);
        }

        Focused = true;
        base.OnMouseDown(e);
    }

    /// <summary>
    /// Raises the KeyDown event to handle text editing.
    /// </summary>
    protected internal override void OnKeyDown(KeyEventArgs e)
    {
        if (!Enabled) return;
        _engine.HandleKeyDown(e, Context);
        base.OnKeyDown(e);
    }

    /// <summary>
    /// Raises the TextInput event to insert typed text.
    /// </summary>
    protected internal override void OnTextInput(string text)
    {
        if (!Enabled) return;
        _engine.HandleTextInput(text, Context);
    }

    /// <summary>
    /// Occurs when the text changes.
    /// </summary>
    public event EventHandler? TextChanged;

    private sealed class TextEditorContext : ITextEditorContext
    {
        private readonly TextBox _owner;
        public TextEditorContext(TextBox owner) => _owner = owner;
        public Font Font => _owner.EffectiveFont;
        public float Zoom => _owner.EffectiveZoom;
        public int TextAreaWidth => _owner.Width - 8;
        public void Invalidate() => _owner.Invalidate();
    }
}
