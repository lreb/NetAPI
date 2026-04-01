using NetAPI.Domain.Common;
using NetAPI.Domain.Exceptions;

namespace NetAPI.Domain.ValueObjects;

public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money() { Currency = string.Empty; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new DomainException("Money amount cannot be negative.");

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new DomainException("Currency must be a valid 3-letter ISO code (e.g. USD).");

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public static Money Zero(string currency) => new(0, currency);
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new DomainException("Cannot add money with different currencies.");
        return new Money(Amount + other.Amount, Currency);
    }

    public override string ToString() => $"{Amount:F2} {Currency}";
}
