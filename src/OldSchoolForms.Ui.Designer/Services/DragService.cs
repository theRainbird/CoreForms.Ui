using System;
using System.Collections.Generic;
using System.Linq;
using OldSchoolForms.Ui.Core;

namespace OldSchoolForms.Ui.Designer.Services;

/// <summary>
/// Handles mouse-driven move and resize operations for controls on the design surface.
/// Tracks the drag state and applies bounds changes in real time.
/// </summary>
public class DragService
{
    private readonly DesignSurface _surface;
    private readonly SelectionService _selectionService;
    private readonly SnapService _snapService;

    private bool _isDragging;
    private ResizeHandle _activeHandle = ResizeHandle.None;
    private Point _dragStart;
    private readonly List<DesignItem> _dragItems = new();
    private readonly Dictionary<DesignItem, Point> _surfaceOrigins = new();

    private ContainerControl? _dropTarget;

    private SnapResult _currentSnap = new();

    private const int MinControlSize = 10;

    /// <summary>
    /// Gets or sets the minimum size applied when resizing a control.
    /// Defaults to <see cref="MinControlSize"/>; callers may raise it for
    /// container roots (e.g. a design form) that must stay large enough to
    /// host their title bar or a usable client area.
    /// </summary>
    public Size MinResizeSize { get; set; } = new Size(MinControlSize, MinControlSize);

    /// <summary>
    /// Raised when a drag operation starts.
    /// </summary>
    public event EventHandler? DragStarted;

    /// <summary>
    /// Raised when a drag operation completes (mouse up).
    /// </summary>
    public event EventHandler? DragCompleted;

    /// <summary>
    /// Raised when the dragged controls are reparented onto a different container (or back to the surface).
    /// </summary>
    public event EventHandler? DragReparented;

    /// <summary>
    /// Gets whether a drag operation is currently in progress.
    /// </summary>
    public bool IsDragging => _isDragging;

    /// <summary>
    /// Gets the active resize handle during a resize operation.
    /// </summary>
    public ResizeHandle ActiveHandle => _activeHandle;

    /// <summary>
    /// Gets the current snap result with guide lines for rendering.
    /// </summary>
    public SnapResult CurrentSnap => _currentSnap;

    /// <summary>
    /// Initializes a new instance of <see cref="DragService"/>.
    /// </summary>
    /// <param name="surface">The design surface.</param>
    /// <param name="selectionService">The selection service.</param>
    /// <param name="snapService">The snap service for smart guides, or null to disable.</param>
    /// <exception cref="ArgumentNullException">Thrown if surface or selectionService is null.</exception>
    public DragService(DesignSurface surface, SelectionService selectionService, SnapService? snapService = null)
    {
        _surface = surface ?? throw new ArgumentNullException(nameof(surface));
        _selectionService = selectionService ?? throw new ArgumentNullException(nameof(selectionService));
        _snapService = snapService ?? new SnapService();
    }

    /// <summary>
    /// Begins a drag operation for moving the selected controls.
    /// </summary>
    /// <param name="screenPoint">The initial mouse point in form coordinates.</param>
    public void BeginMove(Point screenPoint)
    {
        if (_selectionService.SelectedCount == 0) return;

        // The root control (the design form or user control) may be resized but never
        // moved, so exclude it from move drags. If only the root is selected, no drag starts.
        var movable = _selectionService.SelectedItems
            .Where(i => !ReferenceEquals(i.Control, _surface.RootControl))
            .ToArray();
        if (movable.Length == 0) return;

        _isDragging = true;
        _activeHandle = ResizeHandle.None;
        _dragStart = screenPoint;
        _currentSnap = new SnapResult();

        _dragItems.Clear();
        _surfaceOrigins.Clear();
        foreach (var item in movable)
        {
            item.SnapshotBounds();
            _surfaceOrigins[item] = item.Control.PointToScreen(Point.Empty);
            _dragItems.Add(item);
        }

        // Start the drag inside the primary selection's current container, so that
        // moving within it produces no reparent while dragging across reparents.
        // The surface itself is normalised to null (its items are tracked on the flat
        // design list, not inside a container).
        var primaryParent = _selectionService.PrimarySelection?.Control?.Parent as ContainerControl;
        _dropTarget = primaryParent != null && !ReferenceEquals(primaryParent, _surface)
            ? primaryParent
            : null;

        OnDragStarted();
    }

    /// <summary>
    /// Begins a drag operation for resizing a single control via the specified handle.
    /// </summary>
    /// <param name="screenPoint">The initial mouse point in form coordinates.</param>
    /// <param name="handle">The resize handle being dragged.</param>
    public void BeginResize(Point screenPoint, ResizeHandle handle)
    {
        var primary = _selectionService.PrimarySelection;
        if (primary == null || handle == ResizeHandle.None) return;

        _isDragging = true;
        _activeHandle = handle;
        _dragStart = screenPoint;
        _currentSnap = new SnapResult();

        _dragItems.Clear();
        _dragItems.Add(primary);
        primary.SnapshotBounds();

        OnDragStarted();
    }

    /// <summary>
    /// Updates the drag operation with the current mouse position.
    /// Applies smart guide snapping when enabled.
    /// </summary>
    /// <param name="screenPoint">The current mouse point in form coordinates.</param>
    public void ContinueDrag(Point screenPoint)
    {
        if (!_isDragging) return;

        if (_activeHandle == ResizeHandle.None)
        {
            // Detect live reparenting onto a different container (or back to the surface).
            var target = _surface.FindDropContainerAt(screenPoint, exclude: _selectionService.PrimarySelection?.Control);
            if (!ReferenceEquals(target, _dropTarget))
            {
                ReparentDragItems(target);
                _dropTarget = target;
                OnReparented();
            }

            // Move in surface coordinates so reparenting is seamless: PointToClient
            // converts the desired surface position into whatever the current parent is.
            MoveDragItems(screenPoint);

            // Snap only applies to a single control moving on the surface itself.
            if (_dragItems.Count == 1 && _dropTarget == null && _surface.SnapToGrid)
            {
                ApplyMoveSnap();
            }
        }
        else
        {
            var target = _dragItems[0];
            var ctrl = target.Control;
            var original = target.OriginalBounds;

            int dx = screenPoint.X - _dragStart.X;
            int dy = screenPoint.Y - _dragStart.Y;

            int newX = original.X, newY = original.Y;
            int newW = original.Width, newH = original.Height;

            bool leftAnchor = (_activeHandle & (ResizeHandle.TopLeft | ResizeHandle.MiddleLeft | ResizeHandle.BottomLeft)) != 0;
            bool rightAnchor = (_activeHandle & (ResizeHandle.TopRight | ResizeHandle.MiddleRight | ResizeHandle.BottomRight)) != 0;

            if (leftAnchor)
            {
                newX = original.X + dx;
                newW = original.Width - dx;
                if (newW < MinResizeSize.Width)
                {
                    newX = original.X + original.Width - MinResizeSize.Width;
                    newW = MinResizeSize.Width;
                }
            }
            else if (rightAnchor)
            {
                newW = original.Width + dx;
                if (newW < MinResizeSize.Width) newW = MinResizeSize.Width;
            }

            bool topAnchor = (_activeHandle & (ResizeHandle.TopLeft | ResizeHandle.TopCenter | ResizeHandle.TopRight)) != 0;
            bool bottomAnchor = (_activeHandle & (ResizeHandle.BottomLeft | ResizeHandle.BottomCenter | ResizeHandle.BottomRight)) != 0;

            if (topAnchor)
            {
                newY = original.Y + dy;
                newH = original.Height - dy;
                if (newH < MinResizeSize.Height)
                {
                    newY = original.Y + original.Height - MinResizeSize.Height;
                    newH = MinResizeSize.Height;
                }
            }
            else if (bottomAnchor)
            {
                newH = original.Height + dy;
                if (newH < MinResizeSize.Height) newH = MinResizeSize.Height;
            }

            ctrl.Bounds = new Rectangle(newX, newY, newW, newH);
        }
    }

    private void ApplyMoveSnap()
    {
        var primary = _dragItems[0];
        var movingBounds = primary.Control.Bounds;
        var siblingBounds = _surface.Items
            .Where(i => i != primary)
            .Select(i => i.Control.Bounds)
            .ToList();

        _currentSnap = _snapService.ComputeSnap(movingBounds, primary.OriginalBounds, siblingBounds);

        if (_currentSnap.HasSnap)
        {
            primary.Control.Location = new Point(
                movingBounds.X + _currentSnap.SnapX,
                movingBounds.Y + _currentSnap.SnapY);
        }
    }

    private void MoveDragItems(Point screenPoint)
    {
        int dx = screenPoint.X - _dragStart.X;
        int dy = screenPoint.Y - _dragStart.Y;

        foreach (var item in _dragItems)
        {
            var ctrl = item.Control;
            var origin = _surfaceOrigins[item];
            var desired = new Point(origin.X + dx, origin.Y + dy);

            // Place the control so its top-left follows the mouse in surface coordinates.
            // Subtract the current parent's screen origin and its render offset (e.g. a form
            // title bar) so the location is expressed in the (possibly reparented) parent's
            // client coordinate space and the control lands under the cursor.
            var parent = ctrl.Parent;
            var parentOrigin = parent != null ? parent.PointToScreen(Point.Empty) : Point.Empty;
            var renderOffset = parent != null ? parent.GetChildRenderOffsetPublic() : Point.Empty;
            ctrl.Location = new Point(
                desired.X - parentOrigin.X - renderOffset.X,
                desired.Y - parentOrigin.Y - renderOffset.Y);
        }
    }

    private void ReparentDragItems(ContainerControl? target)
    {
        // A null target means the controls are being moved back onto the design
        // surface itself, which is where surface-level controls are parented.
        var actualParent = target ?? _surface;
        foreach (var item in _dragItems)
        {
            var ctrl = item.Control;
            if (ReferenceEquals(actualParent, ctrl) || IsAncestorOf(ctrl, actualParent))
                continue;
            ctrl.Parent = actualParent;
            item.ParentItem = !ReferenceEquals(actualParent, _surface)
                ? _surface.FindItem(actualParent)
                : null;
        }
    }

    private static bool IsAncestorOf(Control ancestor, Control descendant)
    {
        var current = descendant.Parent;
        while (current != null)
        {
            if (ReferenceEquals(current, ancestor)) return true;
            current = current.Parent;
        }
        return false;
    }

    /// <summary>
    /// Ends the current drag operation.
    /// </summary>
    public void EndDrag()
    {
        if (!_isDragging) return;
        _isDragging = false;
        _activeHandle = ResizeHandle.None;
        _dragItems.Clear();
        _currentSnap = new SnapResult();
        OnDragCompleted();
    }

    /// <summary>
    /// Cancels the current drag, restoring original bounds.
    /// </summary>
    public void CancelDrag()
    {
        if (!_isDragging) return;

        foreach (var item in _dragItems)
            item.Control.Bounds = item.OriginalBounds;

        _isDragging = false;
        _activeHandle = ResizeHandle.None;
        _dragItems.Clear();
        _currentSnap = new SnapResult();
    }

    /// <summary>
    /// Hit-tests the resize handles for the given control at the specified point.
    /// </summary>
    public ResizeHandle HitTestHandles(DesignItem item, Point point)
    {
        if (item == null) return ResizeHandle.None;

        var bounds = item.Control.Bounds;
        int hs = SelectionService.GetHandleSize();
        int half = hs / 2;

        if (HitTestRect(point, bounds.X - half, bounds.Y - half, hs, hs)) return ResizeHandle.TopLeft;
        if (HitTestRect(point, bounds.Right - half, bounds.Y - half, hs, hs)) return ResizeHandle.TopRight;
        if (HitTestRect(point, bounds.X - half, bounds.Bottom - half, hs, hs)) return ResizeHandle.BottomLeft;
        if (HitTestRect(point, bounds.Right - half, bounds.Bottom - half, hs, hs)) return ResizeHandle.BottomRight;

        if (bounds.Width > hs * 3)
        {
            int cx = bounds.X + bounds.Width / 2 - half;
            if (HitTestRect(point, cx, bounds.Y - half, hs, hs)) return ResizeHandle.TopCenter;
            if (HitTestRect(point, cx, bounds.Bottom - half, hs, hs)) return ResizeHandle.BottomCenter;
        }

        if (bounds.Height > hs * 3)
        {
            int cy = bounds.Y + bounds.Height / 2 - half;
            if (HitTestRect(point, bounds.X - half, cy, hs, hs)) return ResizeHandle.MiddleLeft;
            if (HitTestRect(point, bounds.Right - half, cy, hs, hs)) return ResizeHandle.MiddleRight;
        }

        return ResizeHandle.None;
    }

    private static bool HitTestRect(Point p, int x, int y, int w, int h)
    {
        return p.X >= x && p.X < x + w && p.Y >= y && p.Y < y + h;
    }

    private void OnDragStarted() => DragStarted?.Invoke(this, EventArgs.Empty);
    private void OnDragCompleted() => DragCompleted?.Invoke(this, EventArgs.Empty);
    private void OnReparented() => DragReparented?.Invoke(this, EventArgs.Empty);
}
