using OldSchoolForms.Ui.Controls.Advanced;
using OldSchoolForms.Ui.Controls.Basic;
using OldSchoolForms.Ui.Core;

namespace OldSchoolForms.Ui.Demo;

public class DebugForm : Form
{
    private TextBox _textBox;
    private TabControl _tabControl;
    
    public DebugForm()
    {
        Size = new Size(800, 600);
        Text = "Debug Form";
        FormBorderStyle = FormBorderStyle.Sizable;

        var page1 = new TabPage() { Text = "Tab1", Name = "page1" };
        
        _tabControl = new TabControl()
        {
            Dock = DockStyle.Fill
        };
        
        _tabControl.AddTabPage(page1);
        
        _textBox = new TextBox()
        {
            Location = new Point(20, 300),
            Width = 400,
            Text = "Enter text here..."
        };
        
        page1.Controls.Add(_textBox);
        
        Controls.Add(_tabControl);
    }
}