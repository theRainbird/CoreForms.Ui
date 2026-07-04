using OldSchoolForms.Ui.Controls;
using OldSchoolForms.Ui.Controls.Advanced;
using OldSchoolForms.Ui.Controls.Basic;
using OldSchoolForms.Ui.Controls.Containers;
using OldSchoolForms.Ui.Core;
using OldSchoolForms.Ui.Theming;
using Graphics = OldSchoolForms.Ui.Rendering.Graphics;

namespace OldSchoolForms.Ui.Demo.UserControls;

/// <summary>
/// Demonstrates the KanbanBoardView control with sample tasks, drag-and-drop, and category colors.
/// </summary>
public class KanbanBoardPage : UserControl
{
    private readonly KanbanBoardView _kanban;
    private readonly ListBox _logList;
    private readonly CheckBox _allowDropCheck;
    private readonly CheckBox _allowReorderCheck;
    private readonly Label _selectedCardLabel;
    private int _taskCounter;

    private sealed class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string AssignedTo { get; set; } = string.Empty;
        public string State { get; set; } = "ToDo";
        public List<string> Tags { get; set; } = new();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KanbanBoardPage"/> class.
    /// </summary>
    public KanbanBoardPage()
    {
        var theme = ThemeManager.CurrentTheme;

        _kanban = new KanbanBoardView
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 0, 6, 0),
            DisplayMember = "Title",
            DescriptionMember = "",
            ValueMember = "Id",
            StateMember = "State",
            TagMember = "Tags",
            CategoryMember = "Category",
            AssignedToMember = "AssignedTo",
        };

        _kanban.CategoryColors["Bug"] = Color.FromArgb(220, 50, 50);
        _kanban.CategoryColors["Feature"] = Color.FromArgb(50, 150, 50);
        _kanban.CategoryColors["Task"] = Color.FromArgb(50, 100, 200);
        _kanban.CategoryColors["Improvement"] = Color.FromArgb(200, 150, 50);

        _kanban.Columns.Add(new KanbanBoardColumn
        {
            Name = "ToDo",
            StateValue = "ToDo",
            HeaderColor = Color.FromArgb(80, 80, 80),
        });
        _kanban.Columns.Add(new KanbanBoardColumn
        {
            Name = "In Progress",
            StateValue = "InProgress",
            HeaderColor = Color.FromArgb(50, 100, 200),
        });
        _kanban.Columns.Add(new KanbanBoardColumn
        {
            Name = "Review",
            StateValue = "Review",
            HeaderColor = Color.FromArgb(200, 150, 50),
        });
        _kanban.Columns.Add(new KanbanBoardColumn
        {
            Name = "Done",
            StateValue = "Done",
            HeaderColor = Color.FromArgb(50, 150, 50),
        });

        // Allowed transitions: ToDo→InProgress, InProgress→Review, Review→Done, and backwards
        _kanban.AllowedTransitions.Add(("ToDo", "InProgress"));
        _kanban.AllowedTransitions.Add(("InProgress", "Review"));
        _kanban.AllowedTransitions.Add(("Review", "Done"));
        _kanban.AllowedTransitions.Add(("InProgress", "ToDo"));
        _kanban.AllowedTransitions.Add(("Review", "InProgress"));
        _kanban.AllowedTransitions.Add(("Done", "Review"));

        // Wire events
        _kanban.CardClick += OnCardClicked;
        _kanban.CardGotFocus += OnCardGotFocus;
        _kanban.CardDoubleClick += OnCardDoubleClicked;
        _kanban.CardMoving += OnCardMoving;
        _kanban.CardMoved += OnCardMoved;
        _kanban.CardReordering += OnCardReordering;
        _kanban.CardReordered += OnCardReordered;

        // Control panel (right side)
        var controlPanel = new GroupBox
        {
            Text = "Controls & Log",
        };

        _allowDropCheck = new CheckBox
        {
            Text = "Allow Drop",
            Checked = true,
            Location = new Point(10, 10),
            Size = new Size(110, 22),
        };
        _allowDropCheck.CheckedChanged += (_, _) => _kanban.AllowDrop = _allowDropCheck.Checked;

        _allowReorderCheck = new CheckBox
        {
            Text = "Allow Reorder",
            Checked = true,
            Location = new Point(10, 36),
            Size = new Size(110, 22),
        };
        _allowReorderCheck.CheckedChanged += (_, _) => _kanban.AllowReorder = _allowReorderCheck.Checked;

        var addTaskBtn = new Button
        {
            Text = "Add Task",
            Location = new Point(10, 68),
            Size = new Size(100, 28),
        };
        addTaskBtn.Click += (_, _) => AddSampleTask();

        var clearLogBtn = new Button
        {
            Text = "Clear Log",
            Location = new Point(120, 68),
            Size = new Size(100, 28),
        };
        clearLogBtn.Click += (_, _) => _logList.Items.Clear();

        _selectedCardLabel = new Label
        {
            Text = "Selected: none",
            Location = new Point(10, 104),
            Size = new Size(220, 20),
            ForeColor = theme.GrayText,
        };

        _logList = new ListBox
        {
            Location = new Point(10, 130),
            Size = new Size(220, 300),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
        };

        controlPanel.Controls.Add(_allowDropCheck);
        controlPanel.Controls.Add(_allowReorderCheck);
        controlPanel.Controls.Add(addTaskBtn);
        controlPanel.Controls.Add(clearLogBtn);
        controlPanel.Controls.Add(_selectedCardLabel);
        controlPanel.Controls.Add(_logList);

        _kanban.Dock = DockStyle.Fill;
        controlPanel.Dock = DockStyle.Right;
        controlPanel.Width = 250;

        Controls.Add(_kanban);
        Controls.Add(controlPanel);

        PerformLayout();

        // Populate sample tasks
        AddSampleTask("Design login page", "Create wireframe and implement login form", "Feature", "Alice", "ToDo", new[] { "UI", "Auth" });
        AddSampleTask("Fix navigation bug", "Menu doesn't close on click outside", "Bug", "Bob", "InProgress", new[] { "Urgent" });
        AddSampleTask("Add dark mode", "Implement dark theme toggle", "Feature", "Alice", "Review", new[] { "UI", "UX" });
        AddSampleTask("Write unit tests", "Add tests for data layer", "Task", "Charlie", "ToDo", new[] { "Testing" });
        AddSampleTask("Optimize queries", "Add indexes to frequently queried columns", "Improvement", "Bob", "Done", new[] { "Performance" });
        AddSampleTask("Update docs", "Update API documentation for v2", "Task", "Charlie", "Review", new[] { "Docs" });
        AddSampleTask("User feedback form", "Add feedback form to contact page", "Feature", "Alice", "ToDo", new[] { "UI", "Backend" });
        AddSampleTask("Fix memory leak", "Dispose event handlers in long-lived services", "Bug", "Bob", "InProgress", new[] { "Urgent", "Backend" });
    }

    private void AddSampleTask(string? title = null, string? desc = null, string? category = null,
        string? assignee = null, string? state = null, string[]? tags = null)
    {
        _taskCounter++;
        var item = new TaskItem
        {
            Id = _taskCounter,
            Title = title ?? $"Sample Task {_taskCounter}",
            Description = desc ?? $"Description for task {_taskCounter}",
            Category = category ?? "Task",
            AssignedTo = assignee ?? "Unassigned",
            State = state ?? "ToDo",
            Tags = tags?.ToList() ?? new List<string> { "New" },
        };

        var items = (_kanban.DataSource as List<TaskItem>) ?? new List<TaskItem>();
        items.Add(item);
        _kanban.DataSource = null;
        _kanban.DataSource = items;
    }

    private void AddSampleTask()
    {
        var categories = new[] { "Bug", "Feature", "Task", "Improvement" };
        var states = new[] { "ToDo", "InProgress", "Review", "Done" };
        var members = new[] { "Alice", "Bob", "Charlie" };

        AddSampleTask(
            title: $"Task {_taskCounter + 1}",
            desc: "Auto-generated task",
            category: categories[_taskCounter % categories.Length],
            assignee: members[_taskCounter % members.Length],
            state: states[_taskCounter % states.Length],
            tags: new[] { "Auto" }
        );
        Log($"Added task {_taskCounter}");
    }

    private void OnCardClicked(object? sender, KanbanBoardCardEventArgs e)
    {
        _selectedCardLabel.Text = $"Selected: {e.Card.DisplayText}";
        Log($"Click: {e.Card.DisplayText}");
    }

    private void OnCardGotFocus(object? sender, KanbanBoardCardEventArgs e)
    {
        Log($"Focus: {e.Card.DisplayText}");
    }

    private void OnCardDoubleClicked(object? sender, KanbanBoardCardEventArgs e)
    {
        Log($"Double-click: {e.Card.DisplayText}");
    }

    private void OnCardMoving(object? sender, KanbanBoardCardMovingEventArgs e)
    {
        Log($"Moving: {e.Card.DisplayText} from {e.FromColumn.Name} to {e.ToColumn.Name}");
        // Example: prevent moving items assigned to "Charlie" to Done
        if (e.Card.AssignedToText == "Charlie" && e.ToColumn.StateValue as string == "Done")
        {
            Log("  BLOCKED: Charlie's tasks cannot go to Done");
            e.Cancel = true;
        }
    }

    private void OnCardMoved(object? sender, KanbanBoardCardMovingEventArgs e)
    {
        Log($"Moved: {e.Card.DisplayText} -> {e.ToColumn.Name}");
    }

    private void OnCardReordering(object? sender, KanbanBoardCardReorderingEventArgs e)
    {
        Log($"Reordering: {e.Card.DisplayText} from pos {e.OldIndex} to {e.NewIndex}");
    }

    private void OnCardReordered(object? sender, KanbanBoardCardReorderingEventArgs e)
    {
        Log($"Reordered: {e.Card.DisplayText} to pos {e.NewIndex}");
    }

    private void Log(string message)
    {
        _logList.Items.Insert(0, message);
        if (_logList.Items.Count > 100)
            _logList.Items.RemoveAt(_logList.Items.Count - 1);
    }
}
