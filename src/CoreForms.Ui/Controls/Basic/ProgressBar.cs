using CoreForms.Ui.Core;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Basic;

public class ProgressBar : Control
{
    private int _minimum;
    private int _maximum = 100;
    private int _value;
    private Orientation _orientation = Orientation.Horizontal;

    public ProgressBar()
    {
        Size = new Size(200, 28);
    }

    public int Minimum
    {
        get => _minimum;
        set
        {
            _minimum = value;
            if (_value < _minimum) _value = _minimum;
            Invalidate();
        }
    }

    public int Maximum
    {
        get => _maximum;
        set
        {
            _maximum = value;
            if (_value > _maximum) _value = _maximum;
            Invalidate();
        }
    }

    public int Value
    {
        get => _value;
        set
        {
            if (value < _minimum) value = _minimum;
            if (value > _maximum) value = _maximum;
            if (_value != value)
            {
                _value = value;
                OnValueChanged();
                Invalidate();
            }
        }
    }

    public Orientation Orientation
    {
        get => _orientation;
        set
        {
            _orientation = value;
            Invalidate();
        }
    }

    public override void Render(Graphics g)
    {
        if (!Visible) return;

        g.FillRectangle(BackColor, 0, 0, Width, Height);
        g.DrawRectangle(Color.FromArgb(128, 128, 128), 0, 0, Width, Height, 1);

        if (_maximum > _minimum)
        {
            var percent = (float)(_value - _minimum) / (_maximum - _minimum);
            var fillWidth = (int)((Width - 2) * percent);

            if (_orientation == Orientation.Horizontal)
            {
                var progressColor = Color.FromArgb(0, 120, 215);
                g.FillRectangle(progressColor, 1, 1, fillWidth, Height - 2);
            }
            else
            {
                var progressColor = Color.FromArgb(0, 120, 215);
                g.FillRectangle(progressColor, 1, Height - 1 - fillWidth, Width - 2, fillWidth);
            }
        }

        base.Render(g);
    }

    protected virtual void OnValueChanged()
    {
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? ValueChanged;
}

public enum Orientation
{
    Horizontal,
    Vertical
}