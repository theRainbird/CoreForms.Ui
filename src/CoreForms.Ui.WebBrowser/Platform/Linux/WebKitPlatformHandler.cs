using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CoreForms.Ui.WebBrowser.Controls;
using CoreForms.Ui.WebBrowser.Events;
using Rectangle = CoreForms.Ui.Core.Rectangle;

namespace CoreForms.Ui.WebBrowser.Platform.Linux;

public class WebKitPlatformHandler : IWebViewPlatformHandler
{
    private readonly WebView _webView;
    private bool _isInitialized;
    private bool _disposed;
    private Process? _helperProcess;

    public bool CanGoBack => false;
    public bool CanGoForward => false;
    public bool IsInitialized => _isInitialized;
    public string? InitializationError => null;

    public event EventHandler<WebNavigatingEventArgs>? Navigating;
    public event EventHandler<WebNavigatedEventArgs>? Navigated;

    public WebKitPlatformHandler(WebView webView)
    {
        _webView = webView;
    }

    public void Initialize(uint parentWindowId, IntPtr nativeWindowHandle)
    {
        try
        {
            Console.WriteLine("[WebKitPlatformHandler] Initialized (helper process mode)");
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WebKitPlatformHandler] Failed to initialize: {ex.Message}");
        }
    }

    public void Navigate(string url)
    {
        if (!_isInitialized) return;

        try
        {
            Console.WriteLine($"[WebKitPlatformHandler] Opening URL: {url}");

            KillHelper();

            var helperPath = GetHelperPath();
            if (helperPath == null)
            {
                Console.Error.WriteLine("[WebKitPlatformHandler] Helper executable not found");
                Navigated?.Invoke(this, new WebNavigatedEventArgs(url, WebNavigationResult.Failure));
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = helperPath,
                Arguments = url,
                UseShellExecute = false,
                RedirectStandardError = true
            };

            _helperProcess = Process.Start(startInfo);
            if (_helperProcess == null)
            {
                Console.Error.WriteLine("[WebKitPlatformHandler] Failed to start helper process");
                Navigated?.Invoke(this, new WebNavigatedEventArgs(url, WebNavigationResult.Failure));
                return;
            }

            _helperProcess.ErrorDataReceived += (s, e) => { if (e.Data != null) Console.WriteLine($"[Helper] {e.Data}"); };
            _helperProcess.BeginErrorReadLine();

            _helperProcess.EnableRaisingEvents = true;
            _helperProcess.Exited += (s, e) =>
            {
                Console.WriteLine("[WebKitPlatformHandler] Helper process exited");
                _helperProcess = null;
            };

            Console.WriteLine($"[WebKitPlatformHandler] Helper started (PID: {_helperProcess.Id})");
            Navigated?.Invoke(this, new WebNavigatedEventArgs(url, WebNavigationResult.Success));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WebKitPlatformHandler] Failed to launch helper: {ex.Message}");
            Navigated?.Invoke(this, new WebNavigatedEventArgs(url, WebNavigationResult.Failure));
        }
    }

    public void NavigateToString(string html) { }
    public void GoBack() { }
    public void GoForward() { }
    public void Refresh() { }
    public void Stop() { KillHelper(); }

    public Task<string> EvaluateScriptAsync(string script)
        => Task.FromResult(string.Empty);

    public void UpdateBounds(Rectangle bounds) { }
    public void SetVisible(bool visible) { }
    public void SetEnabled(bool enabled) { }

    private void KillHelper()
    {
        try
        {
            if (_helperProcess != null && !_helperProcess.HasExited)
            {
                _helperProcess.Kill();
                _helperProcess.WaitForExit(1000);
                _helperProcess = null;
            }
        }
        catch { }
    }

    private static string? GetHelperPath()
    {
        var nativePath = Path.Combine(AppContext.BaseDirectory, "WebBrowserHelper");
        if (File.Exists(nativePath)) return nativePath;
        var dotnetPath = Path.Combine(AppContext.BaseDirectory, "CoreForms.Ui.WebBrowser.Helper");
        if (File.Exists(dotnetPath)) return dotnetPath;
        return null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        KillHelper();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
