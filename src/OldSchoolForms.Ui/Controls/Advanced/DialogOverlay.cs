using OldSchoolForms.Ui.Core;
using OldSchoolForms.Ui.Theming;
using Graphics = OldSchoolForms.Ui.Rendering.Graphics;

namespace OldSchoolForms.Ui.Controls.Advanced;

/// <summary>
/// An overlay control that renders a modal form dialog within the owner form surface.
/// Used on Wayland where separate native GLFW windows cannot be focused or kept above
/// their owner by the application. The overlay covers the owner's client area, draws a
/// semi-transparent dimming background, and renders the dialog form's controls within a
/// framed window. All input events are intercepted and forwarded to the dialog form.
/// Supports resizing when the dialog form's FormBorderStyle is set to Sizable.
/// </summary>
internal class DialogOverlay : Control
{
    private readonly Form _owner;
    private readonly Form _dialogForm;
    private readonly bool _isSizable;
    private int _dialogWidth;
    private int _dialogHeight;
    private int _dialogX;
    private int _dialogY;
    private const int TitleBarHeight = 30;
    private const int ResizeMargin = 6;
    private const int MinDialogWidth = 200;        // min form width
    private const int MinDialogHeight = 180;        // min total visual height (content + title bar)
    private bool _closeButtonHovered;
    private bool _closeButtonPressed;

    // Resize state
    private enum ResizeEdge { None, Left, Right, Top, Bottom, TopLeft, TopRight, BottomLeft, BottomRight }
    private ResizeEdge _activeEdge;
    private ResizeEdge _dragEdge;
    private int _dragStartX;
    private int _dragStartY;
    private int _dragStartDialogX;
    private int _dragStartDialogY;
    private int _dragStartDialogW;
    private int _dragStartDialogH;

    // Drag-move state (title bar)
    private bool _isDragging;
    private int _dragMoveStartX;
    private int _dragMoveStartY;
    private int _dragMoveStartDialogX;
    private int _dragMoveStartDialogY;

    /// <summary>
    /// Gets the dialog result.
    /// </summary>
    public DialogResult DialogResult
    {
        get => _dialogForm.DialogResult;
        set
        {
            if (value != DialogResult.None)
                _dialogForm.DialogResult = value;
        }
    }

    /// <summary>
    /// The overlay draws with transparency so content behind it is still rendered.
    /// </summary>
    public override bool IsOpaque => false;

    /// <summary>
    /// Initializes a new DialogOverlay that hosts the specified dialog form on the owner form.
    /// All coordinates are in logical units (before zoom) — the Graphics object handles
    /// conversion to pixel space via its Zoom and offset.
    /// </summary>
    /// <param name="owner">The owner form.</param>
    /// <param name="dialogForm">The dialog form to render.</param>
    public DialogOverlay(Form owner, Form dialogForm)
    {
        _owner = owner;
        _dialogForm = dialogForm;
        _isSizable = dialogForm.FormBorderStyle == FormBorderStyle.Sizable;
        Bounds = new Rectangle(0, 0, owner.Width, owner.Height);
        Visible = true;
        Enabled = true;
        TabStop = true;

        _dialogWidth = dialogForm.Width;
        _dialogHeight = dialogForm.Height + TitleBarHeight;
        _dialogX = (owner.Width - _dialogWidth) / 2;
        _dialogY = (owner.Height - _dialogHeight) / 2;
    }

    /// <summary>
    /// Renders the dimming overlay, dialog frame, and dialog form controls.
    /// </summary>
    public override void Render(Graphics g)
    {
        if (!Visible) return;

        var theme = ThemeManager.CurrentTheme;

        g.FillRectangle(theme.MessageBoxOverlay, 0, 0, _owner.Width, _owner.Height);

        g.FillRectangle(theme.ControlBackground, _dialogX, _dialogY, _dialogWidth, _dialogHeight);
        g.DrawRectangle(theme.MessageBoxBorder, _dialogX, _dialogY, _dialogWidth, _dialogHeight, 2);

        // Render the form content BELOW the title bar.
        // Clip to prevent content from overflowing outside the dialog.
        g.Save();
        g.TranslateTransform(_dialogX, _dialogY + TitleBarHeight);
        g.SetClip(new Rectangle(0, 0, _dialogWidth, _dialogHeight - TitleBarHeight));
        _dialogForm.Render(g);
        g.ResetClip();
        g.Restore();

        g.FillRectangle(theme.ActiveCaption, _dialogX, _dialogY, _dialogWidth, TitleBarHeight);

        var titleFont = _dialogForm.EffectiveFont;
        g.DrawString(_dialogForm.Text ?? string.Empty, titleFont, theme.ActiveCaptionText, _dialogX + 10, _dialogY + 6);

        int closeBtnSize = TitleBarHeight;
        int closeBtnX = _dialogX + _dialogWidth - closeBtnSize;
        int closeBtnY = _dialogY;
        var closeColor = _closeButtonHovered
            ? (_closeButtonPressed ? theme.Highlight : theme.ControlDark)
            : theme.ActiveCaption;
        g.FillRectangle(closeColor, closeBtnX, closeBtnY, closeBtnSize, closeBtnSize);
        var closeTextColor = _closeButtonHovered ? theme.HighlightText : theme.ActiveCaptionText;
        if (g.MeasureText != null)
        {
            var (charW, charH) = g.MeasureText("\u00D7", titleFont, 1f);
            int charX = closeBtnX + (closeBtnSize - (int)charW) / 2;
            int charY = closeBtnY + (closeBtnSize - (int)charH) / 2;
            g.DrawString("\u00D7", titleFont, closeTextColor, charX, charY);
        }
        else
        {
            g.DrawString("\u00D7", titleFont, closeTextColor, closeBtnX + 6, closeBtnY + 4);
        }

        base.Render(g);
    }

    /// <summary>
    /// Forwards mouse-down to the dialog form or starts a resize operation.
    /// </summary>
    protected internal override void OnMouseDown(EventArgs e)
    {
        if (e is MouseEventArgs me)
        {
            if (IsCloseButtonHit(me.X, me.Y))
            {
                _closeButtonPressed = true;
                Invalidate();
                return;
            }

            bool inTitleBar = me.Y >= _dialogY && me.Y < _dialogY + TitleBarHeight;

            if (_isSizable)
            {
                _activeEdge = HitTestEdge(me.X, me.Y);
                if (_activeEdge != ResizeEdge.None)
                {
                    _dragEdge = _activeEdge;
                    _dragStartX = me.X;
                    _dragStartY = me.Y;
                    _dragStartDialogX = _dialogX;
                    _dragStartDialogY = _dialogY;
                    _dragStartDialogW = _dialogWidth;
                    _dragStartDialogH = _dialogHeight;
                    _dialogForm.SuspendLayout();
                    return;
                }
            }

            if (inTitleBar)
            {
                _isDragging = true;
                _dragMoveStartX = me.X;
                _dragMoveStartY = me.Y;
                _dragMoveStartDialogX = _dialogX;
                _dragMoveStartDialogY = _dialogY;
                return;
            }

            var dialogArgs = new MouseEventArgs(me.Button, me.Clicks, me.X - _dialogX, me.Y - _dialogY - TitleBarHeight, me.Delta);
            _dialogForm.OnMouseDown(dialogArgs);
        }
        base.OnMouseDown(e);
    }

    /// <summary>
    /// Forwards mouse-up to the dialog form or ends a resize operation.
    /// </summary>
    protected internal override void OnMouseUp(EventArgs e)
    {
        if (_closeButtonPressed && e is MouseEventArgs me)
        {
            _closeButtonPressed = false;
            if (IsCloseButtonHit(me.X, me.Y))
            {
                _dialogForm.DialogResult = DialogResult.Cancel;
                _dialogForm.Close();
                return;
            }
            Invalidate();
        }

        if (_isDragging)
        {
            _isDragging = false;
            _owner.Cursor = null;
            return;
        }

        if (_dragEdge != ResizeEdge.None)
        {
            _dialogForm.ResumeLayout(true);
            _dragEdge = ResizeEdge.None;
            _owner.Cursor = null;
            return;
        }

        if (e is MouseEventArgs me2)
        {
            var dialogArgs = new MouseEventArgs(me2.Button, me2.Clicks, me2.X - _dialogX, me2.Y - _dialogY - TitleBarHeight, me2.Delta);
            _dialogForm.OnMouseUp(dialogArgs);
        }
        base.OnMouseUp(e);
    }

    /// <summary>
    /// Updates cursor for resize edges, tracks close button hover, and handles resize drag.
    /// </summary>
    protected internal override void OnMouseMove(EventArgs e)
    {
        if (e is MouseEventArgs me)
        {
            bool wasHovered = _closeButtonHovered;
            _closeButtonHovered = IsCloseButtonHit(me.X, me.Y);
            if (wasHovered != _closeButtonHovered)
                Invalidate();

            if (_isDragging)
            {
                _dialogX = _dragMoveStartDialogX + (me.X - _dragMoveStartX);
                _dialogY = _dragMoveStartDialogY + (me.Y - _dragMoveStartY);
                Invalidate();
                return;
            }

            if (_dragEdge != ResizeEdge.None)
            {
                ProcessResizeDrag(me.X, me.Y);
                return;
            }

            if (_isSizable)
            {
                _activeEdge = HitTestEdge(me.X, me.Y);
                _owner.Cursor = _activeEdge switch
                {
                    ResizeEdge.Left or ResizeEdge.Right => SystemCursorType.SizeWE,
                    ResizeEdge.Top or ResizeEdge.Bottom => SystemCursorType.SizeNS,
                    ResizeEdge.TopLeft or ResizeEdge.BottomRight => SystemCursorType.SizeAll,
                    ResizeEdge.TopRight or ResizeEdge.BottomLeft => SystemCursorType.SizeAll,
                    _ => null
                };
            }

            var dialogArgs = new MouseEventArgs(me.Button, me.Clicks, me.X - _dialogX, me.Y - _dialogY - TitleBarHeight, me.Delta);
            _dialogForm.OnMouseMove(dialogArgs);
        }
        base.OnMouseMove(e);
    }

    /// <summary>
    /// Forwards mouse-wheel events to the dialog form.
    /// </summary>
    protected internal override void OnMouseWheel(EventArgs e)
    {
        if (e is MouseEventArgs me)
        {
            var dialogArgs = new MouseEventArgs(me.Button, me.Clicks, me.X - _dialogX, me.Y - _dialogY - TitleBarHeight, me.Delta);
            _dialogForm.OnMouseWheel(dialogArgs);
        }
        base.OnMouseWheel(e);
    }

    /// <summary>
    /// Forwards key-down events to the dialog form. Handles Escape and Alt+F4 to close.
    /// </summary>
    protected internal override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Core.Keys.Escape ||
            (e.KeyCode == Core.Keys.F4 && e.Modifiers.HasFlag(ModifierKeys.Alt)))
        {
            _dialogForm.DialogResult = DialogResult.Cancel;
            _dialogForm.Close();
            e.Handled = true;
            return;
        }
        _dialogForm.OnKeyDown(e);
        base.OnKeyDown(e);
    }

    protected internal override void OnKeyUp(KeyEventArgs e)
    {
        _dialogForm.OnKeyUp(e);
        base.OnKeyUp(e);
    }

    protected internal override void OnTextInput(string text)
    {
        _dialogForm.OnTextInput(text);
        base.OnTextInput(text);
    }

    protected internal override void OnGotFocus(EventArgs e)
    {
        _dialogForm.Focused = true;
        _dialogForm.OnGotFocus(e);
        base.OnGotFocus(e);
    }

    protected internal override void OnLostFocus(EventArgs e)
    {
        _dialogForm.OnLostFocus(e);
        base.OnLostFocus(e);
    }

    private bool IsCloseButtonHit(int x, int y)
    {
        int closeBtnSize = TitleBarHeight;
        int closeBtnX = _dialogX + _dialogWidth - closeBtnSize;
        int closeBtnY = _dialogY;
        return x >= closeBtnX && x < closeBtnX + closeBtnSize &&
               y >= closeBtnY && y < closeBtnY + closeBtnSize;
    }

    private ResizeEdge HitTestEdge(int x, int y)
    {
        bool onLeft = x >= _dialogX - ResizeMargin && x <= _dialogX + ResizeMargin;
        bool onRight = x >= _dialogX + _dialogWidth - ResizeMargin && x <= _dialogX + _dialogWidth;
        bool onTop = y >= _dialogY && y <= _dialogY + ResizeMargin;
        bool onBottom = y >= _dialogY + _dialogHeight - ResizeMargin && y <= _dialogY + _dialogHeight;

        if (onTop && onLeft) return ResizeEdge.TopLeft;
        if (onTop && onRight) return ResizeEdge.TopRight;
        if (onBottom && onLeft) return ResizeEdge.BottomLeft;
        if (onBottom && onRight) return ResizeEdge.BottomRight;
        if (onLeft) return ResizeEdge.Left;
        if (onRight) return ResizeEdge.Right;
        if (onTop) return ResizeEdge.Top;
        if (onBottom) return ResizeEdge.Bottom;
        return ResizeEdge.None;
    }

    private void ProcessResizeDrag(int currentX, int currentY)
    {
        int dx = currentX - _dragStartX;
        int dy = currentY - _dragStartY;
        int newX = _dragStartDialogX;
        int newY = _dragStartDialogY;
        int newW = _dragStartDialogW;
        int newH = _dragStartDialogH;

        switch (_dragEdge)
        {
            case ResizeEdge.Left:
                newX = _dragStartDialogX + dx;
                newW = _dragStartDialogW - dx;
                break;
            case ResizeEdge.Right:
                newW = _dragStartDialogW + dx;
                break;
            case ResizeEdge.Top:
                newY = _dragStartDialogY + dy;
                newH = _dragStartDialogH - dy;
                break;
            case ResizeEdge.Bottom:
                newH = _dragStartDialogH + dy;
                break;
            case ResizeEdge.TopLeft:
                newX = _dragStartDialogX + dx;
                newY = _dragStartDialogY + dy;
                newW = _dragStartDialogW - dx;
                newH = _dragStartDialogH - dy;
                break;
            case ResizeEdge.TopRight:
                newY = _dragStartDialogY + dy;
                newW = _dragStartDialogW + dx;
                newH = _dragStartDialogH - dy;
                break;
            case ResizeEdge.BottomLeft:
                newX = _dragStartDialogX + dx;
                newW = _dragStartDialogW - dx;
                newH = _dragStartDialogH + dy;
                break;
            case ResizeEdge.BottomRight:
                newW = _dragStartDialogW + dx;
                newH = _dragStartDialogH + dy;
                break;
        }

        if (newW < MinDialogWidth)
        {
            if (_dragEdge is ResizeEdge.Left or ResizeEdge.BottomLeft or ResizeEdge.TopLeft)
                newX = _dragStartDialogX + _dragStartDialogW - MinDialogWidth;
            newW = MinDialogWidth;
        }
        if (newH < MinDialogHeight)
        {
            if (_dragEdge is ResizeEdge.Top or ResizeEdge.TopLeft or ResizeEdge.TopRight)
                newY = _dragStartDialogY + _dragStartDialogH - MinDialogHeight;
            newH = MinDialogHeight;
        }

        _dialogX = newX;
        _dialogY = newY;
        _dialogWidth = newW;
        _dialogHeight = newH;

        _dialogForm.Width = newW;
        _dialogForm.Height = newH - TitleBarHeight;

        Invalidate();
    }
}
