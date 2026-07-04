using OldSchoolForms.Ui.Core;

namespace OldSchoolForms.Ui.Controls.Advanced;

public class DataGridViewGroup
{
    private readonly List<DataGridViewGroup> _childGroups = new();
    private readonly List<int> _rowIndices = new();

    public DataGridViewGroup(int columnIndex, int level, object? key, DataGridViewGroup? parent)
    {
        ColumnIndex = columnIndex;
        Level = level;
        Key = key;
        Parent = parent;
    }

    public int ColumnIndex { get; }
    public int Level { get; }
    public object? Key { get; }
    public DataGridViewGroup? Parent { get; }
    public IReadOnlyList<DataGridViewGroup> ChildGroups => _childGroups;
    public IReadOnlyList<int> RowIndices => _rowIndices;
    public bool IsCollapsed { get; set; }
    public string? CustomHeaderText { get; set; }
    public Font? CustomFont { get; set; }
    public Color? CustomForeColor { get; set; }
    public Color? CustomBackColor { get; set; }
    public DataGridViewContentAlignment? CustomTextAlign { get; set; }

    public int TotalRowCount
    {
        get
        {
            if (_rowIndices.Count > 0)
                return _rowIndices.Count;
            int count = 0;
            foreach (var child in _childGroups)
                count += child.TotalRowCount;
            return count;
        }
    }

    internal void AddChild(DataGridViewGroup group) => _childGroups.Add(group);
    internal void AddRowIndex(int rowIndex) => _rowIndices.Add(rowIndex);
    internal void SortRowIndices(Comparison<int> comparison) => _rowIndices.Sort(comparison);
}
