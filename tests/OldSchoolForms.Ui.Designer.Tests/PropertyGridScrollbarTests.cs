using System.Linq;
using OldSchoolForms.Ui.Core;
using OldSchoolForms.Ui.Controls.Basic;
using OldSchoolForms.Ui.Controls.Advanced;
using OldSchoolForms.Ui.Controls.Containers;
using OldSchoolForms.Ui.Designer.Services;
using Xunit;
using Graphics = OldSchoolForms.Ui.Rendering.Graphics;
using PG = OldSchoolForms.Ui.Designer.PropertyGrid.PropertyGrid;

namespace OldSchoolForms.Ui.Designer.Tests;

/// <summary>
/// Tests for the PropertyGrid vertical scrollbar added so that all property rows
/// are reachable when the content overflows the visible area.
/// </summary>
public class PropertyGridScrollbarTests
{
    private sealed class TestablePropertyGrid : PG
    {
        public TestablePropertyGrid(SelectionService selectionService) : base(selectionService) { }

        public void RaiseMouseWheel(float delta)
            => OnMouseWheel(new MouseEventArgs(MouseButtons.None, 0, 0, 0, delta));

        public int TestHitTestRow(int y) => HitTestRow(y);

        public int TestGetRowY(int rowIndex) => GetRowY(rowIndex);

        public bool IsScrollable => _vScrollBar.NeedsScrollbar;

        public int ScrollValue => _vScrollBar.Value;
    }

    private static TestablePropertyGrid CreateGridWithButton(int height)
    {
        var selection = new SelectionService();
        var grid = new TestablePropertyGrid(selection) { Width = 276, Height = height };
        var button = new Button { Location = new Point(10, 10), Size = new Size(100, 30) };
        selection.Select(new DesignItem(button));
        return grid;
    }

    [Fact]
    public void PropertyGrid_ScrollbarAppearsWhenContentOverflows()
    {
        var grid = CreateGridWithButton(100);

        Assert.True(grid.IsScrollable);
    }

    [Fact]
    public void PropertyGrid_MouseWheel_ScrollsContentDown()
    {
        var grid = CreateGridWithButton(100);

        int start = grid.ScrollValue;

        grid.RaiseMouseWheel(-100f);

        Assert.True(grid.ScrollValue > start);
    }

    [Fact]
    public void PropertyGrid_ScrollingMakesOffscreenRowsReachable()
    {
        var grid = CreateGridWithButton(100);

        // The first row sits below the property header and its category header,
        // so it is hit a little further down the grid.
        Assert.Equal(0, grid.TestHitTestRow(60));

        grid.RaiseMouseWheel(-500f);

        // After scrolling, a different (later) row is now hit near the top.
        Assert.NotEqual(0, grid.TestHitTestRow(60));
    }

    [Fact]
    public void PropertyGrid_NoScrollbarWhenContentFits()
    {
        var selection = new SelectionService();
        var grid = new TestablePropertyGrid(selection) { Width = 276, Height = 4000 };
        var button = new Button { Location = new Point(10, 10), Size = new Size(100, 30) };
        selection.Select(new DesignItem(button));

        Assert.False(grid.IsScrollable);
    }

    [Fact]
    public void PropertyGrid_ScrollbarIsDrawnWhenScrollable()
    {
        var grid = CreateGridWithButton(100);
        Assert.True(grid.IsScrollable);

        var gfx = new Graphics();
        grid.Render(gfx);

        int sbw = OldSchoolForms.Ui.Core.ScrollBarEngine.DefaultScrollBarSize;
        bool hasScrollbarFill = gfx.GetCommands()
            .Any(c => c.Type == OldSchoolForms.Ui.Rendering.DrawCommandType.FillRectangle && c.X >= grid.Width - sbw);

        Assert.True(hasScrollbarFill, "The scrollbar track/thumb should be rendered near the right edge.");
    }

    [Fact]
    public void PropertyGrid_CheckOverflowForRealisticHeight()
    {
        var ps = new OldSchoolForms.Ui.Designer.PropertyGrid.PropertyService();
        object?[] controls = { new Button(), new TextBox(), new Panel(), new GroupBox(),
            new TabControl(), new ComboBox(), new ListBox(), new DateTimePicker(),
            new DataGridView(), new ProgressBar() };
        foreach (var obj in controls)
        {
            if (obj is Control ctrl)
            {
                var count = ps.GetProperties(ctrl).Count;
                var grid = CreateGridForControl(ctrl, 850);
                var gfx = new Graphics();
                grid.Render(gfx);
                int sbw = OldSchoolForms.Ui.Core.ScrollBarEngine.DefaultScrollBarSize;
                bool drawn = gfx.GetCommands()
                    .Any(c => c.Type == OldSchoolForms.Ui.Rendering.DrawCommandType.FillRectangle && c.X >= grid.Width - sbw);
                System.Console.WriteLine($"{ctrl.GetType().Name}: {count} props, IsScrollable={grid.IsScrollable}, scrollbarDrawn={drawn}");
            }
        }
    }

    private static TestablePropertyGrid CreateGridForControl(Control control, int height)
    {
        var selection = new SelectionService();
        var grid = new TestablePropertyGrid(selection) { Width = 276, Height = height };
        selection.Select(new DesignItem(control));
        return grid;
    }
}
