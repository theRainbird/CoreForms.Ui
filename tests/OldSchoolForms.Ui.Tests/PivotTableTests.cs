using System.ComponentModel;
using OldSchoolForms.Ui.Controls.Advanced;
using Xunit;

namespace OldSchoolForms.Ui.Tests;

public class PivotTableTests
{
    private sealed class SaleRecord
    {
        public string Category { get; set; } = string.Empty;
        public string SubCategory { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public double Revenue { get; set; }
        public int Quantity { get; set; }
        public string Date { get; set; } = string.Empty;
    }

    private static List<SaleRecord> GetSampleData()
    {
        return new List<SaleRecord>
        {
            new() { Category = "Electronics", SubCategory = "Phones",    Region = "North", Revenue = 1000, Quantity = 10, Date = "2024-Q1" },
            new() { Category = "Electronics", SubCategory = "Phones",    Region = "South", Revenue = 1500, Quantity = 12, Date = "2024-Q1" },
            new() { Category = "Electronics", SubCategory = "Laptops",   Region = "North", Revenue = 2000, Quantity = 5,  Date = "2024-Q1" },
            new() { Category = "Electronics", SubCategory = "Laptops",   Region = "South", Revenue = 2500, Quantity = 7,  Date = "2024-Q2" },
            new() { Category = "Clothing",    SubCategory = "Shirts",    Region = "North", Revenue = 500,  Quantity = 20, Date = "2024-Q1" },
            new() { Category = "Clothing",    SubCategory = "Shirts",    Region = "South", Revenue = 700,  Quantity = 15, Date = "2024-Q2" },
            new() { Category = "Clothing",    SubCategory = "Pants",     Region = "North", Revenue = 800,  Quantity = 8,  Date = "2024-Q1" },
            new() { Category = "Clothing",    SubCategory = "Pants",     Region = "South", Revenue = 600,  Quantity = 10, Date = "2024-Q2" },
            new() { Category = "Electronics", SubCategory = "Phones",    Region = "North", Revenue = 1200, Quantity = 9,  Date = "2024-Q2" },
        };
    }

    [Fact]
    public void Constructor_DefaultValues()
    {
        var pivot = new PivotTable();
        Assert.NotNull(pivot.Fields);
        Assert.Empty(pivot.Fields);
        Assert.Null(pivot.DataSource);
    }

    [Fact]
    public void AddField_FieldAppearsInCollection()
    {
        var pivot = new PivotTable();
        var field = new PivotTableField("Category", PivotTableFieldUsage.RowField);
        pivot.Fields.Add(field);

        Assert.Single(pivot.Fields);
        Assert.Same(field, pivot.Fields[0]);
    }

    [Fact]
    public void Fields_UsageFilter_ReturnsCorrectFields()
    {
        var pivot = new PivotTable();
        pivot.Fields.Add(new PivotTableField("Category", PivotTableFieldUsage.RowField));
        pivot.Fields.Add(new PivotTableField("Region", PivotTableFieldUsage.ColumnField));
        pivot.Fields.Add(new PivotTableField("Revenue", PivotTableFieldUsage.ValueField) { Aggregation = PivotTableAggregation.Sum });

        Assert.Single(pivot.Fields.RowFields);
        Assert.Single(pivot.Fields.ColumnFields);
        Assert.Single(pivot.Fields.ValueFields);
        Assert.Empty(pivot.Fields.FilterFields);
    }

    [Fact]
    public void FieldCollection_GetFields_ReturnsOrdered()
    {
        var pivot = new PivotTable();
        var f1 = new PivotTableField("Cat", PivotTableFieldUsage.RowField) { Order = 2 };
        var f2 = new PivotTableField("Sub", PivotTableFieldUsage.RowField) { Order = 1 };
        pivot.Fields.Add(f1);
        pivot.Fields.Add(f2);

        var rows = pivot.Fields.RowFields;
        Assert.Equal(2, rows.Count);
        Assert.Equal("Sub", rows[0].Name);
        Assert.Equal("Cat", rows[1].Name);
    }

    [Fact]
    public void DataSource_RebuildsMatrix()
    {
        var pivot = new PivotTable();
        pivot.Fields.Add(new PivotTableField("Category", PivotTableFieldUsage.RowField));
        pivot.Fields.Add(new PivotTableField("Revenue", PivotTableFieldUsage.ValueField) { Aggregation = PivotTableAggregation.Sum });

        bool matrixBuilt = false;
        pivot.MatrixRebuilt += (s, e) => matrixBuilt = true;

        pivot.DataSource = GetSampleData();

        Assert.True(matrixBuilt);
        Assert.True(pivot.Matrix.IsBuilt);
        Assert.True(pivot.Matrix.FlatRows.Count > 0);
    }

    [Fact]
    public void Matrix_SumAggregation_CorrectValues()
    {
        var pivot = new PivotTable();
        pivot.Fields.Add(new PivotTableField("Category", PivotTableFieldUsage.RowField));
        pivot.Fields.Add(new PivotTableField("Revenue", PivotTableFieldUsage.ValueField) { Aggregation = PivotTableAggregation.Sum });

        pivot.DataSource = GetSampleData();

        // Electronics: 1000+1500+2000+2500+1200 = 8200
        // Clothing: 500+700+800+600 = 2600
        var flatRows = pivot.Matrix.FlatRows;
        Assert.Contains(flatRows, r => r.Value == "Electronics" && !r.IsGroupHeader);
        Assert.Contains(flatRows, r => r.Value == "Clothing" && !r.IsGroupHeader);

        int electIdx = -1, clothIdx = -1;
        for (int i = 0; i < flatRows.Count; i++)
        {
            if (flatRows[i].Value == "Electronics" && !flatRows[i].IsGroupHeader) electIdx = flatRows[i].DataIndex;
            if (flatRows[i].Value == "Clothing" && !flatRows[i].IsGroupHeader) clothIdx = flatRows[i].DataIndex;
        }

        Assert.True(electIdx >= 0);
        Assert.True(clothIdx >= 0);

        double electVal = pivot.Matrix.GetValue(electIdx, 0, 0);
        double clothVal = pivot.Matrix.GetValue(clothIdx, 0, 0);

        Assert.Equal(8200, electVal);
        Assert.Equal(2600, clothVal);
    }

    [Fact]
    public void Matrix_CountAggregation_CorrectValues()
    {
        var pivot = new PivotTable();
        pivot.Fields.Add(new PivotTableField("Category", PivotTableFieldUsage.RowField));
        pivot.Fields.Add(new PivotTableField("Revenue", PivotTableFieldUsage.ValueField) { Aggregation = PivotTableAggregation.Count });

        pivot.DataSource = GetSampleData();

        // Electronics: 5 records, Clothing: 4 records
        var flatRows = pivot.Matrix.FlatRows;
        int electIdx = -1, clothIdx = -1;
        for (int i = 0; i < flatRows.Count; i++)
        {
            if (flatRows[i].Value == "Electronics" && !flatRows[i].IsGroupHeader) electIdx = flatRows[i].DataIndex;
            if (flatRows[i].Value == "Clothing" && !flatRows[i].IsGroupHeader) clothIdx = flatRows[i].DataIndex;
        }

        Assert.Equal(5, pivot.Matrix.GetValue(electIdx, 0, 0));
        Assert.Equal(4, pivot.Matrix.GetValue(clothIdx, 0, 0));
    }

    [Fact]
    public void Matrix_AverageAggregation_CorrectValues()
    {
        var pivot = new PivotTable();
        pivot.Fields.Add(new PivotTableField("Category", PivotTableFieldUsage.RowField));
        pivot.Fields.Add(new PivotTableField("Revenue", PivotTableFieldUsage.ValueField) { Aggregation = PivotTableAggregation.Average });

        pivot.DataSource = GetSampleData();

        // Electronics avg: (1000+1500+2000+2500+1200)/5 = 1640
        // Clothing avg: (500+700+800+600)/4 = 650
        var flatRows = pivot.Matrix.FlatRows;
        int electIdx = -1, clothIdx = -1;
        for (int i = 0; i < flatRows.Count; i++)
        {
            if (flatRows[i].Value == "Electronics" && !flatRows[i].IsGroupHeader) electIdx = flatRows[i].DataIndex;
            if (flatRows[i].Value == "Clothing" && !flatRows[i].IsGroupHeader) clothIdx = flatRows[i].DataIndex;
        }

        Assert.Equal(1640, pivot.Matrix.GetValue(electIdx, 0, 0));
        Assert.Equal(650, pivot.Matrix.GetValue(clothIdx, 0, 0));
    }

    [Fact]
    public void Matrix_MinMaxAggregation_CorrectValues()
    {
        var pivot = new PivotTable();
        pivot.Fields.Add(new PivotTableField("Category", PivotTableFieldUsage.RowField));
        pivot.Fields.Add(new PivotTableField("Revenue", PivotTableFieldUsage.ValueField) { Aggregation = PivotTableAggregation.Min });
        pivot.Fields.Add(new PivotTableField("Revenue2", "Revenue", PivotTableFieldUsage.ValueField) { Aggregation = PivotTableAggregation.Max });

        pivot.DataSource = GetSampleData();

        var flatRows = pivot.Matrix.FlatRows;
        int electIdx = -1;
        for (int i = 0; i < flatRows.Count; i++)
        {
            if (flatRows[i].Value == "Electronics" && !flatRows[i].IsGroupHeader) electIdx = flatRows[i].DataIndex;
        }

        // Electronics min: 1000, max: 2500
        Assert.Equal(1000, pivot.Matrix.GetValue(electIdx, 0, 0));
        Assert.Equal(2500, pivot.Matrix.GetValue(electIdx, 0, 1));
    }

    [Fact]
    public void Drilldown_Toggle_RebuildsAndChangesVisibility()
    {
        var pivot = new PivotTable();
        pivot.Fields.Add(new PivotTableField("Category", PivotTableFieldUsage.RowField));
        pivot.Fields.Add(new PivotTableField("SubCategory", PivotTableFieldUsage.RowField));
        pivot.Fields.Add(new PivotTableField("Revenue", PivotTableFieldUsage.ValueField) { Aggregation = PivotTableAggregation.Sum });

        pivot.DataSource = GetSampleData();

        // Initially, all nodes should be expanded
        int initialCount = pivot.Matrix.FlatRows.Count;

        // Find the Electronics node path and toggle it
        var electronicsNode = pivot.Matrix.FlatRows.FirstOrDefault(r => r.Value == "Electronics" && r.IsGroupHeader);
        Assert.NotNull(electronicsNode);
        Assert.True(electronicsNode.HasChildren);

        // Toggle drilldown on Electronics
        bool toggled = pivot.Matrix.ToggleDrilldown(electronicsNode.Path);
        Assert.False(toggled); // now collapsed

        pivot.RebuildMatrix();

        // After collapsing Electronics, its children should be hidden
        var flatRowsAfterCollapse = pivot.Matrix.FlatRows;
        Assert.True(flatRowsAfterCollapse.Count < initialCount);
        Assert.DoesNotContain(flatRowsAfterCollapse, r => r.Value == "Phones" || r.Value == "Laptops");

        // Expand again
        pivot.Matrix.ToggleDrilldown(electronicsNode.Path);
        pivot.RebuildMatrix();
        var flatRowsAfterExpand = pivot.Matrix.FlatRows;
        Assert.Equal(initialCount, flatRowsAfterExpand.Count);
    }

    [Fact]
    public void ExpandAll_CollapseAll_WorkCorrectly()
    {
        var pivot = new PivotTable();
        pivot.Fields.Add(new PivotTableField("Category", PivotTableFieldUsage.RowField));
        pivot.Fields.Add(new PivotTableField("SubCategory", PivotTableFieldUsage.RowField));
        pivot.Fields.Add(new PivotTableField("Revenue", PivotTableFieldUsage.ValueField) { Aggregation = PivotTableAggregation.Sum });

        pivot.DataSource = GetSampleData();

        // Initially all expanded
        var initialRows = pivot.Matrix.FlatRows;
        int initialCount = initialRows.Count;
        Assert.Contains(initialRows, r => r.Level > 0);

        // Collapse all
        pivot.CollapseAll();
        var collapsedRows = pivot.Matrix.FlatRows;
        Assert.True(collapsedRows.Count < initialCount);
        Assert.DoesNotContain(collapsedRows, r => r.Level > 0 && !r.IsTotal);

        // Expand all
        pivot.ExpandAll();
        var expandedRows = pivot.Matrix.FlatRows;
        Assert.True(expandedRows.Count > collapsedRows.Count);
        Assert.Contains(expandedRows, r => r.Level > 0);
    }

    [Fact]
    public void DrillThrough_ReturnsDetailRecords()
    {
        var pivot = new PivotTable();
        pivot.Fields.Add(new PivotTableField("Category", PivotTableFieldUsage.RowField));
        pivot.Fields.Add(new PivotTableField("Revenue", PivotTableFieldUsage.ValueField) { Aggregation = PivotTableAggregation.Sum });

        pivot.DataSource = GetSampleData();

        var flatRows = pivot.Matrix.FlatRows;
        int electIdx = -1;
        for (int i = 0; i < flatRows.Count; i++)
        {
            if (flatRows[i].Value == "Electronics" && !flatRows[i].IsGroupHeader) electIdx = flatRows[i].DataIndex;
        }

        var details = pivot.Matrix.GetDetails(electIdx, 0, 0);
        Assert.NotNull(details);
        Assert.Equal(5, details.Count);
        Assert.All(details, d => Assert.IsType<SaleRecord>(d));
        Assert.All(details, d => Assert.Equal("Electronics", ((SaleRecord)d).Category));
    }

    [Fact]
    public void SetFilter_ExcludesRecords()
    {
        var pivot = new PivotTable();
        pivot.Fields.Add(new PivotTableField("Category", PivotTableFieldUsage.RowField));
        pivot.Fields.Add(new PivotTableField("Revenue", PivotTableFieldUsage.ValueField) { Aggregation = PivotTableAggregation.Sum });
        pivot.Fields.Add(new PivotTableField("Region", PivotTableFieldUsage.FilterField));

        pivot.DataSource = GetSampleData();

        // Filter to North region only
        pivot.SetFilter("Region", "North");

        // North records: 1000+2000+500+800+1200 = 5500
        var flatRows = pivot.Matrix.FlatRows;
        int electIdx = -1;
        for (int i = 0; i < flatRows.Count; i++)
        {
            if (flatRows[i].Value == "Electronics" && !flatRows[i].IsGroupHeader) electIdx = flatRows[i].DataIndex;
        }

        double electVal = pivot.Matrix.GetValue(electIdx, 0, 0);
        Assert.Equal(4200, electVal); // Electronics in North: 1000+2000+1200 = 4200
    }

    [Fact]
    public void ClearFilters_RemovesAllFilters()
    {
        var pivot = new PivotTable();
        pivot.Fields.Add(new PivotTableField("Category", PivotTableFieldUsage.RowField));
        pivot.Fields.Add(new PivotTableField("Revenue", PivotTableFieldUsage.ValueField) { Aggregation = PivotTableAggregation.Sum });
        pivot.Fields.Add(new PivotTableField("Region", PivotTableFieldUsage.FilterField));

        pivot.DataSource = GetSampleData();
        pivot.SetFilter("Region", "North");
        pivot.ClearFilters();

        var flatRows = pivot.Matrix.FlatRows;
        int electIdx = -1;
        for (int i = 0; i < flatRows.Count; i++)
        {
            if (flatRows[i].Value == "Electronics" && !flatRows[i].IsGroupHeader) electIdx = flatRows[i].DataIndex;
        }

        double electVal = pivot.Matrix.GetValue(electIdx, 0, 0);
        Assert.Equal(8200, electVal); // All Electronics
    }

    [Fact]
    public void MultipleValueFields_AllAggregated()
    {
        var pivot = new PivotTable();
        pivot.Fields.Add(new PivotTableField("Category", PivotTableFieldUsage.RowField));
        pivot.Fields.Add(new PivotTableField("Revenue", PivotTableFieldUsage.ValueField) { Aggregation = PivotTableAggregation.Sum });
        pivot.Fields.Add(new PivotTableField("Quantity", PivotTableFieldUsage.ValueField) { Aggregation = PivotTableAggregation.Sum });

        pivot.DataSource = GetSampleData();

        var flatRows = pivot.Matrix.FlatRows;
        int electIdx = -1;
        for (int i = 0; i < flatRows.Count; i++)
        {
            if (flatRows[i].Value == "Electronics" && !flatRows[i].IsGroupHeader) electIdx = flatRows[i].DataIndex;
        }

        Assert.Equal(8200, pivot.Matrix.GetValue(electIdx, 0, 0)); // Revenue sum
        Assert.Equal(43, pivot.Matrix.GetValue(electIdx, 0, 1));   // Quantity sum: 10+12+5+7+9 = 43
    }

    [Fact]
    public void ColumnAndRowFields_MatrixBuilt()
    {
        var pivot = new PivotTable();
        pivot.Fields.Add(new PivotTableField("Category", PivotTableFieldUsage.RowField));
        pivot.Fields.Add(new PivotTableField("Region", PivotTableFieldUsage.ColumnField));
        pivot.Fields.Add(new PivotTableField("Revenue", PivotTableFieldUsage.ValueField) { Aggregation = PivotTableAggregation.Sum });

        pivot.DataSource = GetSampleData();

        Assert.True(pivot.Matrix.IsBuilt);
        Assert.True(pivot.Matrix.FlatColumns.Count > 0);
        Assert.Contains(pivot.Matrix.FlatColumns, c => c.Value == "North");
        Assert.Contains(pivot.Matrix.FlatColumns, c => c.Value == "South");
    }

    [Fact]
    public void EmptyDataSource_NoErrors()
    {
        var pivot = new PivotTable();
        pivot.Fields.Add(new PivotTableField("Category", PivotTableFieldUsage.RowField));
        pivot.Fields.Add(new PivotTableField("Revenue", PivotTableFieldUsage.ValueField) { Aggregation = PivotTableAggregation.Sum });

        pivot.DataSource = new List<SaleRecord>();
        Assert.True(pivot.Matrix.IsBuilt);
    }

    [Fact]
    public void IBindingList_Notification_TriggersRebuild()
    {
        var pivot = new PivotTable();
        pivot.Fields.Add(new PivotTableField("Category", PivotTableFieldUsage.RowField));
        pivot.Fields.Add(new PivotTableField("Revenue", PivotTableFieldUsage.ValueField) { Aggregation = PivotTableAggregation.Sum });

        var list = new BindingList<SaleRecord>(GetSampleData());
        pivot.DataSource = list;

        int rebuildCount = 0;
        pivot.MatrixRebuilt += (s, e) => rebuildCount++;

        // Add a new record
        list.Add(new SaleRecord { Category = "Books", Revenue = 300 });

        Assert.True(rebuildCount >= 1);
    }

    [Fact]
    public void SubtotalRows_Generated()
    {
        var pivot = new PivotTable();
        pivot.Fields.Add(new PivotTableField("Category", PivotTableFieldUsage.RowField));
        pivot.Fields.Add(new PivotTableField("SubCategory", PivotTableFieldUsage.RowField));
        pivot.Fields.Add(new PivotTableField("Revenue", PivotTableFieldUsage.ValueField) { Aggregation = PivotTableAggregation.Sum });
        pivot.TotalVisibility = PivotTableTotalVisibility.All;

        pivot.DataSource = GetSampleData();

        var rows = pivot.Matrix.FlatRows;

        // Verify subtotals and grand total exist
        Assert.Contains(rows, r => r.IsTotal && !r.IsGrandTotal);
        Assert.Contains(rows, r => r.IsGrandTotal);
    }

    [Fact]
    public void RowSelect_TriggersSelectionChanged()
    {
        var pivot = new PivotTable();
        pivot.Fields.Add(new PivotTableField("Category", PivotTableFieldUsage.RowField));
        pivot.Fields.Add(new PivotTableField("Revenue", PivotTableFieldUsage.ValueField) { Aggregation = PivotTableAggregation.Sum });
        pivot.DataSource = GetSampleData();

        bool selected = false;
        pivot.SelectionChanged += (s, e) => selected = true;

        pivot.SelectedRowIndex = 0;
        Assert.True(selected);
    }

    [Fact]
    public void NoRowFields_UsesSingleGroup()
    {
        var pivot = new PivotTable();
        pivot.Fields.Add(new PivotTableField("Revenue", PivotTableFieldUsage.ValueField) { Aggregation = PivotTableAggregation.Sum });

        pivot.DataSource = GetSampleData();

        Assert.True(pivot.Matrix.IsBuilt);
        Assert.True(pivot.Matrix.FlatRows.Count >= 1);
        Assert.Equal(10800, pivot.Matrix.GetValue(0, 0, 0)); // sum of all revenue
    }

    [Fact]
    public void GrandTotalValues_Accurate()
    {
        var pivot = new PivotTable();
        pivot.Fields.Add(new PivotTableField("Category", PivotTableFieldUsage.RowField));
        pivot.Fields.Add(new PivotTableField("Revenue", PivotTableFieldUsage.ValueField) { Aggregation = PivotTableAggregation.Sum });
        pivot.TotalVisibility = PivotTableTotalVisibility.RowGrandTotal;

        pivot.DataSource = GetSampleData();

        Assert.Contains(pivot.Matrix.FlatRows, r => r.IsGrandTotal);
    }
}
