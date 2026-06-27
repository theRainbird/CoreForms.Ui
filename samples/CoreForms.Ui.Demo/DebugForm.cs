using CoreForms.Ui.Controls.Basic;
using CoreForms.Ui.Core;

namespace CoreForms.Ui.Demo;

public class DebugForm : Form
{
    private TextBox _textBox;
    
    public DebugForm()
    {
        Size = new Size(800, 600);
        Text = "Debug Form";

        _textBox = new TextBox()
        {
            Location = new Point(20, 560),
            Width = 400,
            Text = "Enter text here..."
        };
        
        Controls.Add(_textBox);
    }
}