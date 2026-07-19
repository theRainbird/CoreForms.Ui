using OldSchoolForms.Ui.Controls.Advanced;
using OldSchoolForms.Ui.Controls.Basic;
using OldSchoolForms.Ui.Controls.Containers;
using OldSchoolForms.Ui.Controls;
using OldSchoolForms.Ui.Resources;
using OldSchoolForms.Ui.Core;

namespace OldSchoolForms.Ui.Demo.UserControls;

/// <summary>
/// Demonstrates the HtmlBox table features: insert table, add/remove rows and columns, cell navigation.
/// </summary>
public class HtmlBoxTablePage : UserControl
{
    public event EventHandler<StatusTextChangedEventArgs>? StatusTextChanged;

    public HtmlBoxTablePage()
    {
        var toolStrip = new ToolStrip { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden };

        var insertTableButton = new ToolStripButton("Insert Table", Icons.Table24!)
            { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText };
        var addRowButton = new ToolStripButton("Add Row", Icons.AddCircle24!)
            { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText };
        var removeRowButton = new ToolStripButton("Remove Row", Icons.Minus!)
            { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText };
        var addColButton = new ToolStripButton("Add Column", Icons.AddCircle24!)
            { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText };
        var removeColButton = new ToolStripButton("Remove Column", Icons.Minus!)
            { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText };

        var htmlBox = new HtmlBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = false,
            LinkBehavior = LinkBehavior.OpenInBrowser,
            Html = @"<h1>HTML Table Demo</h1>
<p>Place the cursor in the text below and click <b>Insert Table</b> to add a table, or start typing in an existing table.</p>
<p>Try inserting a table, then use <b>Add Row</b>, <b>Remove Row</b>, <b>Add Column</b>, <b>Remove Column</b> to modify it.</p>"
        };

        insertTableButton.Click += (s, e) =>
        {
            htmlBox.InsertTable(3, 3);
            OnStatusTextChanged("Inserted 3x3 table");
        };

        addRowButton.Click += (s, e) =>
        {
            htmlBox.AddTableRow();
            OnStatusTextChanged("Added row");
        };

        removeRowButton.Click += (s, e) =>
        {
            htmlBox.RemoveTableRow();
            OnStatusTextChanged("Removed row");
        };

        addColButton.Click += (s, e) =>
        {
            htmlBox.AddTableColumn();
            OnStatusTextChanged("Added column");
        };

        removeColButton.Click += (s, e) =>
        {
            htmlBox.RemoveTableColumn();
            OnStatusTextChanged("Removed column");
        };

        htmlBox.ContentChanged += (s, e) =>
        {
            OnStatusTextChanged(htmlBox.CursorDebug);
        };

        toolStrip.Items.Add(insertTableButton);
        toolStrip.Items.Add(new ToolStripSeparator());
        toolStrip.Items.Add(addRowButton);
        toolStrip.Items.Add(removeRowButton);
        toolStrip.Items.Add(new ToolStripSeparator());
        toolStrip.Items.Add(addColButton);
        toolStrip.Items.Add(removeColButton);

        Controls.Add(toolStrip);
        Controls.Add(htmlBox);
    }

    private void OnStatusTextChanged(string text)
    {
        StatusTextChanged?.Invoke(this, new StatusTextChangedEventArgs(text));
    }
}
