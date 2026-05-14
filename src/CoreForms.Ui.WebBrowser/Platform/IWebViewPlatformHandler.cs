using System;
using System.Threading.Tasks;
using CoreForms.Ui.WebBrowser.Events;
using Rectangle = CoreForms.Ui.Core.Rectangle;

namespace CoreForms.Ui.WebBrowser.Platform;

/// <summary>
/// Platform-specific handler for web view operations.
/// </summary>
public interface IWebViewPlatformHandler : IDisposable
{
    /// <summary>
    /// Gets whether the browser can navigate back.
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    /// Gets whether the browser can navigate forward.
    /// </summary>
    bool CanGoForward { get; }

    /// <summary>
    /// Occurs before navigation.
    /// </summary>
    event EventHandler<WebNavigatingEventArgs>? Navigating;

    /// <summary>
    /// Occurs after navigation completes.
    /// </summary>
    event EventHandler<WebNavigatedEventArgs>? Navigated;

    /// <summary>
    /// Gets whether the platform handler was successfully initialized.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Initializes the platform handler with the parent window ID.
    /// </summary>
    /// <param name="parentWindowId">The parent window identifier.</param>
    void Initialize(uint parentWindowId);

    /// <summary>
    /// Navigates to the specified URL.
    /// </summary>
    /// <param name="url">The URL to navigate to.</param>
    void Navigate(string url);

    /// <summary>
    /// Navigates to the specified HTML string.
    /// </summary>
    /// <param name="html">The HTML content.</param>
    void NavigateToString(string html);

    /// <summary>
    /// Navigates back in navigation history.
    /// </summary>
    void GoBack();

    /// <summary>
    /// Navigates forward in navigation history.
    /// </summary>
    void GoForward();

    /// <summary>
    /// Reloads the current page.
    /// </summary>
    void Refresh();

    /// <summary>
    /// Stops current navigation.
    /// </summary>
    void Stop();

    /// <summary>
    /// Evaluates JavaScript in the current page.
    /// </summary>
    /// <param name="script">The JavaScript to execute.</param>
    /// <returns>The result as a string.</returns>
    Task<string> EvaluateScriptAsync(string script);

    /// <summary>
    /// Updates the bounds of the web view.
    /// </summary>
    /// <param name="bounds">The new bounds.</param>
    void UpdateBounds(Rectangle bounds);

    /// <summary>
    /// Sets the visibility of the web view.
    /// </summary>
    /// <param name="visible">Whether the web view should be visible.</param>
    void SetVisible(bool visible);

    /// <summary>
    /// Sets whether the web view is enabled.
    /// </summary>
    /// <param name="enabled">Whether the web view should be enabled.</param>
    void SetEnabled(bool enabled);
}