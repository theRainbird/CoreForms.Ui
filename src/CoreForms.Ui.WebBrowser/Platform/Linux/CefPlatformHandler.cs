using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CoreForms.Ui.WebBrowser.Controls;
using CoreForms.Ui.WebBrowser.Events;
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
    {
        _owner.OnPaintBuffer(buffer, width, height);
    }

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

    protected override void OnAfterCreated(CefBrowser browser)
    {
        _owner.OnAfterCreated(browser);
    }

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
}

internal sealed class CefClientImpl : CefClient
{
    private readonly CefPlatformHandler _owner;
    private readonly CefRenderHandler _renderer;
    private readonly CefLifeSpanHandler _lifeSpan;
    private readonly CefLoadHandler _loader;
    private readonly CefRequestHandler _request;

    public CefClientImpl(CefPlatformHandler owner)
    {
        _owner = owner;
        _renderer = new CefRenderHandlerImpl(owner);
        _lifeSpan = new CefLifeSpanHandlerImpl(owner);
        _loader = new CefLoadHandlerImpl(owner);
        _request = new CefRequestHandlerImpl();
    }

    protected override CefRenderHandler GetRenderHandler() => _renderer;
    protected override CefLifeSpanHandler GetLifeSpanHandler() => _lifeSpan;
    protected override CefLoadHandler GetLoadHandler() => _loader;
    protected override CefRequestHandler GetRequestHandler() => _request;
}

internal sealed class CefBrowserProcessHandlerImpl : CefBrowserProcessHandler
{
    protected override void OnContextInitialized()
    {
        Console.WriteLine("[CefPlatformHandler] CEF context initialized");
    }
}

internal sealed class CefAppImpl : CefApp
{
    private readonly CefBrowserProcessHandler _browserProcessHandler = new CefBrowserProcessHandlerImpl();

    protected override CefBrowserProcessHandler GetBrowserProcessHandler() => _browserProcessHandler;

    protected override void OnBeforeCommandLineProcessing(string processType, CefCommandLine commandLine)
    {
        commandLine.AppendSwitch("disable-gpu");
        commandLine.AppendSwitch("disable-gpu-compositing");
        commandLine.AppendSwitch("disable-software-rasterizer");
        commandLine.AppendSwitch("enable-begin-frame-scheduling");
        commandLine.AppendSwitch("no-zygote");
        commandLine.AppendSwitch("disable-extensions");
        commandLine.AppendSwitch("disable-smooth-scrolling");
        commandLine.AppendSwitch("in-process-gpu");
    }
}

public class CefPlatformHandler : IWebViewPlatformHandler
{
    private readonly WebView _webView;
    private static readonly CefAppImpl _cefApp = new();
    private static bool _cefInitialized;
    private static readonly object _cefLock = new();
    private CefBrowser? _browser;
    private byte[]? _pixelBuffer;
    private int _bufferWidth;
    private int _bufferHeight;
    private bool _disposed;
    private string _currentUrl = string.Empty;

    public int Width { get; private set; } = 640;
    public int Height { get; private set; } = 480;

    public bool CanGoBack => false;
    public bool CanGoForward => false;
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

            CefRuntime.Initialize(mainArgs, settings, _cefApp, IntPtr.Zero);
            _cefInitialized = true;
            Console.WriteLine("[CefPlatformHandler] CEF initialized (multi-threaded mode)");
        }
    }

    private static string GetCefResourcesPath()
    {
        var baseDir = AppContext.BaseDirectory;
        var cefDir = Path.Combine(baseDir, "CefGlueBrowserProcess");
        if (Directory.Exists(cefDir)) return cefDir;
        return baseDir;
    }

    public void Initialize(uint parentWindowId)
    {
        try
        {
            InitializeCef();

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
            Console.WriteLine($"[CefPlatformHandler] Init failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    public void Navigate(string url)
    {
        _currentUrl = url;
        var args = new WebNavigatingEventArgs(url);
        Navigating?.Invoke(this, args);
        if (!args.Cancel)
        {
            _browser?.GetMainFrame()?.LoadUrl(url);
        }
    }

    public void NavigateToString(string html)
    {
        var escaped = html.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        _browser?.GetMainFrame()?.ExecuteJavaScript($"document.body.innerHTML=\"{escaped}\"", string.Empty, 0);
    }

    public void GoBack() => _browser?.GetMainFrame()?.ExecuteJavaScript("history.back()", string.Empty, 0);
    public void GoForward() => _browser?.GetMainFrame()?.ExecuteJavaScript("history.forward()", string.Empty, 0);
    public void Refresh() => _browser?.ReloadIgnoreCache();
    public void Stop() => _browser?.StopLoad();

    public Task<string> EvaluateScriptAsync(string script)
    {
        return Task.FromResult(string.Empty);
    }

    public void UpdateBounds(Rectangle bounds)
    {
        Width = Math.Max(bounds.Width, 1);
        Height = Math.Max(bounds.Height, 1);
        _browser?.GetHost()?.WasResized();
    }

    public void SetVisible(bool visible)
    {
        if (visible) _browser?.GetHost()?.WasResized();
        else _browser?.GetHost()?.WasHidden(true);
    }

    public void SetEnabled(bool enabled) { }

    internal void OnAfterCreated(CefBrowser browser)
    {
        _browser = browser;
        Console.WriteLine("[CefPlatformHandler] Browser ready");
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

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _browser?.GetHost()?.CloseBrowser(true); } catch { }
        try { _browser?.Dispose(); } catch { }
        GC.SuppressFinalize(this);
    }
}
