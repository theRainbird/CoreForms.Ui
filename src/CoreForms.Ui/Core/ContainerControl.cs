namespace CoreForms.Ui.Core;

public class ContainerControl : Control
{
    private Control? _activeControl;
    private bool _focused;

    public Control? ActiveControl
    {
        get => _activeControl;
        set => _activeControl = value;
    }

    public bool Focused
    {
        get => _focused;
        set => _focused = value;
    }

    protected internal override void OnMouseDown(EventArgs e)
    {
        base.OnMouseDown(e);
        Focused = true;
    }
}