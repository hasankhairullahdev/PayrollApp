using PayrollApp.Domain.ValueObjects;

namespace PayrollApp.Domain.Events;

public record PayslipGenerated
{
    public Guid PayrollRunId { get; init; }
    public EmployeeId EmployeeId { get; init; }
    public string FilePath { get; init; }
    public DateTime GeneratedAt { get; init; }

    public PayslipGenerated(
        Guid payrollRunId,
        EmployeeId employeeId,
        string filePath,
        DateTime generatedAt)
    {
        PayrollRunId = payrollRunId;
        EmployeeId = employeeId;
        FilePath = filePath;
        GeneratedAt = generatedAt;
    }
}

// Made with Bob
