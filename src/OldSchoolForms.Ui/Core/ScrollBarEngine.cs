using OldSchoolForms.Ui.Theming;
using Graphics = OldSchoolForms.Ui.Rendering.Graphics;

namespace OldSchoolForms.Ui.Core;

/// <summary>
/// Provides reusable scrollbar logic including state management, rendering, hit testing, and mouse interaction.
/// Designed to be shared by any control requiring scrollbar functionality.
/// Requires an IScrollBarContext for zoom/invalidation/mouse-capture access.
/// Supports both vertical and horizontal orientations with arrow buttons, a draggable thumb, and track clicks.
/// </summary>
public class ScrollBarEngine
{
    /// <summary>
    /// Scrollbar orientation.
    /// </summary>
    public enum ScrollBarOrientation
    {
        /// <summary>
        /// Vertical scrollbar (scrolls up/down).
        /// </summary>
        Vertical,

        /// <summary>
        /// Horizontal scrollbar (scrolls left/right).
        /// </summary>
        Horizontal
    }

    /// <summary>
    /// Results from a hit test against the scrollbar.
    /// </summary>
    public enum ScrollBarHitTest
    {
        /// <summary>
        /// No scrollbar element was hit.
        /// </summary>
        None,

        /// <summary>
        /// The up/left arrow button was hit.
        /// </summary>
        UpButton,

        /// <summary>
        /// The down/right arrow button was hit.
        /// </summary>
        DownButton,

        /// <summary>
        /// The track area before the thumb was hit.
        /// </summary>
        TrackAboveThumb,

        /// <summary>
        /// The draggable thumb was hit.
        /// </summary>
        Thumb,

        /// <summary>
        /// The track area after the thumb was hit.
        /// </summary>
        TrackBelowThumb
    }

    private enum ButtonState
    {
        Normal,
        Hover,
        Pressed
    }

    /// <summary>
    /// Default width for vertical scrollbars.
    /// </summary>
    public const int DefaultScrollBarSize = 16;

    /// <summary>
    /// Default size for arrow buttons.
    /// </summary>
    public const int DefaultButtonSize = 16;

    /// <summary>
    /// Minimum size for the draggable thumb.
    /// </summary>
    public const int MinThumbSize = 16;

    private int _minimum;
    private int _maximum = 100;
    private int _value;
    private int _viewSize = 100;
    private int _contentSize = 100;
    private int _smallChange = 1;
    private int _largeChange = 10;
    private ScrollBarOrientation _orientation;

    private int _scrollBarSize = DefaultScrollBarSize;
    private int _buttonSize = DefaultButtonSize;

    private ButtonState _upButtonState;
    private ButtonState _downButtonState;
    private bool _thumbHovered;
    private bool _isDragging;
    private int _dragStartValue;
    private int _dragStartPos;
    private float _scrollAccumulator;

    /// <summary>
    /// Occurs when the scrollbar value changes (during drag, button click, or track click).
    /// </summary>
    public event EventHandler? Scroll;

    /// <summary>
    /// Occurs when the scrollbar value has finished changing (after drag ends, button click completes).
    /// </summary>
    public event EventHandler? ValueChanged;

    /// <summary>
    /// Gets or sets the minimum scroll value.
    /// </summary>
    public int Minimum
    {
        get => _minimum;
        set
        {
            _minimum = value;
            Value = _value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum scroll value. The effective maximum scroll offset is Maximum - ViewSize (or content-based when using ContentSize).
    /// </summary>
    public int Maximum
    {
        get => _maximum;
        set
        {
            _maximum = value;
            Value = _value;
        }
    }

    /// <summary>
    /// Gets or sets the current scroll position. Clamped to [Minimum, MaxScroll].
    /// </summary>
    public int Value
    {
        get => _value;
        set
        {
            int newValue = Clamp(value);
            if (_value != newValue)
            {
                _value = newValue;
                Scroll?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Gets the maximum scroll offset based on ContentSize and ViewSize.
    /// </summary>
    public int MaxScroll => Math.Max(0, _contentSize - _viewSize);

    /// <summary>
    /// Gets or sets the visible area size in logical units.
    /// </summary>
    public int ViewSize
    {
        get => _viewSize;
        set => _viewSize = Math.Max(0, value);
    }

    /// <summary>
    /// Gets or sets the total content size in logical units.
    /// </summary>
    public int ContentSize
    {
        get => _contentSize;
        set => _contentSize = Math.Max(0, value);
    }

    /// <summary>
    /// Gets or sets the small change value (for arrow button clicks). Default is 1.
    /// </summary>
    public int SmallChange
    {
        get => _smallChange;
        set => _smallChange = Math.Max(1, value);
    }

    /// <summary>
    /// Gets or sets the large change value (for track clicks). Default is 10.
    /// </summary>
    public int LargeChange
    {
        get => _largeChange;
        set => _largeChange = Math.Max(1, value);
    }

    /// <summary>
    /// Gets or sets the scrollbar orientation.
    /// </summary>
    public ScrollBarOrientation Orientation
    {
        get => _orientation;
        set => _orientation = value;
    }

    /// <summary>
    /// Gets or sets the thickness of the scrollbar (width for vertical, height for horizontal). Default is 16.
    /// </summary>
    public int ScrollBarSize
    {
        get => _scrollBarSize;
        set => _scrollBarSize = Math.Max(DefaultButtonSize, value);
    }

    /// <summary>
    /// Gets or sets the arrow button size. Default is 16.
    /// </summary>
    public int ButtonSize
    {
        get => _buttonSize;
        set => _buttonSize = Math.Max(8, value);
    }

    /// <summary>
    /// Gets whether a scrollbar is needed (content exceeds view).
    /// </summary>
    public bool NeedsScrollbar => _contentSize > _viewSize;

    /// <summary>
    /// Gets whether the thumb is currently being dragged.
    /// </summary>
    public bool IsDragging => _isDragging;

    /// <summary>
    /// Gets whether the up/left button is currently pressed.
    /// </summary>
    public bool IsUpButtonPressed => _upButtonState == ButtonState.Pressed;

    /// <summary>
    /// Gets whether the down/right button is currently pressed.
    /// </summary>
    public bool IsDownButtonPressed => _downButtonState == ButtonState.Pressed;

    /// <summary>
    /// Renders the scrollbar using the specified graphics context, bounds, and theme.
    /// </summary>
    /// <param name="g">The Graphics object to render onto.</param>
    /// <param name="bounds">The bounding rectangle of the scrollbar in logical coordinates.</param>
    /// <param name="theme">The current theme providing scrollbar colors.</param>
    public void Render(Graphics g, Rectangle bounds, Theme theme)
    {
        if (!NeedsScrollbar) return;

        var upRect = GetUpButtonRect(bounds);
        var downRect = GetDownButtonRect(bounds);
        var trackRect = GetTrackRect(bounds);
        var thumbRect = GetThumbRect(bounds);

        // Track
        g.FillRectangle(theme.ScrollbarTrack, trackRect.X, trackRect.Y, trackRect.Width, trackRect.Height);
        g.DrawRectangle(theme.ScrollbarBorder, bounds.X, bounds.Y, bounds.Width, bounds.Height, 1);

        // Up button
        DrawButton(g, upRect, theme, _upButtonState, isUp: true);

        // Down button
        DrawButton(g, downRect, theme, _downButtonState, isUp: false);

        // Thumb
        var thumbColor = _thumbHovered || _isDragging ? Lighten(theme.ScrollbarThumb) : theme.ScrollbarThumb;
        g.FillRectangle(thumbColor, thumbRect.X, thumbRect.Y, thumbRect.Width, thumbRect.Height);
        g.DrawRectangle(theme.ScrollbarThumbBorder, thumbRect.X, thumbRect.Y, thumbRect.Width, thumbRect.Height, 1);
    }

    /// <summary>
    /// Determines which part of the scrollbar is at the given point.
    /// </summary>
    /// <param name="point">The point in logical coordinates relative to the scrollbar bounds.</param>
    /// <param name="bounds">The bounding rectangle of the scrollbar.</param>
    /// <returns>A ScrollBarHitTest value indicating which element was hit.</returns>
    public ScrollBarHitTest HitTest(Point point, Rectangle bounds)
    {
        if (!NeedsScrollbar || !bounds.Contains(point.X, point.Y))
            return ScrollBarHitTest.None;

        var upRect = GetUpButtonRect(bounds);
        var downRect = GetDownButtonRect(bounds);
        var thumbRect = GetThumbRect(bounds);

        if (upRect.Contains(point.X, point.Y))
            return ScrollBarHitTest.UpButton;

        if (downRect.Contains(point.X, point.Y))
            return ScrollBarHitTest.DownButton;

        if (thumbRect.Contains(point.X, point.Y))
            return ScrollBarHitTest.Thumb;

        // Determine if click is above or below the thumb within the track
        bool isAbove = _orientation == ScrollBarOrientation.Vertical
            ? point.Y < thumbRect.Y
            : point.X < thumbRect.X;

        return isAbove ? ScrollBarHitTest.TrackAboveThumb : ScrollBarHitTest.TrackBelowThumb;
    }

    /// <summary>
    /// Handles mouse down events. Starts thumb drag, processes button clicks, or initiates track scrolling.
    /// </summary>
    /// <param name="point">The mouse position in logical coordinates relative to the scrollbar bounds.</param>
    /// <param name="bounds">The bounding rectangle of the scrollbar.</param>
    /// <param name="context">The scrollbar context for invalidation and capture.</param>
    public void HandleMouseDown(Point point, Rectangle bounds, IScrollBarContext context)
    {
        if (!NeedsScrollbar) return;

        var hit = HitTest(point, bounds);
        switch (hit)
        {
            case ScrollBarHitTest.UpButton:
                _upButtonState = ButtonState.Pressed;
                ScrollBy(-_smallChange);
                context.Invalidate();
                break;

            case ScrollBarHitTest.DownButton:
                _downButtonState = ButtonState.Pressed;
                ScrollBy(_smallChange);
                context.Invalidate();
                break;

            case ScrollBarHitTest.Thumb:
                _isDragging = true;
                _dragStartValue = _value;
                _dragStartPos = _orientation == ScrollBarOrientation.Vertical ? point.Y : point.X;
                context.CaptureMouse(true);
                context.Invalidate();
                break;

            case ScrollBarHitTest.TrackAboveThumb:
                ScrollBy(-_largeChange);
                context.Invalidate();
                break;

            case ScrollBarHitTest.TrackBelowThumb:
                ScrollBy(_largeChange);
                context.Invalidate();
                break;
        }
    }

    /// <summary>
    /// Handles mouse up events. Ends thumb drag and resets button states.
    /// </summary>
    /// <param name="context">The scrollbar context for invalidation and capture.</param>
    public void HandleMouseUp(IScrollBarContext context)
    {
        bool wasDragging = _isDragging;
        bool wasPressed = _upButtonState == ButtonState.Pressed || _downButtonState == ButtonState.Pressed;

        _upButtonState = ButtonState.Normal;
        _downButtonState = ButtonState.Normal;

        if (_isDragging)
        {
            _isDragging = false;
            context.CaptureMouse(false);
        }

        if (wasDragging || wasPressed)
        {
            ValueChanged?.Invoke(this, EventArgs.Empty);
            context.Invalidate();
        }
    }

    /// <summary>
    /// Handles mouse move events. Updates thumb drag position and hover states.
    /// </summary>
    /// <param name="point">The mouse position in logical coordinates relative to the scrollbar bounds.</param>
    /// <param name="bounds">The bounding rectangle of the scrollbar.</param>
    /// <param name="context">The scrollbar context for invalidation.</param>
    public void HandleMouseMove(Point point, Rectangle bounds, IScrollBarContext context)
    {
        bool needsInvalidate = false;

        if (_isDragging)
        {
            int trackSize = GetTrackSize(bounds);
            int thumbSize = GetThumbSize(bounds);
            int dragRange = trackSize - thumbSize;
            if (dragRange > 0)
            {
                int currentPos = _orientation == ScrollBarOrientation.Vertical ? point.Y : point.X;
                int delta = currentPos - _dragStartPos;
                int maxScroll = MaxScroll;
                int newValue = _dragStartValue + (int)((float)delta / dragRange * maxScroll);
                newValue = Clamp(newValue);
                if (_value != newValue)
                {
                    _value = newValue;
                    Scroll?.Invoke(this, EventArgs.Empty);
                    needsInvalidate = true;
                }
            }
        }
        else
        {
            // Update hover states
            var upRect = GetUpButtonRect(bounds);
            var downRect = GetDownButtonRect(bounds);
            var thumbRect = GetThumbRect(bounds);

            bool upHover = upRect.Contains(point.X, point.Y);
            bool downHover = downRect.Contains(point.X, point.Y);
            bool thumbHover = !upHover && !downHover && thumbRect.Contains(point.X, point.Y);

            if (upHover != (_upButtonState == ButtonState.Hover))
            {
                _upButtonState = upHover ? ButtonState.Hover : ButtonState.Normal;
                needsInvalidate = true;
            }

            if (downHover != (_downButtonState == ButtonState.Hover))
            {
                _downButtonState = downHover ? ButtonState.Hover : ButtonState.Normal;
                needsInvalidate = true;
            }

            if (thumbHover != _thumbHovered)
            {
                _thumbHovered = thumbHover;
                needsInvalidate = true;
            }
        }

        if (needsInvalidate)
            context.Invalidate();
    }

    /// <summary>
    /// Handles mouse leave events. Resets hover states.
    /// </summary>
    /// <param name="context">The scrollbar context for invalidation.</param>
    public void HandleMouseLeave(IScrollBarContext context)
    {
        bool needsInvalidate = _upButtonState == ButtonState.Hover || _downButtonState == ButtonState.Hover || _thumbHovered;

        _upButtonState = ButtonState.Normal;
        _downButtonState = ButtonState.Normal;
        _thumbHovered = false;

        if (needsInvalidate)
            context.Invalidate();
    }

    /// <summary>
    /// Handles mouse wheel events for scrolling. Accumulates fractional delta values (from smooth-scroll devices)
    /// and scrolls by up to one quarter of the view size per full notch, rounded to SmallChange multiples.
    /// </summary>
    /// <param name="delta">The mouse wheel delta value (float; ±1 per notch, fractional for smooth scroll).</param>
    /// <param name="context">The scrollbar context for invalidation.</param>
    public void HandleMouseWheel(float delta, IScrollBarContext context)
    {
        if (!NeedsScrollbar) return;
        _scrollAccumulator += delta;
        int steps = (int)_scrollAccumulator;
        if (steps == 0) return;
        _scrollAccumulator -= steps;

        int scrollAmount = Math.Max(_smallChange, _viewSize / 4);
        scrollAmount = (scrollAmount / _smallChange) * _smallChange;
        ScrollBy(-steps * scrollAmount);
    }

    /// <summary>
    /// Scrolls by the specified delta amount. Positive values scroll down/right, negative values scroll up/left.
    /// </summary>
    /// <param name="delta">The amount to scroll.</param>
    public void ScrollBy(int delta)
    {
        Value = _value + delta;
    }

    /// <summary>
    /// Scrolls to the specified value directly.
    /// </summary>
    /// <param name="value">The target scroll value.</param>
    public void ScrollTo(int value)
    {
        Value = value;
    }

    /// <summary>
    /// Ensures the range [offset, offset + size] is visible, scrolling if necessary.
    /// </summary>
    /// <param name="offset">The start offset of the element to make visible.</param>
    /// <param name="size">The size of the element to make visible.</param>
    public void EnsureVisible(int offset, int size)
    {
        if (size > _viewSize)
        {
            Value = offset;
            return;
        }

        if (offset < _value)
            Value = offset;
        else if (offset + size > _value + _viewSize)
            Value = offset + size - _viewSize;
    }

    /// <summary>
    /// Gets the rectangle for the up/left arrow button within the given bounds.
    /// </summary>
    public Rectangle GetUpButtonRect(Rectangle bounds)
    {
        return _orientation == ScrollBarOrientation.Vertical
            ? new Rectangle(bounds.X, bounds.Y, bounds.Width, _buttonSize)
            : new Rectangle(bounds.X, bounds.Y, _buttonSize, bounds.Height);
    }

    /// <summary>
    /// Gets the rectangle for the down/right arrow button within the given bounds.
    /// </summary>
    public Rectangle GetDownButtonRect(Rectangle bounds)
    {
        return _orientation == ScrollBarOrientation.Vertical
            ? new Rectangle(bounds.X, bounds.Bottom - _buttonSize, bounds.Width, _buttonSize)
            : new Rectangle(bounds.Right - _buttonSize, bounds.Y, _buttonSize, bounds.Height);
    }

    /// <summary>
    /// Gets the track rectangle (the area between the two arrow buttons).
    /// </summary>
    public Rectangle GetTrackRect(Rectangle bounds)
    {
        return _orientation == ScrollBarOrientation.Vertical
            ? new Rectangle(bounds.X, bounds.Y + _buttonSize, bounds.Width, bounds.Height - _buttonSize * 2)
            : new Rectangle(bounds.X + _buttonSize, bounds.Y, bounds.Width - _buttonSize * 2, bounds.Height);
    }

    /// <summary>
    /// Gets the rectangle for the draggable thumb within the given bounds.
    /// </summary>
    public Rectangle GetThumbRect(Rectangle bounds)
    {
        int trackSize = GetTrackSize(bounds);
        int thumbSize = GetThumbSize(bounds);
        int maxScroll = MaxScroll;

        if (maxScroll <= 0)
        {
            return _orientation == ScrollBarOrientation.Vertical
                ? new Rectangle(bounds.X + 2, bounds.Y + _buttonSize + 2, bounds.Width - 4, trackSize - 4)
                : new Rectangle(bounds.X + _buttonSize + 2, bounds.Y + 2, trackSize - 4, bounds.Height - 4);
        }

        int thumbPos = maxScroll > 0
            ? (int)((float)_value / maxScroll * (trackSize - thumbSize))
            : 0;

        return _orientation == ScrollBarOrientation.Vertical
            ? new Rectangle(bounds.X + 2, bounds.Y + _buttonSize + thumbPos, bounds.Width - 4, thumbSize)
            : new Rectangle(bounds.X + _buttonSize + thumbPos, bounds.Y + 2, thumbSize, bounds.Height - 4);
    }

    private int GetTrackSize(Rectangle bounds)
    {
        return _orientation == ScrollBarOrientation.Vertical
            ? bounds.Height - _buttonSize * 2
            : bounds.Width - _buttonSize * 2;
    }

    private int GetThumbSize(Rectangle bounds)
    {
        if (_contentSize <= 0 || _viewSize <= 0)
            return MinThumbSize;

        int trackSize = GetTrackSize(bounds);
        float ratio = (float)_viewSize / _contentSize;
        int thumbSize = Math.Max(MinThumbSize, (int)(trackSize * ratio));

        // Ensure thumb doesn't exceed track
        return Math.Min(thumbSize, trackSize);
    }

    private void DrawButton(Graphics g, Rectangle rect, Theme theme, ButtonState state, bool isUp)
    {
        var bgColor = state switch
        {
            ButtonState.Pressed => Lighten(theme.ScrollbarThumb),
            ButtonState.Hover => Lighten(theme.ScrollbarTrack),
            _ => theme.ScrollbarTrack
        };

        g.FillRectangle(bgColor, rect.X, rect.Y, rect.Width, rect.Height);
        g.DrawRectangle(theme.ScrollbarBorder, rect.X, rect.Y, rect.Width, rect.Height, 1);

        // Draw arrow triangle
        DrawArrow(g, theme.ControlText, rect, isUp);
    }

    private void DrawArrow(Graphics g, Color color, Rectangle rect, bool isUp)
    {
        float cx = rect.X + rect.Width / 2f;
        float cy = rect.Y + rect.Height / 2f;
        float arrowSize = Math.Min(rect.Width, rect.Height) * 0.18f;

        float x1, y1, x2, y2, x3, y3;
        if (_orientation == ScrollBarOrientation.Vertical)
        {
            if (isUp)
            {
                x1 = cx; y1 = cy - arrowSize;
                x2 = cx - arrowSize; y2 = cy + arrowSize;
                x3 = cx + arrowSize; y3 = cy + arrowSize;
            }
            else
            {
                x1 = cx; y1 = cy + arrowSize;
                x2 = cx - arrowSize; y2 = cy - arrowSize;
                x3 = cx + arrowSize; y3 = cy - arrowSize;
            }
        }
        else
        {
            if (isUp)
            {
                x1 = cx - arrowSize; y1 = cy;
                x2 = cx + arrowSize; y2 = cy - arrowSize;
                x3 = cx + arrowSize; y3 = cy + arrowSize;
            }
            else
            {
                x1 = cx + arrowSize; y1 = cy;
                x2 = cx - arrowSize; y2 = cy - arrowSize;
                x3 = cx - arrowSize; y3 = cy + arrowSize;
            }
        }

        g.FillTriangle(color, x1, y1, x2, y2, x3, y3);
    }

    private int Clamp(int value)
    {
        int maxScroll = MaxScroll;
        if (value < _minimum) return _minimum;
        if (value > maxScroll) return maxScroll;
        return value;
    }

    private static Color Lighten(Color color)
    {
        int r = Math.Min(255, color.R + 30);
        int g = Math.Min(255, color.G + 30);
        int b = Math.Min(255, color.B + 30);
        return Color.FromArgb(r, g, b, color.A);
    }
}
