#if WINDOWS
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CoreForms.Ui.WebBrowser.Controls;
using CoreForms.Ui.WebBrowser.Events;
using Microsoft.Web.WebView2.Core;
using Rectangle = CoreForms.Ui.Core.Rectangle;

namespace CoreForms.Ui.WebBrowser.Platform.Windows;

/// <summary>
/// Windows-specific web view handler using WebView2.
/// </summary>
public class WebView2PlatformHandler : IWebViewPlatformHandler
{
    private readonly WebView _webView;
    private bool _isInitialized;
    private bool _disposed;
    private IntPtr _parentHwnd;
    private CoreWebView2Controller? _controller;
    private string? _pendingNavigationUrl;
    private string? _pendingNavigationHtml;
    private string? _initError;
    private Rectangle _pendingBounds;

    /// <inheritdoc/>
    public bool CanGoBack { get; private set; }

    /// <inheritdoc/>
    public bool CanGoForward { get; private set; }

    /// <inheritdoc/>
    public bool IsInitialized => _isInitialized;

    /// <inheritdoc/>
    public string? InitializationError => _initError;

    /// <inheritdoc/>
    public event EventHandler<WebNavigatingEventArgs>? Navigating;

    /// <inheritdoc/>
    public event EventHandler<WebNavigatedEventArgs>? Navigated;

    /// <summary>
    /// Initializes a new instance of <see cref="WebView2PlatformHandler"/>.
    /// </summary>
    /// <param name="webView">The owning WebView control.</param>
    public WebView2PlatformHandler(WebView webView)
    {
        _webView = webView;
    }

    /// <inheritdoc/>
    public void Initialize(uint parentWindowId, IntPtr nativeWindowHandle)
    {
        _parentHwnd = nativeWindowHandle;

        try
        {
            if (_parentHwnd == IntPtr.Zero)
            {
                _initError = "WebView2: No valid parent HWND";
                System.Diagnostics.Debug.WriteLine($"[WebView2] {_initError}");
                return;
            }

            _ = InitializeAsync();
        }
        catch (Exception ex)
        {
            _initError = $"WebView2 init failed: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[WebView2] {_initError}");
        }
    }

    private async Task InitializeAsync()
    {
        try
        {
            var env = await CoreWebView2Environment.CreateAsync();
            _controller = await env.CreateCoreWebView2ControllerAsync(_parentHwnd);

            _controller.CoreWebView2.NavigationStarting += OnNavigationStarting;
            _controller.CoreWebView2.NavigationCompleted += OnNavigationCompleted;

            var b = _pendingBounds.Width > 0 ? _pendingBounds : _webView.Bounds;
            _controller.Bounds = new System.Drawing.Rectangle(
                b.X, b.Y,
                Math.Max(b.Width, 1),
                Math.Max(b.Height, 1));
            _controller.IsVisible = _webView.Visible;

            _isInitialized = true;
            System.Diagnostics.Debug.WriteLine("[WebView2] Initialized successfully");

            if (!string.IsNullOrEmpty(_pendingNavigationUrl))
            {
                System.Diagnostics.Debug.WriteLine($"[WebView2] Processing pending navigate: {_pendingNavigationUrl}");
                _controller.CoreWebView2.Navigate(_pendingNavigationUrl);
                _pendingNavigationUrl = null;
            }
            else if (!string.IsNullOrEmpty(_pendingNavigationHtml))
            {
                System.Diagnostics.Debug.WriteLine($"[WebView2] Processing pending NavigateToString");
                _controller.CoreWebView2.NavigateToString(_pendingNavigationHtml);
                _pendingNavigationHtml = null;
            }
        }
        catch (Exception ex)
        {
            _initError = $"WebView2 async init failed: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[WebView2] {_initError}");
        }
    }

    /// <inheritdoc/>
    public void Navigate(string url)
    {
        if (_disposed) return;

        if (_isInitialized && _controller?.CoreWebView2 != null)
        {
            System.Diagnostics.Debug.WriteLine($"[WebView2] Navigate immediate: {url}");
            _controller.CoreWebView2.Navigate(url);
        }
        else if (_initError == null)
        {
            System.Diagnostics.Debug.WriteLine($"[WebView2] Navigate pending (init={_isInitialized}): {url}");
            _pendingNavigationUrl = url;
            _pendingNavigationHtml = null;
        }
    }

    /// <inheritdoc/>
    public void NavigateToString(string html)
    {
        if (_disposed) return;

        if (_isInitialized && _controller?.CoreWebView2 != null)
        {
            _controller.CoreWebView2.NavigateToString(html);
        }
        else if (_initError == null)
        {
            _pendingNavigationHtml = html;
            _pendingNavigationUrl = null;
        }
    }

    /// <inheritdoc/>
    public void GoBack()
    {
        if (_controller?.CoreWebView2?.CanGoBack == true)
            _controller.CoreWebView2.GoBack();
    }

    /// <inheritdoc/>
    public void GoForward()
    {
        if (_controller?.CoreWebView2?.CanGoForward == true)
            _controller.CoreWebView2.GoForward();
    }

    /// <inheritdoc/>
    public void Refresh()
    {
        _controller?.CoreWebView2?.Reload();
    }

    /// <inheritdoc/>
    public void Stop()
    {
        _controller?.CoreWebView2?.Stop();
    }

    /// <inheritdoc/>
    public Task<string> EvaluateScriptAsync(string script)
    {
        if (_controller?.CoreWebView2 == null)
            return Task.FromResult(string.Empty);

        try
        {
            return _controller.CoreWebView2.ExecuteScriptAsync(script);
        }
        catch
        {
            return Task.FromResult(string.Empty);
        }
    }

    /// <inheritdoc/>
    public void UpdateBounds(Rectangle bounds)
    {
        _pendingBounds = bounds;

        if (_controller != null)
        {
            _controller.Bounds = new System.Drawing.Rectangle(
                bounds.X, bounds.Y,
                Math.Max(bounds.Width, 1),
                Math.Max(bounds.Height, 1));
        }
    }

    /// <inheritdoc/>
    public void SetVisible(bool visible)
    {
        if (_controller != null)
            _controller.IsVisible = visible;
    }

    /// <inheritdoc/>
    public void SetEnabled(bool enabled)
    {
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            _pendingNavigationUrl = null;
            _pendingNavigationHtml = null;
            _controller?.Close();
            _controller = null;
        }
        catch
        {
        }
    }

    private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        var args = new WebNavigatingEventArgs(e.Uri);
        Navigating?.Invoke(this, args);
        e.Cancel = args.Cancel;
    }

    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        try
        {
            CanGoBack = _controller?.CoreWebView2?.CanGoBack ?? false;
            CanGoForward = _controller?.CoreWebView2?.CanGoForward ?? false;
        }
        catch
        {
        }

        var uri = _controller?.CoreWebView2?.Source ?? string.Empty;
        var result = e.IsSuccess ? WebNavigationResult.Success : WebNavigationResult.Failure;
        Navigated?.Invoke(this, new WebNavigatedEventArgs(uri, result));
    }
}
#else

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CoreForms.Ui.WebBrowser.Controls;
using CoreForms.Ui.WebBrowser.Events;
using Rectangle = CoreForms.Ui.Core.Rectangle;

namespace CoreForms.Ui.WebBrowser.Platform.Windows;

/// <summary>
/// Windows-specific web view handler using WebView2.
/// Note: This stub requires the Microsoft.Web.WebView2 NuGet package on Windows.
/// </summary>
public class WebView2PlatformHandler : IWebViewPlatformHandler
{
    private readonly WebView _webView;
    private bool _isInitialized;
    private bool _disposed;

    /// <inheritdoc/>
    public bool CanGoBack => false;

    /// <inheritdoc/>
    public bool CanGoForward => false;

    /// <inheritdoc/>
    public bool IsInitialized => _isInitialized;

    /// <inheritdoc/>
    public string? InitializationError => null;

    /// <inheritdoc/>
    public event EventHandler<WebNavigatingEventArgs>? Navigating;

    /// <inheritdoc/>
    public event EventHandler<WebNavigatedEventArgs>? Navigated;

    /// <summary>
    /// Initializes a new instance of <see cref="WebView2PlatformHandler"/>.
    /// </summary>
    /// <param name="webView">The owning WebView control.</param>
    public WebView2PlatformHandler(WebView webView)
    {
        _webView = webView;
    }

    /// <inheritdoc/>
    public void Initialize(uint parentWindowId, IntPtr nativeWindowHandle)
    {
        System.Diagnostics.Debug.WriteLine("[WebView2] Stub initialized - requires WebView2 package on Windows");
        _isInitialized = true;
    }

    /// <inheritdoc/>
    public void Navigate(string url)
    {
        System.Diagnostics.Debug.WriteLine($"[WebView2] Navigate to: {url}");
    }

    /// <inheritdoc/>
    public void NavigateToString(string html)
    {
        System.Diagnostics.Debug.WriteLine("[WebView2] NavigateToString");
    }

    /// <inheritdoc/>
    public void GoBack() { }

    /// <inheritdoc/>
    public void GoForward() { }

    /// <inheritdoc/>
    public void Refresh() { }

    /// <inheritdoc/>
    public void Stop() { }

    /// <inheritdoc/>
    public Task<string> EvaluateScriptAsync(string script)
    {
        return Task.FromResult(string.Empty);
    }

    /// <inheritdoc/>
    public void UpdateBounds(Rectangle bounds) { }

    /// <inheritdoc/>
    public void SetVisible(bool visible) { }

    /// <inheritdoc/>
    public void SetEnabled(bool enabled) { }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
#endif
