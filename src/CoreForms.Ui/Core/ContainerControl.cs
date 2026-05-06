using CoreForms.Ui.Core;

namespace CoreForms.Ui.Core;

public class ContainerControl : Control
{
    private Control? _activeControl;

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

    protected internal override void OnMouseDown(EventArgs e)
    {
        var args = e as MouseEventArgs;
        if (args != null)
        {
            var target = GetChildAtPoint(new Point(args.X, args.Y));
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

    protected internal override void OnKeyDown(KeyEventArgs e)
    {
        if (ActiveControl != null)
        {
            ActiveControl.OnKeyDown(e);
            if (e.Handled) return;
        }
        base.OnKeyDown(e);
    }

    protected internal override void OnKeyUp(KeyEventArgs e)
    {
        if (ActiveControl != null)
        {
            ActiveControl.OnKeyUp(e);
            if (e.Handled) return;
        }
        base.OnKeyUp(e);
    }

    protected internal override void OnKeyPress(KeyPressEventArgs e)
    {
        if (ActiveControl != null)
        {
            ActiveControl.OnKeyPress(e);
            if (e.Handled) return;
        }
        base.OnKeyPress(e);
    }

    protected internal override void OnTextInput(string text)
    {
        if (ActiveControl != null)
        {
            ActiveControl.OnTextInput(text);
        }
    }
}