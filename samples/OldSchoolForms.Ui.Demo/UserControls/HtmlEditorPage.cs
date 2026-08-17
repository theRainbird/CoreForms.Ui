using OldSchoolForms.Ui.Controls.Advanced;
using OldSchoolForms.Ui.Controls.Basic;
using OldSchoolForms.Ui.Controls.Containers;
using OldSchoolForms.Ui.Controls;
using OldSchoolForms.Ui.Resources;
using Graphics = OldSchoolForms.Ui.Rendering.Graphics;
using OldSchoolForms.Ui.Core;

namespace OldSchoolForms.Ui.Demo.UserControls;

/// <summary>
/// Demonstrates the HtmlBox rich text editor with a formatting toolbar, table operations, and HTML source view.
/// </summary>
public class HtmlEditorPage : UserControl
{
    private string _lastHtml = string.Empty;
    private bool _updating = false;
    private HtmlBox? _htmlBox;
    private MemoBox? _sourceBox;
    private ToolStripButton? _boldButton;

    /// <summary>
    /// Occurs when the status text should be updated.
    /// </summary>
    public event EventHandler<StatusTextChangedEventArgs>? StatusTextChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="HtmlEditorPage"/> class.
    /// </summary>
    public HtmlEditorPage()
    {
        var toolStrip = new ToolStrip { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden };

        _boldButton = new ToolStripButton(SR.GetString("ToolBold"), Icons.TextEditStyle24!) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText };
        var italicButton = new ToolStripButton(SR.GetString("ToolItalic")) { DisplayStyle = ToolStripItemDisplayStyle.Text };
        var underlineButton = new ToolStripButton(SR.GetString("ToolUnderline")) { DisplayStyle = ToolStripItemDisplayStyle.Text };
        var bulletListButton = new ToolStripButton("", Icons.TextBulletListSquare24!) { DisplayStyle = ToolStripItemDisplayStyle.Image };
        var numberListButton = new ToolStripButton("", Icons.NumberSymbolSquare24!) { DisplayStyle = ToolStripItemDisplayStyle.Image };
        var linkButton = new ToolStripButton("", Icons.Link24!) { DisplayStyle = ToolStripItemDisplayStyle.Image };
        var imageButton = new ToolStripButton("", Icons.Image24!) { DisplayStyle = ToolStripItemDisplayStyle.Image };

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

        _htmlBox = new HtmlBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = false,
            LinkBehavior = LinkBehavior.OpenInBrowser,
            Html = @"<h1>HTML Editor</h1>
<p>Welcome to the <b>OldSchoolForms</b> HTML editor!</p>
<p>This is a <a href=""https://example.com"">link</a> example.</p>
<ul>
<li>First item</li>
<li>Second item</li>
<li>Third item</li>
</ul>
<ol>
<li>Number one</li>
<li>Number two</li>
<li>Number three</li>
</ol>
<h2>Table Demo</h2>
<p>Place the cursor here and click <b>Insert Table</b> to add a table, or use <b>Add Row</b>, <b>Remove Row</b>, <b>Add Column</b>, <b>Remove Column</b> to modify it.</p>"
        };
        _lastHtml = _htmlBox.Html;

        System.Console.WriteLine($"[HEP-INIT] Creating sourceBox with _lastHtml len={_lastHtml.Length}");
        _sourceBox = new MemoBox
        {
            Dock = DockStyle.Fill,
            WordWrap = false,
            Font = new Font("Arial", 10, FontStyle.Regular),
            Text = _lastHtml
        };
        System.Console.WriteLine($"[HEP-INIT] sourceBox.Text after init: len={_sourceBox.Text.Length}");

        var splitPanel = new SplitPanel
        {
            Dock = DockStyle.Fill,
            Orientation = SplitOrientation.Horizontal,
            SplitterDistance = 300
        };

        HtmlBox htmlBox = _htmlBox;
        MemoBox sourceBox = _sourceBox;
        ToolStripButton boldButton = _boldButton;
        ToolStripButton italicBtn = italicButton;
        ToolStripButton underlineBtn = underlineButton;

        void UpdateFormatButtons()
        {
            boldButton.Checked = htmlBox.IsBold;
            italicBtn.Checked = htmlBox.IsItalic;
            underlineBtn.Checked = htmlBox.IsUnderline;
        }

        boldButton.Click += (s, e) => { htmlBox.ApplyFormat("bold"); UpdateFormatButtons(); };
        italicButton.Click += (s, e) => { htmlBox.ApplyFormat("italic"); UpdateFormatButtons(); };
        underlineButton.Click += (s, e) => { htmlBox.ApplyFormat("underline"); UpdateFormatButtons(); };
        bulletListButton.Click += (s, e) => { htmlBox.ApplyFormat("insertUnorderedList"); };
        numberListButton.Click += (s, e) => { htmlBox.ApplyFormat("insertOrderedList"); };
        linkButton.Click += (s, e) => { htmlBox.ApplyFormat("createLink"); };
        imageButton.Click += (s, e) => { htmlBox.ApplyFormat("insertImage"); };

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
            System.Console.WriteLine($"[HEP-CC] >>> ENTER: _updating={_updating}, _lastHtmlLen={_lastHtml.Length}, sourceBoxTextLen={sourceBox.Text.Length}");
            UpdateFormatButtons();
            OnStatusTextChanged(htmlBox.CursorDebug);
            string currentHtml = htmlBox.Html;
            System.Console.WriteLine($"[HEP-CC] Html get: len={currentHtml.Length}, lastLen={_lastHtml.Length}, diff={currentHtml != _lastHtml}");
            if (currentHtml != _lastHtml && !_updating)
            {
                _updating = true;
                _lastHtml = currentHtml;
                System.Console.WriteLine($"[HEP-CC] Setting sourceBox.Text len={currentHtml.Length}, content={currentHtml.Substring(0, Math.Min(50, currentHtml.Length))}...");
                sourceBox.Text = currentHtml;
                System.Console.WriteLine($"[HEP-CC] sourceBox.Text after set: len={sourceBox.Text.Length}");
                _updating = false;
            }
            else
            {
                System.Console.WriteLine($"[HEP-CC] SKIP: cond={currentHtml != _lastHtml}, upd={_updating}");
            }
            System.Console.WriteLine($"[HEP-CC] <<< EXIT: _updating={_updating}, _lastHtmlLen={_lastHtml.Length}");
        };

        sourceBox.TextChanged += (s, e) =>
        {
            System.Console.WriteLine($"[HEP-TX] >>> ENTER: _updating={_updating}, sourceBoxTextLen={sourceBox.Text.Length}, _lastHtmlLen={_lastHtml.Length}");
            if (_updating)
            {
                System.Console.WriteLine($"[HEP-TX] <<< SKIP (updating)");
                return;
            }
            string currentText = sourceBox.Text;
            System.Console.WriteLine($"[HEP-TX] currentText len={currentText.Length}, diff={currentText != _lastHtml}");
            if (currentText != _lastHtml)
            {
                _updating = true;
                _lastHtml = currentText;
                System.Console.WriteLine($"[HEP-TX] Setting htmlBox.Html len={currentText.Length}");
                htmlBox.Html = currentText;
                System.Console.WriteLine($"[HEP-TX] htmlBox.Html after set: len={htmlBox.Html.Length}");
                _updating = false;
            }
            else
            {
                System.Console.WriteLine($"[HEP-TX] <<< SKIP (same as _lastHtml)");
            }
            System.Console.WriteLine($"[HEP-TX] <<< EXIT: _updating={_updating}, _lastHtmlLen={_lastHtml.Length}");
        };

        toolStrip.Items.Add(boldButton);
        toolStrip.Items.Add(italicButton);
        toolStrip.Items.Add(underlineButton);
        toolStrip.Items.Add(new ToolStripSeparator());
        toolStrip.Items.Add(bulletListButton);
        toolStrip.Items.Add(numberListButton);
        toolStrip.Items.Add(new ToolStripSeparator());
        toolStrip.Items.Add(linkButton);
        toolStrip.Items.Add(imageButton);
        toolStrip.Items.Add(new ToolStripSeparator());
        toolStrip.Items.Add(insertTableButton);
        toolStrip.Items.Add(new ToolStripSeparator());
        toolStrip.Items.Add(addRowButton);
        toolStrip.Items.Add(removeRowButton);
        toolStrip.Items.Add(new ToolStripSeparator());
        toolStrip.Items.Add(addColButton);
        toolStrip.Items.Add(removeColButton);

        splitPanel.Panel1.Controls.Add(sourceBox);
        splitPanel.Panel2.Controls.Add(htmlBox);

        Controls.Add(splitPanel);
        Controls.Add(toolStrip);
    }

    private void OnStatusTextChanged(string text)
    {
        StatusTextChanged?.Invoke(this, new StatusTextChangedEventArgs(text));
    }
}
