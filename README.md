# CoreForms.Ui

A cross-platform UI framework for .NET 10+ that works on Linux and Windows with minimal dependencies. Inspired by Windows Forms.

## Features

- **Cross-Platform**: Runs on Linux and Windows
- **Minimal Dependencies**: Only SDL2 required
- **WinForms-like API**: Familiar programming model
- **Rich Controls**: Label, Button, TextBox, CheckBox, RadioButton, ListBox, ComboBox, ProgressBar, Panel, MenuStrip, ToolStrip, TabControl, StatusStrip, DataGridView, and more

## Requirements

### Runtime Dependencies

| Platform | Required Libraries |
|----------|-------------------|
| **Linux** | SDL2, SDL2_ttf |
| **Windows** | SDL2.dll, SDL2_ttf.dll |

## Installation

### Linux (Ubuntu/Debian)

```bash
# Install SDL2 and SDL2_ttf
sudo apt install libsdl2-dev libsdl2-ttf-dev

# Verify installation - on some systems the library is named libSDL2-2.0.so.0
ldconfig -p | grep libSDL2
```

### Linux (Fedora)

```bash
sudo dnf install SDL2-devel SDL2_ttf-devel
```

### Linux (Arch)

```bash
sudo pacman -S sdl2 sdl2_ttf
```

### Windows

1. Download SDL2 development libraries from https://github.com/libsdl-org/SDL/releases
2. Download SDL2_ttf from https://github.com/libsdl-org/SDL_ttf/releases
3. Place `SDL2.dll` and `SDL2_ttf.dll` in your application's directory

#### NuGet Package (Alternative)

Use the `SDL2-CS` NuGet package which includes native binaries:

```xml
<PackageReference Include="SDL2-CS" Version="2.0.0" />
```

## Building

```bash
# Restore and build all projects
dotnet build

# Run tests
dotnet test

# Run demo (requires X11/display server)
dotnet run --project samples/CoreForms.Ui.Demo
```

## Project Structure

```
CoreForms.Ui/
├── src/
│   ├── CoreForms.Ui/           # Main framework
│   └── CoreForms.Ui.Sdl/       # SDL2 backend
├── samples/
│   └── CoreForms.Ui.Demo/      # Demo application
├── tests/
│   └── CoreForms.Ui.Tests/    # Unit tests
└── README.md
```

## Quick Start

```csharp
using CoreForms.Ui.Core;
using CoreForms.Ui.Controls.Basic;

var form = new Form
{
    Text = "My App",
    Width = 800,
    Height = 600
};

var button = new Button
{
    Text = "Click Me",
    Location = new Point(50, 50),
    Size = new Size(100, 40)
};

button.Click += (s, e) => Console.WriteLine("Clicked!");

form.Controls.Add(button);

Application.Run(form);
```

## Architecture

- **Custom Types**: Uses custom `Color`, `Font`, `Point`, `Size`, `Rectangle` to avoid System.Drawing conflicts
- **Graphics**: Command-list pattern for platform-independent rendering
- **Platform Layer**: Direct P/Invoke to SDL2

## Troubleshooting

### "Unable to load shared library 'libSDL2.so.0'"

Ensure SDL2 is installed:
```bash
sudo apt install libsdl2-dev
```

### "Unable to load shared library 'libSDL2_ttf.so.0'"

Install SDL2_ttf:
```bash
sudo apt install libsdl2-ttf-dev
```

### Demo doesn't show window

The demo requires an X11 display server. On headless systems, use Xvfb:
```bash
xvfb-run dotnet run --project samples/CoreForms.Ui.Demo
```

## License

MIT License