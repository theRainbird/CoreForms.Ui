using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using CoreForms.Ui.Core;
using CoreForms.Ui.Rendering;
using CoreForms.Ui.Resources;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using SkiaSharp;
using Svg.Skia;
using Key = Silk.NET.Input.Key;

namespace CoreForms.Ui.Platform;

/// <summary>
/// Provides the platform abstraction layer using Silk.NET for window management and input,
/// and SkiaSharp for 2D rendering. Supports multiple simultaneous windows, each with its own
/// renderer and font renderer.
/// </summary>
public static class Platform
{
    private static readonly Dictionary<uint, WindowContext> _contexts = new();
    private static Form? _focusedWindow;
    private static Point _lastMousePosition;
    private static uint _nextWindowId = 1;
    private static readonly Dictionary<(MessageBoxIcon icon, uint windowId, int size), IGraphicsImage> _iconImageCache = new();
    private static readonly Stopwatch _frameTimer = Stopwatch.StartNew();
    private static readonly TimeSpan FrameInterval = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 30);
    private static long _lastRenderTicks;
    private static IKeyboard? _keyboard;

    private static readonly Dictionary<uint, List<Action>> _windowCleanupActions = new();

    private static readonly Dictionary<Form, Form> _modalOwnerMap = new();

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr GlfwGetWin32WindowDelegate(IntPtr window);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void GlfwSetWindowAttribDelegate(IntPtr window, int attrib, int value);

    private const int GLFW_FLOATING = 0x00020007;

    private static readonly GlfwSetWindowAttribDelegate? _glfwSetWindowAttrib = ResolveGlfwSetWindowAttrib();

    [DllImport("libc", EntryPoint = "dlopen", ExactSpelling = true)]
    private static extern IntPtr DlOpen(string? filename, int flags);

    [DllImport("libc", EntryPoint = "dlsym", ExactSpelling = true)]
    private static extern IntPtr DSym(IntPtr handle, string symbol);

    private const int RTLD_LAZY = 1;

    /// <summary>
    /// Called every frame before event processing. External components can hook here.
    /// </summary>
    public static Action? OnFrame { get; set; }

    /// <summary>
    /// Gets the WindowContext for the specified window ID, or null if not found.
    /// </summary>
    /// <param name="windowId">The window ID.</param>
    /// <returns>The WindowContext, or null if not found.</returns>
    internal static WindowContext? GetWindowContext(uint windowId)
    {
        return _contexts.TryGetValue(windowId, out var ctx) ? ctx : null;
    }

    /// <summary>
    /// Gets the WindowContext for the specified form, or null if not found.
    /// </summary>
    /// <param name="form">The form.</param>
    /// <returns>The WindowContext, or null if not found.</returns>
    internal static WindowContext? GetWindowContext(Form form)
    {
        return _contexts.TryGetValue(form.WindowId, out var ctx) ? ctx : null;
    }

    /// <summary>
    /// Gets the native platform window handle for the specified form.
    /// On Windows this is an HWND, on X11 an X11 Window ID, on Wayland a wl_surface pointer.
    /// </summary>
    /// <param name="form">The form whose native handle to retrieve.</param>
    /// <returns>The native window handle, or <see cref="IntPtr.Zero"/> if not available.</returns>
    public static nint GetNativeWindowHandle(Form form)
    {
        var ctx = GetWindowContext(form);
        if (ctx == null)
            return nint.Zero;

        // On Windows, IView.Handle returns the GLFW window pointer, not the Win32 HWND.
        // WebView2 requires a real HWND. glfw3.dll is already loaded by Silk.NET into the
        // process; retrieve its module handle and resolve glfwGetWin32Window via GetProcAddress.
        // Direct DllImport("glfw3") fails because Silk.NET's native resolver only handles
        // calls from Silk.NET assemblies, not from CoreForms.Ui.
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                var glfwModule = GetModuleHandle("glfw3.dll");
                if (glfwModule != IntPtr.Zero)
                {
                    var pFunc = GetProcAddress(glfwModule, "glfwGetWin32Window");
                    if (pFunc != IntPtr.Zero)
                    {
                        var func = Marshal.GetDelegateForFunctionPointer<GlfwGetWin32WindowDelegate>(pFunc);
                        var hwnd = func(ctx.Window.Handle);
                        if (hwnd != IntPtr.Zero)
                            return hwnd;
                    }
                }
            }
            catch
            {
                // Fall through to fallback
            }
        }

        // On non-Windows platforms (X11, Wayland), the GLFW pointer is sufficient
        // for most purposes since CEF and WebKit don't use this handle directly.
        return ctx.Window.Handle;
    }

    private static GlfwSetWindowAttribDelegate? ResolveGlfwSetWindowAttrib()
    {
        IntPtr pFunc = IntPtr.Zero;
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var mod = GetModuleHandle("glfw3.dll");
                if (mod != IntPtr.Zero)
                    pFunc = GetProcAddress(mod, "glfwSetWindowAttrib");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var global = DlOpen(null, RTLD_LAZY);
                if (global != IntPtr.Zero)
                    pFunc = DSym(global, "glfwSetWindowAttrib");
            }
        }
        catch
        {
            return null;
        }

        if (pFunc != IntPtr.Zero)
            return Marshal.GetDelegateForFunctionPointer<GlfwSetWindowAttribDelegate>(pFunc);
        return null;
    }

    internal static void RegisterModal(Form modalForm, Form ownerForm)
    {
        _modalOwnerMap[ownerForm] = modalForm;
        SetModalFloating(modalForm, floating: true);
    }

    internal static void UnregisterModal(Form modalForm, Form ownerForm)
    {
        _modalOwnerMap.Remove(ownerForm);
        SetModalFloating(modalForm, floating: false);
    }

    private static void SetModalFloating(Form form, bool floating)
    {
        if (_glfwSetWindowAttrib == null)
            return;
        if (!_contexts.TryGetValue(form.WindowId, out var ctx))
            return;
        try
        {
            _glfwSetWindowAttrib(ctx.Window.Handle, GLFW_FLOATING, floating ? 1 : 0);
        }
        catch { }
    }

    internal static void CleanupModalMap(Form form)
    {
        var keysToRemove = new List<Form>();
        foreach (var kvp in _modalOwnerMap)
        {
            if (kvp.Key == form || kvp.Value == form)
                keysToRemove.Add(kvp.Key);
        }
        foreach (var key in keysToRemove)
            _modalOwnerMap.Remove(key);
    }

    /// <summary>
    /// Creates a new window for the specified form.
    /// The renderer is initialized asynchronously when the window loads.
    /// </summary>
    /// <param name="form">The form to create a window for.</param>
    /// <returns>A unique handle identifier for the window.</returns>
    /// <exception cref="InvalidOperationException">Thrown when window creation fails.</exception>
    public static IntPtr CreateWindow(Form form)
    {
        var options = WindowOptions.Default;
        options.Title = form.Text ?? string.Empty;
        options.Size = new Vector2D<int>(form.Width > 0 ? form.Width : 800, form.Height > 0 ? form.Height : 600);
        options.IsVisible = true;
        options.ShouldSwapAutomatically = false;
        options.API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.Default, new APIVersion(3, 3));
        options.PreferredDepthBufferBits = 24;
        options.Samples = 0;

        if (form.FormBorderStyle == FormBorderStyle.FixedSingle ||
            form.FormBorderStyle == FormBorderStyle.None ||
            form.FormBorderStyle == FormBorderStyle.FixedDialog ||
            form.FormBorderStyle == FormBorderStyle.Fixed3D)
        {
            options.WindowBorder = WindowBorder.Fixed;
        }
        else
        {
            options.WindowBorder = WindowBorder.Resizable;
        }

        var window = Window.Create(options);

        uint windowId = _nextWindowId++;
        form.WindowId = windowId;

        var ctx = new WindowContext(window, windowId, form);
        _contexts[windowId] = ctx;
        _focusedWindow = form;

        var handle = new IntPtr(windowId);
        form.SetHandle(handle);

        Log($"[Platform] CreateWindow: id={windowId} form='{form.Text}' focusedWindow='{_focusedWindow?.Text}'");

        window.Load += () =>
        {
            var input = window.CreateInput();
            var keyboard = input.Keyboards.FirstOrDefault();
            var mouse = input.Mice.FirstOrDefault();
            _keyboard = keyboard;

            var cleanup = new List<Action>();
            _windowCleanupActions[windowId] = cleanup;

            if (keyboard != null)
            {
                Action<IKeyboard, Key, int> keyDown = (kb, key, keyCode) =>
                {
                    Log($"[Platform] KeyDown: key={key} focusedWindow='{_focusedWindow?.Text}' windowId={windowId}");
                    // If this form is the owner of a modal dialog, route keyboard to the modal
                    if (_modalOwnerMap.TryGetValue(form, out var modalForm) && modalForm.Handle != IntPtr.Zero)
                    {
                        var modalArgs = new KeyEventArgs
                        {
                            KeyCode = MapKeyCode(key),
                            Modifiers = MapModifierKeys(keyboard)
                        };
                        modalForm.OnKeyDown(modalArgs);
                        return;
                    }
                    if (_focusedWindow == null) return;
                    var args = new KeyEventArgs
                    {
                        KeyCode = MapKeyCode(key),
                        Modifiers = MapModifierKeys(keyboard)
                    };
                    _focusedWindow.OnKeyDown(args);
                };
                keyboard.KeyDown += keyDown;
                cleanup.Add(() => keyboard.KeyDown -= keyDown);

                Action<IKeyboard, Key, int> keyUp = (kb, key, keyCode) =>
                {
                    if (_modalOwnerMap.TryGetValue(form, out var modalForm) && modalForm.Handle != IntPtr.Zero)
                    {
                        var modalArgs = new KeyEventArgs
                        {
                            KeyCode = MapKeyCode(key),
                            Modifiers = MapModifierKeys(keyboard)
                        };
                        modalForm.OnKeyUp(modalArgs);
                        return;
                    }
                    if (_focusedWindow == null) return;
                    var args = new KeyEventArgs
                    {
                        KeyCode = MapKeyCode(key),
                        Modifiers = MapModifierKeys(keyboard)
                    };
                    _focusedWindow.OnKeyUp(args);
                };
                keyboard.KeyUp += keyUp;
                cleanup.Add(() => keyboard.KeyUp -= keyUp);

                Action<IKeyboard, char> keyChar = (kb, ch) =>
                {
                    if (_modalOwnerMap.TryGetValue(form, out var modalForm) && modalForm.Handle != IntPtr.Zero)
                    {
                        modalForm.OnTextInput(ch.ToString());
                        return;
                    }
                    if (_focusedWindow == null) return;
                    _focusedWindow.OnTextInput(ch.ToString());
                };
                keyboard.KeyChar += keyChar;
                cleanup.Add(() => keyboard.KeyChar -= keyChar);
            }
            else
            {
                Log($"[Platform] WARNING: No keyboard for window id={windowId}");
            }

            if (mouse != null)
            {
                Action<IMouse, MouseButton> mouseDown = (m, button) =>
                {
                    // Block input to forms that are owners of active modal dialogs,
                    // and try to refocus the modal (works on X11, best-effort on Wayland).
                    if (_modalOwnerMap.ContainsKey(form))
                    {
                        Log($"[Platform] MouseDown BLOCKED for owner form='{form.Text}'");
                        if (_modalOwnerMap.TryGetValue(form, out var modalForm) &&
                            modalForm.Handle != IntPtr.Zero &&
                            _contexts.TryGetValue(modalForm.WindowId, out var modalCtx))
                        {
                            modalCtx.Window.Focus();
                            modalCtx.Window.IsVisible = true;
                        }
                        return;
                    }
                    // Use 'm' (the IMouse parameter) not the captured variable — Silk.NET may
                    // recreate the IMouse object after focus transitions (Wayland/GLFW).
                    var pos = m.Position;
                    float zoom = form.Zoom;
                    var point = new Point((int)(pos.X / zoom), (int)(pos.Y / zoom));
                    var btn = MapMouseButton(button);
                    Log($"[Platform] MouseDown form='{form.Text}' btn={btn} pos=({point.X},{point.Y})");
                    var args = new MouseEventArgs(btn, 1, point.X, point.Y, 0);
                    form.OnMouseDown(args);
                };
                mouse.MouseDown += mouseDown;
                cleanup.Add(() => mouse.MouseDown -= mouseDown);

                Action<IMouse, MouseButton> mouseUp = (m, button) =>
                {
                    var pos = m.Position;
                    float zoom = form.Zoom;
                    var point = new Point((int)(pos.X / zoom), (int)(pos.Y / zoom));
                    var btn = MapMouseButton(button);
                    var args = new MouseEventArgs(btn, 1, point.X, point.Y, 0);
                    form.OnMouseUp(args);
                };
                mouse.MouseUp += mouseUp;
                cleanup.Add(() => mouse.MouseUp -= mouseUp);

                Action<IMouse, System.Numerics.Vector2> mouseMove = (m, pos) =>
                {
                    float zoom = form.Zoom;
                    var point = new Point((int)(pos.X / zoom), (int)(pos.Y / zoom));
                    _lastMousePosition = point;
                    form.Cursor = null;
                    var args = new MouseEventArgs(MouseButtons.None, 0, point.X, point.Y, 0);
                    form.OnMouseMove(args);
                    ApplyFormCursor(form, m);
                };
                mouse.MouseMove += mouseMove;
                cleanup.Add(() => mouse.MouseMove -= mouseMove);

                Action<IMouse, ScrollWheel> scroll = (m, wheel) =>
                {
                    float zoom = form.Zoom;
                    var point = new Point((int)(m.Position.X / zoom), (int)(m.Position.Y / zoom));
                    var args = new MouseEventArgs(MouseButtons.None, 0, point.X, point.Y, wheel.Y);
                    form.OnMouseWheel(args);
                };
                mouse.Scroll += scroll;
                cleanup.Add(() => mouse.Scroll -= scroll);
            }

            Action<Vector2D<int>> resize = size =>
            {
                if (size.X > 0 && size.Y > 0)
                {
                    form.SuspendLayout();
                    form.Width = (int)MathF.Ceiling(size.X / form.Zoom);
                    form.Height = (int)MathF.Ceiling(size.Y / form.Zoom);
                    form.ResumeLayout(true);
                    form.OnResize(EventArgs.Empty);
                    form.Invalidate();
                }
            };
            window.Resize += resize;
            cleanup.Add(() => window.Resize -= resize);

            Action closing = () =>
            {
                Log($"[Platform] Closing event for window id={windowId} form='{form.Text}'");
                ctx.IsClosing = true;
            };
            window.Closing += closing;
            cleanup.Add(() => window.Closing -= closing);

            Action<bool> focusChanged = focused =>
            {
                Log($"[Platform] FocusChanged: focused={focused} windowId={windowId} form='{form.Text}'");
                if (focused)
                {
                    // Always trigger a re-render when focus is regained (Alt+Tab back, etc.)
                    form.Invalidate();

                    // Force at least one render pass regardless of IsVisible state.
                    // After Alt+Tab the compositor may lag and IsVisible can still be false
                    // even though the window has focus. ForceRender ensures the window renders
                    // and is cleared after one successful render.
                    if (_contexts.TryGetValue(windowId, out var ctx))
                        ctx.ForceRender = true;

                    // If this form is the owner of a modal dialog, redirect focus to the modal.
                    // Window.Focus() is required on Wayland where mouse events are only
                    // delivered to the focused window. On some GLFW/Wayland versions this
                    // may be a no-op — IsVisible = true is an additional fallback that forces
                    // the compositor to bring the surface to the foreground.
                    if (_modalOwnerMap.TryGetValue(form, out var modalForm) &&
                        modalForm.Handle != IntPtr.Zero &&
                        _contexts.TryGetValue(modalForm.WindowId, out var modalCtx))
                    {
                        try { modalCtx.Window.Focus(); } catch { }
                        try { modalCtx.Window.IsVisible = true; } catch { }
                        modalCtx.ForceRender = true;
                        _focusedWindow = modalForm;
                        modalForm.Focused = true;
                        modalForm.OnGotFocus(EventArgs.Empty);
                        return;
                    }
                    _focusedWindow = form;
                    form.Focused = true;
                    form.OnGotFocus(EventArgs.Empty);
                }
                else
                {
                    form.Focused = false;
                    form.OnLostFocus(EventArgs.Empty);
                    // Clear the global focused window on focus loss to prevent stale references.
                    // Without this, keyboard events may be routed to a window that no longer has focus.
                    if (_focusedWindow == form)
                        _focusedWindow = null;
                }
            };
            window.FocusChanged += focusChanged;
            cleanup.Add(() => window.FocusChanged -= focusChanged);

            Action<Vector2D<int>> move = pos =>
            {
                form.X = pos.X;
                form.Y = pos.Y;
            };
            window.Move += move;
            cleanup.Add(() => window.Move -= move);

            Action<WindowState> stateChanged = state =>
            {
                form.WindowState = state switch
                {
                    Silk.NET.Windowing.WindowState.Minimized => FormWindowState.Minimized,
                    Silk.NET.Windowing.WindowState.Maximized => FormWindowState.Maximized,
                    _ => FormWindowState.Normal
                };
                form.Invalidate();
            };
            window.StateChanged += stateChanged;
            cleanup.Add(() => window.StateChanged -= stateChanged);

            try
            {
                ctx.InitializeRenderer();
                Log($"[Platform] Renderer initialized for window id={windowId}");
            }
            catch (Exception ex)
            {
                Log($"[Platform] Failed to initialize renderer: {ex.Message}");
            }
        };

        window.Initialize();

        // Convert initial pixel dimensions to logical coordinates
        form.Width = (int)(form.Width / form.Zoom);
        form.Height = (int)(form.Height / form.Zoom);

        return handle;
    }

    private static WindowContext? GetFormContext(Form form)
    {
        return _contexts.TryGetValue(form.WindowId, out var ctx) ? ctx : null;
    }

    /// <summary>
    /// Destroys the window with the specified handle and releases its rendering resources.
    /// Called when a form is programmatically closed (e.g., MessageBox button click).
    /// The GL context is still valid here, so we can do full cleanup.
    /// </summary>
    /// <param name="handle">The window handle identifier.</param>
    public static void DestroyWindow(IntPtr handle)
    {
        uint windowId = (uint)handle;
        if (!_contexts.TryGetValue(windowId, out var ctx))
            return;

        Log($"[Platform] DestroyWindow: id={windowId} form='{ctx.Form.Text}'");

        CleanupWindowOnClose(windowId, ctx, ctx.Form, glCleanup: true);

        try { ctx.Window.IsVisible = false; } catch { }
        try { ctx.Window.Close(); } catch { }
    }

    /// <summary>
    /// Performs cleanup for a closing window. When glCleanup is true, GL resources are
    /// cleaned up (safe for programmatic close). When false, only non-GL cleanup is done
    /// (safe for deferred cleanup from the Closing event).
    /// </summary>
    private static void CleanupWindowOnClose(uint windowId, WindowContext ctx, Form form, bool glCleanup)
    {
        if (_windowCleanupActions.TryGetValue(windowId, out var cleanup))
        {
            foreach (var action in cleanup)
                action();
            _windowCleanupActions.Remove(windowId);
        }

        ctx.IsClosing = true;
        _contexts.Remove(windowId);

        if (_focusedWindow == form)
        {
            _focusedWindow = _contexts.Count > 0
                ? _contexts.Values.FirstOrDefault()?.Form
                : null;
        }

        // If the closing form was a modal dialog or an owner, clean up the modal map
        CleanupModalMap(form);

        Log($"[Platform] CleanupWindowOnClose: id={windowId} glCleanup={glCleanup} focusedWindow now='{_focusedWindow?.Text}'");

        CleanupIconImages(windowId);

        if (glCleanup)
        {
            // Programmatic close: GL context is still valid, clean up GL resources
            try { ctx.Window.MakeCurrent(); } catch { }
        }
        else
        {
            // Deferred close (from Closing event): GL context may be invalid, skip GL calls
            try { ctx.Renderer?.MarkContextLost(); } catch { }
        }

        try { ctx.Dispose(); } catch { }

        form.OnFormClosing(new FormClosingEventArgs(CloseReason.UserClosing, false));
        Application.Instance.UnregisterForm(form);
        form.SetHandle(IntPtr.Zero);
    }

    /// <summary>
    /// Sets the title of the window with the specified handle.
    /// </summary>
    /// <param name="handle">The window handle identifier.</param>
    /// <param name="title">The new title.</param>
    public static void SetWindowTitle(IntPtr handle, string title)
    {
        uint windowId = (uint)handle;
        if (_contexts.TryGetValue(windowId, out var ctx))
        {
            ctx.Window.Title = title;
        }
    }

    /// <summary>
    /// Minimizes the window with the specified handle.
    /// </summary>
    /// <param name="handle">The window handle identifier.</param>
    public static void MinimizeWindow(IntPtr handle)
    {
        uint windowId = (uint)handle;
        if (_contexts.TryGetValue(windowId, out var ctx))
        {
            ctx.Window.WindowState = WindowState.Minimized;
        }
    }

    /// <summary>
    /// Maximizes the window with the specified handle.
    /// </summary>
    /// <param name="handle">The window handle identifier.</param>
    public static void MaximizeWindow(IntPtr handle)
    {
        uint windowId = (uint)handle;
        if (_contexts.TryGetValue(windowId, out var ctx))
        {
            ctx.Window.WindowState = WindowState.Maximized;
        }
    }

    /// <summary>
    /// Restores the window with the specified handle to its normal state.
    /// </summary>
    /// <param name="handle">The window handle identifier.</param>
    public static void RestoreWindow(IntPtr handle)
    {
        uint windowId = (uint)handle;
        if (_contexts.TryGetValue(windowId, out var ctx))
        {
            ctx.Window.WindowState = WindowState.Normal;
        }
    }

    /// <summary>
    /// Moves the window to the specified position.
    /// </summary>
    /// <param name="handle">The window handle identifier.</param>
    /// <param name="x">The new x-coordinate.</param>
    /// <param name="y">The new y-coordinate.</param>
    public static void MoveWindow(IntPtr handle, int x, int y)
    {
        uint windowId = (uint)handle;
        if (_contexts.TryGetValue(windowId, out var ctx))
        {
            ctx.Window.Position = new Vector2D<int>(x, y);
        }
    }

    /// <summary>
    /// Resizes the window to the specified dimensions.
    /// </summary>
    /// <param name="handle">The window handle identifier.</param>
    /// <param name="width">The new width.</param>
    /// <param name="height">The new height.</param>
    public static void ResizeWindow(IntPtr handle, int width, int height)
    {
        uint windowId = (uint)handle;
        if (_contexts.TryGetValue(windowId, out var ctx))
        {
            ctx.Window.Size = new Vector2D<int>(width, height);
        }
    }

    /// <summary>
    /// Gets the position of the window.
    /// </summary>
    /// <param name="handle">The window handle identifier.</param>
    /// <returns>A tuple containing the x and y coordinates.</returns>
    public static (int x, int y) GetWindowPosition(IntPtr handle)
    {
        uint windowId = (uint)handle;
        if (_contexts.TryGetValue(windowId, out var ctx))
        {
            var pos = ctx.Window.Position;
            return (pos.X, pos.Y);
        }
        return (0, 0);
    }

    /// <summary>
    /// Gets the size of the window.
    /// </summary>
    /// <param name="handle">The window handle identifier.</param>
    /// <returns>A tuple containing the width and height.</returns>
    public static (int width, int height) GetWindowSize(IntPtr handle)
    {
        uint windowId = (uint)handle;
        if (_contexts.TryGetValue(windowId, out var ctx))
        {
            var size = ctx.Window.Size;
            return (size.X, size.Y);
        }
        return (0, 0);
    }

    /// <summary>
    /// Gets the window state of the window.
    /// </summary>
    /// <param name="handle">The window handle identifier.</param>
    /// <returns>The current window state.</returns>
    public static FormWindowState GetWindowState(IntPtr handle)
    {
        uint windowId = (uint)handle;
        if (_contexts.TryGetValue(windowId, out var ctx))
        {
            return ctx.Window.WindowState switch
            {
                WindowState.Minimized => FormWindowState.Minimized,
                WindowState.Maximized => FormWindowState.Maximized,
                _ => FormWindowState.Normal
            };
        }
        return FormWindowState.Normal;
    }

    /// <summary>
    /// Brings the window to the front of the z-order.
    /// </summary>
    /// <param name="handle">The window handle identifier.</param>
    public static void BringToFront(IntPtr handle)
    {
        uint windowId = (uint)handle;
        if (_contexts.TryGetValue(windowId, out var ctx))
        {
            try { ctx.Window.Focus(); } catch { }
        }
    }

    /// <summary>
    /// Gets the currently focused window.
    /// </summary>
    public static Form? FocusedWindow => _focusedWindow;

    /// <summary>
    /// Logs a debug message. Calls are completely eliminated in Release builds.
    /// </summary>
    [Conditional("DEBUG")]
    private static void Log(string message) => Console.WriteLine(message);

    /// <summary>
    /// Measures the dimensions of text using the font renderer of the specified window.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="font">The font to use.</param>
    /// <returns>A tuple containing the width and height.</returns>
    public static (int width, int height) MeasureText(string text, Core.Font font)
    {
        return MeasureText(text, font, 1.0f);
    }

    /// <summary>
    /// Measures the dimensions of text using the font renderer of the specified window with zoom scaling.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="font">The font to use.</param>
    /// <param name="zoom">The zoom factor for scaling.</param>
    /// <returns>A tuple containing the width and height.</returns>
    public static (int width, int height) MeasureText(string text, Core.Font font, float zoom)
    {
        var ctx = _contexts.Values.FirstOrDefault(c => c.IsInitialized);
        if (ctx == null || ctx.FontRenderer == null)
            return (0, 0);
        return ctx.FontRenderer.MeasureText(text, font, zoom);
    }

    /// <summary>
    /// Sets whether the window has borders.
    /// </summary>
    /// <param name="handle">The window handle identifier.</param>
    /// <param name="bordered">Whether the window should have borders.</param>
    public static void SetBordered(IntPtr handle, bool bordered)
    {
        uint windowId = (uint)handle;
        if (_contexts.TryGetValue(windowId, out var ctx))
        {
            ctx.Window.WindowBorder = bordered ? WindowBorder.Fixed : WindowBorder.Hidden;
        }
    }

    /// <summary>
    /// Loads a message box icon as an IGraphicsImage for the specified window.
    /// Uses Svg.Skia for resolution-independent SVG icons with alpha transparency.
    /// The image is cached per (icon, windowId, size) pair.
    /// </summary>
    /// <param name="icon">The message box icon type to load.</param>
    /// <param name="windowId">The window ID to associate the icon with.</param>
    /// <param name="size">The desired icon size in pixels. Default is 48.</param>
    /// <returns>The IGraphicsImage, or null if the icon could not be loaded.</returns>
    public static IGraphicsImage? LoadMessageBoxIcon(MessageBoxIcon icon, uint windowId, int size = 48)
    {
        if (icon == MessageBoxIcon.None)
            return null;

        var key = (icon, windowId, size);
        if (_iconImageCache.TryGetValue(key, out var cached))
            return cached;

        string? resourceName = GetIconResourceName(icon);
        if (resourceName == null)
            return null;

        var image = LoadSvgResource(resourceName, size);
        if (image != null)
        {
            _iconImageCache[key] = image;
        }

        return image;
    }

    /// <summary>
    /// Loads an SVG resource from an embedded resource and renders it to an IGraphicsImage
    /// at the specified pixel size with full alpha transparency support.
    /// Uses Svg.Skia for pure C# SVG rasterization.
    /// </summary>
    /// <param name="resourceName">The manifest resource name of the SVG file.</param>
    /// <param name="size">The desired width/height in pixels.</param>
    /// <returns>The IGraphicsImage, or null if loading failed.</returns>
    public static IGraphicsImage? LoadSvgResource(string resourceName, int size)
    {
        try
        {
            var assembly = typeof(Platform).Assembly;
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                return null;
            return SvgImage.FromSvgStream(stream, size);
        }
        catch (Exception ex)
        {
            Log($"[SVG] EXCEPTION: {ex.GetType().Name}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Loads a raster image resource (PNG, JPEG, BMP, etc.) from an embedded resource.
    /// Supports alpha transparency for cutout images.
    /// </summary>
    /// <param name="resourceName">The manifest resource name of the image file.</param>
    /// <returns>The IGraphicsImage, or null if loading failed.</returns>
    public static IGraphicsImage? LoadRasterImageResource(string resourceName)
    {
        return RasterImage.FromResource(resourceName);
    }

    private static string? GetIconResourceName(MessageBoxIcon icon)
    {
        return icon switch
        {
            MessageBoxIcon.Information => "CoreForms.Ui.Resources.Icons.SvgRepo.info.svg",
            MessageBoxIcon.Warning => "CoreForms.Ui.Resources.Icons.FluentColor.ic_fluent_warning_48_color.svg",
            MessageBoxIcon.Error => "CoreForms.Ui.Resources.Icons.FluentColor.ic_fluent_error_circle_48_color.svg",
            MessageBoxIcon.Question => "CoreForms.Ui.Resources.Icons.FluentColor.ic_fluent_question_circle_48_color.svg",
            _ => null
        };
    }

    /// <summary>
    /// Cleans up icon images associated with the specified window ID.
    /// Called when a window is being destroyed.
    /// </summary>
    /// <param name="windowId">The window ID whose icon images should be cleaned up.</param>
    internal static void CleanupIconImages(uint windowId)
    {
        var keysToRemove = new List<(MessageBoxIcon, uint, int)>();
        foreach (var kvp in _iconImageCache)
        {
            if (kvp.Key.Item2 == windowId)
            {
                kvp.Value?.Dispose();
                keysToRemove.Add(kvp.Key);
            }
        }
        foreach (var key in keysToRemove)
        {
            _iconImageCache.Remove(key);
        }
    }

    /// <summary>
    /// Processes all pending events and renders all active windows.
    /// Called by the application's main loop.
    /// </summary>
    /// <param name="app">The application instance.</param>
    public static void ProcessEvents(Application app)
    {
        OnFrame?.Invoke();
        var contextsSnapshot = _contexts.Values.ToList();
        var pendingCleanup = new List<WindowContext>();

        foreach (var ctx in contextsSnapshot)
        {
            if (ctx.IsClosing)
            {
                if (_contexts.ContainsKey(ctx.WindowId))
                    pendingCleanup.Add(ctx);
                continue;
            }

            try
            {
                // Safeguard against DoEvents blocking indefinitely on some platforms.
                // Under normal conditions DoEvents returns in <1ms. A 2-second threshold
                // detects hangs without interfering with normal operation.
                var sw = Stopwatch.StartNew();
                ctx.DoEvents();
                if (sw.ElapsedMilliseconds > 2000)
                {
                    Console.WriteLine($"[Platform] WARNING: DoEvents took {sw.ElapsedMilliseconds}ms for window id={ctx.WindowId} form='{ctx.Form.Text}'");
                }
            }
            catch { }

            if (ctx.IsClosing && _contexts.ContainsKey(ctx.WindowId))
                pendingCleanup.Add(ctx);
        }

        foreach (var ctx in pendingCleanup)
        {
            Log($"[Platform] Deferred cleanup: id={ctx.WindowId} form='{ctx.Form.Text}'");
            CleanupWindowOnClose(ctx.WindowId, ctx, ctx.Form, glCleanup: false);
            // Actually close and hide the Silk.NET window (CleanupWindowOnClose only removes
            // from _contexts and disposes renderers, but does not destroy the native window)
            try { ctx.Window.IsVisible = false; } catch { }
            try { ctx.Window.Close(); } catch { }
        }

        // 30 fps frame pacing: sleep until the next frame is due
        long elapsed = _frameTimer.Elapsed.Ticks - _lastRenderTicks;
        if (elapsed < FrameInterval.Ticks)
        {
            // Sleep for the remaining time (max 33ms at 30fps), clamped to a minimum of 1ms
            long remaining = FrameInterval.Ticks - elapsed;
            int sleepMs = (int)(remaining / TimeSpan.TicksPerMillisecond);
            if (sleepMs > 0)
                Thread.Sleep(sleepMs);
        }

        foreach (var ctx in _contexts.Values.ToList())
        {
            if (ctx.IsClosing || !ctx.IsInitialized || ctx.Renderer == null || ctx.FontRenderer == null)
                continue;

            var form = ctx.Form;

            // Skip minimized windows
            if (form.WindowState == FormWindowState.Minimized)
                continue;

            // Skip hidden windows — SwapBuffers can block indefinitely on some Linux
            // GPU drivers when the window is not on the current virtual desktop or
            // is fully obscured by other windows. GLFW does not fire any event to
            // notify us, so we must check IsVisible explicitly. ForceRender overrides
            // this check to ensure at least one render pass after focus transitions.
            if (!ctx.Window.IsVisible && !ctx.ForceRender)
                continue;

            try
            {
                ctx.Window.MakeCurrent();
                RenderForm(ctx);
                ctx.Window.SwapBuffers();
                // Clear ForceRender after a successful render pass.
                ctx.ForceRender = false;
            }
            catch (Exception ex)
            {
                // Clear ForceRender even on error to avoid infinite forced renders.
                ctx.ForceRender = false;
                Console.WriteLine($"[Platform] Render error for '{form.Text}': {ex.GetType().Name} - {ex.Message}");
            }
        }

        _lastRenderTicks = _frameTimer.Elapsed.Ticks;
    }

    private static void RenderForm(WindowContext ctx)
    {
        var form = ctx.Form;
        var renderer = ctx.Renderer!;
        var fontRenderer = ctx.FontRenderer!;

        form.ClearRenderFlag();
        renderer.Clear(form.BackColor);

        using var g = new Graphics();
        g.MeasureText = (text, font, zoom) => fontRenderer.MeasureText(text, font, zoom);
        form.Render(g);

        foreach (var cmd in g.GetCommands())
        {
            ExecuteDrawCommand(cmd, renderer, fontRenderer);
        }

        using var gOverlay = new Graphics();
        gOverlay.MeasureText = (text, font, zoom) => fontRenderer.MeasureText(text, font, zoom);
        form.RenderOverlay(gOverlay);

        foreach (var cmd in gOverlay.GetCommands())
        {
            ExecuteDrawCommand(cmd, renderer, fontRenderer);
        }

        renderer.Present();
    }

    private static void ExecuteDrawCommand(DrawCommand cmd, SkiaRenderer renderer, SkiaFontRenderer fontRenderer)
    {
        renderer.SetClipRect(cmd.ClipBounds);

        switch (cmd.Type)
        {
            case DrawCommandType.FillRectangle:
                renderer.FillRectangle(cmd.Color, cmd.X, cmd.Y, cmd.Width, cmd.Height);
                break;
            case DrawCommandType.DrawRectangle:
                renderer.DrawRectangle(cmd.Color, cmd.X, cmd.Y, cmd.Width, cmd.Height, cmd.LineWidth);
                break;
            case DrawCommandType.DrawLine:
                renderer.DrawLine(cmd.Color, cmd.X, cmd.Y, cmd.X2, cmd.Y2, cmd.LineWidth);
                break;
            case DrawCommandType.DrawString:
                if (!string.IsNullOrEmpty(cmd.Text) && cmd.Font != null)
                {
                    fontRenderer.DrawText(cmd.Text, cmd.Font, cmd.Color, cmd.X, cmd.Y, renderer.Canvas, cmd.Zoom);
                }
                break;
            case DrawCommandType.FillTriangle:
                renderer.FillTriangle(cmd.Color, cmd.X, cmd.Y, cmd.X2, cmd.Y2, cmd.X3, cmd.Y3);
                break;
            case DrawCommandType.FillPie:
                renderer.FillPie(cmd.Color, cmd.X, cmd.Y, cmd.Width, cmd.Height, cmd.StartAngle, cmd.SweepAngle);
                break;
            case DrawCommandType.DrawArc:
                renderer.DrawArc(cmd.Color, cmd.X, cmd.Y, cmd.Width, cmd.Height, cmd.StartAngle, cmd.SweepAngle, cmd.LineWidth);
                break;
            case DrawCommandType.FillPolygon:
                if (cmd.Points != null)
                    renderer.FillPolygon(cmd.Color, cmd.Points);
                break;
            case DrawCommandType.DrawPolygon:
                if (cmd.Points != null)
                    renderer.DrawPolygon(cmd.Color, cmd.Points, cmd.LineWidth);
                break;
            case DrawCommandType.FillEllipse:
                renderer.FillEllipse(cmd.Color, cmd.X, cmd.Y, cmd.Width, cmd.Height);
                break;
            case DrawCommandType.DrawEllipse:
                renderer.DrawEllipse(cmd.Color, cmd.X, cmd.Y, cmd.Width, cmd.Height, cmd.LineWidth);
                break;
            case DrawCommandType.DrawImage:
                if (cmd.Image?.NativeImage != null)
                {
                    var skImage = cmd.Image.NativeImage;
                    var drawX = cmd.X;
                    var drawY = cmd.Y;
                    var drawW = cmd.Width;
                    var drawH = cmd.Height;

                    if (cmd.Image is SvgImage svgImage)
                    {
                        int targetW = Math.Max(1, (int)Math.Ceiling(cmd.Width));
                        int targetH = Math.Max(1, (int)Math.Ceiling(cmd.Height));
                        var rasterized = svgImage.GetRasterized(targetW, targetH);
                        if (rasterized != null)
                        {
                            skImage = rasterized;
                            drawX = MathF.Round(cmd.X);
                            drawY = MathF.Round(cmd.Y);
                            drawW = targetW;
                            drawH = targetH;
                        }
                    }

                    renderer.DrawImage(skImage, drawX, drawY, drawW, drawH);
                }
                break;
        }
    }

    private static Core.Keys MapKeyCode(Key key)
    {
        return key switch
        {
            Key.Enter => Core.Keys.Enter,
            Key.Escape => Core.Keys.Escape,
            Key.Backspace => Core.Keys.Back,
            Key.Tab => Core.Keys.Tab,
            Key.Space => Core.Keys.Space,
            Key.Left => Core.Keys.Left,
            Key.Right => Core.Keys.Right,
            Key.Up => Core.Keys.Up,
            Key.Down => Core.Keys.Down,
            Key.Home => Core.Keys.Home,
            Key.End => Core.Keys.End,
            Key.PageUp => Core.Keys.PageUp,
            Key.PageDown => Core.Keys.PageDown,
            Key.Insert => Core.Keys.Insert,
            Key.Delete => Core.Keys.Delete,
            Key.F1 => Core.Keys.F1,
            Key.F2 => Core.Keys.F2,
            Key.F3 => Core.Keys.F3,
            Key.F4 => Core.Keys.F4,
            Key.F5 => Core.Keys.F5,
            Key.F6 => Core.Keys.F6,
            Key.F7 => Core.Keys.F7,
            Key.F8 => Core.Keys.F8,
            Key.F9 => Core.Keys.F9,
            Key.F10 => Core.Keys.F10,
            Key.F11 => Core.Keys.F11,
            Key.F12 => Core.Keys.F12,
            Key.A => Core.Keys.A,
            Key.B => Core.Keys.B,
            Key.C => Core.Keys.C,
            Key.D => Core.Keys.D,
            Key.E => Core.Keys.E,
            Key.F => Core.Keys.F,
            Key.G => Core.Keys.G,
            Key.H => Core.Keys.H,
            Key.I => Core.Keys.I,
            Key.J => Core.Keys.J,
            Key.K => Core.Keys.K,
            Key.L => Core.Keys.L,
            Key.M => Core.Keys.M,
            Key.N => Core.Keys.N,
            Key.O => Core.Keys.O,
            Key.P => Core.Keys.P,
            Key.Q => Core.Keys.Q,
            Key.R => Core.Keys.R,
            Key.S => Core.Keys.S,
            Key.T => Core.Keys.T,
            Key.U => Core.Keys.U,
            Key.V => Core.Keys.V,
            Key.W => Core.Keys.W,
            Key.X => Core.Keys.X,
            Key.Y => Core.Keys.Y,
            Key.Z => Core.Keys.Z,
            Key.Number0 => Core.Keys.D0,
            Key.Number1 => Core.Keys.D1,
            Key.Number2 => Core.Keys.D2,
            Key.Number3 => Core.Keys.D3,
            Key.Number4 => Core.Keys.D4,
            Key.Number5 => Core.Keys.D5,
            Key.Number6 => Core.Keys.D6,
            Key.Number7 => Core.Keys.D7,
            Key.Number8 => Core.Keys.D8,
            Key.Number9 => Core.Keys.D9,
            Key.AltLeft or Key.AltRight => Core.Keys.Menu,
            _ => Core.Keys.None
        };
    }

    private static ModifierKeys MapModifierKeys(IKeyboard keyboard)
    {
        var modifiers = ModifierKeys.None;
        if (keyboard.IsKeyPressed(Key.ShiftLeft) || keyboard.IsKeyPressed(Key.ShiftRight))
            modifiers |= ModifierKeys.Shift;
        if (keyboard.IsKeyPressed(Key.ControlLeft) || keyboard.IsKeyPressed(Key.ControlRight))
            modifiers |= ModifierKeys.Control;
        if (keyboard.IsKeyPressed(Key.AltLeft) || keyboard.IsKeyPressed(Key.AltRight))
            modifiers |= ModifierKeys.Alt;
        return modifiers;
    }

    private static MouseButtons MapMouseButton(Silk.NET.Input.MouseButton button)
    {
        return button switch
        {
            Silk.NET.Input.MouseButton.Left => MouseButtons.Left,
            Silk.NET.Input.MouseButton.Right => MouseButtons.Right,
            Silk.NET.Input.MouseButton.Middle => MouseButtons.Middle,
            _ => MouseButtons.None
        };
    }

    private static void ApplyFormCursor(Form form, IMouse mouse)
    {
        var cursorType = form.Cursor;
        try
        {
            if (cursorType.HasValue)
            {
                var mapped = MapToStandardCursor(cursorType.Value);
                mouse.Cursor.Type = CursorType.Standard;
                mouse.Cursor.StandardCursor = mapped;
            }
            else
            {
                mouse.Cursor.Type = CursorType.Standard;
                mouse.Cursor.StandardCursor = StandardCursor.Default;
            }
        }
        catch
        {
            // Ignore cursor errors (some backends may not support certain cursor types)
        }
    }

    private static StandardCursor MapToStandardCursor(SystemCursorType cursor)
    {
        return cursor switch
        {
            SystemCursorType.Arrow => StandardCursor.Arrow,
            SystemCursorType.IBeam => StandardCursor.IBeam,
            SystemCursorType.Crosshair => StandardCursor.Crosshair,
            SystemCursorType.Hand => StandardCursor.Hand,
            SystemCursorType.SizeWE => StandardCursor.HResize,
            SystemCursorType.SizeNS => StandardCursor.VResize,
            SystemCursorType.SizeAll => StandardCursor.ResizeAll,
            SystemCursorType.NotAllowed => StandardCursor.NotAllowed,
            _ => StandardCursor.Default
        };
    }

    /// <summary>
    /// Gets the text content from the system clipboard.
    /// </summary>
    /// <returns>The clipboard text, or null if empty or not text.</returns>
    /// <summary>
    /// Gets the currently pressed modifier keys.
    /// </summary>
    /// <summary>
    /// Gets the currently pressed modifier keys.
    /// </summary>
    public static ModifierKeys GetCurrentModifiers()
    {
        if (_keyboard == null) return ModifierKeys.None;
        return MapModifierKeys(_keyboard);
    }

    /// <summary>
    /// Gets the text content from the system clipboard.
    /// </summary>
    /// <returns>The clipboard text, or null if empty or not text.</returns>
    public static string? GetClipboardText()
    {
        return _keyboard?.ClipboardText;
    }

    /// <summary>
    /// Sets the text content of the system clipboard.
    /// </summary>
    /// <param name="text">The text to set.</param>
    public static void SetClipboardText(string? text)
    {
        if (_keyboard != null && text != null)
        {
            _keyboard.ClipboardText = text;
        }
    }
}