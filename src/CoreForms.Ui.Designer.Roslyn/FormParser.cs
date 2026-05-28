using System;
using System.Collections.Generic;
using CoreForms.Ui.Core;
using CoreForms.Ui.Designer;

namespace CoreForms.Ui.Designer.Roslyn;

/// <summary>
/// Parses a Windows Forms-style InitializeComponent method from C# source code
/// and reconstructs the control hierarchy on a <see cref="DesignSurface"/>.
/// </summary>
public class FormParser
{
    /// <summary>
    /// Parses the given source code and populates the design surface with the reconstructed controls.
    /// </summary>
    /// <param name="sourceCode">The full C# source code of the form file (e.g., MainForm.cs or MainForm.Designer.cs).</param>
    /// <param name="surface">The design surface to populate.</param>
    /// <returns>A list of warnings or informational messages generated during parsing.</returns>
    /// <exception cref="ArgumentNullException">Thrown if sourceCode or surface is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if InitializeComponent cannot be found or parsed.</exception>
    public List<string> Parse(string sourceCode, DesignSurface surface)
    {
        if (sourceCode == null) throw new ArgumentNullException(nameof(sourceCode));
        if (surface == null) throw new ArgumentNullException(nameof(surface));

        var warnings = new List<string>();

        try
        {
            // Placeholder — Roslyn syntax tree parsing will be implemented in Phase 7.
            // For now, this method surfaces a clear explanation.
            warnings.Add("Parsing of existing form files is not yet implemented.");
            warnings.Add("Expected Roslyn-based syntax analysis in Phase 7.");

            // In the final implementation, this method will:
            // 1. Parse sourceCode with Microsoft.CodeAnalysis.CSharp.SyntaxFactory.ParseCompilationUnit
            // 2. Walk the tree to find the partial class containing InitializeComponent
            // 3. Extract field declarations (private Button button1;)
            // 4. Parse InitializeComponent body:
            //    - new Button() → Activator.CreateInstance
            //    - Property assignments → control.Property = value
            //    - Event assignments → store event name (for code navigation)
            //    - Controls.Add() → build hierarchy
            // 5. Call surface.AddControl(control, location) for each control
        }
        catch (Exception ex)
        {
            warnings.Add($"Parsing error: {ex.Message}");
        }

        return warnings;
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
