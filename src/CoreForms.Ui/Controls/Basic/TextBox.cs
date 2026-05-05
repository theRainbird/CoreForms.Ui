using CoreForms.Ui.Core;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Basic;

public class TextBox : Control
{
    private string _text = string.Empty;
    private int _selectionStart;
    private int _selectionLength;
    private bool _focused;

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
        _focused = true;
        base.OnMouseDown(e);
    }

    public event EventHandler? TextChanged;
}