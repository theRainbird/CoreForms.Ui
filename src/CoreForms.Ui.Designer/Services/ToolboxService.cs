using System;
using System.Collections.Generic;
using CoreForms.Ui.Core;
using CoreForms.Ui.Controls.Basic;
using CoreForms.Ui.Controls;
using CoreForms.Ui.Controls.Containers;
using CoreForms.Ui.Controls.Advanced;
using CoreForms.Ui.Layout;

namespace CoreForms.Ui.Designer.Services;

/// <summary>
/// Represents a registered control type in the toolbox.
/// </summary>
public class ToolboxItem
{
    /// <summary>
    /// Gets the display name shown in the toolbox.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the category (e.g. "Basic", "Containers", "Advanced").
    /// </summary>
    public string Category { get; }

    /// <summary>
    /// Gets the control type.
    /// </summary>
    public Type ControlType { get; }

    /// <summary>
    /// Gets the default size for newly created controls of this type.
    /// </summary>
    public Size DefaultSize { get; }

    /// <summary>
    /// Initializes a new toolbox item.
    /// </summary>
    public ToolboxItem(string displayName, string category, Type controlType, Size defaultSize)
    {
        DisplayName = displayName;
        Category = category;
        ControlType = controlType;
        DefaultSize = defaultSize;
    }
}

/// <summary>
/// Manages the palette of controls available for placement on the design surface.
/// Provides factory methods to create control instances and tracks default sizes.
/// </summary>
public class ToolboxService
{
    private readonly List<ToolboxItem> _items = new();

    /// <summary>
    /// Raised when the toolbox contents change.
    /// </summary>
    public event EventHandler? ToolboxChanged;

    /// <summary>
    /// Gets the list of all registered toolbox items.
    /// </summary>
    public IReadOnlyList<ToolboxItem> Items => _items.AsReadOnly();

    /// <summary>
    /// Initializes the toolbox with the standard set of CoreForms controls.
    /// </summary>
    public ToolboxService()
    {
        RegisterStandardControls();
    }

    /// <summary>
    /// Registers a new control type in the toolbox.
    /// </summary>
    public void Register(ToolboxItem item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        _items.Add(item);
        OnToolboxChanged();
    }

    /// <summary>
    /// Creates a control instance for the specified toolbox item.
    /// The control is assigned a default name based on its type.
    /// </summary>
    public Control CreateControl(ToolboxItem item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));

        var control = (Control)Activator.CreateInstance(item.ControlType)!;
        control.Name = GenerateControlName(control);
        control.Size = item.DefaultSize;
        return control;
    }

    /// <summary>
    /// Creates a control instance by type name.
    /// </summary>
    public Control? CreateControl(string typeName)
    {
        foreach (var item in _items)
        {
            if (item.DisplayName == typeName || item.ControlType.Name == typeName)
                return CreateControl(item);
        }
        return null;
    }

    private void RegisterStandardControls()
    {
        Register(new ToolboxItem("Button", "Basic", typeof(Button), new Size(120, 40)));
        Register(new ToolboxItem("Label", "Basic", typeof(Label), new Size(150, 28)));
        Register(new ToolboxItem("TextBox", "Basic", typeof(TextBox), new Size(200, 28)));
        Register(new ToolboxItem("CheckBox", "Basic", typeof(CheckBox), new Size(120, 28)));
        Register(new ToolboxItem("RadioButton", "Basic", typeof(RadioButton), new Size(120, 28)));
        Register(new ToolboxItem("ComboBox", "Basic", typeof(ComboBox), new Size(200, 28)));
        Register(new ToolboxItem("ListBox", "Basic", typeof(ListBox), new Size(160, 100)));
        Register(new ToolboxItem("ProgressBar", "Basic", typeof(ProgressBar), new Size(200, 28)));
        Register(new ToolboxItem("DateTimePicker", "Basic", typeof(DateTimePicker), new Size(200, 28)));

        Register(new ToolboxItem("Panel", "Containers", typeof(Panel), new Size(300, 200)));
        Register(new ToolboxItem("GroupBox", "Containers", typeof(GroupBox), new Size(300, 200)));
        Register(new ToolboxItem("TabControl", "Containers", typeof(TabControl), new Size(400, 300)));
        Register(new ToolboxItem("SplitPanel", "Containers", typeof(SplitPanel), new Size(400, 300)));
        Register(new ToolboxItem("FlowLayoutPanel", "Containers", typeof(FlowLayoutPanel), new Size(400, 300)));

        Register(new ToolboxItem("DataGridView", "Advanced", typeof(DataGridView), new Size(400, 200)));
        Register(new ToolboxItem("TreeView", "Advanced", typeof(TreeView), new Size(200, 200)));
        Register(new ToolboxItem("CalendarView", "Advanced", typeof(CalendarView), new Size(400, 300)));
        Register(new ToolboxItem("HtmlBox", "Advanced", typeof(HtmlBox), new Size(400, 300)));
    }

    private static string GenerateControlName(Control control)
    {
        string baseName = control.GetType().Name;
        return baseName.ToLowerInvariant()[0] + baseName[1..];
    }

    private void OnToolboxChanged() => ToolboxChanged?.Invoke(this, EventArgs.Empty);
}
