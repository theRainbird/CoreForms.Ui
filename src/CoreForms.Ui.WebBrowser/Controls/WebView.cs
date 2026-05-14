using System;
using System.Threading.Tasks;
using CoreForms.Ui.Core;
using CoreForms.Ui.WebBrowser.Events;
using CoreForms.Ui.WebBrowser.Platform;
using Graphics = CoreForms.Ui.Rendering.Graphics;
using Xilium.CefGlue;

namespace CoreForms.Ui.WebBrowser.Controls;

/// <summary>
/// Represents a control that displays web content from a URL or HTML string.
/// </summary>
public class WebView : Control
{
    private IWebViewPlatformHandler? _platformHandler;
    private string _source = string.Empty;
    private bool _isLoaded;
    private bool _initFailed;
    private string? _initError;
    private Rendering.PixelImage? _cachedPixelImage;

    #region Properties

    /// <summary>
    /// Gets or sets the URL to display.
    /// </summary>
    public string? Url
    {
        get => _source;
        set
        {
            if (_source != value)
            {
                _source = value ?? string.Empty;
                if (_isLoaded && _platformHandler != null)
                {
                    NavigateInternal(_source);
                }
                else if (!string.IsNullOrEmpty(_source))
                {
                    EnsureInitialized();
                    NavigateInternal(_source);
                }
            }
        }
    }

    /// <summary>
    /// Gets whether the browser can navigate back.
    /// </summary>
    public bool CanGoBack => _platformHandler?.CanGoBack ?? false;

    /// <summary>
    /// Gets whether the browser can navigate forward.
    /// </summary>
    public bool CanGoForward => _platformHandler?.CanGoForward ?? false;

    /// <summary>
    /// Gets or sets whether JavaScript is enabled.
    /// </summary>
    public bool IsScriptEnabled { get; set; } = true;

    #endregion

    #region Events

    /// <summary>
    /// Occurs before navigation.
    /// </summary>
    public event EventHandler<WebNavigatingEventArgs>? Navigating;

    /// <summary>
    /// Occurs after navigation completes.
    /// </summary>
    public event EventHandler<WebNavigatedEventArgs>? Navigated;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of WebView.
    /// </summary>
    public WebView()
    {
        TabStop = true;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Navigates to the specified URL.
    /// </summary>
    /// <param name="url">The URL to navigate to.</param>
    public void Navigate(string url)
    {
        _source = url;
        EnsureInitialized();
        if (_platformHandler != null)
        {
            NavigateInternal(url);
        }
    }

    /// <summary>
    /// Navigates to the specified HTML content.
    /// </summary>
    /// <param name="html">The HTML content to display.</param>
    public void NavigateToString(string html)
    {
        EnsureInitialized();
        _platformHandler?.NavigateToString(html);
    }

    /// <summary>
    /// Navigates to the previous page in navigation history.
    /// </summary>
    public void GoBack()
    {
        EnsureInitialized();
        _platformHandler?.GoBack();
    }

    /// <summary>
    /// Navigates to the next page in navigation history.
    /// </summary>
    public void GoForward()
    {
        EnsureInitialized();
        _platformHandler?.GoForward();
    }

    /// <summary>
    /// Reloads the current page.
    /// </summary>
    public void Refresh()
    {
        EnsureInitialized();
        _platformHandler?.Refresh();
    }

    /// <summary>
    /// Stops current navigation.
    /// </summary>
    public void Stop()
    {
        _platformHandler?.Stop();
    }

    /// <summary>
    /// Increases the browser content zoom level.
    /// </summary>
    public void ZoomIn()
    {
        if (_platformHandler is Platform.Linux.CefPlatformHandler cef)
            cef.ZoomIn();
    }

    /// <summary>
    /// Decreases the browser content zoom level.
    /// </summary>
    public void ZoomOut()
    {
        if (_platformHandler is Platform.Linux.CefPlatformHandler cef)
            cef.ZoomOut();
    }

    /// <summary>
    /// Resets the browser content zoom level to 100%.
    /// </summary>
    public void ResetZoom()
    {
        if (_platformHandler is Platform.Linux.CefPlatformHandler cef)
            cef.ResetZoom();
    }

    /// <summary>
    /// Executes JavaScript in the current page.
    /// </summary>
    /// <param name="script">The JavaScript code to execute.</param>
    /// <returns>The result of the script execution.</returns>
    public Task<string> EvaluateScriptAsync(string script)
    {
        if (_platformHandler == null)
        {
            return Task.FromResult(string.Empty);
        }
        return _platformHandler.EvaluateScriptAsync(script);
    }

    #endregion

    #region Control Lifecycle

    /// <inheritdoc/>
    protected override void OnBoundsChanged()
    {
        base.OnBoundsChanged();
        _platformHandler?.UpdateBounds(Bounds);
    }

    /// <inheritdoc/>
    public override void Render(Graphics g)
    {
        base.Render(g);

        if (_platformHandler is Platform.Linux.CefPlatformHandler cef)
        {
            var pixels = cef.GetPixelBuffer();
            Console.Write(""); // trim output
            if (pixels != null)
            {
                _cachedPixelImage?.Dispose();
                _cachedPixelImage = new Rendering.PixelImage(
                    pixels, cef.BufferWidth, cef.BufferHeight);
                Console.WriteLine($"[WebView] Draw CEF {cef.BufferWidth}x{cef.BufferHeight} -> {Width}x{Height} image={_cachedPixelImage.NativeImage != null}");
                g.DrawImage(_cachedPixelImage, 0, 0, Width, Height);
                return;
            }
        }

        if (_initFailed || _platformHandler == null)
        {
            g.FillRectangle(Color.White, 0, 0, Width, Height);
            g.DrawRectangle(Color.FromArgb(180, 180, 180), 0, 0, Width, Height, 1);

            string line1 = "WebView";
            string line2 = !string.IsNullOrEmpty(_source) ? _source : "(no URL)";
            string line3 = _initFailed ? $"Error: {_initError}" : "Platform handler not available";

            g.DrawString(line1, Font.Default, Color.FromArgb(60, 60, 60), 10, 10);
            g.DrawString(line2, Font.Default, Color.FromArgb(100, 100, 200), 10, 30);
            g.DrawString(line3, Font.Default, Color.FromArgb(200, 60, 60), 10, 50);
        }
    }

    /// <inheritdoc/>
    protected override void OnVisibleChanged()
    {
        base.OnVisibleChanged();
        _platformHandler?.SetVisible(Visible);
    }

    /// <inheritdoc/>
    protected override void OnEnabledChanged()
    {
        base.OnEnabledChanged();
        _platformHandler?.SetEnabled(Enabled);
    }

    /// <inheritdoc/>
    protected override void OnMouseDown(EventArgs e)
    {
        base.OnMouseDown(e);
        if (_platformHandler is Platform.Linux.CefPlatformHandler cef && e is MouseEventArgs me)
            cef.SendMouseDown(me);
    }

    /// <inheritdoc/>
    protected override void OnMouseUp(EventArgs e)
    {
        base.OnMouseUp(e);
        if (_platformHandler is Platform.Linux.CefPlatformHandler cef && e is MouseEventArgs me)
            cef.SendMouseUp(me);
    }

    /// <inheritdoc/>
    protected override void OnMouseMove(EventArgs e)
    {
        base.OnMouseMove(e);
        if (_platformHandler is Platform.Linux.CefPlatformHandler cef && e is MouseEventArgs me)
            cef.SendMouseMove(me.X, me.Y);
    }

    /// <inheritdoc/>
    protected override void OnMouseWheel(EventArgs e)
    {
        base.OnMouseWheel(e);
        if (_platformHandler is Platform.Linux.CefPlatformHandler cef && e is MouseEventArgs me)
        {
            if (CoreForms.Ui.Platform.Platform.GetCurrentModifiers().HasFlag(CoreForms.Ui.Core.ModifierKeys.Control))
            {
                if (me.Delta > 0) cef.ZoomIn();
                else cef.ZoomOut();
            }
            else
            {
                cef.SendMouseWheel(me);
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (_platformHandler is Platform.Linux.CefPlatformHandler cef)
        {
            cef.SendKeyDown(e);
            e.Handled = true;
        }
    }

    /// <inheritdoc/>
    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (_platformHandler is Platform.Linux.CefPlatformHandler cef)
        {
            cef.SendKeyUp(e);
            e.Handled = true;
        }
    }

    /// <inheritdoc/>
    protected override void OnTextInput(string text)
    {
        base.OnTextInput(text);
        if (_platformHandler is Platform.Linux.CefPlatformHandler cef)
            cef.SendTextInput(text);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cachedPixelImage?.Dispose();
            _cachedPixelImage = null;
            _platformHandler?.Dispose();
            _platformHandler = null;
        }
        base.Dispose(disposing);
    }

    #endregion

    #region Private Methods

    private void EnsureInitialized()
    {
        if (_isLoaded) return;

        try
        {
            _platformHandler = WebViewPlatformHandlerFactory.Create(this);

            _platformHandler.Navigating += OnPlatformNavigating;
            _platformHandler.Navigated += OnPlatformNavigated;

            var form = FindForm();
            _platformHandler.Initialize(form?.WindowId ?? 0);

            if (!_platformHandler.IsInitialized)
            {
                _initFailed = true;
                _initError = "Platform handler initialization failed (native libraries not found or incompatible)";
            }

            _isLoaded = true;
        }
        catch (Exception ex)
        {
            _initFailed = true;
            _initError = ex.Message;
            _isLoaded = true;
        }
    }

    private void NavigateInternal(string url)
    {
        var args = new WebNavigatingEventArgs(url);
        OnNavigating(args);

        if (!args.Cancel && _platformHandler != null)
        {
            _platformHandler.Navigate(url);
        }
    }

    private void OnNavigating(WebNavigatingEventArgs e)
    {
        Navigating?.Invoke(this, e);
    }

    private void OnPlatformNavigating(object? sender, WebNavigatingEventArgs e)
    {
        OnNavigating(e);
    }

    private void OnPlatformNavigated(object? sender, WebNavigatedEventArgs e)
    {
        Navigated?.Invoke(this, e);
    }

    #endregion
}