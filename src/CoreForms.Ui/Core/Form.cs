using CoreForms.Ui.Controls.Advanced;
using CoreForms.Ui.Controls.Containers;
using CoreForms.Ui.Theming;
using System;

namespace CoreForms.Ui.Core;

/// <summary>
/// Represents a window or dialog in the application.
/// </summary>
public class Form : ContainerControl, IWin32Window
{
    private string _title = string.Empty;
    private bool _modal;
    private Form? _ownerForm;
    private DialogResult _dialogResult = DialogResult.None;

    private FormWindowState _windowState = FormWindowState.Normal;
    private FormBorderStyle _formBorderStyle = FormBorderStyle.Sizable;
    private IntPtr _handle;
    private Control? _captureControl;
    private float _zoom = Dpi.GetDefaultZoom();
    private bool _zoomSet;
    private bool _processingKeyDown;
    private bool _processingKeyUp;
    private bool _requiresRender = true;
    private ModifierKeys _currentModifiers;
    private Keys _lastKeyDown;
    private readonly Dictionary<Keys, KeyRepeatState> _heldKeys = new();
    private readonly Dictionary<Keys, string> _keyTextMap = new();
    private const int KeyRepeatDelay = 400;
    private const int KeyRepeatInterval = 50;
    private SystemCursorType? _cursor;

    private struct KeyRepeatState
    {
        public long FirstPressTime;
        public long LastRepeatTime;
    }

    /// <summary>
    /// Initializes a new instance of Form.
    /// </summary>
    public Form()
    {
        _backColor = ThemeManager.CurrentTheme.WindowBackground;
        _foreColor = ThemeManager.CurrentTheme.WindowText;
    }

    /// <summary>
    /// Gets the internal window identifier handle.
    /// </summary>
    public IntPtr Handle => _handle;

    /// <summary>
    /// Gets the native platform window handle (HWND on Windows, X11 Window / wl_surface on Linux).
    /// </summary>
    nint IWin32Window.Handle => Platform.Platform.GetNativeWindowHandle(this);

    /// <summary>
    /// Sets the native window handle. Called by the platform layer after window creation.
    /// </summary>
    /// <param name="handle">The window handle identifier.</param>
    internal void SetHandle(IntPtr handle) => _handle = handle;

    /// <summary>
    /// Gets or sets the native window ID.
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
    /// Gets or sets the system cursor type for this form.
    /// Set by controls during mouse event handling to request a cursor change.
    /// </summary>
    /// <summary>
    /// Gets or sets the system cursor type for this form.
    /// Set by controls during mouse event handling to request a cursor change.
    /// </summary>
    public SystemCursorType? Cursor
    {
        get => _cursor;
        set => _cursor = value;
    }

    /// <summary>
    /// Gets whether this form is displayed modally.
    /// </summary>
    public bool Modal => _modal;

    /// <summary>
    /// Gets or sets the dialog result for this form.
    /// When set to a value other than None on a modal form, the form closes.
    /// </summary>
    public DialogResult DialogResult
    {
        get => _dialogResult;
        set
        {
            _dialogResult = value;
            if (_modal && value != DialogResult.None)
                Close();
        }
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
    /// Gets or sets the zoom factor for the form and all its controls.
    /// The zoom affects all rendering (fonts, controls, images) and coordinates.
    /// Clamped between 0.25 (25%) and 4.0 (400%).
    /// </summary>
    public float Zoom
    {
        get => _zoom;
        set
        {
            _zoomSet = true;
            var oldZoom = _zoom;
            _zoom = Dpi.ClampZoom(value);
            if (Math.Abs(oldZoom - _zoom) > 0.001f)
            {
                var pixelWidth = (int)(Width * oldZoom);
                var pixelHeight = (int)(Height * oldZoom);
                SuspendLayout();
                Width = (int)MathF.Ceiling(pixelWidth / _zoom);
                Height = (int)MathF.Ceiling(pixelHeight / _zoom);
                ResumeLayout(true);
                OnResize(EventArgs.Empty);
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets the client area size of the form in actual screen pixels.
    /// This is the logical size multiplied by the zoom factor.
    /// </summary>
    public Size ClientSizePixels => new Size(
        (int)(Width * Zoom),
        (int)(Height * Zoom));

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
    /// Inherits zoom from the currently focused window if zoom was not explicitly set.
    /// </summary>
    public void Show()
    {
        if (_handle != IntPtr.Zero)
        {
            Console.WriteLine($"[Form.Show] handle already set, skipping. form='{Text}'");
            return;
        }

        if (!_zoomSet)
        {
            var activeForm = CoreForms.Ui.Platform.Platform.FocusedWindow;
            if (activeForm != null && activeForm != this)
                _zoom = activeForm.Zoom;
        }

        Create();
        Application.Instance.RegisterForm(this);
        OnShown(EventArgs.Empty);

        // Force an initial render so the window appears and is responsive
        Invalidate();
    }

    /// <summary>
    /// Shows the form as a modal dialog with no owner.
    /// </summary>
    /// <returns>One of the DialogResult values.</returns>
    public DialogResult ShowDialog()
    {
        return ShowDialog(owner: null);
    }

    /// <summary>
    /// Shows the form as a modal dialog with the specified owner.
    /// Blocks until the dialog is closed, running a nested message loop.
    /// </summary>
    /// <param name="owner">The window that owns this dialog, or null.</param>
    /// <returns>One of the DialogResult values.</returns>
    public DialogResult ShowDialog(IWin32Window? owner)
    {
        if (_handle != IntPtr.Zero)
            return _dialogResult;

        if (owner != null)
        {
            foreach (var f in Application.Instance.GetForms())
            {
                if (f is IWin32Window w && w.Handle == owner.Handle)
                {
                    _ownerForm = f;
                    if (!_zoomSet)
                        _zoom = f.Zoom;
                    break;
                }
            }
        }
        else
        {
            _ownerForm = Application.Instance.GetForms().LastOrDefault(f => f != this && f.Handle != IntPtr.Zero);
            if (!_zoomSet && _ownerForm != null)
                _zoom = _ownerForm.Zoom;
        }

        if (_ownerForm != null && _ownerForm != this)
            _ownerForm.Enabled = false;

        _modal = true;
        _dialogResult = DialogResult.None;

        Create();
        Application.Instance.RegisterForm(this);
        OnShown(EventArgs.Empty);

        while (_modal && Application.Running)
        {
            Platform.Platform.ProcessEvents(Application.Instance);
            if (Handle == IntPtr.Zero)
            {
                if (_dialogResult == DialogResult.None)
                    _dialogResult = DialogResult.Cancel;
                _modal = false;
            }
        }

        if (_ownerForm != null && _ownerForm != this && _ownerForm.Handle != IntPtr.Zero)
        {
            _ownerForm.Enabled = true;
            Platform.Platform.BringToFront(_ownerForm.Handle);
        }

        _ownerForm = null;
        return _dialogResult;
    }

    /// <summary>
    /// Gets whether the form needs to be re-rendered.
    /// Returns true if any control on the form has been invalidated since the last render.
    /// </summary>
    internal bool RequiresRender => _requiresRender || HasDirtyDescendant();

    /// <summary>
    /// Invalidates the entire form, forcing a full re-render on the next frame.
    /// </summary>
    public override void Invalidate()
    {
        _requiresRender = true;
        base.Invalidate();
    }

    /// <summary>
    /// Clears the render-required flag after rendering is complete.
    /// </summary>
    internal void ClearRenderFlag()
    {
        _requiresRender = false;
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

        if (_modal && _dialogResult == DialogResult.None)
            _dialogResult = DialogResult.Cancel;

        _modal = false;
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
    /// <param name="width">The new logical width.</param>
    /// <param name="height">The new logical height.</param>
    public void SetSize(int width, int height)
    {
        if (_handle != IntPtr.Zero)
        {
            Platform.Platform.ResizeWindow(_handle, (int)(width * Zoom), (int)(height * Zoom));
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
            var localPoint = _captureControl.PointToClient(new Point(args.X, args.Y));
            var localArgs = new MouseEventArgs(args.Button, args.Clicks, localPoint.X, localPoint.Y, args.Delta);
            _captureControl.OnMouseDown(localArgs);
            return;
        }

        var modalOverlay = GetVisibleModalOverlay();
        if (modalOverlay != null)
        {
            modalOverlay.OnMouseDown(e);
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
            var localPoint = _captureControl.PointToClient(new Point(args.X, args.Y));
            var localArgs = new MouseEventArgs(args.Button, args.Clicks, localPoint.X, localPoint.Y, args.Delta);
            _captureControl.OnMouseUp(localArgs);
            return;
        }

        var modalOverlay = GetVisibleModalOverlay();
        if (modalOverlay != null)
        {
            modalOverlay.OnMouseUp(e);
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
            var localPoint = _captureControl.PointToClient(new Point(args.X, args.Y));
            var localArgs = new MouseEventArgs(args.Button, args.Clicks, localPoint.X, localPoint.Y, args.Delta);
            _captureControl.OnMouseMove(localArgs);
            return;
        }

        var modalOverlay = GetVisibleModalOverlay();
        if (modalOverlay != null)
        {
            modalOverlay.OnMouseMove(e);
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

        var modalOverlay = GetVisibleModalOverlay();
        if (modalOverlay != null)
        {
            modalOverlay.OnMouseWheel(e);
            return;
        }

        base.OnMouseWheel(e);
    }

    private Control? GetVisibleModalOverlay()
    {
        for (int i = Controls.Count - 1; i >= 0; i--)
        {
            var child = Controls[i];
            if (child.Visible && child is Controls.Advanced.MessageBoxOverlay)
            {
                return child;
            }
        }
        return null;
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
    /// Called when the theme changes. Propagates the theme change to all child controls.
    /// </summary>
    /// <param name="newTheme">The new theme that was activated.</param>
    public override void OnThemeChanged(Theme newTheme)
    {
        if (!_backColorSet)
            _backColor = newTheme.WindowBackground;
        if (!_foreColorSet)
            _foreColor = newTheme.WindowText;
        PropagateThemeChange(this, newTheme);
        Invalidate();
    }

    private static void PropagateThemeChange(Control parent, Theme theme)
    {
        foreach (Control child in parent.Controls)
        {
            child.OnThemeChanged(theme);
            if (child is ContainerControl container)
            {
                PropagateThemeChange(container, theme);
            }
        }
    }

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
        ClearHeldKeys();
        base.OnLostFocus(e);
    }

    /// <summary>
    /// Processes key repeat events for held keys. Called from the application main loop.
    /// Generates synthetic KeyDown and TextInput events for keys held beyond the repeat delay.
    /// </summary>
    public void ProcessKeyRepeat()
    {
        if (_heldKeys.Count == 0) return;

        long now = Environment.TickCount;

        foreach (var kvp in _heldKeys)
        {
            var key = kvp.Key;
            var state = kvp.Value;
            long elapsed = now - state.FirstPressTime;

            if (elapsed < KeyRepeatDelay)
                continue;

            long sinceLastRepeat = now - state.LastRepeatTime;
            if (sinceLastRepeat >= KeyRepeatInterval)
            {
                state.LastRepeatTime = now;
                _heldKeys[key] = state;

                var keyArgs = new KeyEventArgs
                {
                    KeyCode = key,
                    Modifiers = _currentModifiers
                };
                OnKeyDown(keyArgs);

                if (_keyTextMap.TryGetValue(key, out var text))
                {
                    OnTextInput(text);
                }
            }
        }
    }

    private void ClearHeldKeys()
    {
        _heldKeys.Clear();
        _keyTextMap.Clear();
    }

    /// <summary>
    /// Raises the TextInput event, routing to the active control.
    /// </summary>
    /// <param name="text">The input text.</param>
    protected internal override void OnTextInput(string text)
    {
        if (!Enabled) return;
        if (!string.IsNullOrEmpty(text) && _lastKeyDown != Keys.None)
        {
            _keyTextMap[_lastKeyDown] = text;
        }
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
    internal bool IsProcessingKeyDown => _processingKeyDown;
    internal void SetProcessingKeyDown(bool value) => _processingKeyDown = value;

    protected internal override void OnKeyDown(KeyEventArgs e)
    {
        _currentModifiers = e.Modifiers;
        if (!Enabled || _processingKeyDown) return;

        if (!_heldKeys.ContainsKey(e.KeyCode))
        {
            _heldKeys[e.KeyCode] = new KeyRepeatState
            {
                FirstPressTime = Environment.TickCount,
                LastRepeatTime = Environment.TickCount
            };
        }
        _lastKeyDown = e.KeyCode;

        _processingKeyDown = true;
        try
        {

        var modalOverlay = GetVisibleModalOverlay();
        if (modalOverlay != null)
        {
            modalOverlay.OnKeyDown(e);
            return;
        }

        if (ActiveControl != null && ActiveControl.Enabled)
        {
            ActiveControl.OnKeyDown(e);
            if (e.Handled) return;
        }

        if (e.Modifiers.HasFlag(ModifierKeys.Control) && !e.Handled)
        {
            switch (e.KeyCode)
            {
                case Keys.C:
                    if (ActiveControl != null)
                    {
                        var method = ActiveControl.GetType().GetMethod("CopyToClipboard");
                        method?.Invoke(ActiveControl, null);
                    }
                    e.Handled = true;
                    return;
                case Keys.X:
                    if (ActiveControl is Controls.Basic.TextBox tb)
                    {
                        tb.Cut();
                        e.Handled = true;
                    }
                    return;
                case Keys.V:
                    if (ActiveControl is Controls.Basic.TextBox tb2)
                    {
                        tb2.Paste();
                        e.Handled = true;
                    }
                    return;
            }
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
        finally
        {
            _processingKeyDown = false;
        }
    }

    /// <summary>
    /// Raises the KeyUp event, routing to the active control.
    /// </summary>
    /// <param name="e">A KeyEventArgs that contains the event data.</param>
    protected internal override void OnKeyUp(KeyEventArgs e)
    {
        _currentModifiers = e.Modifiers;
        _heldKeys.Remove(e.KeyCode);
        _keyTextMap.Remove(e.KeyCode);

        if (!Enabled || _processingKeyUp) return;
        _processingKeyUp = true;
        try
        {
            if (ActiveControl != null)
            {
                ActiveControl.OnKeyUp(e);
                if (e.Handled) return;
            }
            base.OnKeyUp(e);
        }
        finally
        {
            _processingKeyUp = false;
        }
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