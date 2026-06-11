namespace PayrollApp.Domain.ValueObjects;

/// <summary>
/// Value object untuk line item dalam payroll run.
/// Represents gaji satu karyawan dalam satu periode payroll.
/// </summary>
public record PayrollLineItem
{
    public Guid Id { get; init; }
    public string EmployeeId { get; init; }
    public string EmployeeName { get; init; }
    public decimal BasicSalary { get; init; }
    public decimal TotalAllowances { get; init; }
    public decimal TotalOvertime { get; init; }
    public decimal GrossSalary { get; init; }
    public decimal TotalDeductions { get; init; }
    public BPJSComponent BPJS { get; init; }
    public decimal Pph21 { get; init; }
    public decimal TakeHomePay { get; init; }
    
    // Prorate info
    public bool IsProrated { get; init; }
    public int? WorkingDays { get; init; }
    public int? TotalWorkingDays { get; init; }
    public decimal? ProratePercentage { get; init; }

    public PayrollLineItem(
        Guid id,
        string employeeId,
        string employeeName,
        decimal basicSalary,
        decimal totalAllowances,
        decimal totalOvertime,
        decimal grossSalary,
        decimal totalDeductions,
        BPJSComponent bpjs,
        decimal pph21,
        decimal takeHomePay,
        bool isProrated = false,
        int? workingDays = null,
        int? totalWorkingDays = null,
        decimal? proratePercentage = null)
    {
        Id = id;
        EmployeeId = employeeId;
        EmployeeName = employeeName;
        BasicSalary = basicSalary;
        TotalAllowances = totalAllowances;
        TotalOvertime = totalOvertime;
        GrossSalary = grossSalary;
        TotalDeductions = totalDeductions;
        BPJS = bpjs;
        Pph21 = pph21;
        TakeHomePay = takeHomePay;
        IsProrated = isProrated;
        WorkingDays = workingDays;
        TotalWorkingDays = totalWorkingDays;
        ProratePercentage = proratePercentage;
    }
}

// Made with Bob
