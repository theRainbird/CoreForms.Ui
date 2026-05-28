# KanbanBoardView Control

The `KanbanBoardView` control displays data items as cards organized in configurable columns, following the Kanban board pattern. It supports data binding, drag-and-drop between columns, drag reordering within columns, keyboard navigation, configurable state transitions, and rich card visualization with categories, tags, and assignee information.

## Namespace

`CoreForms.Ui.Controls.Advanced`

## Basic Usage

```csharp
var kanban = new KanbanBoardView();

// Define columns
kanban.Columns.Add(new KanbanBoardColumn
{
    Name = "ToDo",
    StateValue = "ToDo",
    HeaderColor = Color.FromArgb(80, 80, 80)
});
kanban.Columns.Add(new KanbanBoardColumn
{
    Name = "Done",
    StateValue = "Done",
    HeaderColor = Color.FromArgb(50, 150, 50)
});

// Bind data
kanban.DataSource = tasks;
kanban.DisplayMember = "Title";
kanban.StateMember = "State";

// Add to form
form.Controls.Add(kanban);
```

## Data Binding

Bind to any `IEnumerable` or `IBindingList` (e.g., `List<T>`, `BindingSource`):

```csharp
public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }
    public string AssignedTo { get; set; }
    public string State { get; set; }
    public List<string> Tags { get; set; }
}

var tasks = new List<TaskItem>
{
    new TaskItem { Id = 1, Title = "Design login", State = "ToDo" },
    new TaskItem { Id = 2, Title = "Fix bug", State = "Done" },
};

kanban.DataSource = tasks;
kanban.DisplayMember = "Title";
kanban.DescriptionMember = "Description";
kanban.ValueMember = "Id";
kanban.StateMember = "State";
```

When the data source implements `IBindingList`, the board stays synchronized with add/remove/change operations. Cards are automatically placed in the column whose `StateValue` matches the item's `StateMember` property value.

## Column Configuration

Each column is defined by a `KanbanBoardColumn`:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Name` | `string` | `""` | Display name shown in the column header |
| `StateValue` | `object?` | `null` | Value that identifies this column; cards whose `StateMember` matches are placed here |
| `BackColor` | `Color` | Theme default | Background color of the column body |
| `HeaderColor` | `Color` | Theme default | Background color of the column header |

Columns are added to the `KanbanBoardColumnCollection` accessed via the `Columns` property:

```csharp
kanban.Columns.Add(new KanbanBoardColumn { Name = "ToDo", StateValue = "ToDo", HeaderColor = Color.FromArgb(80, 80, 80) });
kanban.Columns.Clear();
kanban.Columns.Remove(column);
```

### Column Width

Columns are automatically sized to fill the available width equally, regardless of the `Width` property. When the board is resized, columns redistribute proportionally.

## Properties

### Data Binding

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `DataSource` | `object?` | `null` | Data source (`BindingSource`, `IBindingList`, or `IEnumerable`) |
| `DisplayMember` | `string` | `""` | Property name for the card title (displayed in bold) |
| `DescriptionMember` | `string` | `""` | Property name for the card description text |
| `ValueMember` | `string` | `""` | Property name for the card value (displayed as `#value`) |
| `StateMember` | `string` | `""` | Property name for the card state; determines which column the card appears in |
| `TagMember` | `string` | `""` | Property name for tags (must return `IEnumerable<string>`) |
| `CategoryMember` | `string` | `""` | Property name for the category string |
| `AssignedToMember` | `string` | `""` | Property name for the assignee string |

### Behavior

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `AllowDrop` | `bool` | `true` | Whether drag-and-drop is allowed globally |
| `AllowReorder` | `bool` | `true` | Whether reordering cards within a column is allowed |
| `AllowedTransitions` | `List<(object?, object?)>` | empty | List of allowed (FromState, ToState) pairs for drag moves. Empty list = all transitions allowed |
| `CategoryColors` | `Dictionary<string, Color>` | empty | Mapping of category names to chip background colors |
| `SelectedCard` | `KanbanBoardCard?` | `null` | The currently selected card (read-only) |
| `FocusedCard` | `KanbanBoardCard?` | `null` | The currently focused card (read-only) |

### Allowed Transitions

Restrict which column changes are permitted via drag-and-drop:

```csharp
// Only allow forward moves in the workflow
kanban.AllowedTransitions.Add(("ToDo", "InProgress"));
kanban.AllowedTransitions.Add(("InProgress", "Review"));
kanban.AllowedTransitions.Add(("Review", "Done"));

// Also allow moving backward
kanban.AllowedTransitions.Add(("InProgress", "ToDo"));
```

When a move is attempted that doesn't match any entry in `AllowedTransitions`, the drag operation is rejected (no visual drop target). The `CardMoving` event can also cancel moves programmatically (see Events below).

### Category Colors

```csharp
kanban.CategoryColors["Bug"] = Color.FromArgb(220, 50, 50);
kanban.CategoryColors["Feature"] = Color.FromArgb(50, 150, 50);
kanban.CategoryColors["Task"] = Color.FromArgb(50, 100, 200);
```

## Events

| Event | EventArgs | Cancelable | Description |
|-------|-----------|-----------|-------------|
| `CardGotFocus` | `KanbanBoardCardEventArgs` | No | A card received focus (mouse or keyboard) |
| `CardClick` | `KanbanBoardCardEventArgs` | No | A card was clicked |
| `CardDoubleClick` | `KanbanBoardCardEventArgs` | No | A card was double-clicked |
| `CardMoving` | `KanbanBoardCardMovingEventArgs` | Yes | Before a card is moved to a different column |
| `CardMoved` | `KanbanBoardCardMovingEventArgs` | No | After a card has been moved to a different column |
| `CardReordering` | `KanbanBoardCardReorderingEventArgs` | Yes | Before a card is reordered within its column |
| `CardReordered` | `KanbanBoardCardReorderingEventArgs` | No | After a card has been reordered within its column |

### Event example: blocking moves

```csharp
kanban.CardMoving += (s, e) =>
{
    // Prevent Charlie's tasks from entering "Done"
    if (e.Card.AssignedToText == "Charlie" && e.ToColumn.StateValue as string == "Done")
        e.Cancel = true;
};
```

### Event example: logging moves

```csharp
kanban.CardMoved += (s, e) =>
{
    Console.WriteLine($"{e.Card.DisplayText}: {e.FromColumn.Name} -> {e.ToColumn.Name}");
};
```

## EventArgs Types

All event arg types are in `CoreForms.Ui.Controls.Advanced`:

| Type | Properties |
|------|-----------|
| `KanbanBoardCardEventArgs` | `Card` |
| `KanbanBoardCardCancelEventArgs` | `Card`, `Cancel` (inherited) |
| `KanbanBoardCardMovingEventArgs` | `Card`, `FromColumn`, `ToColumn`, `NewIndex`, `Cancel` |
| `KanbanBoardCardReorderingEventArgs` | `Card`, `Column`, `OldIndex`, `NewIndex`, `Cancel` |

## Card Layout

Each card displays the following information (only fields with data are shown):

```
┌──────────────────────────┐
│ Title (bold)             │  ← DisplayMember
│ Description text         │  ← DescriptionMember
│ [Feature]                │  ← CategoryMember (colored chip)
│ [Urgent] [Frontend]      │  ← TagMember (gray chips)
│ 👤 Alice                 │  ← AssignedToMember
│ ─────────────────────── │
│ #42                      │  ← ValueMember
└──────────────────────────┘
```

## Keyboard Navigation

| Key | Action |
|-----|--------|
| Left Arrow | Move focus to previous column |
| Right Arrow | Move focus to next column |
| Up Arrow | Move focus to previous card (or wrap to previous column) |
| Down Arrow | Move focus to next card (or wrap to next column) |
| Tab | Move focus to next card |
| Shift+Tab | Move focus to previous card |
| Enter / Space | Click the focused card |
| Home | Move focus to the first card |
| End | Move focus to the last card |

## Drag & Drop

- Cards can be dragged between columns by clicking and dragging. A ghost card follows the cursor during drag.
- Drop targets are highlighted when a valid drop is detected.
- Within the same column, cards can be reordered by dragging to a new position (requires `AllowReorder = true`).
- The `AllowedTransitions` list and the `CardMoving` event can both prevent moves.
- The mouse is captured during drag operations via `CapturingMouse`.

## Data Write-Back

When a card is successfully moved to a different column, the `StateMember` property on the underlying data item is automatically updated to the destination column's `StateValue`:

```csharp
// If StateMember = "State" and card is moved to the "Done" column:
item.State == "Done";  // true after move
```

## Theme Support

The control respects the active theme via `OnThemeChanged`. Column colors fall back to theme defaults when not explicitly set. The control uses:
- `WindowBackground` for the board background
- `ControlBackground` / `Highlight` for column defaults
- `ControlText` for card text
- `FocusIndicator` for focused card borders
- `Highlight` for drop target highlights
- `GrayText` for the value footer

## KanbanBoardCard

The `KanbanBoardCard` class wraps a data item and caches display values:

| Property | Type | Description |
|----------|------|-------------|
| `DataItem` | `object` | The underlying data source item |
| `DisplayText` | `string` | Computed title from `DisplayMember` |
| `DescriptionText` | `string` | Computed description from `DescriptionMember` |
| `CategoryText` | `string` | Computed category from `CategoryMember` |
| `AssignedToText` | `string` | Computed assignee from `AssignedToMember` |
| `Tags` | `IReadOnlyList<string>` | Computed tags from `TagMember` |
| `StateValue` | `object?` | The state value determining column placement |
| `Value` | `object?` | The value from `ValueMember` |
