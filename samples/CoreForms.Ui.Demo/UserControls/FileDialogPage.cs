using CoreForms.Ui.Controls.Basic;
using CoreForms.Ui.Controls.Containers;
using CoreForms.Ui.Controls;
using Graphics = CoreForms.Ui.Rendering.Graphics;
using CoreForms.Ui.Core;

namespace CoreForms.Ui.Demo.UserControls;

/// <summary>
/// Demonstrates OpenFileDialog and SaveFileDialog usage.
/// </summary>
public class FileDialogPage : UserControl
{
    /// <summary>
    /// Occurs when the status text should be updated.
    /// </summary>
    public event EventHandler<StatusTextChangedEventArgs>? StatusTextChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileDialogPage"/> class.
    /// </summary>
    public FileDialogPage()
    {
        var dbusTestButton = new Button
        {
            Text = SR.GetString("BtnTestDBus"),
            Location = new Point(10, 10),
            Size = new Size(180, 30)
        };
        dbusTestButton.Click += (s, e) =>
        {
            OnStatusTextChanged(SR.GetString("StatusTestDBus"));
        };

        var groupBox = new GroupBox
        {
            Text = SR.GetString("GroupOpenFileDialog"),
            Location = new Point(10, 50),
            Size = new Size(350, 250)
        };

        var openSingleButton = new Button { Text = SR.GetString("BtnOpenFile"), Location = new Point(10, 25), Size = new Size(150, 35) };
        openSingleButton.Click += (s, e) =>
        {
            var dlg = new OpenFileDialog
            {
                Title = SR.GetString("DlgTitleSelectTextFile"),
                Filter = SR.GetString("DlgFilterTextAndAll"),
                FilterIndex = 1,
                CheckFileExists = true
            };
            var result = dlg.ShowDialog();
            OnStatusTextChanged(result == DialogResult.OK
                ? string.Format(SR.GetString("StatusOpenFormat"), dlg.FileName)
                : SR.GetString("StatusOpenCancelled"));
        };

        var openMultiButton = new Button { Text = SR.GetString("BtnOpenFilesMulti"), Location = new Point(10, 70), Size = new Size(150, 35) };
        openMultiButton.Click += (s, e) =>
        {
            var dlg = new OpenFileDialog
            {
                Title = SR.GetString("DlgTitleSelectMulti"),
                Filter = SR.GetString("DlgFilterAllCSharpText"),
                Multiselect = true,
                CheckFileExists = true
            };
            var result = dlg.ShowDialog();
            if (result == DialogResult.OK)
            {
                OnStatusTextChanged(string.Format(SR.GetString("StatusOpenFormat"), string.Join("; ", dlg.FileNames)));
            }
            else
            {
                OnStatusTextChanged(SR.GetString("StatusOpenCancelled"));
            }
        };

        var openWithInitialDirButton = new Button { Text = SR.GetString("BtnOpenFromTemp"), Location = new Point(10, 115), Size = new Size(150, 35) };
        openWithInitialDirButton.Click += (s, e) =>
        {
            var dlg = new OpenFileDialog
            {
                Title = SR.GetString("DlgTitleOpenTemp"),
                InitialDirectory = "/tmp",
                Filter = SR.GetString("DlgFilterAll"),
                DefaultExt = "txt",
                AddExtension = true
            };
            var result = dlg.ShowDialog();
            OnStatusTextChanged(result == DialogResult.OK
                ? string.Format(SR.GetString("StatusOpenFormat"), dlg.FileName)
                : SR.GetString("StatusOpenCancelled"));
        };

        var resultLabel = new Label
        {
            Text = SR.GetString("LabelResultInStatusBar"),
            Location = new Point(10, 170),
            Size = new Size(320, 50)
        };

        groupBox.Controls.Add(openSingleButton);
        groupBox.Controls.Add(openMultiButton);
        groupBox.Controls.Add(openWithInitialDirButton);
        groupBox.Controls.Add(resultLabel);

        var saveGroupBox = new GroupBox
        {
            Text = SR.GetString("GroupSaveFileDialog"),
            Location = new Point(370, 50),
            Size = new Size(350, 250)
        };

        var saveButton = new Button { Text = SR.GetString("BtnSaveFile"), Location = new Point(10, 25), Size = new Size(150, 35) };
        saveButton.Click += (s, e) =>
        {
            var dlg = new SaveFileDialog
            {
                Title = SR.GetString("DlgTitleSaveAs"),
                Filter = SR.GetString("DlgFilterTextAndAll"),
                DefaultExt = "txt",
                AddExtension = true,
                OverwritePrompt = true,
                FileName = SR.GetString("DefaultSaveFileName")
            };
            var result = dlg.ShowDialog();
            OnStatusTextChanged(result == DialogResult.OK
                ? string.Format(SR.GetString("StatusSaveFormat"), dlg.FileName)
                : SR.GetString("StatusSaveCancelled"));
        };

        var saveWithDirButton = new Button { Text = SR.GetString("BtnSaveToTemp"), Location = new Point(10, 70), Size = new Size(150, 35) };
        saveWithDirButton.Click += (s, e) =>
        {
            var dlg = new SaveFileDialog
            {
                Title = SR.GetString("DlgTitleSaveTemp"),
                InitialDirectory = "/tmp",
                Filter = SR.GetString("DlgFilterCSharpAll"),
                DefaultExt = "cs",
                AddExtension = true,
                OverwritePrompt = true,
                FileName = SR.GetString("DefaultSaveFileNameCs")
            };
            var result = dlg.ShowDialog();
            OnStatusTextChanged(result == DialogResult.OK
                ? string.Format(SR.GetString("StatusSaveFormat"), dlg.FileName)
                : SR.GetString("StatusSaveCancelled"));
        };

        saveGroupBox.Controls.Add(saveButton);
        saveGroupBox.Controls.Add(saveWithDirButton);

        Controls.Add(dbusTestButton);
        Controls.Add(groupBox);
        Controls.Add(saveGroupBox);
    }

    private void OnStatusTextChanged(string text)
    {
        StatusTextChanged?.Invoke(this, new StatusTextChangedEventArgs(text));
    }
}
