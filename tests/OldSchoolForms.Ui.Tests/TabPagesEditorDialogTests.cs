using OldSchoolForms.Ui.Controls.Advanced;
using OldSchoolForms.Ui.Dialogs;
using Xunit;

namespace OldSchoolForms.Ui.Tests;

/// <summary>
/// Drives the TabPages editor reconcile logic (applied only on commit) headlessly via a
/// testable subclass so the TabControl collection stays correct for rename/add/remove.
/// </summary>
public class TabPagesEditorDialogTests
{
    private sealed class TestableDialog : TabPagesEditorDialog
    {
        public TestableDialog(TabControl tabControl) : base(tabControl) { }

        public void TestApplyChanges() => ApplyChanges();

        public DataGridView Grid => _grid!;
    }

    [Fact]
    public void ApplyChanges_RenamesExistingPage_InPlace_AndAppendsNew()
    {
        var tab = new TabControl();
        var existing = new TabPage { Name = "existing", Text = "Existing" };
        tab.AddTabPage(existing);

        var dialog = new TestableDialog(tab);
        // The ctor already bound the existing page to row 0; rename it in place.
        dialog.Grid.Rows[0].Cells[0].Value = "existing";
        dialog.Grid.Rows[0].Cells[1].Value = "Existing Renamed";
        dialog.Grid.AddRow("", "Brand New");

        dialog.TestApplyChanges();

        Assert.Equal(2, tab.TabPages.Count);
        Assert.Same(existing, tab.TabPages[0]);
        Assert.Equal("existing", existing.Name);
        Assert.Equal("Existing Renamed", existing.Text);

        Assert.Equal("Brand New", tab.TabPages[1].Text);
        Assert.Equal(string.Empty, tab.TabPages[1].Name);
    }

    [Fact]
    public void ApplyChanges_RemovesAbsentPages()
    {
        var tab = new TabControl();
        var keep = new TabPage { Name = "keep", Text = "Keep" };
        var drop = new TabPage { Name = "drop", Text = "Drop" };
        tab.AddTabPage(keep);
        tab.AddTabPage(drop);

        var dialog = new TestableDialog(tab);
        // The ctor bound keep to row 0 and drop to row 1; remove the drop row so it is absent.
        dialog.Grid.Rows.Remove(dialog.Grid.Rows[1]);

        dialog.TestApplyChanges();

        Assert.Single(tab.TabPages);
        Assert.Same(keep, tab.TabPages[0]);
        Assert.DoesNotContain(drop, tab.TabPages);
        Assert.DoesNotContain(drop, tab.Controls);
    }

    [Fact]
    public void ShowDialog_NullTabControl_Throws()
    {
        Assert.Throws<System.ArgumentNullException>(() => TabPagesEditorDialog.ShowDialog(null!));
    }
}
