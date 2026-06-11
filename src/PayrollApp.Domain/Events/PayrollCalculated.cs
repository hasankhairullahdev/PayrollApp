using PayrollApp.Domain.ValueObjects;

namespace PayrollApp.Domain.Events;

public record PayrollCalculated
{
    public Guid PayrollRunId { get; init; }
    public Money TotalAmount { get; init; }
    public int TotalEmployees { get; init; }
    public DateTime CalculatedAt { get; init; }

    public PayrollCalculated(
        Guid payrollRunId,
        Money totalAmount,
        int totalEmployees,
        DateTime calculatedAt)
    {
        PayrollRunId = payrollRunId;
        TotalAmount = totalAmount;
        TotalEmployees = totalEmployees;
        CalculatedAt = calculatedAt;
    }
}

// Made with Bob
