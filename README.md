# CoreForms.Ui

A cross-platform .NET UI framework for Linux and Windows. Provides a Windows Forms-inspired API and uses **Silk.NET** (OpenGL 3.3) for windowing and input, **SkiaSharp** for hardware-accelerated 2D graphics, and **Svg.Skia** for resolution-independent SVG icon rendering.

## Features

- **Cross-Platform** – Runs on Linux and Windows with .NET 10+
- **WinForms-like API** – Familiar programming model with `Form`, `Control`, `Application.Run()`, event handlers
- **GPU-Accelerated Rendering** – SkiaSharp with OpenGL context; automatic software fallback
- **25+ Controls** – From Label, Button, TextBox to DataGridView, TreeView, TabControl, HtmlBox
- **SVG Icons** – 890+ Fluent UI Color SVGs as embedded resources, resolution-independent via Svg.Skia
- **Data Binding** – Full BindingSource / CurrencyManager system with INotifyPropertyChanged support
- **HTML Rendering** – Built-in HTML parser (HtmlAgilityPack), CSS parser, and WYSIWYG editor (HtmlBox)
- **Theming** – Light and dark themes, switchable at runtime
- **Layout Managers** – FlowLayoutPanel, TableLayoutPanel, Dock/Anchor, Padding
- **Menus & Toolbars** – MenuStrip with mnemonic support (`&File` → Alt+F), ToolStrip with buttons and text fields
- **MessageBox** – Modal dialogs with standard button combinations, icons, and localization (EN/DE)
- **Multi-Window** – Any number of windows, each with its own renderer context
- **DPI Awareness** – Automatic scaling per platform (Windows: system DPI, Linux: 96)
- **WebView** – Optional Chromium-based WebView control (separate package)
- **DataGridView** – Automatic column mapping, sorting, BindingList support

## Controls

| Category | Controls |
|----------|----------|
| **Basic** | Label, Button, TextBox, CheckBox, RadioButton, ListBox, ComboBox, ProgressBar, PictureBox, Spinner, GroupBox |
| **Container** | Panel, SplitPanel, GroupBox, UserControl, TabControl, TabPage |
| **Menus/Toolbars** | MenuStrip, ToolStrip, ToolStripButton, ToolStripLabel, ToolStripTextBox, ToolStripSeparator, ToolStripMenuItem |
| **Advanced** | DataGridView, TreeView (with ImageList), HtmlBox (WYSIWYG HTML editor) |
| **Web** | WebView (Chromium via CefGlue, separate package) |
| **Dialogs** | MessageBox (modal, button combinations, icons, EN/DE localization) |

## Requirements

- .NET 10.0 SDK or later
- Linux: OpenGL 3.3 drivers (typically pre-installed on desktop systems)
- Windows: No additional system dependencies
- Optional for WebView: `webkit2gtk-4.1` (Linux) / Chromium (Windows)

## Build & Run

```bash
# Build all projects
dotnet build

# Run tests
dotnet test

# Run demo (requires X11/display server)
dotnet run --project samples/CoreForms.Ui.Demo
```

On headless systems, use Xvfb:

```bash
xvfb-run dotnet run --project samples/CoreForms.Ui.Demo
```

## Hello World

```csharp
using CoreForms.Ui.Core;
using CoreForms.Ui.Controls.Basic;
using CoreForms.Ui.Theming;

var form = new Form
{
    Title = "Hello World",
    Width = 400,
    Height = 300
};

var label = new Label
{
    Text = "Welcome to CoreForms.Ui!",
    Location = new Point(50, 50),
    Size = new Size(300, 30)
};

var button = new Button
{
    Text = "Click Me",
    Location = new Point(50, 100),
    Size = new Size(100, 40)
};

button.Click += (s, e) =>
{
    MessageBox.Show("Hello World!", "Greeting",
        MessageBoxButtons.OK, MessageBoxIcon.Information);
};

form.Controls.Add(label);
form.Controls.Add(button);

Application.Run(form);
```

## Project Structure

```
CoreForms.Ui/
├── src/
│   ├── CoreForms.Ui/                  # Main framework
│   │   ├── Core/                      # Base classes (Control, Form, Application)
│   │   ├── Controls/                  # Controls (Basic, Advanced, Container)
│   │   ├── Rendering/                 # Graphics engine (SkiaSharp, HTML)
│   │   ├── Platform/                  # Platform abstraction (Silk.NET)
│   │   ├── Theming/                   # Theme system (Light, Dark)
│   │   ├── Data/                      # Data binding (BindingSource, CurrencyManager)
│   │   ├── Layout/                    # Layout managers (FlowLayout, TableLayout)
│   │   ├── Html/                      # HTML DOM, CSS parser, RichTextEngine
│   │   └── Resources/                 # SVG icons, localization (.resx)
│   ├── CoreForms.Ui.WebBrowser/       # WebView control (CefGlue)
│   └── CoreForms.Ui.WebBrowser.Helper/# Native helper process for WebKit
├── samples/
│   └── CoreForms.Ui.Demo/             # Demo application
├── tests/
│   └── CoreForms.Ui.Tests/            # Unit tests (xUnit)
└── README.md
```

## Architecture

- **Custom Types** – Uses its own `Color`, `Font`, `Point`, `Size`, `Rectangle` (in `Core/SystemTypes.cs`) to avoid conflicts with System.Drawing
- **Graphics** – Command-list pattern (`Rendering/Graphics.cs`): collects drawing commands, executed by the renderer for platform independence
- **SkiaSharp Rendering** – Each window has its own `SkiaRenderer` + `SkiaFontRenderer` via `WindowContext`. GPU acceleration via OpenGL 3.3 with transparent software fallback
- **Silk.NET** – Windowing and input abstraction (keyboard, mouse); cross-platform without direct P/Invokes in framework code
- **SVG Icons** – Embedded SVG resources → Svg.Skia parse/rasterize → available as textures in the renderer; cached at multiple resolutions
- **Theme System** – Abstract `Theme` base class with `LightTheme` and `DarkTheme`; `ThemeManager` notifies all controls via `WeakReference` on theme switch

## Key Namespaces

| Namespace | Description |
|-----------|-------------|
| `CoreForms.Ui.Core` | Base classes: `Control`, `Form`, `Application`, `ContainerControl`, system types (`Color`, `Point`, `Size`, etc.) |
| `CoreForms.Ui.Controls.Basic` | Standard controls: `Button`, `TextBox`, `Label`, `CheckBox`, `RadioButton`, `ListBox`, `ComboBox`, `ProgressBar`, `Spinner`, `PictureBox` |
| `CoreForms.Ui.Controls.Advanced` | Advanced controls: `DataGridView`, `TabControl`, `HtmlBox` |
| `CoreForms.Ui.Controls.Containers` | Containers: `Panel`, `SplitPanel`, `MenuStrip`, `ToolStrip` |
| `CoreForms.Ui.Rendering` | Graphics: `Graphics` (command list), `SkiaRenderer`, `SkiaFontRenderer`, `HtmlRenderer` |
| `CoreForms.Ui.Theming` | Themes: `Theme`, `LightTheme`, `DarkTheme`, `ThemeManager` |
| `CoreForms.Ui.Data` | Data binding: `BindingSource`, `BindingContext`, `CurrencyManager` |
| `CoreForms.Ui.Layout` | Layout: `FlowLayoutPanel`, `TableLayoutPanel` |

## API Overview

### Creating a form

```csharp
var form = new Form
{
    Title = "My Window",
    Width = 800,
    Height = 600,
    Zoom = 1.25f  // 125% scaling
};
```

### Adding controls

```csharp
var button = new Button
{
    Text = "Save",
    Location = new Point(10, 10),
    Size = new Size(100, 35),
    Anchor = AnchorStyles.Top | AnchorStyles.Left
};

button.Click += (sender, args) => { /* ... */ };

form.Controls.Add(button);
```

### MessageBox

```csharp
var result = MessageBox.Show("Are you sure?", "Confirm",
    MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

if (result == DialogResult.Yes) { /* ... */ }
```

### Switching themes

```csharp
ThemeManager.SetTheme(new DarkTheme());
// or
ThemeManager.SetTheme(new LightTheme());
```

### Data binding

```csharp
var source = new BindingSource { DataSource = myList };
var textBox = new TextBox();
textBox.DataBindings.Add("Text", source, "Name");
```

### Menus

```csharp
var menuStrip = new MenuStrip();
var fileMenu = new ToolStripMenuItem("&File");
fileMenu.DropDownItems.Add(new ToolStripMenuItem("&Open"));
fileMenu.DropDownItems.Add(new ToolStripMenuItem("&Save"));
menuStrip.Items.Add(fileMenu);
form.Controls.Add(menuStrip);
```

## Troubleshooting

### "Unable to create OpenGL context" or rendering issues

Ensure your system supports OpenGL 3.3. On Linux, install Mesa drivers:

```bash
sudo apt install mesa-utils
```

On virtual machines or headless systems, use software rendering or `xvfb-run`.

### Demo doesn't show a window

The demo requires an X11 display server. On headless systems, use Xvfb:

```bash
xvfb-run dotnet run --project samples/CoreForms.Ui.Demo
```

## Related Projects

- [CoreForms.Ui.WebBrowser](src/CoreForms.Ui.WebBrowser/) – Chromium-based WebView (CefGlue)

## License

MIT License
