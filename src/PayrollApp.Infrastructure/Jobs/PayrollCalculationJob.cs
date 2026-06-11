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
    private readonly ILogger<PayrollCalculationJob> _logger;
    
    public PayrollCalculationJob(
        IDocumentStore documentStore,
        ILogger<PayrollCalculationJob> logger)
    {
        _documentStore = documentStore;
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
    /// TODO: Implement proper employee repository/query.
    /// For now, using mock data with various scenarios for testing.
    /// </summary>
    private async Task<List<Employee>> GetActiveEmployeesAsync(IDocumentSession session, int month, int year, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        
        var employees = new List<Employee>();
        
        // Employee 1: Manager - High salary, married with 2 kids, has NPWP
        var emp1 = new Employee(
            Guid.NewGuid(),
            "EMP-001",
            "Budi Santoso",
            "budi.santoso@company.com",
            "123456789012345", // NPWP
            "K/2", // Married with 2 kids
            new DateOnly(2020, 1, 1)
        );
        emp1.AddSalaryComponent(new SalaryComponent(
            Guid.NewGuid(),
            "Basic Salary",
            new Money(15_000_000m),
            Domain.Enums.SalaryComponentType.BasicSalary,
            new DateOnly(2020, 1, 1)
        ));
        emp1.AddSalaryComponent(new SalaryComponent(
            Guid.NewGuid(),
            "Tunjangan Jabatan",
            new Money(3_000_000m),
            Domain.Enums.SalaryComponentType.FixedAllowance,
            new DateOnly(2020, 1, 1)
        ));
        emp1.AddSalaryComponent(new SalaryComponent(
            Guid.NewGuid(),
            "Tunjangan Transport",
            new Money(1_500_000m),
            Domain.Enums.SalaryComponentType.FixedAllowance,
            new DateOnly(2020, 1, 1)
        ));
        employees.Add(emp1);
        
        // Employee 2: Staff - Medium salary, single, has NPWP
        var emp2 = new Employee(
            Guid.NewGuid(),
            "EMP-002",
            "Siti Nurhaliza",
            "siti.nurhaliza@company.com",
            "987654321098765", // NPWP
            "TK/0", // Single
            new DateOnly(2022, 6, 15)
        );
        emp2.AddSalaryComponent(new SalaryComponent(
            Guid.NewGuid(),
            "Basic Salary",
            new Money(8_000_000m),
            Domain.Enums.SalaryComponentType.BasicSalary,
            new DateOnly(2022, 6, 15)
        ));
        emp2.AddSalaryComponent(new SalaryComponent(
            Guid.NewGuid(),
            "Tunjangan Transport",
            new Money(1_000_000m),
            Domain.Enums.SalaryComponentType.FixedAllowance,
            new DateOnly(2022, 6, 15)
        ));
        emp2.AddSalaryComponent(new SalaryComponent(
            Guid.NewGuid(),
            "Tunjangan Makan",
            new Money(500_000m),
            Domain.Enums.SalaryComponentType.FixedAllowance,
            new DateOnly(2022, 6, 15)
        ));
        employees.Add(emp2);
        
        // Employee 3: Junior - Low salary, married, NO NPWP (higher tax)
        var emp3 = new Employee(
            Guid.NewGuid(),
            "EMP-003",
            "Ahmad Hidayat",
            "ahmad.hidayat@company.com",
            null, // NO NPWP - will get 20% higher tax
            "K/0", // Married, no kids
            new DateOnly(2023, 3, 1)
        );
        emp3.AddSalaryComponent(new SalaryComponent(
            Guid.NewGuid(),
            "Basic Salary",
            new Money(5_000_000m),
            Domain.Enums.SalaryComponentType.BasicSalary,
            new DateOnly(2023, 3, 1)
        ));
        emp3.AddSalaryComponent(new SalaryComponent(
            Guid.NewGuid(),
            "Tunjangan Transport",
            new Money(500_000m),
            Domain.Enums.SalaryComponentType.FixedAllowance,
            new DateOnly(2023, 3, 1)
        ));
        employees.Add(emp3);
        
        // Employee 4: Senior - Above BPJS cap, married with 1 kid
        var emp4 = new Employee(
            Guid.NewGuid(),
            "EMP-004",
            "Dewi Lestari",
            "dewi.lestari@company.com",
            "456789012345678", // NPWP
            "K/1", // Married with 1 kid
            new DateOnly(2019, 8, 1)
        );
        emp4.AddSalaryComponent(new SalaryComponent(
            Guid.NewGuid(),
            "Basic Salary",
            new Money(20_000_000m), // Above BPJS cap
            Domain.Enums.SalaryComponentType.BasicSalary,
            new DateOnly(2019, 8, 1)
        ));
        emp4.AddSalaryComponent(new SalaryComponent(
            Guid.NewGuid(),
            "Tunjangan Jabatan",
            new Money(5_000_000m),
            Domain.Enums.SalaryComponentType.FixedAllowance,
            new DateOnly(2019, 8, 1)
        ));
        emp4.AddSalaryComponent(new SalaryComponent(
            Guid.NewGuid(),
            "Tunjangan Transport",
            new Money(2_000_000m),
            Domain.Enums.SalaryComponentType.FixedAllowance,
            new DateOnly(2019, 8, 1)
        ));
        employees.Add(emp4);
        
        // Employee 5: New hire - Joined mid-month (prorate scenario)
        // Only add if current period matches join month
        if (year == 2026 && month == 6)
        {
            var emp5 = new Employee(
                Guid.NewGuid(),
                "EMP-005",
                "Rina Wijaya",
                "rina.wijaya@company.com",
                "789012345678901", // NPWP
                "TK/1", // Single with 1 dependent
                new DateOnly(2026, 6, 15) // Joined mid-June - will be prorated
            );
            emp5.AddSalaryComponent(new SalaryComponent(
                Guid.NewGuid(),
                "Basic Salary",
                new Money(7_000_000m),
                Domain.Enums.SalaryComponentType.BasicSalary,
                new DateOnly(2026, 6, 15)
            ));
            emp5.AddSalaryComponent(new SalaryComponent(
                Guid.NewGuid(),
                "Tunjangan Transport",
                new Money(800_000m),
                Domain.Enums.SalaryComponentType.FixedAllowance,
                new DateOnly(2026, 6, 15)
            ));
            employees.Add(emp5);
        }
        
        _logger.LogInformation("Loaded {Count} mock employees for testing", employees.Count);
        return employees;
    }
}

// Made with Bob
