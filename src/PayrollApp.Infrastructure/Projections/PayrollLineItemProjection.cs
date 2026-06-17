using Marten;
using Marten.Events.Projections;
using PayrollApp.Domain.Events;
using PayrollApp.Infrastructure.ReadModels;

namespace PayrollApp.Infrastructure.Projections;

/// <summary>
/// Event projection untuk PayrollLineItem read model.
/// Diupdate saat PayrollCalculated event.
/// MUST be partial for Marten 9.x source generator
/// </summary>
public partial class PayrollLineItemProjection : EventProjection
{
    // Create line items dari PayrollCalculated event
    public void Project(PayrollCalculated @event, IDocumentOperations ops)
    {
        foreach (var item in @event.LineItems)
        {
            var lineItem = new ReadModels.PayrollLineItem
            {
                Id = Guid.NewGuid(),
                PayrollRunId = @event.PayrollRunId,
                EmployeeId = item.EmployeeId,
                EmployeeCode = item.EmployeeCode,
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

            ops.Store(lineItem);
        }
    }
}

// Made with Bob