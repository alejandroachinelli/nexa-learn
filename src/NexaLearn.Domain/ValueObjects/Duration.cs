using NexaLearn.Domain.Common;

namespace NexaLearn.Domain.ValueObjects;

public sealed class Duration : ValueObject
{
    public int Minutes { get; }
    public double Hours => Minutes / 60.0;

    private Duration(int minutes) => Minutes = minutes;

    public static Result<Duration> Create(int minutes)
    {
        if (minutes <= 0)
            return Result<Duration>.Failure("La duración debe ser mayor a cero minutos.");

        return Result<Duration>.Success(new Duration(minutes));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Minutes;
    }
}
