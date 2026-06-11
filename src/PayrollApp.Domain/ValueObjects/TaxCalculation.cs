namespace PayrollApp.Domain.ValueObjects;

public record TaxCalculation
{
    public Money GrossAmount { get; init; }
    public Money Pph21Amount { get; init; }
    public Money NetAmount { get; init; }
    public string PtkpStatus { get; init; }
    public bool HasNpwp { get; init; }

    public TaxCalculation(
        Money grossAmount,
        Money pph21Amount,
        string ptkpStatus,
        bool hasNpwp)
    {
        GrossAmount = grossAmount;
        Pph21Amount = pph21Amount;
        NetAmount = grossAmount - pph21Amount;
        PtkpStatus = ptkpStatus;
        HasNpwp = hasNpwp;
    }

    public static TaxCalculation Zero => new(
        Money.Zero,
        Money.Zero,
        "TK/0",
        true
    );
}

// Made with Bob
