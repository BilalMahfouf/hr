using VeterinaryApi.Common.Errors;

namespace VeterinaryApi.Domain.Common;


public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
        {
            throw new DomainException(
                Error.Validation($"{nameof(Money)}.InvalidAmount",
                "Invalid Money Amount (less then 0)"));
        }
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency is required.", nameof(currency));
        }

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    public static Money InDzd(decimal amount) => new(amount, "DZD");

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot add money of different currencies.");
        return new Money(Amount + other.Amount, Currency);
    }

    public override string ToString() => $"{Amount} {Currency}";
}
