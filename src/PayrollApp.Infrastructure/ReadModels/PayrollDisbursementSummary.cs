namespace PayrollApp.Infrastructure.ReadModels;

/// <summary>
/// Read model untuk summary disbursement payroll.
/// Digunakan untuk tracking transfer bank dan konfirmasi pembayaran.
/// </summary>
public class PayrollDisbursementSummary
{
    /// <summary>
    /// Unique identifier (sama dengan PayrollRun Id)
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// PayrollRun Id
    /// </summary>
    public Guid PayrollRunId { get; set; }
    
    /// <summary>
    /// Periode payroll (bulan)
    /// </summary>
    public int Month { get; set; }
    
    /// <summary>
    /// Periode payroll (tahun)
    /// </summary>
    public int Year { get; set; }
    
    // === Disbursement Info ===
    
    /// <summary>
    /// Total amount yang akan ditransfer
    /// </summary>
    public decimal TotalAmount { get; set; }
    
    /// <summary>
    /// Jumlah karyawan yang akan menerima transfer
    /// </summary>
    public int TotalEmployees { get; set; }
    
    /// <summary>
    /// Path ke file bank yang di-generate (untuk upload ke bank)
    /// </summary>
    public string? BankFilePath { get; set; }
    
    /// <summary>
    /// Format file bank (SKN, RTGS, LLG, dll)
    /// </summary>
    public string? BankFileFormat { get; set; }
    
    /// <summary>
    /// Tanggal disbursement initiated
    /// </summary>
    public DateTime InitiatedAt { get; set; }
    
    /// <summary>
    /// User yang initiate disbursement
    /// </summary>
    public string InitiatedBy { get; set; } = string.Empty;
    
    // === Confirmation Info ===
    
    /// <summary>
    /// Apakah disbursement sudah dikonfirmasi (transfer selesai)
    /// </summary>
    public bool IsConfirmed { get; set; }
    
    /// <summary>
    /// Tanggal konfirmasi transfer selesai
    /// </summary>
    public DateTime? ConfirmedAt { get; set; }
    
    /// <summary>
    /// User yang konfirmasi transfer selesai
    /// </summary>
    public string? ConfirmedBy { get; set; }
    
    /// <summary>
    /// Catatan konfirmasi (optional)
    /// </summary>
    public string? ConfirmationNotes { get; set; }
    
    // === Bank Breakdown ===
    
    /// <summary>
    /// Breakdown per bank: bank name → total amount
    /// Untuk tracking berapa yang harus ditransfer ke masing-masing bank
    /// </summary>
    public Dictionary<string, decimal> BankBreakdown { get; set; } = new();
    
    /// <summary>
    /// Breakdown per bank: bank name → jumlah karyawan
    /// </summary>
    public Dictionary<string, int> BankEmployeeCount { get; set; } = new();
    
    // === Employee Details ===
    
    /// <summary>
    /// List detail transfer per karyawan
    /// </summary>
    public List<EmployeeTransferDetail> EmployeeTransfers { get; set; } = new();
    
    // === Display Helpers ===
    
    /// <summary>
    /// Format periode untuk display: "Januari 2024"
    /// </summary>
    public string PeriodDisplay => $"{GetMonthName(Month)} {Year}";
    
    /// <summary>
    /// Format total amount untuk display: "Rp 100.000.000"
    /// </summary>
    public string TotalAmountDisplay => $"Rp {TotalAmount:N0}";
    
    /// <summary>
    /// Status display: "Menunggu Konfirmasi" atau "Selesai"
    /// </summary>
    public string StatusDisplay => IsConfirmed ? "Selesai" : "Menunggu Konfirmasi";
    
    /// <summary>
    /// Format tanggal untuk display
    /// </summary>
    public string InitiatedAtDisplay => InitiatedAt.ToString("dd MMM yyyy HH:mm");
    
    /// <summary>
    /// Format tanggal konfirmasi untuk display
    /// </summary>
    public string? ConfirmedAtDisplay => ConfirmedAt?.ToString("dd MMM yyyy HH:mm");
    
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

/// <summary>
/// Detail transfer untuk satu karyawan
/// </summary>
public class EmployeeTransferDetail
{
    /// <summary>
    /// Employee Id
    /// </summary>
    public string EmployeeId { get; set; } = string.Empty;
    
    /// <summary>
    /// Nama karyawan
    /// </summary>
    public string EmployeeName { get; set; } = string.Empty;
    
    /// <summary>
    /// Nama bank
    /// </summary>
    public string BankName { get; set; } = string.Empty;
    
    /// <summary>
    /// Nomor rekening
    /// </summary>
    public string BankAccountNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// Nama pemilik rekening
    /// </summary>
    public string BankAccountName { get; set; } = string.Empty;
    
    /// <summary>
    /// Amount yang akan ditransfer (take home pay)
    /// </summary>
    public decimal Amount { get; set; }
    
    /// <summary>
    /// Format amount untuk display: "Rp 8.500.000"
    /// </summary>
    public string AmountDisplay => $"Rp {Amount:N0}";
}

// Made with Bob
