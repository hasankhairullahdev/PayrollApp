using PayrollApp.Domain.ValueObjects;

namespace PayrollApp.Domain.Aggregates;

public class Employee
{
    public Guid Id { get; internal set; }
    public string EmployeeCode { get; internal set; }
    public string FullName { get; internal set; }
    public string Email { get; internal set; }
    public string? Npwp { get; internal set; }
    public string PtkpStatus { get; internal set; } // TK/0, TK/1, K/0, K/1, K/2, K/3
    public DateOnly JoinDate { get; internal set; }
    public DateOnly? ResignDate { get; internal set; }
    public bool IsActive { get; internal set; }
    private List<SalaryComponent> _salaryComponents = new();
    public List<SalaryComponent> SalaryComponents
    {
        get => _salaryComponents;
        set => _salaryComponents = value ?? new List<SalaryComponent>();
    }

    private Employee()
    {
        EmployeeCode = null!;
        FullName = null!;
        Email = null!;
        PtkpStatus = null!;
        _salaryComponents = new List<SalaryComponent>();
    }

    public Employee(
        Guid id,
        string employeeCode,
        string fullName,
        string email,
        string? npwp,
        string ptkpStatus,
        DateOnly joinDate)
    {
        if (string.IsNullOrWhiteSpace(employeeCode))
            throw new ArgumentException("Employee code cannot be empty", nameof(employeeCode));

        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name cannot be empty", nameof(fullName));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        if (string.IsNullOrWhiteSpace(ptkpStatus))
            throw new ArgumentException("PTKP status cannot be empty", nameof(ptkpStatus));

        Id = id;
        EmployeeCode = employeeCode;
        FullName = fullName;
        Email = email;
        Npwp = npwp;
        PtkpStatus = ptkpStatus;
        JoinDate = joinDate;
        IsActive = true;
        SalaryComponents = new List<SalaryComponent>();
    }

    public bool HasNpwp => !string.IsNullOrWhiteSpace(Npwp);

    public void AddSalaryComponent(SalaryComponent component)
    {
        if (component == null)
            throw new ArgumentNullException(nameof(component));

        SalaryComponents.Add(component);
    }

    public void RemoveSalaryComponent(Guid componentId)
    {
        var component = SalaryComponents.FirstOrDefault(c => c.ComponentId == componentId);
        if (component != null)
        {
            SalaryComponents.Remove(component);
        }
    }

    public void UpdateBasicInfo(string fullName, string email, string? npwp)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name cannot be empty", nameof(fullName));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        FullName = fullName;
        Email = email;
        Npwp = npwp;
    }

    public void UpdatePtkpStatus(string newPtkpStatus)
    {
        if (string.IsNullOrWhiteSpace(newPtkpStatus))
            throw new ArgumentException("PTKP status cannot be empty", nameof(newPtkpStatus));

        PtkpStatus = newPtkpStatus;
    }

    public void UpdateSalaryComponents(List<SalaryComponent> newComponents)
    {
        if (newComponents == null)
            throw new ArgumentNullException(nameof(newComponents));

        // Clear existing components
        SalaryComponents.Clear();

        // Add new components
        foreach (var component in newComponents)
        {
            SalaryComponents.Add(component);
        }
    }

    public void Resign(DateOnly resignDate)
    {
        if (resignDate < JoinDate)
            throw new ArgumentException("Resign date cannot be before join date", nameof(resignDate));

        ResignDate = resignDate;
        IsActive = false;
    }

    public IEnumerable<SalaryComponent> GetActiveComponents(DateOnly period)
    {
        return SalaryComponents.Where(c => c.IsActiveFor(period));
    }

    public Money GetBasicSalary(DateOnly period)
    {
        var basicSalaryComponent = GetActiveComponents(period)
            .FirstOrDefault(c => c.Type == Enums.SalaryComponentType.BasicSalary);

        return basicSalaryComponent?.Amount ?? Money.Zero;
    }

    public Money GetTotalAllowances(DateOnly period)
    {
        var allowances = GetActiveComponents(period)
            .Where(c => c.Type == Enums.SalaryComponentType.FixedAllowance || 
                       c.Type == Enums.SalaryComponentType.VariableAllowance)
            .Select(c => c.Amount);

        return allowances.Aggregate(Money.Zero, (sum, amount) => sum + amount);
    }

    public Money GetTotalDeductions(DateOnly period)
    {
        var deductions = GetActiveComponents(period)
            .Where(c => c.Type == Enums.SalaryComponentType.Deduction)
            .Select(c => c.Amount);

        return deductions.Aggregate(Money.Zero, (sum, amount) => sum + amount);
    }
}

// Made with Bob
