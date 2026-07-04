# DataGridView Control

The `DataGridView` control provides a flexible, high-performance grid for displaying and editing tabular data. It supports data binding, sorting, grouping, inline cell editing, and multiple cell editor types.

## Namespace

`OldSchoolForms.Ui.Controls.Advanced`

## Basic Usage

```csharp
var grid = new DataGridView();
grid.Columns.Add(new DataGridViewColumn
{
    HeaderText = "Name",
    DataPropertyName = "Name",
    Width = 150,
    CellEditType = DataGridViewColumnEditType.TextBox
});
grid.Columns.Add(new DataGridViewColumn
{
    HeaderText = "Age",
    DataPropertyName = "Age",
    Width = 80,
    CellEditType = DataGridViewColumnEditType.TextBox
});
grid.Rows.Add(new DataGridViewRow());
grid.Rows[0].Cells.Add(new DataGridViewCell { Value = "Alice" });
grid.Rows[0].Cells.Add(new DataGridViewCell { Value = 30 });
```

## Data Binding

Bind to any `IEnumerable` or `IBindingList` (e.g., `List<T>`, `BindingSource`):

```csharp
var items = new List<Person>
{
    new Person { Name = "Alice", Age = 30 },
    new Person { Name = "Bob", Age = 25 }
};
grid.DataSource = items;
```

When binding, columns with a non-empty `DataPropertyName` automatically read values from the corresponding property on each data item. Changes made through cell editing are written back to the source properties. If the source implements `IBindingList`, the grid stays synchronized with add/remove/change operations.

### BindingSource Integration

When a `BindingSource` is used as `DataSource`, selecting a row automatically updates `BindingSource.Position`. Conversely, the grid also integrates with `Form.BindingContext` / `CurrencyManager` for position tracking.

## Columns

| Property | Type | Default | Description |
|---|---|---|---|
| `Name` | `string` | `""` | Column name |
| `HeaderText` | `string` | `""` | Column header display text |
| `DataPropertyName` | `string` | `""` | Property name on data-bound objects |
| `Width` | `int` | `100` | Column width in pixels |
| `ReadOnly` | `bool` | `false` | Prevents editing of cells in this column |
| `Resizable` | `bool` | `true` | Whether the user can resize the column |
| `Sortable` | `bool` | `true` | Whether clicking the header sorts by this column |
| `Groupable` | `bool` | `true` | Whether this column can be used for grouping |
| `CellEditType` | `DataGridViewColumnEditType` | `None` | Type of editing control |
| `ValueType` | `Type?` | `null` | Type of values in the column |
| `TextAlign` | `DataGridViewContentAlignment` | `Left` | Horizontal alignment of cell content |
| `FormatString` | `string?` | `null` | .NET format string (e.g., `"N2"`, `"d"`, `"C"`) |
| `Items` | `List<object>?` | `null` | Drop-down items for ComboBox columns |
| `ComboBoxDropDownStyle` | `DropDownStyle` | `DropDownList` | ComboBox drop-down style |
| `TrueValue` / `FalseValue` | `object?` | `null` | Custom true/false values for CheckBox columns |
| `PickerFormat` | `DateTimePickerFormat` | `Short` | Date/time picker format |
| `PickerCustomFormat` | `string?` | `null` | Custom format string for DateTimePicker |
| `ButtonText` | `string?` | `null` | Static text for Button cells (falls back to cell value) |
| `ButtonIcon` | `IGraphicsImage?` | `null` | SVG icon for Button cells |

## Cell Editor Types

| Value | Editing Control | Behavior |
|---|---|---|
| `None` | — | Display-only cells |
| `TextBox` | `TextBox` | Free-text entry |
| `ComboBox` | `ComboBox` | Selection from a drop-down list (`Items`) |
| `CheckBox` | — | Inline toggle; no persistent editor (toggles on click/Space) |
| `DateTimePicker` | `DateTimePicker` | Date/time picker with format support |
| `Button` | — | Clickable button; fires `CellButtonClick` event |

## Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `Columns` | `DataGridViewColumnCollection` | — | Column collection |
| `Rows` | `DataGridViewRowCollection` | — | Row collection |
| `SelectedRowIndex` | `int` | `-1` | Index of the selected row |
| `SelectedColumnIndex` | `int` | `-1` | Index of the selected column |
| `SelectedRow` | `DataGridViewRow?` | — | Currently selected row |
| `SelectedValue` | `object?` | — | Value of the selected cell |
| `AllowUserToAddRows` | `bool` | `true` | Show new-row placeholder and allow row addition |
| `AllowUserToDeleteRows` | `bool` | `true` | Allow row deletion |
| `ReadOnly` | `bool` | `false` | Prevents all cell editing |
| `MultiSelect` | `bool` | `false` | Enable multi-selection |
| `ColumnHeadersVisible` | `bool` | `true` | Show/hide column headers |
| `RowHeadersVisible` | `bool` | `true` | Show/hide row number headers |
| `ShowGridLines` | `bool` | `true` | Show/hide grid lines |
| `SelectionMode` | `DataGridViewSelectionMode` | `RowHeaderSelect` | Selection behavior |
| `DataSource` | `object?` | `null` | Data source for automatic row population |
| `DataMember` | `string` | `""` | Data member name |
| `IsCurrentCellInEditMode` | `bool` | — | Whether a cell is actively being edited |
| `EditMode` | `DataGridViewEditMode` | `EditOnEnter` | How editing is triggered |
| `ShowGroupingBar` | `bool` | `false` | Show the grouping bar above column headers |
| `GroupingBarHeight` | `int` | `30` | Height of the grouping bar |
| `GroupHeaderFont` | `Font` | `Font.Default` | Font for group headers |
| `GroupHeaderForeColor` | `Color` | theme-dependent | Foreground color for group headers |
| `GroupHeaderBackColor` | `Color` | theme-dependent | Background color for group headers |
| `GroupHeaderTextAlign` | `DataGridViewContentAlignment` | `Left` | Text alignment for group headers |
| `GroupHeaderHeight` | `int` | `30` | Height of group header rows |
| `GroupHeaderIndent` | `int` | `20` | Indentation per group level |
| `GroupedColumnIndices` | `IReadOnlyList<int>` | — | Column indices currently used for grouping |
| `GroupRoots` | `IReadOnlyList<DataGridViewGroup>` | — | Root group nodes |

## Selection Modes

| Value | Description |
|---|---|
| `RowHeaderSelect` | Clicking a row header selects the row |
| `ColumnHeaderSelect` | Clicking a column header selects the column |
| `FullRowSelect` | Clicking any cell selects the entire row |
| `FullColumnSelect` | Clicking any cell selects the entire column |
| `CellSelect` | Only the clicked cell is selected |

## Edit Modes

| Value | Description |
|---|---|
| `EditOnEnter` | Editing starts when the cell receives focus (via click or keyboard navigation) |
| `EditOnF2` | Editing starts on F2 key press or when typing a printable character |
| `EditProgrammatically` | Editing only starts when `BeginEdit()` is called programmatically |

## Events

| Event | Args | Description |
|---|---|---|
| `SelectionChanged` | `EventArgs` | Fires when the selected row or column changes |
| `CellClick` | `DataGridViewCellEventArgs` | Fires when a cell is clicked |
| `CellButtonClick` | `DataGridViewCellEventArgs` | Fires when a button cell is clicked |
| `CellValueChanged` | `DataGridViewCellEventArgs` | Fires when a cell value has been committed |
| `ColumnHeaderMouseClick` | `DataGridViewCellEventArgs` | Fires when a column header is clicked |
| `CellBeginEdit` | `DataGridViewCellEventArgs` | Fires when a cell enters edit mode |
| `CellEndEdit` | `DataGridViewCellEventArgs` | Fires when a cell exits edit mode |
| `CellFormatting` | `DataGridViewCellFormattingEventArgs` | Fires when a cell value needs formatting for display |
| `CellParsing` | `DataGridViewCellParsingEventArgs` | Fires when an editor value is parsed back to the cell |
| `DataError` | `DataGridViewDataErrorEventArgs` | Fires on data conversion errors during editing |
| `GroupHeaderFormatting` | `DataGridViewGroupHeaderFormattingEventArgs` | Fires when a group header is being rendered |

## Cell Value Formatting

Handle the `CellFormatting` event to customize how cell values are displayed:

```csharp
grid.CellFormatting += (s, e) =>
{
    if (e.ColumnIndex == 2 && e.Value is DateTime dt)
    {
        e.Value = dt.ToString("yyyy-MM-dd");
        e.FormattingApplied = true;
    }
};
```

## Cell Value Parsing

Handle the `CellParsing` event to customize how editor values are converted back to cell values:

```csharp
grid.CellParsing += (s, e) =>
{
    if (e.ColumnIndex == 1 && e.DesiredType == typeof(int))
    {
        if (int.TryParse(e.RawValue?.ToString(), out int result))
        {
            e.Value = result;
            e.ParsingApplied = true;
        }
    }
};
```

## Data Error Handling

Handle the `DataError` event to manage invalid cell values gracefully:

```csharp
grid.DataError += (s, e) =>
{
    // Log the error, show custom message, or suppress the default dialog
    e.Handled = true;
};
```

## Sorting

Columns with `Sortable = true` can be sorted by clicking the column header. Clicking the same header toggles between ascending and descending order. The sort arrow indicator is rendered in the column header.

```csharp
grid.Columns[0].Sortable = true;
```

## Grouping

DataGridView supports multi-level grouping. Columns must have `Groupable = true` (default). Columns can be dragged into the grouping bar at runtime, or grouped programmatically:

```csharp
// Programmatic grouping
grid.AddGroupColumn(0); // Group by column index 0
grid.AddGroupColumn(1); // Then by column index 1 (secondary group level)
grid.RemoveGroupColumn(0); // Remove a grouping level by its index in the group list
grid.ClearGrouping(); // Remove all grouping
```

When grouping is active, the grid displays collapsible group headers. Groups can be collapsed/expanded by clicking the header row. The grouping bar at the top shows active group pills with a remove (×) button.

### Group Header Customization

Handle the `GroupHeaderFormatting` event to customize group header appearance:

```csharp
grid.GroupHeaderFormatting += (s, e) =>
{
    if (e.Level == 0 && e.ColumnIndex == 0)
    {
        e.HeaderText = $"Department: {e.GroupValue} ({e.GroupItemCount} employees)";
        e.ForeColor = Color.FromArgb(0, 0, 180);
        e.Font = new Font("Arial", 10, FontStyle.Bold);
    }
};
```

## Keyboard Navigation

| Key | Action |
|---|---|
| Arrow keys | Navigate between cells |
| Tab / Shift+Tab | Move to next/previous cell (wraps rows) |
| Enter | Commit edit, move to next cell |
| Escape | Cancel edit, restore original value |
| Home | Go to first row |
| End | Go to last row |
| Page Up / Page Down | Scroll one page up/down |
| F2 | Start editing current cell |
| Space | Toggle CheckBox or open DateTimePicker dropdown |
| Ctrl+C | Copy selected cell value to clipboard |

## Methods

| Method | Description |
|---|---|
| `AddRow(params object[] values)` | Add a new row with cell values |
| `AddGroupColumn(int columnIndex)` | Add a column as a grouping level |
| `RemoveGroupColumn(int levelIndex)` | Remove a grouping level by its index |
| `ClearGrouping()` | Remove all grouping |
| `BeginEdit()` | Programmatically start editing the current cell |
| `EndEdit(bool commit)` | End the current cell edit, optionally committing the value |
| `Copy()` | Copy the selected cell value to clipboard |
| `ClearSort()` | Clear all column sort orders |

## Drag-to-Group

When the grouping bar is visible (`ShowGroupingBar = true`), users can drag column headers into the grouping bar to create groups. Pill-shaped indicators appear in the grouping bar. Dragging a pill out of the bar removes that grouping level. A drag indicator follows the mouse cursor showing which column is being dragged.
