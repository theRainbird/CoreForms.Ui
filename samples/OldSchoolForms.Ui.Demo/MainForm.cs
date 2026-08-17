using System.Globalization;
using OldSchoolForms.Ui.Controls;
using OldSchoolForms.Ui.Controls.Advanced;
using OldSchoolForms.Ui.Controls.Basic;
using OldSchoolForms.Ui.Controls.Containers;
using OldSchoolForms.Ui.Core;
using OldSchoolForms.Ui.Demo.UserControls;
using OldSchoolForms.Ui.Resources;
using OldSchoolForms.Ui.Theming;
using Graphics = OldSchoolForms.Ui.Rendering.Graphics;

namespace OldSchoolForms.Ui.Demo;

/// <summary>
/// The main form of the demo application, hosting menus, toolbars, status bar, and tabbed content.
/// </summary>
public class MainForm : Form
{
    private Label _statusLabel = null!;
    private DebugForm _debugForm = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainForm"/> class.
    /// </summary>
    public MainForm()
    {
        Text = SR.GetString("FormTitle");
        Width = 1600;
        Height = 1080;
        Zoom = 1.25f;
        
        PopulateForm();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {   
        if (_debugForm != null)
            _debugForm.Dispose();
        
        base.OnFormClosing(e);
    }

    private void PopulateForm()
    {
        SuspendLayout();
        Controls.Clear();
        _statusLabel = null;

        var menuStrip = CreateMenuStrip();
        var toolStrip = CreateToolStrip();
        var statusStrip = CreateStatusStrip();
        var mainTabControl = CreateMainTabControl();

        Controls.Add(menuStrip);
        Controls.Add(toolStrip);
        Controls.Add(statusStrip);
        Controls.Add(mainTabControl);

        Text = SR.GetString("FormTitle");
        ResumeLayout();
    }

    private MenuStrip CreateMenuStrip()
    {
        var menuStrip = new MenuStrip();
        menuStrip.Dock = DockStyle.Top;
        menuStrip.Size = new Size(900, 30);

        var fileItem = new ToolStripMenuItem(SR.GetString("MenuFile"));
        var fileNewItem = new ToolStripMenuItem(SR.GetString("MenuNew"));
        fileNewItem.Click += (s, e) => MessageBox.Show(SR.GetString("MsgTextNewFile"), SR.GetString("MsgTitleNewFile"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        var fileOpenItem = new ToolStripMenuItem(SR.GetString("MenuOpen"));
        fileOpenItem.Click += (s, e) => MessageBox.Show(SR.GetString("MsgTextOpen"), SR.GetString("MsgTitleOpen"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        var fileSaveItem = new ToolStripMenuItem(SR.GetString("MenuSave"));
        fileSaveItem.Click += (s, e) => MessageBox.Show(SR.GetString("MsgTextSaved"), SR.GetString("MsgTitleSave"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        var fileExitItem = new ToolStripMenuItem(SR.GetString("MenuExit"));
        fileExitItem.Click += (s, e) =>
        {
            var result = MessageBox.Show(SR.GetString("MsgTextExit"), SR.GetString("MsgTitleExit"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
                Application.Exit();
        };
        fileItem.DropDownItems.Add(fileNewItem);
        fileItem.DropDownItems.Add(fileOpenItem);
        fileItem.DropDownItems.Add(fileSaveItem);
        fileItem.DropDownItems.Add(fileExitItem);
        menuStrip.Items.Add(fileItem);

        var editItem = new ToolStripMenuItem(SR.GetString("MenuEdit"));
        var editUndoItem = new ToolStripMenuItem(SR.GetString("MenuUndo"));
        editUndoItem.Click += (s, e) => MessageBox.Show(SR.GetString("MsgTextUndo"), SR.GetString("MsgTitleUndo"), MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
        var editRedoItem = new ToolStripMenuItem(SR.GetString("MenuRedo"));
        editRedoItem.Click += (s, e) => MessageBox.Show(SR.GetString("MsgTextRedo"), SR.GetString("MsgTitleRedo"), MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
        var editDeleteItem = new ToolStripMenuItem(SR.GetString("MenuDelete"));
        editDeleteItem.Click += (s, e) => MessageBox.Show(SR.GetString("MsgTextDelete"), SR.GetString("MsgTitleDelete"), MessageBoxButtons.YesNoCancel, MessageBoxIcon.Error);
        var editSep1 = new ToolStripMenuItem("-");
        var editCutItem = new ToolStripMenuItem(SR.GetString("MenuCut"));
        editCutItem.Click += (s, e) => {
            if (ActiveControl is TextBox tb)
            {
                try { tb.Cut(); }
                catch (Exception ex) { MessageBox.Show($"Clipboard error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        };
        var editCopyItem = new ToolStripMenuItem(SR.GetString("MenuCopy"));
        editCopyItem.Click += (s, e) => {
            if (ActiveControl != null)
            {
                try {
                    var method = ActiveControl.GetType().GetMethod("CopyToClipboard");
                    method?.Invoke(ActiveControl, null);
                } catch (Exception ex) { MessageBox.Show(string.Format(SR.GetString("MsgTextClipboardError"), ex.Message), SR.GetString("MsgTitleError"), MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        };
        var editPasteItem = new ToolStripMenuItem(SR.GetString("MenuPaste"));
        editPasteItem.Click += (s, e) => {
            if (ActiveControl is TextBox tb)
            {
                try { tb.Paste(); }
                catch (Exception ex) { MessageBox.Show(string.Format(SR.GetString("MsgTextClipboardError"), ex.Message), SR.GetString("MsgTitleError"), MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        };
        var editSelectAllItem = new ToolStripMenuItem(SR.GetString("MenuSelectAll"));
        editSelectAllItem.Click += (s, e) => {
            if (ActiveControl is TextBox tb)
                tb.SelectAll();
        };
        editItem.DropDownItems.Add(editUndoItem);
        editItem.DropDownItems.Add(editRedoItem);
        editItem.DropDownItems.Add(editDeleteItem);
        editItem.DropDownItems.Add(editSep1);
        editItem.DropDownItems.Add(editCutItem);
        editItem.DropDownItems.Add(editCopyItem);
        editItem.DropDownItems.Add(editPasteItem);
        editItem.DropDownItems.Add(editSelectAllItem);
        menuStrip.Items.Add(editItem);

        var viewItem = new ToolStripMenuItem(SR.GetString("MenuView"));
        var viewRefreshItem = new ToolStripMenuItem(SR.GetString("MenuRefresh"));
        viewRefreshItem.Click += (s, e) => MessageBox.Show(SR.GetString("MsgTextRefreshed"), SR.GetString("MsgTitleRefresh"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        var viewFullscreenItem = new ToolStripMenuItem(SR.GetString("MenuFullscreen"));
        viewFullscreenItem.Click += (s, e) => MessageBox.Show(SR.GetString("MsgTextFullscreen"), SR.GetString("MsgTitleFullscreen"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        viewItem.DropDownItems.Add(viewRefreshItem);
        viewItem.DropDownItems.Add(viewFullscreenItem);
        menuStrip.Items.Add(viewItem);

        var helpItem = new ToolStripMenuItem(SR.GetString("MenuHelp"));
        var helpAboutItem = new ToolStripMenuItem(SR.GetString("MenuAbout"));
        helpAboutItem.Click += (s, e) => MessageBox.Show(string.Format(SR.GetString("MsgTextAbout"), "\n"), SR.GetString("MsgTitleAbout"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        var helpLicenseItem = new ToolStripMenuItem(SR.GetString("MenuLicense"));
        helpLicenseItem.Click += (s, e) => MessageBox.Show(SR.GetString("MsgTextLicense"), SR.GetString("MsgTitleLicenseError"), MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Error);
        helpItem.DropDownItems.Add(helpAboutItem);
        helpItem.DropDownItems.Add(helpLicenseItem);
        menuStrip.Items.Add(helpItem);

        var languageItem = new ToolStripMenuItem(SR.GetString("MenuLanguage"));
        languageItem.DropDownItems.Add(CreateLanguageItem("English", "en"));
        languageItem.DropDownItems.Add(CreateLanguageItem("Deutsch", "de"));
        languageItem.DropDownItems.Add(CreateLanguageItem("Français", "fr"));
        languageItem.DropDownItems.Add(CreateLanguageItem("Italiano", "it"));
        languageItem.DropDownItems.Add(CreateLanguageItem("Español", "es"));
        languageItem.DropDownItems.Add(CreateLanguageItem("Русский", "ru"));
        menuStrip.Items.Add(languageItem);

        return menuStrip;
    }

    private ToolStrip CreateToolStrip()
    {
        var toolStrip = new ToolStrip();
        toolStrip.Dock = DockStyle.Top;

        var newButton = new ToolStripButton(SR.GetString("ToolNew"), Icons.DocumentAdd24!);
        newButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
        newButton.Click += (s, e) =>
        {
            _statusLabel!.Text = SR.GetString("StatusNewClicked");

            if (_debugForm != null)
            {
                _debugForm.Dispose();
            }
            
            _debugForm = new DebugForm();
            _debugForm.ShowDialog(this);
        };
        toolStrip.Items.Add(newButton);

        var openButton = new ToolStripButton(SR.GetString("ToolOpen"), Icons.Document24!);
        openButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
        openButton.Click += (s, e) => _statusLabel!.Text = SR.GetString("StatusOpenClicked");
        toolStrip.Items.Add(openButton);

        var saveButton = new ToolStripButton(SR.GetString("ToolSave"), Icons.DocumentEdit24!);
        saveButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
        saveButton.Click += (s, e) => _statusLabel!.Text = SR.GetString("StatusSaveClicked");
        toolStrip.Items.Add(saveButton);

        toolStrip.Items.Add(new ToolStripSeparator());

        var searchBox = new ToolStripTextBox();
        searchBox.TextBoxWidth = 120;
        searchBox.TextChanged += (s, e) => _statusLabel!.Text = string.Format(SR.GetString("StatusSearchFormat"), searchBox.Text);
        toolStrip.Items.Add(searchBox);

        var searchButton = new ToolStripButton(SR.GetString("ToolSearch"), Icons.SearchSparkle24!);
        searchButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
        searchButton.Click += (s, e) => _statusLabel!.Text = string.Format(SR.GetString("StatusSearchingFormat"), searchBox.Text);
        toolStrip.Items.Add(searchButton);

        toolStrip.Items.Add(new ToolStripSeparator());

        var zoomLabel = new ToolStripLabel(SR.GetString("ToolZoomLabel"));
        toolStrip.Items.Add(zoomLabel);

        var zoomComboBox = new ToolStripLabel(SR.GetString("ToolZoom100"));
        zoomComboBox.IsLink = true;
        zoomComboBox.Click += (s, e) =>
        {
            Zoom = Zoom == 1.0f ? 1.5f : 1.0f;
            zoomComboBox.Text = $"{(int)(Zoom * 100)}%";
            _statusLabel!.Text = string.Format(SR.GetString("StatusZoomFormat"), zoomComboBox.Text);
        };
        toolStrip.Items.Add(zoomComboBox);

        toolStrip.Items.Add(new ToolStripSeparator());

        ToolStripButton? lightButton = null;
        ToolStripButton? darkButton = null;

        lightButton = new ToolStripButton(SR.GetString("ToolLight"), Icons.WeatherSunnyLow24!);
        lightButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
        lightButton.Checked = true;
        lightButton.CheckOnClick = true;
        lightButton.CheckedChanged += (s, e) =>
        {
            if (lightButton.Checked)
            {
                ThemeManager.SetTheme(new LightTheme());
                darkButton!.Checked = false;
                _statusLabel!.Text = SR.GetString("StatusThemeLight");
            }
        };
        toolStrip.Items.Add(lightButton);

        darkButton = new ToolStripButton(SR.GetString("ToolDark"), Icons.WeatherSnowflake24!);
        darkButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
        darkButton.CheckOnClick = true;
        darkButton.CheckedChanged += (s, e) =>
        {
            if (darkButton.Checked)
            {
                ThemeManager.SetTheme(new DarkTheme());
                lightButton!.Checked = false;
                _statusLabel!.Text = SR.GetString("StatusThemeDark");
            }
        };
        toolStrip.Items.Add(darkButton);

        toolStrip.Items.Add(new ToolStripSeparator());

        var toggleEnabledButton = new ToolStripButton(SR.GetString("ToolToggleEnabled"));
        toggleEnabledButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
        toggleEnabledButton.Click += (s, e) =>
        {
            ToggleControlsEnabled(this, toolStrip);
            _statusLabel!.Text = SR.GetString("StatusToggled");
        };
        toolStrip.Items.Add(toggleEnabledButton);

        toolStrip.Items.Add(new ToolStripSeparator());

        var helpButton = new ToolStripButton(SR.GetString("ToolHelp"), Icons.QuestionCircle24!);
        helpButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
        helpButton.Click += (s, e) => MessageBox.Show(string.Format(SR.GetString("MsgTextToolStripDemo"), "\n"), SR.GetString("MsgTitleHelp"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        toolStrip.Items.Add(helpButton);

        return toolStrip;
    }

    private Panel CreateStatusStrip()
    {
        _statusLabel = new Label
        {
            Text = SR.GetString("StatusReady"),
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent
        };

        var statusStrip = new Panel
        {
            BackColor = ThemeManager.CurrentTheme.ControlBackground,
            Size = new Size(900, 24)
        };
        statusStrip.Dock = DockStyle.Bottom;
        statusStrip.Padding = new Padding(8, 0, 8, 0);
        statusStrip.Controls.Add(_statusLabel);

        return statusStrip;
    }

    private TabControl CreateMainTabControl()
    {
        var tabControl = new TabControl();
        tabControl.Dock = DockStyle.Fill;

        tabControl.AddTabPage(CreateTabPage(SR.GetString("TabBasicControls"), CreateBasicControlsPage()));
        tabControl.AddTabPage(CreateTabPage(SR.GetString("TabDataGrid"), CreateDataGridPage()));
        tabControl.AddTabPage(CreateTabPage(SR.GetString("TabPivotTable"), CreatePivotTablePage()));
        tabControl.AddTabPage(CreateTabPage(SR.GetString("TabDataBinding"), CreateDataBindingPage()));
        tabControl.AddTabPage(CreateTabPage(SR.GetString("TabMessageBoxes"), CreateMessageBoxPage()));
        tabControl.AddTabPage(CreateTabPage(SR.GetString("TabFileDialogs"), CreateFileDialogPage()));
        tabControl.AddTabPage(CreateTabPage(SR.GetString("TabPrintDialog"), CreatePrintDialogPage()));
        tabControl.AddTabPage(CreateTabPage(SR.GetString("TabDockAnchor"), CreateDockAnchorPage()));
        tabControl.AddTabPage(CreateTabPage("Flow/Table Layout", CreateLayoutPage()));
        tabControl.AddTabPage(CreateTabPage(SR.GetString("TabTreeView"), CreateTreeViewPage()));
        tabControl.AddTabPage(CreateTabPage(SR.GetString("TabUserControl"), CreateUserControlPage()));
        tabControl.AddTabPage(CreateTabPage(SR.GetString("TabImages"), CreateImagesPage()));
        tabControl.AddTabPage(CreateTabPage(SR.GetString("TabSplitPanel"), CreateSplitPanelPage()));
        tabControl.AddTabPage(CreateTabPage(SR.GetString("TabWebBrowser"), CreateWebBrowserPage()));
        tabControl.AddTabPage(CreateTabPage(SR.GetString("TabHtmlEditor"), CreateHtmlEditorPage()));
        tabControl.AddTabPage(CreateTabPage(SR.GetString("TabCalendar"), CreateCalendarPage()));
        tabControl.AddTabPage(CreateTabPage("Kanban Board", CreateKanbanBoardPage()));
        tabControl.AddTabPage(CreateTabPage(SR.GetString("TabDiagram"), CreateDiagramPage()));
        tabControl.AddTabPage(CreateTabPage(SR.GetString("TabMultiWindows"), CreateMultiWindowPage()));
        tabControl.AddTabPage(CreateTabPage("Reports", CreateReportPage()));

        return tabControl;
    }

    private static TabPage CreateTabPage(string text, Control content)
    {
        var page = new TabPage { Text = text };
        page.Controls.Add(content);
        content.Dock = DockStyle.Fill;
        return page;
    }

    private BasicControlsPage CreateBasicControlsPage()
    {
        var page = new BasicControlsPage();
        page.StatusTextChanged += (s, e) => _statusLabel!.Text = e.Text;
        return page;
    }

    private DataGridPage CreateDataGridPage()
    {
        var page = new DataGridPage();
        page.StatusTextChanged += (s, e) => _statusLabel!.Text = e.Text;
        return page;
    }

    private PivotTablePage CreatePivotTablePage()
    {
        return new PivotTablePage();
    }

    private DataBindingPage CreateDataBindingPage()
    {
        var page = new DataBindingPage();
        return page;
    }

    private MessageBoxPage CreateMessageBoxPage()
    {
        var page = new MessageBoxPage();
        page.StatusTextChanged += (s, e) => _statusLabel!.Text = e.Text;
        return page;
    }

    private FileDialogPage CreateFileDialogPage()
    {
        var page = new FileDialogPage();
        page.StatusTextChanged += (s, e) => _statusLabel!.Text = e.Text;
        return page;
    }

    private PrintDialogPage CreatePrintDialogPage()
    {
        var page = new PrintDialogPage();
        page.StatusTextChanged += (s, e) => _statusLabel!.Text = e.Text;
        return page;
    }

    private static DockAnchorPage CreateDockAnchorPage()
    {
        return new DockAnchorPage();
    }

    private static TreeViewPage CreateTreeViewPage()
    {
        return new TreeViewPage();
    }

    private UserControlDemoPage CreateUserControlPage()
    {
        var page = new UserControlDemoPage();
        page.StatusTextChanged += (s, e) => _statusLabel!.Text = e.Text;
        return page;
    }

    private static ImagesPage CreateImagesPage()
    {
        return new ImagesPage();
    }

    private SplitPanelPage CreateSplitPanelPage()
    {
        var page = new SplitPanelPage();
        page.StatusTextChanged += (s, e) => _statusLabel!.Text = e.Text;
        return page;
    }

    private static WebBrowserPage CreateWebBrowserPage()
    {
        return new WebBrowserPage();
    }

    private HtmlEditorPage CreateHtmlEditorPage()
    {
        var page = new HtmlEditorPage();
        page.StatusTextChanged += (s, e) => _statusLabel!.Text = e.Text;
        return page;
    }

    private CalendarPage CreateCalendarPage()
    {
        var page = new CalendarPage();
        page.StatusTextChanged += (s, e) => _statusLabel!.Text = e.Text;
        return page;
    }

    private KanbanBoardPage CreateKanbanBoardPage()
    {
        return new KanbanBoardPage();
    }

    private DiagramPage CreateDiagramPage()
    {
        var page = new DiagramPage();
        page.StatusTextChanged += (s, e) => _statusLabel!.Text = e.Text;
        return page;
    }

    private MultiWindowPage CreateMultiWindowPage()
    {
        var page = new MultiWindowPage(this);
        page.StatusTextChanged += (s, e) => _statusLabel!.Text = e.Text;
        return page;
    }

    private ReportPage CreateReportPage()
    {
        var page = new ReportPage();
        page.StatusTextChanged += (s, e) => _statusLabel!.Text = e.Text;
        return page;
    }

    private static LayoutPage CreateLayoutPage()
    {
        return new LayoutPage();
    }

    private ToolStripMenuItem CreateLanguageItem(string displayName, string cultureCode)
    {
        var item = new ToolStripMenuItem(displayName);
        item.Click += (s, e) =>
        {
            LocalizationManager.SetCulture(new CultureInfo(cultureCode));
            LocalizationManager.SaveCurrentCulture();
            PopulateForm();
        };
        return item;
    }

    private static void ToggleControlsEnabled(Control parent, Control exclude)
    {
        foreach (var control in parent.Controls)
        {
            if (control == exclude)
                continue;
            control.Enabled = !control.Enabled;
            if (control.Controls.Count > 0)
                ToggleControlsEnabled(control, exclude);
        }
    }
}
