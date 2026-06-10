using CoreForms.Ui.Core;
using CoreForms.Ui.Rendering;
using Silk.NET.Windowing;

namespace CoreForms.Ui.Platform;

/// <summary>
/// Holds per-window rendering resources for a Silk.NET window.
/// Each window has its own renderer and font renderer because SkiaSharp
/// surfaces and font caches are per-context.
/// Renderer creation is deferred until the window's Load event fires,
/// since the OpenGL context is only available after window initialization.
/// </summary>
internal sealed class WindowContext : IDisposable
{
    private SkiaRenderer? _renderer;
    private SkiaFontRenderer? _fontRenderer;

    /// <summary>
    /// Gets the Silk.NET window.
    /// </summary>
    public IWindow Window { get; }

    /// <summary>
    /// Gets the unique window identifier.
    /// </summary>
    public uint WindowId { get; }

    /// <summary>
    /// Gets the form associated with this window.
    /// </summary>
    public Form Form { get; }

    /// <summary>
    /// Gets the SkiaSharp renderer for this window.
    /// Returns null if the window has not yet loaded (before the GL context is ready).
    /// </summary>
    public SkiaRenderer? Renderer => _renderer;

    /// <summary>
    /// Gets the font renderer for this window.
    /// Returns null if the window has not yet loaded.
    /// </summary>
    public SkiaFontRenderer? FontRenderer => _fontRenderer;

    /// <summary>
    /// Gets whether the renderer has been initialized (i.e., the window has loaded).
    /// </summary>
    public bool IsInitialized => _renderer != null;

    /// <summary>
    /// Gets or sets whether this window context is being closed.
    /// When true, the ProcessEvents loop skips rendering for this context
    /// and no GL calls should be made.
    /// </summary>
    public bool IsClosing { get; set; }

    /// <summary>
    /// Gets or sets whether this window should be rendered regardless of its IsVisible state.
    /// Used after focus transitions (Alt+Tab) to ensure the window renders even if the
    /// compositor hasn't updated IsVisible yet. Automatically cleared after one render pass.
    /// </summary>
    public bool ForceRender { get; set; }

    /// <summary>
    /// Initializes a new WindowContext with the specified window and form.
    /// Renderer initialization is deferred until InitializeRenderer is called.
    /// </summary>
    /// <param name="window">The Silk.NET window.</param>
    /// <param name="windowId">The unique window identifier.</param>
    /// <param name="form">The form associated with this window.</param>
    public WindowContext(IWindow window, uint windowId, Form form)
    {
        Window = window;
        WindowId = windowId;
        Form = form;
    }

    /// <summary>
    /// Initializes the renderer and font renderer after the window has loaded
    /// and the OpenGL context is available.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when renderer creation fails.</exception>
    public void InitializeRenderer()
    {
        if (_renderer != null) return;

        _renderer = new SkiaRenderer(Window);
        _fontRenderer = new SkiaFontRenderer();
    }

    /// <summary>
    /// Processes pending events for this window.
    /// </summary>
    public void DoEvents()
    {
        try
        {
            Window.DoEvents();
        }
        catch { }
    }

    /// <summary>
    /// Releases the renderer, font renderer, and icon image resources.
    /// Cleans up GL resources before disposing the renderer.
    /// </summary>
    public void Dispose()
    {
        _renderer?.CleanupGLResources();
        _renderer?.MarkContextLost();
        Platform.CleanupIconImages(WindowId);
        _renderer?.Dispose();
        _fontRenderer?.Dispose();
    }
}