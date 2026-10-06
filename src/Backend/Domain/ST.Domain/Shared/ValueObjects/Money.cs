namespace ST.Domain.Shared.ValueObjects;

public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency must be specified", nameof(currency));

        Amount = amount;
        Currency = currency.Trim().ToUpperInvariant();
    }

    public static Money Zero(string currency) => new(0m, currency);

    public static Money operator +(Money left, Money right)
    {
        ArgumentNullException.ThrowIfNull(left, nameof(left));
        ArgumentNullException.ThrowIfNull(right, nameof(right));
        if (left.Currency != right.Currency)
            throw new InvalidOperationException($"Cannot add monies with different currencies: {left.Currency} and {right.Currency}");

        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        ArgumentNullException.ThrowIfNull(left, nameof(left));
        ArgumentNullException.ThrowIfNull(right, nameof(right));
        if (left.Currency != right.Currency)
            throw new InvalidOperationException($"Cannot subtract monies with different currencies: {left.Currency} and {right.Currency}");

        return new Money(left.Amount - right.Amount, left.Currency);
    }

    public static Money operator *(Money left, decimal multiplier)
    {
        ArgumentNullException.ThrowIfNull(left, nameof(left));
        return new Money(left.Amount * multiplier, left.Currency);
    }

    public static Money operator /(Money left, decimal divisor)
    {
        ArgumentNullException.ThrowIfNull(left, nameof(left));
        if (divisor == 0)
            throw new DivideByZeroException("Cannot divide money by zero");

        return new Money(left.Amount / divisor, left.Currency);
    }

    public override string ToString()
    {
        return $"{Currency} {Amount:N2}";
    }
}
