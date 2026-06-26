using CoreForms.Ui.Core;
using CoreForms.Ui.Theming;
using CoreForms.Ui.Rendering;

namespace CoreForms.Ui.Layout;

/// <summary>
/// A layout panel that arranges child controls in rows and columns.
/// </summary>
public class TableLayoutPanel : ContainerControl
{
    private int _rowCount = 2;
    private int _columnCount = 2;
    private readonly List<TableLayoutStyle> _styles = new();
    private (Point location, Size size)[]? _layoutCache;
    private int _cachedLayoutVersion = -1;

    /// <summary>
    /// Initializes a new instance of TableLayoutPanel.
    /// </summary>
    public TableLayoutPanel()
    {
        Size = new Size(300, 200);
        _backColor = ThemeManager.CurrentTheme.ControlBackground;
    }

    /// <summary>
    /// Called when the theme changes. Updates tablelayoutpanel-specific colors.
    /// </summary>
    /// <param name="newTheme">The new theme that was activated.</param>
    public override void OnThemeChanged(Theme newTheme)
    {
        if (!_backColorSet)
            _backColor = newTheme.ControlBackground;
        Invalidate();
    }

    /// <summary>
    /// Gets or sets the number of rows in the table.
    /// </summary>
    public int RowCount
    {
        get => _rowCount;
        set
        {
            _rowCount = value;
            MarkLayoutDirty();
            PerformLayout();
        }
    }

    /// <summary>
    /// Gets or sets the number of columns in the table.
    /// </summary>
    public int ColumnCount
    {
        get => _columnCount;
        set
        {
            _columnCount = value;
            MarkLayoutDirty();
            PerformLayout();
        }
    }

    private void LayoutControls()
    {
        if (Controls.Count == 0) return;

        if (_cachedLayoutVersion == _layoutVersion && _layoutCache != null && _layoutCache.Length == Controls.Count)
        {
            for (int i = 0; i < Controls.Count; i++)
            {
                var child = Controls[i];
                child._layoutDrivenBoundsChange = true;
                child.Location = _layoutCache[i].location;
                child.Size = _layoutCache[i].size;
                child._layoutDrivenBoundsChange = false;
            }
            return;
        }

        int padLeft = Padding.Left + 2;
        int padTop = Padding.Top + 2;
        int innerWidth = Width - Padding.Horizontal - 4;
        int innerHeight = Height - Padding.Vertical - 4;

        int cellWidth = innerWidth / _columnCount;
        int cellHeight = innerHeight / _rowCount;

        var cache = new (Point location, Size size)[Controls.Count];

        int index = 0;
        for (int row = 0; row < _rowCount && index < Controls.Count; row++)
        {
            for (int col = 0; col < _columnCount && index < Controls.Count; col++)
            {
                var child = Controls[index];
                child._layoutDrivenBoundsChange = true;
                var loc = new Point(col * cellWidth + padLeft, row * cellHeight + padTop);
                var sz = new Size(cellWidth - 4, cellHeight - 4);
                child.Location = loc;
                child.Size = sz;
                child._layoutDrivenBoundsChange = false;
                cache[index] = (loc, sz);
                index++;
            }
        }

        _layoutCache = cache;
        _cachedLayoutVersion = _layoutVersion;
    }

    /// <summary>
    /// Called when the control needs to perform layout.
    /// </summary>
    protected override void OnLayout()
    {
        LayoutControls();
    }

    /// <summary>
    /// Renders the control, its background, and grid lines.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
    public override void Render(Graphics g)
    {
        if (!Visible) return;

        var theme = ThemeManager.CurrentTheme;

        g.FillRectangle(BackColor, 0, 0, Width, Height);

        int padLeft = Padding.Left + 2;
        int padTop = Padding.Top + 2;
        int innerWidth = Width - Padding.Horizontal - 4;
        int innerHeight = Height - Padding.Vertical - 4;

        int cellWidth = innerWidth / _columnCount;
        int cellHeight = innerHeight / _rowCount;

        for (int row = 0; row <= _rowCount; row++)
        {
            g.DrawLine(theme.TableLayoutGridLine, padLeft, row * cellHeight + padTop, Width - Padding.Right - 2, row * cellHeight + padTop);
        }
        for (int col = 0; col <= _columnCount; col++)
        {
            g.DrawLine(theme.TableLayoutGridLine, col * cellWidth + padLeft, padTop, col * cellWidth + padLeft, Height - Padding.Bottom - 2);
        }

        base.Render(g);
    }
}