using PayrollApp.Domain.Enums;
using PayrollApp.Domain.ValueObjects;

namespace PayrollApp.Infrastructure.ReadModels;

/// <summary>
/// Read model untuk summary payroll run
/// Diupdate via Marten projection dari domain events
/// </summary>
public class PayrollRunSummary
{
    public Guid Id { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public PayrollStatus Status { get; set; }
    public int TotalEmployees { get; set; }
    public decimal TotalAmount { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? LockedBy { get; set; }
    public DateTime? LockedAt { get; set; }
    
    // For display
    public string PeriodDisplay => $"{GetMonthName(Month)} {Year}";
    public string StatusDisplay => Status.ToString();
    public string TotalAmountDisplay => $"Rp {TotalAmount:N0}";

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
