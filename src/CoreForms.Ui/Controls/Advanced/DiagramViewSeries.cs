using System.Collections;
using System.Collections.Generic;
using CoreForms.Ui.Core;

namespace CoreForms.Ui.Controls.Advanced;

/// <summary>
/// Represents a series of data points in a diagram with a name and color.
/// </summary>
public class DiagramViewSeries
{
    private readonly List<DiagramViewDataPoint> _points = new();

    /// <summary>
    /// Gets or sets the display name of this series (used in the legend).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the primary color for this series.
    /// If null, the palette color at the series index is used.
    /// </summary>
    public Color? Color { get; set; }

    /// <summary>
    /// Gets or sets an optional per-series diagram type override.
    /// If null, the parent DiagramView's ChartType is used.
    /// </summary>
    public DiagramType? DiagramType { get; set; }

    /// <summary>
    /// Gets the list of data points in this series.
    /// </summary>
    public IList<DiagramViewDataPoint> Points => _points;

    /// <summary>
    /// Occurs when the series data or appearance has changed.
    /// </summary>
    public event EventHandler? Changed;

    /// <summary>
    /// Raises the Changed event.
    /// </summary>
    public void NotifyChanged() => Changed?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Adds a data point to the series.
    /// </summary>
    /// <param name="point">The data point to add.</param>
    public void Add(DiagramViewDataPoint point)
    {
        _points.Add(point);
        NotifyChanged();
    }

    /// <summary>
    /// Adds a data point with the specified label and value.
    /// </summary>
    /// <param name="label">The category label.</param>
    /// <param name="value">The numeric value.</param>
    public void Add(string? label, double value)
    {
        _points.Add(new DiagramViewDataPoint(label, value));
        NotifyChanged();
    }

    /// <summary>
    /// Removes all data points from this series.
    /// </summary>
    public void Clear()
    {
        _points.Clear();
        NotifyChanged();
    }
}

/// <summary>
/// Represents a collection of diagram series with change notification.
/// </summary>
public class DiagramViewSeriesCollection : IList<DiagramViewSeries>
{
    private readonly List<DiagramViewSeries> _list = new();

    /// <summary>
    /// Occurs when the collection has changed.
    /// </summary>
    public event EventHandler? CollectionChanged;

    /// <summary>
    /// Gets the number of series in the collection.
    /// </summary>
    public int Count => _list.Count;

    /// <summary>
    /// Gets a value indicating whether the collection is read-only.
    /// </summary>
    public bool IsReadOnly => false;

    /// <summary>
    /// Gets or sets the series at the specified index.
    /// </summary>
    public DiagramViewSeries this[int index]
    {
        get => _list[index];
        set
        {
            if (_list[index] != null)
                _list[index].Changed -= OnSeriesChanged;
            _list[index] = value;
            value.Changed += OnSeriesChanged;
            OnCollectionChanged();
        }
    }

    /// <summary>
    /// Adds a series to the collection.
    /// </summary>
    public void Add(DiagramViewSeries item)
    {
        item.Changed += OnSeriesChanged;
        _list.Add(item);
        OnCollectionChanged();
    }

    /// <summary>
    /// Removes all series from the collection.
    /// </summary>
    public void Clear()
    {
        foreach (var s in _list)
            s.Changed -= OnSeriesChanged;
        _list.Clear();
        OnCollectionChanged();
    }

    /// <summary>
    /// Determines whether the collection contains the specified series.
    /// </summary>
    public bool Contains(DiagramViewSeries item) => _list.Contains(item);

    /// <summary>
    /// Copies the collection to an array.
    /// </summary>
    public void CopyTo(DiagramViewSeries[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

    /// <summary>
    /// Returns an enumerator for the collection.
    /// </summary>
    public IEnumerator<DiagramViewSeries> GetEnumerator() => _list.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();

    /// <summary>
    /// Determines the index of the specified series.
    /// </summary>
    public int IndexOf(DiagramViewSeries item) => _list.IndexOf(item);

    /// <summary>
    /// Inserts a series at the specified index.
    /// </summary>
    public void Insert(int index, DiagramViewSeries item)
    {
        item.Changed += OnSeriesChanged;
        _list.Insert(index, item);
        OnCollectionChanged();
    }

    /// <summary>
    /// Removes the specified series from the collection.
    /// </summary>
    public bool Remove(DiagramViewSeries item)
    {
        item.Changed -= OnSeriesChanged;
        if (_list.Remove(item))
        {
            OnCollectionChanged();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Removes the series at the specified index.
    /// </summary>
    public void RemoveAt(int index)
    {
        _list[index].Changed -= OnSeriesChanged;
        _list.RemoveAt(index);
        OnCollectionChanged();
    }

    private void OnSeriesChanged(object? sender, EventArgs e) => OnCollectionChanged();

    private void OnCollectionChanged() => CollectionChanged?.Invoke(this, EventArgs.Empty);
}
