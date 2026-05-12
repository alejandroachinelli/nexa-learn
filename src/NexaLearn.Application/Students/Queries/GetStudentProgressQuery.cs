using MediatR;
using NexaLearn.Application.Students.DTOs;
using NexaLearn.Domain.Common;

namespace NexaLearn.Application.Students.Queries;

public record GetStudentProgressQuery(Guid StudentId) : IRequest<Result<StudentProgressDto>>;
