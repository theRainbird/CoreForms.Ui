using CoreForms.Ui.Core;
using CoreForms.Ui.Theming;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Advanced;

/// <summary>
/// An overlay control that renders a modal form dialog within the owner form surface.
/// Used on Wayland where separate native GLFW windows cannot be focused or kept above
/// their owner by the application. The overlay covers the owner's client area, draws a
/// semi-transparent dimming background, and renders the dialog form's controls within a
/// framed window. All input events are intercepted and forwarded to the dialog form.
/// </summary>
internal class DialogOverlay : Control
{
    private readonly Form _owner;
    private readonly Form _dialogForm;
    private readonly int _dialogWidth;
    private readonly int _dialogHeight;
    private readonly int _dialogX;
    private readonly int _dialogY;
    private const int TitleBarHeight = 30;
    private bool _closeButtonHovered;
    private bool _closeButtonPressed;

    /// <summary>
    /// Gets the dialog result. When set to a value other than None, the overlay closes.
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
        Bounds = new Rectangle(0, 0, owner.Width, owner.Height);
        Visible = true;
        Enabled = true;
        TabStop = true;

        _dialogWidth = dialogForm.Width;
        _dialogHeight = dialogForm.Height;
        _dialogX = (owner.Width - _dialogWidth) / 2;
        _dialogY = (owner.Height - _dialogHeight) / 2;
    }

    /// <summary>
    /// Renders the dimming overlay, dialog frame, and the dialog form's controls.
    /// Uses Save/Restore to temporarily translate the Graphics origin so the dialog
    /// form renders at the correct position with the correct zoom.
    /// </summary>
    /// <param name="g">The Graphics object.</param>
    public override void Render(Graphics g)
    {
        if (!Visible) return;

        var theme = ThemeManager.CurrentTheme;

        // Dimming overlay over the entire owner area
        g.FillRectangle(theme.MessageBoxOverlay, 0, 0, _owner.Width, _owner.Height);

        // Dialog background and border
        g.FillRectangle(theme.ControlBackground, _dialogX, _dialogY, _dialogWidth, _dialogHeight);
        g.DrawRectangle(theme.MessageBoxBorder, _dialogX, _dialogY, _dialogWidth, _dialogHeight, 2);

        // Render the dialog form's controls within a translated context.
        // Save/Restore ensures the translation does not affect subsequent rendering.
        g.Save();
        g.TranslateTransform(_dialogX, _dialogY);
        _dialogForm.Render(g);
        g.Restore();

        // Title bar and close button must be drawn AFTER _dialogForm.Render(g)
        // because Form.Render fills the entire dialog area with BackColor,
        // which would overwrite the title bar if drawn beforehand.
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
    /// Forwards mouse-down events to the dialog form or handles close button clicks.
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

            var dialogArgs = new MouseEventArgs(me.Button, me.Clicks, me.X - _dialogX, me.Y - _dialogY, me.Delta);
            _dialogForm.OnMouseDown(dialogArgs);
        }
        base.OnMouseDown(e);
    }

    /// <summary>
    /// Forwards mouse-up events to the dialog form or handles close button clicks.
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

        if (e is MouseEventArgs me2)
        {
            var dialogArgs = new MouseEventArgs(me2.Button, me2.Clicks, me2.X - _dialogX, me2.Y - _dialogY, me2.Delta);
            _dialogForm.OnMouseUp(dialogArgs);
        }
        base.OnMouseUp(e);
    }

    /// <summary>
    /// Forwards mouse-move events to the dialog form and tracks close button hover.
    /// </summary>
    protected internal override void OnMouseMove(EventArgs e)
    {
        if (e is MouseEventArgs me)
        {
            bool wasHovered = _closeButtonHovered;
            _closeButtonHovered = IsCloseButtonHit(me.X, me.Y);
            if (wasHovered != _closeButtonHovered)
                Invalidate();

            var dialogArgs = new MouseEventArgs(me.Button, me.Clicks, me.X - _dialogX, me.Y - _dialogY, me.Delta);
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
            var dialogArgs = new MouseEventArgs(me.Button, me.Clicks, me.X - _dialogX, me.Y - _dialogY, me.Delta);
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

    /// <summary>
    /// Forwards key-up events to the dialog form.
    /// </summary>
    protected internal override void OnKeyUp(KeyEventArgs e)
    {
        _dialogForm.OnKeyUp(e);
        base.OnKeyUp(e);
    }

    /// <summary>
    /// Forwards text input to the dialog form.
    /// </summary>
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
        int closeBtnSize = TitleBarHeight - 4;
        int closeBtnX = _dialogX + _dialogWidth - closeBtnSize - 2;
        int closeBtnY = _dialogY + 2;
        return x >= closeBtnX && x < closeBtnX + closeBtnSize &&
               y >= closeBtnY && y < closeBtnY + closeBtnSize;
    }
}
