using OldSchoolForms.Ui.Core;

namespace OldSchoolForms.Ui.Controls.Advanced;

public class DataGridViewGroupHeaderFormattingEventArgs : EventArgs
{
    public DataGridViewGroupHeaderFormattingEventArgs(int columnIndex, int level, object? groupValue, int groupItemCount, string headerText)
    {
        ColumnIndex = columnIndex;
        Level = level;
        GroupValue = groupValue;
        GroupItemCount = groupItemCount;
        HeaderText = headerText;
    }

    public int ColumnIndex { get; }
    public int Level { get; }
    public object? GroupValue { get; }
    public int GroupItemCount { get; }
    public string HeaderText { get; set; }
    public Font? Font { get; set; }
    public Color? ForeColor { get; set; }
    public Color? BackColor { get; set; }
    public DataGridViewContentAlignment? TextAlign { get; set; }
}
