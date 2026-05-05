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
        set => _anchor = value;
    }

    public DockStyle Dock
    {
        get => _dock;
        set => _dock = value;
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

    public virtual void Invalidate()
    {
    }

    public virtual void Invalidate(Rectangle rect)
    {
    }

    public Point PointToClient(Point screenPoint)
    {
        return screenPoint;
    }

    public Point PointToScreen(Point clientPoint)
    {
        return clientPoint;
    }

    protected virtual void OnBoundsChanged()
    {
        BoundsChanged?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? BoundsChanged;

    protected virtual void OnVisibleChanged()
    {
        Invalidate();
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
    public event EventHandler? KeyDown;
    public event EventHandler? KeyPress;
    public event EventHandler? KeyUp;

    protected virtual void OnClick(EventArgs e) => Click?.Invoke(this, e);
    protected virtual void OnDoubleClick(EventArgs e) => DoubleClick?.Invoke(this, e);
    protected virtual void OnMouseEnter(EventArgs e) => MouseEnter?.Invoke(this, e);
    protected virtual void OnMouseLeave(EventArgs e) => MouseLeave?.Invoke(this, e);
    protected internal virtual void OnMouseMove(EventArgs e) => MouseMove?.Invoke(this, e);
    protected internal virtual void OnMouseDown(EventArgs e) => MouseDown?.Invoke(this, e);
    protected internal virtual void OnMouseUp(EventArgs e) => MouseUp?.Invoke(this, e);
    protected internal virtual void OnKeyDown(KeyEventArgs e) => KeyDown?.Invoke(this, e);
    protected internal virtual void OnKeyPress(KeyPressEventArgs e) => KeyPress?.Invoke(this, e);
    protected internal virtual void OnKeyUp(KeyEventArgs e) => KeyUp?.Invoke(this, e);
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
        }
    }

    public void Remove(Control control)
    {
        _controls.Remove(control);
    }

    public void Clear()
    {
        _controls.Clear();
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