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
    /// Gets the child control at the specified point.
    /// </summary>
    /// <param name="point">The point to test.</param>
    /// <returns>The child control at the specified point, or null if none found.</returns>
    protected Control? GetChildAtPoint(Point point)
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
    /// Raises the MouseDown event, routing to the appropriate child control.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseDown(EventArgs e)
    {
        var args = e as MouseEventArgs;
        if (args != null)
        {
            var point = new Point(args.X, args.Y);
            var target = GetChildAtPoint(point);
            if (target != null)
            {
                var localArgs = new MouseEventArgs(args.Button, args.Clicks, args.X - target.X, args.Y - target.Y, args.Delta);
                target.OnMouseDown(localArgs);
                ActiveControl = target;
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
            var target = GetChildAtPoint(new Point(args.X, args.Y));
            if (target != null)
            {
                var localArgs = new MouseEventArgs(args.Button, args.Clicks, args.X - target.X, args.Y - target.Y, args.Delta);
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
            var target = GetChildAtPoint(new Point(args.X, args.Y));
            if (target != null)
            {
                var localArgs = new MouseEventArgs(args.Button, args.Clicks, args.X - target.X, args.Y - target.Y, args.Delta);
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
            var target = GetChildAtPoint(new Point(args.X, args.Y));
            if (target != null)
            {
                target.OnMouseWheel(e);
                return;
            }
        }
        base.OnMouseWheel(e);
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