using System;
using System.Collections.Generic;
using System.ComponentModel;
using CoreForms.Ui.Data;
using CoreForms.Ui.Rendering;
using CoreForms.Ui.Theming;

namespace CoreForms.Ui.Core;

/// <summary>
/// Base class for all UI controls, providing properties, methods, and events for visual elements.
/// </summary>
public class Control : Component, IThemeChangeSubscriber, INotifyPropertyChanged
{
    private string _name = string.Empty;
    private Control? _parent;
    private Rectangle _bounds;
    private Size _preferredSize;
    private bool _visible = true;
    private bool _enabled = true;
    protected Color _backColor;
    protected Color _foreColor;
    private Font? _font;
    protected bool _backColorSet;
    protected bool _foreColorSet;
    private ControlCollection? _controls;
    private string _text = string.Empty;
    private AnchorStyles _anchor = AnchorStyles.Top | AnchorStyles.Left;
    private DockStyle _dock = DockStyle.None;
    private bool _focused;
    private bool _tabStop;
    private int _tabIndex;
    private bool _dirty;
    private bool _capturingMouse;
    private Padding _padding;
    private int _anchorRightDistance;
    private int _anchorBottomDistance;
    private int _layoutSuspendCount;
    internal bool _layoutDrivenBoundsChange;
    private ControlState _state = ControlState.None;
    private Form? _cachedForm;

    /// <summary>
    /// Initializes a new instance of Control.
    /// </summary>
    public Control()
    {
        var theme = ThemeManager.CurrentTheme;
        _backColor = theme.ControlBackground;
        _foreColor = theme.ControlText;
        ThemeManager.Subscribe(this);
    }

    /// <summary>
    /// Gets the effective zoom factor for this control.
    /// Returns the zoom from the parent form, or 1.0 if no form is found.
    /// </summary>
    public float EffectiveZoom
    {
        get
        {
            var form = FindForm();
            return form?.Zoom ?? 1.0f;
        }
    }

    /// <summary>
    /// Gets the current visual state of the control (hovered, pressed, focused).
    /// </summary>
    protected ControlState State
    {
        get => _state;
        private set
        {
            if (_state != value)
            {
                _state = value;
                OnStateChanged();
            }
        }
    }

    /// <summary>
    /// Gets whether the control is currently hovered.
    /// </summary>
    protected bool IsHovered => (_state & ControlState.Hovered) != 0;

    /// <summary>
    /// Gets whether the control has a mouse button pressed.
    /// </summary>
    protected bool IsPressed => (_state & ControlState.Pressed) != 0;

    /// <summary>
    /// Gets or sets whether the control is allowed to show focus cues.
    /// </summary>
    protected virtual bool ShowFocusCues => true;

    /// <summary>
    /// Called when the control's state changes (hover, pressed, focus).
    /// </summary>
    protected virtual void OnStateChanged()
    {
    }

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises the PropertyChanged event for the specified property.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private ControlBindingsCollection? _dataBindings;

    /// <summary>
    /// Gets the collection of data bindings for this control.
    /// </summary>
    public ControlBindingsCollection DataBindings => _dataBindings ??= new ControlBindingsCollection(this);

    /// <summary>
    /// Gets or sets the name of the control.
    /// </summary>
    public string Name
    {
        get => _name;
        set => _name = value;
    }

    /// <summary>
    /// Gets or sets the parent control of this control.
    /// </summary>
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
                InvalidateCachedForm();
            }
        }
    }

    private void InvalidateCachedForm()
    {
        _cachedForm = null;
        for (int i = 0; _controls != null && i < _controls.Count; i++)
        {
            _controls[i].InvalidateCachedForm();
        }
    }

    /// <summary>
    /// Gets or sets the bounds (position and size) of the control.
    /// </summary>
    public Rectangle Bounds
    {
        get => _bounds;
        set
        {
            if (_bounds.X != value.X || _bounds.Y != value.Y || _bounds.Width != value.Width || _bounds.Height != value.Height)
            {
                _bounds = value;
                OnBoundsChanged();
                OnPropertyChanged(nameof(Bounds));
            }
        }
    }

    /// <summary>
    /// Gets or sets the x-coordinate of the control's left edge.
    /// </summary>
    public int X
    {
        get => _bounds.X;
        set => Bounds = new Rectangle(value, _bounds.Y, _bounds.Width, _bounds.Height);
    }

    /// <summary>
    /// Gets or sets the y-coordinate of the control's top edge.
    /// </summary>
    public int Y
    {
        get => _bounds.Y;
        set => Bounds = new Rectangle(_bounds.X, value, _bounds.Width, _bounds.Height);
    }

    /// <summary>
    /// Gets or sets the width of the control.
    /// </summary>
    public int Width
    {
        get => _bounds.Width;
        set => Bounds = new Rectangle(_bounds.X, _bounds.Y, value, _bounds.Height);
    }

    /// <summary>
    /// Gets or sets the height of the control.
    /// </summary>
    public int Height
    {
        get => _bounds.Height;
        set => Bounds = new Rectangle(_bounds.X, _bounds.Y, _bounds.Width, value);
    }

    /// <summary>
    /// Gets or sets the location (x and y coordinates) of the control's upper-left corner.
    /// </summary>
    public Point Location
    {
        get => new Point(_bounds.X, _bounds.Y);
        set => Bounds = new Rectangle(value.X, value.Y, _bounds.Width, _bounds.Height);
    }

    /// <summary>
    /// Gets or sets the size (width and height) of the control.
    /// </summary>
    public Size Size
    {
        get => _bounds.Size;
        set => Bounds = new Rectangle(_bounds.X, _bounds.Y, value.Width, value.Height);
    }

    /// <summary>
    /// Gets or sets the preferred size of the control.
    /// </summary>
    public Size PreferredSize
    {
        get => _preferredSize;
        set => _preferredSize = value;
    }

    /// <summary>
    /// Gets or sets whether the control is visible.
    /// </summary>
    public bool Visible
    {
        get => _visible;
        set
        {
            if (_visible != value)
            {
                _visible = value;
                OnVisibleChanged();
                OnPropertyChanged(nameof(Visible));
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the control can respond to user interaction.
    /// </summary>
    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled != value)
            {
                _enabled = value;
                OnEnabledChanged();
                OnPropertyChanged(nameof(Enabled));
            }
        }
    }

    /// <summary>
    /// Gets or sets the background color of the control.
    /// Individual color settings take precedence over theme colors.
    /// </summary>
    public Color BackColor
    {
        get => _backColor;
        set
        {
            if (_backColor != value)
            {
                _backColor = value;
                _backColorSet = true;
                OnPropertyChanged(nameof(BackColor));
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets the foreground color of the control.
    /// Individual color settings take precedence over theme colors.
    /// </summary>
    public Color ForeColor
    {
        get => _foreColor;
        set
        {
            if (_foreColor != value)
            {
                _foreColor = value;
                _foreColorSet = true;
                OnPropertyChanged(nameof(ForeColor));
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets the font used by the control.
    /// If not set, the font is inherited from the parent control or the active theme.
    /// </summary>
    public Font? Font
    {
        get => _font;
        set
        {
            if (_font != value)
            {
                _font = value;
                OnPropertyChanged(nameof(Font));
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets the effective font, traversing up the parent chain if not set.
    /// Falls back to the active theme's default font.
    /// </summary>
    public Font EffectiveFont
    {
        get
        {
            if (_font != null)
                return _font;
            if (_parent != null)
                return _parent.EffectiveFont;
            return ThemeManager.CurrentTheme.DefaultFont;
        }
    }

    /// <summary>
    /// Gets whether the control is opaque (fully covers the area behind it).
    /// A control is considered opaque if its background color has full alpha (A == 255).
    /// Controls that draw custom opaque content should override this property.
    /// </summary>
    public virtual bool IsOpaque => _backColor.A == 255;

    /// <summary>
    /// Gets or sets the text displayed by the control.
    /// </summary>
    public string Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value;
                OnTextChanged();
                OnPropertyChanged(nameof(Text));
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets which edges of the control are anchored to its parent.
    /// </summary>
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

    /// <summary>
    /// Gets or sets which edge of the parent container the control is docked to.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the padding within the control.
    /// </summary>
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

    /// <summary>
    /// Gets the client rectangle of the control (bounds minus padding).
    /// </summary>
    public Rectangle ClientRectangle => new Rectangle(
        _padding.Left, _padding.Top,
        Math.Max(0, _bounds.Width - _padding.Horizontal),
        Math.Max(0, _bounds.Height - _padding.Vertical));

    /// <summary>
    /// Gets the client size of the control.
    /// </summary>
    public Size ClientSize => ClientRectangle.Size;

    /// <summary>
    /// Gets whether the control currently has input focus.
    /// </summary>
    public bool Focused
    {
        get => _focused;
        internal set
        {
if (_focused != value)
        {
            if (value)
            {
                if (this is Form) return;
                var form = FindForm();
                if (form != null)
                {
                    ClearFocusRecursive(form, this);
                    form.ActiveControl = this;
                    ClearToolStripTextBoxFocus(form, this);
                }
            }
            _focused = value;
            if (value)
            {
                OnGotFocus(EventArgs.Empty);
                Invalidate();
            }
            else
                OnLostFocus(EventArgs.Empty);
        }
        }
    }

    private static void ClearFocusRecursive(Control parent, Control? except)
    {
        for (int i = 0; i < parent.Controls.Count; i++)
        {
            var child = parent.Controls[i];
            if (child != except && child._focused)
            {
                child._focused = false;
                child.OnLostFocus(EventArgs.Empty);
            }
            if (child is ContainerControl container)
            {
                ClearFocusRecursive(container, except);
            }
        }
    }

    private static void ClearToolStripTextBoxFocus(Control parent, Control except)
    {
        for (int i = 0; i < parent.Controls.Count; i++)
        {
            var child = parent.Controls[i];
            if (child != except && child is Controls.Containers.ToolStrip toolStrip)
            {
                toolStrip.ClearTextBoxFocus();
            }
            if (child is ContainerControl container)
            {
                ClearToolStripTextBoxFocus(container, except);
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the control can receive focus via tab navigation.
    /// </summary>
    public bool TabStop
    {
        get => _tabStop;
        set => _tabStop = value;
    }

    /// <summary>
    /// Gets or sets the index in the tab order of the control.
    /// </summary>
    public int TabIndex
    {
        get => _tabIndex;
        set => _tabIndex = value;
    }

    /// <summary>
    /// Gets or sets whether the control is capturing mouse input.
    /// </summary>
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

    /// <summary>
    /// Processes a mnemonic key character, activating the appropriate control.
    /// Override this to handle mnemonic keys (e.g., Alt+F for File menu).
    /// </summary>
    /// <param name="charCode">The character code of the pressed key.</param>
    /// <returns>True if the mnemonic was processed; otherwise, false.</returns>
    public virtual bool ProcessMnemonic(char charCode)
    {
        return false;
    }

    /// <summary>
    /// Gets the collection of child controls.
    /// </summary>
    public ControlCollection Controls => _controls ??= new ControlCollection(this);

    /// <summary>
    /// Creates the control and all its child controls.
    /// </summary>
    public virtual void Create()
    {
        foreach (Control child in Controls)
        {
            child.Create();
        }
    }

    /// <summary>
    /// Determines whether the specified child control is completely occluded by opaque siblings
    /// that are drawn later (higher Z-order). A fully occluded child can be skipped during rendering.
    /// </summary>
    /// <param name="child">The child control to test.</param>
    /// <param name="childIndex">The index of the child in the Controls collection.</param>
    /// <returns>True if the child is fully occluded by later siblings; otherwise, false.</returns>
    private bool IsCompletelyOccluded(Control child, int childIndex)
    {
        var childBounds = child.Bounds;
        for (int i = childIndex + 1; i < Controls.Count; i++)
        {
            var sibling = Controls[i];
            if (!sibling.Visible || !sibling.IsOpaque)
                continue;
            if (sibling.Bounds.Contains(childBounds))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Renders the control and its children using the specified graphics object.
    /// Applies zoom scaling to all child controls.
    /// Children that are fully occluded by opaque siblings are skipped.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
    public virtual void Render(Graphics g)
    {
        _dirty = false;
        g.Zoom = EffectiveZoom;
        for (int i = 0; i < Controls.Count; i++)
        {
            var child = Controls[i];
            if (!child.Visible) continue;
            if (IsCompletelyOccluded(child, i)) continue;

            g.Save();
            g.TranslateTransform(child.X, child.Y);
            child.Render(g);
            g.Restore();
        }
    }

    /// <summary>
    /// Renders overlay elements (like dropdowns) on top of other controls.
    /// Children that are fully occluded by opaque siblings are skipped.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
    public virtual void RenderOverlay(Graphics g)
    {
        g.Zoom = EffectiveZoom;
        for (int i = 0; i < Controls.Count; i++)
        {
            var child = Controls[i];
            if (!child.Visible) continue;
            if (IsCompletelyOccluded(child, i)) continue;

            g.Save();
            g.TranslateTransform(child.X, child.Y);
            child.RenderOverlay(g);
            g.Restore();
        }
    }

    /// <summary>
    /// Invalidates the entire control, forcing a repaint.
    /// Propagates the invalidation up to the parent Form so the form-level
    /// dirty check triggers a render on the next frame.
    /// </summary>
    public virtual void Invalidate()
    {
        _dirty = true;
        if (this is not Form)
        {
            var form = FindForm();
            if (form != null && form != this)
            {
                form.Invalidate();
            }
        }
    }

    /// <summary>
    /// Invalidates the specified region of the control.
    /// </summary>
    /// <param name="rect">The rectangle to invalidate.</param>
    public virtual void Invalidate(Rectangle rect)
    {
        _dirty = true;
    }

    /// <summary>
    /// Gets whether the control needs to be repainted.
    /// </summary>
    public bool Dirty => _dirty;

    /// <summary>
    /// Searches for the parent Form of this control.
    /// </summary>
    /// <returns>The parent Form, or null if no parent Form exists.</returns>
    public Form? FindForm()
    {
        if (_cachedForm != null)
            return _cachedForm;

        Control? current = this;
        while (current != null)
        {
            if (current is Form form)
            {
                _cachedForm = form;
                return form;
            }
            current = current.Parent;
        }
        return null;
    }

    /// <summary>
    /// Gets the render offset applied to child controls by this parent.
    /// Override to report visual offsets (e.g., TabControl's tab header).
    /// </summary>
    /// <returns>The render offset applied to children.</returns>
    protected internal virtual Point GetChildRenderOffset() => Point.Empty;

    /// <summary>
    /// Gets the cumulative position of this control relative to its parent Form.
    /// Sums the X/Y coordinates of all parent controls up to the Form,
    /// including any child render offsets applied by parent controls.
    /// </summary>
    /// <returns>A point representing the control's position in form coordinates.</returns>
    public virtual Point GetFormRelativePosition()
    {
        int x = X;
        int y = Y;
        Control? current = Parent;
        while (current != null && current is not Form)
        {
            x += current.X;
            y += current.Y;
            var offset = current.GetChildRenderOffset();
            x += offset.X;
            y += offset.Y;
            current = current.Parent;
        }
        return new Point(x, y);
    }

    /// <summary>
    /// Tests whether the specified point is within the bounds of the control.
    /// </summary>
    /// <param name="point">The point to test (should be in the coordinate space of the control's parent).</param>
    /// <returns>True if the point is within the control's bounds; otherwise, false.</returns>
    public virtual bool HitTest(Point point)
    {
        return Bounds.Contains(point);
    }

    /// <summary>
    /// Converts screen coordinates to client coordinates.
    /// </summary>
    /// <param name="screenPoint">A point in screen coordinates.</param>
    /// <returns>A point in client coordinates.</returns>
    public Point PointToClient(Point screenPoint)
    {
        return screenPoint;
    }

    /// <summary>
    /// Converts client coordinates to screen coordinates.
    /// </summary>
    /// <param name="clientPoint">A point in client coordinates.</param>
    /// <returns>A point in screen coordinates.</returns>
    public Point PointToScreen(Point clientPoint)
    {
        return clientPoint;
    }

    /// <summary>
    /// Forces the control to perform layout of its child controls.
    /// </summary>
    public void PerformLayout()
    {
        if (_layoutSuspendCount > 0) return;
        OnLayout();
    }

    /// <summary>
    /// Temporarily suspends layout operations.
    /// </summary>
    public void SuspendLayout()
    {
        _layoutSuspendCount++;
    }

    /// <summary>
    /// Resumes normal layout operations.
    /// </summary>
    public void ResumeLayout()
    {
        ResumeLayout(true);
    }

    /// <summary>
    /// Resumes normal layout operations, optionally performing layout immediately.
    /// </summary>
    /// <param name="performLayout">Whether to perform layout immediately.</param>
    public void ResumeLayout(bool performLayout)
    {
        if (_layoutSuspendCount > 0)
            _layoutSuspendCount--;
        if (_layoutSuspendCount == 0 && performLayout)
            PerformLayout();
    }

    /// <summary>
    /// Called when the control needs to arrange its child controls.
    /// </summary>
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

    /// <summary>
    /// Called when the bounds of the control change.
    /// </summary>
    protected virtual void OnBoundsChanged()
    {
        BoundsChanged?.Invoke(this, EventArgs.Empty);
        if (!_layoutDrivenBoundsChange)
        {
            UpdateAnchorDistances();
        }
        PerformLayout();
    }

    /// <summary>
    /// Occurs when the control's bounds change.
    /// </summary>
    public event EventHandler? BoundsChanged;

    /// <summary>
    /// Called when the visibility of the control changes.
    /// </summary>
    protected virtual void OnVisibleChanged()
    {
        Invalidate();
        Parent?.PerformLayout();
    }

    /// <summary>
    /// Called when the enabled state of the control changes.
    /// </summary>
    protected virtual void OnEnabledChanged()
    {
        Invalidate();
    }

    /// <summary>
    /// Called when the text of the control changes.
    /// </summary>
    protected virtual void OnTextChanged()
    {
    }

    /// <summary>
    /// Occurs when the control is clicked.
    /// </summary>
    public event EventHandler? Click;

    /// <summary>
    /// Occurs when the control is double-clicked.
    /// </summary>
    public event EventHandler? DoubleClick;

    /// <summary>
    /// Occurs when the mouse cursor enters the control.
    /// </summary>
    public event EventHandler? MouseEnter;

    /// <summary>
    /// Occurs when the mouse cursor leaves the control.
    /// </summary>
    public event EventHandler? MouseLeave;

    /// <summary>
    /// Occurs when the mouse cursor moves over the control.
    /// </summary>
    public event EventHandler? MouseMove;

    /// <summary>
    /// Occurs when a mouse button is pressed over the control.
    /// </summary>
    public event EventHandler? MouseDown;

    /// <summary>
    /// Occurs when a mouse button is released over the control.
    /// </summary>
    public event EventHandler? MouseUp;

    /// <summary>
    /// Occurs when the mouse wheel is rotated.
    /// </summary>
    public event EventHandler? MouseWheel;

    /// <summary>
    /// Occurs when a key is pressed while the control has focus.
    /// </summary>
    public event EventHandler? KeyDown;

    /// <summary>
    /// Occurs when a character key is pressed while the control has focus.
    /// </summary>
    public event EventHandler? KeyPress;

    /// <summary>
    /// Occurs when a key is released while the control has focus.
    /// </summary>
    public event EventHandler? KeyUp;

    /// <summary>
    /// Occurs when the control receives focus.
    /// </summary>
    public event EventHandler? GotFocus;

    /// <summary>
    /// Occurs when the control loses focus.
    /// </summary>
    public event EventHandler? LostFocus;

    /// <summary>
    /// Raises the Click event.
    /// </summary>
    /// <param name="e">An EventArgs that contains the event data.</param>
    protected virtual void OnClick(EventArgs e)
    {
        if (!Enabled) return;
        Click?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the DoubleClick event.
    /// </summary>
    /// <param name="e">An EventArgs that contains the event data.</param>
    protected virtual void OnDoubleClick(EventArgs e)
    {
        if (!Enabled) return;
        DoubleClick?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the MouseEnter event and sets the Hovered state.
    /// </summary>
    /// <param name="e">An EventArgs that contains the event data.</param>
    protected virtual void OnMouseEnter(EventArgs e)
    {
        if (!Enabled) return;
        if (!IsHovered)
        {
            State |= ControlState.Hovered;
        }
        MouseEnter?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the MouseLeave event and clears the Hovered state.
    /// </summary>
    /// <param name="e">An EventArgs that contains the event data.</param>
    protected virtual void OnMouseLeave(EventArgs e)
    {
        if (IsHovered)
        {
            State &= ~ControlState.Hovered;
        }
        MouseLeave?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the MouseMove event.
    /// </summary>
    /// <param name="e">An EventArgs that contains the event data.</param>
    protected internal virtual void OnMouseMove(EventArgs e)
    {
        if (!Enabled) return;
        MouseMove?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the MouseDown event and sets the Pressed state.
    /// </summary>
    /// <param name="e">An EventArgs that contains the event data.</param>
    protected internal virtual void OnMouseDown(EventArgs e)
    {
        if (!Enabled) return;
        if (!IsPressed)
        {
            State |= ControlState.Pressed;
        }
        MouseDown?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the MouseUp event and clears the Pressed state.
    /// </summary>
    /// <param name="e">An EventArgs that contains the event data.</param>
    protected internal virtual void OnMouseUp(EventArgs e)
    {
        if (!Enabled) return;
        if (IsPressed)
        {
            State &= ~ControlState.Pressed;
        }
        MouseUp?.Invoke(this, e);
        OnClick(EventArgs.Empty);
    }

    /// <summary>
    /// Raises the MouseWheel event.
    /// </summary>
    /// <param name="e">An EventArgs that contains the event data.</param>
    protected internal virtual void OnMouseWheel(EventArgs e)
    {
        if (!Enabled) return;
        MouseWheel?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the KeyDown event.
    /// </summary>
    /// <param name="e">A KeyEventArgs that contains the event data.</param>
    protected internal virtual void OnKeyDown(KeyEventArgs e)
    {
        if (!Enabled) return;
        KeyDown?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the KeyPress event.
    /// </summary>
    /// <param name="e">A KeyPressEventArgs that contains the event data.</param>
    protected internal virtual void OnKeyPress(KeyPressEventArgs e)
    {
        if (!Enabled) return;
        KeyPress?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the KeyUp event.
    /// </summary>
    /// <param name="e">A KeyEventArgs that contains the event data.</param>
    protected internal virtual void OnKeyUp(KeyEventArgs e)
    {
        if (!Enabled) return;
        KeyUp?.Invoke(this, e);
    }

    /// <summary>
    /// Called when text input is received.
    /// </summary>
    /// <param name="text">The input text.</param>
    protected internal virtual void OnTextInput(string text)
    {
        if (!Enabled) return;
    }

    /// <summary>
    /// Raises the GotFocus event.
    /// </summary>
    /// <param name="e">An EventArgs that contains the event data.</param>
    protected internal virtual void OnGotFocus(EventArgs e)
    {
        if (!Focused)
        {
            State |= ControlState.Focused;
        }
        OnFocused();
        GotFocus?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the LostFocus event.
    /// </summary>
    /// <param name="e">An EventArgs that contains the event data.</param>
    protected internal virtual void OnLostFocus(EventArgs e)
    {
        if (Focused)
        {
            State &= ~ControlState.Focused;
        }
        OnUnfocused();
        LostFocus?.Invoke(this, e);
    }

    /// <summary>
    /// Called when the control receives focus. Override to provide custom focus behavior.
    /// </summary>
    protected internal virtual void OnFocused() { }

    /// <summary>
    /// Called when the control loses focus. Override to provide custom focus behavior.
    /// </summary>
    protected internal virtual void OnUnfocused() { }

    /// <summary>
    /// Draws the focus indicator for the control if it has focus and focus cues are enabled.
    /// Override to customize focus rendering or provide different behavior.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
    protected virtual void DrawFocusIndicator(Graphics g)
    {
        if (Focused && ShowFocusCues)
        {
            g.DrawRectangle(FocusColor, 0, 0, Width, Height, 2);
        }
    }

    /// <summary>
    /// Gets the color used for the focus indicator. Override to customize.
    /// </summary>
    protected virtual Color FocusColor => ThemeManager.CurrentTheme.FocusIndicator;

    /// <summary>
    /// Gets a color from the active theme. Override to provide custom theme color resolution.
    /// Individual control colors take precedence over theme colors.
    /// </summary>
    /// <param name="themeColorSelector">A function that selects the color from the theme.</param>
    /// <returns>The theme color, or the control's color if explicitly set.</returns>
    protected Color GetThemeColor(Func<Theme, Color> themeColorSelector)
    {
        return themeColorSelector(ThemeManager.CurrentTheme);
    }

    /// <summary>
    /// Called when the theme changes. Invalidates the control to trigger repaint with new theme colors.
    /// </summary>
    /// <param name="newTheme">The new theme that was activated.</param>
    public virtual void OnThemeChanged(Theme newTheme)
    {
        if (!_backColorSet)
            _backColor = newTheme.ControlBackground;
        if (!_foreColorSet)
            _foreColor = newTheme.ControlText;
        Invalidate();
    }

    /// <summary>
    /// Called when the control is activated (e.g., Enter or Space key pressed while focused).
    /// Override to provide custom activation behavior. Base implementation is empty.
    /// </summary>
    protected internal virtual void OnActivate() { }
}

/// <summary>
/// Provides a collection for managing child controls.
/// </summary>
public class ControlCollection : IEnumerable<Control>
{
    private readonly Control _owner;
    private readonly List<Control> _controls = new();

    /// <summary>
    /// Initializes a new instance of ControlCollection for the specified owner.
    /// </summary>
    /// <param name="owner">The owner control that owns this collection.</param>
    public ControlCollection(Control owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// Gets the number of controls in the collection.
    /// </summary>
    public int Count => _controls.Count;

    /// <summary>
    /// Gets the control at the specified index.
    /// </summary>
    /// <param name="index">The index of the control to retrieve.</param>
    /// <returns>The control at the specified index.</returns>
    public Control this[int index] => _controls[index];

    /// <summary>
    /// Adds a control to the collection.
    /// </summary>
    /// <param name="control">The control to add.</param>
    public void Add(Control control)
    {
        if (!_controls.Contains(control))
        {
            _controls.Add(control);
            control.Parent = _owner;
            control.UpdateAnchorDistances();
            _owner.Invalidate();
            _owner.PerformLayout();
        }
    }

    /// <summary>
    /// Removes a control from the collection.
    /// </summary>
    /// <param name="control">The control to remove.</param>
    public void Remove(Control control)
    {
        if (_controls.Remove(control))
        {
            _owner.Invalidate();
            _owner.PerformLayout();
        }
    }

    /// <summary>
    /// Removes all controls from the collection.
    /// </summary>
    public void Clear()
    {
        _controls.Clear();
        _owner.Invalidate();
        _owner.PerformLayout();
    }

    /// <summary>
    /// Finds a control by its name.
    /// </summary>
    /// <param name="name">The name of the control to find.</param>
    /// <param name="recursive">Whether to search child containers recursively.</param>
    /// <returns>The control with the specified name, or null if not found.</returns>
    public Control? FindControl(string name, bool recursive = false)
    {
        if (string.IsNullOrEmpty(name))
            return null;

        foreach (Control control in _controls)
        {
            if (control.Name == name)
                return control;

            if (recursive)
            {
                Control? found = control.Controls.FindControl(name, true);
                if (found != null)
                    return found;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds a control by its name, cast to the specified type.
    /// </summary>
    /// <typeparam name="T">The type of control to find.</typeparam>
    /// <param name="name">The name of the control to find.</param>
    /// <param name="recursive">Whether to search child containers recursively.</param>
    /// <returns>The control cast to the specified type, or null if not found.</returns>
    public T? FindControl<T>(string name, bool recursive = false) where T : Control
    {
        return FindControl(name, recursive) as T;
    }

    /// <summary>
    /// Copies the control's value to the system clipboard.
    /// </summary>
    public void CopyToClipboard()
    {
        var txt = ((Control)(object)this).Text;
        if (txt != null && txt.Length > 0)
        {
            Core.Clipboard.SetText(txt);
        }
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator for the collection.</returns>
    public IEnumerator<Control> GetEnumerator() => _controls.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// Specifies which edges of a control are anchored to its parent.
/// </summary>
[Flags]
public enum AnchorStyles
{
    /// <summary>
    /// No edge is anchored.
    /// </summary>
    None = 0,

    /// <summary>
    /// The top edge is anchored.
    /// </summary>
    Top = 1,

    /// <summary>
    /// The bottom edge is anchored.
    /// </summary>
    Bottom = 2,

    /// <summary>
    /// The left edge is anchored.
    /// </summary>
    Left = 4,

    /// <summary>
    /// The right edge is anchored.
    /// </summary>
    Right = 8
}

/// <summary>
/// Specifies which edge of a control is docked to its parent.
/// </summary>
public enum DockStyle
{
    /// <summary>
    /// No docking.
    /// </summary>
    None,

    /// <summary>
    /// The control is docked to the top edge of its parent.
    /// </summary>
    Top,

    /// <summary>
    /// The control is docked to the bottom edge of its parent.
    /// </summary>
    Bottom,

    /// <summary>
    /// The control is docked to the left edge of its parent.
    /// </summary>
    Left,

    /// <summary>
    /// The control is docked to the right edge of its parent.
    /// </summary>
    Right,

    /// <summary>
    /// The control fills the remaining space in its parent.
    /// </summary>
    Fill
}