using System;
using System.IO;
using CoreForms.Ui.Core;
using CoreForms.Ui.Controls.Basic;
using CoreForms.Ui.Controls.Containers;
using CoreForms.Ui.Designer.PropertyGrid;
using CoreForms.Ui.Designer.Services;
using CoreForms.Ui.Theming;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Designer.Standalone;

/// <summary>
/// The main window of the CoreForms Form Designer standalone application.
/// Hosts the toolbox, design surface, and menu bar.
/// </summary>
public class DesignerForm : Form
{
    private readonly ToolboxService _toolboxService;
    private readonly DesignSurface _designSurface;
    private readonly PropertyGrid.PropertyGrid _propertyGrid;
    private ToolboxControl _toolbox = null!;
    private Panel _toolboxPanel = null!;
    private StatusStripPanel _statusPanel = null!;
    private Label _statusLabel = null!;
    private Label _positionLabel = null!;

    private bool _showGrid = true;
    private bool _snapToGrid = true;

    private const int ToolboxWidth = 220;
    private const int StatusBarHeight = 24;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public DesignerForm()
    {
        Title = "CoreForms Form Designer";
        Width = 1400;
        Height = 900;
        Zoom = 1.0f;

        _toolboxService = new ToolboxService();
        _designSurface = new DesignSurface();
        _propertyGrid = new PropertyGrid.PropertyGrid(_designSurface.Selection);

        BuildUI();
        WireEvents();
    }

    private void BuildUI()
    {
        SuspendLayout();

        var menuStrip = BuildMenuStrip();
        Controls.Add(menuStrip);

        _toolboxPanel = new Panel
        {
            Dock = DockStyle.Left,
            Width = ToolboxWidth,
            Padding = new Padding(0, 0, 4, 0)
        };
        _toolbox = new ToolboxControl(_toolboxService)
        {
            Dock = DockStyle.Fill
        };
        _toolboxPanel.Controls.Add(_toolbox);
        Controls.Add(_toolboxPanel);

        _designSurface.Dock = DockStyle.Fill;
        _designSurface.BackColor = Color.FromArgb(240, 240, 240);
        Controls.Add(_designSurface);

        var propertyPanel = new Panel
        {
            Dock = DockStyle.Right,
            Width = 280,
            Padding = new Padding(4, 0, 0, 0)
        };
        _propertyGrid.Dock = DockStyle.Fill;
        propertyPanel.Controls.Add(_propertyGrid);
        Controls.Add(propertyPanel);

        BuildStatusStrip();
        Controls.Add(_statusPanel!);

        ResumeLayout();
    }

    private MenuStrip BuildMenuStrip()
    {
        var menu = new MenuStrip();
        menu.Dock = DockStyle.Top;

        var fileItem = new ToolStripMenuItem("&Datei");
        var newItem = new ToolStripMenuItem("&Neu");
        newItem.Click += (_, _) => NewForm();
        fileItem.DropDownItems.Add(newItem);
        var openItem = new ToolStripMenuItem("&Öffnen...");
        openItem.Click += (_, _) => OpenForm();
        fileItem.DropDownItems.Add(openItem);
        var saveItem = new ToolStripMenuItem("&Speichern");
        saveItem.Click += (_, _) => SaveForm();
        fileItem.DropDownItems.Add(saveItem);
        var saveAsItem = new ToolStripMenuItem("Speichern &unter...");
        saveAsItem.Click += (_, _) => SaveFormAs();
        fileItem.DropDownItems.Add(saveAsItem);
        fileItem.DropDownItems.Add(new ToolStripMenuItem("-"));
        var exitItem = new ToolStripMenuItem("Be&enden");
        exitItem.Click += (_, _) => Close();
        fileItem.DropDownItems.Add(exitItem);

        var editItem = new ToolStripMenuItem("&Bearbeiten");
        var undoItem = new ToolStripMenuItem("&Rückgängig");
        undoItem.Click += (_, _) => _designSurface.Undo.Undo();
        editItem.DropDownItems.Add(undoItem);
        var redoItem = new ToolStripMenuItem("&Wiederholen");
        redoItem.Click += (_, _) => _designSurface.Undo.Redo();
        editItem.DropDownItems.Add(redoItem);
        editItem.DropDownItems.Add(new ToolStripMenuItem("-"));
        var deleteItem = new ToolStripMenuItem("&Löschen");
        deleteItem.Click += (_, _) => _designSurface.DeleteSelected();
        editItem.DropDownItems.Add(deleteItem);
        var selectAllItem = new ToolStripMenuItem("&Alles auswählen");
        selectAllItem.Click += (_, _) => _designSurface.Selection.SelectAll(_designSurface.Items);
        editItem.DropDownItems.Add(selectAllItem);

        var viewItem = new ToolStripMenuItem("&Ansicht");
        var showGridItem = new ToolStripMenuItem("&Raster anzeigen");
        showGridItem.Click += (_, _) =>
        {
            _showGrid = !_showGrid;
            _designSurface.ShowGrid = _showGrid;
        };
        viewItem.DropDownItems.Add(showGridItem);
        var snapGridItem = new ToolStripMenuItem("Am &Raster ausrichten");
        snapGridItem.Click += (_, _) =>
        {
            _snapToGrid = !_snapToGrid;
            _designSurface.SnapToGrid = _snapToGrid;
        };
        viewItem.DropDownItems.Add(snapGridItem);

        var helpItem = new ToolStripMenuItem("&Hilfe");
        var aboutItem = new ToolStripMenuItem("&Info");
        aboutItem.Click += (_, _) =>
            MessageBox.Show("CoreForms Form Designer\nVersion 1.0\n\nEin grafischer Formular-Designer für CoreForms.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        helpItem.DropDownItems.Add(aboutItem);

        menu.Items.Add(fileItem);
        menu.Items.Add(editItem);
        menu.Items.Add(viewItem);
        menu.Items.Add(helpItem);

        return menu;
    }

    private void BuildStatusStrip()
    {
        _statusPanel = new StatusStripPanel();
        _statusPanel.Dock = DockStyle.Bottom;
        _statusPanel.Height = StatusBarHeight;

        _statusLabel = new Label
        {
            Text = "Bereit",
            Dock = DockStyle.Left,
            BackColor = Color.Transparent,
            Width = 400
        };

        _positionLabel = new Label
        {
            Text = "",
            Dock = DockStyle.Right,
            BackColor = Color.Transparent,
            Width = 250
        };

        _statusPanel.Controls.Add(_statusLabel);
        _statusPanel.Controls.Add(_positionLabel);
    }

    private void WireEvents()
    {
        _toolbox.DragStarted += (_, e) =>
        {
            _designSurface.BeginExternalDrop(e.Item);
            _statusLabel.Text = $"Platziere {e.Item.DisplayName} – Klicken Sie auf die Designfläche";
        };

        _designSurface.SelectionChanged += (_, _) =>
        {
            var primary = _designSurface.Selection.PrimarySelection;
            _statusLabel.Text = primary != null
                ? $"Selektiert: {primary.Control.Name ?? primary.Control.GetType().Name}"
                : "Keine Auswahl";
        };

        _designSurface.ContentModified += (_, _) =>
        {
            _statusLabel.Text = "Geändert";
        };
    }

    private Point ScreenToDesignSurface(Point screenPoint)
    {
        var origin = _designSurface.PointToScreen(Point.Empty);
        return new Point(
            screenPoint.X - origin.X,
            screenPoint.Y - origin.Y);
    }

    private void NewForm()
    {
        _designSurface.ClearAll();
        _statusLabel.Text = "Neues Formular";
        Title = "CoreForms Form Designer - [Unbenannt]";
    }

    private void OpenForm()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Formular öffnen",
            Filter = "C# Dateien (*.cs)|*.cs|Alle Dateien (*.*)|*.*"
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            try
            {
                _designSurface.ClearAll();
                var parser = new Roslyn.FormParser();
                var warnings = parser.Parse(File.ReadAllText(dialog.FileName), _designSurface);
                _statusLabel.Text = $"Geöffnet: {dialog.FileName}";
                Title = $"CoreForms Form Designer - [{dialog.FileName}]";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Öffnen: {ex.Message}", "Fehler",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void SaveForm()
    {
        var generator = new Roslyn.CodeGenerator(_designSurface);
        string code = generator.Generate("CoreForms.Ui.Demo", "MainForm");
        _statusLabel.Text = "Gespeichert (Vorschau generiert)";
    }

    private void SaveFormAs()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Formular speichern",
            Filter = "C# Dateien (*.cs)|*.cs|Alle Dateien (*.*)|*.*"
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            var generator = new Roslyn.CodeGenerator(_designSurface);
            string code = generator.Generate("CoreForms.Ui.Demo", "MainForm");
            File.WriteAllText(dialog.FileName, code);
            _statusLabel.Text = $"Gespeichert: {dialog.FileName}";
            Title = $"CoreForms Form Designer - [{dialog.FileName}]";
        }
    }
}

/// <summary>
/// A status strip panel with a top border line.
/// </summary>
internal class StatusStripPanel : Panel
{
    public StatusStripPanel()
    {
        BackColor = ThemeManager.CurrentTheme.ControlBackground;
        Height = 24;
    }

    public override void Render(Graphics g)
    {
        var theme = ThemeManager.CurrentTheme;
        g.DrawLine(theme.StatusStripTopLine, 0, 0, Width, 0);
        g.FillRectangle(theme.ControlBackground, 0, 1, Width, Height - 1);
        base.Render(g);
    }
}
