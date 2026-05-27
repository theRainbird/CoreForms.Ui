# CoreForms.Ui - Cross-Platform UI Framework

## Build & Run
```bash
dotnet build                    # Build all projects
dotnet test                     # Run tests (89 passing)
dotnet run --project samples/CoreForms.Ui.Demo  # Run demo (requires X11/display)
```

## Project Structure
- `src/CoreForms.Ui/` - Main framework (net10.0)
- `samples/CoreForms.Ui.Demo/` - Demo app
- `tests/CoreForms.Ui.Tests/` - Unit tests

## Implemented Features

**Controls (Basic):**
- Label, Button, TextBox, CheckBox, RadioButton, ListBox, ComboBox, ProgressBar, FontPicker

**Controls (Containers):**
- Panel, MenuStrip, ToolStrip

**Core Utilities:**
- FontManager (SKFontManager-based font enumeration, lazy typeface loading, memory-efficient)

**Controls (Advanced):**
- TabControl, TabPage, StatusStrip
- MessageBox (modal dialog with standard button combinations, icons, localization)
- DataGridView
- CalendarView (Month/Week/Day views, appointment display & editing with modal dialog, mini-calendar navigation, Outlook-like UI)

**Layout:**
- FlowLayoutPanel, TableLayoutPanel

**Rendering:**
- Graphics (command-list pattern)
- SkiaRenderer (SkiaSharp-based, GPU-accelerated via OpenGL 3.3, alpha blending, software fallback)
- SkiaFontRenderer (SkiaSharp-based)
- SVG icon support via Svg.Skia (resolution-independent, alpha transparency)

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

**Platform layer uses Silk.NET** - Windowing, input, and OpenGL context are managed via Silk.NET:
- No manual native library installation required (Silk.NET bundles native dependencies via NuGet)
- SVG rendering uses managed Svg.Skia library

**Custom type dependencies** - Avoids System.Drawing conflicts:
- Custom `Color`, `Font`, `Point`, `Size`, `Rectangle` in `Core/SystemTypes.cs`
- Custom `Graphics` in `Rendering/Graphics.cs` (use alias: `using Graphics = CoreForms.Ui.Rendering.Graphics`)
- Custom `SkiaRenderer` in `Rendering/SkiaRenderer.cs`
- `FontManager` in `Core/FontManager.cs` (uses `SKFontManager` for RAM-efficient font enumeration)

**Headless limitation** - Demo won't show window without X11/display server.

**Cross-Platform Rule:**
- All new code must work on both Linux and Windows
- Native libraries must be available for both platforms (SkiaSharp bundles native libs automatically)
- P/Invoke must resolve native libraries for both platforms
- File paths, directory separators, and line endings must be cross-platform compatible

## Architecture
- Controls inherit from `CoreForms.Ui.Core.Control`
- Graphics uses command-list pattern for platform-independent rendering
- Platform abstraction with Silk.NET (windowing, input, OpenGL context)
- SVG icons: embedded resources → Svg.Skia parse/rasterize → RGBA pixels → SkiaSharp textures

## Code Conventions

**Label text without trailing colons:**
- Label control text must not end with a colon (`:`)
- Use `Text = "Name"` instead of `Text = "Name:"` or `Text = SR.GetString("LabelName")` instead of `Text = SR.GetString("LabelName") + ":"`
- This applies to all controls that serve as labels (Label, ToolStripLabel, etc.)
- Resource strings for label captions must not include trailing colons

**Deleting files that cause build errors:**
- AI agents must NOT delete source code files that cause build errors but are not directly related to the current task without first asking the user for permission
- Instead, the agent should inform the user and ask for confirmation

**XML Summary Comments:**
- All classes, properties, events, and methods in the codebase (except unit tests) must have XML summary comments
- XML summaries must include parameters (`<param name="...">`) and thrown exceptions (`<exception cref="...">`) where applicable
- Properties must document their intended purpose
- Events must describe when they are raised
- Methods must describe their functionality and parameters