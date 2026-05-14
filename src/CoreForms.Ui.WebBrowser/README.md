# CoreForms.Ui.WebBrowser

Ein Webbrowser-View-Control für CoreForms.Ui-Anwendungen. Ermöglicht die Einbettung von Webinhalten (Webseiten, HTML) in eine CoreForms.Ui-Anwendung.

## Unterstützte Plattformen

| Plattform | Browser-Engine | Systemanforderungen |
|-----------|---------------|---------------------|
| Windows 10/11 | WebView2 | WebView2 Runtime (vorinstalliert oder herunterladbar) |
| Linux | WebKitGTK | libwebkit2gtk-4.1-0 |

## Installation

### Projekt-Referenz hinzufügen

Fügen Sie in Ihrer Anwendung eine Referenz zum `CoreForms.Ui.WebBrowser`-Projekt hinzu:

```xml
<ItemGroup>
  <ProjectReference Include="..\CoreForms.Ui.WebBrowser\CoreForms.Ui.WebBrowser.csproj" />
</ItemGroup>
```

### Systemabhängigkeiten

#### Linux

Installieren Sie die erforderlichen GTK- und WebKit-Bibliotheken:

**Ubuntu/Debian:**
```bash
sudo apt install libwebkit2gtk-4.1-0 libgtk-4-1 libglib2.0-0
```

**Fedora:**
```bash
sudo dnf install webkit2gtk4.1 libgtk-4.1
```

**Arch Linux:**
```bash
sudo pacman -S webkit2gtk-4.1 gtk4
```

## Verwendung

### Grundlegende Verwendung

```csharp
using CoreForms.Ui;
using CoreForms.Ui.Controls;
using CoreForms.Ui.WebBrowser.Controls;

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

        // URL laden
        _webView.Navigate("https://example.com");
    }
}
```

### HTML-Inhalt anzeigen

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
    <h1>Hallo CoreForms.Ui!</h1>
    <p>Dies ist eingebetteter HTML-Inhalt.</p>
</body>
</html>");
```

### Navigation

```csharp
// Zurück
if (_webView.CanGoBack)
{
    _webView.GoBack();
}

// Vorwärts
if (_webView.CanGoForward)
{
    _webView.GoForward();
}

// Aktualisieren
_webView.Refresh();
```

### JavaScript ausführen

```csharp
// JavaScript im Kontext der Seite ausführen
var result = await _webView.EvaluateScriptAsync("document.title");
Console.WriteLine($"Seitentitel: {result}");

// DOM-Manipulation
await _webView.EvaluateScriptAsync("document.body.style.backgroundColor = 'lightblue'");
```

### Events

```csharp
_webView.Navigating += (sender, e) =>
{
    Console.WriteLine($"Navigiere zu: {e.Url}");
    // Navigation abbrechen: e.Cancel = true;
};

_webView.Navigated += (sender, e) =>
{
    Console.WriteLine($"Navigation abgeschlossen: {e.Result}");
    if (e.Result == WebNavigationResult.Success)
    {
        Console.WriteLine("Seite erfolgreich geladen!");
    }
};
```

### Vollständiges Beispiel

```csharp
using System;
using CoreForms.Ui;
using CoreForms.Ui.Controls;
using CoreForms.Ui.WebBrowser.Controls;
using CoreForms.Ui.WebBrowser.Events;

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
        Text = "CoreForms.Ui WebBrowser Demo";
        Size = new Size(1024, 768);

        var toolPanel = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(1024, 40)
        };

        _backButton = new Button { Text = "Zurück", Location = new Point(10, 5), Size = new Size(80, 30) };
        _forwardButton = new Button { Text = "Vorwärts", Location = new Point(100, 5), Size = new Size(80, 30) };
        _refreshButton = new Button { Text = "Aktualisieren", Location = new Point(190, 5), Size = new Size(100, 30) };

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

## API-Referenz

### WebView-Eigenschaften

| Eigenschaft | Typ | Beschreibung |
|-------------|-----|---------------|
| `Url` | `string?` | Die anzuzeigende URL |
| `CanGoBack` | `bool` | Ob eine vorherige Seite im Verlauf ist |
| `CanGoForward` | `bool` | Ob eine nächste Seite im Verlauf ist |
| `IsScriptEnabled` | `bool` | JavaScript aktiviert (Standard: true) |
| `Visible` | `bool` | Sichtbarkeit des Controls |
| `Enabled` | `bool` | Aktiviert/Deaktiviert |

### WebView-Methoden

| Methode | Beschreibung |
|---------|-------------|
| `Navigate(string url)` | Navigiert zur angegebenen URL |
| `NavigateToString(string html)` | Zeigt HTML-Inhalt an |
| `GoBack()` | Navigiert zur vorherigen Seite |
| `GoForward()` | Navigiert zur nächsten Seite |
| `Refresh()` | Lädt die aktuelle Seite neu |
| `Stop()` | Stoppt die aktuelle Navigation |
| `EvaluateScriptAsync(string script)` | Führt JavaScript aus |

### WebView-Events

| Event | Beschreibung |
|-------|-------------|
| `Navigating` | Wird vor einer Navigation ausgelöst |
| `Navigated` | Wird nach Abschluss einer Navigation ausgelöst |

## Bekannte Einschränkungen

1. **Windows:** WebView2 muss installiert sein. Die Runtime ist auf Windows 10/11 meist vorinstalliert.

2. **Linux:** Das WebView-Fenster wird als separates GTK-Window erstellt und über Socket-Embedding eingebettet. Dies erfordert eine funktionierende GTK-Umgebung.

3. **CoreForms.Ui-Integration:** Das WebView-Control nutzt die Handle-Architektur von CoreForms.Ui. Die Fenster-ID wird aus dem übergeordneten Form ermittelt.

## Fehlerbehebung

### Windows: "WebView2 not found"
- WebView2 Runtime installieren: https://developer.microsoft.com/de-de/microsoft-edge/webview2/

### Linux: "libwebkit2gtk not found"
- Systempakete installieren (siehe oben)

### Linux: "gtk_init_check failed"
- Stellen Sie sicher, dass eine grafische Umgebung (X11/Wayland) läuft
- Display-Variable setzen: `export DISPLAY=:0`

## Lizenz

Dieses Projekt ist Teil von CoreForms.Ui und unterliegt den gleichen Lizenzbedingungen.