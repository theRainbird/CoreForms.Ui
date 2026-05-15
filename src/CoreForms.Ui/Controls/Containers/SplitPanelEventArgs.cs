using System;

namespace CoreForms.Ui.Controls.Containers;

/// <summary>
/// Provides data for the Splitter drag events.
/// </summary>
public class SplitterDragEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the new split position.
    /// </summary>
    public int NewPosition { get; set; }
}
