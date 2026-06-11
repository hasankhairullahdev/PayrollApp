using PayrollApp.Domain.Enums;
using PayrollApp.Domain.Events;
using PayrollApp.Domain.Exceptions;
using PayrollApp.Domain.ValueObjects;

namespace PayrollApp.Domain.Aggregates;

public partial class PayrollRun
{
    private readonly List<object> _uncommittedEvents = new();

    public Guid Id { get; private set; }
    public int Month { get; private set; }
    public int Year { get; private set; }
    public PayrollStatus Status { get; private set; }
    public Money TotalAmount { get; private set; } = Money.Zero;
    public int TotalEmployees { get; private set; }
    public List<PayrollLineItem> LineItems { get; private set; } = new();
    public string? CreatedBy { get; private set; }
    public string? ApprovedBy { get; private set; }
    public string? LockedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public DateTime? LockedAt { get; private set; }

    // For event sourcing reconstruction
    public int Version { get; private set; }

    // Private constructor for event sourcing
    private PayrollRun() { }

    // Factory method for creating new aggregate
    public static PayrollRun Create(int month, int year, string createdBy)
    {
        if (month < 1 || month > 12)
            throw new ArgumentException("Month must be between 1 and 12", nameof(month));

        if (year < 2020 || year > 2100)
            throw new ArgumentException("Year must be between 2020 and 2100", nameof(year));

        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException("CreatedBy cannot be empty", nameof(createdBy));

        var payrollRun = new PayrollRun();
        var @event = new PayrollRunCreated(
            Guid.NewGuid(),
            month,
            year,
            createdBy,
            DateTime.UtcNow
        );

        payrollRun.RaiseEvent(@event);
        return payrollRun;
    }

    public void StartCalculation()
    {
        if (Status != PayrollStatus.Draft)
            throw new InvalidPayrollStateException(
                $"Cannot start calculation. Payroll must be in Draft status, current status: {Status}");

        Status = PayrollStatus.Calculating;
    }

    public void MarkCalculated(List<PayrollLineItem> lineItems, decimal totalAmount)
    {
        if (Status != PayrollStatus.Calculating)
            throw new InvalidPayrollStateException(
                $"Cannot mark as calculated. Payroll must be in Calculating status, current status: {Status}");

        if (lineItems == null || lineItems.Count == 0)
            throw new ArgumentException("LineItems cannot be empty", nameof(lineItems));

        var @event = new PayrollCalculated(
            Id,
            lineItems,
            totalAmount,
            DateTime.UtcNow
        );

        RaiseEvent(@event);
    }

    public void StartReview(string reviewedBy)
    {
        if (Status != PayrollStatus.Calculated)
            throw new InvalidPayrollStateException(
                $"Cannot start review. Payroll must be in Calculated status, current status: {Status}");

        if (string.IsNullOrWhiteSpace(reviewedBy))
            throw new ArgumentException("ReviewedBy cannot be empty", nameof(reviewedBy));

        var @event = new PayrollReviewStarted(
            Id,
            reviewedBy,
            DateTime.UtcNow
        );

        RaiseEvent(@event);
    }

    public void Approve(string approvedBy, string? notes = null)
    {
        if (Status != PayrollStatus.UnderReview)
            throw new InvalidPayrollStateException(
                $"Cannot approve. Payroll must be in UnderReview status, current status: {Status}");

        if (string.IsNullOrWhiteSpace(approvedBy))
            throw new ArgumentException("ApprovedBy cannot be empty", nameof(approvedBy));

        var @event = new PayrollApproved(
            Id,
            approvedBy,
            notes,
            DateTime.UtcNow
        );

        RaiseEvent(@event);
    }

    public void Reject(string rejectedBy, string reason)
    {
        if (Status != PayrollStatus.UnderReview)
            throw new InvalidPayrollStateException(
                $"Cannot reject. Payroll must be in UnderReview status, current status: {Status}");

        if (string.IsNullOrWhiteSpace(rejectedBy))
            throw new ArgumentException("RejectedBy cannot be empty", nameof(rejectedBy));

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason cannot be empty", nameof(reason));

        var @event = new PayrollRejected(
            Id,
            rejectedBy,
            reason,
            DateTime.UtcNow
        );

        RaiseEvent(@event);
    }

    public void Lock(string lockedBy)
    {
        if (Status != PayrollStatus.Approved)
            throw new InvalidPayrollStateException(
                $"Cannot lock. Payroll must be in Approved status, current status: {Status}");

        if (string.IsNullOrWhiteSpace(lockedBy))
            throw new ArgumentException("LockedBy cannot be empty", nameof(lockedBy));

        var @event = new PayrollLocked(
            Id,
            lockedBy,
            DateTime.UtcNow
        );

        RaiseEvent(@event);
    }

    public void GeneratePayslip(string employeeId, string pdfPath)
    {
        if (Status != PayrollStatus.Locked)
            throw new InvalidPayrollStateException(
                $"Cannot generate payslip. Payroll must be in Locked status, current status: {Status}");

        if (string.IsNullOrWhiteSpace(employeeId))
            throw new ArgumentException("EmployeeId cannot be empty", nameof(employeeId));

        if (string.IsNullOrWhiteSpace(pdfPath))
            throw new ArgumentException("PdfPath cannot be empty", nameof(pdfPath));

        if (!Guid.TryParse(employeeId, out var employeeGuid))
            throw new ArgumentException("EmployeeId must be a valid GUID", nameof(employeeId));

        var @event = new PayslipGenerated(
            Id,
            new EmployeeId(employeeGuid),
            pdfPath,
            DateTime.UtcNow
        );

        RaiseEvent(@event);
    }

    public void InitiateDisbursement(string bankFileUrl, string bankName)
    {
        if (Status != PayrollStatus.Locked)
            throw new InvalidPayrollStateException(
                $"Cannot initiate disbursement. Payroll must be in Locked status, current status: {Status}");

        if (string.IsNullOrWhiteSpace(bankFileUrl))
            throw new ArgumentException("BankFileUrl cannot be empty", nameof(bankFileUrl));

        if (string.IsNullOrWhiteSpace(bankName))
            throw new ArgumentException("BankName cannot be empty", nameof(bankName));

        var @event = new DisbursementInitiated(
            Id,
            bankFileUrl,
            TotalAmount,
            bankName,
            DateTime.UtcNow
        );

        RaiseEvent(@event);
    }

    public void ConfirmDisbursement(string confirmedBy)
    {
        if (Status != PayrollStatus.Locked)
            throw new InvalidPayrollStateException(
                $"Cannot confirm disbursement. Payroll must be in Locked status, current status: {Status}");

        if (string.IsNullOrWhiteSpace(confirmedBy))
            throw new ArgumentException("ConfirmedBy cannot be empty", nameof(confirmedBy));

        var @event = new DisbursementConfirmed(
            Id,
            confirmedBy,
            DateTime.UtcNow
        );

        RaiseEvent(@event);
    }

    // Event sourcing: raise and apply event
    private void RaiseEvent(object @event)
    {
        Apply(@event);
        _uncommittedEvents.Add(@event);
        Version++;
    }

    // Event sourcing: apply event to update state (private for internal use)
    private void Apply(object @event)
    {
        switch (@event)
        {
            case PayrollRunCreated e:
                Apply(e);
                break;
            case PayrollCalculated e:
                Apply(e);
                break;
            case PayrollReviewStarted e:
                Apply(e);
                break;
            case PayrollApproved e:
                Apply(e);
                break;
            case PayrollRejected e:
                Apply(e);
                break;
            case PayrollLocked e:
                Apply(e);
                break;
            case DisbursementConfirmed e:
                Apply(e);
                break;
            default:
                // Ignore events we don't handle
                break;
        }
    }

    // Public Apply methods for Marten source generator
    public void Apply(PayrollRunCreated e)
    {
        Id = e.PayrollRunId;
        Month = e.Month;
        Year = e.Year;
        Status = PayrollStatus.Draft;
        CreatedBy = e.CreatedBy;
        CreatedAt = e.CreatedAt;
    }

    public void Apply(PayrollCalculated e)
    {
        Status = PayrollStatus.Calculated;
        LineItems = e.LineItems;
        TotalAmount = new Money(e.TotalAmount);
        TotalEmployees = e.LineItems.Count;
    }

    public void Apply(PayrollReviewStarted e)
    {
        Status = PayrollStatus.UnderReview;
    }

    public void Apply(PayrollApproved e)
    {
        Status = PayrollStatus.Approved;
        ApprovedBy = e.ApprovedBy;
        ApprovedAt = e.ApprovedAt;
    }

    public void Apply(PayrollRejected e)
    {
        Status = PayrollStatus.Draft; // Back to draft after rejection
    }

    public void Apply(PayrollLocked e)
    {
        Status = PayrollStatus.Locked;
        LockedBy = e.LockedBy;
        LockedAt = e.LockedAt;
    }

    public void Apply(PayslipGenerated e)
    {
        // No state change needed for payslip generation
    }

    public void Apply(DisbursementInitiated e)
    {
        // No state change needed for disbursement initiation
    }

    public void Apply(DisbursementConfirmed e)
    {
        Status = PayrollStatus.Disbursed;
    }
    // For Marten event sourcing - load events into aggregate
    private void Load(IEnumerable<object> events)
    {
        foreach (var @event in events)
        {
            Apply(@event);
            Version++;
        }
    }

    // Factory method for reconstructing aggregate from events
    public static PayrollRun FromEvents(IEnumerable<object> events)
    {
        var aggregate = new PayrollRun();
        aggregate.Load(events);
        return aggregate;
    }

    public IEnumerable<object> GetUncommittedEvents() => _uncommittedEvents;

    public void ClearUncommittedEvents() => _uncommittedEvents.Clear();
}

// Made with Bob
