using CoreForms.Ui.Core;
using CoreForms.Ui.Rendering;

namespace CoreForms.Ui.Platform;

/// <summary>
/// Holds per-window rendering resources for a native SDL2 window.
/// Each window has its own renderer and font renderer because SDL2
/// textures and rendering contexts are tied to a specific renderer.
/// </summary>
internal sealed class WindowContext : IDisposable
{
    /// <summary>
    /// Gets the native SDL2 window handle.
    /// </summary>
    public IntPtr WindowHandle { get; }

    /// <summary>
    /// Gets the SDL2 window ID.
    /// </summary>
    public uint WindowId { get; }

    /// <summary>
    /// Gets the form associated with this window.
    /// </summary>
    public Form Form { get; }

    /// <summary>
    /// Gets the SDL2 renderer for this window.
    /// </summary>
    public SdlRenderer Renderer { get; }

    /// <summary>
    /// Gets the font renderer for this window.
    /// </summary>
    public FontRenderer FontRenderer { get; }

    /// <summary>
    /// Initializes a new WindowContext with the specified window handle, form, renderer, and font renderer.
    /// </summary>
    /// <param name="windowHandle">The native SDL2 window handle.</param>
    /// <param name="windowId">The SDL2 window ID.</param>
    /// <param name="form">The form associated with this window.</param>
    /// <param name="renderer">The SDL2 renderer for this window.</param>
    /// <param name="fontRenderer">The font renderer for this window.</param>
    public WindowContext(IntPtr windowHandle, uint windowId, Form form, SdlRenderer renderer, FontRenderer fontRenderer)
    {
        WindowHandle = windowHandle;
        WindowId = windowId;
        Form = form;
        Renderer = renderer;
        FontRenderer = fontRenderer;
    }

    /// <summary>
    /// Releases the renderer and font renderer resources.
    /// </summary>
    public void Dispose()
    {
        Renderer.Dispose();
        FontRenderer.Dispose();
    }
}