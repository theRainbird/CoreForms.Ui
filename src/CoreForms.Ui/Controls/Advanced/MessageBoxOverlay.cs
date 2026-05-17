using CoreForms.Ui.Core;
using CoreForms.Ui.Controls.Basic;
using CoreForms.Ui.Theming;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Advanced;

/// <summary>
/// Represents an overlay control that renders a modal MessageBox within the owner form.
/// This provides true modality on Linux by rendering as an in-window overlay rather than a separate window.
/// </summary>
internal class MessageBoxOverlay : Control
{
    private readonly Form _owner;
    private readonly string _messageText;
    private readonly string _caption;
    private readonly MessageBoxButtons _buttons;
    private readonly MessageBoxIcon _icon;
    private readonly MessageBoxDefaultButton _defaultButton;
    private DialogResult _dialogResult = DialogResult.None;
    private IGraphicsImage? _iconImage;
    private readonly int _iconSize = 48;
    private readonly List<Button> _dialogButtons = new();
    private int _dialogWidth;
    private int _dialogHeight;
    private int _dialogX;
    private int _dialogY;
    private bool _isInitialized;
    private int _focusedButtonIndex;
    private int _hoveredButtonIndex = -1;

    /// <summary>
    /// Gets the result of the message box dialog.
    /// </summary>
    public DialogResult DialogResult => _dialogResult;

    /// <summary>
    /// Initializes a new MessageBoxOverlay with the specified parameters.
    /// </summary>
    /// <param name="owner">The owner form that will display this overlay.</param>
    /// <param name="text">The message text to display.</param>
    /// <param name="caption">The title bar caption.</param>
    /// <param name="buttons">The buttons to display.</param>
    /// <param name="icon">The icon to display.</param>
    /// <param name="defaultButton">The default button.</param>
    public MessageBoxOverlay(Form owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton)
    {
        _owner = owner;
        _messageText = text;
        _caption = caption;
        _buttons = buttons;
        _icon = icon;
        _defaultButton = defaultButton;

        Bounds = new Rectangle(0, 0, owner.Width, owner.Height);
        Visible = true;
        Enabled = true;
        TabStop = true;
    }

    /// <summary>
    /// Initializes the dialog dimensions and buttons after the overlay is added to the owner.
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized) return;
        _isInitialized = true;

        _owner.PerformLayout();
        _owner.ActiveControl = this;

        int padding = 20;
        int iconAreaWidth = _icon != MessageBoxIcon.None ? _iconSize + padding : 0;

        int textWidth = EstimateTextWidth(_messageText);
        int textHeight = EstimateTextHeight(_messageText);
        int contentWidth = iconAreaWidth + textWidth;
        int buttonRowHeight = 50;

        var buttonDefs = GetButtonDefinitions(_buttons);
        int buttonWidth = 100;
        int gap = 10;
        int totalButtonsWidth = buttonDefs.Count * buttonWidth + (buttonDefs.Count - 1) * gap;

        int totalWidth = Math.Max(contentWidth + padding * 2, totalButtonsWidth + padding * 2);
        int contentPadding = 30;
        int totalHeight = contentPadding + textHeight + padding + buttonRowHeight + padding;

        _dialogWidth = Math.Max(totalWidth, 250);
        _dialogHeight = Math.Max(totalHeight, 150);

        _dialogX = (_owner.Width - _dialogWidth) / 2;
        _dialogY = (_owner.Height - _dialogHeight) / 2;

        CreateButtons(padding, buttonRowHeight);

        LoadIconTexture();
    }

    private void LoadIconTexture()
    {
        if (_icon == MessageBoxIcon.None || _owner.WindowId == 0)
            return;

        var ctx = Platform.Platform.GetWindowContext(_owner.WindowId);
        if (ctx != null)
        {
            float zoom = EffectiveZoom;
            int iconPixelSize = (int)(_iconSize * zoom);
            _iconImage = Platform.Platform.LoadMessageBoxIcon(_icon, ctx.WindowId, iconPixelSize);
        }
    }

    /// <summary>
    /// Renders the overlay background (semi-transparent dimming) and the dialog box.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
    public override void Render(Graphics g)
    {
        if (!Visible) return;

        if (Bounds.Width != _owner.Width || Bounds.Height != _owner.Height)
        {
            Bounds = new Rectangle(0, 0, _owner.Width, _owner.Height);
        }

        Initialize();

        var theme = ThemeManager.CurrentTheme;

        g.FillRectangle(theme.MessageBoxOverlay, 0, 0, _owner.Width, _owner.Height);

        g.FillRectangle(theme.ControlBackground, _dialogX, _dialogY, _dialogWidth, _dialogHeight);
        g.DrawRectangle(theme.MessageBoxBorder, _dialogX, _dialogY, _dialogWidth, _dialogHeight, 2);

        int titleBarHeight = 30;
        g.FillRectangle(theme.ActiveCaption, _dialogX, _dialogY, _dialogWidth, titleBarHeight);

        var titleFont = EffectiveFont;
        g.DrawString(_caption, titleFont, theme.ActiveCaptionText, _dialogX + 10, _dialogY + 6);

        RenderDialogContent(g);

        base.Render(g);
    }

    private void RenderDialogContent(Graphics g)
    {
        int padding = 20;
        int titleBarHeight = 30;
        int contentPadding = 30;
        int contentY = _dialogY + titleBarHeight + contentPadding;
        int iconAreaWidth = _icon != MessageBoxIcon.None ? _iconSize + padding : 0;
        int buttonRowHeight = 50;
        int textHeight = EstimateTextHeight(_messageText);

        int contentAreaHeight = _dialogHeight - titleBarHeight - buttonRowHeight - padding - contentPadding;
        int iconY = contentY + (contentAreaHeight - _iconSize) / 2;

        if (_icon != MessageBoxIcon.None && _iconImage != null)
        {
            int iconX = _dialogX + padding;
            g.DrawImage(_iconImage, iconX, iconY, _iconSize, _iconSize);
        }

        var font = EffectiveFont;
        int textX = _dialogX + padding + iconAreaWidth;
        int textY = contentY + (contentAreaHeight - textHeight) / 2;

        string[] lines = _messageText.Split('\n');
        foreach (string line in lines)
        {
            g.DrawString(line, font, ForeColor, textX, textY);
            textY += CoordinateTransform.GetItemHeight(font, EffectiveZoom);
        }

        for (int i = 0; i < _dialogButtons.Count; i++)
        {
            var btn = _dialogButtons[i];
            if (btn.Visible)
            {
                bool isFocused = (i == _focusedButtonIndex);
                bool isHovered = (i == _hoveredButtonIndex);
                RenderButton(g, btn, isFocused, isHovered);
            }
        }
    }

    private void RenderButton(Graphics g, Button btn, bool isFocused, bool isHovered)
    {
        var theme = ThemeManager.CurrentTheme;
        Color backColor = btn.BackColor;
        if (isHovered)
        {
            backColor = theme.ControlLight;
        }

        g.FillRectangle(backColor, btn.X, btn.Y, btn.Width, btn.Height);
        g.DrawRectangle(theme.ControlDark, btn.X, btn.Y, btn.Width, btn.Height, 1);

        if (isFocused)
        {
            g.DrawRectangle(theme.Highlight, btn.X + 1, btn.Y + 1, btn.Width - 2, btn.Height - 2, 2);
        }

        var font = btn.Font ?? EffectiveFont;
        var textColor = btn.Enabled ? btn.ForeColor : theme.GrayText;
        float zoom = EffectiveZoom;
        var textSize = Platform.Platform.MeasureText(btn.Text, font, zoom);
        var textX = btn.X + (btn.Width - textSize.width / zoom) / 2;
        var textY = btn.Y + (btn.Height - textSize.height / zoom) / 2;

        g.DrawString(btn.Text, font, textColor, textX, textY);
    }

    /// <summary>
    /// Renders overlay elements for child controls.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
    public override void RenderOverlay(Graphics g)
    {
        if (!Visible) return;
        base.RenderOverlay(g);
    }

    private void CreateButtons(int padding, int buttonRowHeight)
    {
        _dialogButtons.Clear();

        var buttonDefs = GetButtonDefinitions(_buttons);
        int buttonWidth = 100;
        int buttonHeight = 36;
        int gap = 10;
        int totalButtonsWidth = buttonDefs.Count * buttonWidth + (buttonDefs.Count - 1) * gap;
        int startX = (_dialogWidth - totalButtonsWidth) / 2;
        int buttonY = _dialogY + _dialogHeight - padding - buttonHeight;

        int buttonTextIndex = 0;
        foreach (var (text, result) in buttonDefs)
        {
            var btn = new Button
            {
                Text = text,
                Width = buttonWidth,
                Height = buttonHeight,
                X = _dialogX + startX + buttonTextIndex * (buttonWidth + gap),
                Y = buttonY,
                TabStop = true,
                TabIndex = buttonTextIndex
            };

            var localResult = result;
            btn.Click += (s, e) =>
            {
                _dialogResult = localResult;
                Close();
            };

            _dialogButtons.Add(btn);
            buttonTextIndex++;
        }

        SetDefaultButton(buttonDefs);
    }

    private void SetDefaultButton(List<(string text, DialogResult result)> buttonDefs)
    {
        if (_dialogButtons.Count == 0) return;

        int defaultIndex = _defaultButton switch
        {
            MessageBoxDefaultButton.Button2 => Math.Min(1, _dialogButtons.Count - 1),
            MessageBoxDefaultButton.Button3 => Math.Min(2, _dialogButtons.Count - 1),
            _ => 0
        };

        _focusedButtonIndex = defaultIndex;
    }

    /// <summary>
    /// Handles mouse down events, routing to dialog buttons if clicked.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseDown(EventArgs e)
    {
        var args = e as MouseEventArgs;
        if (args == null) return;

        for (int i = 0; i < _dialogButtons.Count; i++)
        {
            var btn = _dialogButtons[i];
            if (btn.Visible && btn.Bounds.Contains(args.X, args.Y))
            {
                var localArgs = new MouseEventArgs(args.Button, args.Clicks, args.X - btn.X, args.Y - btn.Y, args.Delta);
                btn.OnMouseDown(localArgs);
                _focusedButtonIndex = i;
                return;
            }
        }
    }

    /// <summary>
    /// Handles mouse up events, routing to dialog buttons if released.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseUp(EventArgs e)
    {
        var args = e as MouseEventArgs;
        if (args == null) return;

        foreach (var btn in _dialogButtons)
        {
            if (btn.Visible && btn.Bounds.Contains(args.X, args.Y))
            {
                var localArgs = new MouseEventArgs(args.Button, args.Clicks, args.X - btn.X, args.Y - btn.Y, args.Delta);
                btn.OnMouseUp(localArgs);
                return;
            }
        }
    }

    /// <summary>
    /// Handles mouse move events, routing to dialog buttons if hovering.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseMove(EventArgs e)
    {
        var args = e as MouseEventArgs;
        if (args == null) return;

        int newHoveredIndex = -1;
        for (int i = 0; i < _dialogButtons.Count; i++)
        {
            var btn = _dialogButtons[i];
            if (btn.Visible && btn.Bounds.Contains(args.X, args.Y))
            {
                var localArgs = new MouseEventArgs(args.Button, args.Clicks, args.X - btn.X, args.Y - btn.Y, args.Delta);
                btn.OnMouseMove(localArgs);
                newHoveredIndex = i;
                break;
            }
        }

        if (newHoveredIndex != _hoveredButtonIndex)
        {
            _hoveredButtonIndex = newHoveredIndex;
        }
    }

    /// <summary>
    /// Handles key down events, processing Enter, Escape, Tab and Arrow keys.
    /// </summary>
    /// <param name="e">A KeyEventArgs that contains the event data.</param>
    protected internal override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Core.Keys.Enter)
        {
            var btn = GetDefaultButton();
            btn?.PerformClick();
            e.Handled = true;
            return;
        }

        if (e.KeyCode == Core.Keys.Escape)
        {
            var cancelBtn = GetCancelButton();
            if (cancelBtn != null)
            {
                cancelBtn.PerformClick();
            }
            e.Handled = true;
            return;
        }

        if (e.KeyCode == Core.Keys.Tab)
        {
            if (e.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                if (_focusedButtonIndex > 0)
                    _focusedButtonIndex--;
                else
                    _focusedButtonIndex = _dialogButtons.Count - 1;
            }
            else
            {
                if (_focusedButtonIndex < _dialogButtons.Count - 1)
                    _focusedButtonIndex++;
                else
                    _focusedButtonIndex = 0;
            }
            e.Handled = true;
            return;
        }

        if (e.KeyCode == Core.Keys.Left)
        {
            if (_focusedButtonIndex > 0)
            {
                _focusedButtonIndex--;
            }
            else
            {
                _focusedButtonIndex = _dialogButtons.Count - 1;
            }
            e.Handled = true;
            return;
        }

        if (e.KeyCode == Core.Keys.Right)
        {
            if (_focusedButtonIndex < _dialogButtons.Count - 1)
            {
                _focusedButtonIndex++;
            }
            else
            {
                _focusedButtonIndex = 0;
            }
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    private Button? GetDefaultButton()
    {
        if (_dialogButtons.Count == 0) return null;

        int defaultIndex = _defaultButton switch
        {
            MessageBoxDefaultButton.Button2 => Math.Min(1, _dialogButtons.Count - 1),
            MessageBoxDefaultButton.Button3 => Math.Min(2, _dialogButtons.Count - 1),
            _ => 0
        };

        return _dialogButtons[defaultIndex];
    }

    private Button? GetCancelButton()
    {
        foreach (var btn in _dialogButtons)
        {
            if (btn.Text == SR.GetString("Cancel"))
                return btn;
        }
        return _dialogButtons.Count > 0 ? _dialogButtons[^1] : null;
    }

    /// <summary>
    /// Closes the message box overlay and removes it from the owner form.
    /// </summary>
    public void Close()
    {
        _owner.Controls.Remove(this);
        _owner.PerformLayout();
    }

    private static List<(string text, DialogResult result)> GetButtonDefinitions(MessageBoxButtons buttons)
    {
        return buttons switch
        {
            MessageBoxButtons.OK => new List<(string, DialogResult)>
            {
                (SR.GetString("OK"), DialogResult.OK)
            },
            MessageBoxButtons.OKCancel => new List<(string, DialogResult)>
            {
                (SR.GetString("OK"), DialogResult.OK),
                (SR.GetString("Cancel"), DialogResult.Cancel)
            },
            MessageBoxButtons.YesNo => new List<(string, DialogResult)>
            {
                (SR.GetString("Yes"), DialogResult.Yes),
                (SR.GetString("No"), DialogResult.No)
            },
            MessageBoxButtons.YesNoCancel => new List<(string, DialogResult)>
            {
                (SR.GetString("Yes"), DialogResult.Yes),
                (SR.GetString("No"), DialogResult.No),
                (SR.GetString("Cancel"), DialogResult.Cancel)
            },
            MessageBoxButtons.RetryCancel => new List<(string, DialogResult)>
            {
                (SR.GetString("Retry"), DialogResult.Retry),
                (SR.GetString("Cancel"), DialogResult.Cancel)
            },
            MessageBoxButtons.AbortRetryIgnore => new List<(string, DialogResult)>
            {
                (SR.GetString("Abort"), DialogResult.Abort),
                (SR.GetString("Retry"), DialogResult.Retry),
                (SR.GetString("Ignore"), DialogResult.Ignore)
            },
            _ => new List<(string, DialogResult)>
            {
                (SR.GetString("OK"), DialogResult.OK)
            }
        };
    }

    private static int EstimateTextWidth(string text)
    {
        int maxWidth = 0;
        string[] lines = text.Split('\n');
        foreach (string line in lines)
        {
            int w = line.Length * 8;
            if (w > maxWidth) maxWidth = w;
        }
        return Math.Min(maxWidth, 500);
    }

    private static int EstimateTextHeight(string text)
    {
        string[] lines = text.Split('\n');
        return lines.Length * 22;
    }
}
