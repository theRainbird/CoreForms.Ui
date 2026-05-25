using CoreForms.Ui.Core;
using CoreForms.Ui.Controls.Basic;
using CoreForms.Ui.Controls.Containers;
using CoreForms.Ui.Layout;
using Xunit;

namespace CoreForms.Ui.Tests;

public class DockAndAnchorTests
{
    private ContainerControl CreateContainer(int width = 400, int height = 300)
    {
        var container = new ContainerControl();
        container.Size = new Size(width, height);
        return container;
    }

    private Control CreateChild(int x, int y, int w, int h)
    {
        var control = new Label();
        control.Location = new Point(x, y);
        control.Size = new Size(w, h);
        return control;
    }

    [Fact]
    public void Dock_Top_ShouldFillTopEdge()
    {
        var container = CreateContainer(400, 300);
        var child = CreateChild(0, 0, 100, 50);
        child.Dock = DockStyle.Top;
        container.Controls.Add(child);

        Assert.Equal(0, child.X);
        Assert.Equal(0, child.Y);
        Assert.Equal(400, child.Width);
        Assert.Equal(50, child.Height);
    }

    [Fact]
    public void Dock_Bottom_ShouldFillBottomEdge()
    {
        var container = CreateContainer(400, 300);
        var child = CreateChild(0, 0, 100, 50);
        child.Dock = DockStyle.Bottom;
        container.Controls.Add(child);

        Assert.Equal(0, child.X);
        Assert.Equal(250, child.Y);
        Assert.Equal(400, child.Width);
        Assert.Equal(50, child.Height);
    }

    [Fact]
    public void Dock_Left_ShouldFillLeftEdge()
    {
        var container = CreateContainer(400, 300);
        var child = CreateChild(0, 0, 80, 50);
        child.Dock = DockStyle.Left;
        container.Controls.Add(child);

        Assert.Equal(0, child.X);
        Assert.Equal(0, child.Y);
        Assert.Equal(80, child.Width);
        Assert.Equal(300, child.Height);
    }

    [Fact]
    public void Dock_Right_ShouldFillRightEdge()
    {
        var container = CreateContainer(400, 300);
        var child = CreateChild(0, 0, 100, 50);
        child.Dock = DockStyle.Right;
        container.Controls.Add(child);

        Assert.Equal(300, child.X);
        Assert.Equal(0, child.Y);
        Assert.Equal(100, child.Width);
        Assert.Equal(300, child.Height);
    }

    [Fact]
    public void Dock_Fill_ShouldFillEntireArea()
    {
        var container = CreateContainer(400, 300);
        var child = CreateChild(0, 0, 100, 50);
        child.Dock = DockStyle.Fill;
        container.Controls.Add(child);

        Assert.Equal(0, child.X);
        Assert.Equal(0, child.Y);
        Assert.Equal(400, child.Width);
        Assert.Equal(300, child.Height);
    }

    [Fact]
    public void Dock_Multiple_ShouldStackSequentially()
    {
        var container = CreateContainer(400, 300);
        var top = CreateChild(0, 0, 100, 30);
        top.Dock = DockStyle.Top;
        var bottom = CreateChild(0, 0, 100, 40);
        bottom.Dock = DockStyle.Bottom;
        var left = CreateChild(0, 0, 80, 50);
        left.Dock = DockStyle.Left;

        container.Controls.Add(top);
        container.Controls.Add(bottom);
        container.Controls.Add(left);

        Assert.Equal(0, top.X);
        Assert.Equal(0, top.Y);
        Assert.Equal(400, top.Width);
        Assert.Equal(30, top.Height);

        Assert.Equal(0, bottom.X);
        Assert.Equal(260, bottom.Y);
        Assert.Equal(400, bottom.Width);
        Assert.Equal(40, bottom.Height);

        Assert.Equal(0, left.X);
        Assert.Equal(30, left.Y);
        Assert.Equal(80, left.Width);
        Assert.Equal(230, left.Height);
    }

    [Fact]
    public void Dock_Fill_WithOtherDocked_ShouldFillRemainingSpace()
    {
        var container = CreateContainer(400, 300);
        var top = CreateChild(0, 0, 100, 30);
        top.Dock = DockStyle.Top;
        var fill = CreateChild(0, 0, 50, 50);
        fill.Dock = DockStyle.Fill;

        container.Controls.Add(top);
        container.Controls.Add(fill);

        Assert.Equal(0, fill.X);
        Assert.Equal(30, fill.Y);
        Assert.Equal(400, fill.Width);
        Assert.Equal(270, fill.Height);
    }

    [Fact]
    public void Dock_WithPadding_ShouldRespectPadding()
    {
        var container = CreateContainer(400, 300);
        container.Padding = new Padding(10);
        var child = CreateChild(0, 0, 100, 50);
        child.Dock = DockStyle.Fill;
        container.Controls.Add(child);

        Assert.Equal(10, child.X);
        Assert.Equal(10, child.Y);
        Assert.Equal(380, child.Width);
        Assert.Equal(280, child.Height);
    }

    [Fact]
    public void Dock_TopThenLeft_ShouldReduceAvailableArea()
    {
        var container = CreateContainer(400, 300);
        var top = CreateChild(0, 0, 100, 50);
        top.Dock = DockStyle.Top;
        var left = CreateChild(0, 0, 80, 50);
        left.Dock = DockStyle.Left;

        container.Controls.Add(top);
        container.Controls.Add(left);

        Assert.Equal(0, left.X);
        Assert.Equal(50, left.Y);
        Assert.Equal(80, left.Width);
        Assert.Equal(250, left.Height);
    }

    [Fact]
    public void Dock_None_ShouldNotChangePosition()
    {
        var container = CreateContainer(400, 300);
        var child = CreateChild(50, 60, 100, 40);
        child.Dock = DockStyle.None;
        container.Controls.Add(child);

        Assert.Equal(50, child.X);
        Assert.Equal(60, child.Y);
        Assert.Equal(100, child.Width);
        Assert.Equal(40, child.Height);
    }

    [Fact]
    public void Anchor_TopLeft_Default_ShouldKeepPositionOnResize()
    {
        var container = CreateContainer(400, 300);
        var child = CreateChild(10, 20, 100, 50);
        child.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        container.Controls.Add(child);

        container.Size = new Size(500, 400);

        Assert.Equal(10, child.X);
        Assert.Equal(20, child.Y);
        Assert.Equal(100, child.Width);
        Assert.Equal(50, child.Height);
    }

    [Fact]
    public void Anchor_TopLeftRight_ShouldStretchHorizontally()
    {
        var container = CreateContainer(400, 300);
        var child = CreateChild(10, 20, 380, 50);
        child.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        container.Controls.Add(child);

        Assert.Equal(10, child.X);
        Assert.Equal(380, child.Width);

        container.Size = new Size(500, 400);

        Assert.Equal(10, child.X);
        Assert.Equal(20, child.Y);
        Assert.Equal(480, child.Width);
        Assert.Equal(50, child.Height);
    }

    [Fact]
    public void Anchor_TopLeftBottom_ShouldStretchVertically()
    {
        var container = CreateContainer(400, 300);
        var child = CreateChild(10, 20, 100, 270);
        child.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom;
        container.Controls.Add(child);

        container.Size = new Size(500, 400);

        Assert.Equal(10, child.X);
        Assert.Equal(20, child.Y);
        Assert.Equal(100, child.Width);
        Assert.Equal(370, child.Height);
    }

    [Fact]
    public void Anchor_TopRight_ShouldMoveWithRightEdge()
    {
        var container = CreateContainer(400, 300);
        var child = CreateChild(300, 20, 80, 50);
        child.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        container.Controls.Add(child);

        container.Size = new Size(500, 400);

        Assert.Equal(400, child.X);
        Assert.Equal(20, child.Y);
        Assert.Equal(80, child.Width);
        Assert.Equal(50, child.Height);
    }

    [Fact]
    public void Anchor_BottomLeft_ShouldMoveWithBottomEdge()
    {
        var container = CreateContainer(400, 300);
        var child = CreateChild(10, 250, 100, 40);
        child.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        container.Controls.Add(child);

        container.Size = new Size(500, 400);

        Assert.Equal(10, child.X);
        Assert.Equal(350, child.Y);
        Assert.Equal(100, child.Width);
        Assert.Equal(40, child.Height);
    }

    [Fact]
    public void Anchor_All_ShouldStretchBothDirections()
    {
        var container = CreateContainer(400, 300);
        var child = CreateChild(10, 20, 380, 270);
        child.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        container.Controls.Add(child);

        container.Size = new Size(500, 400);

        Assert.Equal(10, child.X);
        Assert.Equal(20, child.Y);
        Assert.Equal(480, child.Width);
        Assert.Equal(370, child.Height);
    }

    [Fact]
    public void Anchor_BottomRight_ShouldMoveToBottomRight()
    {
        var container = CreateContainer(400, 300);
        var child = CreateChild(300, 250, 80, 40);
        child.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        container.Controls.Add(child);

        container.Size = new Size(500, 400);

        Assert.Equal(400, child.X);
        Assert.Equal(350, child.Y);
        Assert.Equal(80, child.Width);
        Assert.Equal(40, child.Height);
    }

    [Fact]
    public void Anchor_None_ShouldKeepPositionOnResize()
    {
        var container = CreateContainer(400, 300);
        var child = CreateChild(50, 60, 100, 40);
        child.Anchor = AnchorStyles.None;
        container.Controls.Add(child);

        container.Size = new Size(500, 400);

        Assert.Equal(50, child.X);
        Assert.Equal(60, child.Y);
        Assert.Equal(100, child.Width);
        Assert.Equal(40, child.Height);
    }

    [Fact]
    public void Anchor_ChangeAfterAdd_ShouldPreserveDistances()
    {
        var container = CreateContainer(400, 300);
        var child = CreateChild(10, 20, 100, 50);
        container.Controls.Add(child);

        child.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        container.Size = new Size(500, 400);

        Assert.Equal(10, child.X);
        Assert.Equal(20, child.Y);
        Assert.Equal(200, child.Width);
        Assert.Equal(50, child.Height);
    }

    [Fact]
    public void Dock_ChangeAfterAdd_ShouldRearrange()
    {
        var container = CreateContainer(400, 300);
        var child = CreateChild(50, 60, 100, 50);
        container.Controls.Add(child);

        Assert.Equal(50, child.X);

        child.Dock = DockStyle.Top;

        Assert.Equal(0, child.X);
        Assert.Equal(0, child.Y);
        Assert.Equal(400, child.Width);
        Assert.Equal(50, child.Height);
    }

    [Fact]
    public void Dock_ContainerResize_ShouldUpdateDockedChildren()
    {
        var container = CreateContainer(400, 300);
        var child = CreateChild(0, 0, 100, 50);
        child.Dock = DockStyle.Top;
        container.Controls.Add(child);

        Assert.Equal(400, child.Width);

        container.Size = new Size(600, 400);

        Assert.Equal(0, child.X);
        Assert.Equal(0, child.Y);
        Assert.Equal(600, child.Width);
        Assert.Equal(50, child.Height);
    }

    [Fact]
    public void Padding_Struct_AllEqual()
    {
        var p = new Padding(10);
        Assert.Equal(10, p.Left);
        Assert.Equal(10, p.Top);
        Assert.Equal(10, p.Right);
        Assert.Equal(10, p.Bottom);
        Assert.Equal(20, p.Horizontal);
        Assert.Equal(20, p.Vertical);
    }

    [Fact]
    public void Padding_Struct_IndividualValues()
    {
        var p = new Padding(1, 2, 3, 4);
        Assert.Equal(1, p.Left);
        Assert.Equal(2, p.Top);
        Assert.Equal(3, p.Right);
        Assert.Equal(4, p.Bottom);
        Assert.Equal(4, p.Horizontal);
        Assert.Equal(6, p.Vertical);
    }

    [Fact]
    public void ClientRectangle_NoPadding_ShouldReturnBounds()
    {
        var container = CreateContainer(400, 300);
        var clientRect = container.ClientRectangle;

        Assert.Equal(0, clientRect.X);
        Assert.Equal(0, clientRect.Y);
        Assert.Equal(400, clientRect.Width);
        Assert.Equal(300, clientRect.Height);
    }

    [Fact]
    public void ClientRectangle_WithPadding_ShouldSubtractPadding()
    {
        var container = CreateContainer(400, 300);
        container.Padding = new Padding(10, 20, 30, 40);
        var clientRect = container.ClientRectangle;

        Assert.Equal(10, clientRect.X);
        Assert.Equal(20, clientRect.Y);
        Assert.Equal(360, clientRect.Width);
        Assert.Equal(240, clientRect.Height);
    }

    [Fact]
    public void ClientSize_WithPadding_ShouldReturnInnerSize()
    {
        var container = CreateContainer(400, 300);
        container.Padding = new Padding(10);
        var clientSize = container.ClientSize;

        Assert.Equal(380, clientSize.Width);
        Assert.Equal(280, clientSize.Height);
    }

    [Fact]
    public void SuspendLayout_ResumeLayout_ShouldBatchUpdates()
    {
        var container = CreateContainer(400, 300);
        var child1 = CreateChild(0, 0, 100, 50);
        child1.Dock = DockStyle.Top;
        var child2 = CreateChild(0, 0, 100, 50);
        child2.Dock = DockStyle.Top;

        container.SuspendLayout();
        container.Controls.Add(child1);
        container.Controls.Add(child2);
        container.ResumeLayout(true);

        Assert.Equal(0, child1.Y);
        Assert.Equal(50, child2.Y);
        Assert.Equal(400, child1.Width);
        Assert.Equal(400, child2.Width);
    }

    [Fact]
    public void SuspendLayout_WithoutResume_ShouldNotProcessLayout()
    {
        var container = CreateContainer(400, 300);
        var child = CreateChild(0, 0, 100, 50);
        child.Dock = DockStyle.Top;

        container.SuspendLayout();
        container.Controls.Add(child);
        container.Size = new Size(500, 400);

        Assert.Equal(100, child.Width);
    }

    [Fact]
    public void Dock_Fill_WithPadding_ShouldFillInnerArea()
    {
        var container = CreateContainer(400, 300);
        var docked = CreateChild(0, 0, 50, 50);
        docked.Dock = DockStyle.Fill;
        container.Padding = new Padding(10, 20, 30, 40);
        container.Controls.Add(docked);

        Assert.Equal(10, docked.X);
        Assert.Equal(20, docked.Y);
        Assert.Equal(360, docked.Width);
        Assert.Equal(240, docked.Height);
    }

    [Fact]
    public void Dock_TopThenFill_WithPadding_ShouldStackCorrectly()
    {
        var container = CreateContainer(400, 300);
        container.Padding = new Padding(10);
        var top = CreateChild(0, 0, 100, 30);
        top.Dock = DockStyle.Top;
        var fill = CreateChild(0, 0, 50, 50);
        fill.Dock = DockStyle.Fill;

        container.Controls.Add(top);
        container.Controls.Add(fill);

        Assert.Equal(10, top.X);
        Assert.Equal(10, top.Y);
        Assert.Equal(380, top.Width);
        Assert.Equal(30, top.Height);

        Assert.Equal(10, fill.X);
        Assert.Equal(40, fill.Y);
        Assert.Equal(380, fill.Width);
        Assert.Equal(250, fill.Height);
    }

    [Fact]
    public void Dock_RemoveChild_ShouldReLayoutRemaining()
    {
        var container = CreateContainer(400, 300);
        var top = CreateChild(0, 0, 100, 30);
        top.Dock = DockStyle.Top;
        var fill = CreateChild(0, 0, 50, 50);
        fill.Dock = DockStyle.Fill;

        container.Controls.Add(top);
        container.Controls.Add(fill);

        Assert.Equal(400, fill.Width);
        Assert.Equal(270, fill.Height);

        container.Controls.Remove(top);

        Assert.Equal(0, fill.X);
        Assert.Equal(0, fill.Y);
        Assert.Equal(400, fill.Width);
        Assert.Equal(300, fill.Height);
    }

    [Fact]
    public void Anchor_ManualMove_ShouldUpdateDistances()
    {
        var container = CreateContainer(400, 300);
        var child = CreateChild(10, 20, 100, 50);
        child.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        container.Controls.Add(child);

        child.Location = new Point(20, 20);
        child.Size = new Size(370, 50);

        container.Size = new Size(500, 400);

        Assert.Equal(20, child.X);
        Assert.Equal(470, child.Width);
    }

    [Fact]
    public void DockAndAnchor_MixedChildren_ShouldWorkCorrectly()
    {
        var container = CreateContainer(400, 300);
        var menu = CreateChild(0, 0, 100, 25);
        menu.Dock = DockStyle.Top;
        var status = CreateChild(0, 0, 100, 20);
        status.Dock = DockStyle.Bottom;
        var content = CreateChild(10, 35, 380, 245);
        content.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        container.Controls.Add(menu);
        container.Controls.Add(status);
        container.Controls.Add(content);

        container.Size = new Size(600, 400);

        Assert.Equal(0, menu.X);
        Assert.Equal(0, menu.Y);
        Assert.Equal(600, menu.Width);
        Assert.Equal(25, menu.Height);

        Assert.Equal(0, status.X);
        Assert.Equal(380, status.Y);
        Assert.Equal(600, status.Width);
        Assert.Equal(20, status.Height);

        Assert.Equal(10, content.X);
        Assert.Equal(35, content.Y);
        Assert.Equal(580, content.Width);
        Assert.Equal(345, content.Height);
    }

    [Fact]
    public void Anchor_LeftRight_ShouldStretchToFullWidth()
    {
        var container = CreateContainer(400, 300);
        var child = CreateChild(0, 0, 400, 50);
        child.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        container.Controls.Add(child);

        container.Size = new Size(600, 400);

        Assert.Equal(0, child.X);
        Assert.Equal(600, child.Width);
    }

    [Fact]
    public void Dock_Fill_ContainerResize_ShouldExpand()
    {
        var container = CreateContainer(400, 300);
        var fill = CreateChild(0, 0, 50, 50);
        fill.Dock = DockStyle.Fill;
        container.Controls.Add(fill);

        Assert.Equal(0, fill.X);
        Assert.Equal(0, fill.Y);
        Assert.Equal(400, fill.Width);
        Assert.Equal(300, fill.Height);

        container.Size = new Size(800, 600);

        Assert.Equal(0, fill.X);
        Assert.Equal(0, fill.Y);
        Assert.Equal(800, fill.Width);
        Assert.Equal(600, fill.Height);
    }

    [Fact]
    public void Dock_TopAndFill_ContainerResize_ShouldExpand()
    {
        var container = CreateContainer(900, 650);
        var top = CreateChild(0, 0, 900, 30);
        top.Dock = DockStyle.Top;
        var fill = CreateChild(0, 0, 900, 600);
        fill.Dock = DockStyle.Fill;
        container.Controls.Add(top);
        container.Controls.Add(fill);

        Assert.Equal(0, top.X);
        Assert.Equal(0, top.Y);
        Assert.Equal(900, top.Width);
        Assert.Equal(30, top.Height);
        Assert.Equal(0, fill.X);
        Assert.Equal(30, fill.Y);
        Assert.Equal(900, fill.Width);
        Assert.Equal(620, fill.Height);

        container.Size = new Size(1200, 800);

        Assert.Equal(0, top.X);
        Assert.Equal(0, top.Y);
        Assert.Equal(1200, top.Width);
        Assert.Equal(30, top.Height);
        Assert.Equal(0, fill.X);
        Assert.Equal(30, fill.Y);
        Assert.Equal(1200, fill.Width);
        Assert.Equal(770, fill.Height);
    }

    [Fact]
    public void Dock_SuspendLayout_Resize_ShouldWork()
    {
        var container = CreateContainer(900, 650);
        var top = CreateChild(0, 0, 900, 30);
        top.Dock = DockStyle.Top;
        var fill = CreateChild(0, 0, 900, 600);
        fill.Dock = DockStyle.Fill;
        container.Controls.Add(top);
        container.Controls.Add(fill);

        container.SuspendLayout();
        container.Width = 1200;
        container.Height = 800;
        container.ResumeLayout(true);

        Assert.Equal(1200, top.Width);
        Assert.Equal(1200, fill.Width);
        Assert.Equal(770, fill.Height);
    }

    [Fact]
    public void Dock_TwoStepResize_ShouldWork()
    {
        var container = CreateContainer(900, 650);
        var top = CreateChild(0, 0, 900, 30);
        top.Dock = DockStyle.Top;
        var fill = CreateChild(0, 0, 900, 600);
        fill.Dock = DockStyle.Fill;
        container.Controls.Add(top);
        container.Controls.Add(fill);

        container.Width = 1200;
        container.Height = 800;

        Assert.Equal(1200, top.Width);
        Assert.Equal(1200, fill.Width);
        Assert.Equal(770, fill.Height);
    }

    [Fact]
    public void Form_DockFill_Resize_ShouldWork()
    {
        var form = new Form();
        form.Width = 900;
        form.Height = 650;

        var menuStrip = new CoreForms.Ui.Controls.Containers.MenuStrip();
        menuStrip.Dock = DockStyle.Top;
        menuStrip.Size = new Size(900, 30);
        form.Controls.Add(menuStrip);

        var mainPanel = new Panel();
        mainPanel.Dock = DockStyle.Fill;
        mainPanel.Size = new Size(900, 600);
        form.Controls.Add(mainPanel);

        Assert.Equal(900, menuStrip.Width);
        Assert.Equal(30, menuStrip.Height);
        Assert.Equal(0, menuStrip.X);
        Assert.Equal(0, menuStrip.Y);

        Assert.Equal(0, mainPanel.X);
        Assert.Equal(30, mainPanel.Y);
        Assert.Equal(900, mainPanel.Width);
        Assert.Equal(620, mainPanel.Height);

        form.SuspendLayout();
        form.Width = 1200;
        form.Height = 800;
        form.ResumeLayout(true);

        Assert.Equal(1200, menuStrip.Width);
        Assert.Equal(0, mainPanel.X);
        Assert.Equal(30, mainPanel.Y);
        Assert.Equal(1200, mainPanel.Width);
        Assert.Equal(770, mainPanel.Height);
    }

    [Fact]
    public void DockFill_NestedPanel_Resize_ShouldUpdateAnchoredChildren()
    {
        var form = new Form();
        form.Width = 900;
        form.Height = 650;

        var menuStrip = new CoreForms.Ui.Controls.Containers.MenuStrip();
        menuStrip.Dock = DockStyle.Top;
        menuStrip.Size = new Size(900, 30);
        form.Controls.Add(menuStrip);

        var mainPanel = new Panel();
        mainPanel.Dock = DockStyle.Fill;
        mainPanel.Size = new Size(900, 600);
        form.Controls.Add(mainPanel);

        var headerLabel = new Label();
        headerLabel.Text = "Header";
        headerLabel.Location = new Point(15, 10);
        headerLabel.Size = new Size(870, 25);
        headerLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        mainPanel.Controls.Add(headerLabel);

        form.SuspendLayout();
        form.Width = 1200;
        form.Height = 800;
        form.ResumeLayout(true);

        Assert.Equal(1200, mainPanel.Width);
        Assert.Equal(770, mainPanel.Height);
        Assert.Equal(15, headerLabel.X);
        Assert.Equal(10, headerLabel.Y);
        Assert.Equal(1170, headerLabel.Width);
        Assert.Equal(25, headerLabel.Height);
    }

    [Fact]
    public void DockFill_TwoStepResize_WithoutSuspendLayout()
    {
        var form = new Form();
        form.Width = 900;
        form.Height = 650;

        var mainPanel = new Panel();
        mainPanel.Dock = DockStyle.Fill;
        form.Controls.Add(mainPanel);

        var headerLabel = new Label();
        headerLabel.Location = new Point(15, 10);
        headerLabel.Size = new Size(870, 25);
        headerLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        mainPanel.Controls.Add(headerLabel);

        form.Width = 1200;
        form.Height = 800;

        Assert.Equal(1200, mainPanel.Width);
        Assert.Equal(800, mainPanel.Height);
        Assert.Equal(1170, headerLabel.Width);
    }

[Fact]
    public void DockFullScenario_PlatformResizeSimulation()
    {
        var form = new Form();
        form.Width = 900;
        form.Height = 650;

        var menuStrip = new CoreForms.Ui.Controls.Containers.MenuStrip();
        menuStrip.Dock = DockStyle.Top;
        menuStrip.Size = new Size(900, 30);
        form.Controls.Add(menuStrip);

        var statusStrip = new Panel { BackColor = SystemColors.Control, Size = new Size(900, 24) };
        statusStrip.Dock = DockStyle.Bottom;
        form.Controls.Add(statusStrip);

        var leftPanel = new Panel { BackColor = Color.FromArgb(240, 240, 240), Size = new Size(180, 500) };
        leftPanel.Dock = DockStyle.Left;
        form.Controls.Add(leftPanel);

        var navListBox = new ListBox { Location = new Point(10, 35), Size = new Size(150, 300) };
        navListBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        leftPanel.Controls.Add(navListBox);

        var mainPanel = new Panel { BackColor = Color.White, Size = new Size(900, 600) };
        mainPanel.Dock = DockStyle.Fill;
        form.Controls.Add(mainPanel);

        var headerLabel = new Label
        {
            Text = "Header",
            Location = new Point(15, 10),
            Size = new Size(870, 25),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        mainPanel.Controls.Add(headerLabel);

        // Simulate platform resize: set Width first, then Height
        form.Width = 1200;
        form.Height = 800;

        Assert.Equal(1200, menuStrip.Width);
        Assert.Equal(1200, statusStrip.Width);
        Assert.Equal(180, leftPanel.Width);
        Assert.Equal(746, leftPanel.Height);
        Assert.Equal(1200 - 180, mainPanel.Width);
        Assert.Equal(746, mainPanel.Height);
        // headerLabel with Anchor=Top|Left|Right: starts at X=15, initial size fits in parent
        // anchorRightDistance = max(0, mainPanelWidth - (15 + 870)) but 870 > mainPanelWidth(720),
        // so anchorRightDistance = max(0, 720 - 885) = 0
        // After resize: width = mainPanelNewWidth - x - anchorRightDistance = 1020 - 15 - 0 = 1005
        // But actually headerLabel size is set to 870 which is > 720 (initial mainPanel width)
        // With the fix, anchorRightDistance = max(0, 720-885) = 0
        // After resize to mainPanel.Width=1020: headerLabel.Width = 1020 - 15 - 0 = 1005
        Assert.Equal(1005, headerLabel.Width);
    }

    [Fact]
    public void Dock_NoneWithAnchoredSibling_ShouldNotInterfere()
    {
        var container = CreateContainer(400, 300);
        var docked = CreateChild(0, 0, 100, 50);
        docked.Dock = DockStyle.Top;
        var anchored = CreateChild(10, 60, 380, 230);
        anchored.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        container.Controls.Add(docked);
        container.Controls.Add(anchored);

        container.Size = new Size(500, 400);

        Assert.Equal(0, docked.X);
        Assert.Equal(0, docked.Y);
        Assert.Equal(500, docked.Width);
        Assert.Equal(50, docked.Height);

        Assert.Equal(10, anchored.X);
        Assert.Equal(60, anchored.Y);
        Assert.Equal(480, anchored.Width);
        Assert.Equal(330, anchored.Height);
    }
}