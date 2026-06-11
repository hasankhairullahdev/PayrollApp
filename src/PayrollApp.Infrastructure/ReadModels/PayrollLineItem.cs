namespace PayrollApp.Infrastructure.ReadModels;

/// <summary>
/// Read model untuk detail gaji per karyawan dalam satu payroll run
/// </summary>
public class PayrollLineItem
{
    public Guid Id { get; set; }
    public Guid PayrollRunId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    
    // Salary components
    public decimal BasicSalary { get; set; }
    public decimal Allowances { get; set; }
    public decimal Overtime { get; set; }
    public decimal GrossSalary { get; set; }
    
    // Deductions
    public decimal Deductions { get; set; }
    
    // BPJS
    public decimal BpjsKesehatan { get; set; }
    public decimal BpjsKetenagakerjaan { get; set; }
    public decimal TotalBpjs { get; set; }
    
    // Tax
    public decimal Pph21 { get; set; }
    
    // Net
    public decimal TakeHomePay { get; set; }
    
    // Metadata
    public bool IsProrated { get; set; }
    public decimal? ProratePercentage { get; set; }
    public DateTime CalculatedAt { get; set; }
    
    // Display helpers
    public string GrossSalaryDisplay => $"Rp {GrossSalary:N0}";
    public string TakeHomePayDisplay => $"Rp {TakeHomePay:N0}";
    public string Pph21Display => $"Rp {Pph21:N0}";
}

// Made with Bob
