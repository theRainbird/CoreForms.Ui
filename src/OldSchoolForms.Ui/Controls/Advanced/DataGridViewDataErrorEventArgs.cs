using System;

namespace OldSchoolForms.Ui.Controls.Advanced;

/// <summary>
/// Provides data for the <see cref="DataGridView.DataError"/> event.
/// </summary>
public class DataGridViewDataErrorEventArgs : DataGridViewCellEventArgs
{
    /// <summary>
    /// Gets the exception that occurred during data operation.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// Gets or sets whether the exception should be considered handled.
    /// If set to true, no further action is taken. If false (default), a message box is shown.
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>
    /// Initializes a new instance of DataGridViewDataErrorEventArgs.
    /// </summary>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="columnIndex">The column index of the cell.</param>
    /// <param name="rowIndex">The row index of the cell.</param>
    public DataGridViewDataErrorEventArgs(Exception exception, int columnIndex, int rowIndex)
        : base(columnIndex, rowIndex)
    {
        Exception = exception;
    }
}
