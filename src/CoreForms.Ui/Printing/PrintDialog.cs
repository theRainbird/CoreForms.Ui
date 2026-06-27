using CoreForms.Ui.Core;
using CoreForms.Ui.Platform;

namespace CoreForms.Ui.Printing;

/// <summary>
/// Displays a print dialog box that allows users to select a printer
/// and choose print settings.
/// </summary>
public sealed class PrintDialog : Component
{
    /// <summary>
    /// Gets or sets the PrinterSettings for the dialog.
    /// </summary>
    public PrinterSettings? PrinterSettings { get; set; }

    /// <summary>
    /// Gets or sets the PrintDocument associated with the dialog.
    /// </summary>
    public PrintDocument? Document { get; set; }

    /// <summary>
    /// Gets or sets whether the Current Page option is enabled.
    /// </summary>
    public bool AllowCurrentPage { get; set; }

    /// <summary>
    /// Gets or sets whether the Pages (range) option is enabled.
    /// </summary>
    public bool AllowSomePages { get; set; }

    /// <summary>
    /// Gets or sets whether the Selection option is enabled.
    /// </summary>
    public bool AllowSelection { get; set; }

    /// <summary>
    /// Gets or sets whether the Print to File option is enabled.
    /// </summary>
    public bool AllowPrintToFile { get; set; }

    /// <summary>
    /// Gets or sets whether the Network button is shown.
    /// </summary>
    public bool ShowNetwork { get; set; }

    /// <summary>
    /// Shows the print dialog with no explicit owner.
    /// </summary>
    public DialogResult ShowDialog()
    {
        return ShowDialog(owner: null);
    }

    /// <summary>
    /// Shows the print dialog with the specified owner window.
    /// </summary>
    /// <param name="owner">The owner window.</param>
    /// <returns>One of the DialogResult values.</returns>
    public DialogResult ShowDialog(INativeWindow? owner)
    {
        Form? ownerForm = null;
        if (owner is Form f)
            ownerForm = f;

        bool wasEnabled = ownerForm != null && ownerForm.Enabled;
        if (ownerForm != null)
            ownerForm.Enabled = false;

        try
        {
            var settings = PrinterSettings ?? Document?.PrinterSettings ?? new PrinterSettings();
            var pageSettings = Document?.DefaultPageSettings;

            var result = PrintDialogImpl.ShowDialog(
                ownerForm, settings, pageSettings,
                AllowCurrentPage, AllowSomePages, AllowSelection,
                AllowPrintToFile, ShowNetwork,
                out var resultSettings, out var resultPageSettings);

            if (result == DialogResult.OK)
            {
                PrinterSettings = resultSettings;
                if (Document != null)
                {
                    Document.PrinterSettings = resultSettings;
                    if (resultPageSettings != null)
                        Document.DefaultPageSettings = resultPageSettings;
                }
            }

            return result;
        }
        finally
        {
            if (ownerForm != null)
                ownerForm.Enabled = wasEnabled;
        }
    }
}
