using System.Linq;
using OldSchoolForms.Ui.Core;
using OldSchoolForms.Ui.Controls.Basic;
using OldSchoolForms.Ui.Designer;
using OldSchoolForms.Ui.Designer.Roslyn;
using Xunit;

namespace OldSchoolForms.Ui.Designer.Tests;

/// <summary>
/// Verifies that <see cref="CodeGenerator"/> emits unique, valid C# field names and never
/// repeats a variable name across the generated declarations, instantiations and hierarchy.
/// </summary>
public class CodeGeneratorTests
{
    private static IEnumerable<string> DeclaredFields(string code)
    {
        foreach (var raw in code.Replace("\r", "").Split('\n'))
        {
            var line = raw.Trim();
            if (line.StartsWith("private ") && line.EndsWith(";"))
            {
                var body = line[..^1].Trim();
                var parts = body.Split(' ');
                yield return parts[^1];
            }
        }
    }

    private static IEnumerable<string> InstantiatedFields(string code)
    {
        foreach (var raw in code.Replace("\r", "").Split('\n'))
        {
            var line = raw.Trim();
            if (line.EndsWith("();") && line.Contains(" = new "))
            {
                var name = line[..line.IndexOf(" = new", StringComparison.Ordinal)].Trim();
                yield return name;
            }
        }
    }

    private static DesignSurface SurfaceWithTwoToolboxButtons()
    {
        var surface = new DesignSurface { SnapToGrid = false };
        var buttonItem = surface.Toolbox.Items.First(i => i.ControlType == typeof(Button));
        surface.AddControlFromToolbox(buttonItem, new Point(30, 30));
        surface.AddControlFromToolbox(buttonItem, new Point(200, 30));
        return surface;
    }

    [Fact]
    public void Generate_TwoToolboxButtons_SameDefaultName_AreUnique()
    {
        var surface = SurfaceWithTwoToolboxButtons();

        string code = new CodeGenerator(surface).Generate("Demo", "MainForm");

        var declared = DeclaredFields(code).ToList();
        Assert.Equal(2, declared.Count);
        Assert.Equal(declared.Distinct().Count(), declared.Count);
    }

    [Fact]
    public void Generate_TwoToolboxButtons_NextOneGetsIncrementingSuffix()
    {
        var surface = SurfaceWithTwoToolboxButtons();

        string code = new CodeGenerator(surface).Generate("Demo", "MainForm");

        // The first control keeps the toolbox default, the second is suffixed to stay unique.
        Assert.Equal(new[] { "button", "button1" }, DeclaredFields(code));
    }

    [Fact]
    public void Generate_TwoToolboxLabels_SameDefaultName_AreUnique()
    {
        var surface = new DesignSurface { SnapToGrid = false };
        var labelItem = surface.Toolbox.Items.First(i => i.ControlType == typeof(Label));
        surface.AddControlFromToolbox(labelItem, new Point(30, 30));
        surface.AddControlFromToolbox(labelItem, new Point(200, 30));

        string code = new CodeGenerator(surface).Generate("Demo", "MainForm");

        var declared = DeclaredFields(code).ToList();
        Assert.Equal(2, declared.Count);
        Assert.Equal(declared.Distinct().Count(), declared.Count);
    }

    [Fact]
    public void Generate_ControlNamedKeyword_IsSanitizedAndUnique()
    {
        var surface = new DesignSurface { SnapToGrid = false };
        var a = new Button { Name = "class", Size = new Size(120, 40) };
        var b = new Button { Name = "class", Size = new Size(120, 40) };
        surface.AddControl(a, new Point(30, 30));
        surface.AddControl(b, new Point(200, 30));

        string code = new CodeGenerator(surface).Generate("Demo", "MainForm");

        var declared = DeclaredFields(code).ToList();
        Assert.Equal(2, declared.Count);
        Assert.Equal(declared.Distinct().Count(), declared.Count);
        Assert.DoesNotContain("class", declared);
        Assert.DoesNotContain("MainForm", declared);
    }

    [Fact]
    public void Generate_ControlNameWithInvalidCharacters_IsSanitizedIntoValidIdentifier()
    {
        var surface = new DesignSurface { SnapToGrid = false };
        var control = new Button { Name = "my button!", Size = new Size(120, 40) };
        surface.AddControl(control, new Point(30, 30));

        string code = new CodeGenerator(surface).Generate("Demo", "MainForm");

        // The field identifier is sanitized (illegal characters -> underscore); the raw value
        // only survives as the string literal of the control's Name property.
        Assert.Equal(new[] { "my_button_" }, DeclaredFields(code));
        Assert.Contains("my_button_", code);
        Assert.Contains("my_button_.Name = \"my button!\";", code);
    }

    [Fact]
    public void Generate_ControlNamedLikeClass_IsRoutedAroundTheClassName()
    {
        var surface = new DesignSurface { SnapToGrid = false };
        surface.AddControl(new Button { Name = "MainForm", Size = new Size(120, 40) }, new Point(30, 30));

        string code = new CodeGenerator(surface).Generate("Demo", "MainForm");

        // The field must not reuse the class name verbatim.
        Assert.DoesNotContain("MainForm", DeclaredFields(code));
    }

    [Fact]
    public void Generate_EmptySurface_DeclaresNoFields_RootControlIsExcluded()
    {
        var surface = new DesignSurface { SnapToGrid = false };

        string code = new CodeGenerator(surface).Generate("Demo", "MainForm");

        Assert.Empty(DeclaredFields(code));
    }

    [Fact]
    public void Generate_FieldNamesMatchInstantiations_ForEveryControl()
    {
        var surface = SurfaceWithTwoToolboxButtons();
        surface.AddControl(new TextBox { Size = new Size(150, 28) }, new Point(30, 100));

        string code = new CodeGenerator(surface).Generate("Demo", "MainForm");

        var declared = DeclaredFields(code).OrderBy(f => f).ToList();
        var instantiated = InstantiatedFields(code).OrderBy(f => f).ToList();
        Assert.Equal(declared, instantiated);
        Assert.Equal(3, declared.Count);
    }
}
