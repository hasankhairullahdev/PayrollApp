namespace PayrollApp.Domain.ValueObjects;

public record EmployeeId
{
    public Guid Value { get; init; }

    public EmployeeId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("EmployeeId cannot be empty", nameof(value));
        
        Value = value;
    }

    public static EmployeeId New() => new(Guid.NewGuid());

    public static EmployeeId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(EmployeeId employeeId) => employeeId.Value;
}

// Made with Bob
