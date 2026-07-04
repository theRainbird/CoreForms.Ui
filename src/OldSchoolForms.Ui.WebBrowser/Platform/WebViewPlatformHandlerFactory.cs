using System;
using System.Runtime.InteropServices;

namespace OldSchoolForms.Ui.WebBrowser.Platform;

/// <summary>
/// Factory for creating platform-specific web view handlers.
/// </summary>
public static class WebViewPlatformHandlerFactory
{
    /// <summary>
    /// Creates a platform-appropriate web view handler.
    /// </summary>
    /// <param name="webView">The web view control.</param>
    /// <returns>A platform-specific handler.</returns>
    /// <exception cref="NotSupportedException">Thrown when platform is not supported.</exception>
    public static IWebViewPlatformHandler Create(OldSchoolForms.Ui.WebBrowser.Controls.WebView webView)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new Windows.WebView2PlatformHandler(webView);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return new Linux.CefPlatformHandler(webView);
        }
        throw new NotSupportedException("WebView is not supported on this platform. Supported: Windows, Linux");
    }
}