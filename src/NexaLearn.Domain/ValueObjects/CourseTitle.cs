using NexaLearn.Domain.Common;

namespace NexaLearn.Domain.ValueObjects;

public sealed class CourseTitle : ValueObject
{
    public const int MaxLength = 200;

    public string Value { get; }

    private CourseTitle(string value) => Value = value;

    public static Result<CourseTitle> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<CourseTitle>.Failure("El título del curso no puede estar vacío.");

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
            return Result<CourseTitle>.Failure($"El título del curso no puede superar los {MaxLength} caracteres.");

        return Result<CourseTitle>.Success(new CourseTitle(trimmed));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
