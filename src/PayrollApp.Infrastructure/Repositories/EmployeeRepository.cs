using Marten;
using PayrollApp.Domain.Aggregates;

namespace PayrollApp.Infrastructure.Repositories;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Employee?> GetByCodeAsync(string employeeCode, CancellationToken cancellationToken = default);
    Task<List<Employee>> GetActiveEmployeesAsync(CancellationToken cancellationToken = default);
    Task<List<Employee>> GetActiveEmployeesForPeriodAsync(int month, int year, CancellationToken cancellationToken = default);
    Task<(List<Employee> Employees, int TotalCount)> GetEmployeesAsync(bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default);
    Task SaveAsync(Employee employee, CancellationToken cancellationToken = default);
    Task UpdateAsync(Employee employee, CancellationToken cancellationToken = default);
}

public class EmployeeRepository : IEmployeeRepository
{
    private readonly IDocumentStore _documentStore;

    public EmployeeRepository(IDocumentStore documentStore)
    {
        _documentStore = documentStore;
    }

    public async Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var session = _documentStore.LightweightSession();
        return await session.LoadAsync<Employee>(id, cancellationToken);
    }

    public async Task<Employee?> GetByCodeAsync(string employeeCode, CancellationToken cancellationToken = default)
    {
        await using var session = _documentStore.QuerySession();
        return await session.Query<Employee>()
            .FirstOrDefaultAsync(e => e.EmployeeCode == employeeCode, cancellationToken);
    }

    public async Task<List<Employee>> GetActiveEmployeesAsync(CancellationToken cancellationToken = default)
    {
        await using var session = _documentStore.QuerySession();
        var result = await session.Query<Employee>()
            .Where(e => e.IsActive)
            .ToListAsync(cancellationToken);
        return result.ToList();
    }

    public async Task<List<Employee>> GetActiveEmployeesForPeriodAsync(int month, int year, CancellationToken cancellationToken = default)
    {
        await using var session = _documentStore.QuerySession();
        var periodDate = new DateOnly(year, month, 1);
        
        var result = await session.Query<Employee>()
            .Where(e => e.IsActive && e.JoinDate <= periodDate)
            .ToListAsync(cancellationToken);
        return result.ToList();
    }

    public async Task<(List<Employee> Employees, int TotalCount)> GetEmployeesAsync(bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        await using var session = _documentStore.QuerySession();
        
        // Build query
        var allEmployees = await session.Query<Employee>().ToListAsync(cancellationToken);
        
        // Filter in memory
        var filtered = isActive.HasValue
            ? allEmployees.Where(e => e.IsActive == isActive.Value).ToList()
            : allEmployees;
        
        var totalCount = filtered.Count;
        
        // Paginate
        var result = filtered
            .OrderBy(e => e.EmployeeCode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return (result, totalCount);
    }

    public async Task SaveAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        await using var session = _documentStore.LightweightSession();
        session.Store(employee);
        await session.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        await using var session = _documentStore.LightweightSession();
        session.Update(employee);
        await session.SaveChangesAsync(cancellationToken);
    }
}

// Made with Bob