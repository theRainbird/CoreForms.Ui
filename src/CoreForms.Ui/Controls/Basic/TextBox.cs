using CoreForms.Ui.Core;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Basic;

public class TextBox : Control
{
    private string _text = string.Empty;
    private int _selectionStart;
    private int _selectionLength;

    public TextBox()
    {
        BackColor = Color.White;
        ForeColor = Color.Black;
        Size = new Size(200, 32);
    }

    public new string Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value;
                OnTextChanged();
                Invalidate();
            }
        }
    }

    public int SelectionStart
    {
        get => _selectionStart;
        set => _selectionStart = value;
    }

    public int SelectionLength
    {
        get => _selectionLength;
        set => _selectionLength = value;
    }

    public string SelectedText => _selectionLength > 0 && _selectionStart < _text.Length
        ? _text.Substring(_selectionStart, Math.Min(_selectionLength, _text.Length - _selectionStart))
        : string.Empty;

    public override void Render(Rendering.Graphics g)
    {
        if (!Visible) return;

        g.FillRectangle(BackColor, 0, 0, Width, Height);

        if (Focused)
            g.DrawRectangle(SystemColors.Highlight, 0, 0, Width, Height, 2);
        else
            g.DrawRectangle(Color.FromArgb(128, 128, 128), 0, 0, Width, Height, 1);

        var font = Font ?? Font.Default;
        g.DrawString(_text, font, ForeColor, 3, (Height - (int)font.Size) / 2);

        base.Render(g);
    }

    protected override void OnTextChanged()
    {
        base.OnTextChanged();
        TextChanged?.Invoke(this, EventArgs.Empty);
    }

    protected internal override void OnMouseDown(EventArgs e)
    {
        Focused = true;
        base.OnMouseDown(e);
    }

    protected internal override void OnKeyDown(KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Back:
                if (_text.Length > 0 && _selectionStart > 0)
                {
                    _text = _text.Remove(_selectionStart - 1, 1);
                    _selectionStart--;
                    OnTextChanged();
                }
                e.Handled = true;
                break;
            case Keys.Delete:
                if (_selectionStart < _text.Length)
                {
                    _text = _text.Remove(_selectionStart, 1);
                    OnTextChanged();
                }
                e.Handled = true;
                break;
            case Keys.Left:
                if (_selectionStart > 0)
                    _selectionStart--;
                e.Handled = true;
                break;
            case Keys.Right:
                if (_selectionStart < _text.Length)
                    _selectionStart++;
                e.Handled = true;
                break;
            case Keys.Home:
                _selectionStart = 0;
                e.Handled = true;
                break;
            case Keys.End:
                _selectionStart = _text.Length;
                e.Handled = true;
                break;
        }
        base.OnKeyDown(e);
    }

    protected internal override void OnTextInput(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        _text = _text.Insert(_selectionStart, text);
        _selectionStart += text.Length;
        OnTextChanged();
    }

    public event EventHandler? TextChanged;
}