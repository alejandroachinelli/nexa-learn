using MediatR;
using NexaLearn.Application.Enrollments.Commands;

namespace NexaLearn.Api.Endpoints;

internal static class EnrollmentEndpoints
{
    internal record CompleteLessonRequest(Guid LessonId);

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
        CompleteLessonRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new CompleteLessonCommand(id, request.LessonId), ct);
        return result.IsFailure
            ? Results.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest)
            : Results.NoContent();
    }
}
