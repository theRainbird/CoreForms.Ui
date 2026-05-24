using CoreForms.Ui.Core;
using CoreForms.Ui.Rendering;

namespace CoreForms.Ui.Reports;

/// <summary>
/// Displays a single page of a rendered report with zoom, drop-shadow, and page navigation.
/// Contains no toolbar or status bar — designed to be embedded inside a <see cref="ReportViewer"/>
/// or used standalone.
/// </summary>
public class ReportPreviewControl : Control
{
    private Report? _report;
    private List<ReportPage> _pages = new();
    private int _currentPageIndex;
    private float _zoom = 1.0f;
    private bool _rendered;

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

    /// <summary>
    /// Gets the list of rendered pages.
    /// </summary>
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
                Invalidate();
                OnPageChanged();
            }
        }
    }

    /// <summary>
    /// Gets the current page number (1-based).
    /// </summary>
    public int CurrentPage => _currentPageIndex + 1;

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
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
    /// Renders (or re-renders) the report. Call this after setting <see cref="Report"/>.
    /// </summary>
    public void RefreshReport()
    {
        if (_report == null)
        {
            _pages = new List<ReportPage>();
            _rendered = true;
            _currentPageIndex = 0;
            Invalidate();
            OnPageChanged();
            return;
        }

        var engine = new ReportRenderEngine();
        _pages = engine.Render(_report, 96f);

        _currentPageIndex = 0;
        _rendered = true;
        Invalidate();
        OnPageChanged();
    }

    /// <summary>
    /// Navigates to the first page.
    /// </summary>
    public void FirstPage() => CurrentPageIndex = 0;

    /// <summary>
    /// Navigates to the previous page.
    /// </summary>
    public void PreviousPage() => CurrentPageIndex--;

    /// <summary>
    /// Navigates to the next page.
    /// </summary>
    public void NextPage() => CurrentPageIndex++;

    /// <summary>
    /// Navigates to the last page.
    /// </summary>
    public void LastPage() => CurrentPageIndex = Math.Max(0, _pages.Count - 1);

    /// <summary>
    /// Renders the current report page centered in the available area with a drop shadow.
    /// </summary>
    public override void Render(Graphics g)
    {
        var theme = Theming.ThemeManager.CurrentTheme;
        g.FillRectangle(theme.ControlBackground, 0, 0, Width, Height);

        if (!_rendered || _report == null || _pages.Count == 0)
        {
            string msg = _report == null ? "No report assigned" : "Click RefreshReport to render";
            var font = new Font("Arial", 14);
            g.DrawString(msg, font, theme.GrayText, 20, 20);
            base.Render(g);
            return;
        }

        var page = _pages[_currentPageIndex];

        float pageW = page.Width.ToPixelF(96) * _zoom;
        float pageH = page.Height.ToPixelF(96) * _zoom;

        float offsetX = Math.Max(10, (Width - pageW) / 2f);
        float offsetY = Math.Max(10, (Height - pageH) / 2f);

        g.FillRectangle(PageShadowColor, offsetX + 4, offsetY + 4, pageW, pageH);
        g.FillRectangle(Color.White, offsetX, offsetY, pageW, pageH);
        g.DrawRectangle(PageBorderColor, offsetX, offsetY, pageW, pageH, 1);

        float savedZoom = g.Zoom;
        g.Zoom = _zoom;
        g.ImportCommands(page.Graphics.GetCommands(), offsetX, offsetY);
        g.Zoom = savedZoom;

        base.Render(g);
    }

    /// <summary>
    /// Handles mouse wheel for page navigation.
    /// </summary>
    protected internal override void OnMouseWheel(EventArgs e)
    {
        if (_pages.Count <= 1) return;

        if (e is MouseEventArgs mouseArgs)
        {
            if (mouseArgs.Delta < 0)
                NextPage();
            else
                PreviousPage();
        }
    }

    /// <summary>
    /// Handles keyboard navigation.
    /// </summary>
    protected internal override void OnKeyDown(KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.PageUp:
            case Keys.Left:
            case Keys.Up:
                PreviousPage();
                e.Handled = true;
                break;
            case Keys.PageDown:
            case Keys.Right:
            case Keys.Down:
                NextPage();
                e.Handled = true;
                break;
            case Keys.Home:
                FirstPage();
                e.Handled = true;
                break;
            case Keys.End:
                LastPage();
                e.Handled = true;
                break;
        }
    }

    /// <summary>
    /// Raises the <see cref="PageChanged"/> event.
    /// </summary>
    protected virtual void OnPageChanged()
    {
        PageChanged?.Invoke(this, EventArgs.Empty);
    }
}
