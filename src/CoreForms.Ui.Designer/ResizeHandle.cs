using System;

namespace CoreForms.Ui.Designer;

/// <summary>
/// Identifies one of the eight resize handles on a selected control.
/// </summary>
[Flags]
public enum ResizeHandle
{
    None = 0,
    TopLeft = 1,
    TopCenter = 2,
    TopRight = 4,
    MiddleLeft = 8,
    MiddleRight = 16,
    BottomLeft = 32,
    BottomCenter = 64,
    BottomRight = 128
}
