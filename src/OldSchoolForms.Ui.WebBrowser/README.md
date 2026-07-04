# OldSchoolForms.Ui.WebBrowser

A web browser view control for OldSchoolForms.Ui applications. Enables embedding web content (web pages, HTML) into a OldSchoolForms.Ui application.

## Supported Platforms

| Platform | Browser Engine | Rendering |
|----------|---------------|-----------|
| Windows 10/11 | WebView2 (Chromium) | HWND child embedding |
| Linux | CefGlue (Chromium Embedded Framework) | Off-Screen Rendering (OSR) |

## Installation

### Add Project Reference

Add a reference to the `OldSchoolForms.Ui.WebBrowser` project in your application:

```xml
<ItemGroup>
  <ProjectReference Include="..\OldSchoolForms.Ui.WebBrowser\OldSchoolForms.Ui.WebBrowser.csproj" />
</ItemGroup>
```

### System Dependencies

#### Windows

The WebView2 Runtime is required. It is pre-installed on most Windows 10/11 systems. If not available, download it from:
https://developer.microsoft.com/en-us/microsoft-edge/webview2/

#### Linux

CefGlue bundles its own Chromium runtime, so no additional system packages are required beyond a basic X11/Wayland environment. The CEF runtime includes all necessary libraries (Vulkan, ICU, etc.) and is included in the NuGet package.

Ensure a graphical environment is running:
- X11: `export DISPLAY=:0`
- Wayland: Works with Wayland-compliant applications

## Usage

### Basic Usage

```csharp
using OldSchoolForms.Ui;
using OldSchoolForms.Ui.Controls;
using OldSchoolForms.Ui.WebBrowser.Controls;

public class MainForm : Form
{
    private WebView _webView;

    public MainForm()
    {
        Text = "WebBrowser Demo";
        Size = new Size(1024, 768);

        _webView = new WebView
        {
            Location = new Point(0, 0),
            Size = new Size(1024, 700)
        };

        Controls.Add(_webView);

        // Load URL
        _webView.Navigate("https://example.com");
    }
}
```

### Display HTML Content

```csharp
_webView.NavigateToString(@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial; padding: 20px; }
        h1 { color: #333; }
    </style>
</head>
<body>
    <h1>Hello OldSchoolForms.Ui!</h1>
    <p>This is embedded HTML content.</p>
</body>
</html>");
```

### Navigation

```csharp
// Go back
if (_webView.CanGoBack)
{
    _webView.GoBack();
}

// Go forward
if (_webView.CanGoForward)
{
    _webView.GoForward();
}

// Refresh
_webView.Refresh();

// Stop
_webView.Stop();
```

### Zoom (Linux/CefGlue only)

```csharp
// Increase zoom
_webView.ZoomIn();

// Decrease zoom
_webView.ZoomOut();

// Reset zoom to 100%
_webView.ResetZoom();
```

### Execute JavaScript

```csharp
// Execute JavaScript in the page context
var result = await _webView.EvaluateScriptAsync("document.title");
Console.WriteLine($"Page title: {result}");

// DOM manipulation
await _webView.EvaluateScriptAsync("document.body.style.backgroundColor = 'lightblue'");
```

### Events

```csharp
_webView.Navigating += (sender, e) =>
{
    Console.WriteLine($"Navigating to: {e.Url}");
    // Cancel navigation: e.Cancel = true;
};

_webView.Navigated += (sender, e) =>
{
    Console.WriteLine($"Navigation completed: {e.Result}");
    if (e.Result == WebNavigationResult.Success)
    {
        Console.WriteLine("Page loaded successfully!");
    }
};
```

### Complete Example

```csharp
using System;
using OldSchoolForms.Ui;
using OldSchoolForms.Ui.Controls;
using OldSchoolForms.Ui.WebBrowser.Controls;
using OldSchoolForms.Ui.WebBrowser.Events;

public class Program
{
    [STAThread]
    public static void Main()
    {
        Application.Run(new MainForm());
    }
}

public class MainForm : Form
{
    private WebView _webView;
    private Button _backButton;
    private Button _forwardButton;
    private Button _refreshButton;

    public MainForm()
    {
        Text = "OldSchoolForms.Ui WebBrowser Demo";
        Size = new Size(1024, 768);

        var toolPanel = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(1024, 40)
        };

        _backButton = new Button { Text = "Back", Location = new Point(10, 5), Size = new Size(80, 30) };
        _forwardButton = new Button { Text = "Forward", Location = new Point(100, 5), Size = new Size(80, 30) };
        _refreshButton = new Button { Text = "Refresh", Location = new Point(190, 5), Size = new Size(100, 30) };

        _backButton.Click += (s, e) => _webView.GoBack();
        _forwardButton.Click += (s, e) => _webView.GoForward();
        _refreshButton.Click += (s, e) => _webView.Refresh();

        toolPanel.Controls.Add(_backButton);
        toolPanel.Controls.Add(_forwardButton);
        toolPanel.Controls.Add(_refreshButton);

        _webView = new WebView
        {
            Location = new Point(0, 40),
            Size = new Size(1024, 700)
        };

        _webView.Navigating += OnNavigating;
        _webView.Navigated += OnNavigated;

        Controls.Add(toolPanel);
        Controls.Add(_webView);

        _webView.Navigate("https://www.google.com");
    }

    private void OnNavigating(object? sender, WebNavigatingEventArgs e)
    {
        Console.WriteLine($"Navigating to: {e.Url}");
    }

    private void OnNavigated(object? sender, WebNavigatedEventArgs e)
    {
        Console.WriteLine($"Navigated: {e.Result} - {e.Url}");
        _backButton.Enabled = _webView.CanGoBack;
        _forwardButton.Enabled = _webView.CanGoForward;
    }
}
```

## Architecture

The WebView control uses a platform-abstracted approach with different browser engines per platform:

### Windows: WebView2 (HWND Embedding)

On Windows, the control uses Microsoft WebView2 which creates a real child HWND inside the parent form's client area. The OS compositor handles rendering the child HWND on top of the parent surface. Bounds are converted from control-relative to form-absolute coordinates.

### Linux: CefGlue (Off-Screen Rendering)

On Linux, the control uses CefGlue (Chromium Embedded Framework) in Off-Screen Rendering (OSR) mode. CEF renders to a BGRA pixel buffer, which is then drawn via the OldSchoolForms.Ui graphics system (`g.DrawImage()`). All mouse, keyboard, and text input is forwarded manually through CefGlue's input API. Popup overlays (e.g., `<select>` dropdowns) are rendered as a second pixel buffer overlay.

## API Reference

### WebView Properties

| Property | Type | Description |
|----------|------|-------------|
| `Url` | `string?` | The URL to display |
| `CanGoBack` | `bool` | Whether there is a previous page in the history |
| `CanGoForward` | `bool` | Whether there is a next page in the history |
| `IsScriptEnabled` | `bool` | JavaScript enabled (default: true) |

### WebView Methods

| Method | Description |
|--------|-------------|
| `Navigate(string url)` | Navigates to the specified URL |
| `NavigateToString(string html)` | Displays HTML content |
| `GoBack()` | Navigates to the previous page |
| `GoForward()` | Navigates to the next page |
| `Refresh()` | Reloads the current page |
| `Stop()` | Stops the current navigation |
| `EvaluateScriptAsync(string script)` | Executes JavaScript, returns result as string |
| `ZoomIn()` | Increases zoom level (Linux/CefGlue only) |
| `ZoomOut()` | Decreases zoom level (Linux/CefGlue only) |
| `ResetZoom()` | Resets zoom to 100% (Linux/CefGlue only) |

### WebView Events

| Event | Description |
|-------|-------------|
| `Navigating` | Raised before navigation. Contains `Url` and `Cancel` properties. |
| `Navigated` | Raised after navigation completes. Contains `Url` and `Result` (`WebNavigationResult` enum). |

## Known Limitations

1. **Windows:** WebView2 Runtime must be installed. It is usually pre-installed on Windows 10/11.

2. **Linux:** CefGlue bundles its own Chromium runtime, which increases the application size. The first launch may take longer as CEF initializes.

3. **Linux:** OSR rendering may have slightly different performance characteristics compared to native window embedding. GPU acceleration depends on Vulkan driver availability.

4. **OldSchoolForms.Ui Integration:** The WebView control inherits from `Control` and integrates with the OldSchoolForms.Ui handle architecture, event forwarding, and rendering pipeline.

## Troubleshooting

### Windows: "WebView2 not found"
- Install WebView2 Runtime: https://developer.microsoft.com/en-us/microsoft-edge/webview2/

### Linux: No browser content displayed
- Ensure a graphical environment (X11/Wayland) is running
- Check that Vulkan drivers are available for GPU acceleration
- Set display variable: `export DISPLAY=:0`

### Linux: CEF initialization fails
- Ensure required X11 libraries are installed (typically pre-installed on desktop Linux)
- Check application logs for CEF-specific error messages

## License

This project is part of OldSchoolForms.Ui and is subject to the same license terms.
