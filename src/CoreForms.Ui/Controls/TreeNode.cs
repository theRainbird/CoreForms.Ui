using System.Collections.Generic;
using CoreForms.Ui.Core;
using CoreForms.Ui.Rendering;
using CoreForms.Ui.Theming;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls;

/// <summary>
/// Represents a node in a TreeView.
/// </summary>
public class TreeNode
{
    /// <summary>
    /// The text displayed for the node.
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Optional icon index from the TreeView's ImageList.
    /// </summary>
    public int? ImageIndex { get; set; }

    /// <summary>
    /// Parent node; null for root nodes.
    /// </summary>
    public TreeNode? Parent { get; private set; }

    /// <summary>
    /// Child nodes.
    /// </summary>
    public List<TreeNode> Children { get; } = new();

    /// <summary>
    /// Indicates whether the node is expanded.
    /// </summary>
    public bool IsExpanded { get; internal set; } = true;

    /// <summary>
    /// User‑defined data attached to this node.
    /// </summary>
    public object? Tag { get; set; }

    /// <summary>
    /// Internal bounds used for hit‑testing during rendering.
    /// </summary>
    internal Rectangle Bounds { get; set; }

    /// <summary>
    /// Called when the node's visual state changes, to trigger a TreeView repaint.
    /// </summary>
    internal Action? OnInvalidate { get; set; }

    /// <summary>
    /// Creates a new TreeNode with the specified text.
    /// </summary>
    /// <param name="text">Node label.</param>
    public TreeNode(string text)
    {
        Text = text;
    }

    /// <summary>
    /// Adds a child node.
    /// </summary>
    /// <param name="child">The child to add.</param>
    public void Add(TreeNode child)
    {
        child.Parent = this;
        child.OnInvalidate = OnInvalidate;
        Children.Add(child);
        OnInvalidate?.Invoke();
    }

    /// <summary>
    /// Toggles the expanded state.
    /// </summary>
    public void Toggle()
    {
        IsExpanded = !IsExpanded;
        OnInvalidate?.Invoke();
    }
}
