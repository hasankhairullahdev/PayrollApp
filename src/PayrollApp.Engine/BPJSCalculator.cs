using PayrollApp.Domain.ValueObjects;

namespace PayrollApp.Engine;

/// <summary>
/// Calculator untuk BPJS Kesehatan dan Ketenagakerjaan
/// Sesuai peraturan BPJS terbaru
/// </summary>
public static class BPJSCalculator
{
    // BPJS Kesehatan
    private const decimal KesehatanEmployeeRate = 0.01m;      // 1%
    private const decimal KesehatanEmployerRate = 0.04m;      // 4%
    private const decimal KesehatanCap = 12_000_000m;         // Cap 12 juta

    // BPJS Ketenagakerjaan - JHT (Jaminan Hari Tua)
    private const decimal JhtEmployeeRate = 0.02m;            // 2%
    private const decimal JhtEmployerRate = 0.037m;           // 3.7%

    // BPJS Ketenagakerjaan - JP (Jaminan Pensiun)
    private const decimal JpEmployeeRate = 0.01m;             // 1%
    private const decimal JpEmployerRate = 0.02m;             // 2%
    private const decimal JpCap = 10_042_300m;                // Cap ~10 juta (upah maksimal)

    // BPJS Ketenagakerjaan - JKK (Jaminan Kecelakaan Kerja) - employer only
    private const decimal JkkRate = 0.0024m;                  // 0.24% (risiko rendah)

    // BPJS Ketenagakerjaan - JKM (Jaminan Kematian) - employer only
    private const decimal JkmRate = 0.003m;                   // 0.3%

    /// <summary>
    /// Calculate semua komponen BPJS berdasarkan gaji
    /// </summary>
    /// <param name="salary">Gaji yang dijadikan dasar perhitungan BPJS</param>
    /// <returns>BPJSComponent dengan breakdown semua komponen</returns>
    public static BPJSComponent Calculate(Money salary)
    {
        if (salary.Amount <= 0)
        {
            return BPJSComponent.Zero;
        }

        // BPJS Kesehatan - dengan cap 12 juta
        var kesehatanBase = Math.Min(salary.Amount, KesehatanCap);
        var kesehatanEmployee = new Money(kesehatanBase * KesehatanEmployeeRate).Round();
        var kesehatanEmployer = new Money(kesehatanBase * KesehatanEmployerRate).Round();

        // BPJS Ketenagakerjaan - JHT (no cap)
        var jhtEmployee = new Money(salary.Amount * JhtEmployeeRate).Round();
        var jhtEmployer = new Money(salary.Amount * JhtEmployerRate).Round();

        // BPJS Ketenagakerjaan - JP (dengan cap ~10 juta)
        var jpBase = Math.Min(salary.Amount, JpCap);
        var jpEmployee = new Money(jpBase * JpEmployeeRate).Round();
        var jpEmployer = new Money(jpBase * JpEmployerRate).Round();

        // BPJS Ketenagakerjaan - JKK (employer only, no cap)
        var jkkEmployer = new Money(salary.Amount * JkkRate).Round();

        // BPJS Ketenagakerjaan - JKM (employer only, no cap)
        var jkmEmployer = new Money(salary.Amount * JkmRate).Round();

        return new BPJSComponent(
            jhtEmployee,
            jhtEmployer,
            jpEmployee,
            jpEmployer,
            jkkEmployer,
            jkmEmployer,
            kesehatanEmployee,
            kesehatanEmployer
        );
    }

    /// <summary>
    /// Calculate hanya BPJS Kesehatan
    /// </summary>
    public static (Money Employee, Money Employer) CalculateKesehatan(Money salary)
    {
        if (salary.Amount <= 0)
            return (Money.Zero, Money.Zero);

        var base_ = Math.Min(salary.Amount, KesehatanCap);
        var employee = new Money(base_ * KesehatanEmployeeRate).Round();
        var employer = new Money(base_ * KesehatanEmployerRate).Round();

        return (employee, employer);
    }

    /// <summary>
    /// Calculate hanya BPJS Ketenagakerjaan (JHT + JP + JKK + JKM)
    /// </summary>
    public static BPJSComponent CalculateKetenagakerjaan(Money salary)
    {
        if (salary.Amount <= 0)
            return BPJSComponent.Zero;

        // JHT
        var jhtEmployee = new Money(salary.Amount * JhtEmployeeRate).Round();
        var jhtEmployer = new Money(salary.Amount * JhtEmployerRate).Round();

        // JP
        var jpBase = Math.Min(salary.Amount, JpCap);
        var jpEmployee = new Money(jpBase * JpEmployeeRate).Round();
        var jpEmployer = new Money(jpBase * JpEmployerRate).Round();

        // JKK & JKM
        var jkkEmployer = new Money(salary.Amount * JkkRate).Round();
        var jkmEmployer = new Money(salary.Amount * JkmRate).Round();

        return new BPJSComponent(
            jhtEmployee,
            jhtEmployer,
            jpEmployee,
            jpEmployer,
            jkkEmployer,
            jkmEmployer,
            Money.Zero,
            Money.Zero
        );
    }

    /// <summary>
    /// Get total employee contribution (potongan dari gaji)
    /// </summary>
    public static Money GetEmployeeContribution(Money salary)
    {
        var bpjs = Calculate(salary);
        return bpjs.TotalEmployeeContribution;
    }

    /// <summary>
    /// Get total employer contribution (beban perusahaan)
    /// </summary>
    public static Money GetEmployerContribution(Money salary)
    {
        var bpjs = Calculate(salary);
        return bpjs.TotalEmployerContribution;
    }

    /// <summary>
    /// Calculate BPJS dengan custom JKK rate (untuk industri dengan risiko berbeda)
    /// </summary>
    /// <param name="salary">Gaji dasar</param>
    /// <param name="jkkRate">Custom JKK rate (0.24% untuk risiko rendah, 0.54% sedang, 0.89% tinggi, dst)</param>
    public static BPJSComponent CalculateWithCustomJkk(Money salary, decimal jkkRate)
    {
        if (salary.Amount <= 0)
            return BPJSComponent.Zero;

        var kesehatanBase = Math.Min(salary.Amount, KesehatanCap);
        var kesehatanEmployee = new Money(kesehatanBase * KesehatanEmployeeRate).Round();
        var kesehatanEmployer = new Money(kesehatanBase * KesehatanEmployerRate).Round();

        var jhtEmployee = new Money(salary.Amount * JhtEmployeeRate).Round();
        var jhtEmployer = new Money(salary.Amount * JhtEmployerRate).Round();

        var jpBase = Math.Min(salary.Amount, JpCap);
        var jpEmployee = new Money(jpBase * JpEmployeeRate).Round();
        var jpEmployer = new Money(jpBase * JpEmployerRate).Round();

        var jkkEmployer = new Money(salary.Amount * jkkRate).Round();
        var jkmEmployer = new Money(salary.Amount * JkmRate).Round();

        return new BPJSComponent(
            jhtEmployee,
            jhtEmployer,
            jpEmployee,
            jpEmployer,
            jkkEmployer,
            jkmEmployer,
            kesehatanEmployee,
            kesehatanEmployer
        );
    }
}

// Made with Bob
