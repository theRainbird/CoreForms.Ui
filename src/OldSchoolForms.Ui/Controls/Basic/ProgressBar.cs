using OldSchoolForms.Ui.Core;
using OldSchoolForms.Ui.Theming;
using Graphics = OldSchoolForms.Ui.Rendering.Graphics;

namespace OldSchoolForms.Ui.Controls.Basic;

/// <summary>
/// A control that displays a visual indicator of progress.
/// </summary>
public class ProgressBar : Control
{
    private int _minimum;
    private int _maximum = 100;
    private int _value;
    private Orientation _orientation = Orientation.Horizontal;

    /// <summary>
    /// Initializes a new instance of ProgressBar.
    /// </summary>
    public ProgressBar()
    {
        Size = new Size(200, 28);
    }

    /// <summary>
    /// Gets or sets the minimum value.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the maximum value.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the current value.
    /// </summary>
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
                OnPropertyChanged(nameof(Value));
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets the orientation of the progress bar.
    /// </summary>
    public Orientation Orientation
    {
        get => _orientation;
        set
        {
            _orientation = value;
            Invalidate();
        }
    }

    /// <summary>
    /// Renders the progress bar with its filled portion.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
    public override void Render(Graphics g)
    {
        if (!Visible) return;

        var theme = ThemeManager.CurrentTheme;

        g.FillRectangle(BackColor, 0, 0, Width, Height);
        g.DrawRectangle(theme.Border, 0, 0, Width, Height, 1);

        if (_maximum > _minimum)
        {
            var percent = (float)(_value - _minimum) / (_maximum - _minimum);
            var fillWidth = (int)((Width - 2) * percent);
            var fillColor = Enabled ? theme.ProgressBarFill : theme.GrayText;

            if (_orientation == Orientation.Horizontal)
            {
                g.FillRectangle(fillColor, 1, 1, fillWidth, Height - 2);
            }
            else
            {
                g.FillRectangle(fillColor, 1, Height - 1 - fillWidth, Width - 2, fillWidth);
            }
        }

        base.Render(g);
    }

    /// <summary>
    /// Raises the ValueChanged event.
    /// </summary>
    protected virtual void OnValueChanged()
    {
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Occurs when the value changes.
    /// </summary>
    public event EventHandler? ValueChanged;
}

/// <summary>
/// Specifies the orientation of the progress bar.
/// </summary>
public enum Orientation
{
    /// <summary>
    /// Horizontal orientation (left to right).
    /// </summary>
    Horizontal,

    /// <summary>
    /// Vertical orientation (bottom to top).
    /// </summary>
    Vertical
}