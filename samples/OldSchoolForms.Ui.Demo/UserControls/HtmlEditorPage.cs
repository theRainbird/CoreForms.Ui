using OldSchoolForms.Ui.Controls.Advanced;
using OldSchoolForms.Ui.Controls.Basic;
using OldSchoolForms.Ui.Controls.Containers;
using OldSchoolForms.Ui.Controls;
using OldSchoolForms.Ui.Resources;
using Graphics = OldSchoolForms.Ui.Rendering.Graphics;
using OldSchoolForms.Ui.Core;

namespace OldSchoolForms.Ui.Demo.UserControls;

/// <summary>
/// Demonstrates the HtmlBox rich text editor with a formatting toolbar and HTML source view.
/// </summary>
public class HtmlEditorPage : UserControl
{
    private string _lastHtml = string.Empty;

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

        var boldButton = new ToolStripButton(SR.GetString("ToolBold"), Icons.TextEditStyle24!) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText };
        var italicButton = new ToolStripButton(SR.GetString("ToolItalic")) { DisplayStyle = ToolStripItemDisplayStyle.Text };
        var underlineButton = new ToolStripButton(SR.GetString("ToolUnderline")) { DisplayStyle = ToolStripItemDisplayStyle.Text };
        var bulletListButton = new ToolStripButton("", Icons.TextBulletListSquare24!) { DisplayStyle = ToolStripItemDisplayStyle.Image };
        var numberListButton = new ToolStripButton("", Icons.NumberSymbolSquare24!) { DisplayStyle = ToolStripItemDisplayStyle.Image };
        var linkButton = new ToolStripButton("", Icons.Link24!) { DisplayStyle = ToolStripItemDisplayStyle.Image };
        var imageButton = new ToolStripButton("", Icons.Image24!) { DisplayStyle = ToolStripItemDisplayStyle.Image };

        var htmlBox = new HtmlBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = false,
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
</ol>"
        };
        _lastHtml = htmlBox.Html;

        var sourceBox = new MemoBox
        {
            Dock = DockStyle.Fill,
            WordWrap = false,
            Font = new Font("Arial", 10, FontStyle.Regular),
            Text = _lastHtml
        };

        var splitPanel = new SplitPanel
        {
            Dock = DockStyle.Fill,
            Orientation = SplitOrientation.Horizontal,
            SplitterDistance = 300
        };

        void UpdateFormatButtons()
        {
            boldButton.Checked = htmlBox.IsBold;
            italicButton.Checked = htmlBox.IsItalic;
            underlineButton.Checked = htmlBox.IsUnderline;
        }

        boldButton.Click += (s, e) => { htmlBox.ApplyFormat("bold"); UpdateFormatButtons(); };
        italicButton.Click += (s, e) => { htmlBox.ApplyFormat("italic"); UpdateFormatButtons(); };
        underlineButton.Click += (s, e) => { htmlBox.ApplyFormat("underline"); UpdateFormatButtons(); };
        bulletListButton.Click += (s, e) => { htmlBox.ApplyFormat("insertUnorderedList"); };
        numberListButton.Click += (s, e) => { htmlBox.ApplyFormat("insertOrderedList"); };
        linkButton.Click += (s, e) => { htmlBox.ApplyFormat("createLink"); };
        imageButton.Click += (s, e) => { htmlBox.ApplyFormat("insertImage"); };

        htmlBox.ContentChanged += (s, e) =>
        {
            UpdateFormatButtons();
            OnStatusTextChanged(htmlBox.CursorDebug);
            string currentHtml = htmlBox.Html;
            if (currentHtml != _lastHtml)
            {
                _lastHtml = currentHtml;
                sourceBox.Text = currentHtml;
            }
        };

        sourceBox.TextChanged += (s, e) =>
        {
            string currentText = sourceBox.Text;
            if (currentText != _lastHtml)
            {
                _lastHtml = currentText;
                htmlBox.Html = currentText;
            }
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
