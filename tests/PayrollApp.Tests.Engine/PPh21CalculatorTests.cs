using PayrollApp.Domain.ValueObjects;
using PayrollApp.Engine;
using Shouldly;

namespace PayrollApp.Tests.Engine;

public class PPh21CalculatorTests
{
    [Fact]
    public void Calculate_WithLowIncome_ShouldReturnZeroTax()
    {
        // Arrange
        var monthlyIncome = new Money(5_000_000m); // 5 juta per bulan
        var ptkpStatus = "TK/0";
        var hasNpwp = true;

        // Act
        var result = PPh21Calculator.Calculate(monthlyIncome, ptkpStatus, hasNpwp);

        // Assert
        result.Pph21Amount.Amount.ShouldBe(0m);
        result.GrossAmount.ShouldBe(monthlyIncome);
        result.NetAmount.ShouldBe(monthlyIncome);
    }

    [Fact]
    public void Calculate_WithMediumIncome_ShouldCalculateCorrectTax()
    {
        // Arrange
        var monthlyIncome = new Money(10_000_000m); // 10 juta per bulan
        var ptkpStatus = "TK/0";
        var hasNpwp = true;

        // Act
        var result = PPh21Calculator.Calculate(monthlyIncome, ptkpStatus, hasNpwp);

        // Assert
        result.Pph21Amount.Amount.ShouldBeGreaterThan(0m);
        result.NetAmount.Amount.ShouldBeLessThan(monthlyIncome.Amount);
        result.NetAmount.ShouldBe(monthlyIncome - result.Pph21Amount);
    }

    [Fact]
    public void Calculate_WithoutNpwp_ShouldApply20PercentSurcharge()
    {
        // Arrange
        var monthlyIncome = new Money(10_000_000m);
        var ptkpStatus = "TK/0";

        // Act
        var withNpwp = PPh21Calculator.Calculate(monthlyIncome, ptkpStatus, true);
        var withoutNpwp = PPh21Calculator.Calculate(monthlyIncome, ptkpStatus, false);

        // Assert
        withoutNpwp.Pph21Amount.Amount.ShouldBeGreaterThan(withNpwp.Pph21Amount.Amount);
        
        // Should be approximately 20% higher
        var expectedWithoutNpwp = withNpwp.Pph21Amount.Amount * 1.2m;
        Math.Abs(withoutNpwp.Pph21Amount.Amount - expectedWithoutNpwp).ShouldBeLessThan(10m); // Allow small rounding difference
    }

    [Fact]
    public void Calculate_WithHighIncome_ShouldApplyHigherRate()
    {
        // Arrange
        var lowIncome = new Money(10_000_000m);
        var highIncome = new Money(50_000_000m);
        var ptkpStatus = "TK/0";
        var hasNpwp = true;

        // Act
        var lowResult = PPh21Calculator.Calculate(lowIncome, ptkpStatus, hasNpwp);
        var highResult = PPh21Calculator.Calculate(highIncome, ptkpStatus, hasNpwp);

        // Assert
        var lowRate = lowResult.Pph21Amount.Amount / lowIncome.Amount * 100;
        var highRate = highResult.Pph21Amount.Amount / highIncome.Amount * 100;
        
        highRate.ShouldBeGreaterThan(lowRate);
    }

    [Fact]
    public void Calculate_WithZeroIncome_ShouldReturnZeroTax()
    {
        // Arrange
        var monthlyIncome = Money.Zero;
        var ptkpStatus = "TK/0";
        var hasNpwp = true;

        // Act
        var result = PPh21Calculator.Calculate(monthlyIncome, ptkpStatus, hasNpwp);

        // Assert
        result.Pph21Amount.ShouldBe(Money.Zero);
        result.NetAmount.ShouldBe(Money.Zero);
    }

    [Fact]
    public void CalculateAnnual_WithIncomeBelowPtkp_ShouldReturnZeroTax()
    {
        // Arrange
        var annualIncome = new Money(50_000_000m); // 50 juta per tahun
        var ptkpStatus = "TK/0"; // PTKP 54 juta
        var hasNpwp = true;

        // Act
        var result = PPh21Calculator.CalculateAnnual(annualIncome, ptkpStatus, hasNpwp);

        // Assert
        result.Pph21Amount.Amount.ShouldBe(0m);
    }

    [Fact]
    public void CalculateAnnual_WithIncomeAbovePtkp_ShouldCalculateProgressiveTax()
    {
        // Arrange
        var annualIncome = new Money(100_000_000m); // 100 juta per tahun
        var ptkpStatus = "TK/0"; // PTKP 54 juta
        var hasNpwp = true;

        // Act
        var result = PPh21Calculator.CalculateAnnual(annualIncome, ptkpStatus, hasNpwp);

        // Assert
        result.Pph21Amount.Amount.ShouldBeGreaterThan(0m);
        
        // PKP = 100jt - 54jt = 46jt
        // Tax = 46jt × 5% = 2.3jt
        result.Pph21Amount.Amount.ShouldBe(2_300_000m);
    }

    [Fact]
    public void CalculateAnnual_WithDifferentPtkpStatus_ShouldGiveDifferentResults()
    {
        // Arrange
        var annualIncome = new Money(100_000_000m);
        var hasNpwp = true;

        // Act
        var tk0 = PPh21Calculator.CalculateAnnual(annualIncome, "TK/0", hasNpwp);
        var k3 = PPh21Calculator.CalculateAnnual(annualIncome, "K/3", hasNpwp);

        // Assert
        // K/3 has higher PTKP (72jt vs 54jt), so should have lower tax
        k3.Pph21Amount.Amount.ShouldBeLessThan(tk0.Pph21Amount.Amount);
    }

    [Fact]
    public void EstimateMonthlyFromAnnual_ShouldDivideBy12()
    {
        // Arrange
        var annualIncome = new Money(120_000_000m); // 120 juta per tahun
        var ptkpStatus = "TK/0";
        var hasNpwp = true;

        // Act
        var monthlyEstimate = PPh21Calculator.EstimateMonthlyFromAnnual(annualIncome, ptkpStatus, hasNpwp);
        var annualTax = PPh21Calculator.CalculateAnnual(annualIncome, ptkpStatus, hasNpwp);

        // Assert
        var expectedMonthly = (annualTax.Pph21Amount / 12m).Round();
        monthlyEstimate.Amount.ShouldBe(expectedMonthly.Amount);
    }
}

// Made with Bob
