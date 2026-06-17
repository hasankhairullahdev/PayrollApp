using PayrollApp.Domain.Enums;

namespace PayrollApp.Domain.ValueObjects;

public record SalaryComponent
{
    public Guid ComponentId { get; init; }
    public string Name { get; init; } = null!;
    public Money Amount { get; init; } = null!;
    public SalaryComponentType Type { get; init; }
    public DateOnly EffectiveDate { get; init; }

    // Parameterless constructor for Marten deserialization
    public SalaryComponent()
    {
    }

    public SalaryComponent(
        Guid componentId,
        string name,
        Money amount,
        SalaryComponentType type,
        DateOnly effectiveDate)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Component name cannot be empty", nameof(name));

        ComponentId = componentId;
        Name = name;
        Amount = amount;
        Type = type;
        EffectiveDate = effectiveDate;
    }

    public bool IsActiveFor(DateOnly period) => EffectiveDate <= period;
}

// Made with Bob
