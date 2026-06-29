using System.Collections;
using System.Reflection;
using CoreForms.Ui.Core;

namespace CoreForms.Ui.Controls.Advanced;

/// <summary>
/// Represents a single node in the row or column hierarchy tree.
/// </summary>
internal class PivotTreeNode
{
    public string Value { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int Level { get; set; }
    public PivotTreeNode? Parent { get; set; }
    public List<PivotTreeNode> Children { get; } = new();
    public List<int> RecordIndices { get; } = new();
    public bool IsExpanded { get; set; } = true;
    public bool IsLeaf => Children.Count == 0;
    public bool HasChildren => Children.Count > 0;
}

/// <summary>
/// A visible row in the flattened matrix, which may be a header group, data row, or total row.
/// </summary>
internal class PivotFlatRow
{
    public string Value { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int Level { get; set; }
    public bool IsGroupHeader { get; set; }
    public bool IsTotal { get; set; }
    public bool IsGrandTotal { get; set; }
    public bool HasChildren { get; set; }
    public bool IsExpanded { get; set; }
    public PivotTreeNode? TreeNode { get; set; }

    /// <summary>
    /// Index into the 2D value array. -1 for group headers (they don't have corresponding data cells directly).
    /// For data rows, this is the row index in the data grid.
    /// For total rows, this maps to the subtotal/grandtotal row.
    /// </summary>
    public int DataIndex { get; set; } = -1;
}

/// <summary>
/// A visible column in the flattened matrix.
/// </summary>
internal class PivotFlatColumn
{
    public string Value { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int Level { get; set; }
    public bool IsTotal { get; set; }
    public bool IsGrandTotal { get; set; }
    public int Span { get; set; } = 1;

    /// <summary>
    /// The index in the data values array.
    /// </summary>
    public int DataIndex { get; set; } = -1;
}

/// <summary>
/// Internal engine that builds the cross-tabulation matrix from flat data,
/// handles drilldown state, and provides aggregated values for rendering.
/// </summary>
internal class PivotTableMatrix
{
    private readonly List<object> _records = new();
    private readonly List<PivotTableField> _rowFields = new();
    private readonly List<PivotTableField> _columnFields = new();
    private readonly List<PivotTableField> _valueFields = new();
    private readonly Dictionary<string, bool> _drillState = new(StringComparer.OrdinalIgnoreCase);

    // Tree structures
    private PivotTreeNode? _rowRoot;
    private PivotTreeNode? _columnRoot;

    // Flattened visible rows/columns
    private List<PivotFlatRow> _flatRows = new();
    private List<PivotFlatColumn> _flatColumns = new();

    // Aggregated values [rowDataIndex, colDataIndex, valFieldIndex]
    private double[,,]? _values;

    // Detail records for drill-through [rowDataIndex, colDataIndex, valFieldIndex]
    private List<object>[,,]? _details;

    // Total rows/columns indices
    private int _grandTotalRowIndex = -1;
    private int _grandTotalColumnIndex = -1;

    // Property cache for fast reflection
    private readonly Dictionary<string, PropertyInfo?> _propertyCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the flat list of visible rows after drilldown filtering.
    /// </summary>
    public IReadOnlyList<PivotFlatRow> FlatRows => _flatRows;

    /// <summary>
    /// Gets the flat list of visible columns after drilldown filtering.
    /// </summary>
    public IReadOnlyList<PivotFlatColumn> FlatColumns => _flatColumns;

    /// <summary>
    /// Gets the number of data rows (excluding group headers).
    /// </summary>
    public int DataRowCount { get; private set; }

    /// <summary>
    /// Gets the number of data columns (excluding hierarchy headers).
    /// </summary>
    public int DataColumnCount { get; private set; }

    /// <summary>
    /// Gets the number of value fields.
    /// </summary>
    public int ValueFieldCount => _valueFields.Count;

    /// <summary>
    /// Gets whether the matrix has been built.
    /// </summary>
    public bool IsBuilt { get; private set; }

    /// <summary>
    /// Gets the grand total row data index.
    /// </summary>
    public int GrandTotalRowIndex => _grandTotalRowIndex;

    /// <summary>
    /// Gets the grand total column data index.
    /// </summary>
    public int GrandTotalColumnIndex => _grandTotalColumnIndex;

    /// <summary>
    /// Gets the maximum row hierarchy depth.
    /// </summary>
    public int MaxRowLevel { get; private set; }

    /// <summary>
    /// Gets the maximum column hierarchy depth.
    /// </summary>
    public int MaxColumnLevel { get; private set; }

    /// <summary>
    /// Returns the aggregated value at the specified position.
    /// </summary>
    public double GetValue(int rowDataIndex, int colDataIndex, int valFieldIndex)
    {
        if (_values == null) return 0;
        if (rowDataIndex < 0 || colDataIndex < 0) return 0;
        if (rowDataIndex >= _values.GetLength(0) || colDataIndex >= _values.GetLength(1) || valFieldIndex >= _values.GetLength(2))
            return 0;
        return _values[rowDataIndex, colDataIndex, valFieldIndex];
    }

    /// <summary>
    /// Returns the detail records at the specified position.
    /// </summary>
    public IReadOnlyList<object> GetDetails(int rowDataIndex, int colDataIndex, int valFieldIndex)
    {
        if (_details == null) return Array.Empty<object>();
        if (rowDataIndex < 0 || colDataIndex < 0) return Array.Empty<object>();
        if (rowDataIndex >= _details.GetLength(0) || colDataIndex >= _details.GetLength(1) || valFieldIndex >= _details.GetLength(2))
            return Array.Empty<object>();
        return _details[rowDataIndex, colDataIndex, valFieldIndex] ?? new List<object>();
    }

    /// <summary>
    /// Rebuilds the entire matrix from the specified records and fields.
    /// </summary>
    public void Build(
        IEnumerable<object> records,
        List<PivotTableField> rowFields,
        List<PivotTableField> columnFields,
        List<PivotTableField> valueFields,
        List<PivotTableField>? filterFields,
        Dictionary<string, object?>? filterValues,
        Dictionary<string, bool>? drillState,
        PivotTableTotalVisibility totals)
    {
        _rowFields.Clear();
        _rowFields.AddRange(rowFields);
        _columnFields.Clear();
        _columnFields.AddRange(columnFields);
        _valueFields.Clear();
        _valueFields.AddRange(valueFields);

        MaxRowLevel = _rowFields.Count;
        MaxColumnLevel = _columnFields.Count;

        // Build record list (apply filters)
        _records.Clear();
        foreach (var record in records)
        {
            if (record == null) continue;
            if (!PassesFilter(record, filterFields, filterValues)) continue;
            _records.Add(record);
        }

        // Apply drill state
        if (drillState != null)
        {
            foreach (var kvp in drillState)
            {
                _drillState[kvp.Key] = kvp.Value;
            }
        }

        // Build hierarchy trees
        _rowRoot = BuildTree(_records, _rowFields, _drillState);
        _columnRoot = BuildTree(_records, _columnFields, null);

        // Flatten trees into visible sequences
        _flatRows = new List<PivotFlatRow>();
        _flatColumns = new List<PivotFlatColumn>();

        FlattenTree(_rowRoot, _flatRows, 0, true, totals);
        FlattenColumnTree(_columnRoot, _flatColumns, 0, totals);

        // Add grand total row
        if ((totals & PivotTableTotalVisibility.RowGrandTotal) != 0 && _rowFields.Count > 0)
        {
            _flatRows.Add(new PivotFlatRow
            {
                Value = "Grand Total",
                Path = "__grandtotal__",
                Level = 0,
                IsTotal = true,
                IsGrandTotal = true
            });
        }

        // Count data rows/columns
        DataRowCount = _flatRows.Count(r => !r.IsGroupHeader || r.IsTotal);
        DataColumnCount = _flatColumns.Count;

        // Allocate value arrays
        int dr = DataRowCount;
        int dc = DataColumnCount;
        int vc = _valueFields.Count;

        if (dr == 0 && dc == 0)
        {
            IsBuilt = true;
            return;
        }

        // Assign data indices
        int dataRowIdx = 0;
        foreach (var row in _flatRows)
        {
            if (!row.IsGroupHeader || row.IsTotal)
            {
                row.DataIndex = dataRowIdx++;
            }
        }

        int dataColIdx = 0;
        foreach (var col in _flatColumns)
        {
            col.DataIndex = dataColIdx++;
        }

        _values = new double[dr, dc, vc];
        _details = new List<object>[dr, dc, vc];

        // Get all row leaf combinations (record index groups for each leaf)
        var rowLeaves = GetLeafGroups(_flatRows);
        var colLeaves = GetColumnLeafGroups(_flatColumns);

        // Aggregate
        for (int ri = 0; ri < rowLeaves.Count; ri++)
        {
            var rowGroup = rowLeaves[ri];
            for (int ci = 0; ci < colLeaves.Count; ci++)
            {
                var colGroup = colLeaves[ci];

                // Intersect record indices
                var intersection = rowGroup.Intersect(colGroup).ToList();
                if (intersection.Count == 0) continue;

                for (int vi = 0; vi < vc; vi++)
                {
                    var field = _valueFields[vi];
                    var values = intersection
                        .Select(idx => GetDoubleValue(_records[idx], field.DataPropertyName))
                        .Where(v => !double.IsNaN(v))
                        .ToList();

                    if (values.Count > 0)
                    {
                        _values[ri, ci, vi] = Aggregate(values, field.Aggregation);
                        _details[ri, ci, vi] = intersection.Select(idx => _records[idx]).ToList();
                    }
                }
            }
        }

        // Apply percentage transforms
        ApplyPercentageTransforms();

        IsBuilt = true;
    }

    /// <summary>
    /// Gets the set of record indices that belong to each data row.
    /// </summary>
    private List<HashSet<int>> GetLeafGroups(List<PivotFlatRow> flatRows)
    {
        var result = new List<HashSet<int>>();
        foreach (var row in flatRows)
        {
            if (row.IsGroupHeader && !row.IsTotal) continue;

            if (row.IsTotal && !row.IsGrandTotal && row.TreeNode != null)
            {
                // Subtotal: collect all leaves under this node
                var set = new HashSet<int>();
                CollectAllLeaves(row.TreeNode, set);
                result.Add(set);
            }
            else if (row.IsGrandTotal)
            {
                // Grand total: all records
                var set = new HashSet<int>();
                for (int i = 0; i < _records.Count; i++)
                    set.Add(i);
                result.Add(set);
            }
            else if (row.TreeNode != null)
            {
                // Normal data row: if leaf, use its records; otherwise collect all descendants
                var set = new HashSet<int>();
                CollectAllLeaves(row.TreeNode, set);
                result.Add(set);
            }
            else
            {
                result.Add(new HashSet<int>());
            }
        }
        return result;
    }

    /// <summary>
    /// Gets the set of record indices that belong to each data column.
    /// </summary>
    private List<HashSet<int>> GetColumnLeafGroups(List<PivotFlatColumn> flatColumns)
    {
        var result = new List<HashSet<int>>();
        foreach (var col in flatColumns)
        {
            if (col.IsGrandTotal)
            {
                var set = new HashSet<int>();
                for (int i = 0; i < _records.Count; i++)
                    set.Add(i);
                result.Add(set);
            }
            else
            {
                // Find the column tree node by path
                var node = FindColumnNode(_columnRoot, col.Path);
                if (node != null)
                {
                    var set = new HashSet<int>();
                    CollectAllLeaves(node, set);
                    result.Add(set);
                }
                else
                {
                    result.Add(new HashSet<int>());
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Finds a column node by its full path.
    /// </summary>
    private static PivotTreeNode? FindColumnNode(PivotTreeNode? root, string path)
    {
        if (root == null) return null;
        if (root.Path == path) return root;

        foreach (var child in root.Children)
        {
            var found = FindColumnNode(child, path);
            if (found != null) return found;
        }
        return null;
    }

    private static void CollectAllLeaves(PivotTreeNode node, HashSet<int> result)
    {
        if (node.IsLeaf)
        {
            foreach (var idx in node.RecordIndices)
                result.Add(idx);
        }
        else
        {
            foreach (var child in node.Children)
                CollectAllLeaves(child, result);
        }
    }

    /// <summary>
    /// Checks whether a record passes the active filter values.
    /// </summary>
    private bool PassesFilter(
        object record,
        List<PivotTableField>? filterFields,
        Dictionary<string, object?>? filterValues)
    {
        if (filterFields == null || filterValues == null || filterFields.Count == 0)
            return true;

        foreach (var field in filterFields)
        {
            if (filterValues.TryGetValue(field.DataPropertyName, out var filterVal))
            {
                if (filterVal == null) continue;
                var recordVal = GetPropertyValue(record, field.DataPropertyName);
                if (!Equals(recordVal, filterVal))
                    return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Builds a hierarchy tree from records grouped by the specified fields.
    /// </summary>
    private PivotTreeNode BuildTree(
        List<object> records,
        List<PivotTableField> fields,
        Dictionary<string, bool>? drillState)
    {
        var root = new PivotTreeNode
        {
            Value = "__root__",
            Path = "__root__",
            Level = -1,
            IsExpanded = true
        };

        if (fields.Count == 0)
        {
            // No row/column fields: create a single leaf node with all records
            var leaf = new PivotTreeNode
            {
                Value = string.Empty,
                Path = string.Empty,
                Level = 0,
                Parent = root,
                IsExpanded = true
            };
            for (int i = 0; i < records.Count; i++)
                leaf.RecordIndices.Add(i);
            root.Children.Add(leaf);
            return root;
        }

        for (int i = 0; i < records.Count; i++)
        {
            var record = records[i];
            var current = root;

            for (int level = 0; level < fields.Count; level++)
            {
                var field = fields[level];
                var val = GetPropertyValue(record, field.DataPropertyName)?.ToString() ?? "(blank)";
                var path = current.Path + "|" + val;

                var existing = current.Children.Find(n => n.Value == val);
                if (existing == null)
                {
                    bool expanded = drillState == null || !drillState.ContainsKey(path) || drillState[path];
                    existing = new PivotTreeNode
                    {
                        Value = val,
                        Path = path,
                        Level = level,
                        Parent = current,
                        IsExpanded = level < fields.Count - 1 && expanded
                    };
                    current.Children.Add(existing);
                }

                if (level == fields.Count - 1)
                {
                    existing.RecordIndices.Add(i);
                }

                current = existing;
            }
        }

        // Sort children alphabetically at each level
        SortTree(root);

        return root;
    }

    private static void SortTree(PivotTreeNode node)
    {
        node.Children.Sort((a, b) => string.Compare(a.Value, b.Value, StringComparison.OrdinalIgnoreCase));
        foreach (var child in node.Children)
            SortTree(child);
    }

    /// <summary>
    /// Flattens the row tree into a visible list, respecting drilldown state.
    /// </summary>
    private void FlattenTree(
        PivotTreeNode node,
        List<PivotFlatRow> result,
        int depth,
        bool showChildren,
        PivotTableTotalVisibility totals)
    {
        foreach (var child in node.Children)
        {
            bool nodeExpanded = child.IsExpanded;
            bool hasChildren = child.HasChildren;
            bool isHeader = hasChildren;

            // Add group header
            if (depth >= 0 || child.Level >= 0)
            {
                result.Add(new PivotFlatRow
                {
                    Value = child.Value,
                    Path = child.Path,
                    Level = child.Level,
                    IsGroupHeader = isHeader,
                    HasChildren = hasChildren,
                    IsExpanded = nodeExpanded,
                    TreeNode = child
                });
            }

            // Add subtotal row if node has children and subtotals are enabled
            if (hasChildren && (totals & PivotTableTotalVisibility.Subtotals) != 0)
            {
                result.Add(new PivotFlatRow
                {
                    Value = child.Value,
                    Path = child.Path,
                    Level = child.Level,
                    IsGroupHeader = false,
                    IsTotal = true,
                    HasChildren = false,
                    IsExpanded = false,
                    TreeNode = child
                });
            }

            // If expanded, recurse into children
            if (nodeExpanded && hasChildren)
            {
                FlattenTree(child, result, depth + 1, showChildren, totals);
            }
        }
    }

    /// <summary>
    /// Flattens the column tree into a visible list. Column trees are always fully expanded.
    /// </summary>
    private void FlattenColumnTree(
        PivotTreeNode node,
        List<PivotFlatColumn> result,
        int depth,
        PivotTableTotalVisibility totals)
    {
        foreach (var child in node.Children)
        {
            bool hasChildren = child.HasChildren;

            result.Add(new PivotFlatColumn
            {
                Value = child.Value,
                Path = child.Path,
                Level = child.Level,
                Span = 1
            });

            if (hasChildren)
            {
                // Mark parent with correct span (calculated after recursion)
                FlattenColumnTree(child, result, depth + 1, totals);
            }
        }

        // Calculate span for parent nodes (from bottom up)
        if (node.Children.Count > 0 && node.Level >= 0)
        {
            foreach (var child in node.Children)
            {
                foreach (var flat in result)
                {
                    if (flat.Path == child.Path && flat.Level == child.Level)
                    {
                        flat.Span = CountColumnLeaves(child);
                        break;
                    }
                }
            }
        }

        // Add grand total column
        if (_columnFields.Count > 0 && (totals & PivotTableTotalVisibility.ColumnGrandTotal) != 0)
        {
            // Only add once at root level
            if (node.Level < 0)
            {
                result.Add(new PivotFlatColumn
                {
                    Value = "Grand Total",
                    Path = "__grandtotal__",
                    Level = 0,
                    Span = 1,
                    IsTotal = true,
                    IsGrandTotal = true
                });
            }
        }
    }

    private static int CountColumnLeaves(PivotTreeNode node)
    {
        if (node.IsLeaf) return 1;
        int count = 0;
        foreach (var child in node.Children)
            count += CountColumnLeaves(child);
        return count;
    }

    /// <summary>
    /// Applies percentage transforms to values if configured on value fields.
    /// </summary>
    private void ApplyPercentageTransforms()
    {
        if (_values == null) return;

        for (int vi = 0; vi < _valueFields.Count; vi++)
        {
            var field = _valueFields[vi];

            if (field.ShowAsPercentageOfGrandTotal)
            {
                double grandTotal = 0;
                for (int r = 0; r < DataRowCount; r++)
                    for (int c = 0; c < DataColumnCount; c++)
                        grandTotal += _values[r, c, vi];

                if (grandTotal != 0)
                {
                    for (int r = 0; r < DataRowCount; r++)
                        for (int c = 0; c < DataColumnCount; c++)
                            _values[r, c, vi] = _values[r, c, vi] / grandTotal * 100;
                }
            }
            else if (field.ShowAsPercentageOfRow)
            {
                for (int r = 0; r < DataRowCount; r++)
                {
                    double rowTotal = 0;
                    for (int c = 0; c < DataColumnCount; c++)
                        rowTotal += _values[r, c, vi];

                    if (rowTotal != 0)
                    {
                        for (int c = 0; c < DataColumnCount; c++)
                            _values[r, c, vi] = _values[r, c, vi] / rowTotal * 100;
                    }
                }
            }
            else if (field.ShowAsPercentageOfColumn)
            {
                for (int c = 0; c < DataColumnCount; c++)
                {
                    double colTotal = 0;
                    for (int r = 0; r < DataRowCount; r++)
                        colTotal += _values[r, c, vi];

                    if (colTotal != 0)
                    {
                        for (int r = 0; r < DataRowCount; r++)
                            _values[r, c, vi] = _values[r, c, vi] / colTotal * 100;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Toggles the expanded state of a drilldown node and returns the new state.
    /// </summary>
    public bool ToggleDrilldown(string path)
    {
        bool current = _drillState.GetValueOrDefault(path, true);
        bool newState = !current;
        _drillState[path] = newState;
        return newState;
    }

    /// <summary>
    /// Clears the drill state (all nodes expanded).
    /// </summary>
    public void ClearDrillState()
    {
        _drillState.Clear();
    }

    /// <summary>
    /// Sets the drill state so that all non-leaf paths are collapsed.
    /// Only the top-level group headers remain visible.
    /// </summary>
    public void CollapseAllDrillState()
    {
        // Collect all non-leaf paths from the current tree
        var paths = new List<string>();
        CollectNonLeafPaths(_rowRoot, paths);
        
        // Collapse all collected paths
        foreach (var p in paths)
            _drillState[p] = false;
    }

    private static void CollectNonLeafPaths(PivotTreeNode? node, List<string> paths)
    {
        if (node == null) return;
        foreach (var child in node.Children)
        {
            if (child.HasChildren)
            {
                paths.Add(child.Path);
                CollectNonLeafPaths(child, paths);
            }
        }
    }

    /// <summary>
    /// Gets the drilldown state dictionary for serialization/restoration.
    /// </summary>
    public Dictionary<string, bool> GetDrillState()
    {
        var result = new Dictionary<string, bool>(_drillState, StringComparer.OrdinalIgnoreCase);
        // Ensure all current node states are captured
        CaptureDrillState(_rowRoot, result);
        return result;
    }

    private static void CaptureDrillState(PivotTreeNode? node, Dictionary<string, bool> state)
    {
        if (node == null || node.Level < 0) return;
        if (!node.IsExpanded)
            state[node.Path] = false;
        foreach (var child in node.Children)
            CaptureDrillState(child, state);
    }

    /// <summary>
    /// Gets the property value from a record object by property name.
    /// </summary>
    private object? GetPropertyValue(object record, string propertyName)
    {
        if (!_propertyCache.TryGetValue(propertyName, out var prop))
        {
            prop = record.GetType().GetProperty(propertyName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            _propertyCache[propertyName] = prop;
        }

        if (prop != null)
            return prop.GetValue(record);

        // Try field
        var field = record.GetType().GetField(propertyName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        return field?.GetValue(record);
    }

    /// <summary>
    /// Gets a numeric value from a record for aggregation.
    /// </summary>
    private double GetDoubleValue(object record, string propertyName)
    {
        var val = GetPropertyValue(record, propertyName);
        return ConvertToDouble(val);
    }

    /// <summary>
    /// Converts an object to a double value for aggregation.
    /// </summary>
    private static double ConvertToDouble(object? value)
    {
        if (value == null) return double.NaN;
        if (value is double d) return d;
        if (value is int iv) return iv;
        if (value is float fv) return fv;
        if (value is long lv) return lv;
        if (value is decimal dec) return (double)dec;
        if (value is short sv) return sv;
        if (value is byte bv) return bv;
        if (value is uint uiv) return uiv;
        if (value is ulong ulv) return ulv;
        if (value is ushort usv) return usv;
        if (value is sbyte sbv) return sbv;
        return double.NaN;
    }

    /// <summary>
    /// Aggregates a list of values using the specified function.
    /// </summary>
    private static double Aggregate(List<double> values, PivotTableAggregation agg)
    {
        if (values.Count == 0) return 0;
        return agg switch
        {
            PivotTableAggregation.Sum => values.Sum(),
            PivotTableAggregation.Count => values.Count,
            PivotTableAggregation.Average => values.Average(),
            PivotTableAggregation.Min => values.Min(),
            PivotTableAggregation.Max => values.Max(),
            PivotTableAggregation.None => values.Count > 0 ? values[0] : 0,
            _ => values.Sum()
        };
    }
}
