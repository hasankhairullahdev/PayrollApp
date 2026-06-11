namespace PayrollApp.Domain.Exceptions;

public class PayrollAlreadyLockedException : Exception
{
    public Guid PayrollRunId { get; }

    public PayrollAlreadyLockedException(Guid payrollRunId) 
        : base($"Payroll run {payrollRunId} is already locked and cannot be modified")
    {
        PayrollRunId = payrollRunId;
    }

    public PayrollAlreadyLockedException(Guid payrollRunId, string message) 
        : base(message)
    {
        PayrollRunId = payrollRunId;
    }
}

// Made with Bob
