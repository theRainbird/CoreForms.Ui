using CoreForms.Ui.Core;
using CoreForms.Ui.Theming;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Basic;

/// <summary>
/// A control that displays a list of items from which the user can select.
/// </summary>
public class ListBox : Control
{
    private readonly List<object> _items = new();
    private int _selectedIndex = -1;

    /// <summary>
    /// Initializes a new instance of ListBox.
    /// </summary>
    public ListBox()
    {
        var theme = ThemeManager.CurrentTheme;
        _backColor = theme.TextBoxBackground;
        _foreColor = theme.TextBoxText;
        Size = new Size(150, 120);
        TabStop = true;
    }

    /// <summary>
    /// Called when the theme changes. Updates listbox-specific colors.
    /// </summary>
    /// <param name="newTheme">The new theme that was activated.</param>
    public override void OnThemeChanged(Theme newTheme)
    {
        if (!_backColorSet)
            _backColor = newTheme.TextBoxBackground;
        if (!_foreColorSet)
            _foreColor = newTheme.TextBoxText;
        Invalidate();
    }

    /// <summary>
    /// Gets the collection of items in the list box.
    /// </summary>
    public List<object> Items => _items;

    /// <summary>
    /// Gets or sets the index of the selected item.
    /// </summary>
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (_selectedIndex != value && value >= -1 && value < _items.Count)
            {
                _selectedIndex = value;
                OnSelectedIndexChanged();
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets the selected item.
    /// </summary>
    public object? SelectedItem => _selectedIndex >= 0 && _selectedIndex < _items.Count
        ? _items[_selectedIndex]
        : null;

    /// <summary>
    /// Renders the list box with its items.
    /// </summary>
    /// <param name="g">The Graphics object to use for rendering.</param>
    public override void Render(Rendering.Graphics g)
    {
        if (!Visible) return;

        var theme = ThemeManager.CurrentTheme;

        g.FillRectangle(BackColor, 0, 0, Width, Height);

        if (Focused)
            g.DrawRectangle(theme.TextBoxFocusBorder, 0, 0, Width, Height, 2);
        else
            g.DrawRectangle(theme.TextBoxBorder, 0, 0, Width, Height, 1);

        var font = EffectiveFont;
        var itemHeight = CoordinateTransform.GetItemHeight(font, EffectiveZoom);
        var y = 2;

        var textColor = Enabled ? ForeColor : theme.GrayText;
        for (int i = 0; i < _items.Count && y < Height; i++)
        {
            var isSelected = i == _selectedIndex;

            if (isSelected)
            {
                if (Enabled)
                    g.FillRectangle(SystemColors.Highlight, 1, y, Width - 2, itemHeight);
                g.DrawString(_items[i]?.ToString() ?? "", font, Enabled ? SystemColors.HighlightText : textColor, 4, y + 2);
            }
            else
            {
                g.DrawString(_items[i]?.ToString() ?? "", font, textColor, 4, y + 2);
            }

            y += itemHeight;
        }

        base.Render(g);
    }

    /// <summary>
    /// Raises the MouseDown event and selects an item.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected internal override void OnMouseDown(EventArgs e)
    {
        if (!Enabled) return;
        var mouseArgs = e as MouseEventArgs;
        if (mouseArgs != null)
        {
            var font = EffectiveFont;
            var itemHeight = CoordinateTransform.GetItemHeight(font, EffectiveZoom);
            var index = (mouseArgs.Y - 2) / itemHeight;

            if (index >= 0 && index < _items.Count)
            {
                SelectedIndex = index;
            }
        }

        base.OnMouseDown(e);
    }

    /// <summary>
    /// Raises the KeyDown event to handle navigation.
    /// </summary>
    /// <param name="e">A KeyEventArgs that contains the event data.</param>
    protected internal override void OnKeyDown(KeyEventArgs e)
    {
        if (!Enabled) return;
        switch (e.KeyCode)
        {
            case Keys.Up:
                if (_selectedIndex > 0)
                {
                    SelectedIndex--;
                    e.Handled = true;
                }
                break;
            case Keys.Down:
                if (_selectedIndex < _items.Count - 1)
                {
                    SelectedIndex++;
                    e.Handled = true;
                }
                break;
            case Keys.Home:
                if (_items.Count > 0)
                {
                    SelectedIndex = 0;
                    e.Handled = true;
                }
                break;
            case Keys.End:
                if (_items.Count > 0)
                {
                    SelectedIndex = _items.Count - 1;
                    e.Handled = true;
                }
                break;
        }
        base.OnKeyDown(e);
    }

    /// <summary>
    /// Raises the SelectedIndexChanged event.
    /// </summary>
    protected virtual void OnSelectedIndexChanged()
    {
        SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Gets the value of this control to copy to the clipboard.
    /// Returns the selected item's text.
    /// </summary>
    /// <returns>The selected item as string, or null if no selection.</returns>
    protected string? GetClipboardValue() => SelectedItem?.ToString();

    /// <summary>
    /// Occurs when the selected index changes.
    /// </summary>
    public event EventHandler? SelectedIndexChanged;
}