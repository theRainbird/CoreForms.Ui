# CoreForms.Ui - Cross-Platform UI Framework

## Build & Run
```bash
dotnet build                    # Build all projects
dotnet test                     # Run tests (89 passing)
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
- MessageBox (modal dialog with standard button combinations, icons, localization)
- DataGridView

**Layout:**
- FlowLayoutPanel, TableLayoutPanel

**Rendering:**
- Graphics (command-list pattern)
- SdlRenderer (SDL2-based, alpha blending, image/ellipse/triangle drawing)
- FontRenderer (SDL_ttf-based)
- SDL_LoadBMP_RW for BMP icon loading (no SDL2_image dependency)

**Window Management:**
- Minimize, Maximize, Restore
- Move, Resize
- Focus tracking
- GotFocus/LostFocus events
- Multi-window support (per-window Renderer + FontRenderer via WindowContext)
- Modal dialogs (Form.Enabled for owner disabling)

**Menus:**
- MenuStrip with dropdown menus, keyboard navigation, Alt+mnemonic activation
- ToolStripMenuItem with mnemonic support (`&File` → Alt+F opens File menu)

## Important Context

**Platform layer uses SDL2 via P/Invoke** - Works on Linux/Windows with SDL2 installed:
- Linux: `sudo apt install libsdl2-dev libsdl2-ttf-2.0-0`
- Windows: SDL2.dll in app directory
- No SDL2_image required - icons use BMP format loaded via native SDL2

**Custom type dependencies** - Avoids System.Drawing conflicts:
- Custom `Color`, `Font`, `Point`, `Size`, `Rectangle` in `Core/SystemTypes.cs`
- Custom `Graphics` in `Rendering/Graphics.cs` (use alias: `using Graphics = CoreForms.Ui.Rendering.Graphics`)
- Custom `SdlRenderer` in `Rendering/SdlRenderer.cs`

**Headless limitation** - Demo won't show window without X11/display server.

## Architecture
- Controls inherit from `CoreForms.Ui.Core.Control`
- Graphics uses command-list pattern for platform-independent rendering
- Platform abstraction with SDL2 via direct P/Invoke

## Code Conventions

**XML-Summary Kommentare:**
- Alle Klassen, Properties, Events und Methoden in der Codebasis (außer Unit Tests) müssen XML-Summary Kommentare haben
- XML-Summaries müssen Parameter (`<param name="...">`) und ggf. geworfene Exceptions (`<exception cref="...">`) enthalten
- Properties müssen den Verwendungszweck dokumentieren
- Events müssen beschreiben, wann sie ausgelöst werden
- Methoden müssen die Funktionalität und Parameter beschreiben