using PayrollApp.Domain.Enums;
using PayrollApp.Domain.ValueObjects;

namespace PayrollApp.Domain.Events;

public record PayrollCalculated
{
    public Guid PayrollRunId { get; init; }
    public List<PayrollLineItem> LineItems { get; init; }
    public decimal TotalAmount { get; init; }
    public PayrollStatus Status { get; init; }
    public DateTime CalculatedAt { get; init; }

    public PayrollCalculated(
        Guid payrollRunId,
        List<PayrollLineItem> lineItems,
        decimal totalAmount,
        DateTime calculatedAt)
    {
        PayrollRunId = payrollRunId;
        LineItems = lineItems;
        TotalAmount = totalAmount;
        Status = PayrollStatus.Calculated;
        CalculatedAt = calculatedAt;
    }
}

// Made with Bob
