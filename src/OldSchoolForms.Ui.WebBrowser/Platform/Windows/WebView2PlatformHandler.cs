#if WINDOWS
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using OldSchoolForms.Ui.Core;
using OldSchoolForms.Ui.WebBrowser.Controls;
using OldSchoolForms.Ui.WebBrowser.Events;
using Microsoft.Web.WebView2.Core;
using Rectangle = OldSchoolForms.Ui.Core.Rectangle;

namespace OldSchoolForms.Ui.WebBrowser.Platform.Windows;

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
    private OldSchoolForms.Ui.Controls.Advanced.TabControl? _parentTabControl;
    private System.Threading.SynchronizationContext? _uiContext;

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
        _uiContext = System.Threading.SynchronizationContext.Current;

        try
        {
            if (_parentHwnd == IntPtr.Zero)
            {
                _initError = "WebView2: No valid parent HWND";
                return;
            }

            _ = InitializeAsync();
        }
        catch (Exception ex)
        {
            _initError = $"WebView2 init failed: {ex.Message}";
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

            Rectangle initBounds;
            if (_pendingBounds.Width > 0)
            {
                initBounds = _pendingBounds;
            }
            else
            {
                // Fallback: compute position relative to the Form's client area
                // by walking the parent chain and including render offsets.
                var form = _webView.FindForm();
                if (form != null)
                {
                    var origin = _webView.PointToScreen(OldSchoolForms.Ui.Core.Point.Empty);
                    var formOrigin = form.PointToScreen(OldSchoolForms.Ui.Core.Point.Empty);
                    initBounds = new Rectangle(
                        origin.X - formOrigin.X,
                        origin.Y - formOrigin.Y,
                        _webView.Width,
                        _webView.Height);
                }
                else
                {
                    initBounds = _webView.Bounds;
                }
            }
            float zoom = _webView.EffectiveZoom;
            _controller.Bounds = new System.Drawing.Rectangle(
                (int)(initBounds.X * zoom),
                (int)(initBounds.Y * zoom),
                Math.Max((int)(initBounds.Width * zoom), 1),
                Math.Max((int)(initBounds.Height * zoom), 1));
            _controller.IsVisible = _webView.Visible;

            SubscribeToTabChanges();

            _isInitialized = true;

            if (!string.IsNullOrEmpty(_pendingNavigationUrl))
            {
                _controller.CoreWebView2.Navigate(_pendingNavigationUrl);
                _pendingNavigationUrl = null;
            }
            else if (!string.IsNullOrEmpty(_pendingNavigationHtml))
            {
                _controller.CoreWebView2.NavigateToString(_pendingNavigationHtml);
                _pendingNavigationHtml = null;
            }
        }
        catch (Exception ex)
        {
            _initError = $"WebView2 async init failed: {ex.Message}";
        }
    }

    /// <inheritdoc/>
    public void Navigate(string url)
    {
        if (_disposed) return;

        if (_isInitialized && _controller?.CoreWebView2 != null)
        {
            _controller.CoreWebView2.Navigate(url);
        }
        else if (_initError == null)
        {
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
        // Bounds from WebView.OnBoundsChanged are relative to the WebView's parent
        // (e.g. TabPage). WebView2's child HWND is positioned relative to the form,
        // so convert to form-absolute coordinates.
        var form = _webView.FindForm();
        if (form != null)
        {
            var origin = _webView.PointToScreen(OldSchoolForms.Ui.Core.Point.Empty);
            var formOrigin = form.PointToScreen(OldSchoolForms.Ui.Core.Point.Empty);
            bounds = new Rectangle(
                origin.X - formOrigin.X,
                origin.Y - formOrigin.Y,
                bounds.Width,
                bounds.Height);
        }

        _pendingBounds = bounds;

        if (_controller != null)
        {
            float zoom = _webView.EffectiveZoom;
            _controller.Bounds = new System.Drawing.Rectangle(
                (int)(bounds.X * zoom),
                (int)(bounds.Y * zoom),
                Math.Max((int)(bounds.Width * zoom), 1),
                Math.Max((int)(bounds.Height * zoom), 1));
        }
    }

    /// <inheritdoc/>
    public void SetVisible(bool visible)
    {
        if (_controller == null) return;
        // Only show if both the WebView is visible AND its parent tab is selected
        _controller.IsVisible = visible && IsWebViewTabSelected();
    }

    private bool IsWebViewTabSelected()
    {
        if (_parentTabControl == null) return true;
        Control? current = _webView;
        while (current != null && current.Parent != _parentTabControl)
        {
            current = current.Parent;
        }
        var ourTabPage = current as OldSchoolForms.Ui.Controls.Advanced.TabPage;
        return ourTabPage != null && _parentTabControl.SelectedTab == ourTabPage;
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
            if (_parentTabControl != null)
            {
                _parentTabControl.SelectedIndexChanged -= OnTabSelectedIndexChanged;
                _parentTabControl = null;
            }
            _pendingNavigationUrl = null;
            _pendingNavigationHtml = null;
            _controller?.Close();
            _controller = null;
        }
        catch
        {
        }
    }

    private void SubscribeToTabChanges()
    {
        Control? parent = _webView.Parent;
        while (parent != null)
        {
            if (parent is OldSchoolForms.Ui.Controls.Advanced.TabControl tabControl)
            {
                _parentTabControl = tabControl;
                tabControl.SelectedIndexChanged += OnTabSelectedIndexChanged;
                // Set initial visibility based on whether our parent TabPage is selected
                UpdateVisibilityFromTab();
                break;
            }
            parent = parent.Parent;
        }
    }

    private void OnTabSelectedIndexChanged(object? sender, EventArgs e)
    {
        UpdateVisibilityFromTab();
    }

    private void UpdateVisibilityFromTab()
    {
        if (_controller == null) return;
        _controller.IsVisible = _webView.Visible && IsWebViewTabSelected();
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
using OldSchoolForms.Ui.WebBrowser.Controls;
using OldSchoolForms.Ui.WebBrowser.Events;
using Rectangle = OldSchoolForms.Ui.Core.Rectangle;

namespace OldSchoolForms.Ui.WebBrowser.Platform.Windows;

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
        _isInitialized = true;
    }

    /// <inheritdoc/>
    public void Navigate(string url)
    {
    }

    /// <inheritdoc/>
    public void NavigateToString(string html)
    {
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
