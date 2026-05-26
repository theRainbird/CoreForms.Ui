using CoreForms.Ui.Controls;
using CoreForms.Ui.Controls.Advanced;
using CoreForms.Ui.Controls.Basic;
using CoreForms.Ui.Controls.Containers;
using CoreForms.Ui.Core;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Demo.UserControls;

/// <summary>
/// Demonstrates the DiagramView control with various chart types, data binding,
/// trend lines, legend, and zoom/pan interactions.
/// </summary>
public class DiagramPage : UserControl
{
    private readonly DiagramView _diagramView;
    private readonly ComboBox _chartTypeCombo;
    private readonly CheckBox _legendCheck;
    private readonly CheckBox _dataLabelsCheck;
    private readonly CheckBox _trendLineCheck;
    private readonly Label _statusLabel;

    /// <summary>
    /// Occurs when the status text should be updated.
    /// </summary>
    public event EventHandler<StatusTextChangedEventArgs>? StatusTextChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiagramPage"/> class.
    /// </summary>
    public DiagramPage()
    {
        _diagramView = new DiagramView
        {
            Location = new Point(210, 10),
            Size = new Size(760, 610),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
        };

        PopulateSampleData();

        var controlPanel = new GroupBox
        {
            Text = SR.GetString("DgControlPanel"),
            Location = new Point(10, 10),
            Size = new Size(190, 360)
        };

        var chartTypeLabel = new Label
        {
            Text = SR.GetString("DgChartType"),
            Location = new Point(10, 25),
            Size = new Size(170, 20)
        };

        _chartTypeCombo = new ComboBox
        {
            Location = new Point(10, 45),
            Size = new Size(170, 24),
            DropDownStyle = DropDownStyle.DropDownList
        };
        _chartTypeCombo.Items.Add("Bar");
        _chartTypeCombo.Items.Add("Stacked Bar");
        _chartTypeCombo.Items.Add("Pie");
        _chartTypeCombo.Items.Add("Doughnut");
        _chartTypeCombo.Items.Add("Line");
        _chartTypeCombo.Items.Add("Area");
        _chartTypeCombo.Items.Add("Gauge");
        _chartTypeCombo.SelectedIndex = 0;
        _chartTypeCombo.SelectedIndexChanged += OnChartTypeChanged;

        _legendCheck = new CheckBox
        {
            Text = SR.GetString("DgShowLegend"),
            Location = new Point(10, 80),
            Size = new Size(170, 25),
            Checked = true
        };
        _legendCheck.CheckedChanged += (s, e) =>
        {
            _diagramView.ShowLegend = _legendCheck.Checked;
            OnStatusTextChanged(string.Format(SR.GetString("DgStatusLegend"), _legendCheck.Checked));
        };

        _dataLabelsCheck = new CheckBox
        {
            Text = SR.GetString("DgDataLabels"),
            Location = new Point(10, 110),
            Size = new Size(170, 25)
        };
        _dataLabelsCheck.CheckedChanged += (s, e) =>
        {
            _diagramView.DataLabelStyle = _dataLabelsCheck.Checked ? LabelStyle.Value : LabelStyle.None;
            OnStatusTextChanged(string.Format(SR.GetString("DgStatusDataLabels"), _dataLabelsCheck.Checked));
        };

        _trendLineCheck = new CheckBox
        {
            Text = SR.GetString("DgTrendLine"),
            Location = new Point(10, 140),
            Size = new Size(170, 25)
        };
        _trendLineCheck.CheckedChanged += (s, e) =>
        {
            _diagramView.TrendLine.Type = _trendLineCheck.Checked ? TrendLineType.LinearRegression : TrendLineType.None;
            _diagramView.Invalidate();
            OnStatusTextChanged(string.Format(SR.GetString("DgStatusTrendLine"), _trendLineCheck.Checked));
        };

        var resetButton = new Button
        {
            Text = SR.GetString("DgResetView"),
            Location = new Point(10, 180),
            Size = new Size(170, 30)
        };
        resetButton.Click += (s, e) =>
        {
            _diagramView.ResetView();
            OnStatusTextChanged(SR.GetString("DgStatusReset"));
        };

        var fitButton = new Button
        {
            Text = SR.GetString("DgFitToBounds"),
            Location = new Point(10, 220),
            Size = new Size(170, 30)
        };
        fitButton.Click += (s, e) =>
        {
            _diagramView.FitToBounds();
            OnStatusTextChanged(SR.GetString("DgStatusFit"));
        };

        var zoomInButton = new Button
        {
            Text = SR.GetString("DgZoomIn"),
            Location = new Point(10, 260),
            Size = new Size(80, 30)
        };
        zoomInButton.Click += (s, e) =>
        {
            _diagramView.ZoomLevel += 0.2f;
            OnStatusTextChanged(string.Format(SR.GetString("DgStatusZoom"), _diagramView.ZoomLevel * 100));
        };

        var zoomOutButton = new Button
        {
            Text = SR.GetString("DgZoomOut"),
            Location = new Point(100, 260),
            Size = new Size(80, 30)
        };
        zoomOutButton.Click += (s, e) =>
        {
            _diagramView.ZoomLevel -= 0.2f;
            OnStatusTextChanged(string.Format(SR.GetString("DgStatusZoom"), _diagramView.ZoomLevel * 100));
        };

        var hintLabel = new Label
        {
            Text = SR.GetString("DgInteractionHint"),
            Location = new Point(10, 300),
            Size = new Size(170, 50)
        };

        controlPanel.Controls.Add(chartTypeLabel);
        controlPanel.Controls.Add(_chartTypeCombo);
        controlPanel.Controls.Add(_legendCheck);
        controlPanel.Controls.Add(_dataLabelsCheck);
        controlPanel.Controls.Add(_trendLineCheck);
        controlPanel.Controls.Add(resetButton);
        controlPanel.Controls.Add(fitButton);
        controlPanel.Controls.Add(zoomInButton);
        controlPanel.Controls.Add(zoomOutButton);
        controlPanel.Controls.Add(hintLabel);

        _statusLabel = new Label
        {
            Location = new Point(10, 380),
            Size = new Size(190, 30),
            Text = SR.GetString("DgStatusReady")
        };

        Controls.Add(controlPanel);
        Controls.Add(_statusLabel);
        Controls.Add(_diagramView);
    }

    private void PopulateSampleData()
    {
        var products = new[]
        {
            new { Name = SR.GetString("DgProductA"), Color = Core.Color.FromArgb(174, 198, 207) },
            new { Name = SR.GetString("DgProductB"), Color = Core.Color.FromArgb(225, 190, 185) },
            new { Name = SR.GetString("DgProductC"), Color = Core.Color.FromArgb(195, 215, 185) }
        };

        var quarterLabels = new[]
        {
            SR.GetString("DgQ1"),
            SR.GetString("DgQ2"),
            SR.GetString("DgQ3"),
            SR.GetString("DgQ4")
        };

        var salesData = new[]
        {
            new[] { 120.0, 145.0, 110.0, 160.0 },
            new[] { 90.0,  85.0,  110.0, 95.0 },
            new[] { 60.0,  75.0,  85.0,  70.0 }
        };

        for (int si = 0; si < products.Length; si++)
        {
            var series = new DiagramViewSeries
            {
                Name = products[si].Name,
                Color = products[si].Color
            };

            for (int qi = 0; qi < quarterLabels.Length; qi++)
            {
                series.Add(quarterLabels[qi], salesData[si][qi]);
            }

            _diagramView.Series.Add(series);
        }
    }

    private void OnChartTypeChanged(object? sender, EventArgs e)
    {
        _diagramView.ChartType = _chartTypeCombo.SelectedIndex switch
        {
            0 => DiagramType.Bar,
            1 => DiagramType.StackedBar,
            2 => DiagramType.Pie,
            3 => DiagramType.Doughnut,
            4 => DiagramType.Line,
            5 => DiagramType.Area,
            6 => DiagramType.Gauge,
            _ => DiagramType.Bar
        };

        OnStatusTextChanged(string.Format(SR.GetString("DgStatusChartType"), _chartTypeCombo.SelectedItem));
    }

    private void OnStatusTextChanged(string text)
    {
        _statusLabel.Text = text;
        StatusTextChanged?.Invoke(this, new StatusTextChangedEventArgs(text));
    }
}
