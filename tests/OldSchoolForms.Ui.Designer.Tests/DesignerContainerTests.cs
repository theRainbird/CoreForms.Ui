using System;
using System.Linq;
using OldSchoolForms.Ui.Core;
using OldSchoolForms.Ui.Controls.Basic;
using OldSchoolForms.Ui.Controls.Containers;
using OldSchoolForms.Ui.Layout;
using OldSchoolForms.Ui.Designer.Services;
using OldSchoolForms.Ui.Rendering;
using Xunit;

namespace OldSchoolForms.Ui.Designer.Tests;

public class DesignerContainerTests
{
    private static MouseEventArgs LeftMouseDown(int x, int y)
        => new(MouseButtons.Left, 1, x, y, 0f);

    /// <summary>
    /// Exposes the protected mouse-event handlers so the designer's mouse routing
    /// can be exercised headlessly without a real input stack.
    /// </summary>
    private sealed class TestableDesignSurface : DesignSurface
    {
        public void RaiseMouseDown(MouseEventArgs e) => OnMouseDown(e);
        public void RaiseMouseMove(MouseEventArgs e) => OnMouseMove(e);
        public void RaiseMouseUp(MouseEventArgs e) => OnMouseUp(e);
    }

    private static Panel CreatePanel(Point location, Size size)
    {
        var panel = new Panel { Location = location, Size = size };
        return panel;
    }

    [Fact]
    public void AddControl_IntoContainer_SetsHierarchy_AndTracksSingleItem()
    {
        var surface = new DesignSurface { SnapToGrid = false };
        var panel = CreatePanel(new Point(30, 30), new Size(200, 200));
        var panelItem = surface.AddControl(panel, new Point(30, 30));

        var button = new Button { Size = new Size(120, 40) };
        var buttonItem = surface.AddControl(button, panel, new Point(70, 70));

        Assert.Same(surface, panel.Parent);
        Assert.Null(panelItem.ParentItem);
        Assert.Same(panel, button.Parent);
        Assert.Same(panelItem, buttonItem.ParentItem);
        Assert.Equal(new Point(70, 70), button.Location);

        // The control is parented to the container exactly once and tracked once.
        Assert.Single(panel.Controls);
        Assert.Single(surface.Items, i => i.Control == panel);
        Assert.Single(surface.Items, i => i.Control == button);
        Assert.Equal(2, surface.Items.Count);
        Assert.Same(buttonItem, surface.FindItem(button));
    }

    [Fact]
    public void AddControl_IntoContainer_SnapsWhenGridEnabled()
    {
        var surface = new DesignSurface { SnapToGrid = true };
        var panel = CreatePanel(new Point(30, 30), new Size(200, 200));
        surface.AddControl(panel, new Point(30, 30));

        var button = new Button { Size = new Size(120, 40) };
        surface.AddControl(button, panel, new Point(73, 41));

        // Grid size is 8: (73, 41) snaps to (72, 40).
        Assert.Equal(new Point(72, 40), button.Location);
    }

    [Fact]
    public void FindItemAt_ReturnsNestedControl_AndContainerForEmptyArea()
    {
        var surface = new DesignSurface { SnapToGrid = false };
        var panel = CreatePanel(new Point(30, 30), new Size(200, 200));
        surface.AddControl(panel, new Point(30, 30));
        var button = new Button { Size = new Size(120, 40) };
        surface.AddControl(button, panel, new Point(70, 70));

        // Button occupies surface rectangle (100,100)-(220,140).
        var hit = surface.FindItemAt(new Point(150, 115));
        Assert.Same(button, hit?.Control);

        // An empty area inside the panel resolves to the container itself.
        var empty = surface.FindItemAt(new Point(40, 40));
        Assert.Same(panel, empty?.Control);
    }

    [Fact]
    public void FindItemAt_ReturnsTopLevelControl_OnSurface()
    {
        var surface = new DesignSurface { SnapToGrid = false };
        var button = new Button { Location = new Point(50, 50), Size = new Size(120, 40) };
        surface.AddControl(button, new Point(50, 50));

        var hit = surface.FindItemAt(new Point(60, 60));
        Assert.Same(button, hit?.Control);

        Assert.Null(surface.FindItemAt(new Point(5, 5)));
    }

    [Fact]
    public void FindDropContainerAt_ReturnsInnermostContainer_AndNullOutside()
    {
        var surface = new DesignSurface { SnapToGrid = false };
        var panel = CreatePanel(new Point(30, 30), new Size(200, 200));
        surface.AddControl(panel, new Point(30, 30));

        // Inside the panel -> panel.
        Assert.Same(panel, surface.FindDropContainerAt(new Point(100, 100)));

        // Outside the panel -> null.
        Assert.Null(surface.FindDropContainerAt(new Point(5, 5)));
    }

    [Fact]
    public void DeleteSelected_RemovesNestedControl_FromContainer()
    {
        var surface = new DesignSurface { SnapToGrid = false };
        var panel = CreatePanel(new Point(30, 30), new Size(200, 200));
        surface.AddControl(panel, new Point(30, 30));
        var button = new Button { Size = new Size(120, 40) };
        var buttonItem = surface.AddControl(button, panel, new Point(70, 70));

        surface.Selection.Select(buttonItem);
        surface.DeleteSelected();

        Assert.Empty(panel.Controls);
        Assert.Null(surface.FindItem(button));
        Assert.Single(surface.Items);
        Assert.Same(panel, surface.Items[0].Control);
    }

    [Fact]
    public void Drag_WithinSurface_FollowsMouse_WithoutReparenting()
    {
        var surface = new DesignSurface { SnapToGrid = false };
        var button = new Button { Location = new Point(250, 50), Size = new Size(120, 40) };
        var item = surface.AddControl(button, new Point(250, 50));
        surface.Selection.Select(item);

        var reparented = false;
        surface.Drag.DragReparented += (_, _) => reparented = true;

        surface.Drag.BeginMove(new Point(250, 50));
        surface.Drag.ContinueDrag(new Point(200, 80));

        Assert.Equal(new Point(200, 80), button.Location);
        Assert.Equal(new Point(200, 80), button.PointToScreen(Point.Empty));
        Assert.False(reparented);

        surface.Drag.EndDrag();
    }

    [Fact]
    public void Drag_LiveReparentsIntoContainer_PreservingSurfacePosition()
    {
        var surface = new DesignSurface { SnapToGrid = false };
        var panel = CreatePanel(new Point(30, 30), new Size(200, 200));
        var panelItem = surface.AddControl(panel, new Point(30, 30));

        var button = new Button { Size = new Size(120, 40) };
        surface.AddControl(button, new Point(250, 50));
        var buttonItem = surface.FindItem(button)!;
        surface.Selection.Select(buttonItem);

        var reparentCount = 0;
        surface.Drag.DragReparented += (_, _) => reparentCount++;

        // Start the drag on the surface, then drag into the panel.
        surface.Drag.BeginMove(new Point(250, 50));
        surface.Drag.ContinueDrag(new Point(100, 100));

        Assert.Same(panel, button.Parent);
        Assert.Same(panelItem, buttonItem.ParentItem);
        Assert.Equal(new Point(70, 70), button.Location);
        Assert.Equal(new Point(100, 100), button.PointToScreen(Point.Empty));
        Assert.Equal(1, reparentCount);

        surface.Drag.EndDrag();
    }

    [Fact]
    public void Drag_WithinSameContainer_DoesNotReparent()
    {
        var surface = new DesignSurface { SnapToGrid = false };
        var panel = CreatePanel(new Point(30, 30), new Size(200, 200));
        surface.AddControl(panel, new Point(30, 30));

        var button = new Button { Size = new Size(120, 40) };
        surface.AddControl(button, panel, new Point(70, 70));
        var buttonItem = surface.FindItem(button)!;
        surface.Selection.Select(buttonItem);

        var reparentCount = 0;
        surface.Drag.DragReparented += (_, _) => reparentCount++;

        // Drag entirely within the panel, grabbing the button's top-left.
        surface.Drag.BeginMove(new Point(100, 100));
        surface.Drag.ContinueDrag(new Point(120, 110));

        Assert.Same(panel, button.Parent);
        Assert.Equal(new Point(90, 80), button.Location);
        Assert.Equal(new Point(120, 110), button.PointToScreen(Point.Empty));
        Assert.Equal(0, reparentCount);

        surface.Drag.EndDrag();
    }

    [Fact]
    public void ToolboxDrop_IntoContainer_PlacesControlOnce()
    {
        var surface = new TestableDesignSurface { SnapToGrid = false };
        var panel = CreatePanel(new Point(30, 30), new Size(200, 200));
        surface.AddControl(panel, new Point(30, 30));

        var buttonItem = surface.Toolbox.Items.First(i => i.ControlType == typeof(Button));
        surface.BeginExternalDrop(buttonItem);
        Assert.True(surface.IsPendingDrop);

        // Click inside the panel at surface coordinates (100,100).
        surface.RaiseMouseDown(LeftMouseDown(100, 100));

        Assert.False(surface.IsPendingDrop);
        Assert.Equal(2, surface.Items.Count);

        var placed = surface.Items.Single(i => i.Control is Button);
        Assert.Same(panel, placed.Control.Parent);
        Assert.Single(panel.Controls);
    }

    [Fact]
    public void ToolboxDrop_IntoGroupBox_IsGeneric_ForAnyContainer()
    {
        var surface = new TestableDesignSurface { SnapToGrid = false };
        var groupBox = new GroupBox { Location = new Point(30, 30), Size = new Size(300, 200) };
        surface.AddControl(groupBox, new Point(30, 30));

        var buttonItem = surface.Toolbox.Items.First(i => i.ControlType == typeof(Button));
        surface.BeginExternalDrop(buttonItem);

        // Click well inside the group box content area.
        surface.RaiseMouseDown(LeftMouseDown(120, 100));

        Assert.False(surface.IsPendingDrop);
        var placed = surface.Items.Single(i => i.Control is Button);
        Assert.Same(groupBox, placed.Control.Parent);
    }

    [Fact]
    public void SurfaceDrop_PlaceAtClickLocation()
    {
        var surface = new TestableDesignSurface { SnapToGrid = false };

        var drop = new Point(100, 100);
        var buttonItem = surface.Toolbox.Items.First(i => i.ControlType == typeof(Button));
        surface.BeginExternalDrop(buttonItem);
        surface.RaiseMouseDown(LeftMouseDown(drop.X, drop.Y));

        var ctrl = surface.Items.Single().Control;
        // The click point is already in surface-local space, so the control lands exactly there.
        Assert.Equal(drop, ctrl.Location);
    }

    [Fact]
    public void ContainerDrop_PlaceNestedAtClickLocation()
    {
        var surface = new TestableDesignSurface { SnapToGrid = false };
        var panel = CreatePanel(new Point(30, 30), new Size(200, 200));
        surface.AddControl(panel, new Point(30, 30));

        var drop = new Point(100, 100);
        var buttonItem = surface.Toolbox.Items.First(i => i.ControlType == typeof(Button));
        surface.BeginExternalDrop(buttonItem);
        surface.RaiseMouseDown(LeftMouseDown(drop.X, drop.Y));

        var placed = surface.Items.Single(i => i.Control is Button);
        Assert.Same(panel, placed.Control.Parent);
        // Surface click (100,100) minus the panel's surface offset (30,30) => panel-local (70,70).
        Assert.Equal(new Point(70, 70), placed.Control.Location);
    }

    [Fact]
    public void Drag_OuterContainer_OverInnerContainer_DoesNotReparentIntoDescendant()
    {
        var surface = new DesignSurface { SnapToGrid = false };
        var outer = CreatePanel(new Point(30, 30), new Size(300, 300));
        surface.AddControl(outer, new Point(30, 30));

        var inner = CreatePanel(new Point(50, 50), new Size(100, 100));
        surface.AddControl(inner, outer, new Point(50, 50));
        var outerItem = surface.FindItem(outer)!;
        surface.Selection.Select(outerItem);

        var reparentCount = 0;
        surface.Drag.DragReparented += (_, _) => reparentCount++;

        // Start the drag on the outer panel (over its empty area, not the inner),
        // then move the pointer over the inner panel. The innermost container under
        // the pointer is the inner panel, which is a descendant of the dragged one.
        surface.Drag.BeginMove(new Point(40, 40));
        surface.Drag.ContinueDrag(new Point(100, 100));

        // The outer panel must never become a child of its own descendant, which
        // would create a parent/child cycle and infinite recursion.
        Assert.Same(surface, outer.Parent);
        Assert.Equal(1, reparentCount);

        surface.Drag.EndDrag();
    }

    /// <summary>
    /// Reproduces the real-app scenario where the design surface is nested inside
    /// other controls (so it carries a non-zero offset from the form origin). A
    /// toolbox drop inside a container must still land in that container rather than
    /// silently falling back to the surface because surface-local click coordinates
    /// were treated as form coordinates.
    /// </summary>
    [Fact]
    public void ContainerDrop_WithSurfaceOffset_LayersUnderCursor()
    {
        var host = new Panel { Location = new Point(250, 200), Size = new Size(500, 500) };
        var surface = new TestableDesignSurface { Location = Point.Empty, Size = new Size(400, 400), SnapToGrid = false };
        host.Controls.Add(surface);

        var panel = CreatePanel(new Point(30, 30), new Size(200, 200));
        surface.AddControl(panel, new Point(30, 30));

        var drop = new Point(100, 100);
        var buttonItem = surface.Toolbox.Items.First(i => i.ControlType == typeof(Button));
        surface.BeginExternalDrop(buttonItem);
        surface.RaiseMouseDown(LeftMouseDown(drop.X, drop.Y));

        var placed = surface.Items.Single(i => i.Control is Button);
        Assert.Same(panel, placed.Control.Parent);
        Assert.Equal(new Point(70, 70), placed.Control.Location);
    }

    /// <summary>
    /// Dragging a surface-level control onto a container that sits under a real offset
    /// must reparent the control into that container.
    /// </summary>
    [Fact]
    public void Drag_LiveReparentsIntoOffsetContainer_PreservingSurfacePosition()
    {
        var host = new Panel { Location = new Point(250, 200), Size = new Size(500, 500) };
        var surface = new DesignSurface { Location = Point.Empty, Size = new Size(400, 400), SnapToGrid = false };
        host.Controls.Add(surface);

        var panel = CreatePanel(new Point(30, 30), new Size(200, 200));
        surface.AddControl(panel, new Point(30, 30));

        var button = new Button { Size = new Size(120, 40) };
        surface.AddControl(button, new Point(320, 320));
        var buttonItem = surface.FindItem(button)!;
        surface.Selection.Select(buttonItem);

        var reparentCount = 0;
        surface.Drag.DragReparented += (_, _) => reparentCount++;

        surface.Drag.BeginMove(new Point(320, 320));
        surface.Drag.ContinueDrag(new Point(100, 100));

        Assert.Same(panel, button.Parent);
        Assert.Equal(new Point(70, 70), button.Location);
        Assert.Equal(1, reparentCount);

        surface.Drag.EndDrag();
    }

    /// <summary>
    /// Placing a new control inside a container must not change the container's own
    /// location or size. Reproduces the report that dropping a button into a panel
    /// unexpectedly moved and resized the panel.
    /// </summary>
    [Fact]
    public void Drop_IntoContainer_DoesNotAlterPanelBounds()
    {
        var host = new Panel { Location = new Point(250, 200), Size = new Size(500, 500) };
        var surface = new TestableDesignSurface { Location = Point.Empty, Size = new Size(400, 400), SnapToGrid = false };
        host.Controls.Add(surface);

        var panel = CreatePanel(new Point(30, 30), new Size(200, 150));
        surface.AddControl(panel, new Point(30, 30));

        var beforeLocation = panel.Location;
        var beforeSize = panel.Size;

        var buttonItem = surface.Toolbox.Items.First(i => i.ControlType == typeof(Button));
        surface.BeginExternalDrop(buttonItem);
        surface.RaiseMouseDown(LeftMouseDown(100, 100));

        Assert.Equal(beforeLocation, panel.Location);
        Assert.Equal(beforeSize, panel.Size);

        var placed = surface.Items.Single(i => i.Control is Button).Control;
        Assert.Same(panel, placed.Parent);
        // Surface click (100,100) minus panel surface offset (30,30) => panel-local (70,70).
        Assert.Equal(new Point(70, 70), placed.Location);
        // The button must render under the cursor in form coords: surface (100,100) + host (250,200).
        Assert.Equal(new Point(350, 300), placed.PointToScreen(Point.Empty));
    }

    /// <summary>
    /// The selection chrome for a control nested inside a container must be drawn in
    /// this surface's coordinate space, not in the parent's local space. Without the
    /// conversion the box and handles were offset by the container's surface position,
    /// which read like the container itself moving or resizing.
    /// </summary>
    [Fact]
    public void DrawSelectionChrome_ForNestedControl_DrawnAtSurfacePosition()
    {
        var surface = new DesignSurface { SnapToGrid = false, Size = new Size(400, 400) };
        var panel = CreatePanel(new Point(30, 30), new Size(200, 150));
        surface.AddControl(panel, new Point(30, 30));

        var button = new Button { Location = new Point(70, 70), Size = new Size(120, 40) };
        var buttonItem = surface.AddControl(button, panel, new Point(70, 70));
        surface.Selection.Select(buttonItem);

        using var g = new Graphics();
        g.Zoom = 1f;
        surface.Render(g);

        // Primary chrome: outline inset by 1 with lineWidth 2 and size + 2.
        var cmd = g.GetCommands()
            .FirstOrDefault(c => c.Type == DrawCommandType.DrawRectangle && Math.Abs(c.LineWidth - 2f) < 0.001f);
        Assert.NotNull(cmd);
        Assert.Equal(DrawCommandType.DrawRectangle, cmd!.Type);

        // Button surface position: panel (30,30) + button (70,70) = (100,100,120,40).
        var chrome = new Rectangle((int)(cmd.X / g.Zoom), (int)(cmd.Y / g.Zoom),
                                   (int)(cmd.Width / g.Zoom), (int)(cmd.Height / g.Zoom));
        Assert.Equal(new Rectangle(99, 99, 122, 42), chrome);
    }

    /// <summary>
    /// Placing a control inside a FlowLayoutPanel must lay it out immediately by the
    /// flow layout, not leave it at the drop point until the next layout pass.
    /// </summary>
    [Fact]
    public void ToolboxDrop_IntoFlowLayoutPanel_LaysOutImmediately()
    {
        var surface = new TestableDesignSurface { SnapToGrid = false };
        var flow = new FlowLayoutPanel { Location = new Point(30, 30), Size = new Size(200, 200) };
        surface.AddControl(flow, new Point(30, 30));

        var buttonItem = surface.Toolbox.Items.First(i => i.ControlType == typeof(Button));
        surface.BeginExternalDrop(buttonItem);

        // Click inside the flow panel. The flow layout (LeftToRight, no padding)
        // places the first control at its padding origin (0,0), overriding the drop point.
        surface.RaiseMouseDown(LeftMouseDown(100, 100));

        var placed = surface.Items.Single(i => i.Control is Button).Control;
        Assert.Same(flow, placed.Parent);
        Assert.Equal(new Point(0, 0), placed.Location);
    }
}
