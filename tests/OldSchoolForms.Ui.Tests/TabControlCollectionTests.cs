using OldSchoolForms.Ui.Controls.Advanced;
using Xunit;

namespace OldSchoolForms.Ui.Tests;

/// <summary>
/// Tests that the TabControl page-collection operations keep the tab pages and the
/// internal control collection in sync and clamp the selected index.
/// </summary>
public class TabControlCollectionTests
{
    [Fact]
    public void AddTabPage_SyncsTabPagesAndControls_AndDisablesTabStop()
    {
        var tab = new TabControl();
        var page = new TabPage { Name = "p1", Text = "One" };

        tab.AddTabPage(page);

        Assert.Single(tab.TabPages);
        Assert.Single(tab.Controls);
        Assert.Same(page, tab.TabPages[0]);
        Assert.Same(page, tab.Controls[0]);
        Assert.False(page.TabStop);
    }

    [Fact]
    public void AddTabPage_ThrowsOnNullAndIgnoresDuplicates()
    {
        var tab = new TabControl();
        var page = new TabPage();

        Assert.Throws<System.ArgumentNullException>(() => tab.AddTabPage(null!));
        tab.AddTabPage(page);
        tab.AddTabPage(page);

        Assert.Single(tab.TabPages);
        Assert.Single(tab.Controls);
    }

    [Fact]
    public void Indexer_ReturnsPageAtPosition()
    {
        var tab = new TabControl();
        var a = new TabPage { Name = "a", Text = "A" };
        var b = new TabPage { Name = "b", Text = "B" };

        tab.AddTabPage(a);
        tab.AddTabPage(b);

        Assert.Same(a, tab[0]);
        Assert.Same(b, tab[1]);
    }

    [Fact]
    public void RemoveAt_SyncsCollections_AndClampsSelectedIndex()
    {
        var tab = new TabControl();
        var a = new TabPage();
        var b = new TabPage();
        var c = new TabPage();
        tab.AddTabPage(a);
        tab.AddTabPage(b);
        tab.AddTabPage(c);
        tab.SelectedIndex = 2;

        tab.RemoveAt(2);

        Assert.Equal(2, tab.TabPages.Count);
        Assert.DoesNotContain(c, tab.TabPages);
        Assert.DoesNotContain(c, tab.Controls);
        Assert.Equal(1, tab.SelectedIndex);
    }

    [Fact]
    public void RemovePage_ClampsSelectedIndex()
    {
        var tab = new TabControl();
        var a = new TabPage();
        var b = new TabPage();
        tab.AddTabPage(a);
        tab.AddTabPage(b);
        tab.SelectedIndex = 1;

        tab.RemoveTabPage(a);

        Assert.Single(tab.TabPages);
        Assert.Same(b, tab.SelectedTab);
        Assert.Equal(0, tab.SelectedIndex);
    }

    [Fact]
    public void Insert_SyncsOrder_AndControls_AndBumpsSelection()
    {
        var tab = new TabControl();
        var a = new TabPage();
        var mid = new TabPage();
        var b = new TabPage();
        tab.AddTabPage(a);
        tab.AddTabPage(b);
        tab.SelectedIndex = 1;

        tab.Insert(1, mid);

        Assert.Equal(3, tab.TabPages.Count);
        Assert.Same(mid, tab[1]);
        Assert.Same(mid, tab.Controls[1]);
        Assert.Equal(2, tab.SelectedIndex);
    }
}
