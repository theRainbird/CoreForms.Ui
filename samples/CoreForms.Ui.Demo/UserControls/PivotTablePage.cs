using CoreForms.Ui.Controls;
using CoreForms.Ui.Controls.Advanced;
using CoreForms.Ui.Core;
using CoreForms.Ui.Theming;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Demo.UserControls;

public class PivotTablePage : UserControl
{
    private readonly PivotTable _pivotTable;

    public PivotTablePage()
    {
        _pivotTable = new PivotTable
        {
            Dock = DockStyle.Fill,
            Name = "pivotTable"
        };

        BuildSampleData();
        Controls.Add(_pivotTable);
    }

    private void BuildSampleData()
    {
        var data = new List<SaleRecord>();
        var rng = new Random(42);
        string[] categories = ["Electronics", "Clothing", "Food", "Books"];
        string[][] subCategories =
        [
            ["Phones", "Laptops", "Tablets"],
            ["Shirts", "Pants", "Shoes"],
            ["Fruits", "Dairy", "Bakery"],
            ["Fiction", "Non-Fiction", "Science"]
        ];
        string[] regions = ["North", "South", "East", "West"];
        string[] quarters = ["2024-Q1", "2024-Q2", "2024-Q3", "2024-Q4"];

        for (int ci = 0; ci < categories.Length; ci++)
        {
            for (int si = 0; si < subCategories[ci].Length; si++)
            {
                foreach (var region in regions)
                {
                    foreach (var quarter in quarters)
                    {
                        data.Add(new SaleRecord
                        {
                            Category = categories[ci],
                            SubCategory = subCategories[ci][si],
                            Region = region,
                            Quarter = quarter,
                            Revenue = rng.Next(100, 5000),
                            Quantity = rng.Next(1, 50)
                        });
                    }
                }
            }
        }

        _pivotTable.Fields.Add(new PivotTableField(SR.GetString("PtCategory"), "Category", PivotTableFieldUsage.RowField));
        _pivotTable.Fields.Add(new PivotTableField(SR.GetString("PtSubCategory"), "SubCategory", PivotTableFieldUsage.RowField));
        _pivotTable.Fields.Add(new PivotTableField(SR.GetString("PtRegion"), "Region", PivotTableFieldUsage.ColumnField));
        _pivotTable.Fields.Add(new PivotTableField(SR.GetString("PtRevenue"), "Revenue", PivotTableFieldUsage.ValueField)
        {
            Aggregation = PivotTableAggregation.Sum,
            FormatString = "C0"
        });
        _pivotTable.Fields.Add(new PivotTableField(SR.GetString("PtQuantity"), "Quantity", PivotTableFieldUsage.ValueField)
        {
            Aggregation = PivotTableAggregation.Sum,
            FormatString = "N0"
        });
        _pivotTable.Fields.Add(new PivotTableField(SR.GetString("PtQuarter"), "Quarter", PivotTableFieldUsage.FilterField));

        _pivotTable.DataSource = data;
        _pivotTable.ExpandAll();
    }

    public override void Render(Graphics g)
    {
        var theme = ThemeManager.CurrentTheme;
        g.FillRectangle(theme.WindowBackground, X, Y, Width, Height);
        base.Render(g);
    }

    private class SaleRecord
    {
        public string Category { get; set; } = string.Empty;
        public string SubCategory { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string Quarter { get; set; } = string.Empty;
        public double Revenue { get; set; }
        public int Quantity { get; set; }
    }
}
