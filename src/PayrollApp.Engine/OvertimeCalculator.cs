using PayrollApp.Domain.ValueObjects;

namespace PayrollApp.Engine;

/// <summary>
/// Calculator untuk overtime/lembur sesuai UU Ketenagakerjaan Indonesia
/// </summary>
public static class OvertimeCalculator
{
    // Jam kerja normal per hari
    private const int NormalWorkHoursPerDay = 8;
    
    // Jam kerja normal per minggu
    private const int NormalWorkHoursPerWeek = 40;

    /// <summary>
    /// Tipe overtime
    /// </summary>
    public enum OvertimeType
    {
        /// <summary>
        /// Lembur hari kerja biasa (Senin-Jumat)
        /// </summary>
        Weekday,
        
        /// <summary>
        /// Lembur hari libur mingguan (Sabtu-Minggu)
        /// </summary>
        Weekend,
        
        /// <summary>
        /// Lembur hari libur nasional
        /// </summary>
        Holiday
    }

    /// <summary>
    /// Calculate overtime pay berdasarkan gaji pokok dan jam lembur
    /// </summary>
    /// <param name="basicSalary">Gaji pokok per bulan</param>
    /// <param name="overtimeHours">Jumlah jam lembur</param>
    /// <param name="type">Tipe overtime (weekday/weekend/holiday)</param>
    /// <returns>Total upah lembur</returns>
    public static Money Calculate(
        Money basicSalary,
        decimal overtimeHours,
        OvertimeType type)
    {
        if (basicSalary.Amount <= 0 || overtimeHours <= 0)
            return Money.Zero;

        // Hitung upah per jam (1/173 dari gaji pokok)
        // 173 = rata-rata jam kerja per bulan (40 jam/minggu × 52 minggu / 12 bulan)
        var hourlyRate = basicSalary.Amount / 173m;

        return type switch
        {
            OvertimeType.Weekday => CalculateWeekdayOvertime(hourlyRate, overtimeHours),
            OvertimeType.Weekend => CalculateWeekendOvertime(hourlyRate, overtimeHours),
            OvertimeType.Holiday => CalculateHolidayOvertime(hourlyRate, overtimeHours),
            _ => Money.Zero
        };
    }

    /// <summary>
    /// Calculate lembur hari kerja biasa
    /// Jam ke-1: 1.5x upah per jam
    /// Jam ke-2 dst: 2x upah per jam
    /// </summary>
    private static Money CalculateWeekdayOvertime(decimal hourlyRate, decimal hours)
    {
        decimal total = 0;

        if (hours >= 1)
        {
            // Jam pertama: 1.5x
            total += hourlyRate * 1.5m;
            hours -= 1;
        }

        if (hours > 0)
        {
            // Jam berikutnya: 2x
            total += hourlyRate * 2m * hours;
        }

        return new Money(total).Round();
    }

    /// <summary>
    /// Calculate lembur hari libur mingguan (Sabtu/Minggu)
    /// Jika kerja < 8 jam:
    ///   - 7 jam pertama: 2x upah per jam
    ///   - Jam ke-8: 3x upah per jam
    ///   - Jam ke-9 dst: 4x upah per jam
    /// Jika kerja >= 8 jam:
    ///   - 8 jam pertama: 2x upah per jam
    ///   - Jam ke-9 dst: 4x upah per jam
    /// </summary>
    private static Money CalculateWeekendOvertime(decimal hourlyRate, decimal hours)
    {
        decimal total = 0;

        if (hours <= 8)
        {
            // Kurang dari atau sama dengan 8 jam
            if (hours <= 7)
            {
                // 7 jam pertama: 2x
                total += hourlyRate * 2m * hours;
            }
            else
            {
                // 7 jam pertama: 2x
                total += hourlyRate * 2m * 7m;
                // Jam ke-8: 3x
                total += hourlyRate * 3m * (hours - 7);
            }
        }
        else
        {
            // Lebih dari 8 jam
            // 8 jam pertama: 2x
            total += hourlyRate * 2m * 8m;
            // Jam ke-9 dst: 4x
            total += hourlyRate * 4m * (hours - 8);
        }

        return new Money(total).Round();
    }

    /// <summary>
    /// Calculate lembur hari libur nasional
    /// Sama seperti weekend tapi dengan multiplier lebih tinggi
    /// Jika kerja < 8 jam:
    ///   - 7 jam pertama: 2x upah per jam
    ///   - Jam ke-8: 3x upah per jam
    ///   - Jam ke-9 dst: 4x upah per jam
    /// Jika kerja >= 8 jam:
    ///   - 8 jam pertama: 2x upah per jam
    ///   - Jam ke-9 dst: 4x upah per jam
    /// </summary>
    private static Money CalculateHolidayOvertime(decimal hourlyRate, decimal hours)
    {
        // Untuk hari libur nasional, perhitungannya sama dengan weekend
        // tapi bisa ditambahkan bonus atau multiplier tambahan jika diperlukan
        return CalculateWeekendOvertime(hourlyRate, hours);
    }

    /// <summary>
    /// Calculate total overtime untuk multiple entries
    /// </summary>
    public static Money CalculateTotal(
        Money basicSalary,
        IEnumerable<(decimal Hours, OvertimeType Type)> overtimeEntries)
    {
        var total = Money.Zero;

        foreach (var (hours, type) in overtimeEntries)
        {
            total += Calculate(basicSalary, hours, type);
        }

        return total;
    }

    /// <summary>
    /// Get hourly rate dari gaji pokok
    /// </summary>
    public static Money GetHourlyRate(Money basicSalary)
    {
        if (basicSalary.Amount <= 0)
            return Money.Zero;

        return new Money(basicSalary.Amount / 173m).Round();
    }

    /// <summary>
    /// Estimate overtime hours yang dibutuhkan untuk mencapai target amount
    /// </summary>
    public static decimal EstimateHoursForAmount(
        Money basicSalary,
        Money targetAmount,
        OvertimeType type)
    {
        if (basicSalary.Amount <= 0 || targetAmount.Amount <= 0)
            return 0;

        var hourlyRate = basicSalary.Amount / 173m;

        // Simplified estimation (assumes all hours at same rate)
        var multiplier = type switch
        {
            OvertimeType.Weekday => 1.75m, // Average between 1.5x and 2x
            OvertimeType.Weekend => 2.5m,  // Average
            OvertimeType.Holiday => 2.5m,  // Average
            _ => 1.5m
        };

        return targetAmount.Amount / (hourlyRate * multiplier);
    }
}

// Made with Bob
