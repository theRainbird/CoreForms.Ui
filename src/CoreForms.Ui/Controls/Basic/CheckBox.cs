using CoreForms.Ui.Core;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Basic;

public class CheckBox : Control
{
    private bool _checked;

    public CheckBox()
    {
        Size = new Size(200, 28);
        TabStop = true;
    }

    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked != value)
            {
                _checked = value;
                OnCheckedChanged();
                Invalidate();
            }
        }
    }

    public override void Render(Rendering.Graphics g)
    {
        if (!Visible) return;

        g.FillRectangle(Color.White, 0, (Height - 16) / 2, 16, 16);
        g.DrawRectangle(Color.FromArgb(128, 128, 128), 0, (Height - 16) / 2, 16, 16, 1);

        if (_checked)
        {
            var boxY = (Height - 16) / 2f;
            g.DrawLine(Color.Black, 3, boxY + 8, 6, boxY + 11, 3);
            g.DrawLine(Color.Black, 6, boxY + 11, 13, boxY + 4, 3);
        }

        var font = Font ?? Font.Default;
        g.DrawString(Text, font, ForeColor, 20, (Height - (int)font.Size) / 2);

        base.Render(g);
    }

    protected override void OnClick(EventArgs e)
    {
        Checked = !Checked;
        base.OnClick(e);
    }

    protected internal override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Space)
        {
            Checked = !Checked;
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }

    protected virtual void OnCheckedChanged()
    {
        CheckedChanged?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? CheckedChanged;
}