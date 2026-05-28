using System;
using System.Collections.Generic;
using System.Linq;
using CoreForms.Ui.Core;
using CoreForms.Ui.Designer.Services;
using CoreForms.Ui.Theming;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Designer.Standalone;

/// <summary>
/// Event args for toolbox drag operations.
/// </summary>
public class ToolboxDragEventArgs : EventArgs
{
    /// <summary>
    /// Gets the toolbox item being dragged.
    /// </summary>
    public ToolboxItem Item { get; }

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="item">The toolbox item being dragged.</param>
    public ToolboxDragEventArgs(ToolboxItem item)
    {
        Item = item;
    }
}

/// <summary>
/// A visual toolbox control that displays available controls grouped by category.
/// Supports initiating drag operations for placing controls on the design surface.
/// </summary>
public class ToolboxControl : ContainerControl
{
    private readonly ToolboxService _toolboxService;
    private readonly List<ToolboxItem> _filteredItems = new();
    private int _hoveredIndex = -1;
    private int _selectedIndex = -1;
    private int _lastMouseDownY;
    private string? _activeCategory;

    private const int HeaderHeight = 24;
    private const int ItemHeight = 26;
    private const int CategoryGap = 20;

    /// <summary>
    /// Raised when the user starts dragging a toolbox item.
    /// </summary>
    public event EventHandler<ToolboxDragEventArgs>? DragStarted;

    /// <summary>
    /// Gets the currently selected toolbox item, or null.
    /// </summary>
    public ToolboxItem? SelectedItem =>
        _selectedIndex >= 0 && _selectedIndex < _filteredItems.Count
            ? _filteredItems[_selectedIndex]
            : null;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="toolboxService">The toolbox service.</param>
    public ToolboxControl(ToolboxService toolboxService)
    {
        _toolboxService = toolboxService ?? throw new ArgumentNullException(nameof(toolboxService));
        BackColor = ThemeManager.CurrentTheme.ControlLight;
        BuildFilteredList();
    }

    /// <summary>
    /// Filters to show only a specific category. Pass null for all.
    /// </summary>
    public void FilterByCategory(string? category)
    {
        _activeCategory = category;
        BuildFilteredList();
        Invalidate();
    }

    private void BuildFilteredList()
    {
        _filteredItems.Clear();
        if (_activeCategory != null)
            _filteredItems.AddRange(_toolboxService.Items.Where(i => i.Category == _activeCategory));
        else
            _filteredItems.AddRange(_toolboxService.Items);
        _selectedIndex = -1;
    }

    protected override void OnMouseDown(EventArgs e)
    {
        if (e is not MouseEventArgs args) return;
        _lastMouseDownY = args.Y;
        _selectedIndex = HitTestItem(args.Y);
        _hoveredIndex = _selectedIndex;
        Invalidate();
    }

    protected override void OnMouseMove(EventArgs e)
    {
        if (e is not MouseEventArgs args) return;

        int index = HitTestItem(args.Y);
        if (_hoveredIndex != index)
        {
            _hoveredIndex = index;
            Invalidate();
        }

        if (args.Button == MouseButtons.Left && _selectedIndex >= 0)
        {
            if (Math.Abs(args.Y - _lastMouseDownY) > 4)
            {
                var item = _filteredItems[_selectedIndex];
                _selectedIndex = -1;
                DragStarted?.Invoke(this, new ToolboxDragEventArgs(item));
                Invalidate();
            }
        }
    }

    private List<(int y, int height, ToolboxItem? item, bool isHeader, string category)> BuildLayout()
    {
        var layout = new List<(int y, int height, ToolboxItem? item, bool isHeader, string category)>();
        int y = HeaderHeight + 4;
        string? currentCat = null;

        foreach (var item in _toolboxService.Items)
        {
            if (_activeCategory == null && item.Category != currentCat)
            {
                currentCat = item.Category;
                layout.Add((y, CategoryGap, null, true, currentCat));
                y += CategoryGap;
            }

            if (_activeCategory != null && item.Category != _activeCategory)
                continue;

            layout.Add((y, ItemHeight, item, false, item.Category));
            y += ItemHeight;
        }

        return layout;
    }

    private int HitTestItem(int mouseY)
    {
        var layout = BuildLayout();
        foreach (var entry in layout)
        {
            if (entry.isHeader) continue;
            if (mouseY >= entry.y && mouseY < entry.y + entry.height)
                return _filteredItems.IndexOf(entry.item!);
        }
        return -1;
    }

    public override void Render(Graphics g)
    {
        var theme = ThemeManager.CurrentTheme;

        g.FillRectangle(theme.ControlLight, 0, 0, Width, Height);
        g.FillRectangle(theme.ActiveCaption, 0, 0, Width, HeaderHeight);
        g.DrawString("Toolbox", theme.DefaultFont, theme.ActiveCaptionText, 6, 4);

        var layout = BuildLayout();

        foreach (var entry in layout)
        {
            if (entry.isHeader)
            {
                g.FillRectangle(theme.ControlDark, 0, entry.y, Width, entry.height);
                g.DrawString(entry.category, theme.DefaultFont, theme.ControlText, 4, entry.y + 2);
            }
            else
            {
                int idx = _filteredItems.IndexOf(entry.item!);
                var bg = idx == _hoveredIndex ? theme.HoverHighlight : Color.Transparent;
                if (bg.A > 0)
                    g.FillRectangle(bg, 2, entry.y, Width - 4, entry.height);

                var textColor = idx == _hoveredIndex ? theme.HighlightText : theme.ControlText;
                g.DrawString(entry.item!.DisplayName, theme.DefaultFont, textColor, 8, entry.y + 4);
            }
        }
    }
}
