namespace PayrollApp.Application.Common;

public record SalaryComponentDto(
    Guid ComponentId,
    string Name,
    decimal Amount,
    string Type,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo
);

// Made with Bob