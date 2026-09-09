using System;
using System.Collections.Generic;
using System.Linq;
using OldSchoolForms.Ui.Controls.Advanced;
using OldSchoolForms.Ui.Controls.Basic;
using OldSchoolForms.Ui.Controls.Containers;
using OldSchoolForms.Ui.Core;
using Graphics = OldSchoolForms.Ui.Rendering.Graphics;

namespace OldSchoolForms.Ui.Dialogs
{
    /// <summary>
    /// A modal dialog that lets the user view, create, rename and delete the
    /// <see cref="TabPage"/> entries of a <see cref="TabControl"/>. The pages are
    /// listed in a <see cref="DataGridView"/> with inline editable Name and Caption
    /// columns. Changes are only applied to the control when the dialog is confirmed
    /// with OK; cancelling discards all edits without touching the control.
    /// </summary>
    public class TabPagesEditorDialog : Form
    {
        private TabControl? _tabControl;
        protected DataGridView? _grid;

        /// <summary>
        /// Opens the tab pages editor as a separate top-level window and blocks until
        /// the user clicks OK or Cancel.
        /// </summary>
        /// <param name="tabControl">The control whose tab pages are edited.</param>
        /// <param name="owner">The owner form, or null.</param>
        /// <returns>DialogResult.OK if confirmed, DialogResult.Cancel if cancelled.</returns>
        /// <summary>
        /// Shows the TabPages editor modal dialog for the given tab control.
        /// </summary>
        /// <param name="tabControl">The tab control whose pages are edited.</param>
        /// <param name="owner">The optional owning form; its zoom is inherited.</param>
        /// <returns>The dialog result (OK on commit, Cancel otherwise).</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="tabControl"/> is null.</exception>
        public static DialogResult ShowDialog(TabControl tabControl, Form? owner = null)
        {
            if (tabControl == null) throw new ArgumentNullException(nameof(tabControl));

            var dialog = new TabPagesEditorDialog(tabControl)
            {
                Text = LangRes.GetString("TabPagesDialog_Title")
            };

            return dialog.ShowDialog(owner);
        }

        internal TabPagesEditorDialog(TabControl tabControl)
        {
            _tabControl = tabControl;

            Size = new Size(560, 360);
            FormBorderStyle = FormBorderStyle.Sizable;

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                EditMode = DataGridViewEditMode.EditOnEnter,
                ColumnHeadersVisible = true,
                ShowGridLines = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = false
            };

            var nameColumn = new DataGridViewColumn
            {
                Name = "Name",
                HeaderText = LangRes.GetString("TabPagesDialog_Name"),
                Width = 200,
                CellEditType = DataGridViewColumnEditType.TextBox
            };

            var captionColumn = new DataGridViewColumn
            {
                Name = "Caption",
                HeaderText = LangRes.GetString("TabPagesDialog_Caption"),
                Width = 300,
                CellEditType = DataGridViewColumnEditType.TextBox
            };

            _grid.Columns.Add(nameColumn);
            _grid.Columns.Add(captionColumn);

            PopulateGrid();

            Controls.Add(_grid!);

            var buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 40 };

            var btnAdd = new Button
            {
                Text = LangRes.GetString("TabPagesDialog_Add"),
                Dock = DockStyle.Left,
                Width = 100,
                Height = 30
            };
            btnAdd.Click += (_, _) => AddRow();

            var btnDelete = new Button
            {
                Text = LangRes.GetString("TabPagesDialog_Delete"),
                Dock = DockStyle.Left,
                Width = 100,
                Height = 30
            };
            btnDelete.Click += (_, _) => DeleteSelectedRow();

            var btnOk = new Button
            {
                Text = LangRes.GetString("TabPagesDialog_Ok"),
                Dock = DockStyle.Right,
                Width = 75,
                Height = 30
            };
            btnOk.Click += (_, _) => CloseDialog(true);

            var btnCancel = new Button
            {
                Text = LangRes.GetString("TabPagesDialog_Cancel"),
                Dock = DockStyle.Right,
                Width = 75,
                Height = 30
            };
            btnCancel.Click += (_, _) => CloseDialog(false);

            buttonPanel.Controls.AddRange([btnAdd, btnDelete, btnCancel, btnOk]);
            Controls.Add(buttonPanel);
        }

        private void PopulateGrid()
        {
            if (_tabControl == null) return;

            foreach (var page in _tabControl.TabPages)
            {
                var row = new DataGridViewRow();
                row.Cells.Add(new DataGridViewCell { Value = page.Name });
                row.Cells.Add(new DataGridViewCell { Value = page.Text });
                row.DataBoundItem = page;
                _grid!.Rows.Add(row);
            }
        }

        private void AddRow()
        {
            if (_grid == null) return;

            var row = new DataGridViewRow();
            row.Cells.Add(new DataGridViewCell { Value = string.Empty });
            row.Cells.Add(new DataGridViewCell { Value = string.Empty });
            row.DataBoundItem = null;
            _grid.Rows.Add(row);
            _grid.SelectedRowIndex = _grid.Rows.Count - 1;
        }

        private void DeleteSelectedRow()
        {
            if (_grid == null) return;

            int index = _grid.SelectedRowIndex;
            if (index < 0 || index >= _grid.Rows.Count) return;

            var row = _grid.Rows[index];
            _grid.Rows.Remove(row);
            _grid.SelectedRowIndex = index < _grid.Rows.Count ? index : Math.Max(0, _grid.Rows.Count - 1);
        }

        private void CloseDialog(bool ok)
        {
            if (ok)
                ApplyChanges();

            DialogResult = ok ? DialogResult.OK : DialogResult.Cancel;
            _modal = false;
        }

        protected void ApplyChanges()
        {
            if (_tabControl == null || _grid == null) return;

            var stillPresent = new List<TabPage>();
            for (int i = 0; i < _grid.Rows.Count; i++)
            {
                var row = _grid.Rows[i];
                var page = row.DataBoundItem as TabPage;
                if (page == null) continue;

                page.Name = (row.Cells[0].Value as string) ?? string.Empty;
                page.Text = (row.Cells[1].Value as string) ?? string.Empty;
                stillPresent.Add(page);
            }

            foreach (var page in _tabControl.TabPages.Where(p => !stillPresent.Contains(p)).ToList())
                _tabControl.RemoveTabPage(page);

            for (int i = 0; i < _grid.Rows.Count; i++)
            {
                var row = _grid.Rows[i];
                if (row.DataBoundItem != null) continue;

                var newPage = new TabPage
                {
                    Name = (row.Cells[0].Value as string) ?? string.Empty,
                    Text = (row.Cells[1].Value as string) ?? string.Empty
                };
                _tabControl.AddTabPage(newPage);
            }

            _tabControl.Invalidate();
            _tabControl.PerformLayout();
        }

        /// <summary>
        /// Handles keyboard shortcuts. While the grid's inline editor is active, all keys are
        /// delegated to it so Enter commits the cell, Tab moves to the next column and Escape
        /// cancels the edit. Otherwise Enter confirms (OK) and Escape cancels the dialog.
        /// </summary>
        /// <param name="e">The key event data.</param>
        protected internal override void OnKeyDown(KeyEventArgs e)
        {
            if (_grid != null && _grid.IsCurrentCellInEditMode)
            {
                base.OnKeyDown(e);
                return;
            }

            if (e.KeyCode == Keys.Enter)
            {
                CloseDialog(true);
                e.Handled = true;
                return;
            }

            if (e.KeyCode == Keys.Escape)
            {
                CloseDialog(false);
                e.Handled = true;
                return;
            }

            base.OnKeyDown(e);
        }
    }
}
