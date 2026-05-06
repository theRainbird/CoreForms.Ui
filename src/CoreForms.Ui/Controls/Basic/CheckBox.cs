using CoreForms.Ui.Core;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Basic;

/// <summary>
/// A control that can be checked or unchecked.
/// </summary>
public class CheckBox : Control
{
    private bool _checked;

    /// <summary>
    /// Initializes a new instance of CheckBox.
    /// </summary>
    public CheckBox()
    {
        Size = new Size(200, 28);
        TabStop = true;
    }

    /// <summary>
    /// Gets or sets whether the check box is checked.
    /// </summary>
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

    /// <summary>
    /// Renders the check box with its checkbox and text.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
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

    /// <summary>
    /// Raises the Click event and toggles the checked state.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected override void OnClick(EventArgs e)
    {
        Checked = !Checked;
        base.OnClick(e);
    }

    /// <summary>
    /// Raises the KeyDown event to handle Space key.
    /// </summary>
    /// <param name="e">A KeyEventArgs that contains the event data.</param>
    protected internal override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Space)
        {
            Checked = !Checked;
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }

    /// <summary>
    /// Raises the CheckedChanged event.
    /// </summary>
    protected virtual void OnCheckedChanged()
    {
        CheckedChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Occurs when the checked state changes.
    /// </summary>
    public event EventHandler? CheckedChanged;
}