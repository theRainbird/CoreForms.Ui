using CoreForms.Ui.Core;

namespace CoreForms.Ui.Demo;

/// <summary>
/// The main entry point for the demo application.
/// </summary>
class Program
{
    /// <summary>
    /// The main entry point for the demo application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        LocalizationManager.LoadSavedCulture();
        Application.Run(new MainForm());
    }
}
