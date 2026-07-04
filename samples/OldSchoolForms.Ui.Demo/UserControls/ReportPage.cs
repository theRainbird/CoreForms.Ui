using OldSchoolForms.Ui.Controls.Basic;
using OldSchoolForms.Ui.Demo.Models;
using OldSchoolForms.Ui.Controls;
using OldSchoolForms.Ui.Controls.Advanced;
using OldSchoolForms.Ui.Core;
using OldSchoolForms.Ui.Reports;
using Graphics = OldSchoolForms.Ui.Rendering.Graphics;

namespace OldSchoolForms.Ui.Demo.UserControls;

/// <summary>
/// Demonstrates the Report and ReportViewer engine with a grouped employee report.
/// </summary>
public class ReportPage : UserControl
{
    /// <summary>
    /// Occurs when the status text should be updated.
    /// </summary>
    public event EventHandler<StatusTextChangedEventArgs>? StatusTextChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportPage"/> class.
    /// </summary>
    public ReportPage()
    {
        var report = new Report("Employee Report");
        report.PageSetup.SetPaperSize(ReportPaperSize.A4, landscape: true);
        report.PageSetup.SetMargins(1.5);

        var employees = new List<Person>
        {
            new(1, "Alice Wonder", "alice@example.com", "Active") { Salary = 85000m, Department = "Engineering" },
            new(2, "Bob Builder", "bob@example.com", "Active") { Salary = 72000m, Department = "Engineering" },
            new(3, "Charlie Brown", "charlie@example.com", "Inactive") { Salary = 0m },
            new(4, "Diana Prince", "diana@example.com", "Active") { Salary = 95000m, Department = "Marketing" },
            new(5, "Eve Adams", "eve@example.com", "Pending") { Salary = 68000m, Department = "Engineering" },
            new(6, "Frank Castle", "frank@example.com", "Active") { Salary = 78000m, Department = "Sales" },
            new(7, "Grace Hopper", "grace@example.com", "Active") { Salary = 110000m, Department = "Engineering" },
            new(8, "Henry Ford", "henry@example.com", "Inactive") { Salary = 0m },
            new(9, "Ivy League", "ivy@example.com", "Pending") { Salary = 62000m, Department = "Marketing" },
            new(10, "Jack Sparrow", "jack@example.com", "Active") { Salary = 88000m, Department = "Sales" },
            new(11, "Kate Bishop", "kate@example.com", "Active") { Salary = 74000m, Department = "HR" },
            new(12, "Leo Messi", "leo@example.com", "Active") { Salary = 120000m, Department = "Engineering" },
        };
        report.DataSource = employees;

        var titleFont = new Font("Arial", 18, FontStyle.Bold);
        var headerFont = new Font("Arial", 10, FontStyle.Bold);
        var dataFont = new Font("Arial", 9);

        report.PageHeader.Height = 2.5;
        report.PageHeader.BackColor = Color.FromArgb(230, 240, 255);
        report.PageHeader.Controls.Add(new ReportLabel
        {
            Text = "Employee Report",
            Left = 0, Top = 0.2, Width = 26.7, Height = 1.2,
            Font = titleFont, TextAlign = TextAlignment.Center
        });
        report.PageHeader.Controls.Add(new ReportLine
        {
            Left = 0, Top = 1.5, X2 = 26.7, Y2 = 1.5,
            LineWidth = 0.04, LineColor = Color.FromArgb(50, 80, 180)
        });

        var colHdrFont = new Font("Arial", 8, FontStyle.Bold);
        report.PageHeader.Controls.Add(new ReportLabel
        {
            Text = "ID", Left = 0.3, Top = 1.6, Width = 2, Height = 0.4,
            Font = colHdrFont, ForeColor = Color.FromArgb(30, 60, 150),
            TextAlign = TextAlignment.Right
        });
        report.PageHeader.Controls.Add(new ReportLabel
        {
            Text = "Name", Left = 3.0, Top = 1.6, Width = 7, Height = 0.4,
            Font = colHdrFont, ForeColor = Color.FromArgb(30, 60, 150)
        });
        report.PageHeader.Controls.Add(new ReportLabel
        {
            Text = "Email", Left = 10.5, Top = 1.6, Width = 8, Height = 0.4,
            Font = colHdrFont, ForeColor = Color.FromArgb(30, 60, 150)
        });
        report.PageHeader.Controls.Add(new ReportLabel
        {
            Text = "Salary", Left = 19.0, Top = 1.6, Width = 3.5, Height = 0.4,
            Font = colHdrFont, ForeColor = Color.FromArgb(30, 60, 150),
            TextAlign = TextAlignment.Right
        });
        report.PageHeader.Controls.Add(new ReportLabel
        {
            Text = "Active", Left = 23.0, Top = 1.6, Width = 3, Height = 0.4,
            Font = colHdrFont, ForeColor = Color.FromArgb(30, 60, 150)
        });
        report.PageHeader.Controls.Add(new ReportLine
        {
            Left = 0, Top = 2.1, X2 = 26.7, Y2 = 2.1,
            LineWidth = 0.02, LineColor = Color.FromArgb(80, 120, 200)
        });

        var group = new ReportGroup("Status");
        group.Header.Height = 0.7;
        group.Header.BackColor = Color.FromArgb(200, 220, 255);
        group.Header.RepeatOnNewPage = true;
        group.Header.Controls.Add(new ReportTextBox
        {
            Expression = "Status: {Status}",
            Left = 0.5, Top = 0.1, Width = 10, Height = 0.5,
            Font = headerFont, ForeColor = Color.FromArgb(30, 60, 150)
        });
        group.Header.Controls.Add(new ReportLine
        {
            Left = 0.5, Top = 0.65, X2 = 26.2, Y2 = 0.65,
            LineWidth = 0.02, LineColor = Color.FromArgb(150, 180, 220)
        });
        group.Footer.Height = 0.4;
        group.Footer.Controls.Add(new ReportLine
        {
            Left = 0.5, Top = 0.2, X2 = 26.2, Y2 = 0.2,
            LineWidth = 0.02, LineColor = Color.FromArgb(150, 180, 220)
        });
        report.Groups.Add(group);

        report.Detail.Height = 0.55;
        report.Detail.Controls.Add(new ReportTextBox
        {
            DataField = "Id", Left = 0.3, Top = 0.05,
            Width = 2, Height = 0.45, Font = dataFont,
            TextAlign = TextAlignment.Right, Format = "{0:D3}"
        });
        report.Detail.Controls.Add(new ReportTextBox
        {
            DataField = "Name", Left = 3.0, Top = 0.05,
            Width = 7, Height = 0.45, Font = dataFont
        });
        report.Detail.Controls.Add(new ReportTextBox
        {
            DataField = "Email", Left = 10.5, Top = 0.05,
            Width = 8, Height = 0.45, Font = dataFont
        });
        report.Detail.Controls.Add(new ReportTextBox
        {
            DataField = "Salary", Left = 19.0, Top = 0.05,
            Width = 3.5, Height = 0.45, Font = dataFont,
            TextAlign = TextAlignment.Right, Format = "{0:N0} EUR"
        });
        report.Detail.Controls.Add(new ReportCheckBox
        {
            DataField = "IsActive", Left = 23.0, Top = 0.05,
            Width = 3, Height = 0.45
        });

        var chart = new ReportChartControl
        {
            Left = 0, Top = 0, Width = 26.7, Height = 6,
            ChartType = DiagramType.Bar,
            Title = "Salary by Status (Average)",
            Font = dataFont,
            ShowLegend = true,
            LegendPosition = LegendPosition.Right,
            DataLabelStyle = LabelStyle.Value,
        };
        chart.Series.Add(new DiagramViewSeries
        {
            Name = "Avg Salary",
            Color = Color.FromArgb(135, 195, 235),
            Points =
            {
                new DiagramViewDataPoint("Active", 91714.3),
                new DiagramViewDataPoint("Inactive", 0),
                new DiagramViewDataPoint("Pending", 65000),
            }
        });
        var xtab = new ReportCrossTab
        {
            Left = 0, Top = 7, Width = 26.7, Height = 3.5,
            HeaderFont = headerFont,
            CellPadding = 0.08,
            RowPadding = 0.02,
            ColumnHeaderHeight = 0.5,
            Font = dataFont,
            Fields =
            {
                new CrossTabField { DataField = "Status", Usage = FieldUsage.RowField, HeaderText = "Status" },
                new CrossTabField { DataField = "Department", Usage = FieldUsage.ColumnField, HeaderText = "Department" },
                new CrossTabField { DataField = "Salary", Usage = FieldUsage.ValueField, Aggregation = CrossTabAggregation.Sum, Format = "{0:N0} EUR", HeaderText = "Total" },
                new CrossTabField { DataField = "Salary", Usage = FieldUsage.ValueField, Aggregation = CrossTabAggregation.Avg, Format = "{0:N0} EUR", HeaderText = "Average" },
            }
        };
        report.ReportFooter.Height = 11;
        report.ReportFooter.Controls.Add(chart);
        report.ReportFooter.Controls.Add(xtab);

        report.PageFooter.Height = 0.6;
        report.PageFooter.Controls.Add(new ReportLine
        {
            Left = 0, Top = 0.1, X2 = 26.7, Y2 = 0.1,
            LineWidth = 0.02, LineColor = Color.FromArgb(180, 180, 180)
        });
        report.PageFooter.Controls.Add(new ReportLabel
        {
            Text = "Confidential",
            Left = 0, Top = 0.2, Width = 8, Height = 0.35,
            Font = new Font("Arial", 7), ForeColor = Color.FromArgb(128, 128, 128)
        });
        report.PageFooter.Controls.Add(new ReportTextBox
        {
            Expression = "Page {PageNumber} / {TotalPages}",
            Left = 18, Top = 0.2, Width = 8.7, Height = 0.35,
            Font = new Font("Arial", 7), ForeColor = Color.FromArgb(128, 128, 128),
            TextAlign = TextAlignment.Right
        });

        var viewer = new ReportViewer
        {
            Dock = DockStyle.Fill,
            Report = report
        };
        viewer.RefreshReport();

        var refreshBtn = new Button
        {
            Text = "Refresh Report",
            Location = new Point(10, 10),
            Size = new Size(140, 30)
        };
        refreshBtn.Click += (_, _) =>
        {
            viewer.Report = report;
            viewer.RefreshReport();
            OnStatusTextChanged("Report refreshed");
        };

        Controls.Add(viewer);
    }

    private void OnStatusTextChanged(string text)
    {
        StatusTextChanged?.Invoke(this, new StatusTextChangedEventArgs(text));
    }
}
