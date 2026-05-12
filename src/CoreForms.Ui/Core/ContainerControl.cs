using CoreForms.Ui.Core;

namespace CoreForms.Ui.Core;

/// <summary>
/// A control that can contain other controls.
/// </summary>
public class ContainerControl : Control
{
    private Control? _activeControl;

    /// <summary>
    /// Gets or sets the currently active control within the container.
    /// </summary>
    public Control? ActiveControl
    {
        get => _activeControl;
        set
        {
            if (_activeControl != value)
            {
                if (_activeControl != null)
                    _activeControl.Focused = false;
                _activeControl = value;
                if (_activeControl != null)
                    _activeControl.Focused = true;
            }
        }
    }

    /// <summary>
    /// Gets the direct child control at the specified point.
    /// Does not recurse into nested containers — only checks immediate children.
    /// </summary>
    /// <param name="point">The point to test, in this container's coordinate space.</param>
    /// <returns>The direct child control at the point, or null if none found.</returns>
    protected virtual Control? GetChildAtPoint(Point point)
    {
        for (int i = Controls.Count - 1; i >= 0; i--)
        {
            var child = Controls[i];
            if (child.Visible && child.HitTest(point))
            {
                return child;
            }
        }
        return null;
    }

    /// <summary>
    /// Finds the deepest child control at the specified point and computes its local coordinates.
    /// Recursively descends through nested containers, tracking coordinate transformations at each level.
    /// Uses GetChildAtPoint (virtual) so that derived classes like TabControl can apply
    /// coordinate transformations (e.g., tab header offset).
    /// </summary>
    /// <param name="point">The point in this container's coordinate space.</param>
    /// <param name="localPoint">The resulting point in the deepest child's coordinate space.</param>
    /// <returns>The deepest child control, or null if none found.</returns>
    protected virtual Control? GetDeepestChildAtPoint(Point point, out Point localPoint)
    {
        localPoint = point;
        var child = GetChildAtPoint(point);
        if (child != null)
        {
            var childLocal = new Point(point.X - child.X, point.Y - child.Y);
            if (child is ContainerControl container)
            {
                var deepest = container.GetDeepestChildAtPoint(childLocal, out var deepestLocal);
                if (deepest != null)
                {
                    localPoint = deepestLocal;
                    return deepest;
                }
            }
            localPoint = childLocal;
            return child;
        }
        return null;
    }

    /// <summary>
    /// Raises the MouseDown event, routing to the appropriate child control.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseDown(EventArgs e)
    {
        var args = e as MouseEventArgs;
        if (args != null)
        {
            var point = new Point(args.X, args.Y);
            var target = GetDeepestChildAtPoint(point, out var localPoint);
            if (target != null)
            {
                var localArgs = new MouseEventArgs(args.Button, args.Clicks, localPoint.X, localPoint.Y, args.Delta);
                SetActiveControlRecursive(target);
                target.OnMouseDown(localArgs);
                return;
            }
        }
        base.OnMouseDown(e);
    }

    /// <summary>
    /// Raises the MouseUp event, routing to the appropriate child control.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseUp(EventArgs e)
    {
        var args = e as MouseEventArgs;
        if (args != null)
        {
            var point = new Point(args.X, args.Y);
            var target = GetDeepestChildAtPoint(point, out var localPoint);
            if (target != null)
            {
                var localArgs = new MouseEventArgs(args.Button, args.Clicks, localPoint.X, localPoint.Y, args.Delta);
                target.OnMouseUp(localArgs);
                return;
            }
        }
        base.OnMouseUp(e);
    }

    /// <summary>
    /// Raises the MouseMove event, routing to the appropriate child control.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseMove(EventArgs e)
    {
        var args = e as MouseEventArgs;
        if (args != null)
        {
            var point = new Point(args.X, args.Y);
            var target = GetDeepestChildAtPoint(point, out var localPoint);
            if (target != null)
            {
                var localArgs = new MouseEventArgs(args.Button, args.Clicks, localPoint.X, localPoint.Y, args.Delta);
                target.OnMouseMove(localArgs);
                return;
            }
        }
        base.OnMouseMove(e);
    }

    /// <summary>
    /// Raises the MouseWheel event, routing to the appropriate child control.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseWheel(EventArgs e)
    {
        var args = e as MouseEventArgs;
        if (args != null)
        {
            var point = new Point(args.X, args.Y);
            var target = GetDeepestChildAtPoint(point, out _);
            if (target != null)
            {
                target.OnMouseWheel(e);
                return;
            }
        }
        base.OnMouseWheel(e);
    }

    /// <summary>
    /// Sets the active control, walking up through container controls to the form level.
    /// </summary>
    /// <param name="control">The control to activate.</param>
    private void SetActiveControlRecursive(Control control)
    {
        if (control is ContainerControl container)
        {
            ActiveControl = container;
        }
        else
        {
            ActiveControl = control;
        }
    }

    /// <summary>
    /// Raises the KeyDown event, routing to the active control.
    /// </summary>
    /// <param name="e">A KeyEventArgs that contains the event data.</param>
    protected internal override void OnKeyDown(KeyEventArgs e)
    {
        if (ActiveControl != null)
        {
            ActiveControl.OnKeyDown(e);
            if (e.Handled) return;
        }
        base.OnKeyDown(e);
    }

    /// <summary>
    /// Raises the KeyUp event, routing to the active control.
    /// </summary>
    /// <param name="e">A KeyEventArgs that contains the event data.</param>
    protected internal override void OnKeyUp(KeyEventArgs e)
    {
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
        if (ActiveControl != null)
        {
            ActiveControl.OnKeyPress(e);
            if (e.Handled) return;
        }
        base.OnKeyPress(e);
    }

    /// <summary>
    /// Raises the TextInput event, routing to the active control.
    /// </summary>
    /// <param name="text">The input text.</param>
    protected internal override void OnTextInput(string text)
    {
        if (ActiveControl != null)
        {
            ActiveControl.OnTextInput(text);
        }
    }
}