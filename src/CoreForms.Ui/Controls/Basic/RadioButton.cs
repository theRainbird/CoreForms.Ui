using CoreForms.Ui.Core;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Basic;

public class RadioButton : Control
{
    private bool _checked;
    private RadioButton? _group;

    public RadioButton()
    {
        Size = new Size(200, 28);
    }

    public bool Checked
    {
        get => _checked;
        set
        {
            if (value)
            {
                UncheckOthers();
            }

            if (_checked != value)
            {
                _checked = value;
                OnCheckedChanged();
                Invalidate();
            }
        }
    }

    private void UncheckOthers()
    {
        if (Parent is ContainerControl container)
        {
            foreach (var control in container.Controls)
            {
                if (control is RadioButton rb && rb != this && rb.Checked)
                {
                    rb.Checked = false;
                }
            }
        }
    }

    public override void Render(Rendering.Graphics g)
    {
        if (!Visible) return;

        var centerY = Height / 2;

        g.FillRectangle(Color.White, 0, centerY - 8, 16, 16);
        g.DrawRectangle(Color.FromArgb(128, 128, 128), 0, centerY - 8, 16, 16, 1);

        if (_checked)
        {
            g.FillRectangle(Color.Black, 4, centerY - 4, 8, 8);
        }

        var font = Font ?? Font.Default;
        g.DrawString(Text, font, ForeColor, 20, (Height - (int)font.Size) / 2);

        base.Render(g);
    }

    protected override void OnClick(EventArgs e)
    {
        Checked = true;
        base.OnClick(e);
    }

    protected virtual void OnCheckedChanged()
    {
        CheckedChanged?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? CheckedChanged;
}