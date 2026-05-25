using CoreForms.Ui.Controls.Basic;
using CoreForms.Ui.Controls.Containers;
using CoreForms.Ui.Controls.Separators;
using CoreForms.Ui.Controls;
using Graphics = CoreForms.Ui.Rendering.Graphics;
using CoreForms.Ui.Core;

namespace CoreForms.Ui.Demo.UserControls;

/// <summary>
/// Demonstrates Dock and Anchor layout styles.
/// </summary>
public class DockAnchorPage : UserControl
{
    /// <summary>
    /// Occurs when the status text should be updated.
    /// </summary>
    public event EventHandler<StatusTextChangedEventArgs>? StatusTextChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="DockAnchorPage"/> class.
    /// </summary>
    public DockAnchorPage()
    {
        var dockPanel = new Panel
        {
            Location = new Point(10, 10),
            Size = new Size(250, 300),
            BorderStyle = BorderStyle.FixedSingle
        };

        var dockedTopLabel = new Label { Text = SR.GetString("LabelDockTop"), Size = new Size(250, 22) };
        dockedTopLabel.Dock = DockStyle.Top;

        var hSep = new SeperatorControl
        {
            Orientation = SeperatorOrientation.Horizontal,
            Size = new Size(250, 2),
            Dock = DockStyle.Top
        };

        var dockedBottomLabel = new Label { Text = SR.GetString("LabelDockBottom"), Size = new Size(250, 22) };
        dockedBottomLabel.Dock = DockStyle.Bottom;

        var dockedLeftLabel = new Label { Text = SR.GetString("LabelDockLeft"), Size = new Size(30, 176) };
        dockedLeftLabel.Dock = DockStyle.Left;

        var vSep = new SeperatorControl
        {
            Orientation = SeperatorOrientation.Vertical,
            Size = new Size(2, 176),
            Dock = DockStyle.Left
        };

        var centerLabel = new Label { Text = SR.GetString("LabelDockFill"), Size = new Size(50, 50) };
        centerLabel.Dock = DockStyle.Fill;

        dockPanel.Controls.Add(dockedTopLabel);
        dockPanel.Controls.Add(hSep);
        dockPanel.Controls.Add(dockedBottomLabel);
        dockPanel.Controls.Add(dockedLeftLabel);
        dockPanel.Controls.Add(vSep);
        dockPanel.Controls.Add(centerLabel);

        var anchorPanel = new Panel
        {
            Location = new Point(270, 10),
            Size = new Size(300, 300),
            BorderStyle = BorderStyle.FixedSingle
        };

        var anchorLabel = new Label
        {
            Text = SR.GetString("LabelAnchorAll"),
            Location = new Point(10, 10),
            Size = new Size(280, 280)
        };
        anchorLabel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        anchorPanel.Controls.Add(anchorLabel);

        Controls.Add(dockPanel);
        Controls.Add(anchorPanel);
    }
}
