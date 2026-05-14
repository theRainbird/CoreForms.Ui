using System;

namespace CoreForms.Ui.WebBrowser.Events;

/// <summary>
/// Provides data for the WebView.Navigating event.
/// </summary>
public class WebNavigatingEventArgs : EventArgs
{
    /// <summary>
    /// Gets the URL being navigated to.
    /// </summary>
    public string Url { get; }

    /// <summary>
    /// Gets or sets whether to cancel the navigation.
    /// </summary>
    public bool Cancel { get; set; }

    /// <summary>
    /// Initializes a new instance of WebNavigatingEventArgs.
    /// </summary>
    /// <param name="url">The URL being navigated to.</param>
    public WebNavigatingEventArgs(string url)
    {
        Url = url;
    }
}