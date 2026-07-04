# OldSchoolForms.Ui CLI Template

`dotnet new` template for creating new OldSchoolForms.Ui applications.

## Installation

```bash
dotnet new install OldSchoolFormsUi.Templates
```

To install a specific package version:

```bash
dotnet new install OldSchoolFormsUi.Templates.0.1.0.nupkg
```

## Usage

### Create a New Project

```bash
dotnet new oldschoolforms-app -n MeineApp
```

Creates a new project in the `MeineApp/` directory with the following files:

- `MeineApp.csproj` — Project file with reference to `OldSchoolForms.Ui`
- `Program.cs` — Entry point with `[STAThread]` and `Application.Run()`
- `MainForm.cs` — Empty main form (1024×768)

### With WebBrowser Package

```bash
dotnet new oldschoolforms-app -n MeineApp --useWebBrowser true
```

Additionally adds a reference to `OldSchoolForms.Ui.WebBrowser`.

## Template Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `-n, --name` | Project and directory name | (required) |
| `-o, --output` | Output path for the project | Current directory |
| `--useWebBrowser` | Include WebBrowser package | `false` |
| `--framework, -f` | Target framework | `net10.0` |

## Generated Project File

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>$safename$</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="OldSchoolForms.Ui" Version="0.1.0" />
  </ItemGroup>

  <ItemGroup Condition="'$(useWebBrowser)' == 'true'">
    <PackageReference Include="OldSchoolForms.Ui.WebBrowser" Version="0.1.0" />
  </ItemGroup>

</Project>
```

## Generated Code

### Program.cs

```csharp
using OldSchoolForms.Ui.Core;

namespace MeineApp;

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.Run(new MainForm());
    }
}
```

### MainForm.cs

```csharp
using OldSchoolForms.Ui.Core;
using OldSchoolForms.Ui.Theming;

namespace MeineApp;

public class MainForm : Form
{
    public MainForm()
    {
        Title = "MeineApp";
        Width = 1024;
        Height = 768;
    }

    public override void OnThemeChanged(Theme newTheme)
    {
        base.OnThemeChanged(newTheme);
    }
}
```

## Dependencies

| Package | Version | Description |
|---------|---------|-------------|
| `OldSchoolForms.Ui` | 0.1.0 | Core library (Silk.NET, SkiaSharp, Svg.Skia) |
| `OldSchoolForms.Ui.WebBrowser` | 0.1.0 | Optional: Chromium-based WebView |

## License

MIT License — see [LICENSE](../../LICENSE)
