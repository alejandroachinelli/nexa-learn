using FluentValidation;
using MediatR;
using NexaLearn.Domain.Common;

namespace NexaLearn.Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next(cancellationToken);

        var context = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
            return await next(cancellationToken);

        var errorMessage = string.Join("; ", failures.Select(f => f.ErrorMessage));

        // TResponse es siempre Result o Result<T>. Para Result no-genérico usamos la
        // rama directa; para Result<T> usamos reflexión porque T no es conocido aquí.
        if (typeof(TResponse) == typeof(Result))
            return (TResponse)(object)Result.Failure(errorMessage);

        var failureMethod = typeof(TResponse).GetMethod("Failure", [typeof(string)]);
        return (TResponse)failureMethod!.Invoke(null, [errorMessage])!;
    }
}
