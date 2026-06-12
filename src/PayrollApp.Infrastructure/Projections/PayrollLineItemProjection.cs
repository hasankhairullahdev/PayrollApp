using Marten.Events.Projections;
using PayrollApp.Domain.Events;
using PayrollApp.Infrastructure.ReadModels;

namespace PayrollApp.Infrastructure.Projections;

/// <summary>
/// Projection untuk PayrollLineItem read model.
/// Diupdate saat PayrollCalculated event.
/// </summary>
public partial class PayrollLineItemProjection : EventProjection
{
    // Create line items dari PayrollCalculated event
    public IEnumerable<ReadModels.PayrollLineItem> Transform(PayrollCalculated @event)
    {
        var lineItems = new List<ReadModels.PayrollLineItem>();

        foreach (var item in @event.LineItems)
        {
            var lineItem = new ReadModels.PayrollLineItem
            {
                Id = Guid.NewGuid(),
                PayrollRunId = @event.PayrollRunId,
                EmployeeId = Guid.Parse(item.EmployeeId),
                EmployeeCode = item.EmployeeId, // Use EmployeeId as code for now
                EmployeeName = item.EmployeeName,
                
                // Salary components
                BasicSalary = item.BasicSalary,
                Allowances = item.TotalAllowances,
                Overtime = item.TotalOvertime,
                GrossSalary = item.GrossSalary,
                
                // Deductions
                Deductions = item.TotalDeductions,
                
                // BPJS
                BpjsKesehatan = item.BPJS.KesehatanEmployee.Amount,
                BpjsKetenagakerjaan = item.BPJS.JhtEmployee.Amount + item.BPJS.JpEmployee.Amount,
                TotalBpjs = item.BPJS.TotalEmployeeContribution.Amount,
                
                // Tax
                Pph21 = item.Pph21,
                
                // Net
                TakeHomePay = item.TakeHomePay,
                
                // Metadata
                IsProrated = item.IsProrated,
                ProratePercentage = item.ProratePercentage,
                CalculatedAt = @event.CalculatedAt
            };

            lineItems.Add(lineItem);
        }

        return lineItems;
    }
}

// Made with Bob