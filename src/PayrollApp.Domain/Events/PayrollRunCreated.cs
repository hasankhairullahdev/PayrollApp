namespace PayrollApp.Domain.Events;

public record PayrollRunCreated
{
    public Guid PayrollRunId { get; init; }
    public int Month { get; init; }
    public int Year { get; init; }
    public string CreatedBy { get; init; }
    public DateTime CreatedAt { get; init; }

    public PayrollRunCreated(
        Guid payrollRunId,
        int month,
        int year,
        string createdBy,
        DateTime createdAt)
    {
        PayrollRunId = payrollRunId;
        Month = month;
        Year = year;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
    }
}

// Made with Bob
