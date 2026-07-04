using OldSchoolForms.Ui.Controls;
using OldSchoolForms.Ui.Controls.Basic;
using OldSchoolForms.Ui.Controls.Containers;
using OldSchoolForms.Ui.Core;
using OldSchoolForms.Ui.Layout;
using Graphics = OldSchoolForms.Ui.Rendering.Graphics;

namespace OldSchoolForms.Ui.Demo.UserControls;

/// <summary>
/// Demonstrates FlowLayoutPanel and TableLayoutPanel controls.
/// </summary>
public class LayoutPage : UserControl
{
    private readonly FlowLayoutPanel _flowPanel;
    private readonly TableLayoutPanel _tablePanel;
    private readonly Label _flowStatus;
    private int _itemCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="LayoutPage"/> class.
    /// </summary>
    public LayoutPage()
    {
        var flowGroup = new GroupBox
        {
            Text = "FlowLayoutPanel",
            Location = new Point(10, 10),
            Size = new Size(370, 380),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left
        };

        _flowPanel = new FlowLayoutPanel
        {
            Location = new Point(10, 25),
            Size = new Size(350, 310),
            Padding = 5,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        _flowPanel.Controls.Add(CreateFlowButton("Item 1"));
        _flowPanel.Controls.Add(CreateFlowButton("Item 2"));
        _flowPanel.Controls.Add(CreateFlowButton("Item 3"));
        _flowPanel.Controls.Add(CreateFlowButton("Item 4"));
        _flowPanel.Controls.Add(CreateFlowButton("Item 5"));

        var addButton = new Button
        {
            Text = "Add Item",
            Location = new Point(10, 340),
            Size = new Size(100, 25),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left
        };
        addButton.Click += (_, _) =>
        {
            _itemCount++;
            _flowPanel.Controls.Add(CreateFlowButton($"Item {_itemCount}"));
            _flowStatus.Text = $"Items: {_flowPanel.Controls.Count}";
        };

        var removeButton = new Button
        {
            Text = "Remove Last",
            Location = new Point(120, 340),
            Size = new Size(100, 25),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left
        };
        removeButton.Click += (_, _) =>
        {
            if (_flowPanel.Controls.Count > 0)
            {
                _flowPanel.Controls.RemoveAt(_flowPanel.Controls.Count - 1);
                _flowStatus.Text = $"Items: {_flowPanel.Controls.Count}";
            }
        };

        _flowStatus = new Label
        {
            Text = "Items: 5",
            Location = new Point(230, 350),
            Size = new Size(120, 20),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        flowGroup.Controls.Add(_flowPanel);
        flowGroup.Controls.Add(addButton);
        flowGroup.Controls.Add(removeButton);
        flowGroup.Controls.Add(_flowStatus);

        var tableGroup = new GroupBox
        {
            Text = "TableLayoutPanel (3x3)",
            Location = new Point(390, 10),
            Size = new Size(380, 380),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        _tablePanel = new TableLayoutPanel
        {
            Location = new Point(10, 25),
            Size = new Size(360, 345),
            RowCount = 3,
            ColumnCount = 3,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        for (int i = 1; i <= 9; i++)
        {
            _tablePanel.Controls.Add(new Button
            {
                Text = $"Cell {i}",
                BackColor = Core.Color.FromArgb(220, 230, 250)
            });
        }

        tableGroup.Controls.Add(_tablePanel);

        Controls.Add(flowGroup);
        Controls.Add(tableGroup);
    }

    private static Button CreateFlowButton(string text)
    {
        return new Button
        {
            Text = text,
            Size = new Size(90, 30),
            BackColor = Core.Color.FromArgb(230, 245, 230)
        };
    }
}
