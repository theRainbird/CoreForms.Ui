using OldSchoolForms.Ui.Controls.Basic;
using OldSchoolForms.Ui.Theming;
using OldSchoolForms.Ui.Controls;
using Graphics = OldSchoolForms.Ui.Rendering.Graphics;
using OldSchoolForms.Ui.Core;

namespace OldSchoolForms.Ui.Demo.UserControls;

/// <summary>
/// A login user control with username, password, remember-me checkbox and login button.
/// </summary>
public class LoginUserControl : UserControl
{
    private Label _usernameLabel = null!;
    private TextBox _usernameTextBox = null!;
    private Label _passwordLabel = null!;
    private TextBox _passwordTextBox = null!;
    private CheckBox _rememberCheckBox = null!;
    private Button _loginButton = null!;

    /// <summary>
    /// Gets the entered username.
    /// </summary>
    public string Username => _usernameTextBox.Text;

    /// <summary>
    /// Gets the entered password.
    /// </summary>
    public string Password => _passwordTextBox.Text;

    /// <summary>
    /// Occurs when the login button is clicked.
    /// </summary>
    public event EventHandler? LoginClicked;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoginUserControl"/> class.
    /// </summary>
    public LoginUserControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        BackColor = ThemeManager.CurrentTheme.ControlBackground;
        BorderStyle = OldSchoolForms.Ui.Controls.Containers.BorderStyle.FixedSingle;

        _usernameLabel = new Label { Text = SR.GetString("LabelUsername"), Location = new Point(10, 15), Size = new Size(80, 20) };
        _usernameTextBox = new TextBox { Location = new Point(100, 15), Size = new Size(230, 25), Text = SR.GetString("DefaultLoginUsername") };

        _passwordLabel = new Label { Text = SR.GetString("LabelPassword"), Location = new Point(10, 50), Size = new Size(80, 20) };
        _passwordTextBox = new TextBox { Location = new Point(100, 50), Size = new Size(230, 25), Text = SR.GetString("DefaultLoginPassword"), UseSystemPasswordChar = true };

        _rememberCheckBox = new CheckBox { Text = SR.GetString("ChkRememberMe"), Location = new Point(100, 80), Size = new Size(150, 20), Checked = true };

        _loginButton = new Button { Text = SR.GetString("BtnLogin"), Location = new Point(100, 115), Size = new Size(100, 30) };
        _loginButton.Click += (s, e) => LoginClicked?.Invoke(this, EventArgs.Empty);

        Controls.Add(_usernameLabel);
        Controls.Add(_usernameTextBox);
        Controls.Add(_passwordLabel);
        Controls.Add(_passwordTextBox);
        Controls.Add(_rememberCheckBox);
        Controls.Add(_loginButton);
    }
}
