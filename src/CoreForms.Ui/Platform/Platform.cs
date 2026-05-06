using System.Runtime.InteropServices;
using System.Text;
using CoreForms.Ui.Core;
using CoreForms.Ui.Rendering;

namespace CoreForms.Ui.Platform;

public static class Platform
{
    private static bool _initialized;
    private static IntPtr _window;
    private static Form? _currentForm;
    private static SdlRenderer? _renderer;
    private static FontRenderer? _fontRenderer;
    private static readonly Dictionary<uint, Form> _windows = new();
    private static Form? _focusedWindow;
    private static Point _lastMousePosition;

    static Platform()
    {
        NativeLibrary.SetDllImportResolver(typeof(Platform).Assembly, (name, assembly, searchPath) =>
        {
            if (name == "SDL2")
            {
                return NativeLibrary.Load("libSDL2-2.0.so.0");
            }
            if (name == "SDL2_ttf")
            {
                return NativeLibrary.Load("libSDL2_ttf-2.0.so.0");
            }
            return IntPtr.Zero;
        });
    }

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern int SDL_Init(uint flags);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SDL_Quit();

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern IntPtr SDL_CreateWindow(string title, int x, int y, int w, int h, uint flags);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SDL_DestroyWindow(IntPtr window);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern void SDL_SetWindowTitle(IntPtr window, string title);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SDL_PumpEvents();

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern int SDL_PollEvent(out SDL_Event @event);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SDL_Delay(uint ms);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SDL_MinimizeWindow(IntPtr window);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SDL_MaximizeWindow(IntPtr window);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SDL_RestoreWindow(IntPtr window);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SDL_SetWindowPosition(IntPtr window, int x, int y);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SDL_SetWindowSize(IntPtr window, int w, int h);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SDL_GetWindowPosition(IntPtr window, out int x, out int y);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SDL_GetWindowSize(IntPtr window, out int w, out int h);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern uint SDL_GetWindowFlags(IntPtr window);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern int SDL_SetWindowBordered(IntPtr window, int bordered);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SDL_RaiseWindow(IntPtr window);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SDL_ShowWindow(IntPtr window);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SDL_StartTextInput();

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern uint SDL_GetWindowID(IntPtr window);

    private const uint SDL_INIT_VIDEO = 0x20;
    private const uint SDL_WINDOW_SHOWN = 0x4;
    private const uint SDL_WINDOW_RESIZABLE = 0x20;
    private const uint SDL_WINDOW_MAXIMIZED = 0x8;
    private const uint SDL_WINDOW_MINIMIZED = 0x4;
    private const uint SDL_WINDOW_FULLSCREEN = 0x1;
    private const int SDL_WINDOWPOS_CENTERED = 0x2FFF0000;

    private const int SDL_QUIT = 0x100;
    private const int SDL_WINDOWEVENT = 0x200;
    private const int SDL_MOUSEBUTTONDOWN = 0x401;
    private const int SDL_MOUSEBUTTONUP = 0x402;
    private const int SDL_MOUSEMOTION = 0x400;
    private const int SDL_MOUSEWHEEL = 0x403;
    private const int SDL_KEYDOWN = 0x300;
    private const int SDL_KEYUP = 0x301;
    private const int SDL_TEXTINPUT = 0x303;
    private const int SDL_TEXTEDITING = 0x302;

    private const int SDL_WINDOWEVENT_CLOSE = 0xE;
    private const int SDL_WINDOWEVENT_MOVED = 0x4;
    private const int SDL_WINDOWEVENT_RESIZED = 0x5;
    private const int SDL_WINDOWEVENT_MINIMIZED = 0x7;
    private const int SDL_WINDOWEVENT_MAXIMIZED = 0x8;
    private const int SDL_WINDOWEVENT_RESTORED = 0x9;
    private const int SDL_WINDOWEVENT_FOCUS_GAINED = 0xC;
    private const int SDL_WINDOWEVENT_FOCUS_LOST = 0xD;
    private const int SDL_WINDOWEVENT_ENTER = 0xA;
    private const int SDL_WINDOWEVENT_LEAVE = 0xB;
    private const int SDL_WINDOWEVENT_SHOWN = 0x1;
    private const int SDL_WINDOWEVENT_HIDDEN = 0x2;

    [StructLayout(LayoutKind.Explicit, Size = 56)]
    private struct SDL_Event
    {
        [FieldOffset(0)]
        public uint type;
        [FieldOffset(4)]
        public uint timestamp;
        
        // Common fields
        [FieldOffset(8)]
        public uint windowID;
        
        // Mouse button event fields
        [FieldOffset(12)]
        public uint which;
        [FieldOffset(16)]
        public byte button;
        [FieldOffset(17)]
        public byte state;
        [FieldOffset(18)]
        public byte clicks;
        [FieldOffset(20)]
        public int x;
        [FieldOffset(24)]
        public int y;
        
        // Mouse wheel event fields (different offsets!)
        [FieldOffset(16)]
        public int wheelX;
        [FieldOffset(20)]
        public int wheelY;
        
        // Mouse motion event fields
        [FieldOffset(28)]
        public int xrel;
        [FieldOffset(32)]
        public int yrel;
        
        // Window event fields
        [FieldOffset(12)]
        public int event_;
        [FieldOffset(16)]
        public int data1;
        [FieldOffset(20)]
        public int data2;
        
        // Key event fields
        [FieldOffset(16)]
        public int keysymScancode;
        [FieldOffset(20)]
        public int keysymSym;
        [FieldOffset(24)]
        public ushort keysymMod;
        [FieldOffset(26)]
        public uint keysymUnused;
    }

    public static void Initialize()
    {
        if (_initialized) return;
        
        int result = SDL_Init(SDL_INIT_VIDEO);
        if (result < 0)
        {
            throw new InvalidOperationException("SDL_Init failed: " + GetSDLError());
        }
        _initialized = true;
    }

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern IntPtr SDL_GetError();

    private static string GetSDLError()
    {
        var ptr = SDL_GetError();
        return ptr == IntPtr.Zero ? "Unknown error" : Marshal.PtrToStringAnsi(ptr);
    }

    public static IntPtr CreateWindow(Form form)
    {
        Initialize();

        uint flags = SDL_WINDOW_SHOWN;
        if (form.FormBorderStyle != FormBorderStyle.FixedSingle &&
            form.FormBorderStyle != FormBorderStyle.None)
        {
            flags |= SDL_WINDOW_RESIZABLE;
        }

        _window = SDL_CreateWindow(
            form.Text,
            SDL_WINDOWPOS_CENTERED,
            SDL_WINDOWPOS_CENTERED,
            form.Width > 0 ? form.Width : 800,
            form.Height > 0 ? form.Height : 600,
            flags);

        if (_window == IntPtr.Zero)
        {
            throw new InvalidOperationException("SDL_CreateWindow failed: " + GetSDLError());
        }

        uint windowId = SDL_GetWindowID(_window);
        form.WindowId = windowId;
        _windows[windowId] = form;
        _currentForm = form;
        _focusedWindow = form;

        _renderer = new SdlRenderer(_window);
        _fontRenderer = new FontRenderer(_renderer.Handle);
        
        // Ensure window is visible
        SDL_ShowWindow(_window);
        SDL_RaiseWindow(_window);
        
        // Enable text input for keyboard events
        SDL_StartTextInput();
        
        return _window;
    }

    public static void DestroyWindow(IntPtr handle)
    {
        if (handle != IntPtr.Zero)
        {
            uint windowId = SDL_GetWindowID(handle);
            _windows.Remove(windowId);
            if (_focusedWindow != null && _windows.Count > 0)
            {
                _focusedWindow = _windows.Values.FirstOrDefault();
            }
            _renderer?.Dispose();
            _fontRenderer?.Dispose();
            SDL_DestroyWindow(handle);
        }
    }

    public static void SetWindowTitle(IntPtr handle, string title)
    {
        if (handle != IntPtr.Zero)
        {
            SDL_SetWindowTitle(handle, title);
        }
    }

    public static void MinimizeWindow(IntPtr handle)
    {
        if (handle != IntPtr.Zero)
        {
            SDL_MinimizeWindow(handle);
        }
    }

    public static void MaximizeWindow(IntPtr handle)
    {
        if (handle != IntPtr.Zero)
        {
            SDL_MaximizeWindow(handle);
        }
    }

    public static void RestoreWindow(IntPtr handle)
    {
        if (handle != IntPtr.Zero)
        {
            SDL_RestoreWindow(handle);
        }
    }

    public static void MoveWindow(IntPtr handle, int x, int y)
    {
        if (handle != IntPtr.Zero)
        {
            SDL_SetWindowPosition(handle, x, y);
        }
    }

    public static void ResizeWindow(IntPtr handle, int width, int height)
    {
        if (handle != IntPtr.Zero)
        {
            SDL_SetWindowSize(handle, width, height);
        }
    }

    public static (int x, int y) GetWindowPosition(IntPtr handle)
    {
        if (handle != IntPtr.Zero)
        {
            SDL_GetWindowPosition(handle, out int x, out int y);
            return (x, y);
        }
        return (0, 0);
    }

    public static (int width, int height) GetWindowSize(IntPtr handle)
    {
        if (handle != IntPtr.Zero)
        {
            SDL_GetWindowSize(handle, out int w, out int h);
            return (w, h);
        }
        return (0, 0);
    }

    public static FormWindowState GetWindowState(IntPtr handle)
    {
        if (handle != IntPtr.Zero)
        {
            uint flags = SDL_GetWindowFlags(handle);
            if ((flags & SDL_WINDOW_MINIMIZED) != 0)
                return FormWindowState.Minimized;
            if ((flags & SDL_WINDOW_MAXIMIZED) != 0)
                return FormWindowState.Maximized;
        }
        return FormWindowState.Normal;
    }

    public static void BringToFront(IntPtr handle)
    {
        if (handle != IntPtr.Zero)
        {
            SDL_RaiseWindow(handle);
        }
    }

    public static Form? FocusedWindow => _focusedWindow;

    public static (int width, int height) MeasureText(string text, Core.Font font)
    {
        if (_fontRenderer == null)
            return (0, 0);
        return _fontRenderer.MeasureText(text, font);
    }

    public static void SetBordered(IntPtr handle, bool bordered)
    {
        if (handle != IntPtr.Zero)
        {
            SDL_SetWindowBordered(handle, bordered ? 1 : 0);
        }
    }

    public static void ProcessEvents(Application app)
    {
        SDL_PumpEvents();

        while (SDL_PollEvent(out SDL_Event e) == 1)
        {
            switch (e.type)
            {
                case SDL_QUIT:
                    app.OnQuit();
                    break;

                case SDL_WINDOWEVENT:
                    HandleWindowEvent(e);
                    break;

                case SDL_MOUSEBUTTONDOWN:
                case SDL_MOUSEBUTTONUP:
                    HandleMouseButtonEvent(e, e.type == SDL_MOUSEBUTTONDOWN);
                    break;

                case SDL_MOUSEMOTION:
                    HandleMouseMotionEvent(e);
                    break;

                case SDL_MOUSEWHEEL:
                    HandleMouseWheelEvent(e);
                    break;

                case SDL_KEYDOWN:
                    HandleKeyEvent(e, true);
                    break;

                case SDL_KEYUP:
                    HandleKeyEvent(e, false);
                    break;

                case SDL_TEXTINPUT:
                    HandleTextInputEvent(e);
                    break;
            }
        }

        if (_currentForm != null && _renderer != null)
        {
            RenderForm(_currentForm);
        }
        
        // Limit to ~60 FPS to reduce CPU usage
        SDL_Delay(16);
    }

    private static void HandleWindowEvent(SDL_Event e)
    {
        if (!_windows.TryGetValue(e.windowID, out var form)) return;

        int eventType = e.event_;

        if (eventType == SDL_WINDOWEVENT_CLOSE)
        {
            form.Close();
        }
        else if (eventType == SDL_WINDOWEVENT_RESIZED)
        {
            form.Width = e.data1;
            form.Height = e.data2;
            form.OnResize(EventArgs.Empty);
        }
        else if (eventType == SDL_WINDOWEVENT_MOVED)
        {
            form.X = e.data1;
            form.Y = e.data2;
        }
        else if (eventType == SDL_WINDOWEVENT_MINIMIZED)
        {
            form.WindowState = FormWindowState.Minimized;
            form.OnWindowStateChanged();
        }
        else if (eventType == SDL_WINDOWEVENT_MAXIMIZED || eventType == SDL_WINDOWEVENT_FOCUS_LOST)
        {
            uint flags = SDL_GetWindowFlags(form.Handle);
            if ((flags & 0x8) != 0)
            {
                form.WindowState = FormWindowState.Maximized;
                form.OnWindowStateChanged();
            }
            else if ((flags & 0x4) != 0)
            {
                form.WindowState = FormWindowState.Minimized;
                form.OnWindowStateChanged();
            }
            else
            {
                form.Focused = false;
                form.OnLostFocus(EventArgs.Empty);
            }
        }
        else if (eventType == SDL_WINDOWEVENT_RESTORED)
        {
            form.WindowState = FormWindowState.Normal;
            form.OnWindowStateChanged();
        }
        else if (eventType == SDL_WINDOWEVENT_FOCUS_GAINED)
        {
            _focusedWindow = form;
            form.Focused = true;
            form.OnGotFocus(EventArgs.Empty);
        }
    }

    private static void HandleMouseButtonEvent(SDL_Event e, bool isDown)
    {
        if (!_windows.TryGetValue(e.windowID, out var form)) return;

        var point = new Point(e.x, e.y);
        var args = new MouseEventArgs(MouseButtons.Left, e.clicks, point.X, point.Y, 0);

        if (isDown)
            form.OnMouseDown(args);
        else
            form.OnMouseUp(args);
    }

private static void HandleMouseMotionEvent(SDL_Event e)
    {
        if (!_windows.TryGetValue(e.windowID, out var form)) return;

        var point = new Point(e.x, e.y);
        _lastMousePosition = point;
        var args = new MouseEventArgs(MouseButtons.None, 0, point.X, point.Y, 0);
        form.OnMouseMove(args);
    }

    private static void HandleMouseWheelEvent(SDL_Event e)
    {
        if (!_windows.TryGetValue(e.windowID, out var form)) return;

        // SDL2 wheel delta is in wheelY field (positive = up, negative = down)
        var args = new MouseEventArgs(MouseButtons.None, 0, _lastMousePosition.X, _lastMousePosition.Y, e.wheelY);
        form.OnMouseWheel(args);
    }

    private static void HandleKeyEvent(SDL_Event e, bool isDown)
    {
        if (_focusedWindow == null) return;

        var args = new KeyEventArgs
        {
            KeyCode = MapKeyCode(e.keysymSym),
            Modifiers = MapModifierKeys(e.keysymMod)
        };

        if (isDown)
            _focusedWindow.OnKeyDown(args);
        else
            _focusedWindow.OnKeyUp(args);
    }

    private const uint SDLK_SCANCODE_MASK = 0x40000000;

    private static Keys MapKeyCode(int sdlKey)
    {
        if ((sdlKey & SDLK_SCANCODE_MASK) == 0)
        {
            if (sdlKey == 127)
                return Keys.Delete;
            if (sdlKey >= 0 && sdlKey <= 127)
                return (Keys)sdlKey;
            return Keys.None;
        }

        return sdlKey switch
        {
            0x40000050 => Keys.Left,
            0x40000052 => Keys.Up,
            0x4000004F => Keys.Right,
            0x40000051 => Keys.Down,
            0x4000004A => Keys.Home,
            0x4000004D => Keys.End,
            0x4000004B => Keys.PageUp,
            0x4000004E => Keys.PageDown,
            0x40000049 => Keys.Insert,
            0x4000003A => Keys.F1,
            0x4000003B => Keys.F2,
            0x4000003C => Keys.F3,
            0x4000003D => Keys.F4,
            0x4000003E => Keys.F5,
            0x4000003F => Keys.F6,
            0x40000040 => Keys.F7,
            0x40000041 => Keys.F8,
            0x40000042 => Keys.F9,
            0x40000043 => Keys.F10,
            0x40000044 => Keys.F11,
            0x40000045 => Keys.F12,
            _ => Keys.None
        };
    }

    private static ModifierKeys MapModifierKeys(ushort sdlMod)
    {
        var modifiers = ModifierKeys.None;
        if ((sdlMod & 0x01) != 0 || (sdlMod & 0x02) != 0)
            modifiers |= ModifierKeys.Shift;
        if ((sdlMod & 0x40) != 0 || (sdlMod & 0x80) != 0)
            modifiers |= ModifierKeys.Control;
        if ((sdlMod & 0x0100) != 0 || (sdlMod & 0x0200) != 0)
            modifiers |= ModifierKeys.Alt;
        return modifiers;
    }

    private static void HandleTextInputEvent(SDL_Event e)
    {
        if (_focusedWindow == null) return;

        var bytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref e, 1));
        int len = 0;
        for (int i = 12; i < 12 + 32 && i < bytes.Length; i++)
        {
            if (bytes[i] == 0) break;
            len++;
        }
        if (len > 0)
        {
            var text = Encoding.UTF8.GetString(bytes.Slice(12, len));
            _focusedWindow.OnTextInput(text);
        }
    }

    private static void RenderForm(Form form)
    {
        if (_renderer == null) return;

        _renderer.Clear(form.BackColor);

        using var g = new Graphics();
        form.Render(g);

        foreach (var cmd in g.GetCommands())
        {
            ExecuteDrawCommand(cmd);
        }

        _renderer.Present();
    }

    private static void ExecuteDrawCommand(DrawCommand cmd)
    {
        if (_renderer == null) return;

        // Set clip region for this command
        _renderer.SetClipRect(cmd.ClipBounds);

        switch (cmd.Type)
        {
            case DrawCommandType.FillRectangle:
                _renderer.FillRectangle(cmd.Color, cmd.X, cmd.Y, cmd.Width, cmd.Height);
                break;
            case DrawCommandType.DrawRectangle:
                _renderer.DrawRectangle(cmd.Color, cmd.X, cmd.Y, cmd.Width, cmd.Height);
                break;
            case DrawCommandType.DrawLine:
                _renderer.DrawLine(cmd.Color, cmd.X, cmd.Y, cmd.X2, cmd.Y2, cmd.LineWidth);
                break;
            case DrawCommandType.DrawString:
                if (!string.IsNullOrEmpty(cmd.Text) && cmd.Font != null)
                {
                    _fontRenderer?.DrawText(cmd.Text, cmd.Font, cmd.Color, cmd.X, cmd.Y);
                }
                break;
        }
    }
}