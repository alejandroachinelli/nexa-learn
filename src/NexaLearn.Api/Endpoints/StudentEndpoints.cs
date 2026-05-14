using MediatR;
using NexaLearn.Application.Students.Commands;

namespace NexaLearn.Api.Endpoints;

internal static class StudentEndpoints
{
    internal static void MapStudentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/students").WithTags("Students");

        group.MapPost("/", Register);
    }

    private static async Task<IResult> Register(
        RegisterStudentCommand command,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return result.IsFailure
            ? Results.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest)
            : Results.Created($"/api/students/{result.Value}", result.Value);
    }
}
