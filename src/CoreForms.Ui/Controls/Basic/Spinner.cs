using System;
using System.Threading;
using CoreForms.Ui.Core;
using CoreForms.Ui.Theming;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Basic;

/// <summary>
/// Represents an animated loading spinner control with chasing dots.
/// </summary>
public class Spinner : Control
{
    private bool _active;
    private bool _autoStart = true;
    private int _dotCount = 8;
    private int _dotRadius = 4;
    private int _spinnerRadius = 12;
    private int _animationInterval = 80;
    private Color _dotColor;
    private bool _dotColorSet;
    private long _lastTick;
    private Timer? _animationTimer;

    /// <summary>
    /// Initializes a new instance of the Spinner class.
    /// </summary>
    public Spinner()
    {
        Size = new Size(50, 50);
        _backColor = Color.Transparent;
        _backColorSet = true;
        _dotColor = ForeColor;
    }

    /// <summary>
    /// Gets or sets whether the spinner animation is active.
    /// </summary>
    public bool Active
    {
        get => _active;
        set
        {
            if (_active != value)
            {
                _active = value;
                if (_active)
                {
                    _lastTick = Environment.TickCount;
                    StartAnimation();
                }
                else
                {
                    StopAnimation();
                }
                OnActiveChanged();
                Invalidate();
            }
        }
    }

    private void StartAnimation()
    {
        StopAnimation();
        _animationTimer = new Timer(_ => Invalidate(), null, 0, _animationInterval);
    }

    private void StopAnimation()
    {
        _animationTimer?.Dispose();
        _animationTimer = null;
    }

    /// <summary>
    /// Gets or sets whether the spinner starts animating automatically when becoming visible.
    /// </summary>
    public bool AutoStart
    {
        get => _autoStart;
        set
        {
            if (_autoStart != value)
            {
                _autoStart = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets the number of dots in the spinner animation.
    /// </summary>
    public int DotCount
    {
        get => _dotCount;
        set
        {
            if (value < 3) value = 3;
            if (_dotCount != value)
            {
                _dotCount = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets the radius of each dot in pixels.
    /// </summary>
    public int DotRadius
    {
        get => _dotRadius;
        set
        {
            if (value < 1) value = 1;
            if (_dotRadius != value)
            {
                _dotRadius = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets the radius of the spinner circle in pixels.
    /// </summary>
    public int SpinnerRadius
    {
        get => _spinnerRadius;
        set
        {
            if (value < 1) value = 1;
            if (_spinnerRadius != value)
            {
                _spinnerRadius = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets the animation interval in milliseconds between phase advances.
    /// </summary>
    public int AnimationInterval
    {
        get => _animationInterval;
        set
        {
            if (value < 1) value = 1;
            if (_animationInterval != value)
            {
                _animationInterval = value;
                if (_active)
                    StartAnimation();
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets the color of the dots. Defaults to the control's foreground color.
    /// </summary>
    public Color DotColor
    {
        get => _dotColor;
        set
        {
            _dotColor = value;
            _dotColorSet = true;
            Invalidate();
        }
    }

    /// <summary>
    /// Raises the ActiveChanged event.
    /// </summary>
    protected virtual void OnActiveChanged()
    {
        ActiveChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Occurs when the <see cref="Active"/> property changes.
    /// </summary>
    public event EventHandler? ActiveChanged;

    /// <summary>
    /// Renders the spinner with animated chasing dots.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
    public override void Render(Graphics g)
    {
        if (!Visible) return;

        if (_active)
        {
            var elapsed = Environment.TickCount - _lastTick;
            if (elapsed < 0) elapsed = 0;
            var phase = (int)((elapsed / _animationInterval) % _dotCount);

            var centerX = Width / 2.0f;
            var centerY = Height / 2.0f;

            for (var i = 0; i < _dotCount; i++)
            {
                var dist = (i - phase + _dotCount) % _dotCount;
                var opacity = CalculateOpacity(dist, _dotCount);

                var angle = i * 2.0 * Math.PI / _dotCount;
                var x = centerX + (float)Math.Sin(angle) * _spinnerRadius - _dotRadius;
                var y = centerY - (float)Math.Cos(angle) * _spinnerRadius - _dotRadius;

                var dotColor = Color.FromArgb(
                    _dotColor.R, _dotColor.G, _dotColor.B,
                    (int)(_dotColor.A * opacity));

                g.FillEllipse(dotColor, x, y, _dotRadius * 2, _dotRadius * 2);
            }
        }

        base.Render(g);
    }

    private static float CalculateOpacity(int distance, int total)
    {
        var half = total / 2;
        if (distance >= half) return 0.0f;
        return 1.0f - (float)distance / half;
    }

    /// <summary>
    /// Called when the active theme changes. Updates the dot color if not explicitly set.
    /// </summary>
    /// <param name="newTheme">The new theme that was activated.</param>
    public override void OnThemeChanged(Theme newTheme)
    {
        base.OnThemeChanged(newTheme);
        if (!_dotColorSet)
            _dotColor = ForeColor;
    }

    /// <summary>
    /// Called when the visibility of the control changes.
    /// </summary>
    protected override void OnVisibleChanged()
    {
        if (_autoStart && Visible && !_active)
            Active = true;
        else if (_autoStart && !Visible && _active)
            Active = false;

        base.OnVisibleChanged();
    }
}
