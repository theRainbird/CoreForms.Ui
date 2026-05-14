namespace CoreForms.Ui.Core;

/// <summary>
/// Provides context information needed by TextEditorEngine for text measurement and invalidation.
/// </summary>
public interface ITextEditorContext
{
    /// <summary>
    /// Gets the font used for text rendering and measurement.
    /// </summary>
    Font Font { get; }

    /// <summary>
    /// Gets the current zoom factor.
    /// </summary>
    float Zoom { get; }

    /// <summary>
    /// Gets the width of the text display area in pixels (excluding padding and borders).
    /// </summary>
    int TextAreaWidth { get; }

    /// <summary>
    /// Requests the host control to repaint.
    /// </summary>
    void Invalidate();
}
