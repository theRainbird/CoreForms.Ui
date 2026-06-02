using System;
using System.Collections.Generic;
using System.Linq;
using CoreForms.Ui.Core;

namespace CoreForms.Ui.Designer.Services;

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

    private SnapResult _currentSnap = new();

    private const int MinControlSize = 10;

    /// <summary>
    /// Raised when a drag operation starts.
    /// </summary>
    public event EventHandler? DragStarted;

    /// <summary>
    /// Raised when a drag operation completes (mouse up).
    /// </summary>
    public event EventHandler? DragCompleted;

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

        _isDragging = true;
        _activeHandle = ResizeHandle.None;
        _dragStart = screenPoint;
        _currentSnap = new SnapResult();

        _dragItems.Clear();
        foreach (var item in _selectionService.SelectedItems)
        {
            item.SnapshotBounds();
            _dragItems.Add(item);
        }

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

        int dx = screenPoint.X - _dragStart.X;
        int dy = screenPoint.Y - _dragStart.Y;

        if (_activeHandle == ResizeHandle.None)
        {
            foreach (var item in _dragItems)
            {
                var ctrl = item.Control;
                ctrl.Location = new Point(
                    item.OriginalBounds.X + dx,
                    item.OriginalBounds.Y + dy);
            }

            // Apply smart snap
            if (_dragItems.Count == 1 && _surface.SnapToGrid)
            {
                ApplyMoveSnap();
            }
        }
        else
        {
            var target = _dragItems[0];
            var ctrl = target.Control;
            var original = target.OriginalBounds;

            int newX = original.X, newY = original.Y;
            int newW = original.Width, newH = original.Height;

            bool leftAnchor = (_activeHandle & (ResizeHandle.TopLeft | ResizeHandle.MiddleLeft | ResizeHandle.BottomLeft)) != 0;
            bool rightAnchor = (_activeHandle & (ResizeHandle.TopRight | ResizeHandle.MiddleRight | ResizeHandle.BottomRight)) != 0;

            if (leftAnchor)
            {
                newX = original.X + dx;
                newW = original.Width - dx;
                if (newW < MinControlSize)
                {
                    newX = original.X + original.Width - MinControlSize;
                    newW = MinControlSize;
                }
            }
            else if (rightAnchor)
            {
                newW = original.Width + dx;
                if (newW < MinControlSize) newW = MinControlSize;
            }

            bool topAnchor = (_activeHandle & (ResizeHandle.TopLeft | ResizeHandle.TopCenter | ResizeHandle.TopRight)) != 0;
            bool bottomAnchor = (_activeHandle & (ResizeHandle.BottomLeft | ResizeHandle.BottomCenter | ResizeHandle.BottomRight)) != 0;

            if (topAnchor)
            {
                newY = original.Y + dy;
                newH = original.Height - dy;
                if (newH < MinControlSize)
                {
                    newY = original.Y + original.Height - MinControlSize;
                    newH = MinControlSize;
                }
            }
            else if (bottomAnchor)
            {
                newH = original.Height + dy;
                if (newH < MinControlSize) newH = MinControlSize;
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
}
