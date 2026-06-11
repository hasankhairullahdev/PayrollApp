namespace PayrollApp.Domain.Events;

public record DisbursementConfirmed
{
    public Guid PayrollRunId { get; init; }
    public string ConfirmedBy { get; init; }
    public DateTime ConfirmedAt { get; init; }

    public DisbursementConfirmed(
        Guid payrollRunId,
        string confirmedBy,
        DateTime confirmedAt)
    {
        PayrollRunId = payrollRunId;
        ConfirmedBy = confirmedBy;
        ConfirmedAt = confirmedAt;
    }
}

// Made with Bob
