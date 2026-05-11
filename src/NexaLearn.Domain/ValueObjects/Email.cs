using NexaLearn.Domain.Common;

namespace NexaLearn.Domain.ValueObjects;

public sealed class Email : ValueObject
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Result<Email> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<Email>.Failure("El email no puede estar vacío.");

        var normalized = value.Trim().ToLowerInvariant();

        var atIndex = normalized.IndexOf('@');
        if (atIndex <= 0)
            return Result<Email>.Failure("El email debe contener '@'.");

        var domain = normalized[(atIndex + 1)..];
        if (string.IsNullOrEmpty(domain) || !domain.Contains('.'))
            return Result<Email>.Failure("El email debe tener un dominio válido con punto.");

        return Result<Email>.Success(new Email(normalized));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
