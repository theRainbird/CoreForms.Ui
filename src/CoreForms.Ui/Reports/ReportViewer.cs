using CoreForms.Ui.Controls;
using CoreForms.Ui.Controls.Advanced;
using CoreForms.Ui.Controls.Basic;
using CoreForms.Ui.Controls.Containers;
using CoreForms.Ui.Core;
using CoreForms.Ui.Platform;
using CoreForms.Ui.Rendering;
using CoreForms.Ui.Resources;

namespace CoreForms.Ui.Reports;

/// <summary>
/// A composite report viewer control with a <see cref="ToolStrip"/> toolbar at the top,
/// a <see cref="StatusStrip"/> status bar at the bottom, and a <see cref="ReportPreviewControl"/>
/// filling the remaining area. Provides page navigation, zoom, print, and PDF export.
/// Toolbar buttons use SVG icons where available.
/// </summary>
public class ReportViewer : UserControl
{
    private readonly ToolStrip _toolStrip = new() { GripStyle = ToolStripGripStyle.Hidden };
    private readonly ReportPreviewControl _preview = new();
    private readonly StatusStrip _statusStrip = new() { BackColor = Color.FromArgb(240, 240, 240) };
    private readonly Label _statusLabel = new() { Text = "Ready", Location = new Point(8, 3), Size = new Size(300, 18) };

    private readonly ToolStripLabel _lblPage = new() { Text = "1 / ?" };
    private readonly ToolStripTextBox _txtZoom = new() { Text = "100%", TextBoxWidth = 55 };

    private static readonly SvgImage? IconFirst = SvgImage.FromSvgString(
        "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 20 20'><path d='M14 4l-6 6 6 6M9 4l-6 6 6 6' stroke='currentColor' stroke-width='2' fill='none' stroke-linecap='round' stroke-linejoin='round'/></svg>", 20);
    private static readonly SvgImage? IconPrev = SvgImage.FromSvgString(
        "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 20 20'><path d='M13 4l-6 6 6 6' stroke='currentColor' stroke-width='2' fill='none' stroke-linecap='round' stroke-linejoin='round'/></svg>", 20);
    private static readonly SvgImage? IconNext = SvgImage.FromSvgString(
        "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 20 20'><path d='M7 4l6 6-6 6' stroke='currentColor' stroke-width='2' fill='none' stroke-linecap='round' stroke-linejoin='round'/></svg>", 20);
    private static readonly SvgImage? IconLast = SvgImage.FromSvgString(
        "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 20 20'><path d='M6 4l6 6-6 6M11 4l6 6-6 6' stroke='currentColor' stroke-width='2' fill='none' stroke-linecap='round' stroke-linejoin='round'/></svg>", 20);
    private static readonly SvgImage? IconPrint = SvgImage.FromSvgString(
        "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 20 20'>" +
        "<path d='M5 7V3h10v4' stroke='#2563eb' stroke-width='1.5' fill='none' stroke-linecap='round' stroke-linejoin='round'/>" +
        "<rect x='3' y='7' width='14' height='5' rx='1' fill='#3b82f6'/>" +
        "<rect x='5' y='12' width='10' height='5' rx='1' fill='#dbeafe' stroke='#2563eb' stroke-width='1'/>" +
        "<path d='M8 7v3h4V7' fill='#e5e7eb' stroke='#2563eb' stroke-width='0.8'/></svg>", 20);

    /// <summary>
    /// Raised when the current page or total page count changes.
    /// </summary>
    public event EventHandler? PageChanged
    {
        add => _preview.PageChanged += value;
        remove => _preview.PageChanged -= value;
    }

    /// <summary>
    /// Gets or sets the report to preview. Call <see cref="RefreshReport"/> after setting.
    /// </summary>
    public Report? Report
    {
        get => _preview.Report;
        set => _preview.Report = value;
    }

    /// <summary>
    /// Gets the list of rendered pages.
    /// </summary>
    public IReadOnlyList<ReportPage> Pages => _preview.Pages;

    /// <summary>
    /// Gets or sets the current page index (0-based).
    /// </summary>
    public int CurrentPageIndex
    {
        get => _preview.CurrentPageIndex;
        set => _preview.CurrentPageIndex = value;
    }

    /// <summary>
    /// Gets the current page number (1-based).
    /// </summary>
    public int CurrentPage => _preview.CurrentPage;

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int PageCount => _preview.PageCount;

    /// <summary>
    /// Gets or sets the zoom factor (0.25–4.0).
    /// </summary>
    public float Zoom
    {
        get => _preview.Zoom;
        set
        {
            _preview.Zoom = value;
            _txtZoom.Text = $"{(int)(value * 100)}%";
        }
    }

    /// <summary>
    /// Gets or sets whether the toolbar is visible.
    /// </summary>
    public bool ShowToolbar
    {
        get => _toolStrip.Visible;
        set => _toolStrip.Visible = value;
    }

    /// <summary>
    /// Gets or sets whether the status bar is visible.
    /// </summary>
    public bool ShowStatusBar
    {
        get => _statusStrip.Visible;
        set => _statusStrip.Visible = value;
    }

    /// <summary>
    /// Gets the internal ToolStrip used for the toolbar.
    /// </summary>
    public ToolStrip Toolbar => _toolStrip;

    /// <summary>
    /// Gets the internal ReportPreviewControl.
    /// </summary>
    public ReportPreviewControl Preview => _preview;

    /// <summary>
    /// Initializes a new ReportViewer.
    /// </summary>
    public ReportViewer()
    {
        Size = new Size(800, 600);

        var btnFirst = new ToolStripButton(IconFirst) { DisplayStyle = ToolStripItemDisplayStyle.Image };
        var btnPrev = new ToolStripButton(IconPrev) { DisplayStyle = ToolStripItemDisplayStyle.Image };
        var btnNext = new ToolStripButton(IconNext) { DisplayStyle = ToolStripItemDisplayStyle.Image };
        var btnLast = new ToolStripButton(IconLast) { DisplayStyle = ToolStripItemDisplayStyle.Image };

        btnFirst.Click += (_, _) => _preview.FirstPage();
        btnPrev.Click += (_, _) => _preview.PreviousPage();
        btnNext.Click += (_, _) => _preview.NextPage();
        btnLast.Click += (_, _) => _preview.LastPage();

        var btnPrint = new ToolStripButton("Print", IconPrint) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText };
        btnPrint.Click += (_, _) => PrintReport();

        var btnExport = new ToolStripButton("PDF", Icons.DocumentText24) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText };
        btnExport.Click += (_, _) => ExportPdf();

        _txtZoom.TextChanged += (_, _) =>
        {
            if (float.TryParse(_txtZoom.Text?.Replace("%", "")?.Trim(), out var pct))
                _preview.Zoom = Math.Clamp(pct / 100f, 0.25f, 4.0f);
        };

        _toolStrip.Items.Add(btnFirst);
        _toolStrip.Items.Add(btnPrev);
        _toolStrip.Items.Add(_lblPage);
        _toolStrip.Items.Add(btnNext);
        _toolStrip.Items.Add(btnLast);
        _toolStrip.Items.Add(new ToolStripSeparator());
        _toolStrip.Items.Add(btnPrint);
        _toolStrip.Items.Add(btnExport);
        _toolStrip.Items.Add(new ToolStripSeparator());
        _toolStrip.Items.Add(new ToolStripLabel("Zoom"));
        _toolStrip.Items.Add(_txtZoom);
        _toolStrip.Dock = DockStyle.Top;

        _preview.Dock = DockStyle.Fill;

        _statusStrip.Size = new Size(800, 24);
        _statusStrip.Dock = DockStyle.Bottom;
        _statusStrip.Controls.Add(_statusLabel);

        _preview.PageChanged += (_, _) => UpdatePageDisplay();

        Controls.Add(_toolStrip);
        Controls.Add(_statusStrip);
        Controls.Add(_preview);
    }

    private void UpdatePageDisplay()
    {
        string total = PageCount > 0 ? PageCount.ToString() : "?";
        _lblPage.Text = $"{_preview.CurrentPage} / {total}";
        _statusLabel.Text = $"Page {_preview.CurrentPage} of {total}";
    }

    /// <summary>
    /// Renders (or re-renders) the report.
    /// </summary>
    public void RefreshReport()
    {
        _preview.RefreshReport();
    }

    /// <summary>
    /// Prints the report using the system print dialog.
    /// </summary>
    public void PrintReport()
    {
        var report = _preview.Report;
        if (report == null || _preview.PageCount == 0) return;

        var exporter = new ReportExportPdf();
        byte[] pdfBytes = exporter.ExportToBytes(report);

        var printerSettings = new PrinterSettings();
        var printDialog = new PrintDialog { PrinterSettings = printerSettings };

        if (printDialog.ShowDialog() == DialogResult.OK)
        {
            Platform.PrintDialogImpl.PrintPdf(pdfBytes, printerSettings, report.Name);
        }
    }

    /// <summary>
    /// Exports the report to a PDF file.
    /// </summary>
    /// <param name="filePath">Optional path. If null, a save dialog is shown.</param>
    public void ExportPdf(string? filePath = null)
    {
        var report = _preview.Report;
        if (report == null || _preview.PageCount == 0) return;

        if (string.IsNullOrEmpty(filePath))
        {
            var dialog = new SaveFileDialog
            {
                Filter = "PDF files (*.pdf)|*.pdf",
                FileName = report.Name + ".pdf"
            };
            if (dialog.ShowDialog() != DialogResult.OK || string.IsNullOrEmpty(dialog.FileName))
                return;
            filePath = dialog.FileName;
        }

        var exporter = new ReportExportPdf();
        exporter.Export(report, filePath);
    }
}
