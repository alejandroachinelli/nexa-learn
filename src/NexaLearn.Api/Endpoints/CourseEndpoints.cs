using MediatR;
using NexaLearn.Application.Courses.Commands;
using NexaLearn.Application.Courses.Queries;

namespace NexaLearn.Api.Endpoints;

internal static class CourseEndpoints
{
    internal static void MapCourseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/courses").WithTags("Courses");

        group.MapGet("/", ListPublished);
        group.MapGet("/{id:guid}", GetById);
        group.MapPost("/", Create).RequireAuthorization();
        group.MapPost("/{id:guid}/publish", Publish).RequireAuthorization();
    }

    private static async Task<IResult> ListPublished(IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new ListPublishedCoursesQuery(), ct);
        return result.IsFailure
            ? Results.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest)
            : Results.Ok(result.Value);
    }

    private static async Task<IResult> GetById(Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetCourseByIdQuery(id), ct);
        return result.IsFailure
            ? Results.NotFound(result.Error)
            : Results.Ok(result.Value);
    }

    private static async Task<IResult> Create(
        CreateCourseCommand command,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return result.IsFailure
            ? Results.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest)
            : Results.Created($"/api/courses/{result.Value}", result.Value);
    }

    private static async Task<IResult> Publish(Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new PublishCourseCommand(id), ct);
        return result.IsFailure
            ? Results.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest)
            : Results.NoContent();
    }
}
