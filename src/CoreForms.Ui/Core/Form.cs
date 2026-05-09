using CoreForms.Ui.Controls.Advanced;
using CoreForms.Ui.Controls.Containers;
using System;

namespace CoreForms.Ui.Core;

/// <summary>
/// Represents a window or dialog in the application.
/// </summary>
public class Form : ContainerControl
{
    private string _title = string.Empty;
    private bool _resizable = true;
    private FormWindowState _windowState = FormWindowState.Normal;
    private FormBorderStyle _formBorderStyle = FormBorderStyle.Sizable;
    private IntPtr _handle;
    private Control? _captureControl;

    /// <summary>
    /// Gets the native window handle.
    /// </summary>
    public IntPtr Handle => _handle;

    /// <summary>
    /// Sets the native window handle. Called by the platform layer after window creation.
    /// </summary>
    /// <param name="handle">The window handle identifier.</param>
    internal void SetHandle(IntPtr handle) => _handle = handle;

    /// <summary>
    /// Gets or sets the SDL window ID.
    /// </summary>
    public uint WindowId { get; internal set; }

    /// <summary>
    /// Gets or sets the control that is capturing mouse input.
    /// </summary>
    public Control? CaptureControl
    {
        get => _captureControl;
        set => _captureControl = value;
    }

    /// <summary>
    /// Gets or sets the title displayed in the window's title bar.
    /// </summary>
    public string Title
    {
        get => _title;
        set
        {
            _title = value;
            UpdateTitle();
        }
    }

    /// <summary>
    /// Gets or sets the current window state (normal, minimized, or maximized).
    /// </summary>
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

    /// <summary>
    /// Gets or sets the border style of the form.
    /// </summary>
    public FormBorderStyle FormBorderStyle
    {
        get => _formBorderStyle;
        set => _formBorderStyle = value;
    }

    /// <summary>
    /// Occurs when the form is first shown.
    /// </summary>
    public event EventHandler? Shown;

    /// <summary>
    /// Occurs when the form is resized.
    /// </summary>
    public event EventHandler? Resize;

    /// <summary>
    /// Occurs when the form is closing.
    /// </summary>
    public event EventHandler? FormClosing;

    /// <summary>
    /// Occurs when text input is received.
    /// </summary>
    public event EventHandler<TextInputEventArgs>? TextInput;

    /// <summary>
    /// Creates the form and all its child controls.
    /// </summary>
    public override void Create()
    {
        _handle = Platform.Platform.CreateWindow(this);
        base.Create();
        Invalidate();
    }

    /// <summary>
    /// Shows the form and registers it with the application.
    /// </summary>
    public void Show()
    {
        Create();
        Application.Instance.RegisterForm(this);
        OnShown(EventArgs.Empty);
    }

    /// <summary>
    /// Closes the form.
    /// </summary>
    public void Close()
    {
        if (_handle == IntPtr.Zero)
        {
            Console.WriteLine($"[Form.Close] handle is Zero, skipping. form='{Text}'");
            return;
        }
        Console.WriteLine($"[Form.Close] form='{Text}' handle={_handle}");
        Platform.Platform.DestroyWindow(_handle);
    }

    /// <summary>
    /// Minimizes the form to the taskbar.
    /// </summary>
    public void Minimize()
    {
        if (_handle != IntPtr.Zero)
        {
            Platform.Platform.MinimizeWindow(_handle);
        }
    }

    /// <summary>
    /// Maximizes the form to fill the screen.
    /// </summary>
    public void Maximize()
    {
        if (_handle != IntPtr.Zero)
        {
            Platform.Platform.MaximizeWindow(_handle);
        }
    }

    /// <summary>
    /// Restores the form to its previous size after being minimized or maximized.
    /// </summary>
    public void Restore()
    {
        if (_handle != IntPtr.Zero)
        {
            Platform.Platform.RestoreWindow(_handle);
        }
    }

    /// <summary>
    /// Brings the form to the front of the z-order.
    /// </summary>
    public void BringToFront()
    {
        if (_handle != IntPtr.Zero)
        {
            Platform.Platform.BringToFront(_handle);
        }
    }

    /// <summary>
    /// Moves the form to the specified screen coordinates.
    /// </summary>
    /// <param name="x">The new x-coordinate.</param>
    /// <param name="y">The new y-coordinate.</param>
    public void Move(int x, int y)
    {
        if (_handle != IntPtr.Zero)
        {
            Platform.Platform.MoveWindow(_handle, x, y);
        }
    }

    /// <summary>
    /// Resizes the form to the specified dimensions.
    /// </summary>
    /// <param name="width">The new width.</param>
    /// <param name="height">The new height.</param>
    public void SetSize(int width, int height)
    {
        if (_handle != IntPtr.Zero)
        {
            Platform.Platform.ResizeWindow(_handle, width, height);
        }
    }

    /// <summary>
    /// Raises the MouseDown event for the capturing control.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseDown(EventArgs e)
    {
        if (!Enabled) return;
        var args = e as MouseEventArgs;
        if (args != null && _captureControl != null)
        {
            var localArgs = new MouseEventArgs(args.Button, args.Clicks, args.X - _captureControl.X, args.Y - _captureControl.Y, args.Delta);
            _captureControl.OnMouseDown(localArgs);
            return;
        }
        base.OnMouseDown(e);
    }

    /// <summary>
    /// Raises the MouseUp event for the capturing control.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseUp(EventArgs e)
    {
        if (!Enabled) return;
        var args = e as MouseEventArgs;
        if (args != null && _captureControl != null)
        {
            var localArgs = new MouseEventArgs(args.Button, args.Clicks, args.X - _captureControl.X, args.Y - _captureControl.Y, args.Delta);
            _captureControl.OnMouseUp(localArgs);
            return;
        }
        base.OnMouseUp(e);
    }

    /// <summary>
    /// Raises the MouseMove event for the capturing control.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseMove(EventArgs e)
    {
        if (!Enabled) return;
        var args = e as MouseEventArgs;
        if (args != null && _captureControl != null)
        {
            var localArgs = new MouseEventArgs(args.Button, args.Clicks, args.X - _captureControl.X, args.Y - _captureControl.Y, args.Delta);
            _captureControl.OnMouseMove(localArgs);
            return;
        }
        base.OnMouseMove(e);
    }

    /// <summary>
    /// Raises the MouseWheel event for the capturing control.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseWheel(EventArgs e)
    {
        if (!Enabled) return;
        if (_captureControl != null)
        {
            _captureControl.OnMouseWheel(e);
            return;
        }
        base.OnMouseWheel(e);
    }

    private List<Control> GetTabControls()
    {
        var tabs = new List<Control>();
        CollectTabControls(tabs, this);
        var names = string.Join(", ", tabs.ConvertAll(t => $"{(string.IsNullOrEmpty(t.Name) ? t.GetType().Name : t.Name)}"));
        return tabs;
    }

    private void CollectTabControls(List<Control> tabs, Control parent)
    {
        var dockFill = new List<Control>();
        var other = new List<Control>();

        for (int i = 0; i < parent.Controls.Count; i++)
        {
            var child = parent.Controls[i];
            if (!child.Visible || !child.Enabled)
                continue;

            if (child is MenuStrip)
                continue;

            if (child is TabControl tabControl)
            {
                var selectedTab = tabControl.SelectedTab;
                if (selectedTab != null && selectedTab.Visible)
                {
                    CollectTabControls(tabs, selectedTab);
                }
            }
            else if (child is TabPage)
            {
                // Skip TabPages - only selectedTab should be visited via TabControl
            }
            else if (child is ContainerControl container)
            {
                if (child.Dock == DockStyle.Fill)
                {
                    dockFill.Add(child);
                }
                else
                {
                    other.Add(child);
                }
            }
            else if (child.TabStop)
            {
                other.Add(child);
            }
        }

        foreach (var c in dockFill)
        {
            if (c.TabStop)
                tabs.Add(c);
            CollectTabControls(tabs, c);
        }

        foreach (var c in other)
        {
            if (c is ContainerControl cc)
            {
                if (cc.TabStop)
                    tabs.Add(cc);
                CollectTabControls(tabs, cc);
            }
            else if (c.TabStop)
            {
                tabs.Add(c);
            }
        }
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

        var nextControl = tabs[nextIndex];
        ActiveControl = nextControl;
        nextControl.Focused = true;

        var parent = nextControl.Parent;
        while (parent != null && parent != this)
        {
            if (parent is ContainerControl cc)
            {
                cc.ActiveControl = nextControl;
            }
            parent = parent.Parent;
        }
    }

    private bool ProcessArrowKey(Keys key)
    {
        if (ActiveControl == null) return false;

        var tabs = GetTabControls();
        if (tabs.Count == 0) return false;

        int currentIndex = tabs.IndexOf(ActiveControl);
        if (currentIndex < 0) return false;

        int nextIndex = key switch
        {
            Keys.Left or Keys.Up => currentIndex > 0 ? currentIndex - 1 : tabs.Count - 1,
            Keys.Right or Keys.Down => currentIndex < tabs.Count - 1 ? currentIndex + 1 : 0,
            _ => currentIndex
        };

        if (nextIndex != currentIndex)
        {
            ActiveControl = tabs[nextIndex];
            return true;
        }

        return false;
    }

    /// <summary>
    /// Raises the Shown event.
    /// </summary>
    /// <param name="e">An EventArgs that contains the event data.</param>
    protected internal virtual void OnShown(EventArgs e) => Shown?.Invoke(this, e);

    /// <summary>
    /// Raises the Resize event.
    /// </summary>
    /// <param name="e">An EventArgs that contains the event data.</param>
    protected internal virtual void OnResize(EventArgs e)
    {
        PerformLayout();
        Resize?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the FormClosing event.
    /// </summary>
    /// <param name="e">A FormClosingEventArgs that contains the event data.</param>
    protected internal virtual void OnFormClosing(FormClosingEventArgs e) => FormClosing?.Invoke(this, e);

    /// <summary>
    /// Called when the window state changes.
    /// </summary>
    protected internal virtual void OnWindowStateChanged() { }

    /// <summary>
    /// Raises the GotFocus event.
    /// </summary>
    /// <param name="e">An EventArgs that contains the event data.</param>
    protected internal override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
    }

    /// <summary>
    /// Raises the LostFocus event.
    /// </summary>
    /// <param name="e">An EventArgs that contains the event data.</param>
    protected internal override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
    }

    /// <summary>
    /// Raises the TextInput event, routing to the active control.
    /// </summary>
    /// <param name="text">The input text.</param>
    protected internal override void OnTextInput(string text)
    {
        if (!Enabled) return;
        if (ActiveControl != null)
        {
            ActiveControl.OnTextInput(text);
        }
        TextInput?.Invoke(this, new TextInputEventArgs(text));
    }

    /// <summary>
    /// Raises the KeyDown event, handling Tab key navigation and Alt+mnemonic activation.
    /// </summary>
    /// <param name="e">A KeyEventArgs that contains the event data.</param>
    protected internal override void OnKeyDown(KeyEventArgs e)
    {
        if (!Enabled) return;

        if (ActiveControl != null && ActiveControl.Enabled)
        {
            ActiveControl.OnKeyDown(e);
            if (e.Handled) return;
        }

        MenuStrip? activeMenu = null;
        foreach (Control control in Controls)
        {
            if (control is MenuStrip ms && ms.MenuMode)
            {
                activeMenu = ms;
                break;
            }
        }

        if (activeMenu != null)
        {
            activeMenu.OnKeyDown(e);
            if (e.Handled) return;
        }

        if (e.KeyCode == Keys.Tab)
        {
            ProcessTabKey(e.Modifiers.HasFlag(ModifierKeys.Shift));
            e.Handled = true;
            return;
        }

        if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right ||
            e.KeyCode == Keys.Up || e.KeyCode == Keys.Down)
        {
            if (ProcessArrowKey(e.KeyCode))
            {
                e.Handled = true;
                return;
            }
        }

        if (e.Modifiers.HasFlag(ModifierKeys.Alt) || e.KeyCode == Keys.Menu)
        {
            foreach (Control control in Controls)
            {
                if (control is MenuStrip menuStrip)
                {
                    if (e.KeyCode == Keys.Menu || (e.Modifiers.HasFlag(ModifierKeys.Alt) && e.KeyCode == Keys.None))
                    {
                        menuStrip.MenuMode = !menuStrip.MenuMode;
                        e.Handled = true;
                        return;
                    }

                    if (e.KeyCode != Keys.None && e.KeyCode != Keys.Menu)
                    {
                        char keyChar = (char)e.KeyCode;
                        if (menuStrip.ProcessMnemonic(keyChar))
                        {
                            e.Handled = true;
                            return;
                        }
                    }
                }
            }
        }

        base.OnKeyDown(e);
    }

    /// <summary>
    /// Raises the KeyUp event, routing to the active control.
    /// </summary>
    /// <param name="e">A KeyEventArgs that contains the event data.</param>
    protected internal override void OnKeyUp(KeyEventArgs e)
    {
        if (!Enabled) return;
        if (ActiveControl != null)
        {
            ActiveControl.OnKeyUp(e);
            if (e.Handled) return;
        }
        base.OnKeyUp(e);
    }

    /// <summary>
    /// Raises the KeyPress event, routing to the active control.
    /// </summary>
    /// <param name="e">A KeyPressEventArgs that contains the event data.</param>
    protected internal override void OnKeyPress(KeyPressEventArgs e)
    {
        if (!Enabled) return;
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

/// <summary>
/// Specifies the state of a form window.
/// </summary>
public enum FormWindowState
{
    /// <summary>
    /// A normal, resizable window.
    /// </summary>
    Normal,

    /// <summary>
    /// A minimized (minimized to taskbar) window.
    /// </summary>
    Minimized,

    /// <summary>
    /// A maximized (filling the screen) window.
    /// </summary>
    Maximized
}

/// <summary>
/// Specifies the border style of a form.
/// </summary>
public enum FormBorderStyle
{
    /// <summary>
    /// No border.
    /// </summary>
    None,

    /// <summary>
    /// A single-line border that cannot be resized.
    /// </summary>
    FixedSingle,

    /// <summary>
    /// A dialog-style border that cannot be resized.
    /// </summary>
    FixedDialog,

    /// <summary>
    /// A resizable border (default).
    /// </summary>
    Sizable,

    /// <summary>
    /// A fixed 3D border.
    /// </summary>
    Fixed3D
}

/// <summary>
/// Provides data for the FormClosing event.
/// </summary>
public class FormClosingEventArgs : CancelEventArgs
{
    /// <summary>
    /// Gets the reason for the form closing.
    /// </summary>
    public CloseReason CloseReason { get; }

    /// <summary>
    /// Initializes a new instance of FormClosingEventArgs.
    /// </summary>
    /// <param name="closeReason">The reason for closing.</param>
    /// <param name="cancel">Whether to cancel the close operation.</param>
    public FormClosingEventArgs(CloseReason closeReason, bool cancel)
        : base(cancel)
    {
        CloseReason = closeReason;
    }
}

/// <summary>
/// Specifies the reason for closing a form.
/// </summary>
public enum CloseReason
{
    /// <summary>
    /// No reason specified.
    /// </summary>
    None,

    /// <summary>
    /// The form is closing due to another form being closed.
    /// </summary>
    FormClosing,

    /// <summary>
    /// The user closed the form (e.g., clicked the close button).
    /// </summary>
    UserClosing,

    /// <summary>
    /// The application is exiting.
    /// </summary>
    ApplicationExitCall,

    /// <summary>
    /// An MDI child form is closing.
    /// </summary>
    MdiFormClosing,

    /// <summary>
    /// The Windows task manager is closing the application.
    /// </summary>
    TaskManagerClosing,

    /// <summary>
    /// Windows is shutting down.
    /// </summary>
    WindowsShutDown
}