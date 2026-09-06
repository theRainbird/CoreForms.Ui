using OldSchoolForms.Ui.Controls.Advanced;
using OldSchoolForms.Ui.Controls.Basic;
using OldSchoolForms.Ui.Controls.Containers;
using OldSchoolForms.Ui.Controls;
using OldSchoolForms.Ui.Core;

namespace OldSchoolForms.Ui.Demo.UserControls;

/// <summary>
/// Demonstrates the HtmlBox rich text editor with an internal formatting toolbar and HTML source view.
/// </summary>
public class HtmlEditorPage : UserControl
{
    private string _lastHtml = string.Empty;
    private bool _updating = false;
    private HtmlBox? _htmlBox;
    private MemoBox? _sourceBox;

    /// <summary>
    /// Occurs when the status text should be updated.
    /// </summary>
    public event EventHandler<StatusTextChangedEventArgs>? StatusTextChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="HtmlEditorPage"/> class.
    /// </summary>
    public HtmlEditorPage()
    {
        _htmlBox = new HtmlBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = false,
            LinkBehavior = LinkBehavior.OpenInBrowser,
            ShowToolbar = true,
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
<p>Use the internal toolbar to format text, change alignment, and apply background color.</p>"
        };
        _lastHtml = _htmlBox.Html;

        _sourceBox = new MemoBox
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

        HtmlBox htmlBox = _htmlBox;
        MemoBox sourceBox = _sourceBox;

        htmlBox.ContentChanged += (s, e) =>
        {
            OnStatusTextChanged(htmlBox.CursorDebug);
            string currentHtml = htmlBox.Html;
            if (currentHtml != _lastHtml && !_updating)
            {
                _updating = true;
                _lastHtml = currentHtml;
                sourceBox.Text = currentHtml;
                _updating = false;
            }
        };

        sourceBox.TextChanged += (s, e) =>
        {
            if (_updating)
            {
                return;
            }
            string currentText = sourceBox.Text;
            if (currentText != _lastHtml)
            {
                _updating = true;
                _lastHtml = currentText;
                htmlBox.Html = currentText;
                _updating = false;
            }
        };

        splitPanel.Panel1.Controls.Add(sourceBox);
        splitPanel.Panel2.Controls.Add(htmlBox);

        Controls.Add(splitPanel);
    }

    private void OnStatusTextChanged(string text)
    {
        StatusTextChanged?.Invoke(this, new StatusTextChangedEventArgs(text));
    }
}
