using CoreForms.Ui.Controls;
using Graphics = CoreForms.Ui.Rendering.Graphics;
using CoreForms.Ui.Core;

namespace CoreForms.Ui.Demo.UserControls;

/// <summary>
/// Demonstrates the use of a custom reusable UserControl (LoginUserControl).
/// </summary>
public class UserControlDemoPage : UserControl
{
    /// <summary>
    /// Occurs when the status text should be updated.
    /// </summary>
    public event EventHandler<StatusTextChangedEventArgs>? StatusTextChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserControlDemoPage"/> class.
    /// </summary>
    public UserControlDemoPage()
    {
        var loginControl = new LoginUserControl
        {
            Location = new Point(10, 10),
            Size = new Size(350, 200)
        };
        loginControl.LoginClicked += (s, e) =>
        {
            OnStatusTextChanged(string.Format(SR.GetString("StatusLoginFormat"), loginControl.Username));
        };

        Controls.Add(loginControl);
    }

    private void OnStatusTextChanged(string text)
    {
        StatusTextChanged?.Invoke(this, new StatusTextChangedEventArgs(text));
    }
}
