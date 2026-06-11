namespace PayrollApp.Domain.Exceptions;

public class InvalidPayrollStateException : Exception
{
    public InvalidPayrollStateException(string message) : base(message)
    {
    }

    public InvalidPayrollStateException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}

// Made with Bob
