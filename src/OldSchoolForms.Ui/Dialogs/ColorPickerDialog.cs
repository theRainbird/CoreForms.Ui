using System.Reflection;
using OldSchoolForms.Ui.Controls.Advanced;
using OldSchoolForms.Ui.Controls.Basic;
using OldSchoolForms.Ui.Controls.Containers;
using OldSchoolForms.Ui.Core;
using OldSchoolForms.Ui.Layout;
using OldSchoolForms.Ui.Rendering;
using OldSchoolForms.Ui.Theming;
using Graphics = OldSchoolForms.Ui.Rendering.Graphics;

namespace OldSchoolForms.Ui.Dialogs
{
    /// <summary>
    /// A modal dialog that allows users to select a color from system colors or a custom palette.
    /// Opens as a separate top-level window using the Silk.NET platform layer.
    /// </summary>
    public class ColorPickerDialog : Form
    {
        private TabControl? _tabControl;
        private DataGridView? _systemColorsGrid;
        private ScrollablePalettePanel? _scrollablePalette;
        private TextBox? _rBox;
        private TextBox? _gBox;
        private TextBox? _bBox;
        private TextBox? _aBox;
        private Label? _labelR;
        private Label? _labelG;
        private Label? _labelB;
        private Label? _labelA;

        private Color _currentSelectedColor;
        private bool _isSystemColor;
        private bool _updatingRgbBoxes;
        private volatile bool _isClosed;

        /// <summary>
        /// Gets the selected color.
        /// </summary>
        public Color SelectedColor => _currentSelectedColor;

        /// <summary>
        /// Gets whether the selected color is a system color.
        /// </summary>
        public bool IsSystemColor => _isSystemColor;

        private struct SystemColorEntry
        {
            public string Name;
            public Color Color;
        }

        private List<SystemColorEntry> _systemColors = new();

        /// <summary>
        /// Opens the color picker dialog as a separate top-level window and blocks until the user clicks OK or Cancel.
        /// </summary>
        /// <param name="initialColor">The color to initially display.</param>
        /// <param name="isSystemColor">Whether the initial color is a system color.</param>
        /// <param name="owner">The owner form, or null.</param>
        /// <returns>DialogResult.OK if confirmed, DialogResult.Cancel if cancelled.</returns>
        public static DialogResult ShowDialog(Color initialColor, bool isSystemColor, Form? owner = null)
        {
            var dialog = 
                new ColorPickerDialog(initialColor, isSystemColor)
                {
                    Text = LangRes.GetString("ColorPickerDialog_Title")
                };
            
            return dialog.ShowDialog(owner);
        }
        
        /// <summary>
        /// Constructor.
        /// </summary>
        private ColorPickerDialog(Color initialColor, bool isSystemColor)
        {
            _currentSelectedColor = initialColor;
            _isSystemColor = isSystemColor;

            Size = new Size(450, 570);
            FormBorderStyle = FormBorderStyle.Sizable;
            
            _tabControl = new TabControl { Dock = DockStyle.Fill, Name = nameof(_tabControl) };
            Controls.Add(_tabControl);

            // --- Tab 1: System Colors ---
            var systemTab = new TabPage { Text = LangRes.GetString("ColorPickerDialog_SystemColorsTab") };
            
            _systemColorsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.RowHeaderSelect,
                ShowGridLines = true
            };
            _systemColorsGrid.CellClick += OnSystemColorsGridCellClick;
            _systemColorsGrid.CellPainting += OnSystemColorsGridCellPainting;
            _systemColorsGrid.CellFormatting += OnSystemColorsGridCellFormatting;
            systemTab.Controls.Add(_systemColorsGrid);
            _tabControl.TabPages.Add(systemTab);

            // --- Tab 2: Palette & RGB ---
            var paletteTab = new TabPage { Text = LangRes.GetString("ColorPickerDialog_PaletteTab") };

            _scrollablePalette = new ScrollablePalettePanel
            {
                Dock = DockStyle.Fill
            };
            _scrollablePalette.ColorSelected += color => SelectColor(color, false);
            paletteTab.Controls.Add(_scrollablePalette);

            // RGB input area at bottom
            var rgbPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Core.Color.Transparent
            };

            const int textW = 40;
            const int gap = 4;
            int tX0 = 15;
            int tX1 = tX0 + textW + gap;
            int tX2 = tX1 + textW + gap;
            int tX3 = tX2 + textW + gap;

            _labelR = new Label { Text = LangRes.GetString("ColorPickerDialog_LabelR"), Width = 14, Height = 14, X = tX0 + (textW - 14) / 2, Y = 5 };
            _rBox = new TextBox { X = tX0, Y = 22, Width = textW, Height = 24, Text = "0" };
            _labelG = new Label { Text = LangRes.GetString("ColorPickerDialog_LabelG"), Width = 14, Height = 14, X = tX1 + (textW - 14) / 2, Y = 5 };
            _gBox = new TextBox { X = tX1, Y = 22, Width = textW, Height = 24, Text = "0" };
            _labelB = new Label { Text = LangRes.GetString("ColorPickerDialog_LabelB"), Width = 14, Height = 14, X = tX2 + (textW - 14) / 2, Y = 5 };
            _bBox = new TextBox { X = tX2, Y = 22, Width = textW, Height = 24, Text = "0" };
            _labelA = new Label { Text = LangRes.GetString("ColorPickerDialog_LabelA"), Width = 14, Height = 14, X = tX3 + (textW - 14) / 2, Y = 5 };
            _aBox = new TextBox { X = tX3, Y = 22, Width = textW, Height = 24, Text = "255" };

            rgbPanel.Controls.AddRange([_labelR!, _rBox!, _labelG!, _gBox!, _labelB!, _bBox!, _labelA!, _aBox!]);
            paletteTab.Controls.Add(rgbPanel);

            // RGB events
            _rBox.TextChanged += (s, e) => UpdateColorFromRGB();
            _gBox.TextChanged += (s, e) => UpdateColorFromRGB();
            _bBox.TextChanged += (s, e) => UpdateColorFromRGB();
            _aBox.TextChanged += (s, e) => UpdateColorFromRGB();

            _tabControl.TabPages.Add(paletteTab);

            LoadSystemColors();
            PopulatePalette();

            // Set initial state
            if (_isSystemColor)
            {
                _tabControl.SelectedIndex = 0;
                SelectSystemColor(_currentSelectedColor);
            }
            else
            {
                _tabControl.SelectedIndex = 1;
                UpdateRGBBoxes(_currentSelectedColor);
            }

            // Add OK/Cancel buttons at the bottom of the dialog
            var buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 50 };
            var btnOk = new Button { Text = LangRes.GetString("OK"), Width = 75, Height = 30, X = 280, Y = 10 };
            var btnCancel = new Button { Text = LangRes.GetString("Cancel"), Width = 75, Height = 30, X = 365, Y = 10 };
            btnOk.Click += (s, e) => CloseDialog(true);
            btnCancel.Click += (s, e) => CloseDialog(false);
            buttonPanel.Controls.AddRange([btnOk, btnCancel]);
            Controls.Add(buttonPanel);
        }

        private void CloseDialog(bool ok)
        {
            DialogResult = ok ? DialogResult.OK : DialogResult.Cancel;
            _modal = false;
            _isClosed = true;
        }

        private void LoadSystemColors()
        {
            var type = typeof(OldSchoolForms.Ui.Core.SystemColors);
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);

            var nameCol = 
                new DataGridViewColumn
                {
                    Name = "Name", 
                    HeaderText = LangRes.GetString("ColorPickerDialog_ColumnName"),
                    Width = 150
                };
            
            _systemColorsGrid!.Columns.Add(nameCol);
            
            var colorCol = 
                new DataGridViewColumn
                {
                    Name = "Color", 
                    HeaderText = LangRes.GetString("ColorPickerDialog_ColumnColor"),
                    Width = 120,
                    CellEditType = DataGridViewColumnEditType.None
                };
            
            _systemColorsGrid!.Columns.Add(colorCol);

            int fieldIdx = 0;
            
            foreach (var field in fields)
            {
                if (field.FieldType == typeof(Color))
                {
                    try
                    {
                        var color = (Color)field.GetValue(null)!;
                        var name = field.Name;
                        Console.WriteLine($"[CPD] SystemColor idx={fieldIdx} name={name}");
                        _systemColors.Add(new SystemColorEntry { Name = name, Color = color });
                        fieldIdx++;
                    }
                    catch { }
                }
            }

            foreach (var entry in _systemColors)
            {
                var row = new DataGridViewRow();
                row.Cells.Add(new DataGridViewCell { Value = entry.Name });
                row.Cells.Add(new DataGridViewCell { Value = entry.Color });
                _systemColorsGrid.Rows.Add(row);
            }
        }

        private void PopulatePalette()
        {
            var type = typeof(OldSchoolForms.Ui.Core.Color);
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);

            var colors = new List<Core.Color>();
            foreach (var field in fields)
            {
                if (field.FieldType == typeof(Core.Color) && field.Name is not "Empty" and not "Transparent")
                {
                    try
                    {
                        var color = (Core.Color)field.GetValue(null)!;
                        colors.Add(color);
                    }
                    catch { }
                }
            }

            colors.Sort((a, b) => GetHue(a).CompareTo(GetHue(b)));

            foreach (var color in colors)
                _scrollablePalette!.AddColor(color);

            _scrollablePalette.Relayout();
        }

        private static float GetHue(Core.Color color)
        {
            float r = color.R / 255f;
            float g = color.G / 255f;
            float b = color.B / 255f;
            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));
            float delta = max - min;
            if (delta == 0) return 0;
            float hue;
            if (max == r) hue = 60 * (((g - b) / delta) % 6);
            else if (max == g) hue = 60 * (((b - r) / delta) + 2);
            else hue = 60 * (((r - g) / delta) + 4);
            if (hue < 0) hue += 360;
            return hue;
        }
        
        private void OnSystemColorsGridCellClick(object? sender, EventArgs e)
        {
            if (e is DataGridViewCellEventArgs cellArgs && cellArgs.RowIndex >= 0 && cellArgs.RowIndex < _systemColors.Count)
            {
                var entry = _systemColors[cellArgs.RowIndex];
                _currentSelectedColor = entry.Color;
                _isSystemColor = true;
                _systemColorsGrid!.SelectedRowIndex = cellArgs.RowIndex;
            }
        }

        private void OnSystemColorsGridCellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 1)
            {
                var row = _systemColorsGrid!.Rows[e.RowIndex];
                if (row.Cells.Count > 1 && row.Cells[1].Value is Color color)
                {
                    var theme = ThemeManager.CurrentTheme;
                    bool isSelected = e.RowIndex == _systemColorsGrid.SelectedRowIndex;
                    var textColor = isSelected ? theme.HighlightText : theme.DataGridViewCellText;
                    var font = EffectiveFont;
                    var zoom = EffectiveZoom;
                    
                    // Set clip to cell bounds to prevent text overflow
                    e.Graphics.SetClip(e.CellBounds);
                    
                    // Draw small color swatch rectangle (16x16 pixels)
                    const int swatchSize = 16;
                    int swatchX = e.CellBounds.X + 6;
                    int swatchY = e.CellBounds.Y + (e.CellBounds.Height - swatchSize) / 2;
                    e.Graphics.FillRectangle(color, swatchX, swatchY, swatchSize, swatchSize);
                    
                    // Draw RGBA text to the right of the swatch
                    string rgbaText = $"{color.R}, {color.G}, {color.B}, {color.A}";
                    float textX = swatchX + swatchSize + 6;
                    float textY = e.CellBounds.Y + (e.CellBounds.Height - font.Size * zoom) / 2f;
                    e.Graphics.DrawString(rgbaText, font, textColor, textX, textY);
                    
                    e.Graphics.ResetClip();
                    e.Handled = true;
                }
            }
        }

        private void OnSystemColorsGridCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < _systemColorsGrid!.Rows.Count)
            {
                var row = _systemColorsGrid.Rows[e.RowIndex];
                if (e.ColumnIndex == 1 && row.Cells.Count > 1 && row.Cells[1].Value is Color color)
                {
                    e.Value = $"{color.R}, {color.G}, {color.B}, {color.A}";
                    e.FormattingApplied = true;
                }
            }
        }

        private void SelectColor(Color color, bool isSystem)
        {
            _currentSelectedColor = color;
            _isSystemColor = isSystem;

            if (isSystem)
            {
                SelectSystemColor(color);
            }
            else
            {
                UpdateRGBBoxes(color);
            }
        }

        private void SelectSystemColor(Color color)
        {
            int bestIdx = -1;
            int bestDist = int.MaxValue;
            for (int i = 0; i < _systemColors.Count; i++)
            {
                var sc = _systemColors[i].Color;
                if (sc.R == color.R && sc.G == color.G && sc.B == color.B && sc.A == color.A)
                {
                    int dist = Math.Abs(i - _systemColorsGrid!.SelectedRowIndex);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestIdx = i;
                    }
                }
            }
            if (bestIdx != -1)
                _systemColorsGrid!.SelectedRowIndex = bestIdx;
        }

        private void UpdateRGBBoxes(Color color)
        {
            _updatingRgbBoxes = true;
            _rBox!.Text = color.R.ToString();
            _gBox!.Text = color.G.ToString();
            _bBox!.Text = color.B.ToString();
            _aBox!.Text = color.A.ToString();
            _updatingRgbBoxes = false;
        }

        private void UpdateColorFromRGB()
        {
            if (_updatingRgbBoxes) return;
            if (_rBox == null || _gBox == null || _bBox == null || _aBox == null) return;
            
            try
            {
                int r = int.Parse(_rBox.Text);
                int g = int.Parse(_gBox.Text);
                int b = int.Parse(_bBox.Text);
                int a = 255;
                if (int.TryParse(_aBox.Text, out int parsedA)) a = parsedA;

                SelectColor(Color.FromArgb(a, r, g, b), false);
            }
            catch { }
        }

        /// <summary>
        /// A scrollable panel that displays color swatches arranged in a grid,
        /// sorted by hue. Supports vertical scrolling via ScrollBarEngine.
        /// Mouse hit-testing is handled directly without relying on child-button dispatch.
        /// </summary>
        private sealed class ScrollablePalettePanel : ContainerControl
        {
            private readonly ScrollBarEngine _scrollBar = new();
            private int _scrollOffset;
            private readonly List<(Rectangle Bounds, Core.Color Color)> _entries = new();
            private bool _captured;
            private const int SwatchSize = 28;
            private const int Gap = 3;
            private int _cachedContentHeight;

            /// <summary>
            /// Occurs when a color swatch is clicked.
            /// </summary>
            public event Action<Core.Color>? ColorSelected;

            /// <summary>
            /// Initializes a new instance of <see cref="ScrollablePalettePanel"/>.
            /// </summary>
            public ScrollablePalettePanel()
            {
                TabStop = false;
                _scrollBar.Orientation = ScrollBarEngine.ScrollBarOrientation.Vertical;
                _scrollBar.Scroll += (s, e) =>
                {
                    _scrollOffset = _scrollBar.Value;
                    Invalidate();
                };
            }

            /// <summary>
            /// Adds a color swatch to the palette.
            /// </summary>
            /// <param name="color">The color to add.</param>
            public void AddColor(Core.Color color)
            {
                _entries.Add((default, color));
            }

            /// <summary>
            /// Removes all color swatches.
            /// </summary>
            public void Clear()
            {
                _entries.Clear();
                _scrollOffset = 0;
                _scrollBar.ScrollTo(0);
                Invalidate();
            }

            /// <summary>
            /// Recalculates the grid layout and scroll bar parameters.
            /// </summary>
            public void Relayout()
            {
                int cols = Math.Max(1, (Width - ScrollBarEngine.DefaultScrollBarSize - Gap) / (SwatchSize + Gap));

                for (int i = 0; i < _entries.Count; i++)
                {
                    int col = i % cols;
                    int row = i / cols;
                    int x = Gap + col * (SwatchSize + Gap);
                    int y = Gap + row * (SwatchSize + Gap);
                    _entries[i] = (new Rectangle(x, y, SwatchSize, SwatchSize), _entries[i].Color);
                }

                int rows = (_entries.Count + cols - 1) / cols;
                _cachedContentHeight = Gap + rows * (SwatchSize + Gap);

                _scrollBar.SmallChange = SwatchSize + Gap;
                _scrollBar.LargeChange = Height;
                _scrollBar.ViewSize = Height;
                _scrollBar.ContentSize = _cachedContentHeight;

                int maxOffset = _scrollBar.MaxScroll;
                if (_scrollOffset > maxOffset)
                {
                    _scrollOffset = maxOffset;
                    _scrollBar.ScrollTo(maxOffset);
                }
                Invalidate();
            }

            /// <summary>
            /// Renders the palette panel, applying scroll offset and drawing the scrollbar.
            /// </summary>
            public override void Render(Graphics g)
            {
                if (!Visible) return;

                g.Zoom = EffectiveZoom;
                var theme = ThemeManager.CurrentTheme;
                g.FillRectangle(theme.TabContentBackground, 0, 0, Width, Height);

                int sbw = ScrollBarEngine.DefaultScrollBarSize;
                int contentWidth = Width - sbw;
                g.SetClip(new Rectangle(0, 0, contentWidth, Height));

                g.Save();
                g.TranslateTransform(0, -_scrollOffset);

                bool needsScrollbar = _scrollBar.NeedsScrollbar;
                for (int i = 0; i < _entries.Count; i++)
                {
                    var (rect, color) = _entries[i];
                    if (rect.Bottom <= _scrollOffset) continue;
                    if (rect.Y >= _scrollOffset + Height) break;

                    g.FillRectangle(color, rect.X, rect.Y, rect.Width, rect.Height);
                    g.DrawRectangle(theme.ButtonBorder, rect.X, rect.Y, rect.Width, rect.Height, 1);
                }

                g.Restore();
                g.ResetClip();

                if (needsScrollbar)
                {
                    var sbBounds = new Rectangle(Width - sbw, 0, sbw, Height);
                    _scrollBar.Render(g, sbBounds, theme);
                }
            }

            /// <summary>
            /// Handles mouse down events. Intercepts scrollbar clicks; finds palette color
            /// at the click position directly.
            /// </summary>
            protected internal override void OnMouseDown(EventArgs e)
            {
                if (e is MouseEventArgs args)
                {
                    int sbw = ScrollBarEngine.DefaultScrollBarSize;
                    if (_scrollBar.NeedsScrollbar && args.X >= Width - sbw)
                    {
                        var sbBounds = new Rectangle(Width - sbw, 0, sbw, Height);
                        _scrollBar.HandleMouseDown(new Point(args.X, args.Y), sbBounds, new PaletteScrollBarContext(this));
                        return;
                    }

                    var contentPt = new Point(args.X, args.Y + _scrollOffset);
                    for (int i = _entries.Count - 1; i >= 0; i--)
                    {
                        var (rect, color) = _entries[i];
                        if (rect.Contains(contentPt.X, contentPt.Y))
                        {
                            ColorSelected?.Invoke(color);
                            return;
                        }
                    }
                }
                base.OnMouseDown(e);
            }

            /// <summary>
            /// Handles mouse up events for the scrollbar.
            /// </summary>
            protected internal override void OnMouseUp(EventArgs e)
            {
                if (_scrollBar.IsDragging || _scrollBar.IsUpButtonPressed || _scrollBar.IsDownButtonPressed)
                {
                    _scrollBar.HandleMouseUp(new PaletteScrollBarContext(this));
                    return;
                }
                base.OnMouseUp(e);
            }

            /// <summary>
            /// Handles mouse move events for scrollbar hover and drag.
            /// </summary>
            protected internal override void OnMouseMove(EventArgs e)
            {
                if (e is MouseEventArgs args && _scrollBar.NeedsScrollbar)
                {
                    var sbBounds = new Rectangle(Width - ScrollBarEngine.DefaultScrollBarSize, 0, ScrollBarEngine.DefaultScrollBarSize, Height);
                    _scrollBar.HandleMouseMove(new Point(args.X, args.Y), sbBounds, new PaletteScrollBarContext(this));
                }
                base.OnMouseMove(e);
            }

            /// <summary>
            /// Handles mouse wheel events for scrolling.
            /// </summary>
            protected internal override void OnMouseWheel(EventArgs e)
            {
                if (e is MouseEventArgs args && _scrollBar.NeedsScrollbar)
                {
                    _scrollBar.HandleMouseWheel(args.Delta, new PaletteScrollBarContext(this));
                    return;
                }
                base.OnMouseWheel(e);
            }

            /// <summary>
            /// Handles mouse leave events, resetting scrollbar hover states.
            /// </summary>
            protected override void OnMouseLeave(EventArgs e)
            {
                _scrollBar.HandleMouseLeave(new PaletteScrollBarContext(this));
                base.OnMouseLeave(e);
            }

            private sealed class PaletteScrollBarContext : IScrollBarContext
            {
                private readonly ScrollablePalettePanel _owner;
                public PaletteScrollBarContext(ScrollablePalettePanel owner) => _owner = owner;
                public float Zoom => _owner.EffectiveZoom;
                public void Invalidate() => _owner.Invalidate();
                public void CaptureMouse(bool capture) => _owner._captured = capture;
            }
        }
    }
}
