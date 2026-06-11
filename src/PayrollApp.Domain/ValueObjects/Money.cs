namespace PayrollApp.Domain.ValueObjects;

public record Money
{
    public decimal Amount { get; init; }
    public string Currency { get; init; }

    public Money(decimal amount, string currency = "IDR")
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Zero => new(0);

    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount - right.Amount, left.Currency);
    }

    public static Money operator *(Money money, decimal multiplier)
    {
        return new Money(money.Amount * multiplier, money.Currency);
    }

    public static Money operator /(Money money, decimal divisor)
    {
        if (divisor == 0)
            throw new DivideByZeroException("Cannot divide money by zero");
        
        return new Money(money.Amount / divisor, money.Currency);
    }

    public static bool operator >(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount > right.Amount;
    }

    public static bool operator <(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount < right.Amount;
    }

    public static bool operator >=(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount >= right.Amount;
    }

    public static bool operator <=(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount <= right.Amount;
    }

    public Money Round(int decimals = 0)
    {
        return new Money(
            Math.Round(Amount, decimals, MidpointRounding.AwayFromZero),
            Currency
        );
    }

    public Money Percentage(decimal percentage)
    {
        return new Money(Amount * (percentage / 100m), Currency);
    }

    private static void EnsureSameCurrency(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException(
                $"Cannot perform operation on different currencies: {left.Currency} and {right.Currency}"
            );
    }

    public override string ToString() => $"{Currency} {Amount:N0}";
}

// Made with Bob
