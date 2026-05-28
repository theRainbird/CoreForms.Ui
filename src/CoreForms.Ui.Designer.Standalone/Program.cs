using CoreForms.Ui.Core;

namespace CoreForms.Ui.Designer.Standalone;

/// <summary>
/// Entry point for the CoreForms Form Designer standalone application.
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
