using System.Reflection;
using CoreForms.Ui.Core;
using CoreForms.Ui.Rendering;
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
    private static IKeyboard? _keyboard;

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

        Console.WriteLine($"[Platform] CreateWindow: id={windowId} form='{form.Text}' focusedWindow='{_focusedWindow?.Text}'");

        window.Load += () =>
        {
            try
            {
                ctx.InitializeRenderer();
                Console.WriteLine($"[Platform] Renderer initialized for window id={windowId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Platform] Failed to initialize renderer: {ex.Message}");
                return;
            }

            var input = window.CreateInput();
            var keyboard = input.Keyboards.FirstOrDefault();
            var mouse = input.Mice.FirstOrDefault();
            _keyboard = keyboard;

            if (keyboard != null)
            {
                keyboard.KeyDown += (kb, key, keyCode) =>
                {
                    Console.WriteLine($"[Platform] KeyDown: key={key} focusedWindow='{_focusedWindow?.Text}' windowId={windowId}");
                    if (_focusedWindow == null) return;
                    var args = new KeyEventArgs
                    {
                        KeyCode = MapKeyCode(key),
                        Modifiers = MapModifierKeys(keyboard)
                    };
                    _focusedWindow.OnKeyDown(args);
                };

                keyboard.KeyUp += (kb, key, keyCode) =>
                {
                    if (_focusedWindow == null) return;
                    var args = new KeyEventArgs
                    {
                        KeyCode = MapKeyCode(key),
                        Modifiers = MapModifierKeys(keyboard)
                    };
                    _focusedWindow.OnKeyUp(args);
                };

                keyboard.KeyChar += (kb, ch) =>
                {
                    if (_focusedWindow == null) return;
                    _focusedWindow.OnTextInput(ch.ToString());
                };
            }
            else
            {
                Console.WriteLine($"[Platform] WARNING: No keyboard for window id={windowId}");
            }

            if (mouse != null)
            {
                mouse.MouseDown += (m, button) =>
                {
                    var pos = mouse.Position;
                    float zoom = form.Zoom;
                    var point = new Point((int)(pos.X / zoom), (int)(pos.Y / zoom));
                    var btn = MapMouseButton(button);
                    var args = new MouseEventArgs(btn, 1, point.X, point.Y, 0);
                    form.OnMouseDown(args);
                };

                mouse.MouseUp += (m, button) =>
                {
                    var pos = mouse.Position;
                    float zoom = form.Zoom;
                    var point = new Point((int)(pos.X / zoom), (int)(pos.Y / zoom));
                    var btn = MapMouseButton(button);
                    var args = new MouseEventArgs(btn, 1, point.X, point.Y, 0);
                    form.OnMouseUp(args);
                };

                mouse.MouseMove += (m, pos) =>
                {
                    float zoom = form.Zoom;
                    var point = new Point((int)(pos.X / zoom), (int)(pos.Y / zoom));
                    _lastMousePosition = point;
                    var args = new MouseEventArgs(MouseButtons.None, 0, point.X, point.Y, 0);
                    form.OnMouseMove(args);
                };

                mouse.Scroll += (m, wheel) =>
                {
                    var point = _lastMousePosition;
                    var delta = (int)wheel.Y;
                    var args = new MouseEventArgs(MouseButtons.None, 0, point.X, point.Y, delta);
                    form.OnMouseWheel(args);
                };
            }

            window.Resize += size =>
            {
                if (size.X > 0 && size.Y > 0)
                {
                    form.SuspendLayout();
                    form.Width = (int)(size.X / form.Zoom);
                    form.Height = (int)(size.Y / form.Zoom);
                    form.ResumeLayout(true);
                    form.OnResize(EventArgs.Empty);
                }
            };

            window.Closing += () =>
            {
                Console.WriteLine($"[Platform] Closing event for window id={windowId} form='{form.Text}'");
                ctx.IsClosing = true;
            };

            window.FocusChanged += focused =>
            {
                Console.WriteLine($"[Platform] FocusChanged: focused={focused} windowId={windowId} form='{form.Text}'");
                if (focused)
                {
                    _focusedWindow = form;
                    form.Focused = true;
                    form.OnGotFocus(EventArgs.Empty);
                }
                else
                {
                    form.Focused = false;
                    form.OnLostFocus(EventArgs.Empty);
                }
            };

            window.Move += pos =>
            {
                form.X = pos.X;
                form.Y = pos.Y;
            };
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

        Console.WriteLine($"[Platform] DestroyWindow: id={windowId} form='{ctx.Form.Text}'");

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
        ctx.IsClosing = true;
        _contexts.Remove(windowId);

        if (_focusedWindow == form)
        {
            _focusedWindow = _contexts.Count > 0
                ? _contexts.Values.FirstOrDefault()?.Form
                : null;
        }

        Console.WriteLine($"[Platform] CleanupWindowOnClose: id={windowId} glCleanup={glCleanup} focusedWindow now='{_focusedWindow?.Text}'");

        CleanupIconImages(windowId);

        if (glCleanup)
        {
            // Programmatic close: GL context is still valid, clean up GL resources
            try { ctx.Window.MakeCurrent(); } catch { }
        }
        else
        {
            // Deferred close (from Closing event): GL context may be invalid, skip GL calls
            ctx.Renderer?.MarkContextLost();
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
            Console.WriteLine($"[SVG] EXCEPTION: {ex.GetType().Name}: {ex.Message}");
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
            MessageBoxIcon.Information => "CoreForms.Ui.Resources.Icons.FluentColor.ic_fluent_checkmark_circle_24_color.svg",
            MessageBoxIcon.Warning => "CoreForms.Ui.Resources.Icons.FluentColor.ic_fluent_warning_24_color.svg",
            MessageBoxIcon.Error => "CoreForms.Ui.Resources.Icons.FluentColor.ic_fluent_error_circle_24_color.svg",
            MessageBoxIcon.Question => "CoreForms.Ui.Resources.Icons.FluentColor.ic_fluent_question_circle_24_color.svg",
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
                continue;

            try
            {
                ctx.DoEvents();
            }
            catch { }

            if (ctx.IsClosing)
                pendingCleanup.Add(ctx);
        }

        foreach (var ctx in pendingCleanup)
        {
            Console.WriteLine($"[Platform] Deferred cleanup: id={ctx.WindowId} form='{ctx.Form.Text}'");
            CleanupWindowOnClose(ctx.WindowId, ctx, ctx.Form, glCleanup: false);
        }

        foreach (var ctx in _contexts.Values.ToList())
        {
            if (ctx.IsClosing || !ctx.IsInitialized || ctx.Renderer == null || ctx.FontRenderer == null)
                continue;

            try
            {
                ctx.Window.MakeCurrent();
                RenderForm(ctx);
                ctx.Window.SwapBuffers();
            }
            catch { }
        }

        Thread.Sleep(1);
    }

    private static void RenderForm(WindowContext ctx)
    {
        var form = ctx.Form;
        var renderer = ctx.Renderer!;
        var fontRenderer = ctx.FontRenderer!;

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
                    if (cmd.Image is SvgImage svgImage)
                    {
                        int targetW = Math.Max(1, (int)Math.Ceiling(cmd.Width));
                        int targetH = Math.Max(1, (int)Math.Ceiling(cmd.Height));
                        var rasterized = svgImage.GetRasterized(targetW, targetH);
                        if (rasterized != null)
                            skImage = rasterized;
                    }
                    renderer.DrawImage(skImage, cmd.X, cmd.Y, cmd.Width, cmd.Height);
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