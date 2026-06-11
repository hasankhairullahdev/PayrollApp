namespace PayrollApp.Domain.Exceptions;

public class DuplicatePayrollPeriodException : Exception
{
    public int Month { get; }
    public int Year { get; }

    public DuplicatePayrollPeriodException(int month, int year) 
        : base($"A payroll run already exists for period {month:D2}/{year}")
    {
        Month = month;
        Year = year;
    }

    public DuplicatePayrollPeriodException(int month, int year, string message) 
        : base(message)
    {
        Month = month;
        Year = year;
    }
}

// Made with Bob
