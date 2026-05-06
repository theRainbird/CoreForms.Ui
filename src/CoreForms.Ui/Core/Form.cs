using System.Drawing;

namespace CoreForms.Ui.Core;

public class Form : ContainerControl
{
    private string _title = string.Empty;
    private bool _resizable = true;
    private FormWindowState _windowState = FormWindowState.Normal;
    private bool _topMost;
    private FormBorderStyle _formBorderStyle = FormBorderStyle.Sizable;
    private IntPtr _handle;
    private Control? _captureControl;

    public IntPtr Handle => _handle;
    public uint WindowId { get; internal set; }

    public Control? CaptureControl
    {
        get => _captureControl;
        set => _captureControl = value;
    }

    public string Title
    {
        get => _title;
        set
        {
            _title = value;
            UpdateTitle();
        }
    }

    public FormWindowState WindowState
    {
        get => _windowState;
        set
        {
            if (_windowState != value)
            {
                _windowState = value;
                ApplyWindowState();
                OnWindowStateChanged();
            }
        }
    }

    public FormBorderStyle FormBorderStyle
    {
        get => _formBorderStyle;
        set => _formBorderStyle = value;
    }

    public event EventHandler? Shown;
    public event EventHandler? Resize;
    public event EventHandler? FormClosing;
    public event EventHandler<TextInputEventArgs>? TextInput;

    public override void Create()
    {
        _handle = Platform.Platform.CreateWindow(this);
        base.Create();
    }

    public void Show()
    {
        Application.Instance.RegisterForm(this);
        Create();
        OnShown(EventArgs.Empty);
    }

    public void Close()
    {
        OnFormClosing(new FormClosingEventArgs(CloseReason.UserClosing, false));
        Application.Instance.UnregisterForm(this);
        Platform.Platform.DestroyWindow(_handle);
    }

    public void Minimize()
    {
        if (_handle != IntPtr.Zero)
        {
            Platform.Platform.MinimizeWindow(_handle);
        }
    }

    public void Maximize()
    {
        if (_handle != IntPtr.Zero)
        {
            Platform.Platform.MaximizeWindow(_handle);
        }
    }

    public void Restore()
    {
        if (_handle != IntPtr.Zero)
        {
            Platform.Platform.RestoreWindow(_handle);
        }
    }

    public void BringToFront()
    {
        if (_handle != IntPtr.Zero)
        {
            Platform.Platform.BringToFront(_handle);
        }
    }

    public void Move(int x, int y)
    {
        if (_handle != IntPtr.Zero)
        {
            Platform.Platform.MoveWindow(_handle, x, y);
        }
    }

    public void SetSize(int width, int height)
    {
        if (_handle != IntPtr.Zero)
        {
            Platform.Platform.ResizeWindow(_handle, width, height);
        }
    }

    protected internal override void OnMouseDown(EventArgs e)
    {
        var args = e as MouseEventArgs;
        if (args != null && _captureControl != null)
        {
            var localArgs = new MouseEventArgs(args.Button, args.Clicks, args.X - _captureControl.X, args.Y - _captureControl.Y, args.Delta);
            _captureControl.OnMouseDown(localArgs);
            return;
        }
        base.OnMouseDown(e);
    }

    protected internal override void OnMouseUp(EventArgs e)
    {
        var args = e as MouseEventArgs;
        if (args != null && _captureControl != null)
        {
            var localArgs = new MouseEventArgs(args.Button, args.Clicks, args.X - _captureControl.X, args.Y - _captureControl.Y, args.Delta);
            _captureControl.OnMouseUp(localArgs);
            return;
        }
        base.OnMouseUp(e);
    }

    protected internal override void OnMouseMove(EventArgs e)
    {
        var args = e as MouseEventArgs;
        if (args != null && _captureControl != null)
        {
            var localArgs = new MouseEventArgs(args.Button, args.Clicks, args.X - _captureControl.X, args.Y - _captureControl.Y, args.Delta);
            _captureControl.OnMouseMove(localArgs);
            return;
        }
        base.OnMouseMove(e);
    }

    protected internal override void OnMouseWheel(EventArgs e)
    {
        if (_captureControl != null)
        {
            _captureControl.OnMouseWheel(e);
            return;
        }
        base.OnMouseWheel(e);
    }

    private List<Control> GetTabControls()
    {
        var tabs = new List<(Control control, int order)>();
        for (int i = 0; i < Controls.Count; i++)
        {
            var child = Controls[i];
            if (child.Visible && child.Enabled && child.TabStop)
            {
                tabs.Add((child, i));
            }
        }
        tabs.Sort((a, b) =>
        {
            int cmp = a.control.TabIndex.CompareTo(b.control.TabIndex);
            return cmp != 0 ? cmp : a.order.CompareTo(b.order);
        });
        return tabs.ConvertAll(t => t.control);
    }

    private void ProcessTabKey(bool shift)
    {
        var tabs = GetTabControls();
        if (tabs.Count == 0) return;

        int currentIndex = tabs.IndexOf(ActiveControl!);
        int nextIndex;

        if (currentIndex < 0)
        {
            nextIndex = shift ? tabs.Count - 1 : 0;
        }
        else if (shift)
        {
            nextIndex = currentIndex > 0 ? currentIndex - 1 : tabs.Count - 1;
        }
        else
        {
            nextIndex = currentIndex < tabs.Count - 1 ? currentIndex + 1 : 0;
        }

        ActiveControl = tabs[nextIndex];
    }

    protected internal virtual void OnShown(EventArgs e) => Shown?.Invoke(this, e);
    protected internal virtual void OnResize(EventArgs e)
    {
        PerformLayout();
        Resize?.Invoke(this, e);
    }
    protected internal virtual void OnFormClosing(FormClosingEventArgs e) => FormClosing?.Invoke(this, e);
    protected internal virtual void OnWindowStateChanged() { }

    protected internal override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
    }

    protected internal override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
    }

    protected internal override void OnTextInput(string text)
    {
        if (ActiveControl != null)
        {
            ActiveControl.OnTextInput(text);
        }
        TextInput?.Invoke(this, new TextInputEventArgs(text));
    }

    protected internal override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Tab)
        {
            ProcessTabKey(e.Modifiers.HasFlag(ModifierKeys.Shift));
            e.Handled = true;
            return;
        }

        if (ActiveControl != null)
        {
            ActiveControl.OnKeyDown(e);
            if (e.Handled) return;
        }
        base.OnKeyDown(e);
    }

    protected internal override void OnKeyUp(KeyEventArgs e)
    {
        if (ActiveControl != null)
        {
            ActiveControl.OnKeyUp(e);
            if (e.Handled) return;
        }
        base.OnKeyUp(e);
    }

    protected internal override void OnKeyPress(KeyPressEventArgs e)
    {
        if (ActiveControl != null)
        {
            ActiveControl.OnKeyPress(e);
            if (e.Handled) return;
        }
        base.OnKeyPress(e);
    }

    private void UpdateTitle()
    {
        if (_handle != IntPtr.Zero)
        {
            Platform.Platform.SetWindowTitle(_handle, _title);
        }
    }

    private void ApplyWindowState()
    {
        if (_handle == IntPtr.Zero) return;

        switch (_windowState)
        {
            case FormWindowState.Minimized:
                Platform.Platform.MinimizeWindow(_handle);
                break;
            case FormWindowState.Maximized:
                Platform.Platform.MaximizeWindow(_handle);
                break;
            case FormWindowState.Normal:
                Platform.Platform.RestoreWindow(_handle);
                break;
        }
    }

    private void UpdateBorderStyle()
    {
        if (_handle == IntPtr.Zero) return;

        Platform.Platform.SetBordered(_handle, _formBorderStyle != FormBorderStyle.None);
    }
}

public enum FormWindowState
{
    Normal,
    Minimized,
    Maximized
}

public enum FormBorderStyle
{
    None,
    FixedSingle,
    FixedDialog,
    Sizable,
    Fixed3D
}

public class FormClosingEventArgs : CancelEventArgs
{
    public CloseReason CloseReason { get; }

    public FormClosingEventArgs(CloseReason closeReason, bool cancel)
        : base(cancel)
    {
        CloseReason = closeReason;
    }
}

public enum CloseReason
{
    None,
    FormClosing,
    UserClosing,
    ApplicationExitCall,
    MdiFormClosing,
    TaskManagerClosing,
    WindowsShutDown
}