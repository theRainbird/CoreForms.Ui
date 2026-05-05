# CoreForms.Ui - Cross-Platform UI Framework

## Build & Run
```bash
dotnet build                    # Build all projects
dotnet test                     # Run tests (12 passing)
dotnet run --project samples/CoreForms.Ui.Demo  # Run demo (requires X11/display)
```

## Project Structure
- `src/CoreForms.Ui/` - Main framework (net10.0)
- `src/CoreForms.Ui.Sdl/` - SDL2 backend
- `samples/CoreForms.Ui.Demo/` - Demo app
- `tests/CoreForms.Ui.Tests/` - Unit tests

## Implemented Features

**Controls (Basic):**
- Label, Button, TextBox, CheckBox, RadioButton, ListBox, ComboBox, ProgressBar

**Controls (Containers):**
- Panel, MenuStrip, ToolStrip

**Controls (Advanced):**
- TabControl, TabPage, StatusStrip

**Layout:**
- FlowLayoutPanel, TableLayoutPanel

**Rendering:**
- Graphics (command-list pattern)
- SdlRenderer (SDL2-based)
- FontRenderer (SDL_ttf-based)

**Window Management:**
- Minimize, Maximize, Restore
- Move, Resize
- Focus tracking
- GotFocus/LostFocus events

## Important Context

**Platform layer uses SDL2 via P/Invoke** - Works on Linux/Windows with SDL2 installed:
- Linux: `sudo apt install libsdl2-dev`
- Windows: SDL2.dll in app directory

**Custom type dependencies** - Avoids System.Drawing conflicts:
- Custom `Color`, `Font`, `Point`, `Size`, `Rectangle` in `Core/SystemTypes.cs`
- Custom `Graphics` in `Rendering/Graphics.cs` (use alias: `using Graphics = CoreForms.Ui.Rendering.Graphics`)
- Custom `SdlRenderer` in `Rendering/SdlRenderer.cs`

**Headless limitation** - Demo won't show window without X11/display server.

## Architecture
- Controls inherit from `CoreForms.Ui.Core.Control`
- Graphics uses command-list pattern for platform-independent rendering
- Platform abstraction with SDL2 via direct P/Invoke