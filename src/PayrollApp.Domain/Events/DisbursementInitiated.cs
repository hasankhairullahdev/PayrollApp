using PayrollApp.Domain.ValueObjects;

namespace PayrollApp.Domain.Events;

public record DisbursementInitiated
{
    public Guid PayrollRunId { get; init; }
    public string BankFileUrl { get; init; }
    public Money TotalAmount { get; init; }
    public string BankName { get; init; }
    public DateTime InitiatedAt { get; init; }

    public DisbursementInitiated(
        Guid payrollRunId,
        string bankFileUrl,
        Money totalAmount,
        string bankName,
        DateTime initiatedAt)
    {
        PayrollRunId = payrollRunId;
        BankFileUrl = bankFileUrl;
        TotalAmount = totalAmount;
        BankName = bankName;
        InitiatedAt = initiatedAt;
    }
}

// Made with Bob
