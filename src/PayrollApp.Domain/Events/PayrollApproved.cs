namespace PayrollApp.Domain.Events;

public record PayrollApproved
{
    public Guid PayrollRunId { get; init; }
    public string ApprovedBy { get; init; }
    public string? Notes { get; init; }
    public DateTime ApprovedAt { get; init; }

    public PayrollApproved(
        Guid payrollRunId,
        string approvedBy,
        string? notes,
        DateTime approvedAt)
    {
        PayrollRunId = payrollRunId;
        ApprovedBy = approvedBy;
        Notes = notes;
        ApprovedAt = approvedAt;
    }
}

// Made with Bob
