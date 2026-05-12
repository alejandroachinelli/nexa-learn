using FluentAssertions;
using FluentValidation;
using MediatR;
using NexaLearn.Application.Common.Behaviors;
using NexaLearn.Domain.Common;

namespace NexaLearn.Application.Tests.Common;

// --- Tipos auxiliares para los tests ---

public record FakeCommand(string Name) : IRequest<Result<Guid>>;

public class FakeCommandValidator : AbstractValidator<FakeCommand>
{
    public FakeCommandValidator() => RuleFor(x => x.Name).NotEmpty();
}

public class ValidationBehaviorTests
{
    private static RequestHandlerDelegate<Result<Guid>> NextThatSucceeds() =>
        _ => Task.FromResult(Result<Guid>.Success(Guid.NewGuid()));

    // --- Tests ---

    [Fact]
    public async Task Behavior_NoValidators_PassesToNext()
    {
        var behavior = new ValidationBehavior<FakeCommand, Result<Guid>>(
            validators: []);
        var command = new FakeCommand("Alejandro");

        var result = await behavior.Handle(command, NextThatSucceeds(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Behavior_ValidationFails_ReturnsFailureWithoutCallingNext()
    {
        var behavior = new ValidationBehavior<FakeCommand, Result<Guid>>(
            validators: [new FakeCommandValidator()]);
        var command = new FakeCommand("");
        var nextCalled = false;
        RequestHandlerDelegate<Result<Guid>> next = _ =>
        {
            nextCalled = true;
            return Task.FromResult(Result<Guid>.Success(Guid.NewGuid()));
        };

        var result = await behavior.Handle(command, next, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Behavior_ValidationPasses_ExecutesHandler()
    {
        var behavior = new ValidationBehavior<FakeCommand, Result<Guid>>(
            validators: [new FakeCommandValidator()]);
        var command = new FakeCommand("Alejandro");
        var expectedId = Guid.NewGuid();
        RequestHandlerDelegate<Result<Guid>> next = _ =>
            Task.FromResult(Result<Guid>.Success(expectedId));

        var result = await behavior.Handle(command, next, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedId);
    }
}
