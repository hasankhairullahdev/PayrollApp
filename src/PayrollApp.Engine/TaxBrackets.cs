namespace PayrollApp.Engine;

/// <summary>
/// Static tax brackets data untuk PPh 21 calculation
/// Menggunakan Tarif Efektif Rata-rata (TER) sesuai PMK 168/2023
/// </summary>
public static class TaxBrackets
{
    /// <summary>
    /// PTKP (Penghasilan Tidak Kena Pajak) per tahun dalam Rupiah
    /// </summary>
    public static readonly Dictionary<string, decimal> PtkpAnnual = new()
    {
        { "TK/0", 54_000_000m },   // Tidak Kawin, 0 tanggungan
        { "TK/1", 58_500_000m },   // Tidak Kawin, 1 tanggungan
        { "TK/2", 63_000_000m },   // Tidak Kawin, 2 tanggungan
        { "TK/3", 67_500_000m },   // Tidak Kawin, 3 tanggungan
        { "K/0", 58_500_000m },    // Kawin, 0 tanggungan
        { "K/1", 63_000_000m },    // Kawin, 1 tanggungan
        { "K/2", 67_500_000m },    // Kawin, 2 tanggungan
        { "K/3", 72_000_000m }     // Kawin, 3 tanggungan
    };

    /// <summary>
    /// TER (Tarif Efektif Rata-rata) brackets untuk pegawai tetap
    /// Format: (minIncome, maxIncome, rate)
    /// Rate dalam persentase (contoh: 0.5 = 0.5%)
    /// </summary>
    public static readonly List<(decimal Min, decimal Max, decimal Rate)> TerBrackets = new()
    {
        (0m, 5_400_000m, 0m),                    // 0%
        (5_400_001m, 5_650_000m, 0.5m),          // 0.5%
        (5_650_001m, 5_950_000m, 0.75m),         // 0.75%
        (5_950_001m, 6_300_000m, 1m),            // 1%
        (6_300_001m, 6_750_000m, 1.25m),         // 1.25%
        (6_750_001m, 7_500_000m, 1.5m),          // 1.5%
        (7_500_001m, 8_550_000m, 1.75m),         // 1.75%
        (8_550_001m, 9_650_000m, 2m),            // 2%
        (9_650_001m, 10_050_000m, 2.25m),        // 2.25%
        (10_050_001m, 10_350_000m, 2.5m),        // 2.5%
        (10_350_001m, 10_700_000m, 3m),          // 3%
        (10_700_001m, 11_050_000m, 3.5m),        // 3.5%
        (11_050_001m, 11_600_000m, 4m),          // 4%
        (11_600_001m, 12_500_000m, 5m),          // 5%
        (12_500_001m, 13_750_000m, 6m),          // 6%
        (13_750_001m, 15_100_000m, 7m),          // 7%
        (15_100_001m, 16_950_000m, 8m),          // 8%
        (16_950_001m, 19_750_000m, 9m),          // 9%
        (19_750_001m, 24_150_000m, 10m),         // 10%
        (24_150_001m, 26_450_000m, 11m),         // 11%
        (26_450_001m, 28_000_000m, 12m),         // 12%
        (28_000_001m, 30_050_000m, 13m),         // 13%
        (30_050_001m, 32_400_000m, 14m),         // 14%
        (32_400_001m, 35_400_000m, 15m),         // 15%
        (35_400_001m, 39_100_000m, 16m),         // 16%
        (39_100_001m, 43_850_000m, 17m),         // 17%
        (43_850_001m, 47_800_000m, 18m),         // 18%
        (47_800_001m, 51_400_000m, 19m),         // 19%
        (51_400_001m, 56_300_000m, 20m),         // 20%
        (56_300_001m, 62_200_000m, 21m),         // 21%
        (62_200_001m, 68_600_000m, 22m),         // 22%
        (68_600_001m, 77_500_000m, 23m),         // 23%
        (77_500_001m, 89_000_000m, 24m),         // 24%
        (89_000_001m, 103_000_000m, 25m),        // 25%
        (103_000_001m, 125_000_000m, 26m),       // 26%
        (125_000_001m, 157_000_000m, 27m),       // 27%
        (157_000_001m, 206_000_000m, 28m),       // 28%
        (206_000_001m, 337_000_000m, 29m),       // 29%
        (337_000_001m, 454_000_000m, 30m),       // 30%
        (454_000_001m, 550_000_000m, 31m),       // 31%
        (550_000_001m, 695_000_000m, 32m),       // 32%
        (695_000_001m, 910_000_000m, 33m),       // 33%
        (910_000_001m, decimal.MaxValue, 34m)    // 34%
    };

    /// <summary>
    /// Multiplier untuk karyawan tanpa NPWP (tarif lebih tinggi 20%)
    /// </summary>
    public const decimal NoNpwpMultiplier = 1.2m;

    /// <summary>
    /// Get PTKP amount berdasarkan status
    /// </summary>
    public static decimal GetPtkp(string ptkpStatus)
    {
        if (PtkpAnnual.TryGetValue(ptkpStatus, out var amount))
            return amount;

        // Default ke TK/0 jika status tidak valid
        return PtkpAnnual["TK/0"];
    }

    /// <summary>
    /// Get TER rate berdasarkan penghasilan bruto per bulan
    /// </summary>
    public static decimal GetTerRate(decimal monthlyGrossIncome)
    {
        foreach (var bracket in TerBrackets)
        {
            if (monthlyGrossIncome >= bracket.Min && monthlyGrossIncome <= bracket.Max)
                return bracket.Rate;
        }

        // Jika di atas bracket tertinggi, return rate tertinggi
        return TerBrackets[^1].Rate;
    }
}

// Made with Bob
