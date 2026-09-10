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

    [Fact]
    public void PropertyGridRow_CommitEdit_ParsesPointLocation()
    {
        var control = new Control();
        control.Location = new Point(10, 20);

        var property = typeof(Control).GetProperty(nameof(Control.Location))!;
        var descriptor = new PropertyDescriptor(property, control);
        var row = new PropertyGridRow(descriptor, 140);

        row.StartEdit();
        SetEditBuffer(row, "[100, 200]");
        row.CommitEdit();

        Assert.Equal(new Point(100, 200), control.Location);
    }

    [Fact]
    public void PropertyGridRow_CommitEdit_ParsesSize()
    {
        var control = new Control();
        control.Size = new Size(120, 90);

        var property = typeof(Control).GetProperty(nameof(Control.Size))!;
        var descriptor = new PropertyDescriptor(property, control);
        var row = new PropertyGridRow(descriptor, 140);

        row.StartEdit();
        SetEditBuffer(row, "[300, 250]");
        row.CommitEdit();

        Assert.Equal(new Size(300, 250), control.Size);
    }

    [Fact]
    public void PropertyGridRow_CommitEdit_ParsesBoolEnabled()
    {
        var control = new Control();
        control.Enabled = true;

        var property = typeof(Control).GetProperty(nameof(Control.Enabled))!;
        var descriptor = new PropertyDescriptor(property, control);
        var row = new PropertyGridRow(descriptor, 140);

        row.StartEdit();
        SetEditBuffer(row, "False");
        row.CommitEdit();

        Assert.False(control.Enabled);
    }

    [Fact]
    public void PropertyGridRow_CommitEdit_DropsInvalidPointWithoutChangingValue()
    {
        var control = new Control();
        control.Location = new Point(10, 20);

        var property = typeof(Control).GetProperty(nameof(Control.Location))!;
        var descriptor = new PropertyDescriptor(property, control);
        var row = new PropertyGridRow(descriptor, 140);

        row.StartEdit();
        SetEditBuffer(row, "not-a-point");
        row.CommitEdit();

        Assert.Equal(new Point(10, 20), control.Location);
    }

    [Fact]
    public void PropertyGridRow_ToggleBool_FlipsValue()
    {
        var control = new Control();
        control.Enabled = true;

        var property = typeof(Control).GetProperty(nameof(Control.Enabled))!;
        var descriptor = new PropertyDescriptor(property, control);
        var row = new PropertyGridRow(descriptor, 140);

        row.ToggleBool();
        Assert.False(control.Enabled);

        row.ToggleBool();
        Assert.True(control.Enabled);
    }

    [Fact]
    public void PropertyGridRow_IsBoolProperty_MatchesBoolType()
    {
        var boolProperty = typeof(Control).GetProperty(nameof(Control.Enabled))!;
        var locationProperty = typeof(Control).GetProperty(nameof(Control.Location))!;

        var boolRow = new PropertyGridRow(new PropertyDescriptor(boolProperty, new Control()), 140);
        var nonBoolRow = new PropertyGridRow(new PropertyDescriptor(locationProperty, new Control()), 140);

        Assert.True(boolRow.IsBoolProperty);
        Assert.False(nonBoolRow.IsBoolProperty);
    }

    private static void SetEditBuffer(PropertyGridRow row, string value)
    {
        var field = typeof(PropertyGridRow)
            .GetField("_editBuffer", BindingFlags.NonPublic | BindingFlags.Instance)!;
        field.SetValue(row, value);
    }

    private static PropertyGridRow CreateEditRow(string buffer, int caret, int anchor)
    {
        var property = typeof(Control).GetProperty(nameof(Control.Location))!;
        var row = new PropertyGridRow(new PropertyDescriptor(property, new Control()), 140);
        row.StartEdit();
        SetEditBuffer(row, buffer);
        SetCaret(row, caret);
        SetAnchor(row, anchor);
        return row;
    }

    private static int GetCaret(PropertyGridRow row)
        => (int?)typeof(PropertyGridRow).GetField("_caretPos", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(row) ?? -1;

    private static int GetAnchor(PropertyGridRow row)
        => (int?)typeof(PropertyGridRow).GetField("_anchor", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(row) ?? -1;

    private static string GetEditBuffer(PropertyGridRow row)
        => (string?)typeof(PropertyGridRow).GetField("_editBuffer", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(row) ?? string.Empty;

    private static void SetCaret(PropertyGridRow row, int value)
        => typeof(PropertyGridRow).GetField("_caretPos", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(row, value);

    private static void SetAnchor(PropertyGridRow row, int value)
        => typeof(PropertyGridRow).GetField("_anchor", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(row, value);

    [Fact]
    public void PropertyGridRow_MoveCaret_CollapsesSelection_WithoutExtend()
    {
        var row = CreateEditRow("abcdef", caret: 2, anchor: 4);

        row.MoveCaret(5, extend: false);

        Assert.Equal(5, GetCaret(row));
        Assert.Equal(5, GetAnchor(row));
        Assert.False(row.HasSelection);
    }

    [Fact]
    public void PropertyGridRow_MoveCaret_ExtendsSelection_WithExtend()
    {
        var row = CreateEditRow("abcdef", caret: 2, anchor: 2);

        row.MoveCaret(5, extend: true);

        Assert.Equal(5, GetCaret(row));
        Assert.Equal(2, GetAnchor(row));
        Assert.True(row.HasSelection);
        Assert.Equal("cde", row.GetSelectedText());
    }

    [Fact]
    public void PropertyGridRow_MoveLeftRight_MoveCaret_OneStep()
    {
        var row = CreateEditRow("abcdef", caret: 3, anchor: 3);

        row.MoveRight(false);
        Assert.Equal(4, GetCaret(row));

        row.MoveLeft(false);
        Assert.Equal(3, GetCaret(row));
    }

    [Fact]
    public void PropertyGridRow_MoveToStartToEnd_MoveCaret_ToBoundaries()
    {
        var row = CreateEditRow("abcdef", caret: 3, anchor: 3);

        row.MoveToEnd(false);
        Assert.Equal(6, GetCaret(row));

        row.MoveToStart(false);
        Assert.Equal(0, GetCaret(row));
    }

    [Fact]
    public void PropertyGridRow_MoveCaret_ClampsToBufferBounds()
    {
        var row = CreateEditRow("abc", caret: 1, anchor: 1);

        row.MoveCaret(-5, false);
        Assert.Equal(0, GetCaret(row));

        row.MoveCaret(99, false);
        Assert.Equal(3, GetCaret(row));
    }

    [Fact]
    public void PropertyGridRow_DeleteSelection_RemovesSelectedText_AndCollapses()
    {
        var row = CreateEditRow("abcdef", caret: 4, anchor: 1);

        row.DeleteSelection();

        Assert.Equal("aef", GetEditBuffer(row));
        Assert.Equal(1, GetCaret(row));
        Assert.Equal(1, GetAnchor(row));
        Assert.False(row.HasSelection);
    }

    [Fact]
    public void PropertyGridRow_DeleteCharAfterCaret_DeletesSelection_WhenPresent()
    {
        var row = CreateEditRow("abcdef", caret: 2, anchor: 4);

        Assert.True(row.DeleteCharAfterCaret());
        Assert.Equal("abef", GetEditBuffer(row));
        Assert.Equal(2, GetCaret(row));
    }

    [Fact]
    public void PropertyGridRow_DeleteCharAfterCaret_DeletesCharAfterCaret_WhenNoSelection()
    {
        var row = CreateEditRow("abcdef", caret: 2, anchor: 2);

        Assert.True(row.DeleteCharAfterCaret());
        Assert.Equal("abdef", GetEditBuffer(row));
        Assert.Equal(2, GetCaret(row));
    }

    [Fact]
    public void PropertyGridRow_DeleteCharAfterCaret_ReturnsFalse_AtEnd()
    {
        var row = CreateEditRow("abc", caret: 3, anchor: 3);

        Assert.False(row.DeleteCharAfterCaret());
        Assert.Equal("abc", GetEditBuffer(row));
    }

    [Fact]
    public void PropertyGridRow_HandleKey_Backspace_DeletesSelection_First()
    {
        var row = CreateEditRow("abcdef", caret: 4, anchor: 1);

        Assert.True(row.HandleKey('\b'));
        Assert.Equal("aef", GetEditBuffer(row));
        Assert.Equal(1, GetCaret(row));
    }

    [Fact]
    public void PropertyGridRow_SelectWordAt_SelectsSurroundingWord()
    {
        var row = CreateEditRow("foo bar baz", caret: 4, anchor: 4);

        row.SelectWordAt(4);

        Assert.Equal(4, GetAnchor(row));
        Assert.Equal(7, GetCaret(row));
        Assert.Equal("bar", row.GetSelectedText());
    }

    [Fact]
    public void PropertyGridRow_HandleKey_InsertChar_AtCaret()
    {
        var row = CreateEditRow("ac", caret: 1, anchor: 1);

        Assert.True(row.HandleKey('b'));
        Assert.Equal("abc", GetEditBuffer(row));
        Assert.Equal(2, GetCaret(row));
    }

    [Fact]
    public void PropertyGridRow_DeleteSelection_DoesNotThrow_WithOutRangeSelection()
    {
        var row = CreateEditRow("abc", caret: 5, anchor: 3);

        Assert.True(row.HasSelection);
        row.DeleteSelection();

        Assert.Equal("abc", GetEditBuffer(row));
        Assert.Equal(3, GetCaret(row));
        Assert.Equal(3, GetAnchor(row));
    }

    [Fact]
    public void PropertyGridRow_HandleKey_Insert_ReplacesSelection()
    {
        var row = CreateEditRow("abcdef", caret: 4, anchor: 1);

        Assert.True(row.HandleKey('X'));
        Assert.Equal("aXef", GetEditBuffer(row));
        Assert.Equal(2, GetCaret(row));
        Assert.False(row.HasSelection);
    }
}
