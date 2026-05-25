using CoreForms.Ui.Controls.Basic;
using CoreForms.Ui.Controls.Containers;
using CoreForms.Ui.Controls;
using CoreForms.Ui.Core;
using CoreForms.Ui.Resources;
using CoreForms.Ui.WebBrowser.Controls;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Demo.UserControls;

/// <summary>
/// Demonstrates the WebView browser control with navigation toolbar.
/// </summary>
public class WebBrowserPage : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WebBrowserPage"/> class.
    /// </summary>
    public WebBrowserPage()
    {
        var toolStrip = new ToolStrip { Dock = DockStyle.Top };

        var backButton = new ToolStripButton(SR.GetString("BtnWebBack"));
        var forwardButton = new ToolStripButton(SR.GetString("BtnWebForward"));
        var refreshButton = new ToolStripButton("", Icons.ArrowSync24!) { DisplayStyle = ToolStripItemDisplayStyle.Image };
        var stopButton = new ToolStripButton("", Icons.DismissCircle24!) { DisplayStyle = ToolStripItemDisplayStyle.Image };

        var urlTextBox = new ToolStripTextBox { TextBoxWidth = 400 };
        var goButton = new ToolStripButton("", Icons.SearchSparkle24!) { DisplayStyle = ToolStripItemDisplayStyle.Image };

        var zoomInButton = new ToolStripButton("", Icons.AddCircle24!) { DisplayStyle = ToolStripItemDisplayStyle.Image };
        var zoomOutButton = new ToolStripButton("", Icons.DismissCircle24!) { DisplayStyle = ToolStripItemDisplayStyle.Image };
        var zoomResetButton = new ToolStripButton("100%");

        var webView = new WebView
        {
            Dock = DockStyle.Fill
        };

        backButton.Click += (s, e) => webView.GoBack();
        forwardButton.Click += (s, e) => webView.GoForward();
        refreshButton.Click += (s, e) => webView.Refresh();
        stopButton.Click += (s, e) => webView.Stop();
        zoomInButton.Click += (s, e) => webView.ZoomIn();
        zoomOutButton.Click += (s, e) => webView.ZoomOut();
        zoomResetButton.Click += (s, e) => webView.ResetZoom();

        void NavigateToUrl()
        {
            var url = urlTextBox.Text;
            if (!string.IsNullOrWhiteSpace(url))
                webView.Navigate(url);
        }

        goButton.Click += (s, e) => NavigateToUrl();

        toolStrip.KeyDown += (s, e) =>
        {
            if (e is KeyEventArgs ke && ke.KeyCode == Keys.Enter)
            {
                NavigateToUrl();
                ke.Handled = true;
            }
        };

        webView.KeyDown += (s, e) =>
        {
            if (e is KeyEventArgs ke && ke.Modifiers.HasFlag(ModifierKeys.Control))
            {
                switch (ke.KeyCode)
                {
                    case Keys.D0:
                        webView.ResetZoom();
                        ke.Handled = true;
                        break;
                }
            }
        };

        webView.Navigated += (s, e) =>
        {
            urlTextBox.Text = e.Url ?? string.Empty;
        };

        toolStrip.Items.AddRange(new ToolStripItem[]
        {
            backButton, forwardButton, refreshButton, stopButton,
            new ToolStripSeparator(),
            urlTextBox, goButton,
            new ToolStripSeparator(),
            zoomInButton, zoomOutButton, zoomResetButton
        });

        Controls.Add(toolStrip);
        Controls.Add(webView);
    }
}
