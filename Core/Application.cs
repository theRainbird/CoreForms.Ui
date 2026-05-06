using System.Collections.Concurrent;

namespace CoreForms.Ui.Core;

/// <summary>
/// Provides the entry point for the application and manages the application lifecycle.
/// Supports multiple simultaneous windows.
/// </summary>
public class Application
{
    private static Application? _instance;
    private readonly ConcurrentDictionary<IntPtr, Form> _forms = new();
    private bool _running;

    /// <summary>
    /// Gets the singleton instance of the Application class.
    /// </summary>
    public static Application Instance => _instance ??= new Application();

    /// <summary>
    /// Gets whether the application is currently running.
    /// </summary>
    public static bool Running => Instance._running;

    /// <summary>
    /// Runs the application, starting with the specified main form.
    /// </summary>
    /// <param name="mainForm">The main form to display.</param>
    /// <exception cref="InvalidOperationException">Thrown when the application is already running.</exception>
    public static void Run(Form mainForm)
    {
        Instance.RunInternal(mainForm);
    }

    /// <summary>
    /// Exits the application.
    /// </summary>
    public static void Exit()
    {
        Instance.ExitInternal();
    }

    private void RunInternal(Form mainForm)
    {
        if (_running)
            throw new InvalidOperationException("Application is already running.");

        _running = true;

        mainForm.Create();
        _forms.TryAdd(mainForm.Handle, mainForm);

        while (_running && !_forms.IsEmpty)
        {
            ProcessEvents();
            DoEvents();
        }
    }

    private void ExitInternal()
    {
        _running = false;
    }

    private void ProcessEvents()
    {
        Platform.Platform.ProcessEvents(this);
    }

    /// <summary>
    /// Registers a form with the application.
    /// Called after the form has been created and has a valid window handle.
    /// </summary>
    /// <param name="form">The form to register.</param>
    public void RegisterForm(Form form)
    {
        if (form.Handle != IntPtr.Zero)
        {
            _forms.TryAdd(form.Handle, form);
        }
    }

    /// <summary>
    /// Unregisters a form from the application.
    /// </summary>
    /// <param name="form">The form to unregister.</param>
    public void UnregisterForm(Form form)
    {
        _forms.TryRemove(form.Handle, out _);
    }

    /// <summary>
    /// Processes any pending Windows messages.
    /// </summary>
    public static void DoEvents()
    {
    }

    /// <summary>
    /// Called when the application is requested to quit.
    /// </summary>
    public void OnQuit()
    {
        _running = false;
    }
}