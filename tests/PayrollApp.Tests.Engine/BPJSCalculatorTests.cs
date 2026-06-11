using PayrollApp.Domain.ValueObjects;
using PayrollApp.Engine;
using Shouldly;

namespace PayrollApp.Tests.Engine;

public class BPJSCalculatorTests
{
    [Fact]
    public void Calculate_WithNormalSalary_ShouldCalculateAllComponents()
    {
        // Arrange
        var salary = new Money(10_000_000m); // 10 juta

        // Act
        var result = BPJSCalculator.Calculate(salary);

        // Assert
        result.JhtEmployee.Amount.ShouldBe(200_000m); // 2%
        result.JhtEmployer.Amount.ShouldBe(370_000m); // 3.7%
        result.JpEmployee.Amount.ShouldBe(100_000m); // 1%
        result.JpEmployer.Amount.ShouldBe(200_000m); // 2%
        result.JkkEmployer.Amount.ShouldBe(24_000m); // 0.24%
        result.JkmEmployer.Amount.ShouldBe(30_000m); // 0.3%
        result.KesehatanEmployee.Amount.ShouldBe(100_000m); // 1%
        result.KesehatanEmployer.Amount.ShouldBe(400_000m); // 4%
    }

    [Fact]
    public void Calculate_WithSalaryAboveKesehatanCap_ShouldCapKesehatan()
    {
        // Arrange
        var salary = new Money(20_000_000m); // 20 juta (above 12 juta cap)

        // Act
        var result = BPJSCalculator.Calculate(salary);

        // Assert
        // Kesehatan should be capped at 12 juta
        result.KesehatanEmployee.Amount.ShouldBe(120_000m); // 1% of 12jt
        result.KesehatanEmployer.Amount.ShouldBe(480_000m); // 4% of 12jt
        
        // JHT should not be capped
        result.JhtEmployee.Amount.ShouldBe(400_000m); // 2% of 20jt
        result.JhtEmployer.Amount.ShouldBe(740_000m); // 3.7% of 20jt
    }

    [Fact]
    public void Calculate_WithSalaryAboveJpCap_ShouldCapJp()
    {
        // Arrange
        var salary = new Money(15_000_000m); // 15 juta (above ~10 juta JP cap)

        // Act
        var result = BPJSCalculator.Calculate(salary);

        // Assert
        // JP should be capped at ~10.042.300
        result.JpEmployee.Amount.ShouldBe(100_423m); // 1% of cap
        result.JpEmployer.Amount.ShouldBe(200_846m); // 2% of cap
    }

    [Fact]
    public void Calculate_WithZeroSalary_ShouldReturnZero()
    {
        // Arrange
        var salary = Money.Zero;

        // Act
        var result = BPJSCalculator.Calculate(salary);

        // Assert
        result.ShouldBe(BPJSComponent.Zero);
        result.TotalEmployeeContribution.ShouldBe(Money.Zero);
        result.TotalEmployerContribution.ShouldBe(Money.Zero);
    }

    [Fact]
    public void Calculate_TotalContributions_ShouldSumCorrectly()
    {
        // Arrange
        var salary = new Money(10_000_000m);

        // Act
        var result = BPJSCalculator.Calculate(salary);

        // Assert
        var expectedEmployee = result.JhtEmployee + result.JpEmployee + result.KesehatanEmployee;
        var expectedEmployer = result.JhtEmployer + result.JpEmployer + 
                              result.JkkEmployer + result.JkmEmployer + result.KesehatanEmployer;

        result.TotalEmployeeContribution.ShouldBe(expectedEmployee);
        result.TotalEmployerContribution.ShouldBe(expectedEmployer);
        result.TotalContribution.ShouldBe(expectedEmployee + expectedEmployer);
    }

    [Fact]
    public void CalculateKesehatan_ShouldOnlyCalculateKesehatan()
    {
        // Arrange
        var salary = new Money(10_000_000m);

        // Act
        var (employee, employer) = BPJSCalculator.CalculateKesehatan(salary);

        // Assert
        employee.Amount.ShouldBe(100_000m); // 1%
        employer.Amount.ShouldBe(400_000m); // 4%
    }

    [Fact]
    public void CalculateKetenagakerjaan_ShouldExcludeKesehatan()
    {
        // Arrange
        var salary = new Money(10_000_000m);

        // Act
        var result = BPJSCalculator.CalculateKetenagakerjaan(salary);

        // Assert
        result.KesehatanEmployee.ShouldBe(Money.Zero);
        result.KesehatanEmployer.ShouldBe(Money.Zero);
        result.JhtEmployee.Amount.ShouldBeGreaterThan(0);
        result.JpEmployee.Amount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void GetEmployeeContribution_ShouldReturnCorrectTotal()
    {
        // Arrange
        var salary = new Money(10_000_000m);

        // Act
        var contribution = BPJSCalculator.GetEmployeeContribution(salary);

        // Assert
        // JHT (2%) + JP (1%) + Kesehatan (1%) = 4%
        contribution.Amount.ShouldBe(400_000m);
    }

    [Fact]
    public void GetEmployerContribution_ShouldReturnCorrectTotal()
    {
        // Arrange
        var salary = new Money(10_000_000m);

        // Act
        var contribution = BPJSCalculator.GetEmployerContribution(salary);

        // Assert
        // JHT (3.7%) + JP (2%) + JKK (0.24%) + JKM (0.3%) + Kesehatan (4%) = 10.24%
        contribution.Amount.ShouldBe(1_024_000m);
    }

    [Fact]
    public void CalculateWithCustomJkk_ShouldUseCustomRate()
    {
        // Arrange
        var salary = new Money(10_000_000m);
        var customJkkRate = 0.0054m; // 0.54% (medium risk)

        // Act
        var result = BPJSCalculator.CalculateWithCustomJkk(salary, customJkkRate);

        // Assert
        result.JkkEmployer.Amount.ShouldBe(54_000m); // 0.54% of 10jt
    }

    [Fact]
    public void Calculate_WithMinimumWage_ShouldCalculateCorrectly()
    {
        // Arrange
        var minimumWage = new Money(4_900_000m); // UMR Jakarta 2024

        // Act
        var result = BPJSCalculator.Calculate(minimumWage);

        // Assert
        result.TotalEmployeeContribution.Amount.ShouldBeGreaterThan(0);
        result.TotalEmployerContribution.Amount.ShouldBeGreaterThan(0);
        
        // Employee contribution should be around 4% of salary
        var employeePercentage = result.TotalEmployeeContribution.Amount / minimumWage.Amount * 100;
        employeePercentage.ShouldBe(4m, 0.1m); // Allow 0.1% tolerance
    }
}

// Made with Bob
