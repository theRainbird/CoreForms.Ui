using OldSchoolForms.Ui.Core;
using OldSchoolForms.Ui.Theming;

namespace OldSchoolFormsUiApp;

/// <summary>
/// The main application window.
/// </summary>
public class MainForm : Form
{
    /// <summary>
    /// Initializes a new instance of MainForm.
    /// </summary>
    public MainForm()
    {
        Title = "OldSchoolFormsUiApp";
        Width = 1024;
        Height = 768;
    }

    /// <summary>
    /// Called when the theme changes.
    /// </summary>
    /// <param name="newTheme">The new theme that was activated.</param>
    public override void OnThemeChanged(Theme newTheme)
    {
        base.OnThemeChanged(newTheme);
    }
}
