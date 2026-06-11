using PayrollApp.Domain.ValueObjects;

namespace PayrollApp.Engine;

/// <summary>
/// Calculator untuk prorate gaji karyawan yang join/resign di tengah bulan
/// Berdasarkan hari kerja (exclude weekend dan libur nasional)
/// </summary>
public static class ProrateCalculator
{
    /// <summary>
    /// Calculate prorated salary untuk karyawan yang join di tengah bulan
    /// </summary>
    /// <param name="fullMonthlySalary">Gaji penuh per bulan</param>
    /// <param name="joinDate">Tanggal join</param>
    /// <param name="periodMonth">Bulan periode payroll</param>
    /// <param name="periodYear">Tahun periode payroll</param>
    /// <returns>Gaji yang sudah di-prorate</returns>
    public static Money CalculateForJoin(
        Money fullMonthlySalary,
        DateOnly joinDate,
        int periodMonth,
        int periodYear)
    {
        if (fullMonthlySalary.Amount <= 0)
            return Money.Zero;

        var periodStart = new DateOnly(periodYear, periodMonth, 1);
        var periodEnd = new DateOnly(periodYear, periodMonth, DateTime.DaysInMonth(periodYear, periodMonth));

        // Jika join sebelum periode, return full salary
        if (joinDate <= periodStart)
            return fullMonthlySalary;

        // Jika join setelah periode, return zero
        if (joinDate > periodEnd)
            return Money.Zero;

        // Hitung hari kerja dari join date sampai akhir bulan
        var workingDaysWorked = CountWorkingDays(joinDate, periodEnd);
        var totalWorkingDays = CountWorkingDays(periodStart, periodEnd);

        if (totalWorkingDays == 0)
            return Money.Zero;

        var proratedAmount = fullMonthlySalary.Amount * workingDaysWorked / totalWorkingDays;
        return new Money(proratedAmount).Round();
    }

    /// <summary>
    /// Calculate prorated salary untuk karyawan yang resign di tengah bulan
    /// </summary>
    /// <param name="fullMonthlySalary">Gaji penuh per bulan</param>
    /// <param name="resignDate">Tanggal resign (hari terakhir kerja)</param>
    /// <param name="periodMonth">Bulan periode payroll</param>
    /// <param name="periodYear">Tahun periode payroll</param>
    /// <returns>Gaji yang sudah di-prorate</returns>
    public static Money CalculateForResign(
        Money fullMonthlySalary,
        DateOnly resignDate,
        int periodMonth,
        int periodYear)
    {
        if (fullMonthlySalary.Amount <= 0)
            return Money.Zero;

        var periodStart = new DateOnly(periodYear, periodMonth, 1);
        var periodEnd = new DateOnly(periodYear, periodMonth, DateTime.DaysInMonth(periodYear, periodMonth));

        // Jika resign setelah periode, return full salary
        if (resignDate >= periodEnd)
            return fullMonthlySalary;

        // Jika resign sebelum periode, return zero
        if (resignDate < periodStart)
            return Money.Zero;

        // Hitung hari kerja dari awal bulan sampai resign date
        var workingDaysWorked = CountWorkingDays(periodStart, resignDate);
        var totalWorkingDays = CountWorkingDays(periodStart, periodEnd);

        if (totalWorkingDays == 0)
            return Money.Zero;

        var proratedAmount = fullMonthlySalary.Amount * workingDaysWorked / totalWorkingDays;
        return new Money(proratedAmount).Round();
    }

    /// <summary>
    /// Calculate prorated salary dengan custom date range
    /// </summary>
    /// <param name="fullMonthlySalary">Gaji penuh per bulan</param>
    /// <param name="startDate">Tanggal mulai</param>
    /// <param name="endDate">Tanggal akhir</param>
    /// <param name="periodMonth">Bulan periode payroll</param>
    /// <param name="periodYear">Tahun periode payroll</param>
    /// <returns>Gaji yang sudah di-prorate</returns>
    public static Money Calculate(
        Money fullMonthlySalary,
        DateOnly startDate,
        DateOnly endDate,
        int periodMonth,
        int periodYear)
    {
        if (fullMonthlySalary.Amount <= 0)
            return Money.Zero;

        var periodStart = new DateOnly(periodYear, periodMonth, 1);
        var periodEnd = new DateOnly(periodYear, periodMonth, DateTime.DaysInMonth(periodYear, periodMonth));

        // Adjust dates to be within period
        var effectiveStart = startDate < periodStart ? periodStart : startDate;
        var effectiveEnd = endDate > periodEnd ? periodEnd : endDate;

        // Jika range tidak valid, return zero
        if (effectiveStart > effectiveEnd)
            return Money.Zero;

        var workingDaysWorked = CountWorkingDays(effectiveStart, effectiveEnd);
        var totalWorkingDays = CountWorkingDays(periodStart, periodEnd);

        if (totalWorkingDays == 0)
            return Money.Zero;

        var proratedAmount = fullMonthlySalary.Amount * workingDaysWorked / totalWorkingDays;
        return new Money(proratedAmount).Round();
    }

    /// <summary>
    /// Count working days between two dates (exclude weekends)
    /// Simplified version - tidak include libur nasional
    /// Untuk production, bisa integrate dengan holiday calendar
    /// </summary>
    public static int CountWorkingDays(DateOnly startDate, DateOnly endDate)
    {
        if (startDate > endDate)
            return 0;

        int workingDays = 0;
        var currentDate = startDate;

        while (currentDate <= endDate)
        {
            // Skip Saturday (6) and Sunday (0)
            if (currentDate.DayOfWeek != DayOfWeek.Saturday && 
                currentDate.DayOfWeek != DayOfWeek.Sunday)
            {
                workingDays++;
            }

            currentDate = currentDate.AddDays(1);
        }

        return workingDays;
    }

    /// <summary>
    /// Count working days dengan exclude specific holidays
    /// </summary>
    public static int CountWorkingDaysWithHolidays(
        DateOnly startDate,
        DateOnly endDate,
        IEnumerable<DateOnly> holidays)
    {
        if (startDate > endDate)
            return 0;

        var holidaySet = new HashSet<DateOnly>(holidays);
        int workingDays = 0;
        var currentDate = startDate;

        while (currentDate <= endDate)
        {
            // Skip weekends and holidays
            if (currentDate.DayOfWeek != DayOfWeek.Saturday && 
                currentDate.DayOfWeek != DayOfWeek.Sunday &&
                !holidaySet.Contains(currentDate))
            {
                workingDays++;
            }

            currentDate = currentDate.AddDays(1);
        }

        return workingDays;
    }

    /// <summary>
    /// Get prorate percentage (untuk display/reporting)
    /// </summary>
    public static decimal GetProratePercentage(
        DateOnly startDate,
        DateOnly endDate,
        int periodMonth,
        int periodYear)
    {
        var periodStart = new DateOnly(periodYear, periodMonth, 1);
        var periodEnd = new DateOnly(periodYear, periodMonth, DateTime.DaysInMonth(periodYear, periodMonth));

        var effectiveStart = startDate < periodStart ? periodStart : startDate;
        var effectiveEnd = endDate > periodEnd ? periodEnd : endDate;

        if (effectiveStart > effectiveEnd)
            return 0;

        var workingDaysWorked = CountWorkingDays(effectiveStart, effectiveEnd);
        var totalWorkingDays = CountWorkingDays(periodStart, periodEnd);

        if (totalWorkingDays == 0)
            return 0;

        return Math.Round((decimal)workingDaysWorked / totalWorkingDays * 100, 2);
    }

    /// <summary>
    /// Calculate prorated amount untuk komponen gaji tertentu
    /// Berguna untuk prorate tunjangan, bonus, dll
    /// </summary>
    public static Money ProrateComponent(
        Money componentAmount,
        int workedDays,
        int totalDays)
    {
        if (componentAmount.Amount <= 0 || totalDays == 0)
            return Money.Zero;

        var proratedAmount = componentAmount.Amount * workedDays / totalDays;
        return new Money(proratedAmount).Round();
    }
}

// Made with Bob
