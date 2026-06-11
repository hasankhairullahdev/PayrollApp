using PayrollApp.Domain.ValueObjects;
using PayrollApp.Engine;
using Shouldly;

namespace PayrollApp.Tests.Engine;

public class ProrateCalculatorTests
{
    [Fact]
    public void CalculateForJoin_JoinAtStartOfMonth_ShouldReturnFullSalary()
    {
        // Arrange
        var fullSalary = new Money(10_000_000m);
        var joinDate = new DateOnly(2024, 7, 1); // Join tanggal 1
        var periodMonth = 7;
        var periodYear = 2024;

        // Act
        var result = ProrateCalculator.CalculateForJoin(fullSalary, joinDate, periodMonth, periodYear);

        // Assert
        result.ShouldBe(fullSalary);
    }

    [Fact]
    public void CalculateForJoin_JoinInMiddleOfMonth_ShouldProrateCorrectly()
    {
        // Arrange
        var fullSalary = new Money(10_000_000m);
        var joinDate = new DateOnly(2024, 7, 15); // Join tanggal 15
        var periodMonth = 7;
        var periodYear = 2024;

        // Act
        var result = ProrateCalculator.CalculateForJoin(fullSalary, joinDate, periodMonth, periodYear);

        // Assert
        result.Amount.ShouldBeLessThan(fullSalary.Amount);
        result.Amount.ShouldBeGreaterThan(0);
        
        // Should be roughly half (join mid-month)
        result.Amount.ShouldBeInRange(4_000_000m, 6_000_000m);
    }

    [Fact]
    public void CalculateForJoin_JoinAfterPeriod_ShouldReturnZero()
    {
        // Arrange
        var fullSalary = new Money(10_000_000m);
        var joinDate = new DateOnly(2024, 8, 1); // Join bulan depan
        var periodMonth = 7;
        var periodYear = 2024;

        // Act
        var result = ProrateCalculator.CalculateForJoin(fullSalary, joinDate, periodMonth, periodYear);

        // Assert
        result.ShouldBe(Money.Zero);
    }

    [Fact]
    public void CalculateForResign_ResignAtEndOfMonth_ShouldReturnFullSalary()
    {
        // Arrange
        var fullSalary = new Money(10_000_000m);
        var resignDate = new DateOnly(2024, 7, 31); // Resign akhir bulan
        var periodMonth = 7;
        var periodYear = 2024;

        // Act
        var result = ProrateCalculator.CalculateForResign(fullSalary, resignDate, periodMonth, periodYear);

        // Assert
        result.ShouldBe(fullSalary);
    }

    [Fact]
    public void CalculateForResign_ResignInMiddleOfMonth_ShouldProrateCorrectly()
    {
        // Arrange
        var fullSalary = new Money(10_000_000m);
        var resignDate = new DateOnly(2024, 7, 15); // Resign tanggal 15
        var periodMonth = 7;
        var periodYear = 2024;

        // Act
        var result = ProrateCalculator.CalculateForResign(fullSalary, resignDate, periodMonth, periodYear);

        // Assert
        result.Amount.ShouldBeLessThan(fullSalary.Amount);
        result.Amount.ShouldBeGreaterThan(0);
        
        // Should be roughly half (resign mid-month)
        result.Amount.ShouldBeInRange(4_000_000m, 6_000_000m);
    }

    [Fact]
    public void CalculateForResign_ResignBeforePeriod_ShouldReturnZero()
    {
        // Arrange
        var fullSalary = new Money(10_000_000m);
        var resignDate = new DateOnly(2024, 6, 30); // Resign bulan lalu
        var periodMonth = 7;
        var periodYear = 2024;

        // Act
        var result = ProrateCalculator.CalculateForResign(fullSalary, resignDate, periodMonth, periodYear);

        // Assert
        result.ShouldBe(Money.Zero);
    }

    [Fact]
    public void CountWorkingDays_FullMonth_ShouldExcludeWeekends()
    {
        // Arrange
        var startDate = new DateOnly(2024, 7, 1);
        var endDate = new DateOnly(2024, 7, 31);

        // Act
        var workingDays = ProrateCalculator.CountWorkingDays(startDate, endDate);

        // Assert
        // July 2024 has 31 days, should have around 22-23 working days
        workingDays.ShouldBeInRange(20, 24);
    }

    [Fact]
    public void CountWorkingDays_SingleDay_ShouldReturnOne()
    {
        // Arrange
        var date = new DateOnly(2024, 7, 1); // Monday

        // Act
        var workingDays = ProrateCalculator.CountWorkingDays(date, date);

        // Assert
        workingDays.ShouldBe(1);
    }

    [Fact]
    public void CountWorkingDays_Weekend_ShouldReturnZero()
    {
        // Arrange
        var saturday = new DateOnly(2024, 7, 6);
        var sunday = new DateOnly(2024, 7, 7);

        // Act
        var saturdayCount = ProrateCalculator.CountWorkingDays(saturday, saturday);
        var sundayCount = ProrateCalculator.CountWorkingDays(sunday, sunday);

        // Assert
        saturdayCount.ShouldBe(0);
        sundayCount.ShouldBe(0);
    }

    [Fact]
    public void CountWorkingDaysWithHolidays_ShouldExcludeHolidays()
    {
        // Arrange
        var startDate = new DateOnly(2024, 8, 1);
        var endDate = new DateOnly(2024, 8, 31);
        var holidays = new List<DateOnly>
        {
            new DateOnly(2024, 8, 19) // Monday holiday (not weekend)
        };

        // Act
        var workingDays = ProrateCalculator.CountWorkingDaysWithHolidays(startDate, endDate, holidays);
        var workingDaysWithoutHolidays = ProrateCalculator.CountWorkingDays(startDate, endDate);

        // Assert
        workingDays.ShouldBe(workingDaysWithoutHolidays - 1);
    }

    [Fact]
    public void GetProratePercentage_FullMonth_ShouldReturn100Percent()
    {
        // Arrange
        var startDate = new DateOnly(2024, 7, 1);
        var endDate = new DateOnly(2024, 7, 31);
        var periodMonth = 7;
        var periodYear = 2024;

        // Act
        var percentage = ProrateCalculator.GetProratePercentage(startDate, endDate, periodMonth, periodYear);

        // Assert
        percentage.ShouldBe(100m);
    }

    [Fact]
    public void GetProratePercentage_HalfMonth_ShouldReturnApproximately50Percent()
    {
        // Arrange
        var startDate = new DateOnly(2024, 7, 15);
        var endDate = new DateOnly(2024, 7, 31);
        var periodMonth = 7;
        var periodYear = 2024;

        // Act
        var percentage = ProrateCalculator.GetProratePercentage(startDate, endDate, periodMonth, periodYear);

        // Assert
        percentage.ShouldBeInRange(40m, 60m); // Roughly half
    }

    [Fact]
    public void ProrateComponent_ShouldCalculateCorrectly()
    {
        // Arrange
        var componentAmount = new Money(1_000_000m);
        var workedDays = 10;
        var totalDays = 20;

        // Act
        var result = ProrateCalculator.ProrateComponent(componentAmount, workedDays, totalDays);

        // Assert
        result.Amount.ShouldBe(500_000m); // 50%
    }

    [Fact]
    public void Calculate_WithCustomDateRange_ShouldProrateCorrectly()
    {
        // Arrange
        var fullSalary = new Money(10_000_000m);
        var startDate = new DateOnly(2024, 7, 10);
        var endDate = new DateOnly(2024, 7, 20);
        var periodMonth = 7;
        var periodYear = 2024;

        // Act
        var result = ProrateCalculator.Calculate(fullSalary, startDate, endDate, periodMonth, periodYear);

        // Assert
        result.Amount.ShouldBeLessThan(fullSalary.Amount);
        result.Amount.ShouldBeGreaterThan(0);
    }
}

// Made with Bob
