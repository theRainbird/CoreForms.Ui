namespace OldSchoolForms.Ui.Core;

/// <summary>
/// Provides context information needed by ScrollBarEngine for interaction and invalidation.
/// </summary>
public interface IScrollBarContext
{
    /// <summary>
    /// Gets the current zoom factor.
    /// </summary>
    float Zoom { get; }

    /// <summary>
    /// Requests the host control to repaint.
    /// </summary>
    void Invalidate();

    /// <summary>
    /// Requests mouse capture for thumb dragging or button repeat.
    /// </summary>
    void CaptureMouse(bool capture);
}
