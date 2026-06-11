using Hangfire;
using Marten;
using Microsoft.Extensions.Logging;
using PayrollApp.Domain.Aggregates;

namespace PayrollApp.Infrastructure.Jobs;

/// <summary>
/// Background job untuk generate PDF payslip setelah payroll di-lock.
/// Job ini idempotent - bisa dijalankan berkali-kali dengan hasil yang sama.
/// </summary>
public class PayslipGenerationJob
{
    private readonly IDocumentStore _documentStore;
    private readonly ILogger<PayslipGenerationJob> _logger;
    
    public PayslipGenerationJob(
        IDocumentStore documentStore,
        ILogger<PayslipGenerationJob> logger)
    {
        _documentStore = documentStore;
        _logger = logger;
    }
    
    /// <summary>
    /// Execute payslip generation untuk semua karyawan dalam satu PayrollRun.
    /// Triggered setelah PayrollRun di-lock.
    /// </summary>
    [DisableConcurrentExecution(timeoutInSeconds: 600)] // 10 minutes timeout
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 120, 300 })]
    public async Task ExecuteAsync(Guid payrollRunId, IJobCancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting payslip generation for PayrollRun {PayrollRunId}", payrollRunId);
        
        try
        {
            await using var session = _documentStore.LightweightSession();
            
            // Load PayrollRun aggregate
            var payrollRun = await session.Events.AggregateStreamAsync<PayrollRun>(payrollRunId, token: cancellationToken.ShutdownToken);
            
            if (payrollRun == null)
            {
                _logger.LogError("PayrollRun {PayrollRunId} not found", payrollRunId);
                throw new InvalidOperationException($"PayrollRun {payrollRunId} not found");
            }
            
            // Check if payroll is locked (idempotency check)
            if (payrollRun.Status != Domain.Enums.PayrollStatus.Locked)
            {
                _logger.LogWarning("PayrollRun {PayrollRunId} is not in Locked status (current: {Status}). Skipping payslip generation.", 
                    payrollRunId, payrollRun.Status);
                return;
            }
            
            // Get all line items
            var lineItems = payrollRun.LineItems;
            
            _logger.LogInformation("Generating payslips for {EmployeeCount} employees", lineItems.Count);
            
            var generatedCount = 0;
            var failedCount = 0;
            
            foreach (var lineItem in lineItems)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                try
                {
                    // Generate PDF payslip
                    var pdfPath = await GeneratePayslipPdfAsync(payrollRun, lineItem, cancellationToken.ShutdownToken);
                    
                    // Raise PayslipGenerated event
                    payrollRun.GeneratePayslip(lineItem.EmployeeId, pdfPath);
                    
                    generatedCount++;
                    
                    _logger.LogDebug("Generated payslip for employee {EmployeeId}: {PdfPath}", 
                        lineItem.EmployeeId, pdfPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating payslip for employee {EmployeeId}", lineItem.EmployeeId);
                    failedCount++;
                    // Continue with other employees
                }
            }
            
            // Save events
            if (generatedCount > 0)
            {
                session.Events.Append(payrollRunId, payrollRun.GetUncommittedEvents().ToArray());
                await session.SaveChangesAsync(cancellationToken.ShutdownToken);
            }
            
            _logger.LogInformation("Payslip generation completed for PayrollRun {PayrollRunId}. Generated: {GeneratedCount}, Failed: {FailedCount}", 
                payrollRunId, generatedCount, failedCount);
            
            if (failedCount > 0)
            {
                throw new InvalidOperationException($"Failed to generate {failedCount} payslips. Check logs for details.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing payslip generation for PayrollRun {PayrollRunId}", payrollRunId);
            throw; // Let Hangfire handle retry
        }
    }
    
    /// <summary>
    /// Generate PDF payslip untuk satu karyawan.
    /// TODO: Implement actual PDF generation using QuestPDF.
    /// </summary>
    private async Task<string> GeneratePayslipPdfAsync(
        PayrollRun payrollRun, 
        Domain.ValueObjects.PayrollLineItem lineItem, 
        CancellationToken cancellationToken)
    {
        // TODO: Implement QuestPDF generation
        // For now, return mock path
        await Task.Delay(100, cancellationToken); // Simulate PDF generation
        
        var fileName = $"payslip_{payrollRun.Year}{payrollRun.Month:D2}_{lineItem.EmployeeId}.pdf";
        var pdfPath = Path.Combine("payslips", payrollRun.Year.ToString(), payrollRun.Month.ToString("D2"), fileName);
        
        // TODO: Actually generate PDF and save to file system or cloud storage
        // Example:
        // var document = new PayslipDocument(payrollRun, lineItem);
        // document.GeneratePdf(pdfPath);
        
        return pdfPath;
    }
    
    /// <summary>
    /// Generate single payslip untuk satu karyawan (on-demand).
    /// Bisa dipanggil manual kalau ada karyawan yang minta re-generate payslip.
    /// </summary>
    [AutomaticRetry(Attempts = 2)]
    public async Task GenerateSinglePayslipAsync(Guid payrollRunId, string employeeId, IJobCancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating single payslip for employee {EmployeeId} in PayrollRun {PayrollRunId}", 
            employeeId, payrollRunId);
        
        try
        {
            await using var session = _documentStore.LightweightSession();
            
            // Load PayrollRun aggregate
            var payrollRun = await session.Events.AggregateStreamAsync<PayrollRun>(payrollRunId, token: cancellationToken.ShutdownToken);
            
            if (payrollRun == null)
            {
                throw new InvalidOperationException($"PayrollRun {payrollRunId} not found");
            }
            
            // Find line item for employee
            var lineItem = payrollRun.LineItems.FirstOrDefault(x => x.EmployeeId == employeeId);
            if (lineItem == null)
            {
                throw new InvalidOperationException($"Employee {employeeId} not found in PayrollRun {payrollRunId}");
            }
            
            // Generate PDF
            var pdfPath = await GeneratePayslipPdfAsync(payrollRun, lineItem, cancellationToken.ShutdownToken);
            
            // Raise event
            payrollRun.GeneratePayslip(employeeId, pdfPath);
            
            // Save events
            session.Events.Append(payrollRunId, payrollRun.GetUncommittedEvents().ToArray());
            await session.SaveChangesAsync(cancellationToken.ShutdownToken);
            
            _logger.LogInformation("Single payslip generated for employee {EmployeeId}: {PdfPath}", 
                employeeId, pdfPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating single payslip for employee {EmployeeId}", employeeId);
            throw;
        }
    }
}

// Made with Bob
