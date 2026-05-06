using CoreForms.Ui.Rendering;

namespace CoreForms.Ui.Core;

public class Control : Component
{
    private string _name = string.Empty;
    private Control? _parent;
    private Rectangle _bounds;
    private Size _preferredSize;
    private bool _visible = true;
    private bool _enabled = true;
    private Color _backColor = SystemColors.Control;
    private Color _foreColor = SystemColors.ControlText;
    private Font? _font;
    private ControlCollection? _controls;
    private string _text = string.Empty;
    private AnchorStyles _anchor = AnchorStyles.Top | AnchorStyles.Left;
    private DockStyle _dock = DockStyle.None;
    private bool _focused;
    private bool _tabStop;
    private int _tabIndex;
    private bool _capturingMouse;
    private Padding _padding;
    private int _anchorRightDistance;
    private int _anchorBottomDistance;
    private int _layoutSuspendCount;
    internal bool _layoutDrivenBoundsChange;

    public string Name
    {
        get => _name;
        set => _name = value;
    }

    public Control? Parent
    {
        get => _parent;
        set
        {
            if (_parent != value)
            {
                _parent?.Controls.Remove(this);
                _parent = value;
                _parent?.Controls.Add(this);
            }
        }
    }

    public Rectangle Bounds
    {
        get => _bounds;
        set
        {
            if (_bounds.X != value.X || _bounds.Y != value.Y || _bounds.Width != value.Width || _bounds.Height != value.Height)
            {
                _bounds = value;
                OnBoundsChanged();
            }
        }
    }

    public int X
    {
        get => _bounds.X;
        set => Bounds = new Rectangle(value, _bounds.Y, _bounds.Width, _bounds.Height);
    }

    public int Y
    {
        get => _bounds.Y;
        set => Bounds = new Rectangle(_bounds.X, value, _bounds.Width, _bounds.Height);
    }

    public int Width
    {
        get => _bounds.Width;
        set => Bounds = new Rectangle(_bounds.X, _bounds.Y, value, _bounds.Height);
    }

    public int Height
    {
        get => _bounds.Height;
        set => Bounds = new Rectangle(_bounds.X, _bounds.Y, _bounds.Width, value);
    }

    public Point Location
    {
        get => new Point(_bounds.X, _bounds.Y);
        set => Bounds = new Rectangle(value.X, value.Y, _bounds.Width, _bounds.Height);
    }

    public Size Size
    {
        get => _bounds.Size;
        set => Bounds = new Rectangle(_bounds.X, _bounds.Y, value.Width, value.Height);
    }

    public Size PreferredSize
    {
        get => _preferredSize;
        set => _preferredSize = value;
    }

    public bool Visible
    {
        get => _visible;
        set
        {
            if (_visible != value)
            {
                _visible = value;
                OnVisibleChanged();
            }
        }
    }

    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled != value)
            {
                _enabled = value;
                OnEnabledChanged();
            }
        }
    }

    public Color BackColor
    {
        get => _backColor;
        set => _backColor = value;
    }

    public Color ForeColor
    {
        get => _foreColor;
        set => _foreColor = value;
    }

    public Font? Font
    {
        get => _font;
        set => _font = value;
    }

    public string Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value;
                OnTextChanged();
            }
        }
    }

    public AnchorStyles Anchor
    {
        get => _anchor;
        set
        {
            if (_anchor != value)
            {
                _anchor = value;
                UpdateAnchorDistances();
                Parent?.PerformLayout();
            }
        }
    }

    public DockStyle Dock
    {
        get => _dock;
        set
        {
            if (_dock != value)
            {
                _dock = value;
                Parent?.PerformLayout();
            }
        }
    }

    public Padding Padding
    {
        get => _padding;
        set
        {
            if (_padding.Left != value.Left || _padding.Top != value.Top ||
                _padding.Right != value.Right || _padding.Bottom != value.Bottom)
            {
                _padding = value;
                PerformLayout();
            }
        }
    }

    public Rectangle ClientRectangle => new Rectangle(
        _padding.Left, _padding.Top,
        Math.Max(0, _bounds.Width - _padding.Horizontal),
        Math.Max(0, _bounds.Height - _padding.Vertical));

    public Size ClientSize => ClientRectangle.Size;

    public bool Focused
    {
        get => _focused;
        internal set
        {
            if (_focused != value)
            {
                _focused = value;
                if (value)
                    OnGotFocus(EventArgs.Empty);
                else
                    OnLostFocus(EventArgs.Empty);
            }
        }
    }

    public bool TabStop
    {
        get => _tabStop;
        set => _tabStop = value;
    }

    public int TabIndex
    {
        get => _tabIndex;
        set => _tabIndex = value;
    }

    public bool CapturingMouse
    {
        get => _capturingMouse;
        set
        {
            if (_capturingMouse != value)
            {
                _capturingMouse = value;
                var form = FindForm();
                if (form != null)
                {
                    if (value)
                        form.CaptureControl = this;
                    else if (form.CaptureControl == this)
                        form.CaptureControl = null;
                }
            }
        }
    }

    public ControlCollection Controls => _controls ??= new ControlCollection(this);

    public virtual void Create()
    {
        foreach (Control child in Controls)
        {
            child.Create();
        }
    }

    public virtual void Render(Graphics g)
    {
        foreach (Control child in Controls)
        {
            if (child.Visible)
            {
                g.Save();
                g.TranslateTransform(child.X, child.Y);
                child.Render(g);
                g.Restore();
            }
        }
    }

    public virtual void RenderOverlay(Graphics g)
    {
        foreach (Control child in Controls)
        {
            if (child.Visible)
            {
                g.Save();
                g.TranslateTransform(child.X, child.Y);
                child.RenderOverlay(g);
                g.Restore();
            }
        }
    }

    public virtual void Invalidate()
    {
    }

    public virtual void Invalidate(Rectangle rect)
    {
    }

    public Form? FindForm()
    {
        Control? current = this;
        while (current != null)
        {
            if (current is Form form)
                return form;
            current = current.Parent;
        }
        return null;
    }

    public virtual bool HitTest(Point point)
    {
        return Bounds.Contains(point);
    }

    public Point PointToClient(Point screenPoint)
    {
        return screenPoint;
    }

    public Point PointToScreen(Point clientPoint)
    {
        return clientPoint;
    }

    public void PerformLayout()
    {
        if (_layoutSuspendCount > 0) return;
        OnLayout();
    }

    public void SuspendLayout()
    {
        _layoutSuspendCount++;
    }

    public void ResumeLayout()
    {
        ResumeLayout(true);
    }

    public void ResumeLayout(bool performLayout)
    {
        if (_layoutSuspendCount > 0)
            _layoutSuspendCount--;
        if (_layoutSuspendCount == 0 && performLayout)
            PerformLayout();
    }

    protected virtual void OnLayout()
    {
        ProcessDockAndAnchor();
    }

    internal void UpdateAnchorDistances()
    {
        if (_parent != null)
        {
            _anchorRightDistance = Math.Max(0, _parent.Width - (X + Width));
            _anchorBottomDistance = Math.Max(0, _parent.Height - (Y + Height));
        }
    }

    private void ProcessDockAndAnchor()
    {
if (_controls == null || _controls.Count == 0) return;

        ProcessDockLayout();
        ProcessAnchorLayout();
    }

    private void ProcessDockLayout()
    {
        if (_controls == null || _controls.Count == 0) return;

        int areaX = _padding.Left;
        int areaY = _padding.Top;
        int areaW = Math.Max(0, _bounds.Width - _padding.Horizontal);
        int areaH = Math.Max(0, _bounds.Height - _padding.Vertical);

        foreach (Control child in Controls)
        {
            if (child.Dock == DockStyle.None) continue;

            switch (child.Dock)
            {
                case DockStyle.Top:
                    child._layoutDrivenBoundsChange = true;
                    child.Bounds = new Rectangle(areaX, areaY, areaW, child.Height);
                    child._layoutDrivenBoundsChange = false;
                    areaY += child.Height;
                    areaH -= child.Height;
                    break;
                case DockStyle.Bottom:
                    child._layoutDrivenBoundsChange = true;
                    child.Bounds = new Rectangle(areaX, areaY + areaH - child.Height, areaW, child.Height);
                    child._layoutDrivenBoundsChange = false;
                    areaH -= child.Height;
                    break;
                case DockStyle.Left:
                    child._layoutDrivenBoundsChange = true;
                    child.Bounds = new Rectangle(areaX, areaY, child.Width, areaH);
                    child._layoutDrivenBoundsChange = false;
                    areaX += child.Width;
                    areaW -= child.Width;
                    break;
                case DockStyle.Right:
                    child._layoutDrivenBoundsChange = true;
                    child.Bounds = new Rectangle(areaX + areaW - child.Width, areaY, child.Width, areaH);
                    child._layoutDrivenBoundsChange = false;
                    areaW -= child.Width;
                    break;
                case DockStyle.Fill:
                    child._layoutDrivenBoundsChange = true;
                    child.Bounds = new Rectangle(areaX, areaY, areaW, areaH);
                    child._layoutDrivenBoundsChange = false;
                    areaX = areaY = 0;
                    areaW = areaH = 0;
                    break;
            }
        }

        if (areaH < 0) areaH = 0;
        if (areaW < 0) areaW = 0;
    }

    private void ProcessAnchorLayout()
    {
        if (_controls == null || _controls.Count == 0) return;

        int parentWidth = _bounds.Width;
        int parentHeight = _bounds.Height;

        foreach (Control child in Controls)
        {
            if (child.Dock != DockStyle.None) continue;

            var anchor = child.Anchor;
            if (anchor == (AnchorStyles.Top | AnchorStyles.Left)) continue;

            int x = child.X;
            int y = child.Y;
            int w = child.Width;
            int h = child.Height;

            bool anchorLeft = (anchor & AnchorStyles.Left) != 0;
            bool anchorRight = (anchor & AnchorStyles.Right) != 0;
            bool anchorTop = (anchor & AnchorStyles.Top) != 0;
            bool anchorBottom = (anchor & AnchorStyles.Bottom) != 0;

            if (anchorLeft && anchorRight)
            {
                w = parentWidth - x - child._anchorRightDistance;
            }
            else if (anchorRight)
            {
                x = parentWidth - child._anchorRightDistance - w;
            }

            if (anchorTop && anchorBottom)
            {
                h = parentHeight - y - child._anchorBottomDistance;
            }
            else if (anchorBottom)
            {
                y = parentHeight - child._anchorBottomDistance - h;
            }

            child._layoutDrivenBoundsChange = true;
            child.Bounds = new Rectangle(x, y, Math.Max(0, w), Math.Max(0, h));
            child._layoutDrivenBoundsChange = false;
        }
    }

    protected virtual void OnBoundsChanged()
    {
        BoundsChanged?.Invoke(this, EventArgs.Empty);
        if (!_layoutDrivenBoundsChange)
        {
            UpdateAnchorDistances();
        }
        PerformLayout();
    }

    public event EventHandler? BoundsChanged;

    protected virtual void OnVisibleChanged()
    {
        Invalidate();
        Parent?.PerformLayout();
    }

    protected virtual void OnEnabledChanged()
    {
    }

    protected virtual void OnTextChanged()
    {
    }

    public event EventHandler? Click;
    public event EventHandler? DoubleClick;
    public event EventHandler? MouseEnter;
    public event EventHandler? MouseLeave;
    public event EventHandler? MouseMove;
    public event EventHandler? MouseDown;
    public event EventHandler? MouseUp;
    public event EventHandler? MouseWheel;
    public event EventHandler? KeyDown;
    public event EventHandler? KeyPress;
    public event EventHandler? KeyUp;
    public event EventHandler? GotFocus;
    public event EventHandler? LostFocus;

    protected virtual void OnClick(EventArgs e) => Click?.Invoke(this, e);
    protected virtual void OnDoubleClick(EventArgs e) => DoubleClick?.Invoke(this, e);
    protected virtual void OnMouseEnter(EventArgs e) => MouseEnter?.Invoke(this, e);
    protected virtual void OnMouseLeave(EventArgs e) => MouseLeave?.Invoke(this, e);
    protected internal virtual void OnMouseMove(EventArgs e) => MouseMove?.Invoke(this, e);
    protected internal virtual void OnMouseDown(EventArgs e) => MouseDown?.Invoke(this, e);
    protected internal virtual void OnMouseUp(EventArgs e)
    {
        MouseUp?.Invoke(this, e);
        OnClick(EventArgs.Empty);
    }
    protected internal virtual void OnMouseWheel(EventArgs e) => MouseWheel?.Invoke(this, e);
    protected internal virtual void OnKeyDown(KeyEventArgs e) => KeyDown?.Invoke(this, e);
    protected internal virtual void OnKeyPress(KeyPressEventArgs e) => KeyPress?.Invoke(this, e);
    protected internal virtual void OnKeyUp(KeyEventArgs e) => KeyUp?.Invoke(this, e);
    protected internal virtual void OnTextInput(string text) { }
    protected internal virtual void OnGotFocus(EventArgs e) => GotFocus?.Invoke(this, e);
    protected internal virtual void OnLostFocus(EventArgs e) => LostFocus?.Invoke(this, e);
}

public class ControlCollection : IEnumerable<Control>
{
    private readonly Control _owner;
    private readonly List<Control> _controls = new();

    public ControlCollection(Control owner)
    {
        _owner = owner;
    }

    public int Count => _controls.Count;

    public Control this[int index] => _controls[index];

    public void Add(Control control)
    {
        if (!_controls.Contains(control))
        {
            _controls.Add(control);
            control.Parent = _owner;
            control.UpdateAnchorDistances();
            _owner.PerformLayout();
        }
    }

    public void Remove(Control control)
    {
        if (_controls.Remove(control))
        {
            _owner.PerformLayout();
        }
    }

    public void Clear()
    {
        _controls.Clear();
        _owner.PerformLayout();
    }

    public IEnumerator<Control> GetEnumerator() => _controls.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

[Flags]
public enum AnchorStyles
{
    None = 0,
    Top = 1,
    Bottom = 2,
    Left = 4,
    Right = 8
}

public enum DockStyle
{
    None,
    Top,
    Bottom,
    Left,
    Right,
    Fill
}