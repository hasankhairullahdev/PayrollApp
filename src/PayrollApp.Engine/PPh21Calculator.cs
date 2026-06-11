using PayrollApp.Domain.ValueObjects;

namespace PayrollApp.Engine;

/// <summary>
/// Calculator untuk PPh 21 menggunakan metode TER (Tarif Efektif Rata-rata)
/// Sesuai PMK 168/2023
/// </summary>
public static class PPh21Calculator
{
    /// <summary>
    /// Calculate PPh 21 bulanan menggunakan metode TER
    /// </summary>
    /// <param name="monthlyGrossIncome">Penghasilan bruto per bulan (gaji + tunjangan)</param>
    /// <param name="ptkpStatus">Status PTKP (TK/0, TK/1, K/0, K/1, dll)</param>
    /// <param name="hasNpwp">Apakah karyawan punya NPWP</param>
    /// <returns>TaxCalculation dengan detail perhitungan</returns>
    public static TaxCalculation Calculate(
        Money monthlyGrossIncome,
        string ptkpStatus,
        bool hasNpwp)
    {
        if (monthlyGrossIncome.Amount <= 0)
        {
            return new TaxCalculation(
                monthlyGrossIncome,
                Money.Zero,
                ptkpStatus,
                hasNpwp
            );
        }

        // Get TER rate berdasarkan penghasilan bruto bulanan
        var terRate = TaxBrackets.GetTerRate(monthlyGrossIncome.Amount);

        // Calculate PPh 21 menggunakan TER
        var pph21Amount = monthlyGrossIncome.Amount * (terRate / 100m);

        // Jika tidak punya NPWP, tarif lebih tinggi 20%
        if (!hasNpwp)
        {
            pph21Amount *= TaxBrackets.NoNpwpMultiplier;
        }

        // Round ke rupiah terdekat
        var pph21Money = new Money(pph21Amount).Round();

        return new TaxCalculation(
            monthlyGrossIncome,
            pph21Money,
            ptkpStatus,
            hasNpwp
        );
    }

    /// <summary>
    /// Calculate PPh 21 tahunan (untuk bonus atau THR)
    /// </summary>
    /// <param name="annualGrossIncome">Penghasilan bruto per tahun</param>
    /// <param name="ptkpStatus">Status PTKP</param>
    /// <param name="hasNpwp">Apakah karyawan punya NPWP</param>
    /// <returns>TaxCalculation dengan detail perhitungan</returns>
    public static TaxCalculation CalculateAnnual(
        Money annualGrossIncome,
        string ptkpStatus,
        bool hasNpwp)
    {
        if (annualGrossIncome.Amount <= 0)
        {
            return new TaxCalculation(
                annualGrossIncome,
                Money.Zero,
                ptkpStatus,
                hasNpwp
            );
        }

        // Get PTKP amount
        var ptkpAmount = TaxBrackets.GetPtkp(ptkpStatus);

        // Penghasilan Kena Pajak (PKP)
        var pkp = Math.Max(0, annualGrossIncome.Amount - ptkpAmount);

        // Progressive tax calculation
        var tax = CalculateProgressiveTax(pkp);

        // Jika tidak punya NPWP, tarif lebih tinggi 20%
        if (!hasNpwp)
        {
            tax *= TaxBrackets.NoNpwpMultiplier;
        }

        // Round ke rupiah terdekat
        var taxMoney = new Money(tax).Round();

        return new TaxCalculation(
            annualGrossIncome,
            taxMoney,
            ptkpStatus,
            hasNpwp
        );
    }

    /// <summary>
    /// Calculate progressive tax berdasarkan PKP (Penghasilan Kena Pajak)
    /// Layer 1: 0 - 60jt = 5%
    /// Layer 2: 60jt - 250jt = 15%
    /// Layer 3: 250jt - 500jt = 25%
    /// Layer 4: 500jt - 5M = 30%
    /// Layer 5: > 5M = 35%
    /// </summary>
    private static decimal CalculateProgressiveTax(decimal pkp)
    {
        if (pkp <= 0) return 0;

        decimal tax = 0;

        // Layer 1: 0 - 60 juta (5%)
        if (pkp > 0)
        {
            var layer1 = Math.Min(pkp, 60_000_000m);
            tax += layer1 * 0.05m;
        }

        // Layer 2: 60 juta - 250 juta (15%)
        if (pkp > 60_000_000m)
        {
            var layer2 = Math.Min(pkp - 60_000_000m, 190_000_000m);
            tax += layer2 * 0.15m;
        }

        // Layer 3: 250 juta - 500 juta (25%)
        if (pkp > 250_000_000m)
        {
            var layer3 = Math.Min(pkp - 250_000_000m, 250_000_000m);
            tax += layer3 * 0.25m;
        }

        // Layer 4: 500 juta - 5 miliar (30%)
        if (pkp > 500_000_000m)
        {
            var layer4 = Math.Min(pkp - 500_000_000m, 4_500_000_000m);
            tax += layer4 * 0.30m;
        }

        // Layer 5: > 5 miliar (35%)
        if (pkp > 5_000_000_000m)
        {
            var layer5 = pkp - 5_000_000_000m;
            tax += layer5 * 0.35m;
        }

        return tax;
    }

    /// <summary>
    /// Estimate monthly PPh 21 dari gaji tahunan
    /// Berguna untuk proyeksi atau simulasi
    /// </summary>
    public static Money EstimateMonthlyFromAnnual(
        Money annualGrossIncome,
        string ptkpStatus,
        bool hasNpwp)
    {
        var annualTax = CalculateAnnual(annualGrossIncome, ptkpStatus, hasNpwp);
        return (annualTax.Pph21Amount / 12m).Round();
    }
}

// Made with Bob
