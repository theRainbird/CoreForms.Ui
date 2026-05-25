using CoreForms.Ui.Controls.Basic;
using CoreForms.Ui.Controls;
using CoreForms.Ui.Core;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Demo.UserControls;

/// <summary>
/// Demonstrates PrintDialog, printer list, and quick print with PrintDocument.
/// </summary>
public class PrintDialogPage : UserControl
{
    /// <summary>
    /// Occurs when the status text should be updated.
    /// </summary>
    public event EventHandler<StatusTextChangedEventArgs>? StatusTextChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrintDialogPage"/> class.
    /// </summary>
    public PrintDialogPage()
    {
        var printerListBox = new ListBox
        {
            Location = new Point(170, 10),
            Size = new Size(250, 200)
        };

        var setupButton = new Button { Text = SR.GetString("BtnPrintSetup"), Location = new Point(10, 10), Size = new Size(150, 35) };
        setupButton.Click += (s, e) =>
        {
            var dlg = new PrintDialog
            {
                AllowSomePages = true,
                AllowSelection = true,
                AllowCurrentPage = true
            };
            var result = dlg.ShowDialog();
            if (result == DialogResult.OK)
            {
                var ps = dlg.PrinterSettings!;
                OnStatusTextChanged(string.Format(SR.GetString("StatusPrintSetupFormat"), ps.PrinterName, ps.Copies, ps.PrintRange));
            }
            else
            {
                OnStatusTextChanged(SR.GetString("StatusPrintCancelled"));
            }
        };

        var listButton = new Button { Text = SR.GetString("BtnPrinterList"), Location = new Point(10, 55), Size = new Size(150, 35) };
        listButton.Click += (s, e) =>
        {
            printerListBox.Items.Clear();
            var printers = PrinterSettings.InstalledPrinters;
            foreach (var p in printers)
                printerListBox.Items.Add(p);
            OnStatusTextChanged(string.Format(SR.GetString("StatusPrintersFoundFormat"), printers.Length));
        };

        var testPrintButton = new Button { Text = SR.GetString("BtnQuickPrint"), Location = new Point(10, 100), Size = new Size(150, 35) };
        testPrintButton.Click += (s, e) =>
        {
            var doc = new PrintDocument
            {
                DocumentName = SR.GetString("PrintDocName"),
                PrinterSettings = new PrinterSettings()
            };
            doc.DefaultPageSettings = new PageSettings
            {
                PaperSize = Core.PaperSize.A4,
                Landscape = false
            };
            doc.PrintPage += (sender, args) =>
            {
                var g = args.Graphics;
                if (g == null) return;

                var bounds = args.MarginBounds;

                using (var paint = new SkiaSharp.SKPaint
                {
                    Color = SkiaSharp.SKColors.Black,
                    Style = SkiaSharp.SKPaintStyle.Stroke,
                    StrokeWidth = 2
                })
                {
                    g.DrawRectangle(Core.Color.Black, bounds.X, bounds.Y, bounds.Width, bounds.Height);
                }

                g.DrawString(SR.GetString("PrintTestPageTitle"), new Core.Font("Arial", 24), Core.Color.Black,
                    bounds.X + 10, bounds.Y + 10);

                g.DrawString(string.Format(SR.GetString("PrintDateFormat"), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")), new Core.Font("Arial", 12), Core.Color.Black,
                    bounds.X + 10, bounds.Y + 60);

                g.DrawString(SR.GetString("PrintTestPageBody"), new Core.Font("Arial", 12), Core.Color.Black,
                    bounds.X + 10, bounds.Y + 100);

                args.HasMorePages = false;
            };
            try
            {
                doc.Print();
                OnStatusTextChanged(SR.GetString("StatusPrintSent"));
            }
            catch (Exception ex)
            {
                OnStatusTextChanged(string.Format(SR.GetString("StatusPrintErrorFormat"), ex.Message));
                Console.WriteLine($"[Print] Error: {ex}");
            }
        };

        Controls.Add(setupButton);
        Controls.Add(listButton);
        Controls.Add(testPrintButton);
        Controls.Add(printerListBox);
    }

    private void OnStatusTextChanged(string text)
    {
        StatusTextChanged?.Invoke(this, new StatusTextChangedEventArgs(text));
    }
}
