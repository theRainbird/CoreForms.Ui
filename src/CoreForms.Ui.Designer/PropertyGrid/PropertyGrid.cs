using System;
using System.Collections.Generic;
using System.Linq;
using CoreForms.Ui.Core;
using CoreForms.Ui.Designer.Services;
using CoreForms.Ui.Theming;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Designer.PropertyGrid;

/// <summary>
/// A property grid control that displays and edits properties of the currently
/// selected design item. Connects to <see cref="SelectionService"/> to observe
/// selection changes and re-populates the property list.
/// </summary>
public class PropertyGrid : ContainerControl
{
    private readonly PropertyService _propertyService;
    private readonly SelectionService _selectionService;
    private readonly List<PropertyGridRow> _rows = new();

    private const int HeaderHeight = 24;
    private const int RowHeight = 26;
    private const int NameColumnWidth = 140;

    private string? _activeCategory;
    private PropertyGridRow? _editingRow;

    /// <summary>
    /// Gets the current target control whose properties are being edited.
    /// </summary>
    public Control? TargetControl { get; private set; }

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="selectionService">The selection service to observe.</param>
    public PropertyGrid(SelectionService selectionService)
    {
        _propertyService = new PropertyService();
        _selectionService = selectionService ?? throw new ArgumentNullException(nameof(selectionService));
        BackColor = ThemeManager.CurrentTheme.ControlLight;

        _selectionService.SelectionChanged += OnSelectionChanged;
    }

    /// <summary>
    /// Filters the grid to show only a specific category.
    /// Pass null to show all categories.
    /// </summary>
    public void FilterByCategory(string? category)
    {
        _activeCategory = category;
        RebuildRows();
        Invalidate();
    }

    /// <summary>
    /// Forces a refresh of all property values from the selected control.
    /// </summary>
    public void RefreshValues()
    {
        RebuildRows();
        Invalidate();
    }

    private void OnSelectionChanged(object? sender, EventArgs e)
    {
        TargetControl = _selectionService.PrimarySelection?.Control;
        RebuildRows();
        Invalidate();
    }

    private void RebuildRows()
    {
        _rows.Clear();
        if (TargetControl == null) return;

        var properties = _propertyService.GetProperties(TargetControl);

        foreach (var prop in properties)
        {
            if (_activeCategory != null && prop.Category != _activeCategory)
                continue;
            _rows.Add(new PropertyGridRow(prop, NameColumnWidth));
        }
    }

    protected override void OnMouseDown(EventArgs e)
    {
        if (e is not MouseEventArgs args) return;

        int rowIndex = HitTestRow(args.Y);

        // Commit any existing edit before processing new click
        if (_editingRow != null)
        {
            _editingRow.CommitEdit();
            _editingRow = null;
        }

        if (rowIndex >= 0 && rowIndex < _rows.Count)
        {
            var row = _rows[rowIndex];
            row.HandleClick(args.X, args.Y);

            if (row.IsEditing)
            {
                _editingRow = row;
                var form = FindForm();
                if (form != null)
                {
                    form.ActiveControl = this;
                }
                Invalidate();
            }
        }

        base.OnMouseDown(e);
    }

    private int HitTestRow(int y)
    {
        int drawY = HeaderHeight + 4;

        string? currentCat = null;
        for (int i = 0; i < _rows.Count; i++)
        {
            if (currentCat != _rows[i].Category)
            {
                currentCat = _rows[i].Category;
                drawY += 20; // category header
            }

            if (y >= drawY && y < drawY + RowHeight)
                return i;

            drawY += RowHeight;
        }

        return -1;
    }

    protected override void OnTextInput(string text)
    {
        if (_editingRow != null && text.Length > 0)
        {
            if (_editingRow.HandleKey(text[0]))
            {
                Invalidate();
                return;
            }
        }
        base.OnTextInput(text);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (_editingRow != null)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    _editingRow.CommitEdit();
                    _editingRow = null;
                    Invalidate();
                    e.Handled = true;
                    return;

                case Keys.Escape:
                    _editingRow.CancelEdit();
                    _editingRow = null;
                    Invalidate();
                    e.Handled = true;
                    return;

                case Keys.Back:
                    if (_editingRow.HandleKey('\b'))
                    {
                        Invalidate();
                        e.Handled = true;
                    }
                    return;

                case Keys.Delete:
                    if (_editingRow.HandleKey('\x03'))
                    {
                        Invalidate();
                        e.Handled = true;
                    }
                    return;

                case Keys.Tab:
                    _editingRow.CommitEdit();
                    _editingRow = null;
                    Invalidate();
                    break;
            }
        }
        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
    }

    protected override void OnLostFocus(EventArgs e)
    {
        if (_editingRow != null)
        {
            _editingRow.CommitEdit();
            _editingRow = null;
            Invalidate();
        }
        base.OnLostFocus(e);
    }

    public override void Render(Graphics g)
    {
        var theme = ThemeManager.CurrentTheme;

        // Background
        g.FillRectangle(theme.ControlLight, 0, 0, Width, Height);

        // Header
        g.FillRectangle(theme.ActiveCaption, 0, 0, Width, HeaderHeight);
        g.DrawString("Eigenschaften", theme.DefaultFont, theme.ActiveCaptionText, 6, 4);

        if (TargetControl == null)
        {
            g.DrawString("Keine Auswahl", theme.DefaultFont, theme.GrayText, 10, HeaderHeight + 10);
            return;
        }

        // Draw rows
        int drawY = HeaderHeight + 4;
        string? currentCat = null;

        foreach (var row in _rows)
        {
            // Category header
            if (currentCat != row.Category)
            {
                currentCat = row.Category;
                g.FillRectangle(theme.ControlDark, 0, drawY, Width, 20);
                g.DrawString(currentCat, theme.SmallFont, theme.ControlText, 4, drawY + 2);
                drawY += 20;
            }

            // Row background (alternating)
            var rowBack = _rows.IndexOf(row) % 2 == 0
                ? theme.ControlLight
                : theme.AlternateRow;
            g.FillRectangle(rowBack, 0, drawY, Width, RowHeight);

            // Row content
            row.Render(g, drawY, Width, RowHeight, theme);

            drawY += RowHeight;
        }
    }
}
