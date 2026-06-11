using Hangfire;
using Marten;
using Microsoft.Extensions.Logging;
using PayrollApp.Domain.Aggregates;
using PayrollApp.Domain.ValueObjects;
using PayrollApp.Engine;

namespace PayrollApp.Infrastructure.Jobs;

/// <summary>
/// Background job untuk kalkulasi payroll.
/// Job ini idempotent - bisa dijalankan berkali-kali dengan hasil yang sama.
/// </summary>
public class PayrollCalculationJob
{
    private readonly IDocumentStore _documentStore;
    private readonly PPh21Calculator _pph21Calculator;
    private readonly BPJSCalculator _bpjsCalculator;
    private readonly OvertimeCalculator _overtimeCalculator;
    private readonly ProrateCalculator _prorateCalculator;
    private readonly ILogger<PayrollCalculationJob> _logger;
    
    public PayrollCalculationJob(
        IDocumentStore documentStore,
        PPh21Calculator pph21Calculator,
        BPJSCalculator bpjsCalculator,
        OvertimeCalculator overtimeCalculator,
        ProrateCalculator prorateCalculator,
        ILogger<PayrollCalculationJob> logger)
    {
        _documentStore = documentStore;
        _pph21Calculator = pph21Calculator;
        _bpjsCalculator = bpjsCalculator;
        _overtimeCalculator = overtimeCalculator;
        _prorateCalculator = prorateCalculator;
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
            
            // Load PayrollRun aggregate
            var payrollRun = await session.Events.AggregateStreamAsync<PayrollRun>(payrollRunId, token: cancellationToken.ShutdownToken);
            
            if (payrollRun == null)
            {
                _logger.LogError("PayrollRun {PayrollRunId} not found", payrollRunId);
                throw new InvalidOperationException($"PayrollRun {payrollRunId} not found");
            }
            
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
        // 1. Calculate basic salary + allowances
        var basicSalary = employee.BasicSalary;
        var allowances = employee.SalaryComponents
            .Where(x => x.Type == Domain.Enums.SalaryComponentType.Allowance)
            .Sum(x => x.Amount);
        
        // 2. Calculate overtime (if any)
        // TODO: Get actual overtime data from timesheet
        var overtimeAmount = 0m;
        
        // 3. Calculate prorate (if join/resign mid-month)
        var isProrated = false;
        var proratePercentage = 1m;
        int? workingDays = null;
        int? totalWorkingDays = null;
        
        if (employee.JoinDate.HasValue)
        {
            var joinDate = employee.JoinDate.Value;
            if (joinDate.Year == year && joinDate.Month == month && joinDate.Day > 1)
            {
                // Employee joined mid-month
                var startDate = joinDate;
                var endDate = new DateTime(year, month, DateTime.DaysInMonth(year, month));
                var holidays = new List<DateTime>(); // TODO: Get from holiday calendar
                
                var prorateResult = _prorateCalculator.CalculateProrate(basicSalary, startDate, endDate, holidays);
                basicSalary = prorateResult.ProratedAmount;
                isProrated = true;
                proratePercentage = prorateResult.Percentage;
                workingDays = prorateResult.WorkingDays;
                totalWorkingDays = prorateResult.TotalWorkingDays;
            }
        }
        
        // 4. Calculate gross salary
        var grossSalary = basicSalary + allowances + overtimeAmount;
        
        // 5. Calculate BPJS
        var bpjsResult = _bpjsCalculator.Calculate(grossSalary);
        var totalBPJS = bpjsResult.HealthEmployee + bpjsResult.JHTEmployee + bpjsResult.JPEmployee;
        
        // 6. Calculate PPh 21
        var pph21 = _pph21Calculator.Calculate(
            grossSalary,
            employee.PTKPStatus ?? "TK/0",
            employee.HasNPWP
        );
        
        // 7. Calculate deductions
        var deductions = employee.SalaryComponents
            .Where(x => x.Type == Domain.Enums.SalaryComponentType.Deduction)
            .Sum(x => x.Amount);
        
        // 8. Calculate take home pay
        var takeHomePay = grossSalary - totalBPJS - pph21 - deductions;
        
        // 9. Create line item
        return new Domain.ValueObjects.PayrollLineItem(
            Guid.NewGuid(),
            employee.Id.Value,
            employee.Name,
            basicSalary,
            allowances,
            overtimeAmount,
            grossSalary,
            deductions,
            bpjsResult,
            pph21,
            takeHomePay,
            isProrated,
            workingDays,
            totalWorkingDays,
            proratePercentage
        );
    }
    
    /// <summary>
    /// Get active employees untuk periode tertentu.
    /// TODO: Implement proper employee repository/query.
    /// </summary>
    private async Task<List<Employee>> GetActiveEmployeesAsync(IDocumentSession session, int month, int year, CancellationToken cancellationToken)
    {
        // TODO: Query dari Employee aggregate atau read model
        // For now, return mock data
        await Task.CompletedTask;
        
        var mockEmployee = Employee.Create(
            new EmployeeId("EMP-001"),
            "John Doe",
            10_000_000m, // Basic salary 10 juta
            "TK/0",
            true, // Has NPWP
            "BCA",
            "1234567890",
            "John Doe"
        );
        
        // Add allowances
        mockEmployee.AddSalaryComponent(new SalaryComponent(
            "Tunjangan Transport",
            Domain.Enums.SalaryComponentType.Allowance,
            1_000_000m
        ));
        
        mockEmployee.AddSalaryComponent(new SalaryComponent(
            "Tunjangan Makan",
            Domain.Enums.SalaryComponentType.Allowance,
            500_000m
        ));
        
        return new List<Employee> { mockEmployee };
    }
}

// Made with Bob
