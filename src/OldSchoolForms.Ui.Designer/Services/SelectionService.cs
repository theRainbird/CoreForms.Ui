using System;
using System.Collections.Generic;
using System.Linq;
using OldSchoolForms.Ui.Core;

namespace OldSchoolForms.Ui.Designer.Services;

/// <summary>
/// Manages selection of controls on the design surface.
/// Supports single and multiple selection (Ctrl+Click).
/// Fires <see cref="SelectionChanged"/> when the selected set changes.
/// </summary>
public class SelectionService
{
    private readonly List<DesignItem> _selectedItems = new();
    private const int HandleSize = 7;

    /// <summary>
    /// Raised when the current selection changes.
    /// </summary>
    public event EventHandler? SelectionChanged;

    /// <summary>
    /// Gets the number of selected items.
    /// </summary>
    public int SelectedCount => _selectedItems.Count;

    /// <summary>
    /// Gets the first selected item, or null if nothing is selected.
    /// </summary>
    public DesignItem? PrimarySelection => _selectedItems.Count > 0 ? _selectedItems[^1] : null;

    /// <summary>
    /// Gets the list of all selected items (read-only).
    /// </summary>
    public IReadOnlyList<DesignItem> SelectedItems => _selectedItems.AsReadOnly();

    /// <summary>
    /// Selects a single item, clearing all other selections.
    /// </summary>
    /// <param name="item">The item to select.</param>
    public void Select(DesignItem item)
    {
        if (item == null) return;
        if (_selectedItems.Count == 1 && _selectedItems[0] == item) return;

        DeselectAll();
        _selectedItems.Add(item);
        item.Selected = true;
        OnSelectionChanged();
    }

    /// <summary>
    /// Toggles the selection state of an item (additive).
    /// Used for Ctrl+Click behavior.
    /// </summary>
    /// <param name="item">The item to toggle.</param>
    public void ToggleSelection(DesignItem item)
    {
        if (item == null) return;

        if (_selectedItems.Contains(item))
        {
            _selectedItems.Remove(item);
            item.Selected = false;
        }
        else
        {
            _selectedItems.Add(item);
            item.Selected = true;
        }
        OnSelectionChanged();
    }

    /// <summary>
    /// Clears all selections.
    /// </summary>
    public void DeselectAll()
    {
        if (_selectedItems.Count == 0) return;

        foreach (var item in _selectedItems)
            item.Selected = false;
        _selectedItems.Clear();
        OnSelectionChanged();
    }

    /// <summary>
    /// Checks whether the given item is selected.
    /// </summary>
    public bool IsSelected(DesignItem item) => item != null && _selectedItems.Contains(item);

    /// <summary>
    /// Deselects a single item.
    /// </summary>
    public void Deselect(DesignItem item)
    {
        if (item == null || !_selectedItems.Contains(item)) return;
        _selectedItems.Remove(item);
        item.Selected = false;
        OnSelectionChanged();
    }

    /// <summary>
    /// Selects all items in the given list.
    /// </summary>
    public void SelectAll(IEnumerable<DesignItem> items)
    {
        if (items == null) return;
        DeselectAll();
        foreach (var item in items)
        {
            if (!_selectedItems.Contains(item))
            {
                _selectedItems.Add(item);
                item.Selected = true;
            }
        }
        OnSelectionChanged();
    }

    /// <summary>
    /// Gets the handle size in pixels used for rendering and hit-testing resize grips.
    /// </summary>
    public static int GetHandleSize() => HandleSize;

    /// <summary>
    /// Computes the bounding rectangle that encloses all selected controls.
    /// Returns <see cref="Rectangle.Empty"/> if nothing is selected.
    /// </summary>
    public Rectangle GetSelectionBounds()
    {
        if (_selectedItems.Count == 0) return Rectangle.Empty;

        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;

        foreach (var item in _selectedItems)
        {
            var b = item.Control.Bounds;
            if (b.X < minX) minX = b.X;
            if (b.Y < minY) minY = b.Y;
            if (b.Right > maxX) maxX = b.Right;
            if (b.Bottom > maxY) maxY = b.Bottom;
        }

        return new Rectangle(minX, minY, maxX - minX, maxY - minY);
    }

    private void OnSelectionChanged()
    {
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }
}
