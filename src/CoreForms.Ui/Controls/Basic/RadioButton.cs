using CoreForms.Ui.Core;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Basic;

/// <summary>
/// A control that allows selecting one option from a group of options.
/// </summary>
public class RadioButton : Control
{
    private bool _checked;

    /// <summary>
    /// Initializes a new instance of RadioButton.
    /// </summary>
    public RadioButton()
    {
        Size = new Size(200, 28);
        TabStop = true;
    }

    /// <summary>
    /// Gets or sets whether the radio button is checked.
    /// </summary>
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

    /// <summary>
    /// Renders the radio button with its circle and text.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
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
        g.DrawString(Text, font, ForeColor, 20, CoordinateTransform.CenterVertically(Height, font, EffectiveZoom));

        base.Render(g);
    }

    /// <summary>
    /// Raises the Click event and sets the checked state.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected override void OnClick(EventArgs e)
    {
        Checked = true;
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
            Checked = true;
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