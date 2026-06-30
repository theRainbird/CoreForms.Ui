# PivotTable Control

The `PivotTable` control provides an interactive cross-tabulation (pivot) grid that dynamically aggregates data across row and column dimensions. It supports hierarchical drilldown, multiple aggregation types, subtotals, grand totals, filter fields, and drill-through to detail records.

## Namespace

`CoreForms.Ui.Controls.Advanced`

## Basic Usage

```csharp
var pivot = new PivotTable();

// Define dimensions and measures
pivot.Fields.Add(new PivotTableField("Category", PivotTableFieldUsage.RowField));
pivot.Fields.Add(new PivotTableField("Region", PivotTableFieldUsage.ColumnField));
pivot.Fields.Add(new PivotTableField("Revenue", PivotTableFieldUsage.ValueField)
{
    Aggregation = PivotTableAggregation.Sum,
    FormatString = "C2"
});
pivot.Fields.Add(new PivotTableField("Quarter", PivotTableFieldUsage.FilterField));

// Bind data
pivot.DataSource = salesData;
```

## Field Definitions

Fields define how the source data is sliced and aggregated. Each field has a `DataPropertyName` that maps to a property on the data source objects.

### FieldUsage

| Value | Description |
|---|---|
| `RowField` | Dimension placed on the row axis. Multiple row fields create a hierarchy with drilldown. |
| `ColumnField` | Dimension placed on the column axis. Values become column headers. |
| `ValueField` | A measure that is aggregated in the data cells. |
| `FilterField` | A slicer that filters the entire pivot table. Set values via `SetFilter()`. |

### PivotTableField Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `Name` | `string` | `""` | Display name shown in headers |
| `DataPropertyName` | `string` | `""` | Property name on data source objects |
| `Usage` | `PivotTableFieldUsage` | — | How the field is used (row, column, value, filter) |
| `Aggregation` | `PivotTableAggregation` | `None` | Aggregation function for value fields |
| `FormatString` | `string?` | `null` | .NET format string (e.g., `"C2"`, `"N0"`) |
| `ShowAsPercentageOfRow` | `bool` | `false` | Show values as a percentage of the row total |
| `ShowAsPercentageOfColumn` | `bool` | `false` | Show values as a percentage of the column total |
| `ShowAsPercentageOfGrandTotal` | `bool` | `false` | Show values as a percentage of the grand total |
| `IsExpanded` | `bool` | `true` | Default drilldown state for this field level |
| `Width` | `int` | `120` | Preferred width in pixels |
| `Visible` | `bool` | `true` | Whether the field is visible |
| `Order` | `int` | `0` | Display order (lower values appear first) |

### Aggregation Types

| Value | Description |
|---|---|
| `None` | No aggregation (uses the first value in each cell) |
| `Sum` | Sum of values |
| `Count` | Count of records |
| `Average` | Average of values |
| `Min` | Minimum value |
| `Max` | Maximum value |

## Data Binding

Bind to any `IEnumerable` or `IBindingList`:

```csharp
var data = new List<SaleRecord>
{
    new() { Category = "Electronics", Region = "North", Revenue = 1000, Quarter = "2024-Q1" },
    new() { Category = "Clothing",    Region = "South", Revenue = 500,  Quarter = "2024-Q1" }
};
pivot.DataSource = data;
```

The control reads property values via reflection based on `DataPropertyName`. When the source implements `IBindingList`, changes (add, remove, update) trigger an automatic matrix rebuild.

## Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `Fields` | `PivotTableFieldCollection` | — | Field definition collection |
| `DataSource` | `object?` | `null` | Data source (`IEnumerable`, `IBindingList`, `BindingSource`) |
| `DataMember` | `string` | `""` | Property path when the data source contains multiple lists |
| `LayoutMode` | `PivotTableLayoutMode` | `Compact` | Visual layout style for row fields |
| `TotalVisibility` | `PivotTableTotalVisibility` | `All` | Which totals are shown |
| `DrillState` | `IReadOnlyDictionary<string, bool>` | — | Current drilldown state (path → expanded) |
| `SelectedRowIndex` | `int` | `-1` | Selected data row index |
| `SelectedColumnIndex` | `int` | `-1` | Selected data column index |

### Layout Modes

| Value | Description |
|---|---|
| `Compact` | Row fields are nested in a single column with indentation |
| `Outline` | Each row field level can have its own column with subtotals above groups |
| `Tabular` | Each row field level has its own column, no indentation |

### Total Visibility Flags

| Value | Description |
|---|---|
| `None` | No totals |
| `RowGrandTotal` | Show a grand total row |
| `ColumnGrandTotal` | Show a grand total column |
| `Subtotals` | Show subtotals at each group level |
| `All` | Show all totals (default) |

### Color Properties

| Property | Type | Default (Light) | Description |
|---|---|---|---|
| `HeaderBackgroundColor` | `Color` | `(240,240,240)` | Background for row/column header cells |
| `HeaderTextColor` | `Color` | `(0,0,0)` | Text color for headers |
| `AlternateRowColor` | `Color` | `(245,245,250)` | Alternating row background |
| `TotalBackgroundColor` | `Color` | `(230,235,245)` | Background for total rows/columns |
| `GridLineColor` | `Color` | `(200,200,200)` | Grid line color |
| `DrillButtonColor` | `Color` | `(80,80,80)` | Drilldown (+/-) button color |
| `SelectedCellColor` | `Color` | `(200,220,255)` | Selected cell background |
| `ShowGridLines` | `bool` | `true` | Show/hide grid lines |

All color properties are theme-aware. When the theme changes, unset colors update automatically.

## Data Cell Values

Numeric values are right-aligned by default. Column headers, row headers, and value sub-headers are center-aligned. Row header labels are left-aligned.

## Drilldown

When multiple `RowField` values are defined, a hierarchy is created. Each level can be expanded or collapsed by clicking the (+/-) button next to the group header.

```csharp
// Programmatic control
pivot.ExpandAll();
pivot.CollapseAll();

// Read current state
var state = pivot.DrillState;
```

### Drilldown Events

```csharp
pivot.DrilldownToggled += (s, e) =>
{
    Console.WriteLine($"Field {e.RowFieldIndex}, Path '{e.NodePath}' is now {(e.IsExpanded ? "expanded" : "collapsed")}");
};
```

## Drill-Through

Double-clicking a data cell fires the `DrillThrough` event with the detail records that make up the aggregated value:

```csharp
pivot.DrillThrough += (s, e) =>
{
    // e.DetailRecords contains the source objects that contributed to this cell
    ShowDetailPopup(e.DetailRecords);
    e.Handled = true; // suppress default behavior
};
```

## Filtering

Filter fields act as slicers. Set filter values programmatically:

```csharp
// Filter to show only "North" region
pivot.SetFilter("Region", "North");

// Remove the filter
pivot.SetFilter("Region", null);

// Clear all filters
pivot.ClearFilters();
```

## Column Resizing

Column widths can be adjusted by dragging the divider between column headers in the column header area. The cursor changes to a horizontal resize pointer when hovering over a divider.

## Events

| Event | Args | Description |
|---|---|---|
| `CellFormatting` | `PivotTableCellFormattingEventArgs` | Fires when a data cell value is formatted for display |
| `CellPainting` | `PivotTableCellPaintingEventArgs` | Fires when a data cell is painted (allows custom rendering) |
| `CellClick` | `PivotTableCellClickEventArgs` | Fires when a data cell is clicked |
| `DrilldownToggled` | `PivotTableDrilldownEventArgs` | Fires when a drilldown node is expanded or collapsed |
| `DrillThrough` | `PivotTableDrillThroughEventArgs` | Fires when a data cell is double-clicked |
| `SelectionChanged` | `EventArgs` | Fires when the selected row or column changes |
| `MatrixRebuilt` | `EventArgs` | Fires after the internal matrix is rebuilt |

### CellFormatting Event

```csharp
pivot.CellFormatting += (s, e) =>
{
    if (e.RawValue is double val && val > 10000)
    {
        e.FormattedValue = $"{val:N0} ★";
        e.FormattingApplied = true;
    }
};
```

### CellPainting Event

```csharp
pivot.CellPainting += (s, e) =>
{
    if (e.IsTotalCell)
    {
        // Draw a custom background for total cells
        e.Graphics.FillRectangle(Color.FromArgb(200, 220, 240), e.CellBounds);
        e.Graphics.DrawString(e.DisplayText ?? "", new Font("Arial", 10, FontStyle.Bold),
            Color.FromArgb(0, 0, 0), e.CellBounds.X + 4, e.CellBounds.Y + 2);
        e.Handled = true;
    }
};
```

## Keyboard Navigation

| Key | Action |
|---|---|
| Arrow keys | Navigate between data cells |
| Home | Go to first row |
| End | Go to last row |
| Page Up / Page Down | Scroll one page up/down |
| Enter | Toggle drilldown on selected group header |

## Example: Sales Pivot with Drilldown

```csharp
var pivot = new PivotTable();

// Row hierarchy: Category → SubCategory
pivot.Fields.Add(new PivotTableField("Category", PivotTableFieldUsage.RowField));
pivot.Fields.Add(new PivotTableField("SubCategory", PivotTableFieldUsage.RowField));

// Column dimension
pivot.Fields.Add(new PivotTableField("Region", PivotTableFieldUsage.ColumnField));

// Measures
pivot.Fields.Add(new PivotTableField("Revenue", "Revenue", PivotTableFieldUsage.ValueField)
{
    Aggregation = PivotTableAggregation.Sum,
    FormatString = "C0"
});
pivot.Fields.Add(new PivotTableField("Quantity", "Quantity", PivotTableFieldUsage.ValueField)
{
    Aggregation = PivotTableAggregation.Sum,
    FormatString = "N0"
});

// Filter
pivot.Fields.Add(new PivotTableField("Quarter", PivotTableFieldUsage.FilterField));

// Bind data
pivot.DataSource = salesRecords;
pivot.ExpandAll();
```

## Architecture Notes

The control uses an internal `PivotTableMatrix` engine that:

1. Groups source records by row fields (hierarchically) and column fields
2. Aggregates values for each (row, column) intersection
3. Computes subtotals per hierarchy level and grand totals
4. Maintains a drilldown state dictionary (node path → expanded/collapsed)
5. Rebuilds the matrix whenever fields or data source change

The matrix is rebuilt lazily (on the next render pass) when the data source changes via `IBindingList.ListChanged`.
