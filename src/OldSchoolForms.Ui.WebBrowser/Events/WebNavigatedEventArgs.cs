using System;

namespace OldSchoolForms.Ui.WebBrowser.Events;

/// <summary>
/// Provides data for the WebView.Navigated event.
/// </summary>
public class WebNavigatedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the URL that was navigated to.
    /// </summary>
    public string Url { get; }

    /// <summary>
    /// Gets the result of the navigation.
    /// </summary>
    public WebNavigationResult Result { get; }

    /// <summary>
    /// Initializes a new instance of WebNavigatedEventArgs.
    /// </summary>
    /// <param name="url">The URL that was navigated to.</param>
    /// <param name="result">The navigation result.</param>
    public WebNavigatedEventArgs(string url, WebNavigationResult result)
    {
        Url = url;
        Result = result;
    }
}

/// <summary>
/// Represents the result of a web navigation.
/// </summary>
public enum WebNavigationResult
{
    /// <summary>
    /// Navigation succeeded.
    /// </summary>
    Success,

    /// <summary>
    /// Navigation was canceled.
    /// </summary>
    Canceled,

    /// <summary>
    /// Navigation failed.
    /// </summary>
    Failure,

    /// <summary>
    /// Navigation timeout.
    /// </summary>
    Timeout
}