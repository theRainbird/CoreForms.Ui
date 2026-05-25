using CoreForms.Ui.Controls.Advanced;
using CoreForms.Ui.Controls.Basic;
using CoreForms.Ui.Controls.Containers;
using CoreForms.Ui.Controls;
using CoreForms.Ui.Resources;
using Graphics = CoreForms.Ui.Rendering.Graphics;
using CoreForms.Ui.Core;

namespace CoreForms.Ui.Demo.UserControls;

/// <summary>
/// Demonstrates the HtmlBox rich text editor with a formatting toolbar.
/// </summary>
public class HtmlEditorPage : UserControl
{
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
<p>Welcome to the <b>CoreForms</b> HTML editor!</p>
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
        htmlBox.ContentChanged += (s, e) => { UpdateFormatButtons(); OnStatusTextChanged(htmlBox.CursorDebug); };

        toolStrip.Items.Add(boldButton);
        toolStrip.Items.Add(italicButton);
        toolStrip.Items.Add(underlineButton);
        toolStrip.Items.Add(new ToolStripSeparator());
        toolStrip.Items.Add(bulletListButton);
        toolStrip.Items.Add(numberListButton);
        toolStrip.Items.Add(new ToolStripSeparator());
        toolStrip.Items.Add(linkButton);
        toolStrip.Items.Add(imageButton);

        Controls.Add(toolStrip);
        Controls.Add(htmlBox);
    }

    private void OnStatusTextChanged(string text)
    {
        StatusTextChanged?.Invoke(this, new StatusTextChangedEventArgs(text));
    }
}
