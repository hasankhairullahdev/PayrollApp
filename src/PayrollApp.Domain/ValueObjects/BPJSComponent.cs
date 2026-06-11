namespace PayrollApp.Domain.ValueObjects;

public record BPJSComponent
{
    // JHT - Jaminan Hari Tua
    public Money JhtEmployee { get; init; }
    public Money JhtEmployer { get; init; }

    // JP - Jaminan Pensiun
    public Money JpEmployee { get; init; }
    public Money JpEmployer { get; init; }

    // JKK - Jaminan Kecelakaan Kerja (employer only)
    public Money JkkEmployer { get; init; }

    // JKM - Jaminan Kematian (employer only)
    public Money JkmEmployer { get; init; }

    // Kesehatan
    public Money KesehatanEmployee { get; init; }
    public Money KesehatanEmployer { get; init; }

    public BPJSComponent(
        Money jhtEmployee,
        Money jhtEmployer,
        Money jpEmployee,
        Money jpEmployer,
        Money jkkEmployer,
        Money jkmEmployer,
        Money kesehatanEmployee,
        Money kesehatanEmployer)
    {
        JhtEmployee = jhtEmployee;
        JhtEmployer = jhtEmployer;
        JpEmployee = jpEmployee;
        JpEmployer = jpEmployer;
        JkkEmployer = jkkEmployer;
        JkmEmployer = jkmEmployer;
        KesehatanEmployee = kesehatanEmployee;
        KesehatanEmployer = kesehatanEmployer;
    }

    public Money TotalEmployeeContribution =>
        JhtEmployee + JpEmployee + KesehatanEmployee;

    public Money TotalEmployerContribution =>
        JhtEmployer + JpEmployer + JkkEmployer + JkmEmployer + KesehatanEmployer;

    public Money TotalContribution =>
        TotalEmployeeContribution + TotalEmployerContribution;

    public static BPJSComponent Zero => new(
        Money.Zero,
        Money.Zero,
        Money.Zero,
        Money.Zero,
        Money.Zero,
        Money.Zero,
        Money.Zero,
        Money.Zero
    );
}

// Made with Bob
