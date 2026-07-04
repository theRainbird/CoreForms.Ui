using OldSchoolForms.Ui.Controls.Basic;
using OldSchoolForms.Ui.Controls.Containers;
using OldSchoolForms.Ui.Controls;
using OldSchoolForms.Ui.Core;
using Graphics = OldSchoolForms.Ui.Rendering.Graphics;

namespace OldSchoolForms.Ui.Demo.UserControls;

/// <summary>
/// Demonstrates multi-window support: modeless windows, modal dialogs, and fixed-border dialogs.
/// </summary>
public class MultiWindowPage : UserControl
{
    /// <summary>
    /// Occurs when the status text should be updated.
    /// </summary>
    public event EventHandler<StatusTextChangedEventArgs>? StatusTextChanged;

    private readonly Form _mainForm;

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiWindowPage"/> class.
    /// </summary>
    /// <param name="mainForm">The main form used as owner for modal dialogs.</param>
    public MultiWindowPage(Form mainForm)
    {
        _mainForm = mainForm;

        var modelessGroup = new GroupBox
        {
            Text = SR.GetString("GroupModeless"),
            Location = new Point(10, 10),
            Size = new Size(280, 120)
        };

        var btnSimpleWindow = new Button
        {
            Text = SR.GetString("BtnOpenSimpleWindow"),
            Location = new Point(10, 25),
            Size = new Size(250, 35)
        };
        btnSimpleWindow.Click += (s, e) =>
        {
            var win = new Form
            {
                Title = SR.GetString("SimpleWindowTitle"),
                Text = SR.GetString("SimpleWindowTitle"),
                Width = 400,
                Height = 300
            };
            var label = new Label
            {
                Text = SR.GetString("SimpleWindowLabel"),
                Location = new Point(20, 20),
                Size = new Size(350, 30)
            };
            win.Controls.Add(label);
            win.Show();
            OnStatusTextChanged(SR.GetString("StatusSimpleWindowOpened"));
        };

        var btnWindowControls = new Button
        {
            Text = SR.GetString("BtnOpenWindowWithControls"),
            Location = new Point(10, 70),
            Size = new Size(250, 35)
        };
        btnWindowControls.Click += (s, e) =>
        {
            int clickCount = 0;
            var win = new Form
            {
                Title = SR.GetString("WindowControlsTitle"),
                Text = SR.GetString("WindowControlsTitle"),
                Width = 400,
                Height = 250,
            };
            var clickLabel = new Label
            {
                Text = string.Format(SR.GetString("LabelClickCount"), 0),
                Location = new Point(20, 20),
                Size = new Size(350, 30)
            };
            var countButton = new Button
            {
                Text = SR.GetString("BtnTestButton"),
                Location = new Point(20, 60),
                Size = new Size(120, 30)
            };
            countButton.Click += (_, _) =>
            {
                clickCount++;
                clickLabel.Text = string.Format(SR.GetString("LabelClickCount"), clickCount);
            };
            win.Controls.Add(clickLabel);
            win.Controls.Add(countButton);
            win.Show();
            OnStatusTextChanged(SR.GetString("StatusWindowWithControlsOpened"));
        };

        modelessGroup.Controls.Add(btnSimpleWindow);
        modelessGroup.Controls.Add(btnWindowControls);

        var modalGroup = new GroupBox
        {
            Text = SR.GetString("GroupModal"),
            Location = new Point(300, 10),
            Size = new Size(280, 120)
        };

        var btnModalDialog = new Button
        {
            Text = SR.GetString("BtnOpenModalDialog"),
            Location = new Point(10, 25),
            Size = new Size(250, 35)
        };
        btnModalDialog.Click += (s, e) =>
        {
            var dlg = new Form
            {
                Title = SR.GetString("ModalWindowTitle"),
                Text = SR.GetString("ModalWindowTitle"),
                Width = 350,
                Height = 200,
                FormBorderStyle = FormBorderStyle.FixedDialog,
            };
            var label = new Label
            {
                Text = SR.GetString("ModalWindowLabel"),
                Location = new Point(20, 20),
                Size = new Size(300, 60)
            };
            dlg.Controls.Add(label);
            OnStatusTextChanged(SR.GetString("StatusModalDialogOpened"));
            var result = dlg.ShowDialog(_mainForm);
            OnStatusTextChanged(SR.GetString("StatusModalClosed"));
        };

        var btnModalConfirm = new Button
        {
            Text = SR.GetString("BtnOpenModalConfirm"),
            Location = new Point(10, 70),
            Size = new Size(250, 35)
        };
        btnModalConfirm.Click += (s, e) =>
        {
            var dlg = new Form
            {
                Title = SR.GetString("ConfirmTitle"),
                Text = SR.GetString("ConfirmTitle"),
                Width = 300,
                Height = 180,
                FormBorderStyle = FormBorderStyle.FixedDialog,
            };
            var label = new Label
            {
                Text = SR.GetString("ModalWindowLabel"),
                Location = new Point(20, 20),
                Size = new Size(250, 50)
            };
            var btnOK = new Button
            {
                Text = SR.GetString("OK"),
                Location = new Point(60, 90),
                Size = new Size(80, 30)
            };
            btnOK.Click += (_, _) => dlg.DialogResult = DialogResult.OK;
            var btnCancel = new Button
            {
                Text = SR.GetString("Cancel"),
                Location = new Point(150, 90),
                Size = new Size(80, 30)
            };
            btnCancel.Click += (_, _) => dlg.DialogResult = DialogResult.Cancel;
            dlg.Controls.Add(label);
            dlg.Controls.Add(btnOK);
            dlg.Controls.Add(btnCancel);
            var result = dlg.ShowDialog(_mainForm);
            if (result == DialogResult.OK)
                OnStatusTextChanged(SR.GetString("StatusModalConfirmed"));
            else
                OnStatusTextChanged(SR.GetString("StatusModalCancelled"));
        };

        modalGroup.Controls.Add(btnModalDialog);
        modalGroup.Controls.Add(btnModalConfirm);

        var styleGroup = new GroupBox
        {
            Text = SR.GetString("GroupWindowStyles"),
            Location = new Point(10, 140),
            Size = new Size(570, 80)
        };

        var btnFixedDialog = new Button
        {
            Text = SR.GetString("BtnOpenFixedDialog"),
            Location = new Point(10, 30),
            Size = new Size(250, 35)
        };
        btnFixedDialog.Click += (s, e) =>
        {
            var dlg = new Form
            {
                Title = SR.GetString("FixedDialogTitle"),
                Text = SR.GetString("FixedDialogTitle"),
                Width = 350,
                Height = 200,
                FormBorderStyle = FormBorderStyle.FixedDialog,
            };
            var label = new Label
            {
                Text = SR.GetString("ModalWindowLabel"),
                Location = new Point(20, 20),
                Size = new Size(300, 80)
            };
            dlg.Controls.Add(label);
            OnStatusTextChanged(SR.GetString("StatusFixedDialogOpened"));
            dlg.ShowDialog(_mainForm);
            OnStatusTextChanged(SR.GetString("StatusModalClosed"));
        };

        styleGroup.Controls.Add(btnFixedDialog);

        Controls.Add(modelessGroup);
        Controls.Add(modalGroup);
        Controls.Add(styleGroup);
    }

    private void OnStatusTextChanged(string text)
    {
        StatusTextChanged?.Invoke(this, new StatusTextChangedEventArgs(text));
    }
}
