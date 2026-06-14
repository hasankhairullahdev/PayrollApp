using Hangfire;
using Marten;
using Microsoft.Extensions.Logging;
using PayrollApp.Domain.Aggregates;
using PayrollApp.Domain.ValueObjects;
using PayrollApp.Engine;
using PayrollApp.Infrastructure.Repositories;

namespace PayrollApp.Infrastructure.Jobs;

/// <summary>
/// Background job untuk kalkulasi payroll.
/// Job ini idempotent - bisa dijalankan berkali-kali dengan hasil yang sama.
/// </summary>
public class PayrollCalculationJob
{
    private readonly IDocumentStore _documentStore;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ILogger<PayrollCalculationJob> _logger;
    
    public PayrollCalculationJob(
        IDocumentStore documentStore,
        IEmployeeRepository employeeRepository,
        ILogger<PayrollCalculationJob> logger)
    {
        _documentStore = documentStore;
        _employeeRepository = employeeRepository;
        _logger = logger;
    }
    
    /// <summary>
    /// Execute payroll calculation untuk satu PayrollRun.
    /// Method ini HARUS idempotent - kalau dijalankan 2x hasilnya sama.
    /// </summary>
    [DisableConcurrentExecution(timeoutInSeconds: 300)] // Prevent concurrent execution
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })]
    public async Task ExecuteAsync(Guid payrollRunId, IJobCancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting payroll calculation for PayrollRun {PayrollRunId}", payrollRunId);
        
        try
        {
            await using var session = _documentStore.LightweightSession();
            
            // Load PayrollRun aggregate manually (avoid source generator requirement)
            var events = await session.Events.FetchStreamAsync(payrollRunId, token: cancellationToken.ShutdownToken);
            
            if (events == null || !events.Any())
            {
                _logger.LogError("PayrollRun {PayrollRunId} not found", payrollRunId);
                throw new InvalidOperationException($"PayrollRun {payrollRunId} not found");
            }
            
            // Reconstruct aggregate from events
            var payrollRun = PayrollRun.FromEvents(events.Select(e => e.Data));
            
            // Check if already calculated (idempotency check)
            if (payrollRun.Status != Domain.Enums.PayrollStatus.Calculating)
            {
                _logger.LogWarning("PayrollRun {PayrollRunId} is not in Calculating status (current: {Status}). Skipping calculation.", 
                    payrollRunId, payrollRun.Status);
                return;
            }
            
            // Load all active employees
            // TODO: Implement proper employee repository/query
            // For now, use mock data
            var employees = await GetActiveEmployeesAsync(session, payrollRun.Month, payrollRun.Year, cancellationToken.ShutdownToken);
            
            _logger.LogInformation("Calculating payroll for {EmployeeCount} employees", employees.Count);
            
            var lineItems = new List<Domain.ValueObjects.PayrollLineItem>();
            
            foreach (var employee in employees)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                try
                {
                    var lineItem = CalculateEmployeePayroll(employee, payrollRun.Month, payrollRun.Year);
                    lineItems.Add(lineItem);
                    
                    _logger.LogDebug("Calculated payroll for employee {EmployeeId}: TakeHomePay = {TakeHomePay}", 
                        employee.Id, lineItem.TakeHomePay);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calculating payroll for employee {EmployeeId}", employee.Id);
                    // Continue with other employees
                }
            }
            
            // Mark calculation as completed
            var totalAmount = lineItems.Sum(x => x.TakeHomePay);
            payrollRun.MarkCalculated(lineItems, totalAmount);
            
            // Save events
            session.Events.Append(payrollRunId, payrollRun.GetUncommittedEvents().ToArray());
            
            // Save line items as documents (for querying)
            var readModelLineItems = lineItems.Select(item => new ReadModels.PayrollLineItem
            {
                Id = Guid.NewGuid(),
                PayrollRunId = payrollRunId,
                EmployeeId = Guid.Parse(item.EmployeeId),
                EmployeeCode = item.EmployeeId,
                EmployeeName = item.EmployeeName,
                BasicSalary = item.BasicSalary,
                Allowances = item.TotalAllowances,
                Overtime = item.TotalOvertime,
                GrossSalary = item.GrossSalary,
                Deductions = item.TotalDeductions,
                BpjsKesehatan = item.BPJS.KesehatanEmployee.Amount,
                BpjsKetenagakerjaan = item.BPJS.JhtEmployee.Amount + item.BPJS.JpEmployee.Amount,
                TotalBpjs = item.BPJS.TotalEmployeeContribution.Amount,
                Pph21 = item.Pph21,
                TakeHomePay = item.TakeHomePay,
                IsProrated = item.IsProrated,
                ProratePercentage = item.ProratePercentage,
                CalculatedAt = DateTime.UtcNow
            }).ToList();
            
            session.Store(readModelLineItems.ToArray());
            
            await session.SaveChangesAsync(cancellationToken.ShutdownToken);
            
            _logger.LogInformation("Payroll calculation completed for PayrollRun {PayrollRunId}. Total: {TotalAmount:N0}, Employees: {EmployeeCount}", 
                payrollRunId, totalAmount, lineItems.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing payroll calculation for PayrollRun {PayrollRunId}", payrollRunId);
            throw; // Let Hangfire handle retry
        }
    }
    
    /// <summary>
    /// Calculate payroll untuk satu karyawan
    /// </summary>
    private Domain.ValueObjects.PayrollLineItem CalculateEmployeePayroll(Employee employee, int month, int year)
    {
        var period = new DateOnly(year, month, 1);
        
        // 1. Calculate basic salary + allowances
        var basicSalary = employee.GetBasicSalary(period);
        var allowances = employee.GetTotalAllowances(period);
        
        // 2. Calculate overtime (if any)
        // TODO: Get actual overtime data from timesheet
        var overtimeAmount = Money.Zero;
        
        // 3. Calculate prorate (if join/resign mid-month)
        var isProrated = false;
        var proratePercentage = 1m;
        int? workingDays = null;
        int? totalWorkingDays = null;
        
        var joinDate = employee.JoinDate;
        if (joinDate.Year == year && joinDate.Month == month && joinDate.Day > 1)
        {
            // Employee joined mid-month
            var proratedSalary = ProrateCalculator.CalculateForJoin(basicSalary, joinDate, month, year);
            
            var periodStart = new DateOnly(year, month, 1);
            var periodEnd = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
            
            workingDays = ProrateCalculator.CountWorkingDays(joinDate, periodEnd);
            totalWorkingDays = ProrateCalculator.CountWorkingDays(periodStart, periodEnd);
            proratePercentage = ProrateCalculator.GetProratePercentage(joinDate, periodEnd, month, year) / 100m;
            
            basicSalary = proratedSalary;
            isProrated = true;
        }
        
        // 4. Calculate gross salary
        var grossSalary = basicSalary + allowances + overtimeAmount;
        
        // 5. Calculate BPJS
        var bpjsResult = BPJSCalculator.Calculate(grossSalary);
        var totalBPJS = bpjsResult.TotalEmployeeContribution.Amount;
        
        // 6. Calculate PPh 21
        var pph21Result = PPh21Calculator.Calculate(
            grossSalary,
            employee.PtkpStatus,
            employee.HasNpwp
        );
        
        // 7. Calculate deductions
        var deductions = employee.GetTotalDeductions(period);
        
        // 8. Calculate take home pay
        var takeHomePay = grossSalary.Amount - totalBPJS - pph21Result.Pph21Amount.Amount - deductions.Amount;
        
        // 9. Create line item
        return new Domain.ValueObjects.PayrollLineItem(
            Guid.NewGuid(),
            employee.Id.ToString(),
            employee.FullName,
            basicSalary.Amount,
            allowances.Amount,
            overtimeAmount.Amount,
            grossSalary.Amount,
            deductions.Amount,
            bpjsResult,
            pph21Result.Pph21Amount.Amount,
            takeHomePay,
            isProrated,
            workingDays,
            totalWorkingDays,
            proratePercentage
        );
    }
    
    /// <summary>
    /// Get active employees untuk periode tertentu.
    /// Loads from database via EmployeeRepository.
    /// </summary>
    private async Task<List<Employee>> GetActiveEmployeesAsync(IDocumentSession session, int month, int year, CancellationToken cancellationToken)
    {
        // Load employees from database
        var employees = await _employeeRepository.GetActiveEmployeesForPeriodAsync(month, year, cancellationToken);
        
        // If no employees found, return empty list (no more mock data)
        if (!employees.Any())
        {
            _logger.LogWarning("No active employees found for period {Month}/{Year}", month, year);
            return new List<Employee>();
        }
        
        _logger.LogInformation("Found {Count} active employees for period {Month}/{Year}", employees.Count, month, year);
        return employees;
        
        /* REMOVED: Mock data - now using real database employees
        var employees = new List<Employee>();
        
        */ // End of removed mock data
    }
}

// Made with Bob
