using System.Linq;
using System.Reflection;
using OldSchoolForms.Ui.Controls.Advanced;
using OldSchoolForms.Ui.Core;
using OldSchoolForms.Ui.Design;
using OldSchoolForms.Ui.Designer.PropertyGrid;
using OldSchoolForms.Ui.Rendering;
using OldSchoolForms.Ui.Theming;
using Xunit;

namespace OldSchoolForms.Ui.Designer.Tests;

/// <summary>
/// Verifies that the [EditorButton] mechanism keeps the read-only TabPages property in the
/// PropertyGrid, attaches an editor, and that the row opens that editor on an ellipsis click.
/// </summary>
public class PropertyGridEditorTests
{
    private sealed class StubEditor : IValueEditor
    {
        public bool Opened { get; private set; }
        public Control? OpenedControl { get; private set; }

        public string ButtonText => "…";
        public bool Handles(PropertyInfo property) => property.Name == nameof(TabControl.TabPages);
        public void Open(Control control) { Opened = true; OpenedControl = control; }
    }

    [Fact]
    public void PropertyService_KeepsEditorAnnotatedReadOnlyProperty()
    {
        var service = new PropertyService();
        var tab = new TabControl();

        var descriptor = service.GetProperties(tab).FirstOrDefault(p => p.Name == nameof(TabControl.TabPages));

        Assert.NotNull(descriptor);
        Assert.True(descriptor!.IsReadOnly);
        Assert.NotNull(descriptor.Editor);
    }

    [Fact]
    public void TabPagesEditor_MatchesTabPagesProperty_Only()
    {
        var editor = new TabPagesEditor();
        Assert.Equal("…", editor.ButtonText);

        var tabPages = typeof(TabControl).GetProperty(nameof(TabControl.TabPages));
        var other = typeof(TabControl).GetProperty(nameof(TabControl.SelectedIndex));

        Assert.True(editor.Handles(tabPages!));
        Assert.False(editor.Handles(other!));
    }

    [Fact]
    public void PropertyGridRow_TryHandleEditorClick_OpensEditor()
    {
        var stub = new StubEditor();
        var tab = new TabControl();
        var property = typeof(TabControl).GetProperty(nameof(TabControl.TabPages))!;
        var nameColumnWidth = 140;
        var valueColumnWidth = 160;
        var descriptor = new PropertyDescriptor(property, tab, stub);
        var row = new PropertyGridRow(descriptor, nameColumnWidth);

        var valueColumnX = nameColumnWidth;
        var clickX = valueColumnX + valueColumnWidth - PropertyGridRow.EllipsisButtonWidth + 5;

        bool opened = row.TryHandleEditorClick(clickX, 10, valueColumnX, valueColumnWidth);

        Assert.True(opened);
        Assert.True(stub.Opened);
        Assert.Same(tab, stub.OpenedControl);
    }

    [Fact]
    public void PropertyGridRow_TryHandleEditorClick_ReturnsFalse_OffButton()
    {
        var stub = new StubEditor();
        var tab = new TabControl();
        var property = typeof(TabControl).GetProperty(nameof(TabControl.TabPages))!;
        var nameColumnWidth = 140;
        var valueColumnWidth = 160;
        var descriptor = new PropertyDescriptor(property, tab, stub);
        var row = new PropertyGridRow(descriptor, nameColumnWidth);

        var valueColumnX = nameColumnWidth;

        bool opened = row.TryHandleEditorClick(valueColumnX + 5, 10, valueColumnX, valueColumnWidth);

        Assert.False(opened);
        Assert.False(stub.Opened);
    }

    [Fact]
    public void PropertyGridRow_DrawsCollectionCount_NotTypeName_WithEllipsis()
    {
        var tab = new TabControl();
        tab.AddTabPage(new TabPage { Text = "One" });
        tab.AddTabPage(new TabPage { Text = "Two" });
        tab.AddTabPage(new TabPage { Text = "Three" });

        var property = typeof(TabControl).GetProperty(nameof(TabControl.TabPages))!;
        var descriptor = new PropertyDescriptor(property, tab, new TabPagesEditor());
        var row = new PropertyGridRow(descriptor, 140);

        var g = new Graphics();
        row.Render(g, 0, 300, 30, ThemeManager.CurrentTheme);

        var texts = g.GetCommands()
            .Where(c => c.Type == DrawCommandType.DrawString)
            .Select(c => c.Text)
            .Where(t => t != null)
            .Cast<string>()
            .ToList();

        Assert.Contains("3", texts);
        Assert.DoesNotContain(texts, t => t.Contains("List<"));
        Assert.Contains("…", texts);
    }
}
