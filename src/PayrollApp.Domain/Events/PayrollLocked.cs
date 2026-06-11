namespace PayrollApp.Domain.Events;

public record PayrollLocked
{
    public Guid PayrollRunId { get; init; }
    public string LockedBy { get; init; }
    public DateTime LockedAt { get; init; }

    public PayrollLocked(
        Guid payrollRunId,
        string lockedBy,
        DateTime lockedAt)
    {
        PayrollRunId = payrollRunId;
        LockedBy = lockedBy;
        LockedAt = lockedAt;
    }
}

// Made with Bob
