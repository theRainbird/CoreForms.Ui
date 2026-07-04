using OldSchoolForms.Ui.Controls.Basic;
using OldSchoolForms.Ui.Controls.Containers;
using OldSchoolForms.Ui.Controls;
using Graphics = OldSchoolForms.Ui.Rendering.Graphics;
using OldSchoolForms.Ui.Core;

namespace OldSchoolForms.Ui.Demo.UserControls;

/// <summary>
/// Demonstrates SplitPanel with vertical and horizontal orientations, splitter distance control, and min-size.
/// </summary>
public class SplitPanelPage : UserControl
{
    /// <summary>
    /// Occurs when the status text should be updated.
    /// </summary>
    public event EventHandler<StatusTextChangedEventArgs>? StatusTextChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="SplitPanelPage"/> class.
    /// </summary>
    public SplitPanelPage()
    {
        var verticalSplit = new SplitPanel
        {
            Location = new Point(10, 10),
            Size = new Size(300, 250),
            Orientation = SplitOrientation.Vertical,
            SplitterDistance = 120,
            BorderStyle = BorderStyle.FixedSingle
        };

        var topLabel = new Label
        {
            Text = SR.GetString("LabelTopPanel"),
            Location = new Point(10, 10),
            Size = new Size(100, 30)
        };
        var topButton = new Button
        {
            Text = SR.GetString("BtnButtonInTop"),
            Location = new Point(10, 50),
            Size = new Size(120, 25)
        };
        topButton.Click += (s, e) => OnStatusTextChanged(SR.GetString("StatusTopButtonClicked"));
        verticalSplit.Panel1.Controls.Add(topLabel);
        verticalSplit.Panel1.Controls.Add(topButton);

        var bottomTextBox = new TextBox
        {
            Text = SR.GetString("LabelBottomPanel"),
            Location = new Point(10, 10),
            Size = new Size(260, 25)
        };
        verticalSplit.Panel2.Controls.Add(bottomTextBox);

        var horizontalSplit = new SplitPanel
        {
            Location = new Point(320, 10),
            Size = new Size(300, 250),
            Orientation = SplitOrientation.Horizontal,
            SplitterDistance = 120
        };

        var leftLabel = new Label
        {
            Text = SR.GetString("LabelLeftPanel"),
            Location = new Point(10, 10),
            Size = new Size(100, 30)
        };
        horizontalSplit.Panel1.Controls.Add(leftLabel);

        var rightLabel = new Label
        {
            Text = SR.GetString("LabelRightPanel"),
            Location = new Point(10, 10),
            Size = new Size(140, 30)
        };
        var rightButton = new Button
        {
            Text = SR.GetString("BtnButtonInRight"),
            Location = new Point(10, 50),
            Size = new Size(120, 25)
        };
        rightButton.Click += (s, e) => OnStatusTextChanged(SR.GetString("StatusRightButtonClicked"));
        horizontalSplit.Panel2.Controls.Add(rightLabel);
        horizontalSplit.Panel2.Controls.Add(rightButton);

        var distanceLabel = new Label
        {
            Text = string.Format(SR.GetString("StatusSplitterDistFormat"), verticalSplit.SplitterDistance),
            Location = new Point(10, 275),
            Size = new Size(200, 20)
        };

        var set200Button = new Button
        {
            Text = SR.GetString("BtnSetDist200"),
            Location = new Point(10, 300),
            Size = new Size(120, 25)
        };
        set200Button.Click += (s, e) =>
        {
            verticalSplit.SplitterDistance = 200;
            distanceLabel.Text = string.Format(SR.GetString("StatusSplitterDistFormat"), verticalSplit.SplitterDistance);
            OnStatusTextChanged(SR.GetString("StatusSplitterSet200"));
        };

        var toggleOrientationButton = new Button
        {
            Text = SR.GetString("BtnToggleOrientation"),
            Location = new Point(140, 300),
            Size = new Size(150, 25)
        };
        toggleOrientationButton.Click += (s, e) =>
        {
            horizontalSplit.Orientation = horizontalSplit.Orientation == SplitOrientation.Horizontal
                ? SplitOrientation.Vertical
                : SplitOrientation.Horizontal;
            OnStatusTextChanged(string.Format(SR.GetString("StatusOrientationFormat"), horizontalSplit.Orientation));
        };

        var incMinSizeButton = new Button
        {
            Text = SR.GetString("BtnPanel1Min10"),
            Location = new Point(10, 335),
            Size = new Size(120, 25)
        };
        incMinSizeButton.Click += (s, e) =>
        {
            verticalSplit.Panel1MinSize += 10;
            OnStatusTextChanged(string.Format(SR.GetString("StatusPanel1MinSizeFormat"), verticalSplit.Panel1MinSize));
        };

        verticalSplit.SplitterMoved += (s, e) =>
        {
            distanceLabel.Text = string.Format(SR.GetString("StatusSplitterDistFormat"), verticalSplit.SplitterDistance);
            OnStatusTextChanged(string.Format(SR.GetString("StatusSplitterMovedFormat"), verticalSplit.SplitterDistance));
        };

        Controls.Add(verticalSplit);
        Controls.Add(horizontalSplit);
        Controls.Add(distanceLabel);
        Controls.Add(set200Button);
        Controls.Add(toggleOrientationButton);
        Controls.Add(incMinSizeButton);
    }

    private void OnStatusTextChanged(string text)
    {
        StatusTextChanged?.Invoke(this, new StatusTextChangedEventArgs(text));
    }
}
