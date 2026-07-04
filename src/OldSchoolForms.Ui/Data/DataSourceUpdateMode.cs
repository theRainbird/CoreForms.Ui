namespace OldSchoolForms.Ui.Data;

/// <summary>
/// Specifies when data source updates occur for a bound control property.
/// </summary>
public enum DataSourceUpdateMode
{
    /// <summary>Update the data source whenever the property changes.</summary>
    OnPropertyChanged = 0,
    /// <summary>Update the data source on validation.</summary>
    OnValidation = 1,
    /// <summary>Never update the data source.</summary>
    Never = 2
}
