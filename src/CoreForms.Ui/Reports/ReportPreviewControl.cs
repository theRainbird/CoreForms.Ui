using CoreForms.Ui.Core;
using CoreForms.Ui.Rendering;
using CoreForms.Ui.Theming;

namespace CoreForms.Ui.Reports;

/// <summary>
/// Displays a single page of a rendered report with zoom, scrollbars, drop-shadow,
/// and page navigation. Contains no toolbar or status bar — designed to be embedded
/// inside a <see cref="ReportViewer"/> or used standalone.
/// </summary>
public class ReportPreviewControl : Control
{
    private Report? _report;
    private List<ReportPage> _pages = new();
    private int _currentPageIndex;
    private float _zoom = 1.0f;
    private bool _rendered;

    private readonly ScrollBarEngine _vScrollBar = new() { Orientation = ScrollBarEngine.ScrollBarOrientation.Vertical };
    private readonly ScrollBarEngine _hScrollBar = new() { Orientation = ScrollBarEngine.ScrollBarOrientation.Horizontal };
    private readonly ScrollBarContext _scrollBarCtx;

    private static readonly Color PageShadowColor = Color.FromArgb(128, 128, 128);
    private static readonly Color PageBorderColor = Color.FromArgb(160, 160, 160);

    /// <summary>
    /// Raised whenever <see cref="CurrentPageIndex"/> or <see cref="PageCount"/> changes.
    /// </summary>
    public event EventHandler? PageChanged;

    /// <summary>
    /// Gets or sets the report to display. Call <see cref="RefreshReport"/> after setting.
    /// </summary>
    public Report? Report
    {
        get => _report;
        set
        {
            _report = value;
            _rendered = false;
            Invalidate();
        }
    }

    public IReadOnlyList<ReportPage> Pages => _pages;

    /// <summary>
    /// Gets or sets the current page index (0-based).
    /// </summary>
    public int CurrentPageIndex
    {
        get => _currentPageIndex;
        set
        {
            int clamped = Math.Clamp(value, 0, Math.Max(0, _pages.Count - 1));
            if (_currentPageIndex != clamped)
            {
                _currentPageIndex = clamped;
                _vScrollBar.Value = 0;
                _hScrollBar.Value = 0;
                Invalidate();
                OnPageChanged();
            }
        }
    }

    public int CurrentPage => _currentPageIndex + 1;
    public int PageCount => _pages.Count;

    /// <summary>
    /// Gets or sets the zoom factor (0.25 – 4.0).
    /// </summary>
    public float Zoom
    {
        get => _zoom;
        set
        {
            _zoom = Math.Clamp(value, 0.25f, 4.0f);
            Invalidate();
        }
    }

    /// <summary>
    /// Initializes a new ReportPreviewControl.
    /// </summary>
    public ReportPreviewControl()
    {
        _scrollBarCtx = new ScrollBarContext(this);

        _vScrollBar.SmallChange = 16;
        _vScrollBar.LargeChange = 80;
        _vScrollBar.Scroll += (_, _) => Invalidate();
        _vScrollBar.ValueChanged += (_, _) => Invalidate();

        _hScrollBar.SmallChange = 16;
        _hScrollBar.LargeChange = 80;
        _hScrollBar.Scroll += (_, _) => Invalidate();
        _hScrollBar.ValueChanged += (_, _) => Invalidate();
    }

    /// <summary>
    /// Renders (or re-renders) the report.
    /// </summary>
    public void RefreshReport()
    {
        if (_report == null)
        {
            _pages = new List<ReportPage>();
            _rendered = true;
            _currentPageIndex = 0;
            _vScrollBar.Value = 0;
            _hScrollBar.Value = 0;
            Invalidate();
            OnPageChanged();
            return;
        }

        var engine = new ReportRenderEngine();
        _pages = engine.Render(_report, 96f);

        _currentPageIndex = 0;
        _vScrollBar.Value = 0;
        _hScrollBar.Value = 0;
        _rendered = true;
        Invalidate();
        OnPageChanged();
    }

    public void FirstPage() => CurrentPageIndex = 0;
    public void PreviousPage() => CurrentPageIndex--;
    public void NextPage() => CurrentPageIndex++;
    public void LastPage() => CurrentPageIndex = Math.Max(0, _pages.Count - 1);

    public override void Render(Graphics g)
    {
        var theme = ThemeManager.CurrentTheme;

        g.Save();
        g.SetClip(new Rectangle(0, 0, Width, Height));

        if (!_rendered || _report == null || _pages.Count == 0)
        {
            g.FillRectangle(theme.ControlBackground, 0, 0, Width, Height);
            string msg = _report == null ? "No report assigned" : "Click RefreshReport to render";
            var font = new Font("Arial", 14);
            g.DrawString(msg, font, theme.GrayText, 20, 20);
            g.ResetClip();
            g.Restore();
            base.Render(g);
            return;
        }

        UpdateScrollBarRanges();

        var page = _pages[_currentPageIndex];
        int sbSize = ScrollBarEngine.DefaultScrollBarSize;
        bool needV = _vScrollBar.NeedsScrollbar;
        bool needH = _hScrollBar.NeedsScrollbar;

        int viewW = Width - (needV ? sbSize : 0);
        int viewH = Height - (needH ? sbSize : 0);

        float pageW = page.Width.ToPixelF(96) * _zoom;
        float pageH = page.Height.ToPixelF(96) * _zoom;

        float baseX = Math.Max(0, (viewW - pageW) / 2f);
        float baseY = Math.Max(0, (viewH - pageH) / 2f);

        float offsetX = baseX - _hScrollBar.Value;
        float offsetY = baseY - _vScrollBar.Value;

        g.FillRectangle(theme.ControlBackground, 0, 0, Width, Height);

        g.FillRectangle(PageShadowColor, offsetX + 4, offsetY + 4, pageW, pageH);
        g.FillRectangle(Color.White, offsetX, offsetY, pageW, pageH);
        g.DrawRectangle(PageBorderColor, offsetX, offsetY, pageW, pageH, 1);

        DrawPageMargins(g, offsetX, offsetY, pageW, pageH);

        g.ImportCommands(page.Graphics.GetCommands(), _zoom, offsetX, offsetY);

        if (needV)
            _vScrollBar.Render(g, new Rectangle(Width - sbSize, 0, sbSize, Height - (needH ? sbSize : 0)), theme);
        if (needH)
            _hScrollBar.Render(g, new Rectangle(0, Height - sbSize, Width - (needV ? sbSize : 0), sbSize), theme);

        g.ResetClip();
        g.Restore();
        base.Render(g);
    }

    private void UpdateScrollBarRanges()
    {
        var page = _pages[_currentPageIndex];
        float pageW = page.Width.ToPixelF(96) * _zoom;
        float pageH = page.Height.ToPixelF(96) * _zoom;

        int viewW = Width;
        int viewH = Height;

        float baseX = Math.Max(0, (viewW - pageW) / 2f);
        float baseY = Math.Max(0, (viewH - pageH) / 2f);

        float overflowX = (baseX + pageW) - viewW;
        float overflowY = (baseY + pageH) - viewH;

        _hScrollBar.ContentSize = (int)Math.Max(0, pageW + baseX);
        _hScrollBar.ViewSize = viewW;
        _hScrollBar.Maximum = (int)Math.Max(0, overflowX);

        _vScrollBar.ContentSize = (int)Math.Max(0, pageH + baseY);
        _vScrollBar.ViewSize = viewH;
        _vScrollBar.Maximum = (int)Math.Max(0, overflowY);
    }

    protected internal override void OnMouseWheel(EventArgs e)
    {
        if (e is MouseEventArgs mouseArgs)
        {
            if (_vScrollBar.NeedsScrollbar)
            {
                _vScrollBar.HandleMouseWheel(mouseArgs.Delta, _scrollBarCtx);
            }
            else if (_pages.Count > 1)
            {
                if (mouseArgs.Delta < 0) NextPage();
                else PreviousPage();
            }
        }
    }

    protected internal override void OnMouseDown(EventArgs e)
    {
        if (e is MouseEventArgs me)
        {
            int sbSize = ScrollBarEngine.DefaultScrollBarSize;
            bool needV = _vScrollBar.NeedsScrollbar;
            bool needH = _hScrollBar.NeedsScrollbar;

            if (needV && me.X >= Width - sbSize)
            {
                var sbBounds = new Rectangle(Width - sbSize, 0, sbSize, Height - (needH ? sbSize : 0));
                _vScrollBar.HandleMouseDown(new Point(me.X, me.Y), sbBounds, _scrollBarCtx);
                return;
            }
            if (needH && me.Y >= Height - sbSize)
            {
                var sbBounds = new Rectangle(0, Height - sbSize, Width - (needV ? sbSize : 0), sbSize);
                _hScrollBar.HandleMouseDown(new Point(me.X, me.Y), sbBounds, _scrollBarCtx);
                return;
            }
        }
        base.OnMouseDown(e);
    }

    protected internal override void OnMouseUp(EventArgs e)
    {
        _vScrollBar.HandleMouseUp(_scrollBarCtx);
        _hScrollBar.HandleMouseUp(_scrollBarCtx);
        base.OnMouseUp(e);
    }

    protected internal override void OnMouseMove(EventArgs e)
    {
        if (e is MouseEventArgs me)
        {
            int sbSize = ScrollBarEngine.DefaultScrollBarSize;
            bool needV = _vScrollBar.NeedsScrollbar;
            bool needH = _hScrollBar.NeedsScrollbar;

            if (needV)
            {
                var sbBounds = new Rectangle(Width - sbSize, 0, sbSize, Height - (needH ? sbSize : 0));
                _vScrollBar.HandleMouseMove(new Point(me.X, me.Y), sbBounds, _scrollBarCtx);
            }
            if (needH)
            {
                var sbBounds = new Rectangle(0, Height - sbSize, Width - (needV ? sbSize : 0), sbSize);
                _hScrollBar.HandleMouseMove(new Point(me.X, me.Y), sbBounds, _scrollBarCtx);
            }
        }
        base.OnMouseMove(e);
    }

    protected internal override void OnKeyDown(KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.PageUp: case Keys.Left: case Keys.Up:
                PreviousPage(); e.Handled = true; break;
            case Keys.PageDown: case Keys.Right: case Keys.Down:
                NextPage(); e.Handled = true; break;
            case Keys.Home:
                FirstPage(); e.Handled = true; break;
            case Keys.End:
                LastPage(); e.Handled = true; break;
        }
    }

    protected virtual void OnPageChanged() => PageChanged?.Invoke(this, EventArgs.Empty);

    private void DrawPageMargins(Graphics g, float offsetX, float offsetY, float pageW, float pageH)
    {
        if (_report == null) return;

        float dpi = 96f;
        float lm = _report.LeftMargin.ToPixelF(dpi) * _zoom;
        float rm = _report.RightMargin.ToPixelF(dpi) * _zoom;
        float tm = _report.TopMargin.ToPixelF(dpi) * _zoom;
        float bm = _report.BottomMargin.ToPixelF(dpi) * _zoom;

        if (lm <= 0 && rm <= 0 && tm <= 0 && bm <= 0) return;

        var guideLine = Color.FromArgb(60, 80, 80, 80);
        float px = offsetX + lm;
        float py = offsetY + tm;
        float pw = pageW - lm - rm;
        float ph = pageH - tm - bm;
        g.DrawRectangle(guideLine, px, py, pw, ph, 1);
    }

    private sealed class ScrollBarContext : IScrollBarContext
    {
        private readonly ReportPreviewControl _owner;
        public float Zoom => _owner._zoom;
        public ScrollBarContext(ReportPreviewControl owner) => _owner = owner;
        public void Invalidate() => _owner.Invalidate();
        public void CaptureMouse(bool capture) => _owner.CapturingMouse = capture;
    }
}
