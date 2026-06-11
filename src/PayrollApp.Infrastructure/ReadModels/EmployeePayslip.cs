namespace PayrollApp.Infrastructure.ReadModels;

/// <summary>
/// Read model untuk payslip karyawan individual.
/// Digunakan untuk generate PDF dan tampilan detail payslip.
/// </summary>
public class EmployeePayslip
{
    /// <summary>
    /// Unique identifier untuk payslip (sama dengan PayrollLineItem Id)
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// PayrollRun Id yang memiliki payslip ini
    /// </summary>
    public Guid PayrollRunId { get; set; }
    
    /// <summary>
    /// Employee Id
    /// </summary>
    public string EmployeeId { get; set; } = string.Empty;
    
    /// <summary>
    /// Nama lengkap karyawan
    /// </summary>
    public string EmployeeName { get; set; } = string.Empty;
    
    /// <summary>
    /// Periode payroll (bulan)
    /// </summary>
    public int Month { get; set; }
    
    /// <summary>
    /// Periode payroll (tahun)
    /// </summary>
    public int Year { get; set; }
    
    // === Salary Components ===
    
    /// <summary>
    /// Gaji pokok
    /// </summary>
    public decimal BasicSalary { get; set; }
    
    /// <summary>
    /// Total tunjangan (transport, makan, dll)
    /// </summary>
    public decimal TotalAllowances { get; set; }
    
    /// <summary>
    /// Detail tunjangan per komponen
    /// </summary>
    public Dictionary<string, decimal> AllowanceBreakdown { get; set; } = new();
    
    /// <summary>
    /// Total lembur
    /// </summary>
    public decimal TotalOvertime { get; set; }
    
    /// <summary>
    /// Detail lembur per tipe
    /// </summary>
    public Dictionary<string, decimal> OvertimeBreakdown { get; set; } = new();
    
    /// <summary>
    /// Gross salary (sebelum potongan)
    /// </summary>
    public decimal GrossSalary { get; set; }
    
    // === Deductions ===
    
    /// <summary>
    /// Total potongan (absensi, pinjaman, dll)
    /// </summary>
    public decimal TotalDeductions { get; set; }
    
    /// <summary>
    /// Detail potongan per komponen
    /// </summary>
    public Dictionary<string, decimal> DeductionBreakdown { get; set; } = new();
    
    // === BPJS ===
    
    /// <summary>
    /// BPJS Kesehatan (employee portion)
    /// </summary>
    public decimal BPJSHealthEmployee { get; set; }
    
    /// <summary>
    /// BPJS Kesehatan (employer portion) - untuk informasi saja
    /// </summary>
    public decimal BPJSHealthEmployer { get; set; }
    
    /// <summary>
    /// BPJS JHT (employee portion)
    /// </summary>
    public decimal BPJSJHTEmployee { get; set; }
    
    /// <summary>
    /// BPJS JHT (employer portion) - untuk informasi saja
    /// </summary>
    public decimal BPJSJHTEmployer { get; set; }
    
    /// <summary>
    /// BPJS JP (employee portion)
    /// </summary>
    public decimal BPJSJPEmployee { get; set; }
    
    /// <summary>
    /// BPJS JP (employer portion) - untuk informasi saja
    /// </summary>
    public decimal BPJSJPEmployer { get; set; }
    
    /// <summary>
    /// Total BPJS employee portion (yang dipotong dari gaji)
    /// </summary>
    public decimal TotalBPJSEmployee { get; set; }
    
    /// <summary>
    /// Total BPJS employer portion (untuk informasi)
    /// </summary>
    public decimal TotalBPJSEmployer { get; set; }
    
    // === Tax ===
    
    /// <summary>
    /// PPh 21 yang dipotong
    /// </summary>
    public decimal Pph21 { get; set; }
    
    /// <summary>
    /// Status PTKP (TK/0, K/1, dll)
    /// </summary>
    public string PTKPStatus { get; set; } = string.Empty;
    
    /// <summary>
    /// NPWP karyawan (untuk display di payslip)
    /// </summary>
    public string? NPWP { get; set; }
    
    // === Final Amount ===
    
    /// <summary>
    /// Take home pay (yang diterima karyawan)
    /// </summary>
    public decimal TakeHomePay { get; set; }
    
    // === Prorate Info ===
    
    /// <summary>
    /// Apakah gaji di-prorate (join/resign di tengah bulan)
    /// </summary>
    public bool IsProrated { get; set; }
    
    /// <summary>
    /// Jumlah hari kerja aktual (kalau prorate)
    /// </summary>
    public int? WorkingDays { get; set; }
    
    /// <summary>
    /// Total hari kerja dalam bulan (kalau prorate)
    /// </summary>
    public int? TotalWorkingDays { get; set; }
    
    /// <summary>
    /// Prorate percentage (kalau prorate)
    /// </summary>
    public decimal? ProratePercentage { get; set; }
    
    // === Bank Info ===
    
    /// <summary>
    /// Nama bank karyawan
    /// </summary>
    public string? BankName { get; set; }
    
    /// <summary>
    /// Nomor rekening karyawan
    /// </summary>
    public string? BankAccountNumber { get; set; }
    
    /// <summary>
    /// Nama pemilik rekening
    /// </summary>
    public string? BankAccountName { get; set; }
    
    // === PDF Info ===
    
    /// <summary>
    /// Path ke file PDF payslip (setelah generated)
    /// </summary>
    public string? PayslipPdfPath { get; set; }
    
    /// <summary>
    /// Tanggal PDF payslip di-generate
    /// </summary>
    public DateTime? PayslipGeneratedAt { get; set; }
    
    // === Metadata ===
    
    /// <summary>
    /// Tanggal payslip dibuat
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    // === Display Helpers ===
    
    /// <summary>
    /// Format periode untuk display: "Januari 2024"
    /// </summary>
    public string PeriodDisplay => $"{GetMonthName(Month)} {Year}";
    
    /// <summary>
    /// Format gross salary untuk display: "Rp 10.000.000"
    /// </summary>
    public string GrossSalaryDisplay => $"Rp {GrossSalary:N0}";
    
    /// <summary>
    /// Format take home pay untuk display: "Rp 8.500.000"
    /// </summary>
    public string TakeHomePayDisplay => $"Rp {TakeHomePay:N0}";
    
    /// <summary>
    /// Format prorate info untuk display: "15/22 hari kerja (68%)"
    /// </summary>
    public string? ProrateDisplay => IsProrated && WorkingDays.HasValue && TotalWorkingDays.HasValue
        ? $"{WorkingDays}/{TotalWorkingDays} hari kerja ({ProratePercentage:P0})"
        : null;
    
    private static string GetMonthName(int month) => month switch
    {
        1 => "Januari",
        2 => "Februari",
        3 => "Maret",
        4 => "April",
        5 => "Mei",
        6 => "Juni",
        7 => "Juli",
        8 => "Agustus",
        9 => "September",
        10 => "Oktober",
        11 => "November",
        12 => "Desember",
        _ => month.ToString()
    };
}

// Made with Bob
