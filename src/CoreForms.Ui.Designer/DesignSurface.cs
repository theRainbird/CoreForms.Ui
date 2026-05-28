using System;
using System.Collections.Generic;
using System.Linq;
using CoreForms.Ui.Core;
using CoreForms.Ui.Designer.Services;
using CoreForms.Ui.Theming;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Designer;

/// <summary>
/// The design surface is a <see cref="ContainerControl"/> that hosts and renders
/// controls in design mode. It provides grid rendering, selection chrome,
/// and mouse interaction (selection, move, resize) by coordinating
/// <see cref="SelectionService"/>, <see cref="DragService"/>, and other services.
/// </summary>
public class DesignSurface : ContainerControl
{
    private readonly List<DesignItem> _designItems = new();
    private readonly SelectionService _selectionService;
    private readonly DragService _dragService;
    private readonly ToolboxService _toolboxService;
    private readonly UndoService _undoService;

    private Point _lastMouseDown;
    private bool _hasMouseMovedSinceDown;

    private const int GridSize = 8;
    private bool _showGrid = true;
    private bool _snapToGrid = true;

    /// <summary>
    /// Raised when the selection on the design surface changes.
    /// </summary>
    public event EventHandler? SelectionChanged;

    /// <summary>
    /// Raised when the design surface content is modified (add, delete, move, resize).
    /// </summary>
    public event EventHandler? ContentModified;

    /// <summary>
    /// Gets the selection service for this design surface.
    /// </summary>
    public SelectionService Selection => _selectionService;

    /// <summary>
    /// Gets the drag service for this design surface.
    /// </summary>
    public DragService Drag => _dragService;

    /// <summary>
    /// Gets the toolbox service for this design surface.
    /// </summary>
    public ToolboxService Toolbox => _toolboxService;

    /// <summary>
    /// Gets the undo service for this design surface.
    /// </summary>
    public UndoService Undo => _undoService;

    /// <summary>
    /// Gets the list of all design items on the surface.
    /// </summary>
    public IReadOnlyList<DesignItem> Items => _designItems.AsReadOnly();

    /// <summary>
    /// Gets or sets whether the alignment grid is visible.
    /// </summary>
    public bool ShowGrid
    {
        get => _showGrid;
        set { _showGrid = value; Invalidate(); }
    }

    /// <summary>
    /// Gets or sets whether controls snap to the alignment grid when moved or resized.
    /// </summary>
    public bool SnapToGrid
    {
        get => _snapToGrid;
        set => _snapToGrid = value;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="DesignSurface"/>.
    /// </summary>
    public DesignSurface()
    {
        _selectionService = new SelectionService();
        _dragService = new DragService(this, _selectionService);
        _toolboxService = new ToolboxService();
        _undoService = new UndoService();

        _selectionService.SelectionChanged += (s, e) =>
        {
            SelectionChanged?.Invoke(this, e);
            Invalidate();
        };

        _dragService.DragCompleted += (s, e) =>
        {
            ClearResizeCursor();
            ContentModified?.Invoke(this, e);
        };

        BackColor = Color.White;
    }

    /// <summary>
    /// Adds a control to the design surface at the specified location.
    /// The control is wrapped in a <see cref="DesignItem"/> and added as a child.
    /// </summary>
    /// <param name="control">The control to add.</param>
    /// <param name="location">The location on the design surface.</param>
    /// <returns>The newly created DesignItem.</returns>
    public DesignItem AddControl(Control control, Point location)
    {
        if (control == null) throw new ArgumentNullException(nameof(control));

        control.Location = SnapToGrid ? SnapPoint(location) : location;
        Controls.Add(control);

        var item = new DesignItem(control);
        _designItems.Add(item);

        _selectionService.Select(item);
        _undoService.Clear();
        ContentModified?.Invoke(this, EventArgs.Empty);
        Invalidate();
        return item;
    }

    /// <summary>
    /// Adds a control that was created by a toolbox item.
    /// </summary>
    /// <param name="toolboxItem">The toolbox item describing the control type.</param>
    /// <param name="location">The placement location.</param>
    /// <returns>The newly created DesignItem.</returns>
    public DesignItem AddControlFromToolbox(ToolboxItem toolboxItem, Point location)
    {
        var control = _toolboxService.CreateControl(toolboxItem);
        control.Location = SnapToGrid ? SnapPoint(location) : location;
        return AddControl(control, control.Location);
    }

    /// <summary>
    /// Removes a design item and its underlying control from the surface.
    /// </summary>
    /// <param name="item">The item to remove.</param>
    public void RemoveControl(DesignItem item)
    {
        if (item == null) return;

        _undoService.PushSnapshot(item.Control);
        _selectionService.Deselect(item);
        Controls.Remove(item.Control);
        _designItems.Remove(item);
        ContentModified?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    /// <summary>
    /// Removes all currently selected controls.
    /// </summary>
    public void DeleteSelected()
    {
        var items = _selectionService.SelectedItems.ToArray();
        if (items.Length == 0) return;

        _undoService.PushSnapshot(items.Select(i => i.Control).ToArray());
        _selectionService.DeselectAll();

        foreach (var item in items)
        {
            Controls.Remove(item.Control);
            _designItems.Remove(item);
        }

        ContentModified?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    /// <summary>
    /// Finds the DesignItem that wraps the given control.
    /// </summary>
    public DesignItem? FindItem(Control control)
    {
        return _designItems.FirstOrDefault(di => di.Control == control);
    }

    /// <summary>
    /// Clears all controls from the design surface.
    /// </summary>
    public void ClearAll()
    {
        _selectionService.DeselectAll();
        _designItems.Clear();
        Controls.Clear();
        _undoService.Clear();
        ContentModified?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    /// <summary>
    /// Snaps a point to the nearest grid intersection.
    /// </summary>
    public Point SnapPoint(Point point)
    {
        int grid = GridSize;
        return new Point(
            (int)Math.Round(point.X / (double)grid) * grid,
            (int)Math.Round(point.Y / (double)grid) * grid);
    }

    /// <summary>
    /// Hit-tests the children of this design surface and returns the deepest
    /// <see cref="DesignItem"/> at the given point (in surface coordinates).
    /// Useful for container-aware drop placement.
    /// </summary>
    /// <param name="point">The point in this surface's coordinates.</param>
    /// <returns>The deepest DesignItem at the point, or null if none.</returns>
    public DesignItem? HitTestChild(Point point)
    {
        var child = GetDeepestChildAtPoint(point, out _);
        return child != null && child != this ? FindItem(child) : null;
    }

    /// <summary>
    /// Creates a new control at the specified location by type name.
    /// </summary>
    /// <param name="typeName">The display name or type name of the control.</param>
    /// <param name="location">The placement location.</param>
    /// <returns>The DesignItem, or null if the type was not found.</returns>
    public DesignItem? AddControlByType(string typeName, Point location)
    {
        var control = _toolboxService.CreateControl(typeName);
        if (control == null) return null;
        return AddControl(control, location);
    }

    private static ModifierKeys GetCurrentModifiers()
    {
        try
        {
            return CoreForms.Ui.Platform.Platform.GetCurrentModifiers();
        }
        catch
        {
            return ModifierKeys.None;
        }
    }

    protected override void OnMouseDown(EventArgs e)
    {
        if (e is not MouseEventArgs args) return;

        _lastMouseDown = new Point(args.X, args.Y);
        _hasMouseMovedSinceDown = false;

        // Do NOT call base.OnMouseDown — we intercept all mouse events
        // to prevent routing interaction events to designed controls.
    }

    protected override void OnMouseMove(EventArgs e)
    {
        if (e is not MouseEventArgs args) return;

        var point = new Point(args.X, args.Y);

        if (_dragService.IsDragging)
        {
            _dragService.ContinueDrag(point);
            Invalidate();
            return;
        }

        if (args.Button == MouseButtons.None)
        {
            // Update resize cursor when hovering over handles
            var primary = _selectionService.PrimarySelection;
            if (primary != null)
            {
                var handle = _dragService.HitTestHandles(primary, point);
                if (handle != ResizeHandle.None)
                    SetResizeCursor(handle);
                else
                    ClearResizeCursor();
            }
            else
            {
                ClearResizeCursor();
            }
        }

        if (args.Button == MouseButtons.Left && !_hasMouseMovedSinceDown)
        {
            int moveThreshold = 4;
            if (Math.Abs(point.X - _lastMouseDown.X) > moveThreshold ||
                Math.Abs(point.Y - _lastMouseDown.Y) > moveThreshold)
            {
                _hasMouseMovedSinceDown = true;

                var primarySel = _selectionService.PrimarySelection;
                if (primarySel != null)
                {
                    var handle = _dragService.HitTestHandles(primarySel, _lastMouseDown);
                    if (handle != ResizeHandle.None)
                    {
                        _dragService.BeginResize(_lastMouseDown, handle);
                        _undoService.PushSnapshot(primarySel.Control);
                    }
                    else
                    {
                        _dragService.BeginMove(_lastMouseDown);
                        var controls = _selectionService.SelectedItems.Select(i => i.Control).ToArray();
                        _undoService.PushSnapshot(controls);
                    }
                }
            }
        }
    }

    protected override void OnMouseUp(EventArgs e)
    {
        if (e is not MouseEventArgs args) return;

        if (_dragService.IsDragging)
        {
            _dragService.EndDrag();
            Invalidate();
            return;
        }

        var point = new Point(args.X, args.Y);

        if (!_hasMouseMovedSinceDown && args.Button == MouseButtons.Left)
        {
            // It was a click (not a drag)
            var target = GetDeepestChildAtPoint(point, out var localPoint);
            if (target != null && target != this)
            {
                var item = FindItem(target);
                if (item != null)
                {
                    bool ctrlPressed = GetCurrentModifiers().HasFlag(ModifierKeys.Control);

                    if (ctrlPressed)
                        _selectionService.ToggleSelection(item);
                    else
                        _selectionService.Select(item);
                }
            }
            else
            {
                _selectionService.DeselectAll();
            }
        }

        ClearResizeCursor();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
        {
            DeleteSelected();
            e.Handled = true;
            return;
        }

        if (e.Modifiers.HasFlag(ModifierKeys.Control))
        {
            switch (e.KeyCode)
            {
                case Keys.Z:
                    if (_undoService.CanUndo) _undoService.Undo();
                    e.Handled = true;
                    return;
                case Keys.Y:
                    if (_undoService.CanRedo) _undoService.Redo();
                    e.Handled = true;
                    return;
                case Keys.A:
                    _selectionService.SelectAll(_designItems);
                    Invalidate();
                    e.Handled = true;
                    return;
            }
        }

        // Nudge selected controls with arrow keys
        if (e.KeyCode is Keys.Left or Keys.Right or Keys.Up or Keys.Down)
        {
            int dx = 0, dy = 0;
            int step = _snapToGrid ? GridSize : 1;
            switch (e.KeyCode)
            {
                case Keys.Left: dx = -step; break;
                case Keys.Right: dx = step; break;
                case Keys.Up: dy = -step; break;
                case Keys.Down: dy = step; break;
            }

            foreach (var item in _selectionService.SelectedItems)
            {
                var ctrl = item.Control;
                ctrl.Location = new Point(ctrl.X + dx, ctrl.Y + dy);
            }
            e.Handled = true;
            Invalidate();
            return;
        }

        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
    }

    public override void Render(Graphics g)
    {
        // Draw grid background
        DrawGrid(g);

        // Render all child controls (the designed controls)
        base.Render(g);

        // Draw selection chrome (handles + border) on selected controls
        DrawSelectionChrome(g);
    }

    private void DrawGrid(Graphics g)
    {
        if (!_showGrid) return;

        var gridColor = Color.FromArgb(230, 230, 230);
        int spacing = GridSize;

        for (int x = 0; x < Width; x += spacing)
        {
            for (int y = 0; y < Height; y += spacing)
            {
                g.FillRectangle(gridColor, x, y, 1, 1);
            }
        }
    }

    private void DrawSelectionChrome(Graphics g)
    {
        var selectionColor = Color.FromArgb(0, 120, 215);
        int hs = SelectionService.GetHandleSize();

        foreach (var item in _designItems)
        {
            if (!item.Selected) continue;

            var bounds = item.Control.Bounds;
            bool isPrimary = item == _selectionService.PrimarySelection;

            if (isPrimary)
            {
                g.DrawRectangle(selectionColor, bounds.X - 1, bounds.Y - 1,
                    bounds.Width + 2, bounds.Height + 2, 2);
            }
            else
            {
                g.DrawRectangle(selectionColor, bounds.X, bounds.Y,
                    bounds.Width, bounds.Height, 1);
            }

            if (!isPrimary) continue;

            int half = hs / 2;

            DrawHandle(g, bounds.X - half, bounds.Y - half, hs, hs, selectionColor, true);
            DrawHandle(g, bounds.Right - half, bounds.Y - half, hs, hs, selectionColor, true);
            DrawHandle(g, bounds.X - half, bounds.Bottom - half, hs, hs, selectionColor, true);
            DrawHandle(g, bounds.Right - half, bounds.Bottom - half, hs, hs, selectionColor, true);

            if (bounds.Width > hs * 3)
            {
                int cx = bounds.X + bounds.Width / 2 - half;
                DrawHandle(g, cx, bounds.Y - half, hs, hs, selectionColor, false);
                DrawHandle(g, cx, bounds.Bottom - half, hs, hs, selectionColor, false);
            }

            if (bounds.Height > hs * 3)
            {
                int cy = bounds.Y + bounds.Height / 2 - half;
                DrawHandle(g, bounds.X - half, cy, hs, hs, selectionColor, false);
                DrawHandle(g, bounds.Right - half, cy, hs, hs, selectionColor, false);
            }
        }
    }

    private static void DrawHandle(Graphics g, int x, int y, int w, int h, Color borderColor, bool filled)
    {
        if (filled)
        {
            g.FillRectangle(Color.White, x + 1, y + 1, w - 2, h - 2);
        }
        g.DrawRectangle(borderColor, x, y, w, h, 1);
    }

    private void SetResizeCursor(ResizeHandle handle)
    {
        var form = FindForm();
        if (form == null) return;

        form.Cursor = handle switch
        {
            ResizeHandle.TopLeft or ResizeHandle.BottomRight => SystemCursorType.SizeAll,
            ResizeHandle.TopRight or ResizeHandle.BottomLeft => SystemCursorType.SizeAll,
            ResizeHandle.TopCenter or ResizeHandle.BottomCenter => SystemCursorType.SizeNS,
            ResizeHandle.MiddleLeft or ResizeHandle.MiddleRight => SystemCursorType.SizeWE,
            _ => null
        };
    }

    private void ClearResizeCursor()
    {
        var form = FindForm();
        if (form != null)
            form.Cursor = null;
    }
}
