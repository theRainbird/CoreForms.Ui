using System.Reflection;
using CoreForms.Ui.Controls.Advanced;
using CoreForms.Ui.Controls.Basic;
using CoreForms.Ui.Controls.Containers;
using CoreForms.Ui.Core;
using CoreForms.Ui.Layout;
using CoreForms.Ui.Theming;

namespace CoreForms.Ui.Dialogs
{
    /// <summary>
    /// A modal dialog that allows users to select a color from system colors or a custom palette.
    /// Opens as a separate top-level window using the Silk.NET platform layer.
    /// </summary>
    public class ColorPickerDialog : Form
    {
        private TabControl? _tabControl;
        private DataGridView? _systemColorsGrid;
        private FlowLayoutPanel? _palettePanel;
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
            var dialog = new ColorPickerDialog(initialColor, isSystemColor);

            if (owner != null)
            {
                dialog.Zoom = owner.Zoom;
            }
            else
            {
                var activeForm = CoreForms.Ui.Platform.Platform.FocusedWindow;
                if (activeForm != null)
                    dialog.Zoom = activeForm.Zoom;
            }

            dialog.Size = new Size(450, 500);

            dialog.Text = LangRes.GetString("ColorPickerDialog_Title");
            dialog.FormBorderStyle = FormBorderStyle.FixedDialog;

            return dialog.ShowDialog(owner);
        }
        
        /// <summary>
        /// Constructor.
        /// </summary>
        public ColorPickerDialog(Color initialColor, bool isSystemColor)
        {
            _currentSelectedColor = initialColor;
            _isSystemColor = isSystemColor;
            
            SetupDialog();
        }

        // protected internal override void OnShown(EventArgs e)
        // {
        //     base.OnShown(e);
        //     
        //     MarkLayoutDirty();
        //     PerformLayout();
        // }

        private void SetupDialog()
        {
            _tabControl = new TabControl { Dock = DockStyle.Fill };
            Controls.Add(_tabControl!);

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
            var mainSplit = new SplitPanel { Dock = DockStyle.Fill, Orientation = SplitOrientation.Vertical, SplitterDistance = 300 };
            paletteTab.Controls.Add(mainSplit);

            _palettePanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = 5
            };
            mainSplit.Panel1.Controls.Add(_palettePanel);

            var rgbPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            mainSplit.Panel2.Controls.Add(rgbPanel);

            _labelR = new Label { Text = LangRes.GetString("ColorPickerDialog_LabelR"), Width = 20, X = 10, Y = 10 };
            _rBox = new TextBox { X = 35, Y = 10, Width = 40 };
            _labelG = new Label { Text = LangRes.GetString("ColorPickerDialog_LabelG"), Width = 20, X = 10, Y = 40 };
            _gBox = new TextBox { X = 35, Y = 40, Width = 40 };
            _labelB = new Label { Text = LangRes.GetString("ColorPickerDialog_LabelB"), Width = 20, X = 10, Y = 70 };
            _bBox = new TextBox { X = 35, Y = 70, Width = 40 };
            _labelA = new Label { Text = LangRes.GetString("ColorPickerDialog_LabelA"), Width = 20, X = 10, Y = 100 };
            _aBox = new TextBox { X = 35, Y = 100, Width = 40 };

            rgbPanel.Controls.AddRange(new Control[] { _labelR!, _rBox!, _labelG!, _gBox!, _labelB!, _bBox!, _labelA!, _aBox! });

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
            var type = typeof(CoreForms.Ui.Core.SystemColors);
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
                    Width = 120
                };
            colorCol.CellEditType = DataGridViewColumnEditType.None;
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
            string[] colorNames = { "Red", "Green", "Blue", "Yellow", "Cyan", "Magenta", "Black", "White", "Gray", "Silver", "Maroon", "Olive", "Purple", "Teal", "Navy", "Orange" };
            foreach (var name in colorNames)
            {
                var color = GetColorByName(name);
                AddPaletteButton(color);
            }
        }

        private void AddPaletteButton(Color color)
        {
            var btn = new Button
            {
                Size = new Size(32, 32),
                BackColor = color
            };
            btn.Click += (s, e) => SelectColor(color, false);
            _palettePanel!.Controls.Add(btn);
        }
        
        private static Color GetColorByName(string name)
        {
            return name.ToLowerInvariant() switch
            {
                "red" => Color.FromArgb(255, 0, 0),
                "green" => Color.FromArgb(0, 128, 0),
                "blue" => Color.FromArgb(0, 0, 255),
                "yellow" => Color.FromArgb(255, 255, 0),
                "cyan" => Color.FromArgb(0, 255, 255),
                "magenta" => Color.FromArgb(255, 0, 255),
                "black" => Color.FromArgb(0, 0, 0),
                "white" => Color.FromArgb(255, 255, 255),
                "gray" => Color.FromArgb(128, 128, 128),
                "silver" => Color.FromArgb(192, 192, 192),
                "maroon" => Color.FromArgb(128, 0, 0),
                "olive" => Color.FromArgb(128, 128, 0),
                "purple" => Color.FromArgb(128, 0, 128),
                "teal" => Color.FromArgb(0, 128, 128),
                "navy" => Color.FromArgb(0, 0, 128),
                "orange" => Color.FromArgb(255, 165, 0),
                _ => Color.FromArgb(128, 128, 128)
            };
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
    }
}
