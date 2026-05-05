using System.Collections.Concurrent;

namespace CoreForms.Ui.Core;

public class Application
{
    private static Application? _instance;
    private readonly ConcurrentDictionary<IntPtr, Form> _forms = new();
    private bool _running;

    public static Application Instance => _instance ??= new Application();

    public static bool Running => Instance._running;

    public static void Run(Form mainForm)
    {
        Instance.RunInternal(mainForm);
    }

    public static void Exit()
    {
        Instance.ExitInternal();
    }

    private void RunInternal(Form mainForm)
    {
        if (_running)
            throw new InvalidOperationException("Application is already running.");

        _running = true;

        _forms.TryAdd(mainForm.Handle, mainForm);
        mainForm.Create();

        while (_running && !_forms.IsEmpty)
        {
            ProcessEvents();
            Application.DoEvents();
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

    public void RegisterForm(Form form)
    {
        _forms.TryAdd(form.Handle, form);
    }

    public void UnregisterForm(Form form)
    {
        _forms.TryRemove(form.Handle, out _);
    }

    public static void DoEvents()
    {
    }

    public void OnQuit()
    {
        _running = false;
    }
}