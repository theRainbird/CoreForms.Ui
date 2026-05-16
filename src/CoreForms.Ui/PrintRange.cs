namespace CoreForms.Ui;

/// <summary>
/// Specifies the range of pages to print.
/// </summary>
public enum PrintRange
{
    /// <summary>
    /// All pages are printed.
    /// </summary>
    AllPages = 0,

    /// <summary>
    /// Only the selected pages are printed.
    /// </summary>
    Selection = 1,

    /// <summary>
    /// Only the current page is printed.
    /// </summary>
    CurrentPage = 2,

    /// <summary>
    /// A specific range of pages is printed.
    /// </summary>
    SomePages = 3
}
