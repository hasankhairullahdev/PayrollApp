namespace PayrollApp.Domain.Events;

public record PayrollReviewStarted
{
    public Guid PayrollRunId { get; init; }
    public string ReviewedBy { get; init; }
    public DateTime ReviewStartedAt { get; init; }

    public PayrollReviewStarted(
        Guid payrollRunId,
        string reviewedBy,
        DateTime reviewStartedAt)
    {
        PayrollRunId = payrollRunId;
        ReviewedBy = reviewedBy;
        ReviewStartedAt = reviewStartedAt;
    }
}

// Made with Bob
