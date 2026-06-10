using CoreForms.Ui.Controls.Basic;
using CoreForms.Ui.Controls;
using CoreForms.Ui.Core;
using CoreForms.Ui.Core.Dialogs;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Demo.UserControls;

/// <summary>
/// Demonstrates all MessageBox button and icon combinations.
/// </summary>
public class MessageBoxPage : UserControl
{
    /// <summary>
    /// Occurs when the status text should be updated.
    /// </summary>
    public event EventHandler<StatusTextChangedEventArgs>? StatusTextChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageBoxPage"/> class.
    /// </summary>
    public MessageBoxPage()
    {
        var infoButton = new Button { Text = SR.GetString("BtnInformation"), Location = new Point(10, 10), Size = new Size(150, 35) };
        infoButton.Click += (s, e) =>
        {
            var result = MessageBox.Show(SR.GetString("MsgTextInformation"), SR.GetString("MsgTitleInformation"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            OnStatusTextChanged(string.Format(SR.GetString("StatusResultFormat"), result));
        };

        var warningButton = new Button { Text = SR.GetString("BtnWarning"), Location = new Point(170, 10), Size = new Size(150, 35) };
        warningButton.Click += (s, e) =>
        {
            var result = MessageBox.Show(SR.GetString("MsgTextWarning"), SR.GetString("MsgTitleWarning"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            OnStatusTextChanged(string.Format(SR.GetString("StatusResultFormat"), result));
        };

        var errorButton = new Button { Text = SR.GetString("BtnError"), Location = new Point(330, 10), Size = new Size(150, 35) };
        errorButton.Click += (s, e) =>
        {
            var result = MessageBox.Show(SR.GetString("MsgTextError"), SR.GetString("MsgTitleError"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            OnStatusTextChanged(string.Format(SR.GetString("StatusResultFormat"), result));
        };

        var questionButton = new Button { Text = SR.GetString("BtnQuestionYesNo"), Location = new Point(10, 55), Size = new Size(150, 35) };
        questionButton.Click += (s, e) =>
        {
            var result = MessageBox.Show(SR.GetString("MsgTextQuestion"), SR.GetString("MsgTitleQuestion"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            OnStatusTextChanged(string.Format(SR.GetString("StatusResultFormat"), result));
        };

        var okCancelButton = new Button { Text = SR.GetString("BtnOKCancel"), Location = new Point(170, 55), Size = new Size(150, 35) };
        okCancelButton.Click += (s, e) =>
        {
            var result = MessageBox.Show(SR.GetString("MsgTextConfirmCancel"), SR.GetString("MsgTitleConfirmation"), MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
            OnStatusTextChanged(string.Format(SR.GetString("StatusResultFormat"), result));
        };

        var yesNoCancelButton = new Button { Text = SR.GetString("BtnYesNoCancel"), Location = new Point(330, 55), Size = new Size(180, 35) };
        yesNoCancelButton.Click += (s, e) =>
        {
            var result = MessageBox.Show(SR.GetString("MsgTextSaveChanges"), SR.GetString("MsgTitleSave"), MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
            OnStatusTextChanged(string.Format(SR.GetString("StatusResultFormat"), result));
        };

        var retryButton = new Button { Text = SR.GetString("BtnRetryCancel"), Location = new Point(10, 100), Size = new Size(180, 35) };
        retryButton.Click += (s, e) =>
        {
            var result = MessageBox.Show(SR.GetString("MsgTextConnectionFailed"), SR.GetString("MsgTitleConnection"), MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
            OnStatusTextChanged(string.Format(SR.GetString("StatusResultFormat"), result));
        };

        var abortButton = new Button { Text = SR.GetString("BtnAbortRetryIgnore"), Location = new Point(200, 100), Size = new Size(250, 35) };
        abortButton.Click += (s, e) =>
        {
            var result = MessageBox.Show(SR.GetString("MsgTextOperation"), SR.GetString("MsgTitleOperation"), MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Error);
            OnStatusTextChanged(string.Format(SR.GetString("StatusResultFormat"), result));
        };

        var colorPickerButton = new Button { Text = SR.GetString("BtnColorPicker"), Location = new Point(10, 145), Size = new Size(180, 35) };
        colorPickerButton.Click += (s, e) =>
        {
            var initialColor = Color.FromArgb(255, 0, 0);
            var result = ColorPickerDialog.ShowDialog(initialColor, false);
            OnStatusTextChanged(string.Format(SR.GetString("StatusColorPickerResult"), result, initialColor));
        };

        Controls.Add(infoButton);
        Controls.Add(warningButton);
        Controls.Add(errorButton);
        Controls.Add(questionButton);
        Controls.Add(okCancelButton);
        Controls.Add(yesNoCancelButton);
        Controls.Add(retryButton);
        Controls.Add(abortButton);
        Controls.Add(colorPickerButton);
    }

    private void OnStatusTextChanged(string text)
    {
        StatusTextChanged?.Invoke(this, new StatusTextChangedEventArgs(text));
    }
}
