using CoreForms.Ui.Core;
using CoreForms.Ui.Controls.Basic;
using SkiaSharp;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Advanced;

/// <summary>
/// Represents the visual form used by MessageBox to display a message dialog.
/// This form is created and shown modally by the MessageBox static class.
/// </summary>
internal class MessageBoxForm : Form
{
    private readonly string _messageText;
    private readonly MessageBoxButtons _buttons;
    private readonly MessageBoxIcon _icon;
    private readonly MessageBoxDefaultButton _defaultButton;
    private DialogResult _dialogResult = DialogResult.None;
    private SKImage? _iconImage;
    private int _iconSize = 48;

    /// <summary>
    /// Gets the result of the message box dialog.
    /// </summary>
    public DialogResult DialogResult => _dialogResult;

    /// <summary>
    /// Initializes a new MessageBoxForm with the specified message, caption, buttons, icon, and default button.
    /// </summary>
    /// <param name="text">The message text to display.</param>
    /// <param name="caption">The title bar caption.</param>
    /// <param name="buttons">The buttons to display.</param>
    /// <param name="icon">The icon to display.</param>
    /// <param name="defaultButton">The default button.</param>
    public MessageBoxForm(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton)
    {
        _messageText = text;
        _buttons = buttons;
        _icon = icon;
        _defaultButton = defaultButton;
        _iconImage = null;

        Title = caption;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        BackColor = SystemColors.Control;

        int padding = 20;
        int iconAreaWidth = _icon != MessageBoxIcon.None ? _iconSize + padding : 0;

        int textWidth = EstimateTextWidth(text);
        int textHeight = EstimateTextHeight(text);
        int contentWidth = iconAreaWidth + textWidth;
        int buttonRowHeight = 50;
        int totalWidth = Math.Max(contentWidth + padding * 2, 300);
        int totalHeight = padding + textHeight + padding + buttonRowHeight + padding;

        Width = Math.Max(totalWidth, 250);
        Height = Math.Max(totalHeight, 150);

        CreateButtons(padding, buttonRowHeight);
    }

    /// <summary>
    /// Loads the icon texture after the window has been created.
    /// </summary>
    public void LoadIconTexture()
    {
        if (_icon == MessageBoxIcon.None || Handle == IntPtr.Zero)
            return;

        var ctx = Platform.Platform.GetWindowContext(WindowId);
        if (ctx != null)
        {
            _iconImage = Platform.Platform.LoadMessageBoxIcon(_icon, ctx.WindowId);
        }
    }

    /// <summary>
    /// Renders the message box form including the icon, text, and background.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
    public override void Render(Graphics g)
    {
        if (!Visible) return;

        g.FillRectangle(BackColor, 0, 0, Width, Height);

        int padding = 20;
        int iconAreaWidth = _icon != MessageBoxIcon.None ? _iconSize + padding : 0;

        if (_icon != MessageBoxIcon.None && _iconImage != null)
        {
            int iconY = padding + 4;
            g.DrawImage(_iconImage, padding, iconY, _iconSize, _iconSize);
        }

        var font = Font ?? Font.Default;
        int textX = padding + iconAreaWidth;
        int textY = padding;

        string[] lines = _messageText.Split('\n');
        foreach (string line in lines)
        {
            g.DrawString(line, font, ForeColor, textX, textY);
            textY += (int)font.Size + 4;
        }

        base.Render(g);
    }

    private void CreateButtons(int padding, int buttonRowHeight)
    {
        var buttonDefs = GetButtonDefinitions(_buttons);
        int buttonWidth = 100;
        int buttonHeight = 36;
        int gap = 10;
        int totalButtonsWidth = buttonDefs.Count * buttonWidth + (buttonDefs.Count - 1) * gap;
        int startX = Math.Max(padding, (Width - totalButtonsWidth) / 2);
        int buttonY = Height - padding - buttonHeight;

        int buttonTextIndex = 0;
        foreach (var (text, result) in buttonDefs)
        {
            var btn = new Button
            {
                Text = text,
                Width = buttonWidth,
                Height = buttonHeight,
                X = startX + buttonTextIndex * (buttonWidth + gap),
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

            Controls.Add(btn);
            buttonTextIndex++;
        }

        SetDefaultButton(buttonDefs);
    }

    private void SetDefaultButton(List<(string text, DialogResult result)> buttonDefs)
    {
        if (Controls.Count == 0) return;

        int defaultIndex = _defaultButton switch
        {
            MessageBoxDefaultButton.Button2 => Math.Min(1, Controls.Count - 1),
            MessageBoxDefaultButton.Button3 => Math.Min(2, Controls.Count - 1),
            _ => 0
        };

        ActiveControl = Controls[defaultIndex];
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