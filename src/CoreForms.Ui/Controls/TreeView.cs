using System.Collections.Generic;
using System.Linq;
using CoreForms.Ui.Core;
using CoreForms.Ui.Theming;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls;

/// <summary>
/// Displays a hierarchical collection of <see cref="TreeNode"/> objects with scrolling, icons, and keyboard navigation.
/// </summary>
public class TreeView : Control
{
    private const int DefaultIndent = 24;
    private const int GlyphSize = 12;
    private const int ItemHeight = 18;
    private const int LeftMargin = 4;
    private int _scrollOffsetY;
    private readonly List<TreeNode> _rootNodes = new();
    private readonly ScrollBarEngine _vScrollBar = new();
    private TreeViewScrollBarContext? _scrollBarContext;

    private TreeViewScrollBarContext VScrollBarContext => _scrollBarContext ??= new TreeViewScrollBarContext(this);

    /// <summary>
    /// Gets the collection of root nodes.
    /// </summary>
    public IList<TreeNode> Nodes => _nodesCollection ??= new NodeCollection(_rootNodes, this);

    private NodeCollection? _nodesCollection;

    private sealed class NodeCollection : IList<TreeNode>
    {
        private readonly List<TreeNode> _nodes;
        private readonly TreeView _owner;

        public NodeCollection(List<TreeNode> nodes, TreeView owner)
        {
            _nodes = nodes;
            _owner = owner;
        }

        private void WireNode(TreeNode node)
        {
            node.OnInvalidate = _owner.Invalidate;
            WireChildren(node);
        }

        private static void WireChildren(TreeNode node)
        {
            foreach (var child in node.Children)
            {
                child.OnInvalidate = node.OnInvalidate;
                WireChildren(child);
            }
        }

        public void Add(TreeNode item) { _nodes.Add(item); WireNode(item); _owner.Invalidate(); }
        public void Clear() { _nodes.Clear(); _owner.Invalidate(); }
        public bool Remove(TreeNode item) { var r = _nodes.Remove(item); _owner.Invalidate(); return r; }
        public void RemoveAt(int index) { _nodes.RemoveAt(index); _owner.Invalidate(); }
        public void Insert(int index, TreeNode item) { _nodes.Insert(index, item); WireNode(item); _owner.Invalidate(); }
        public int Count => _nodes.Count;
        public bool IsReadOnly => false;
        public bool Contains(TreeNode item) => _nodes.Contains(item);
        public int IndexOf(TreeNode item) => _nodes.IndexOf(item);
        public void CopyTo(TreeNode[] array, int arrayIndex) => _nodes.CopyTo(array, arrayIndex);
        public IEnumerator<TreeNode> GetEnumerator() => _nodes.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _nodes.GetEnumerator();
        public TreeNode this[int index] { get => _nodes[index]; set { _nodes[index] = value; WireNode(value); _owner.Invalidate(); } }
    }

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
        _vScrollBar.SmallChange = ItemHeight;
        _vScrollBar.LargeChange = ItemHeight * 3;
        _vScrollBar.Scroll += (s, e) =>
        {
            _scrollOffsetY = _vScrollBar.Value;
            Invalidate();
        };
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
        _vScrollBar.ViewSize = Height;
        _vScrollBar.ContentSize = visible.Count * ItemHeight;
        _vScrollBar.EnsureVisible(index * ItemHeight, ItemHeight);
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

        g.FillRectangle(BackColor, 0, 0, Width, Height);

        g.SetClip(new Rectangle(0, 0, Width, Height));
        g.TranslateTransform(0, -_scrollOffsetY);

        var visible = GetVisibleNodes();

        // Pass 1: calculate positions and store bounds
        for (int i = 0; i < visible.Count; i++)
        {
            var node = visible[i];
            var depth = GetDepth(node);
            var x = LeftMargin + depth * Indent;
            var y = i * ItemHeight;
            node.Bounds = new Rectangle(x, y, Width - x, ItemHeight);
        }

        // Pass 2: draw horizontal connectors for each non-root node
        for (int i = 0; i < visible.Count; i++)
        {
            var node = visible[i];
            var depth = GetDepth(node);
            if (depth == 0) continue;

            int lineX = LeftMargin + (depth - 1) * Indent + GlyphSize / 2;
            int endX = LeftMargin + depth * Indent;
            int rowCenterY = i * ItemHeight + ItemHeight / 2;
            DrawDottedLine(g, theme.ControlDark, lineX, rowCenterY, endX, rowCenterY, 2, 2);
        }

        // Pass 2b: draw vertical sibling lines per parent group
        var drawnParents = new HashSet<TreeNode>();
        for (int i = 0; i < visible.Count; i++)
        {
            var node = visible[i];
            var depth = GetDepth(node);
            if (depth == 0) continue;

            var parent = node.Parent!;
            if (drawnParents.Contains(parent)) continue;
            drawnParents.Add(parent);

            int parentIdx = visible.IndexOf(parent);
            if (parentIdx < 0) continue;

            int lastChildIdx = -1;
            for (int j = visible.Count - 1; j >= 0; j--)
            {
                if (visible[j].Parent == parent)
                {
                    lastChildIdx = j;
                    break;
                }
            }

            if (lastChildIdx >= 0)
            {
                int startY = parentIdx * ItemHeight + ItemHeight / 2;
                int endY = lastChildIdx * ItemHeight + ItemHeight / 2;
                int lineX = LeftMargin + (depth - 1) * Indent + GlyphSize / 2;
                DrawDottedLine(g, theme.ControlDark, lineX, startY, lineX, endY, 2, 2);
            }
        }

        // Pass 2c: draw root-level vertical line connecting all root nodes
        if (_rootNodes.Count > 1)
        {
            int firstIdx = -1, lastIdx = -1;
            for (int j = 0; j < visible.Count; j++)
            {
                if (GetDepth(visible[j]) == 0)
                {
                    if (firstIdx < 0) firstIdx = j;
                    lastIdx = j;
                }
            }

            if (firstIdx >= 0 && lastIdx > firstIdx)
            {
                int startY = firstIdx * ItemHeight + ItemHeight / 2;
                int endY = lastIdx * ItemHeight + ItemHeight / 2;
                int lineX = LeftMargin + GlyphSize / 2;
                DrawDottedLine(g, theme.ControlDark, lineX, startY, lineX, endY, 2, 2);
            }
        }

        // Pass 3: draw nodes (glyph backgrounds, glyphs, selection, icons, text)
        for (int i = 0; i < visible.Count; i++)
        {
            var node = visible[i];
            var depth = GetDepth(node);
            var x = LeftMargin + depth * Indent;
            var y = i * ItemHeight;
            var hasChildren = node.Children.Any();

            // Selection background (drawn before anything else for this row)
            if (node == SelectedNode)
            {
                g.FillRectangle(theme.Highlight, 0, y, Width, ItemHeight);
            }

            // Glyph box background and outline
            int glyphY = y + (ItemHeight - GlyphSize) / 2;
            if (hasChildren)
            {
                // Fill glyph background to occlude any vertical lines passing through
                g.FillRectangle(BackColor, x, glyphY, GlyphSize, GlyphSize);
                // Draw glyph outline
                g.DrawRectangle(theme.ControlDark, x, glyphY, GlyphSize, GlyphSize, 1);
                // Horizontal minus line
                var centerX = x + GlyphSize / 2;
                var centerY = glyphY + GlyphSize / 2;
                g.DrawLine(theme.ControlDark, x + 2, centerY, x + GlyphSize - 2, centerY);
                // Vertical plus line (when collapsed)
                if (!node.IsExpanded)
                {
                    g.DrawLine(theme.ControlDark, centerX, glyphY + 2, centerX, glyphY + GlyphSize - 2);
                }
            }

            // Calculate text start position
            int textX = hasChildren ? x + GlyphSize + 2 : x;

            // Icon
            if (ImageList != null && node.ImageIndex.HasValue && node.ImageIndex.Value < ImageList.Count)
            {
                var img = ImageList[node.ImageIndex.Value];
                int iconY = y + (ItemHeight - ItemHeight) / 2;
                g.DrawImage(img, textX, iconY, ItemHeight, ItemHeight);
                textX += ItemHeight + 2;
            }

            // Text
            var textColor = node == SelectedNode ? theme.HighlightText : ForeColor;
            g.DrawString(node.Text, EffectiveFont, textColor, textX, y);
        }

        g.TranslateTransform(0, _scrollOffsetY);

        int visibleCount = GetVisibleNodes().Count;
        int totalContentHeight = visibleCount * ItemHeight;
        _vScrollBar.ViewSize = Height;
        _vScrollBar.ContentSize = totalContentHeight;

        if (_vScrollBar.NeedsScrollbar)
        {
            var scrollBarBounds = new Rectangle(Width - ScrollBarEngine.DefaultScrollBarSize, 0, ScrollBarEngine.DefaultScrollBarSize, Height);
            _vScrollBar.Render(g, scrollBarBounds, theme);
        }

        // Draw border after scrollbar to ensure it stays on top
        g.DrawRectangle(theme.ControlDark, 0.5f, 0.5f, Width - 1, Height - 1, 1);
        if (Focused)
            g.DrawRectangle(theme.FocusIndicator, 1.5f, 1.5f, Width - 3, Height - 3);

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
    /// Called when the theme changes. Updates TreeView-specific colors.
    /// </summary>
    /// <param name="newTheme">The new theme that was activated.</param>
    public override void OnThemeChanged(Theme newTheme)
    {
        if (!_backColorSet)
            _backColor = newTheme.ContentBackground;
        if (!_foreColorSet)
            _foreColor = newTheme.ControlText;
        Invalidate();
    }

    /// <summary>
    /// Handles mouse wheel for vertical scrolling.
    /// </summary>
    protected internal override void OnMouseWheel(EventArgs e)
    {
        var me = e as MouseEventArgs;
        if (me != null)
        {
            int visibleCount = GetVisibleNodes().Count;
            _vScrollBar.SmallChange = ItemHeight;
            _vScrollBar.ViewSize = Height;
            _vScrollBar.ContentSize = visibleCount * ItemHeight;
            if (_vScrollBar.NeedsScrollbar && !_vScrollBar.IsDragging)
            {
                _vScrollBar.HandleMouseWheel(me.Delta, VScrollBarContext);
            }
        }

        base.OnMouseWheel(e);
    }

    /// <summary>
    /// Handles mouse down for selection, expand/collapse, and scrollbar.
    /// </summary>
    protected internal override void OnMouseDown(EventArgs e)
    {
        var me = e as MouseEventArgs;
        if (me != null)
        {
            // Check scrollbar click
            if (_vScrollBar.NeedsScrollbar && me.X >= Width - ScrollBarEngine.DefaultScrollBarSize)
            {
                var scrollBarBounds = new Rectangle(Width - ScrollBarEngine.DefaultScrollBarSize, 0, ScrollBarEngine.DefaultScrollBarSize, Height);
                _vScrollBar.HandleMouseDown(new Point(me.X, me.Y), scrollBarBounds, VScrollBarContext);
                return;
            }

            int virtualY = me.Y + _scrollOffsetY;
            var visible = GetVisibleNodes();

            foreach (var node in visible)
            {
                if (virtualY >= node.Bounds.Y && virtualY < node.Bounds.Y + ItemHeight)
                {
                    int depth = GetDepth(node);
                    int glyphX = LeftMargin + depth * Indent;
                    var relX = me.X - glyphX;

                    if (relX >= 0 && relX < GlyphSize && node.Children.Any())
                    {
                        node.Toggle();
                        Invalidate();
                        return;
                    }

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
    /// Handles mouse up for scrollbar.
    /// </summary>
    protected internal override void OnMouseUp(EventArgs e)
    {
        if (_vScrollBar.IsDragging || _vScrollBar.IsUpButtonPressed || _vScrollBar.IsDownButtonPressed)
        {
            _vScrollBar.HandleMouseUp(VScrollBarContext);
        }
        base.OnMouseUp(e);
    }

    /// <summary>
    /// Handles mouse move for scrollbar hover and drag.
    /// </summary>
    protected internal override void OnMouseMove(EventArgs e)
    {
        if (_vScrollBar.NeedsScrollbar)
        {
            var me = e as MouseEventArgs;
            if (me != null)
            {
                var scrollBarBounds = new Rectangle(Width - ScrollBarEngine.DefaultScrollBarSize, 0, ScrollBarEngine.DefaultScrollBarSize, Height);
                _vScrollBar.HandleMouseMove(new Point(me.X, me.Y), scrollBarBounds, VScrollBarContext);
            }
        }
        base.OnMouseMove(e);
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

    private sealed class TreeViewScrollBarContext : IScrollBarContext
    {
        private readonly TreeView _owner;
        public TreeViewScrollBarContext(TreeView owner) => _owner = owner;
        public float Zoom => _owner.EffectiveZoom;
        public void Invalidate() => _owner.Invalidate();
        public void CaptureMouse(bool capture) => _owner.CapturingMouse = capture;
    }
}