namespace PayrollApp.Domain.Events;

public record PayrollRejected
{
    public Guid PayrollRunId { get; init; }
    public string RejectedBy { get; init; }
    public string Reason { get; init; }
    public DateTime RejectedAt { get; init; }

    public PayrollRejected(
        Guid payrollRunId,
        string rejectedBy,
        string reason,
        DateTime rejectedAt)
    {
        PayrollRunId = payrollRunId;
        RejectedBy = rejectedBy;
        Reason = reason;
        RejectedAt = rejectedAt;
    }
}

// Made with Bob
