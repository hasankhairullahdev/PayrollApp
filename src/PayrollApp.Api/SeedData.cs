using Marten;
using PayrollApp.Domain.Aggregates;
using PayrollApp.Domain.Enums;
using PayrollApp.Domain.ValueObjects;
using PayrollApp.Infrastructure.Security;

namespace PayrollApp.Api;

public static class SeedData
{
    public static async Task SeedUsersAsync(IDocumentStore documentStore, IPasswordHasher passwordHasher)
    {
        await using var session = documentStore.LightweightSession();
        
        // Check if users already exist
        var existingUsers = await session.Query<User>().AnyAsync();
        if (existingUsers)
        {
            Console.WriteLine("Users already seeded. Skipping...");
            return;
        }
        
        Console.WriteLine("Seeding initial users...");
        
        // Admin user
        var admin = User.Register(
            "admin@payroll.com",
            passwordHasher.HashPassword("Admin123!"),
            "System Administrator",
            UserRole.Admin
        );
        session.Events.Append(admin.Id, admin.GetUncommittedEvents().ToArray());
        
        // HR user
        var hr = User.Register(
            "hr@payroll.com",
            passwordHasher.HashPassword("HR123!"),
            "HR Manager",
            UserRole.HR
        );
        session.Events.Append(hr.Id, hr.GetUncommittedEvents().ToArray());
        
        // Finance user
        var finance = User.Register(
            "finance@payroll.com",
            passwordHasher.HashPassword("Finance123!"),
            "Finance Manager",
            UserRole.Finance
        );
        session.Events.Append(finance.Id, finance.GetUncommittedEvents().ToArray());
        
        await session.SaveChangesAsync();
        
        Console.WriteLine("✓ Seeded 3 users successfully");
        Console.WriteLine("  - admin@payroll.com / Admin123!");
        Console.WriteLine("  - hr@payroll.com / HR123!");
        Console.WriteLine("  - finance@payroll.com / Finance123!");
    }
    
    public static async Task SeedEmployeesAsync(IDocumentStore documentStore)
    {
        await using var session = documentStore.LightweightSession();
        
        // Check if employees already exist
        var existingEmployees = await session.Query<Employee>().AnyAsync();
        if (existingEmployees)
        {
            Console.WriteLine("Employees already seeded. Skipping...");
            return;
        }
        
        Console.WriteLine("Seeding initial employees...");
        
        // Employee 1: Manager - High salary, married with 2 kids, has NPWP
        var emp1 = new Employee(
            Guid.NewGuid(),
            "EMP-001",
            "Budi Santoso",
            "budi.santoso@company.com",
            "123456789012345", // NPWP
            "K/2", // Married with 2 kids
            new DateOnly(2020, 1, 1)
        );
        emp1.AddSalaryComponent(new SalaryComponent(
            Guid.NewGuid(),
            "Basic Salary",
            new Money(15_000_000m),
            SalaryComponentType.BasicSalary,
            new DateOnly(2020, 1, 1)
        ));
        emp1.AddSalaryComponent(new SalaryComponent(
            Guid.NewGuid(),
            "Tunjangan Jabatan",
            new Money(3_000_000m),
            SalaryComponentType.FixedAllowance,
            new DateOnly(2020, 1, 1)
        ));
        emp1.AddSalaryComponent(new SalaryComponent(
            Guid.NewGuid(),
            "Tunjangan Transport",
            new Money(1_500_000m),
            SalaryComponentType.FixedAllowance,
            new DateOnly(2020, 1, 1)
        ));
        session.Store(emp1);
        
        // Employee 2: Staff - Medium salary, single, has NPWP
        var emp2 = new Employee(
            Guid.NewGuid(),
            "EMP-002",
            "Siti Nurhaliza",
            "siti.nurhaliza@company.com",
            "987654321098765", // NPWP
            "TK/0", // Single
            new DateOnly(2022, 6, 15)
        );
        emp2.AddSalaryComponent(new SalaryComponent(
            Guid.NewGuid(),
            "Basic Salary",
            new Money(8_000_000m),
            SalaryComponentType.BasicSalary,
            new DateOnly(2022, 6, 15)
        ));
        emp2.AddSalaryComponent(new SalaryComponent(
            Guid.NewGuid(),
            "Tunjangan Transport",
            new Money(1_000_000m),
            SalaryComponentType.FixedAllowance,
            new DateOnly(2022, 6, 15)
        ));
        emp2.AddSalaryComponent(new SalaryComponent(
            Guid.NewGuid(),
            "Tunjangan Makan",
            new Money(500_000m),
            SalaryComponentType.FixedAllowance,
            new DateOnly(2022, 6, 15)
        ));
        session.Store(emp2);
        
        // Employee 3: Junior - Low salary, married, NO NPWP (higher tax)
        var emp3 = new Employee(
            Guid.NewGuid(),
            "EMP-003",
            "Ahmad Hidayat",
            "ahmad.hidayat@company.com",
            null, // NO NPWP - will get 20% higher tax
            "K/0", // Married, no kids
            new DateOnly(2023, 3, 1)
        );
        emp3.AddSalaryComponent(new SalaryComponent(
            Guid.NewGuid(),
            "Basic Salary",
            new Money(5_000_000m),
            SalaryComponentType.BasicSalary,
            new DateOnly(2023, 3, 1)
        ));
        emp3.AddSalaryComponent(new SalaryComponent(
            Guid.NewGuid(),
            "Tunjangan Transport",
            new Money(500_000m),
            SalaryComponentType.FixedAllowance,
            new DateOnly(2023, 3, 1)
        ));
        session.Store(emp3);
        
        // Employee 4: Senior - Above BPJS cap, married with 1 kid
        var emp4 = new Employee(
            Guid.NewGuid(),
            "EMP-004",
            "Dewi Lestari",
            "dewi.lestari@company.com",
            "456789012345678", // NPWP
            "K/1", // Married with 1 kid
            new DateOnly(2019, 8, 1)
        );
        emp4.AddSalaryComponent(new SalaryComponent(
            Guid.NewGuid(),
            "Basic Salary",
            new Money(20_000_000m), // Above BPJS cap
            SalaryComponentType.BasicSalary,
            new DateOnly(2019, 8, 1)
        ));
        emp4.AddSalaryComponent(new SalaryComponent(
            Guid.NewGuid(),
            "Tunjangan Jabatan",
            new Money(5_000_000m),
            SalaryComponentType.FixedAllowance,
            new DateOnly(2019, 8, 1)
        ));
        emp4.AddSalaryComponent(new SalaryComponent(
            Guid.NewGuid(),
            "Tunjangan Transport",
            new Money(2_000_000m),
            SalaryComponentType.FixedAllowance,
            new DateOnly(2019, 8, 1)
        ));
        session.Store(emp4);
        
        await session.SaveChangesAsync();
        
        Console.WriteLine("✓ Seeded 4 employees successfully");
    }
}

// Made with Bob