using System.ComponentModel;

namespace CoreForms.Ui.Demo.Models;

/// <summary>
/// Represents a person entity used throughout the demo for data-binding and display purposes.
/// Implements <see cref="INotifyPropertyChanged"/> to support two-way data binding.
/// </summary>
public class Person : INotifyPropertyChanged
{
    private int _id;
    private string _name;
    private string _email;
    private string _status;
    private bool _isActive;
    private DateTime _birthDate;
    private DateTime _startTime;

    /// <summary>
    /// Initializes a new instance of the <see cref="Person"/> class.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="name">The person's name.</param>
    /// <param name="email">The person's email address.</param>
    /// <param name="status">The person's status (e.g. Active, Inactive, Pending).</param>
    public Person(int id, string name, string email, string status)
    {
        _id = id;
        _name = name;
        _email = email;
        _status = status;
        _isActive = status == "Active";
        _birthDate = new DateTime(1990, 1, 1);
        _startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 9, 0, 0);
    }

    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public int Id
    {
        get => _id;
        set
        {
            if (_id != value) { _id = value; OnPropertyChanged(nameof(Id)); }
        }
    }

    /// <summary>
    /// Gets or sets the person's name.
    /// </summary>
    public string Name
    {
        get => _name;
        set
        {
            if (_name != value) { _name = value; OnPropertyChanged(nameof(Name)); }
        }
    }

    /// <summary>
    /// Gets or sets the person's email address.
    /// </summary>
    public string Email
    {
        get => _email;
        set
        {
            if (_email != value) { _email = value; OnPropertyChanged(nameof(Email)); }
        }
    }

    /// <summary>
    /// Gets or sets the person's status.
    /// </summary>
    public string Status
    {
        get => _status;
        set
        {
            if (_status != value) { _status = value; OnPropertyChanged(nameof(Status)); }
        }
    }

    /// <summary>
    /// Gets or sets the department.
    /// </summary>
    public string Department { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the salary.
    /// </summary>
    public decimal Salary { get; set; }

    /// <summary>
    /// Gets or sets whether the person is active.
    /// </summary>
    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive != value) { _isActive = value; OnPropertyChanged(nameof(IsActive)); }
        }
    }

    /// <summary>
    /// Gets or sets the birth date.
    /// </summary>
    public DateTime BirthDate
    {
        get => _birthDate;
        set
        {
            if (_birthDate != value) { _birthDate = value; OnPropertyChanged(nameof(BirthDate)); }
        }
    }

    /// <summary>
    /// Gets or sets the start time.
    /// </summary>
    public DateTime StartTime
    {
        get => _startTime;
        set
        {
            if (_startTime != value) { _startTime = value; OnPropertyChanged(nameof(StartTime)); }
        }
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
