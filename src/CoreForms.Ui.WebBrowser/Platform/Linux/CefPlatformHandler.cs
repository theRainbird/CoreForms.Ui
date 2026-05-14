using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CoreForms.Ui.WebBrowser.Controls;
using CoreForms.Ui.WebBrowser.Events;
using KeyEventArgs = CoreForms.Ui.Core.KeyEventArgs;
using MouseEventArgs = CoreForms.Ui.Core.MouseEventArgs;
using Rectangle = CoreForms.Ui.Core.Rectangle;
using Xilium.CefGlue;

namespace CoreForms.Ui.WebBrowser.Platform.Linux;

internal sealed class CefRenderHandlerImpl : CefRenderHandler
{
    private readonly CefPlatformHandler _owner;
    public CefRenderHandlerImpl(CefPlatformHandler owner) => _owner = owner;

    protected override bool GetScreenInfo(CefBrowser browser, CefScreenInfo screenInfo) => false;
    protected override CefAccessibilityHandler? GetAccessibilityHandler() => null;

    protected override void GetViewRect(CefBrowser browser, out CefRectangle rect)
    {
        rect = new CefRectangle(0, 0, Math.Max(_owner.Width, 1), Math.Max(_owner.Height, 1));
    }

    protected override void OnPaint(CefBrowser browser, CefPaintElementType type, CefRectangle[] dirtyRects, IntPtr buffer, int width, int height)
        => _owner.OnPaintBuffer(buffer, width, height);

    protected override void OnAcceleratedPaint(CefBrowser browser, CefPaintElementType type, CefRectangle[] dirtyRects, IntPtr sharedHandle) { }
    protected override void OnPopupSize(CefBrowser browser, CefRectangle rect) { }
    protected override void OnPopupShow(CefBrowser browser, bool show) { }
    protected override bool StartDragging(CefBrowser browser, CefDragData dragData, CefDragOperationsMask mask, int x, int y) => false;
    protected override void UpdateDragCursor(CefBrowser browser, CefDragOperationsMask operation) { }
    protected override void OnScrollOffsetChanged(CefBrowser browser, double x, double y) { }
    protected override void OnImeCompositionRangeChanged(CefBrowser browser, CefRange selectedRange, CefRectangle[] characterBounds) { }
    protected override void OnTextSelectionChanged(CefBrowser browser, string selectedText, CefRange selectedRange) { }
    protected override void OnVirtualKeyboardRequested(CefBrowser browser, CefTextInputMode inputMode) { }
}

internal sealed class CefLifeSpanHandlerImpl : CefLifeSpanHandler
{
    private readonly CefPlatformHandler _owner;
    public CefLifeSpanHandlerImpl(CefPlatformHandler owner) => _owner = owner;

    protected override void OnAfterCreated(CefBrowser browser) => _owner.OnAfterCreated(browser);

    protected override bool OnBeforePopup(CefBrowser browser, CefFrame frame, string targetUrl, string targetFrameName, CefWindowOpenDisposition targetDisposition, bool userGesture, CefPopupFeatures popupFeatures, CefWindowInfo windowInfo, ref CefClient client, CefBrowserSettings settings, ref CefDictionaryValue extraInfo, ref bool noJavascriptAccess)
    {
        _owner.Navigate(targetUrl);
        return true;
    }
}

internal sealed class CefLoadHandlerImpl : CefLoadHandler
{
    private readonly CefPlatformHandler _owner;
    public CefLoadHandlerImpl(CefPlatformHandler owner) => _owner = owner;

    protected override void OnLoadEnd(CefBrowser browser, CefFrame frame, int httpStatusCode)
    {
        if (frame.IsMain) _owner.OnNavigated(frame.Url);
    }

    protected override void OnLoadError(CefBrowser browser, CefFrame frame, CefErrorCode errorCode, string errorText, string failedUrl)
    {
        if (frame.IsMain) _owner.OnNavigated(failedUrl);
    }
}

internal sealed class CefRequestHandlerImpl : CefRequestHandler
{
    protected override bool OnBeforeBrowse(CefBrowser browser, CefFrame frame, CefRequest request, bool userGesture, bool isRedirect) => false;
    protected override CefResourceRequestHandler? GetResourceRequestHandler(CefBrowser browser, CefFrame frame, CefRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling) => null;
    protected override bool OnCertificateError(CefBrowser browser, CefErrorCode certError, string requestUrl, CefSslInfo sslInfo, CefCallback callback)
    {
        callback.Continue();
        return true;
    }
}

internal sealed class CefFocusHandlerImpl : CefFocusHandler
{
    private readonly CefPlatformHandler _owner;
    public CefFocusHandlerImpl(CefPlatformHandler owner) => _owner = owner;
    protected override void OnGotFocus(CefBrowser browser) { }
    protected override bool OnSetFocus(CefBrowser browser, CefFocusSource source) => false;
    protected override void OnTakeFocus(CefBrowser browser, bool next) { }
}

internal sealed class CefClientImpl : CefClient
{
    private readonly CefPlatformHandler _owner;
    private readonly CefRenderHandler _renderer;
    private readonly CefLifeSpanHandler _lifeSpan;
    private readonly CefLoadHandler _loader;
    private readonly CefRequestHandler _request;
    private readonly CefFocusHandler _focus;

    public CefClientImpl(CefPlatformHandler owner)
    {
        _owner = owner;
        _renderer = new CefRenderHandlerImpl(owner);
        _lifeSpan = new CefLifeSpanHandlerImpl(owner);
        _loader = new CefLoadHandlerImpl(owner);
        _request = new CefRequestHandlerImpl();
        _focus = new CefFocusHandlerImpl(owner);
    }

    protected override CefRenderHandler GetRenderHandler() => _renderer;
    protected override CefLifeSpanHandler GetLifeSpanHandler() => _lifeSpan;
    protected override CefLoadHandler GetLoadHandler() => _loader;
    protected override CefRequestHandler GetRequestHandler() => _request;
    protected override CefFocusHandler GetFocusHandler() => _focus;
}

internal sealed class CefBrowserProcessHandlerImpl : CefBrowserProcessHandler
{
    protected override void OnContextInitialized() =>
        Console.WriteLine("[CefPlatformHandler] CEF context initialized");
}

internal sealed class CefAppImpl : CefApp
{
    private readonly CefBrowserProcessHandler _browserProcessHandler = new CefBrowserProcessHandlerImpl();
    protected override CefBrowserProcessHandler GetBrowserProcessHandler() => _browserProcessHandler;

    protected override void OnBeforeCommandLineProcessing(string processType, CefCommandLine commandLine)
    {
        commandLine.AppendSwitch("single-process");
        commandLine.AppendSwitch("no-zygote");
        commandLine.AppendSwitch("disable-gpu");
        commandLine.AppendSwitch("enable-begin-frame-scheduling");
        commandLine.AppendSwitch("disable-extensions");
        commandLine.AppendSwitch("disable-smooth-scrolling");
        commandLine.AppendSwitch("use-gl=swiftshader");
        commandLine.AppendSwitch("enable-unsafe-swiftshader");
    }
}

public class CefPlatformHandler : IWebViewPlatformHandler
{
    private readonly WebView _webView;
    private static readonly CefAppImpl _cefApp = new();
    private static bool _cefInitialized;
    private static readonly object _cefLock = new();
    private CefBrowser? _browser;
    private CefBrowserHost? _browserHost;
    private byte[]? _pixelBuffer;
    private int _bufferWidth;
    private int _bufferHeight;
    private bool _disposed;
    private string _currentUrl = string.Empty;
    private string? _pendingUrl;
    private CoreForms.Ui.Core.MouseButtons _mouseButtons;

    public int Width { get; private set; } = 640;
    public int Height { get; private set; } = 480;

    public bool CanGoBack => _browser?.CanGoBack ?? false;
    public bool CanGoForward => _browser?.CanGoForward ?? false;
    public bool IsInitialized => _browser != null;

    public event EventHandler<WebNavigatingEventArgs>? Navigating;
    public event EventHandler<WebNavigatedEventArgs>? Navigated;

    public CefPlatformHandler(WebView webView)
    {
        _webView = webView;
    }

    public static void InitializeCef()
    {
        lock (_cefLock)
        {
            if (_cefInitialized) return;

            var mainArgs = new CefMainArgs(new string[] { });
            var exitCode = CefRuntime.ExecuteProcess(mainArgs, _cefApp, IntPtr.Zero);
            if (exitCode >= 0) Environment.Exit(exitCode);

            var settings = new CefSettings
            {
                MultiThreadedMessageLoop = false,
                NoSandbox = true,
                WindowlessRenderingEnabled = true,
                LogSeverity = CefLogSeverity.Warning,
                CachePath = Path.Combine(Path.GetTempPath(), "CoreFormsCefCache"),
                ResourcesDirPath = GetCefResourcesPath(),
                LocalesDirPath = Path.Combine(GetCefResourcesPath(), "locales")
            };

            var browserProcessPath = Path.Combine(AppContext.BaseDirectory, "CefGlueBrowserProcess", "Xilium.CefGlue.BrowserProcess");
            if (File.Exists(browserProcessPath))
                settings.BrowserSubprocessPath = browserProcessPath;

            CefRuntime.Initialize(mainArgs, settings, _cefApp, IntPtr.Zero);
            _cefInitialized = true;
            Console.WriteLine("[CefPlatformHandler] CEF initialized");
        }
    }

    private static string GetCefResourcesPath()
    {
        var baseDir = AppContext.BaseDirectory;
        var cefDir = Path.Combine(baseDir, "CefGlueBrowserProcess");
        return Directory.Exists(cefDir) ? cefDir : baseDir;
    }

    public void Initialize(uint parentWindowId)
    {
        try
        {
            InitializeCef();
            CoreForms.Ui.Platform.Platform.OnFrame += CefRuntime.DoMessageLoopWork;

            // Use the actual WebView size from the start
            Width = Math.Max(_webView.Width, 1);
            Height = Math.Max(_webView.Height, 1);

            var windowInfo = CefWindowInfo.Create();
            windowInfo.SetAsWindowless(IntPtr.Zero, false);

            var client = new CefClientImpl(this);
            var settings = new CefBrowserSettings();

            _currentUrl = "about:blank";
            CefBrowserHost.CreateBrowser(windowInfo, client, settings, _currentUrl, null);

            Console.WriteLine("[CefPlatformHandler] Browser created (OSR)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CefPlatformHandler] Init failed: {ex}");
        }
    }

    public void Navigate(string url)
    {
        _currentUrl = url;
        var args = new WebNavigatingEventArgs(url);
        Navigating?.Invoke(this, args);
        if (!args.Cancel)
        {
            if (_browser != null)
            {
                _pendingUrl = null;
                _browser.GetMainFrame()?.LoadUrl(url);
            }
            else
            {
                _pendingUrl = url;
            }
        }
    }

    public void NavigateToString(string html)
    {
        if (_browser == null) return;
        var escaped = html.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        _browser.GetMainFrame()?.ExecuteJavaScript($"document.body.innerHTML=\"{escaped}\"", string.Empty, 0);
    }

    public void GoBack() => _browser?.GoBack();
    public void GoForward() => _browser?.GoForward();
    public void Refresh() => _browser?.ReloadIgnoreCache();
    public void Stop() => _browser?.StopLoad();

    public Task<string> EvaluateScriptAsync(string script) => Task.FromResult(string.Empty);

    public void UpdateBounds(Rectangle bounds)
    {
        Width = Math.Max(bounds.Width, 1);
        Height = Math.Max(bounds.Height, 1);
        _browserHost?.WasResized();
    }

    public void SetVisible(bool visible)
    {
        if (visible) _browserHost?.WasResized();
        else _browserHost?.WasHidden(true);
    }

    public void SetEnabled(bool enabled) { }

    // ── Input Forwarding ──────────────────────────────────────────

    public void SendMouseDown(MouseEventArgs e)
    {
        if (_browserHost == null) return;
        _mouseButtons |= e.Button;
        var cefEvent = new CefMouseEvent { X = ScaleX(e.X), Y = ScaleY(e.Y), Modifiers = MapButtons(_mouseButtons) };
        _browserHost.SendMouseClickEvent(cefEvent, MapMouseButton(e.Button), false, Math.Max(e.Clicks, 1));
        _browserHost.SetFocus(true);
    }

    public void SendMouseUp(MouseEventArgs e)
    {
        if (_browserHost == null) return;
        var cefEvent = new CefMouseEvent { X = ScaleX(e.X), Y = ScaleY(e.Y), Modifiers = MapButtons(_mouseButtons) };
        _browserHost.SendMouseClickEvent(cefEvent, MapMouseButton(e.Button), true, Math.Max(e.Clicks, 1));
        _mouseButtons &= ~e.Button;
    }

    public void SendMouseMove(int x, int y)
    {
        if (_browserHost == null) return;
        var cefEvent = new CefMouseEvent { X = ScaleX(x), Y = ScaleY(y), Modifiers = MapButtons(_mouseButtons) };
        _browserHost.SendMouseMoveEvent(cefEvent, false);
    }

    public void SendMouseWheel(MouseEventArgs e)
    {
        if (_browserHost == null) return;
        var cefEvent = new CefMouseEvent { X = ScaleX(e.X), Y = ScaleY(e.Y) };
        // Smooth scrolling: accumulate small deltas and scale to WHEEL_DELTA
        _browserHost.SendMouseWheelEvent(cefEvent, 0, e.Delta * 40);
    }

    public void SendKeyDown(KeyEventArgs e)
    {
        if (_browserHost == null) return;

        var cefEvent = new CefKeyEvent
        {
            EventType = CefKeyEventType.RawKeyDown,
            WindowsKeyCode = MapKeyCode(e.KeyCode),
            NativeKeyCode = (int)e.KeyCode,
            Modifiers = MapModifiers(e.Modifiers),
            IsSystemKey = false
        };
        _browserHost.SendKeyEvent(cefEvent);
    }

    public void SendKeyUp(KeyEventArgs e)
    {
        if (_browserHost == null) return;

        var cefEvent = new CefKeyEvent
        {
            EventType = CefKeyEventType.KeyUp,
            WindowsKeyCode = MapKeyCode(e.KeyCode),
            NativeKeyCode = (int)e.KeyCode,
            Modifiers = MapModifiers(e.Modifiers),
            IsSystemKey = false
        };
        _browserHost.SendKeyEvent(cefEvent);
    }

    public void SendTextInput(string text)
    {
        if (_browserHost == null || string.IsNullOrEmpty(text)) return;

        foreach (char c in text)
        {
            var cefEvent = new CefKeyEvent
            {
                EventType = CefKeyEventType.Char,
                WindowsKeyCode = c,
                NativeKeyCode = c,
                Character = c,
                UnmodifiedCharacter = c,
                IsSystemKey = false
            };
            _browserHost.SendKeyEvent(cefEvent);
        }
    }

    // ── Internal Callbacks ────────────────────────────────────────

    internal void OnAfterCreated(CefBrowser browser)
    {
        _browser = browser;
        _browserHost = browser.GetHost();
        Console.WriteLine("[CefPlatformHandler] Browser ready");

        // Navigate to pending URL if set before browser was ready
        if (_pendingUrl != null)
        {
            var url = _pendingUrl;
            _pendingUrl = null;
            browser.GetMainFrame()?.LoadUrl(url);
        }

        _browserHost.WasResized();
        _browserHost.SetFocus(true);
    }

    internal void OnPaintBuffer(IntPtr buffer, int width, int height)
    {
        var size = width * height * 4;
        if (_pixelBuffer == null || _pixelBuffer.Length != size)
            _pixelBuffer = new byte[size];
        Marshal.Copy(buffer, _pixelBuffer, 0, size);
        _bufferWidth = width;
        _bufferHeight = height;
    }

    internal void OnNavigated(string url)
    {
        _currentUrl = url;
        Navigated?.Invoke(this, new WebNavigatedEventArgs(url, WebNavigationResult.Success));
    }

    public byte[]? GetPixelBuffer() => _pixelBuffer;
    public int BufferWidth => _bufferWidth;
    public int BufferHeight => _bufferHeight;

    // ── Helpers ───────────────────────────────────────────────────

    private int ScaleX(float x) => (int)(x * Width / (float)_webView.Width);
    private int ScaleY(float y) => (int)(y * Height / (float)_webView.Height);

    private static CefMouseButtonType MapMouseButton(CoreForms.Ui.Core.MouseButtons btn) => btn switch
    {
        CoreForms.Ui.Core.MouseButtons.Right => CefMouseButtonType.Right,
        CoreForms.Ui.Core.MouseButtons.Middle => CefMouseButtonType.Middle,
        _ => CefMouseButtonType.Left
    };

    private static CefEventFlags MapButtons(CoreForms.Ui.Core.MouseButtons btns)
    {
        var flags = CefEventFlags.None;
        if (btns.HasFlag(CoreForms.Ui.Core.MouseButtons.Left)) flags |= CefEventFlags.LeftMouseButton;
        if (btns.HasFlag(CoreForms.Ui.Core.MouseButtons.Right)) flags |= CefEventFlags.RightMouseButton;
        if (btns.HasFlag(CoreForms.Ui.Core.MouseButtons.Middle)) flags |= CefEventFlags.MiddleMouseButton;
        return flags;
    }

    private static CefEventFlags MapModifiers(CoreForms.Ui.Core.ModifierKeys mods)
    {
        var flags = CefEventFlags.None;
        if (mods.HasFlag(CoreForms.Ui.Core.ModifierKeys.Shift)) flags |= CefEventFlags.ShiftDown;
        if (mods.HasFlag(CoreForms.Ui.Core.ModifierKeys.Control)) flags |= CefEventFlags.ControlDown;
        if (mods.HasFlag(CoreForms.Ui.Core.ModifierKeys.Alt)) flags |= CefEventFlags.AltDown;
        return flags;
    }

    private static int MapKeyCode(CoreForms.Ui.Core.Keys key)
    {
        // Direct mapping for common keys — works because our Keys enum
        // follows Windows virtual-key codes which CEF also uses.
        return (int)key;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _browserHost?.CloseBrowser(true); } catch { }
        try { _browser?.Dispose(); } catch { }
        GC.SuppressFinalize(this);
    }
}
