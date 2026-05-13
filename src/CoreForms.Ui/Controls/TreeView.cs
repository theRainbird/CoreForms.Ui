using System.Collections.Generic;
using System.Linq;
using CoreForms.Ui.Core;
using CoreForms.Ui.Rendering;
using CoreForms.Ui.Theming;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls;

/// <summary>
/// Displays a hierarchical collection of <see cref="TreeNode"/> objects with scrolling, icons, and keyboard navigation.
/// </summary>
public class TreeView : Control
{
    private const int DefaultIndent = 16;
    private const int GlyphSize = 12;
    private const int ItemHeight = 18; // approximate height per node line
    private int _scrollOffsetY;
    private readonly List<TreeNode> _rootNodes = new();

    /// <summary>
    /// Gets the collection of root nodes.
    /// </summary>
    public List<TreeNode> Nodes => _rootNodes;

    /// <summary>
    /// Currently selected node.
    /// </summary>
    public TreeNode? SelectedNode { get; private set; }

    /// <summary>
    /// Optional image list for node icons.
    /// </summary>
    public ImageList? ImageList { get; set; }

    /// <summary>
    /// Horizontal indentation per tree level.
    /// </summary>
    public int Indent { get; set; } = DefaultIndent;

    /// <summary>
    /// Occurs after the selected node changes.
    /// </summary>
    public event EventHandler? AfterSelect;

    /// <summary>
    /// Initializes a new instance of TreeView.
    /// </summary>
    public TreeView()
    {
        var theme = ThemeManager.CurrentTheme;
        _backColor = theme.ContentBackground;
        _foreColor = theme.ControlText;
        Size = new Size(200, 150);
        TabStop = true;
    }

    /// <summary>
    /// Expands all nodes in the tree.
    /// </summary>
    public void ExpandAll()
    {
        foreach (var node in GetAllNodes())
            node.IsExpanded = true;
        Invalidate();
    }

    /// <summary>
    /// Collapses all nodes in the tree.
    /// </summary>
    public void CollapseAll()
    {
        foreach (var node in GetAllNodes())
            node.IsExpanded = false;
        Invalidate();
    }

    /// <summary>
    /// Ensures the specified node is visible by scrolling if necessary.
    /// </summary>
    /// <param name="node">Node to bring into view.</param>
    public void EnsureVisible(TreeNode node)
    {
        var visible = GetVisibleNodes();
        var index = visible.IndexOf(node);
        if (index < 0) return;
        var top = index * ItemHeight;
        var bottom = top + ItemHeight;
        if (top < _scrollOffsetY)
            _scrollOffsetY = top;
        else if (bottom > _scrollOffsetY + Height)
            _scrollOffsetY = bottom - Height;
        Invalidate();
    }

    /// <summary>
    /// Renders the tree view.
    /// </summary>
    /// <param name="g">Graphics context.</param>
    public override void Render(Graphics g)
    {
        if (!Visible) return;
        var theme = ThemeManager.CurrentTheme;

        // Fill background using BackColor
        g.FillRectangle(BackColor, 0, 0, Width, Height);
        // Border
        g.DrawRectangle(SystemColors.ControlDark, 0, 0, Width, Height, 1);
        // Focus rectangle
        if (Focused)
            g.DrawRectangle(theme.FocusIndicator, 1, 1, Width - 2, Height - 2, 1);

        g.SetClip(new Rectangle(0, 0, Width, Height));
        g.TranslateTransform(0, -_scrollOffsetY);

        // First pass: calculate positions and store bounds
        int y = 0;
        var visible = GetVisibleNodes();
        for (int i = 0; i < visible.Count; i++)
        {
            var node = visible[i];
            var depth = GetDepth(node);
            var x = depth * Indent;
            node.Bounds = new Rectangle(x, y, Width - x, ItemHeight);
            y += ItemHeight;
        }

        // Draw continuous vertical continuation lines for each ancestor column
        // A column needs a line if ANY visible node has an ancestor with a younger sibling at that depth
        for (int colDepth = 0; ; colDepth++)
        {
            int colLineX = colDepth * Indent + GlyphSize / 2;
            bool anyNodeNeedsLine = false;
            foreach (var node in visible)
            {
                int nodeDepth = GetDepth(node);
                if (nodeDepth <= colDepth) continue;
                
                var ancestor = node;
                for (int d = 0; d < nodeDepth - colDepth; d++)
                {
                    if (ancestor.Parent == null) break;
                    ancestor = ancestor.Parent;
                }
                
                bool hasYoungerSibling = false;
                if (ancestor.Parent != null)
                {
                    int idx = ancestor.Parent.Children.IndexOf(ancestor);
                    if (idx < ancestor.Parent.Children.Count - 1) hasYoungerSibling = true;
                }
                else
                {
                    int rootIdx = _rootNodes.IndexOf(ancestor);
                    if (rootIdx >= 0 && rootIdx < _rootNodes.Count - 1) hasYoungerSibling = true;
                }
                
                if (hasYoungerSibling)
                {
                    anyNodeNeedsLine = true;
                    break;
                }
            }
            if (!anyNodeNeedsLine) break; // No more columns need lines
            
            // Find the topmost and bottommost nodes that need this column's line
            int topY = -1;
            int bottomY = -1;
            foreach (var node in visible)
            {
                int nodeDepth = GetDepth(node);
                if (nodeDepth <= colDepth) continue;
                
                var ancestor = node;
                for (int d = 0; d < nodeDepth - colDepth; d++)
                {
                    if (ancestor.Parent == null) break;
                    ancestor = ancestor.Parent;
                }
                
                bool hasYoungerSibling = false;
                if (ancestor.Parent != null)
                {
                    int idx = ancestor.Parent.Children.IndexOf(ancestor);
                    if (idx < ancestor.Parent.Children.Count - 1) hasYoungerSibling = true;
                }
                else
                {
                    int rootIdx = _rootNodes.IndexOf(ancestor);
                    if (rootIdx >= 0 && rootIdx < _rootNodes.Count - 1) hasYoungerSibling = true;
                }
                
                if (hasYoungerSibling)
                {
                    int nodeY = node.Bounds.Y;
                    if (topY < 0 || nodeY < topY) topY = nodeY;
                    int nodeBottom = nodeY + ItemHeight;
                    if (bottomY < 0 || nodeBottom > bottomY) bottomY = nodeBottom;
                }
            }
            
            if (topY >= 0 && bottomY > topY)
            {
                DrawDottedLine(g, SystemColors.ControlDark, colLineX, topY, colLineX, bottomY, 2, 2);
            }
        }

        // Second pass: draw nodes (glyphs, lines, icons, text)
        y = 0;
        for (int i = 0; i < visible.Count; i++)
        {
            var node = visible[i];
            var depth = GetDepth(node);
            var x = depth * Indent;
            var hasChildren = node.Children.Any();
            
            // Glyph box: centered vertically in the row
            int glyphY = y + (ItemHeight - GlyphSize) / 2;
            var glyphRect = new Rectangle(x, glyphY, GlyphSize, GlyphSize);

            // Draw connector lines (dotted)
            if (node.Parent != null)
            {
                int parentY = node.Parent.Bounds.Y + ItemHeight / 2;
                int childY = y + ItemHeight / 2; // center of current row
                int lineX = (depth - 1) * Indent + GlyphSize / 2;
                
                // Vertical dotted line from parent center down to top of this node's glyph box
                int verticalStopY = glyphY;
                DrawDottedLine(g, SystemColors.ControlDark, lineX, parentY, lineX, verticalStopY, 2, 2);
                
                // Horizontal dotted line from vertical line to left edge of glyph box
                DrawDottedLine(g, SystemColors.ControlDark, lineX, childY, x, childY, 2, 2);
            }

            if (hasChildren)
            {
                // box outline
                g.DrawRectangle(SystemColors.ControlDark, glyphRect.X, glyphRect.Y, GlyphSize, GlyphSize, 1);
                // horizontal line (minus)
                var centerX = glyphRect.X + GlyphSize / 2;
                var centerY = glyphRect.Y + GlyphSize / 2;
                g.DrawLine(SystemColors.ControlDark, glyphRect.X + 2, centerY, glyphRect.X + GlyphSize - 2, centerY);
                // vertical line for plus (when collapsed)
                if (!node.IsExpanded)
                {
                    g.DrawLine(SystemColors.ControlDark, centerX, glyphRect.Y + 2, centerX, glyphRect.Y + GlyphSize - 2);
                }
            }

            // Calculate text start position
            int textX = hasChildren ? x + GlyphSize + 2 : x;

            // Selection background (drawn before icon and text)
            if (node == SelectedNode)
            {
                g.FillRectangle(SystemColors.Highlight, 0, y, Width, ItemHeight);
            }

            // Icon (centered vertically in the row)
            if (ImageList != null && node.ImageIndex.HasValue && node.ImageIndex.Value < ImageList.Count)
            {
                var img = ImageList[node.ImageIndex.Value];
                int iconY = y + (ItemHeight - ItemHeight) / 2; // ItemHeight x ItemHeight, so centered
                g.DrawImage(img, textX, iconY, ItemHeight, ItemHeight);
                textX += ItemHeight + 2;
            }

            // Text (drawn last so it appears on top)
            var textColor = node == SelectedNode ? SystemColors.HighlightText : ForeColor;
            g.DrawString(node.Text, EffectiveFont, textColor, textX, y);

            y += ItemHeight;
        }

        g.TranslateTransform(0, _scrollOffsetY);
        base.Render(g);
    }

    /// <summary>
    /// Draws a dotted line between two points.
    /// </summary>
    private static void DrawDottedLine(Graphics g, Color color, float x1, float y1, float x2, float y2, float dotLength, float gapLength)
    {
        float dx = x2 - x1;
        float dy = y2 - y1;
        float length = (float)System.Math.Sqrt(dx * dx + dy * dy);
        if (length < dotLength) return;

        float nx = dx / length;
        float ny = dy / length;
        float drawn = 0;
        bool draw = true;

        while (drawn < length)
        {
            float segLen = draw ? dotLength : gapLength;
            if (drawn + segLen > length) segLen = length - drawn;

            if (draw && segLen > 0)
            {
                g.DrawLine(color, x1 + nx * drawn, y1 + ny * drawn, x1 + nx * (drawn + segLen), y1 + ny * (drawn + segLen));
            }
            drawn += segLen;
            draw = !draw;
        }
    }

    /// <summary>
    /// Handles mouse wheel for vertical scrolling.
    /// </summary>
    protected internal override void OnMouseWheel(EventArgs e)
    {
        var me = e as MouseEventArgs;
        if (me != null)
        {
            // delta is usually +/-120 per notch
            int lines = me.Delta / 120;
            _scrollOffsetY = System.Math.Max(0, _scrollOffsetY - lines * ItemHeight);
            // clamp to content height
            int maxOffset = System.Math.Max(0, GetVisibleNodes().Count * ItemHeight - Height);
            _scrollOffsetY = System.Math.Min(_scrollOffsetY, maxOffset);
            Invalidate();
        }

        base.OnMouseWheel(e);
    }

    /// <summary>
    /// Handles mouse down for selection and expand/collapse.
    /// </summary>
    protected internal override void OnMouseDown(EventArgs e)
    {
        var me = e as MouseEventArgs;
        if (me != null)
        {
            // Calculate virtual Y coordinate (accounting for scroll)
            int virtualY = me.Y + _scrollOffsetY;
            var visible = GetVisibleNodes();
            
            // Find the node at the clicked position using bounds
            foreach (var node in visible)
            {
                if (virtualY >= node.Bounds.Y && virtualY < node.Bounds.Y + ItemHeight)
                {
                    int depth = GetDepth(node);
                    int glyphX = depth * Indent;
                    var relX = me.X - glyphX;
                    
                    // Hit test glyph area
                    if (relX >= 0 && relX < GlyphSize && node.Children.Any())
                    {
                        node.Toggle();
                        Invalidate();
                        return;
                    }

                    // Otherwise select node
                    SelectedNode = node;
                    AfterSelect?.Invoke(this, EventArgs.Empty);
                    Invalidate();
                    return;
                }
            }
        }

        base.OnMouseDown(e);
    }

    /// <summary>
    /// Handles keyboard navigation.
    /// </summary>
    protected internal override void OnKeyDown(KeyEventArgs e)
    {
        var visible = GetVisibleNodes();
        int currentIndex = SelectedNode != null ? visible.IndexOf(SelectedNode) : -1;
        switch (e.KeyCode)
        {
            case Keys.Up:
                if (currentIndex > 0)
                {
                    SelectedNode = visible[currentIndex - 1];
                    EnsureVisible(SelectedNode);
                    e.Handled = true;
                }

                break;
            case Keys.Down:
                if (currentIndex < visible.Count - 1)
                {
                    SelectedNode = visible[currentIndex + 1];
                    EnsureVisible(SelectedNode);
                    e.Handled = true;
                }

                break;
            case Keys.Right:
                if (SelectedNode != null && SelectedNode.Children.Any() && !SelectedNode.IsExpanded)
                {
                    SelectedNode.Toggle();
                    e.Handled = true;
                }

                break;
            case Keys.Left:
                if (SelectedNode != null)
                {
                    if (SelectedNode.IsExpanded && SelectedNode.Children.Any())
                    {
                        SelectedNode.Toggle();
                        e.Handled = true;
                    }
                    else if (SelectedNode.Parent != null)
                    {
                        SelectedNode = SelectedNode.Parent;
                        EnsureVisible(SelectedNode);
                        e.Handled = true;
                    }
                }

                break;
            case Keys.Space:
                SelectedNode?.Toggle();
                e.Handled = true;
                break;
        }

        AfterSelect?.Invoke(this, EventArgs.Empty);
        Invalidate();
        base.OnKeyDown(e);
    }

    // Helper: flatten visible nodes
    private List<TreeNode> GetVisibleNodes()
    {
        var list = new List<TreeNode>();
        foreach (var root in _rootNodes)
            AddVisibleRecursive(root, list);
        return list;
    }

    private void AddVisibleRecursive(TreeNode node, List<TreeNode> list)
    {
        list.Add(node);
        if (node.IsExpanded)
            foreach (var child in node.Children)
                AddVisibleRecursive(child, list);
    }

    private IEnumerable<TreeNode> GetAllNodes()
    {
        var all = new List<TreeNode>();
        foreach (var root in _rootNodes)
            AddAllRecursive(root, all);
        return all;
    }

    private void AddAllRecursive(TreeNode node, List<TreeNode> list)
    {
        list.Add(node);
        foreach (var child in node.Children)
            AddAllRecursive(child, list);
    }

    private int GetDepth(TreeNode node)
    {
        int d = 0;
        var cur = node.Parent;
        while (cur != null)
        {
            d++;
            cur = cur.Parent;
        }

        return d;
    }
}