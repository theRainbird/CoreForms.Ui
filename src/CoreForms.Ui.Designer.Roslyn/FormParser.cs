using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CoreForms.Ui.Core;
using CoreForms.Ui.Controls.Advanced;
using CoreForms.Ui.Controls.Basic;
using CoreForms.Ui.Controls.Containers;
using CoreForms.Ui.Layout;

namespace CoreForms.Ui.Designer.Roslyn;

/// <summary>
/// Parses a Windows Forms-style InitializeComponent method from C# source code
/// and reconstructs the control hierarchy on a <see cref="Designer.DesignSurface"/>.
/// </summary>
public class FormParser
{
    /// <summary>
    /// Delegate used to resolve type names to <see cref="Type"/> instances.
    /// Receives the type name, an optional namespace hint, and the full source code.
    /// Returns the resolved type, or null if the type cannot be found.
    /// </summary>
    /// <param name="typeName">The simple type name (e.g. "Button").</param>
    /// <param name="namespaceHint">An optional namespace hint for disambiguation.</param>
    /// <param name="sourceCode">The full source code of the file being parsed.</param>
    /// <returns>The resolved <see cref="Type"/>, or null if not found.</returns>
    public delegate Type? TypeResolver(string typeName, string? namespaceHint, string sourceCode);

    /// <summary>
    /// Gets or sets the type resolver delegate used to create control instances.
    /// The default resolver handles all built-in CoreForms controls.
    /// Set this to resolve custom controls or <see cref="UserControl"/> types from external assemblies.
    /// </summary>
    public TypeResolver? TypeResolverCallback { get; set; }

    private static readonly Dictionary<string, Type> _builtinTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Button", typeof(Button) },
        { "TextBox", typeof(TextBox) },
        { "Label", typeof(Label) },
        { "CheckBox", typeof(CheckBox) },
        { "RadioButton", typeof(RadioButton) },
        { "ListBox", typeof(ListBox) },
        { "ComboBox", typeof(ComboBox) },
        { "ProgressBar", typeof(ProgressBar) },
        { "FontPicker", typeof(FontPicker) },
        { "TabControl", typeof(TabControl) },
        { "TabPage", typeof(TabPage) },
        { "StatusStrip", typeof(StatusStrip) },
        { "MenuStrip", typeof(MenuStrip) },
        { "ToolStrip", typeof(ToolStrip) },
        { "Panel", typeof(Panel) },
        { "FlowLayoutPanel", typeof(FlowLayoutPanel) },
        { "TableLayoutPanel", typeof(TableLayoutPanel) },
        { "CalendarView", typeof(CalendarView) },
        { "DataGridView", typeof(DataGridView) },
    };

    /// <summary>
    /// Parses the given source code and populates the design surface with the reconstructed controls.
    /// </summary>
    /// <param name="sourceCode">The full C# source code of the form file (e.g., MainForm.cs or MainForm.Designer.cs).</param>
    /// <param name="surface">The design surface to populate.</param>
    /// <returns>A list of warnings or informational messages generated during parsing.</returns>
    /// <exception cref="ArgumentNullException">Thrown if sourceCode or surface is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if InitializeComponent cannot be found or parsed.</exception>
    public List<string> Parse(string sourceCode, Designer.DesignSurface surface)
    {
        if (sourceCode == null) throw new ArgumentNullException(nameof(sourceCode));
        if (surface == null) throw new ArgumentNullException(nameof(surface));

        var warnings = new List<string>();
        surface.ClearAll();

        try
        {
            var body = ExtractInitializeComponentBody(sourceCode);
            if (body == null)
            {
                throw new InvalidOperationException("Could not find InitializeComponent method in source code.");
            }

            var instances = ParseControlInstances(body);
            var assignments = ParsePropertyAssignments(body);
            var hierarchy = ParseControlHierarchy(body);

            foreach (var (fieldName, typeName, instance) in instances)
            {
                warnings.Add($"Parsed instance: {fieldName} ({typeName})");
            }

            var controlMap = new Dictionary<string, Core.Control>();
            var typeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var (fieldName, typeName, instance) in instances)
            {
                typeMap[fieldName] = typeName;
                controlMap[fieldName] = instance;
            }

            foreach (var (fieldName, propMap) in assignments)
            {
                if (!controlMap.TryGetValue(fieldName, out var control))
                {
                    warnings.Add($"Warning: Could not find control '{fieldName}' for property assignment.");
                    continue;
                }

                ApplyPropertyAssignments(control, propMap, warnings);
            }

            foreach (var (parentFieldName, childFieldName) in hierarchy)
            {
                if (controlMap.TryGetValue(parentFieldName, out var parentControl) &&
                    controlMap.TryGetValue(childFieldName, out var childControl))
                {
                    parentControl.Controls.Add(childControl);
                    childControl.Parent = parentControl;
                }
                else
                {
                    warnings.Add($"Warning: Could not resolve hierarchy parent '{parentFieldName}' -> child '{childFieldName}'.");
                }
            }

            foreach (var (fieldName, control) in controlMap)
            {
                if (control.Parent == null)
                {
                    var designItem = surface.AddControl(control, control.Location);
                    designItem.Selected = false;
                    warnings.Add($"Added to surface: {fieldName} at ({control.X}, {control.Y})");
                }
            }

            surface.Invalidate();
        }
        catch (Exception ex)
        {
            warnings.Add($"Parsing error: {ex.Message}");
        }

        return warnings;
    }

    /// <summary>
    /// Extracts the body of the InitializeComponent method from the source code.
    /// </summary>
    /// <param name="sourceCode">The full C# source code.</param>
    /// <returns>The method body as a string, or null if not found.</returns>
    private static string? ExtractInitializeComponentBody(string sourceCode)
    {
        var pattern = @"private\s+void\s+InitializeComponent\s*\(\s*\)\s*\{";
        var match = System.Text.RegularExpressions.Regex.Match(sourceCode, pattern);
        if (!match.Success) return null;

        int start = match.Index + match.Length;
        int depth = 1;
        int i = start;

        while (i < sourceCode.Length && depth > 0)
        {
            if (sourceCode[i] == '{') depth++;
            else if (sourceCode[i] == '}') depth--;
            i++;
        }

        if (depth != 0) return null;
        return sourceCode.Substring(start, i - start - 1);
    }

    private List<(string fieldName, string typeName, Core.Control instance)> ParseControlInstances(string body)
    {
        var result = new List<(string fieldName, string typeName, Core.Control instance)>();
        var pattern = @"(\w+)\s*=\s*new\s+(\w+)\s*\(\s*\)\s*;";
        var matches = System.Text.RegularExpressions.Regex.Matches(body, pattern);

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var fieldName = match.Groups[1].Value;
            var typeName = match.Groups[2].Value;

            Type? controlType = ResolveControlType(typeName);
            if (controlType == null)
            {
                continue;
            }

            var instance = (Core.Control)Activator.CreateInstance(controlType)!;
            result.Add((fieldName, typeName, instance));
        }

        return result;
    }

    private static Dictionary<string, Dictionary<string, string>> ParsePropertyAssignments(string body)
    {
        var result = new Dictionary<string, Dictionary<string, string>>();
        var pattern = @"(\w+)\.(\w+)\s*=\s*([^;]+)\s*;";
        var matches = System.Text.RegularExpressions.Regex.Matches(body, pattern);

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var fieldName = match.Groups[1].Value;
            var propName = match.Groups[2].Value;
            var rawValue = match.Groups[3].Value.Trim();

            if (rawValue.StartsWith("//") || rawValue.StartsWith("this."))
            {
                continue;
            }

            if (!result.TryGetValue(fieldName, out var propMap))
            {
                propMap = new Dictionary<string, string>();
                result[fieldName] = propMap;
            }

            propMap[propName] = rawValue;
        }

        return result;
    }

    private static List<(string parentFieldName, string childFieldName)> ParseControlHierarchy(string body)
    {
        var result = new List<(string parentFieldName, string childFieldName)>();
        var pattern = @"(\w+)\.Controls\.Add\((\w+)\)\s*;";
        var matches = System.Text.RegularExpressions.Regex.Matches(body, pattern);

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var parentFieldName = match.Groups[1].Value;
            var childFieldName = match.Groups[2].Value;
            result.Add((parentFieldName, childFieldName));
        }

        return result;
    }

    private static void ApplyPropertyAssignments(Core.Control control, Dictionary<string, string> propMap, List<string> warnings)
    {
        var controlType = control.GetType();

        foreach (var (propName, rawValue) in propMap)
        {
            try
            {
                var prop = controlType.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                if (prop == null)
                {
                    continue;
                }

                var value = ParsePropertyValue(prop.PropertyType, rawValue);
                if (value != null)
                {
                    prop.SetValue(control, value);
                }
            }
            catch
            {
                // Silently skip properties that cannot be set
            }
        }
    }

    private static object? ParsePropertyValue(Type propType, string rawValue)
    {
        rawValue = rawValue.Trim();

        if (string.IsNullOrEmpty(rawValue)) return null;

        // String literal
        if (rawValue.StartsWith("\"") && rawValue.EndsWith("\""))
        {
            var inner = rawValue.Substring(1, rawValue.Length - 2);
            inner = inner.Replace("\\\"", "\"").Replace("\\\\", "\\").Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t");
            return inner;
        }

        // Boolean
        if (rawValue == "true") return true;
        if (rawValue == "false") return false;

        // Integer
        if (int.TryParse(rawValue, out var intVal)) return intVal;

        // new Point(...)
        if (rawValue.StartsWith("new Point("))
        {
            var m = System.Text.RegularExpressions.Regex.Match(rawValue, @"new\s+Point\s*\(\s*(-?\d+)\s*,\s*(-?\d+)\s*\)");
            if (m.Success)
            {
                return new Core.Point(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value));
            }
        }

        // new Size(...)
        if (rawValue.StartsWith("new Size("))
        {
            var m = System.Text.RegularExpressions.Regex.Match(rawValue, @"new\s+Size\s*\(\s*(-?\d+)\s*,\s*(-?\d+)\s*\)");
            if (m.Success)
            {
                return new Core.Size(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value));
            }
        }

        // Color.FromArgb(...)
        if (rawValue.StartsWith("Color.FromArgb("))
        {
            var m = System.Text.RegularExpressions.Regex.Match(rawValue, @"Color\.ArgbFrom\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*\)");
            if (!m.Success)
            {
                m = System.Text.RegularExpressions.Regex.Match(rawValue, @"Color\.Argb\s*\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*\)");
            }
            if (!m.Success)
            {
                m = System.Text.RegularExpressions.Regex.Match(rawValue, @"Color\.FromArgb\s*\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*\)");
            }
            if (m.Success)
            {
                return Core.Color.FromArgb(
                    int.Parse(m.Groups[1].Value),
                    int.Parse(m.Groups[2].Value),
                    int.Parse(m.Groups[3].Value),
                    int.Parse(m.Groups[4].Value));
            }
        }

        // this.someProperty (skip)
        if (rawValue.StartsWith("this.")) return null;

        // Enum (e.g., DockStyle.Fill)
        if (rawValue.Contains("."))
        {
            var parts = rawValue.Split('.');
            if (parts.Length == 2 && propType.IsEnum)
            {
                try
                {
                    return Enum.Parse(propType, parts[1]);
                }
                catch { /* ignore */ }
            }
        }

        return null;
    }

    private Type? ResolveControlType(string typeName)
    {
        if (string.IsNullOrEmpty(typeName)) return null;

        if (_builtinTypes.TryGetValue(typeName, out var builtinType))
        {
            return builtinType;
        }

        if (TypeResolverCallback != null)
        {
            var resolved = TypeResolverCallback(typeName, null, string.Empty);
            if (resolved != null) return resolved;
        }

        return null;
    }

    /// <summary>
    /// Attempts to detect whether the given source code contains a CoreForms form definition.
    /// </summary>
    /// <param name="sourceCode">The C# source code to inspect.</param>
    /// <returns>True if the source appears to be a CoreForms form.</returns>
    public static bool IsFormFile(string sourceCode)
    {
        if (string.IsNullOrEmpty(sourceCode)) return false;

        return sourceCode.Contains("class ") &&
               (sourceCode.Contains(" : Form") || sourceCode.Contains(" : ContainerControl")) &&
               (sourceCode.Contains("InitializeComponent") ||
                sourceCode.Contains("SuspendLayout") ||
                sourceCode.Contains("Controls.Add"));
    }
}
