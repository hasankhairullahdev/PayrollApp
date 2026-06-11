using Marten.Events.Aggregation;
using PayrollApp.Domain.Events;
using PayrollApp.Infrastructure.ReadModels;

namespace PayrollApp.Infrastructure.Projections;

/// <summary>
/// Single stream projection untuk PayrollRunSummary read model.
/// Diupdate setiap kali ada event baru di PayrollRun aggregate stream.
/// MUST be partial for Marten 9.x source generator
/// </summary>
public partial class PayrollRunSummaryProjection : SingleStreamProjection<PayrollRunSummary, Guid>
{
    /// <summary>
    /// Create initial read model dari PayrollRunCreated event
    /// </summary>
    public PayrollRunSummary Create(PayrollRunCreated @event)
    {
        return new PayrollRunSummary
        {
            Id = @event.PayrollRunId,
            Month = @event.Month,
            Year = @event.Year,
            Status = Domain.Enums.PayrollStatus.Draft,
            TotalEmployees = 0,
            TotalAmount = 0m,
            CreatedBy = @event.CreatedBy,
            CreatedAt = @event.CreatedAt,
            ApprovedBy = null,
            ApprovedAt = null,
            LockedBy = null,
            LockedAt = null
        };
    }
    
    /// <summary>
    /// Update read model setelah calculation selesai
    /// </summary>
    public void Apply(PayrollCalculated @event, PayrollRunSummary summary)
    {
        summary.Status = @event.Status;
        summary.TotalEmployees = @event.LineItems.Count;
        summary.TotalAmount = @event.TotalAmount;
    }
    
    /// <summary>
    /// Update status saat review dimulai
    /// </summary>
    public void Apply(PayrollReviewStarted @event, PayrollRunSummary summary)
    {
        summary.Status = Domain.Enums.PayrollStatus.UnderReview;
    }
    
    /// <summary>
    /// Update read model setelah approved
    /// </summary>
    public void Apply(PayrollApproved @event, PayrollRunSummary summary)
    {
        summary.Status = Domain.Enums.PayrollStatus.Approved;
        summary.ApprovedBy = @event.ApprovedBy;
        summary.ApprovedAt = @event.ApprovedAt;
    }
    
    /// <summary>
    /// Update status saat rejected (kembali ke Draft)
    /// </summary>
    public void Apply(PayrollRejected @event, PayrollRunSummary summary)
    {
        summary.Status = Domain.Enums.PayrollStatus.Draft;
        // Reset approval info karena kembali ke Draft
        summary.ApprovedBy = null;
        summary.ApprovedAt = null;
    }
    
    /// <summary>
    /// Update read model setelah locked
    /// </summary>
    public void Apply(PayrollLocked @event, PayrollRunSummary summary)
    {
        summary.Status = Domain.Enums.PayrollStatus.Locked;
        summary.LockedBy = @event.LockedBy;
        summary.LockedAt = @event.LockedAt;
    }
    
    /// <summary>
    /// Update status setelah disbursement initiated
    /// </summary>
    public void Apply(DisbursementInitiated @event, PayrollRunSummary summary)
    {
        summary.Status = Domain.Enums.PayrollStatus.Disbursed;
    }
    
    /// <summary>
    /// Update status setelah disbursement confirmed
    /// </summary>
    public void Apply(DisbursementConfirmed @event, PayrollRunSummary summary)
    {
        summary.Status = Domain.Enums.PayrollStatus.Disbursed;
    }
}

// Made with Bob
