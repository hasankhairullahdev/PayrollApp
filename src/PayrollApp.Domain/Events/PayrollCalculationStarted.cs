namespace PayrollApp.Domain.Events;

/// <summary>
/// Event yang di-raise saat calculation dimulai (status berubah ke Calculating)
/// </summary>
public record PayrollCalculationStarted(
    Guid PayrollRunId,
    DateTime StartedAt
);

// Made with Bob