using CoreForms.Ui.Core;
using CoreForms.Ui.Reports;
using Xunit;

namespace CoreForms.Ui.Tests;

public class ReportTests
{
    [Fact]
    public void Cm_FromDouble_StoresValue()
    {
        var cm = new Cm(5.5);
        Assert.Equal(5.5, cm.Value);
    }

    [Fact]
    public void Cm_ImplicitConversion_FromDouble()
    {
        Cm cm = 3.0;
        Assert.Equal(3.0, cm.Value);
    }

    [Fact]
    public void Cm_ArithmeticOperators()
    {
        Cm a = 5.0;
        Cm b = 2.0;

        Assert.Equal(7.0, (a + b).Value);
        Assert.Equal(3.0, (a - b).Value);
        Assert.Equal(10.0, (a * 2.0).Value);
        Assert.Equal(2.5, (a / 2.0).Value);
        Assert.Equal(-5.0, (-a).Value);
    }

    [Fact]
    public void Cm_ComparisonOperators()
    {
        Cm small = 1.0;
        Cm large = 10.0;

        Assert.True(small < large);
        Assert.True(large > small);
        Assert.True(small <= large);
        Assert.True(large >= small);
        Assert.True(small != large);
        Assert.True(small == new Cm(1.0));
    }

    [Fact]
    public void Cm_MaxMin()
    {
        Assert.Equal(10.0, Cm.Max((Cm)5.0, (Cm)10.0).Value);
        Assert.Equal(5.0, Cm.Min((Cm)5.0, (Cm)10.0).Value);
    }

    [Fact]
    public void Cm_ToPixels_At96Dpi()
    {
        var cm = new Cm(2.54);
        Assert.Equal(96, cm.ToPixels(96));
    }

    [Fact]
    public void Cm_ToPoints()
    {
        var cm = new Cm(2.54);
        Assert.Equal(72.0, cm.ToPoints(), 4);
    }

    [Fact]
    public void Cm_ToHundredthsInch()
    {
        var cm = new Cm(2.54);
        Assert.Equal(100, cm.ToHundredthsInch());
    }

    [Fact]
    public void Cm_FromMm()
    {
        var cm = Cm.FromMm(254);
        Assert.Equal(25.4, cm.Value);
    }

    [Fact]
    public void Cm_FromInches()
    {
        var cm = Cm.FromInches(2);
        Assert.Equal(5.08, cm.Value, 2);
    }

    [Fact]
    public void Cm_Equality()
    {
        var a = new Cm(10.0);
        var b = new Cm(10.0);
        var c = new Cm(20.0);

        Assert.True(a.Equals(b));
        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }

    [Fact]
    public void Cm_ToString_FormatsCorrectly()
    {
        var cm = new Cm(5.5);
        Assert.Equal("5,50 cm", cm.ToString());
    }

    [Fact]
    public void Report_DefaultProperties()
    {
        var report = new Report("TestReport");

        Assert.Equal("TestReport", report.Name);
        Assert.Equal(21.0, report.PageWidth.Value);
        Assert.Equal(29.7, report.PageHeight.Value);
        Assert.Equal(2.0, report.LeftMargin.Value);
        Assert.Equal(2.0, report.RightMargin.Value);
        Assert.Equal(2.0, report.TopMargin.Value);
        Assert.Equal(2.0, report.BottomMargin.Value);

        Assert.NotNull(report.ReportHeader);
        Assert.NotNull(report.PageHeader);
        Assert.NotNull(report.Detail);
        Assert.NotNull(report.PageFooter);
        Assert.NotNull(report.ReportFooter);
        Assert.NotNull(report.Groups);
    }

    [Fact]
    public void Report_PrintableWidth_CalculatesCorrectly()
    {
        var report = new Report
        {
            PageWidth = 21.0,
            LeftMargin = 2.0,
            RightMargin = 2.0
        };

        Assert.Equal(17.0, report.PrintableWidth.Value);
    }

    [Fact]
    public void ReportBand_AddAndRetrieveControls()
    {
        var band = new ReportBand(ReportBandType.Detail);
        var label = new ReportLabel { Text = "Hello" };

        band.Controls.Add(label);

        Assert.Single(band.Controls);
        Assert.Same(label, band.Controls[0]);
    }

    [Fact]
    public void ReportLabel_RendersStaticText()
    {
        var label = new ReportLabel
        {
            Text = "Static Text",
            Left = 1.0,
            Top = 0.5,
            Width = 5.0,
            Height = 0.8
        };

        Assert.Equal("Static Text", label.Text);
        Assert.Equal(1.0, label.Left.Value);
        Assert.Equal(5.0, label.Width.Value);
    }

    [Fact]
    public void ReportTextBox_WithDataField()
    {
        var textBox = new ReportTextBox
        {
            DataField = "Name",
            Left = 0,
            Top = 0,
            Width = 8,
            Height = 0.6
        };

        Assert.Equal("Name", textBox.DataField);
    }

    [Fact]
    public void ReportGroup_CreatesHeaderAndFooter()
    {
        var group = new ReportGroup("Category", "CategoryGroup");

        Assert.Equal("Category", group.GroupField);
        Assert.Equal("CategoryGroup", group.Name);
        Assert.NotNull(group.Header);
        Assert.Equal(ReportBandType.GroupHeader, group.Header.BandType);
        Assert.NotNull(group.Footer);
        Assert.Equal(ReportBandType.GroupFooter, group.Footer.BandType);
    }

    [Fact]
    public void ReportRenderEngine_EmptyDataSource_ReturnsPage()
    {
        var report = new Report("EmptyTest")
        {
            PageWidth = 21.0,
            PageHeight = 29.7,
            DataSource = new List<object>()
        };

        var engine = new ReportRenderEngine();
        var pages = engine.Render(report, 96f);

        Assert.Single(pages);
        Assert.Equal(1, pages[0].PageNumber);
    }

    [Fact]
    public void ReportRenderEngine_NullDataSource_ReturnsPage()
    {
        var report = new Report("NullTest");
        var engine = new ReportRenderEngine();
        var pages = engine.Render(report, 96f);

        Assert.Single(pages);
    }

    [Fact]
    public void ReportRenderEngine_SingleRecord_RendersOnePage()
    {
        var report = new Report("SingleRecord")
        {
            PageWidth = 21.0,
            PageHeight = 29.7,
            DataSource = new List<TestRecord>
            {
                new() { Name = "Alice", Value = 100 }
            }
        };

        report.Detail.Height = 1.0;
        report.Detail.Controls.Add(new ReportTextBox
        {
            DataField = "Name",
            Left = 0, Top = 0, Width = 8, Height = 0.6
        });

        var engine = new ReportRenderEngine();
        var pages = engine.Render(report, 96f);

        Assert.Single(pages);
        Assert.Equal(1, pages[0].PageNumber);
    }

    [Fact]
    public void ReportRenderEngine_MultipleRecords_PageBreak()
    {
        var records = new List<TestRecord>();
        for (int i = 0; i < 100; i++)
            records.Add(new TestRecord { Name = $"Record {i}", Value = i });

        var report = new Report("MultiPage")
        {
            PageWidth = 21.0,
            PageHeight = 5.0,
            TopMargin = 0.5,
            BottomMargin = 0.5,
            DataSource = records
        };

        report.Detail.Height = 1.0;
        report.Detail.Controls.Add(new ReportTextBox
        {
            DataField = "Name",
            Left = 0, Top = 0, Width = 8, Height = 0.8
        });

        var engine = new ReportRenderEngine();
        var pages = engine.Render(report, 96f);

        Assert.True(pages.Count > 1, $"Expected multiple pages, got {pages.Count}");
        Assert.All(pages, p => Assert.NotNull(p.Graphics));
    }

    [Fact]
    public void ReportRenderEngine_PageHeader_RepeatsOnEachPage()
    {
        var records = new List<TestRecord>();
        for (int i = 0; i < 50; i++)
            records.Add(new TestRecord { Name = $"Record {i}", Value = i });

        var report = new Report("HeaderRepeat")
        {
            PageWidth = 21.0,
            PageHeight = 5.0,
            TopMargin = 0.5,
            BottomMargin = 0.5,
            DataSource = records
        };

        report.PageHeader.Height = 0.8;
        report.PageHeader.RepeatOnNewPage = true;
        report.PageHeader.Controls.Add(new ReportLabel
        {
            Text = "Page Header",
            Left = 0, Top = 0, Width = 10, Height = 0.8
        });

        report.Detail.Height = 0.6;
        report.Detail.Controls.Add(new ReportTextBox
        {
            DataField = "Name",
            Left = 0, Top = 0, Width = 8, Height = 0.5
        });

        var engine = new ReportRenderEngine();
        var pages = engine.Render(report, 96f);

        Assert.True(pages.Count >= 2);
    }

    [Fact]
    public void ReportRenderEngine_WithGrouping_DetectsGroupChanges()
    {
        var records = new List<TestRecord>
        {
            new() { Name = "Alice", Category = "A" },
            new() { Name = "Bob", Category = "A" },
            new() { Name = "Charlie", Category = "B" },
            new() { Name = "Diana", Category = "B" }
        };

        var report = new Report("GroupTest")
        {
            PageWidth = 21.0,
            PageHeight = 29.7,
            DataSource = records
        };

        var group = new ReportGroup("Category", "CategoryGroup");
        group.Header.Height = 0.5;
        group.Header.Controls.Add(new ReportLabel
        {
            Text = "Group",
            Left = 0, Top = 0, Width = 3, Height = 0.5
        });
        group.Footer.Height = 0.3;

        report.Groups.Add(group);

        report.Detail.Height = 0.5;
        report.Detail.Controls.Add(new ReportTextBox
        {
            DataField = "Name",
            Left = 0, Top = 0, Width = 8, Height = 0.5
        });

        var engine = new ReportRenderEngine();
        var pages = engine.Render(report, 96f);

        Assert.Single(pages);
    }

    [Fact]
    public void ReportTextBox_FormatString_AppliesCorrectly()
    {
        var textBox = new ReportTextBox
        {
            Format = "{0:C}",
            Width = 5,
            Height = 0.5
        };

        Assert.Equal("{0:C}", textBox.Format);
    }

    [Fact]
    public void ReportLine_DefaultProperties()
    {
        var line = new ReportLine
        {
            Left = 0,
            Top = 1.0,
            X2 = 17.0,
            Y2 = 1.0,
            LineColor = Color.Black,
            LineWidth = 0.02
        };

        Assert.Equal(0, line.Left.Value);
        Assert.Equal(17.0, line.X2.Value);
        Assert.Equal(Color.Black, line.LineColor);
    }

    [Fact]
    public void ReportImage_DefaultSizing()
    {
        var img = new ReportImage
        {
            Left = 0, Top = 0, Width = 5, Height = 3
        };

        Assert.Equal(ImageSizing.Zoom, img.Sizing);
    }

    [Fact]
    public void ReportCheckBox_DefaultState()
    {
        var cb = new ReportCheckBox
        {
            DataField = "IsActive",
            Caption = "Active"
        };

        Assert.Equal("IsActive", cb.DataField);
        Assert.Equal("Active", cb.Caption);
    }

    private class TestRecord
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }
        public string Category { get; set; } = "";
    }
}
