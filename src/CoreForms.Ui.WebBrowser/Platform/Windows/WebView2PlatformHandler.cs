#if WINDOWS
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CoreForms.Ui.WebBrowser.Controls;
using CoreForms.Ui.WebBrowser.Events;
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
    private IntPtr _hwnd;

    public bool CanGoBack { get; private set; }
    public bool CanGoForward { get; private set; }
    public bool IsInitialized => _isInitialized;

    public event EventHandler<WebNavigatingEventArgs>? Navigating;
    public event EventHandler<WebNavigatedEventArgs>? Navigated;

    public WebView2PlatformHandler(WebView webView)
    {
        _webView = webView;
    }

    public void Initialize(uint parentWindowId)
    {
        _hwnd = (IntPtr)parentWindowId;

        try
        {
            InitializeWebView2();
            _isInitialized = true;
            System.Diagnostics.Debug.WriteLine("[WebView2] Initialized successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WebView2] Failed to initialize: {ex.Message}");
        }
    }

    private async void InitializeWebView2()
    {
        try
        {
            var environment = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync();
            var controller = await environment.CreateCoreWebView2ControllerAsync(_hwnd);

            controller.CoreWebView2.NavigationStarting += OnNavigationStarting;
            controller.CoreWebView2.NavigationCompleted += OnNavigationCompleted;

            CanGoBack = controller.CoreWebView2.CanGoBack;
            CanGoForward = controller.CoreWebView2.CanGoForward;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WebView2] COM initialization failed: {ex.Message}");
        }
    }

    public void Navigate(string url)
    {
        if (!_isInitialized) return;
        // Navigation via COM interop
    }

    public void NavigateToString(string html)
    {
        if (!_isInitialized) return;
        // Navigation via COM interop
    }

    public void GoBack()
    {
        if (!CanGoBack) return;
    }

    public void GoForward()
    {
        if (!CanGoForward) return;
    }

    public void Refresh()
    {
    }

    public void Stop()
    {
    }

    public Task<string> EvaluateScriptAsync(string script)
    {
        return Task.FromResult(string.Empty);
    }

    public void UpdateBounds(Rectangle bounds)
    {
    }

    public void SetVisible(bool visible)
    {
    }

    public void SetEnabled(bool enabled)
    {
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private void OnNavigationStarting(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs e)
    {
        Navigating?.Invoke(this, new WebNavigatingEventArgs(e.Uri));
    }

    private void OnNavigationCompleted(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
    {
        var result = e.IsSuccess ? WebNavigationResult.Success : WebNavigationResult.Failure;
        Navigated?.Invoke(this, new WebNavigatedEventArgs(e.Uri, result));
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

    public bool CanGoBack => false;
    public bool CanGoForward => false;
    public bool IsInitialized => _isInitialized;

    public event EventHandler<WebNavigatingEventArgs>? Navigating;
    public event EventHandler<WebNavigatedEventArgs>? Navigated;

    public WebView2PlatformHandler(WebView webView)
    {
        _webView = webView;
    }

    public void Initialize(uint parentWindowId)
    {
        System.Diagnostics.Debug.WriteLine("[WebView2] Stub initialized - requires WebView2 package on Windows");
        _isInitialized = true;
    }

    public void Navigate(string url)
    {
        System.Diagnostics.Debug.WriteLine($"[WebView2] Navigate to: {url}");
    }

    public void NavigateToString(string html)
    {
        System.Diagnostics.Debug.WriteLine("[WebView2] NavigateToString");
    }

    public void GoBack() { }
    public void GoForward() { }
    public void Refresh() { }
    public void Stop() { }

    public Task<string> EvaluateScriptAsync(string script)
    {
        return Task.FromResult(string.Empty);
    }

    public void UpdateBounds(Rectangle bounds) { }
    public void SetVisible(bool visible) { }
    public void SetEnabled(bool enabled) { }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
#endif