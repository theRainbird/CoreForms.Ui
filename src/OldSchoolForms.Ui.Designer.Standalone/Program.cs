using OldSchoolForms.Ui.Core;

namespace OldSchoolForms.Ui.Designer.Standalone;

/// <summary>
/// Entry point for the OldSchoolForms Form Designer standalone application.
/// </summary>
internal static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    internal static void Main()
    {
        Application.Run(new DesignerForm());
    }
}
