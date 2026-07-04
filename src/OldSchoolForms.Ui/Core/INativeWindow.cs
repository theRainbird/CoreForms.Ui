namespace OldSchoolForms.Ui.Core;

/// <summary>
/// Provides access to the native window handle (HWND / X11 Window / wl_surface).
/// </summary>
public interface INativeWindow
{
    /// <summary>
    /// Gets the native platform-specific window handle.
    /// On Windows this is an HWND, on X11 an X11 Window ID, on Wayland a wl_surface pointer.
    /// </summary>
    nint NativeHandle { get; }
}
