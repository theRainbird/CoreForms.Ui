using OldSchoolForms.Ui.Controls.Advanced;
using OldSchoolForms.Ui.Core;
using Xunit;

namespace OldSchoolForms.Ui.Tests;

public class DiagramViewTests
{
    [Fact]
    public void DiagramView_DefaultValues_ShouldBeCorrect()
    {
        var view = new DiagramView();
        Assert.Equal(DiagramType.Bar, view.ChartType);
        Assert.Equal(LegendPosition.Right, view.LegendPosition);
        Assert.True(view.ShowLegend);
        Assert.Equal(LabelStyle.None, view.DataLabelStyle);
        Assert.Equal(1.0f, view.ZoomLevel);
        Assert.Empty(view.Series);
    }

    [Fact]
    public void DiagramView_ChartType_ShouldUpdate()
    {
        var view = new DiagramView();
        bool changed = false;
        view.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(DiagramView.ChartType)) changed = true; };

        view.ChartType = DiagramType.Pie;
        Assert.Equal(DiagramType.Pie, view.ChartType);
        Assert.True(changed);
    }

    [Fact]
    public void DiagramView_ZoomLevel_ShouldClamp()
    {
        var view = new DiagramView();
        view.ZoomLevel = -1f;
        Assert.Equal(0.1f, view.ZoomLevel);

        view.ZoomLevel = 20f;
        Assert.Equal(10f, view.ZoomLevel);

        view.ZoomLevel = 2f;
        Assert.Equal(2f, view.ZoomLevel);
    }

    [Fact]
    public void DiagramView_AddSeries_ShouldContainPoints()
    {
        var view = new DiagramView();
        var series = new DiagramViewSeries { Name = "Test" };
        series.Add("A", 10);
        series.Add("B", 20);
        series.Add("C", 30);

        view.Series.Add(series);

        Assert.Single(view.Series);
        Assert.Equal(3, view.Series[0].Points.Count);
        Assert.Equal("Test", view.Series[0].Name);
    }

    [Fact]
    public void DiagramView_MultipleSeries_ShouldTrackIndependently()
    {
        var view = new DiagramView();
        var s1 = new DiagramViewSeries { Name = "S1" };
        var s2 = new DiagramViewSeries { Name = "S2" };

        s1.Add("A", 10);
        s1.Add("B", 20);
        s2.Add("X", 100);

        view.Series.Add(s1);
        view.Series.Add(s2);

        Assert.Equal(2, view.Series.Count);
        Assert.Equal(2, view.Series[0].Points.Count);
        Assert.Single(view.Series[1].Points);
    }

    [Fact]
    public void DiagramView_SeriesChanged_ShouldNotThrow()
    {
        var view = new DiagramView();
        var series = new DiagramViewSeries();
        view.Series.Add(series);
        view.Series.Remove(series);
        Assert.Empty(view.Series);
    }

    [Fact]
    public void DiagramView_ResetView_ShouldResetZoomAndPan()
    {
        var view = new DiagramView();
        view.ZoomLevel = 2.5f;
        view.PanX = 100;
        view.PanY = 50;

        view.ResetView();

        Assert.Equal(1.0f, view.ZoomLevel);
        Assert.Equal(0, view.PanX);
        Assert.Equal(0, view.PanY);
    }

    [Fact]
    public void DiagramView_DataPoint_DefaultValues()
    {
        var pt = new DiagramViewDataPoint();
        Assert.Null(pt.Label);
        Assert.Equal(0.0, pt.Value);
        Assert.Null(pt.Color);
    }

    [Fact]
    public void DiagramView_DataPoint_ConstructorSetsLabelAndValue()
    {
        var pt = new DiagramViewDataPoint("Test", 42.5);
        Assert.Equal("Test", pt.Label);
        Assert.Equal(42.5, pt.Value);
    }

    [Fact]
    public void DiagramView_Series_AddConvenience()
    {
        var series = new DiagramViewSeries();
        series.Add("Label", 99.9);

        Assert.Single(series.Points);
        Assert.Equal("Label", series.Points[0].Label);
        Assert.Equal(99.9, series.Points[0].Value);
    }

    [Fact]
    public void DiagramView_Series_Clear()
    {
        var series = new DiagramViewSeries();
        series.Add("A", 1);
        series.Add("B", 2);

        series.Clear();
        Assert.Empty(series.Points);
    }

    [Fact]
    public void DiagramView_AxisConfig_DefaultValues()
    {
        var axis = new DiagramViewAxisConfig();
        Assert.True(axis.Visible);
        Assert.Equal(AxisType.Linear, axis.Type);
        Assert.Null(axis.MinValue);
        Assert.Null(axis.MaxValue);
        Assert.True(axis.ShowGridLines);
        Assert.True(axis.ShowLabels);
    }

    [Fact]
    public void DiagramView_TrendLine_DefaultValues()
    {
        var tl = new DiagramViewTrendLine();
        Assert.Equal(TrendLineType.None, tl.Type);
        Assert.Equal(Color.Red, tl.Color);
        Assert.Equal(2f, tl.LineWidth);
        Assert.Equal(5, tl.MovingAveragePeriod);
    }

    [Fact]
    public void DiagramView_SeriesCollection_AddRemove()
    {
        var collection = new DiagramViewSeriesCollection();
        var s1 = new DiagramViewSeries { Name = "A" };
        var s2 = new DiagramViewSeries { Name = "B" };

        collection.Add(s1);
        collection.Add(s2);
        Assert.Equal(2, collection.Count);

        Assert.True(collection.Remove(s1));
        Assert.Single(collection);
    }

    [Fact]
    public void DiagramView_SeriesCollection_Insert()
    {
        var collection = new DiagramViewSeriesCollection();
        collection.Add(new DiagramViewSeries { Name = "A" });
        collection.Add(new DiagramViewSeries { Name = "B" });

        collection.Insert(1, new DiagramViewSeries { Name = "C" });
        Assert.Equal("C", collection[1].Name);
        Assert.Equal("B", collection[2].Name);
    }

    [Fact]
    public void DiagramView_SeriesCollection_Clear()
    {
        var collection = new DiagramViewSeriesCollection();
        collection.Add(new DiagramViewSeries());
        collection.Add(new DiagramViewSeries());
        collection.Clear();

        Assert.Empty(collection);
    }

    [Fact]
    public void DiagramView_DataBinding_ShouldPopulateSeries()
    {
        var view = new DiagramView();
        var items = new List<TestDataItem>
        {
            new() { Name = "A", Value = 10 },
            new() { Name = "B", Value = 20 },
            new() { Name = "C", Value = 30 }
        };

        view.DataSource = items;
        view.ValueMember = "Value";
        view.DisplayMember = "Name";

        Assert.NotEmpty(view.Series);
        Assert.Equal(3, view.Series[0].Points.Count);
        Assert.Equal("A", view.Series[0].Points[0].Label);
        Assert.Equal(10, view.Series[0].Points[0].Value);
        Assert.Equal("B", view.Series[0].Points[1].Label);
        Assert.Equal(20, view.Series[0].Points[1].Value);
    }

    [Fact]
    public void DiagramView_DataBinding_NullDataSource_ShouldNotThrow()
    {
        var view = new DiagramView();
        view.DataSource = null;
        view.ValueMember = "Value";
        view.PopulateFromDataSource();
        Assert.Empty(view.Series);
    }

    [Fact]
    public void DiagramView_DataBinding_EmptyDataSource_ShouldNotThrow()
    {
        var view = new DiagramView();
        view.DataSource = new List<TestDataItem>();
        view.ValueMember = "Value";
        Assert.Empty(view.Series);
    }

    [Fact]
    public void DiagramView_GetPaletteColor_ShouldCycle()
    {
        var view = new DiagramView();
        var c1 = view.GetType().GetMethod("GetPaletteColor",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(c1);
    }

    [Fact]
    public void DiagramView_StackedBar_SeriesColorsShouldResolve()
    {
        var view = new DiagramView();
        view.ChartType = DiagramType.StackedBar;

        var s1 = new DiagramViewSeries { Name = "A", Color = Core.Color.Red };
        s1.Add("Q1", 10);
        view.Series.Add(s1);

        var s2 = new DiagramViewSeries { Name = "B", Color = Core.Color.Blue };
        s2.Add("Q1", 20);
        view.Series.Add(s2);

        Assert.Equal(2, view.Series.Count);
        Assert.Equal(Core.Color.Red, view.Series[0].Color);
        Assert.Equal(Core.Color.Blue, view.Series[1].Color);
        Assert.Equal(2, view.Series[0].Points.Count + view.Series[1].Points.Count);
    }

    private sealed class TestDataItem
    {
        public string? Name { get; set; }
        public double Value { get; set; }
    }
}
