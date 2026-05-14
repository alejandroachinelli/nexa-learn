using MediatR;
using NexaLearn.Application.Enrollments.Commands;

namespace NexaLearn.Api.Endpoints;

internal static class EnrollmentEndpoints
{
    internal static void MapEnrollmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/enrollments").WithTags("Enrollments");

        group.MapPost("/", Enroll).RequireAuthorization();
        group.MapPost("/{id:guid}/complete-lesson", CompleteLesson).RequireAuthorization();
    }

    private static async Task<IResult> Enroll(
        EnrollStudentCommand command,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return result.IsFailure
            ? Results.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest)
            : Results.Created($"/api/enrollments/{result.Value}", result.Value);
    }

    private static async Task<IResult> CompleteLesson(
        Guid id,
        CompleteLessonCommand command,
        IMediator mediator,
        CancellationToken ct)
    {
        if (id != command.EnrollmentId)
            return Results.Problem("Enrollment ID mismatch", statusCode: StatusCodes.Status400BadRequest);

        var result = await mediator.Send(command, ct);
        return result.IsFailure
            ? Results.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest)
            : Results.NoContent();
    }
}
