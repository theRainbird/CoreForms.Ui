using System;
using System.Collections.Generic;
using OldSchoolForms.Ui.Core;

namespace OldSchoolForms.Ui.Theming;

/// <summary>
/// Manages the active theme and notifies controls when the theme changes.
/// </summary>
public static class ThemeManager
{
    private static Theme _currentTheme = new LightTheme();
    private static readonly List<WeakReference<IThemeChangeSubscriber>> _subscribers = new();

    /// <summary>
    /// Gets the currently active theme.
    /// </summary>
    public static Theme CurrentTheme => _currentTheme;

    /// <summary>
    /// Occurs when the theme is changed.
    /// </summary>
    public static event Action<Theme>? ThemeChanged;

    /// <summary>
    /// Sets the active theme and notifies all subscribers.
    /// </summary>
    /// <param name="theme">The theme to activate.</param>
    /// <exception cref="ArgumentNullException">Thrown when theme is null.</exception>
    public static void SetTheme(Theme theme)
    {
        if (theme == null)
            throw new ArgumentNullException(nameof(theme));

        if (_currentTheme == theme)
            return;

        _currentTheme = theme;
        ThemeChanged?.Invoke(theme);
        NotifySubscribers(theme);
    }

    /// <summary>
    /// Registers a subscriber to receive theme change notifications.
    /// </summary>
    /// <param name="subscriber">The subscriber to register.</param>
    internal static void Subscribe(IThemeChangeSubscriber subscriber)
    {
        if (subscriber == null)
            return;

        CleanDeadReferences();
        _subscribers.Add(new WeakReference<IThemeChangeSubscriber>(subscriber));
    }

    /// <summary>
    /// Unregisters a subscriber from theme change notifications.
    /// </summary>
    /// <param name="subscriber">The subscriber to unregister.</param>
    internal static void Unsubscribe(IThemeChangeSubscriber subscriber)
    {
        if (subscriber == null)
            return;

        for (int i = _subscribers.Count - 1; i >= 0; i--)
        {
            if (_subscribers[i].TryGetTarget(out var target) && target == subscriber)
            {
                _subscribers.RemoveAt(i);
            }
        }
    }

    private static void NotifySubscribers(Theme theme)
    {
        CleanDeadReferences();

        for (int i = _subscribers.Count - 1; i >= 0; i--)
        {
            if (_subscribers[i].TryGetTarget(out var subscriber))
            {
                try
                {
                    subscriber.OnThemeChanged(theme);
                }
                catch
                {
                    // Ignore exceptions in subscriber notifications
                }
            }
        }
    }

    private static void CleanDeadReferences()
    {
        for (int i = _subscribers.Count - 1; i >= 0; i--)
        {
            if (!_subscribers[i].TryGetTarget(out _))
            {
                _subscribers.RemoveAt(i);
            }
        }
    }
}

/// <summary>
/// Interface for objects that can receive theme change notifications.
/// </summary>
public interface IThemeChangeSubscriber
{
    /// <summary>
    /// Called when the theme changes.
    /// </summary>
    /// <param name="newTheme">The new theme that was activated.</param>
    void OnThemeChanged(Theme newTheme);
}
