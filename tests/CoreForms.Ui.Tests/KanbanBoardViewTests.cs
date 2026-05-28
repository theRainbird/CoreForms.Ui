using CoreForms.Ui.Core;
using CoreForms.Ui.Controls.Advanced;
using Xunit;

namespace CoreForms.Ui.Tests;

public class KanbanBoardViewTests
{
    private class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string AssignedTo { get; set; } = string.Empty;
        public string State { get; set; } = "ToDo";
        public List<string> Tags { get; set; } = new();
    }

    [Fact]
    public void Columns_AddColumn_IncreasesCount()
    {
        var view = new KanbanBoardView();
        view.Columns.Add(new KanbanBoardColumn { Name = "ToDo", StateValue = "ToDo" });
        view.Columns.Add(new KanbanBoardColumn { Name = "InProgress", StateValue = "InProgress" });

        Assert.Equal(2, view.Columns.Count);
    }

    [Fact]
    public void DataSource_PopulatesCardsInCorrectColumns()
    {
        var view = new KanbanBoardView();
        view.Columns.Add(new KanbanBoardColumn { Name = "ToDo", StateValue = "ToDo" });
        view.Columns.Add(new KanbanBoardColumn { Name = "Done", StateValue = "Done" });

        var items = new List<TaskItem>
        {
            new() { Id = 1, Title = "Task 1", State = "ToDo" },
            new() { Id = 2, Title = "Task 2", State = "Done" },
            new() { Id = 3, Title = "Task 3", State = "ToDo" },
        };

        view.DisplayMember = "Title";
        view.StateMember = "State";
        view.DataSource = items;

        Assert.Equal(2, view.Columns.Count);
        Assert.Equal(2, view.Columns[0].CardCount);
        Assert.Equal(1, view.Columns[1].CardCount);
    }

    [Fact]
    public void DisplayMember_ShowsCorrectText()
    {
        var view = new KanbanBoardView();
        view.Columns.Add(new KanbanBoardColumn { Name = "All", StateValue = "All" });

        var items = new List<TaskItem>
        {
            new() { Id = 1, Title = "My Task", State = "All" },
        };

        view.DisplayMember = "Title";
        view.StateMember = "State";
        view.DataSource = items;

        Assert.Equal("My Task", view.Columns[0].Cards[0].DisplayText);
    }

    [Fact]
    public void DescriptionMember_ShowsCorrectText()
    {
        var view = new KanbanBoardView();
        view.Columns.Add(new KanbanBoardColumn { Name = "All", StateValue = "All" });

        var items = new List<TaskItem>
        {
            new() { Id = 1, Title = "Task", Description = "A description", State = "All" },
        };

        view.DisplayMember = "Title";
        view.DescriptionMember = "Description";
        view.StateMember = "State";
        view.DataSource = items;

        Assert.Equal("A description", view.Columns[0].Cards[0].DescriptionText);
    }

    [Fact]
    public void ValueMember_ReturnsCorrectValue()
    {
        var view = new KanbanBoardView();
        view.Columns.Add(new KanbanBoardColumn { Name = "All", StateValue = "All" });

        var items = new List<TaskItem>
        {
            new() { Id = 42, Title = "Task", State = "All" },
        };

        view.DisplayMember = "Title";
        view.ValueMember = "Id";
        view.StateMember = "State";
        view.DataSource = items;

        Assert.Equal(42, view.Columns[0].Cards[0].Value);
    }

    [Fact]
    public void CategoryMember_ShowsCorrectText()
    {
        var view = new KanbanBoardView();
        view.Columns.Add(new KanbanBoardColumn { Name = "All", StateValue = "All" });

        var items = new List<TaskItem>
        {
            new() { Id = 1, Title = "Task", Category = "Bug", State = "All" },
        };

        view.DisplayMember = "Title";
        view.CategoryMember = "Category";
        view.StateMember = "State";
        view.DataSource = items;

        Assert.Equal("Bug", view.Columns[0].Cards[0].CategoryText);
    }

    [Fact]
    public void CategoryColors_AreAccessible()
    {
        var view = new KanbanBoardView();
        view.CategoryColors["Bug"] = Color.FromArgb(255, 0, 0);
        view.CategoryColors["Feature"] = Color.FromArgb(0, 255, 0);

        Assert.Equal(2, view.CategoryColors.Count);
    }

    [Fact]
    public void AssignedToMember_ShowsCorrectText()
    {
        var view = new KanbanBoardView();
        view.Columns.Add(new KanbanBoardColumn { Name = "All", StateValue = "All" });

        var items = new List<TaskItem>
        {
            new() { Id = 1, Title = "Task", AssignedTo = "Alice", State = "All" },
        };

        view.DisplayMember = "Title";
        view.AssignedToMember = "AssignedTo";
        view.StateMember = "State";
        view.DataSource = items;

        Assert.Equal("Alice", view.Columns[0].Cards[0].AssignedToText);
    }

    [Fact]
    public void TagMember_ShowsCorrectTags()
    {
        var view = new KanbanBoardView();
        view.Columns.Add(new KanbanBoardColumn { Name = "All", StateValue = "All" });

        var items = new List<TaskItem>
        {
            new() { Id = 1, Title = "Task", Tags = new List<string> { "Urgent", "Frontend" }, State = "All" },
        };

        view.DisplayMember = "Title";
        view.TagMember = "Tags";
        view.StateMember = "State";
        view.DataSource = items;

        Assert.Equal(2, view.Columns[0].Cards[0].Tags.Count);
        Assert.Contains("Urgent", view.Columns[0].Cards[0].Tags);
        Assert.Contains("Frontend", view.Columns[0].Cards[0].Tags);
    }

    [Fact]
    public void CardGotFocus_EventFiresOnFocusSet()
    {
        var view = new KanbanBoardView();
        view.Columns.Add(new KanbanBoardColumn { Name = "All", StateValue = "All" });

        var items = new List<TaskItem>
        {
            new() { Id = 1, Title = "Task A", State = "All" },
            new() { Id = 2, Title = "Task B", State = "All" },
        };

        view.DisplayMember = "Title";
        view.StateMember = "State";
        view.DataSource = items;

        KanbanBoardCard? focusedCard = null;
        view.CardGotFocus += (s, e) => focusedCard = e.Card;

        view.Columns[0].Cards[0].GetType().GetProperty("DisplayText")!
            .SetValue(view.Columns[0].Cards[0], "Task A");

        // Focus first card via keyboard simulation
        view.Focused = true;

        Assert.NotNull(focusedCard);
    }

    [Fact]
    public void AllowDrop_DefaultIsTrue()
    {
        var view = new KanbanBoardView();
        Assert.True(view.AllowDrop);
    }

    [Fact]
    public void AllowedTransitions_Empty_AllowsAllMoves()
    {
        var view = new KanbanBoardView();
        view.AllowedTransitions.Clear();

        Assert.Empty(view.AllowedTransitions);
    }

    [Fact]
    public void AllowReorder_DefaultIsTrue()
    {
        var view = new KanbanBoardView();
        Assert.True(view.AllowReorder);
    }

    [Fact]
    public void Column_Width_MinimumIs50()
    {
        var col = new KanbanBoardColumn { Width = 10 };
        Assert.Equal(50, col.Width);
    }

    [Fact]
    public void Column_BackColor_CanBeSet()
    {
        var col = new KanbanBoardColumn();
        var color = Color.FromArgb(200, 200, 200);
        col.BackColor = color;
        Assert.Equal(color, col.BackColor);
    }

    [Fact]
    public void EmptyDataSource_DoesNotThrow()
    {
        var view = new KanbanBoardView();
        view.Columns.Add(new KanbanBoardColumn { Name = "Col", StateValue = "X" });
        view.DataSource = new List<TaskItem>();
        Assert.Equal(0, view.Columns[0].CardCount);
    }

    [Fact]
    public void NullStateValue_CardGoesToFirstColumn()
    {
        var view = new KanbanBoardView();
        view.Columns.Add(new KanbanBoardColumn { Name = "Default", StateValue = null });
        view.Columns.Add(new KanbanBoardColumn { Name = "Other", StateValue = "Other" });

        var items = new List<TaskItem>
        {
            new() { Id = 1, Title = "Task", State = null! },
        };

        view.DisplayMember = "Title";
        view.StateMember = "State";
        view.DataSource = items;

        Assert.Equal(1, view.Columns[0].CardCount);
    }

    [Fact]
    public void KanbanBoardCard_Constructor_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => new KanbanBoardCard(null!));
    }

    [Fact]
    public void KanbanBoardCard_Properties_AreSettable()
    {
        var card = new KanbanBoardCard(new TaskItem { Id = 1, Title = "Test" });
        card.DisplayText = "Test Card";
        card.DescriptionText = "A description";
        card.CategoryText = "Feature";
        card.AssignedToText = "Bob";
        card.StateValue = "InProgress";
        card.Value = 42;
        card.Tags = new[] { "tag1", "tag2" }.ToList().AsReadOnly();

        Assert.Equal("Test Card", card.DisplayText);
        Assert.Equal("Feature", card.CategoryText);
        Assert.Equal(42, card.Value);
    }

    [Fact]
    public void ColumnCollection_Clear_RemovesAll()
    {
        var view = new KanbanBoardView();
        view.Columns.Add(new KanbanBoardColumn { Name = "A" });
        view.Columns.Add(new KanbanBoardColumn { Name = "B" });
        view.Columns.Clear();
        Assert.Equal(0, view.Columns.Count);
    }
}
