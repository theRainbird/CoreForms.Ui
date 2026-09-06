using System;
using OldSchoolForms.Ui.Core;

namespace OldSchoolForms.Ui.Designer;

/// <summary>
/// Represents a single control placed on the design surface.
/// Tracks selection state, original bounds for undo, and the wrapped control.
/// </summary>
public class DesignItem
{
    /// <summary>
    /// Gets the underlying control instance being designed.
    /// </summary>
    public Control Control { get; }

    /// <summary>
    /// Gets or sets whether this item is currently selected.
    /// </summary>
    public bool Selected { get; set; }

    /// <summary>
    /// Gets or sets the design item that contains this item.
    /// Null when the item is a direct child of the design surface.
    /// Set when a control is added to a container or reparented during drag and drop.
    /// </summary>
    public DesignItem? ParentItem { get; set; }

    /// <summary>
    /// Gets or sets the original bounds before a move/resize operation began.
    /// Used for undo snapshots.
    /// </summary>
    public Rectangle OriginalBounds { get; set; }

    /// <summary>
    /// Initializes a new DesignItem wrapping the specified control.
    /// </summary>
    /// <param name="control">The control to wrap.</param>
    /// <exception cref="ArgumentNullException">Thrown when control is null.</exception>
    public DesignItem(Control control)
    {
        Control = control ?? throw new ArgumentNullException(nameof(control));
        OriginalBounds = control.Bounds;
    }

    /// <summary>
    /// Creates a snapshot of the current bounds for undo purposes.
    /// </summary>
    public void SnapshotBounds()
    {
        OriginalBounds = Control.Bounds;
    }
}
