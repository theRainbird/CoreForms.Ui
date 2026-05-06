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

    protected internal override void OnMouseDown(EventArgs e)
    {
        base.OnMouseDown(e);
        Focused = true;
    }
}