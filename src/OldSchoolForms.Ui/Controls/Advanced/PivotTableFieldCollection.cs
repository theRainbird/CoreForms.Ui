using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace OldSchoolForms.Ui.Controls.Advanced;

/// <summary>
/// Represents a collection of <see cref="PivotTableField"/> objects.
/// Raises change events when fields are added, removed, or modified.
/// </summary>
public class PivotTableFieldCollection : ObservableCollection<PivotTableField>
{
    private readonly Action _invalidate;

    /// <summary>
    /// Initializes a new instance of <see cref="PivotTableFieldCollection"/>.
    /// </summary>
    /// <param name="invalidate">Action to call when the collection or any field changes.</param>
    public PivotTableFieldCollection(Action invalidate)
    {
        _invalidate = invalidate;
    }

    /// <summary>
    /// Gets the fields of the specified usage.
    /// </summary>
    /// <param name="usage">The field usage to filter by.</param>
    /// <returns>A list of fields with the given usage, ordered by <see cref="PivotTableField.Order"/>.</returns>
    public List<PivotTableField> GetFields(PivotTableFieldUsage usage)
    {
        return Items.Where(f => f.Usage == usage).OrderBy(f => f.Order).ToList();
    }

    /// <summary>
    /// Gets all row fields ordered by <see cref="PivotTableField.Order"/>.
    /// </summary>
    public List<PivotTableField> RowFields => GetFields(PivotTableFieldUsage.RowField);

    /// <summary>
    /// Gets all column fields ordered by <see cref="PivotTableField.Order"/>.
    /// </summary>
    public List<PivotTableField> ColumnFields => GetFields(PivotTableFieldUsage.ColumnField);

    /// <summary>
    /// Gets all value fields ordered by <see cref="PivotTableField.Order"/>.
    /// </summary>
    public List<PivotTableField> ValueFields => GetFields(PivotTableFieldUsage.ValueField);

    /// <summary>
    /// Gets all filter fields ordered by <see cref="PivotTableField.Order"/>.
    /// </summary>
    public List<PivotTableField> FilterFields => GetFields(PivotTableFieldUsage.FilterField);

    /// <summary>
    /// Raises the <see cref="ObservableCollection{PivotTableField}.CollectionChanged"/> event
    /// and triggers invalidation.
    /// </summary>
    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnCollectionChanged(e);

        if (e.NewItems != null)
        {
            foreach (PivotTableField field in e.NewItems)
            {
                field.PropertyChanged += OnFieldPropertyChanged;
            }
        }

        if (e.OldItems != null)
        {
            foreach (PivotTableField field in e.OldItems)
            {
                field.PropertyChanged -= OnFieldPropertyChanged;
            }
        }

        _invalidate();
    }

    private void OnFieldPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _invalidate();
    }
}
