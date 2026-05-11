using NexaLearn.Domain.Common;

namespace NexaLearn.Domain.ValueObjects;

public sealed class Money : ValueObject
{
    public static readonly Money Free = new(0m, "USD");

    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Result<Money> Create(decimal amount, string currency)
    {
        if (amount < 0)
            return Result<Money>.Failure("El monto no puede ser negativo.");

        if (string.IsNullOrWhiteSpace(currency))
            return Result<Money>.Failure("La moneda no puede estar vacía.");

        var normalized = currency.Trim().ToUpperInvariant();

        if (normalized.Length != 3)
            return Result<Money>.Failure("La moneda debe tener exactamente 3 caracteres (ISO 4217).");

        return Result<Money>.Success(new Money(amount, normalized));
    }

    public Result<Money> Add(Money other)
    {
        if (Currency != other.Currency)
            return Result<Money>.Failure($"No se pueden sumar monedas distintas: {Currency} y {other.Currency}.");

        return Result<Money>.Success(new Money(Amount + other.Amount, Currency));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
