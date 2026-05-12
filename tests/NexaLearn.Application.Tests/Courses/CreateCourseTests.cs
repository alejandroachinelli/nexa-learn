using FluentAssertions;
using NexaLearn.Application.Courses.Commands;
using NexaLearn.Application.Tests.Common.InMemory;
using NexaLearn.Domain.ValueObjects;

namespace NexaLearn.Application.Tests.Courses;

public class CreateCourseTests
{
    // --- Helpers ---

    private static (CreateCourseCommandHandler handler, InMemoryCourseRepository repo) BuildHandler()
    {
        var repo = new InMemoryCourseRepository();
        var uow = new InMemoryUnitOfWork();
        var handler = new CreateCourseCommandHandler(repo, uow);
        return (handler, repo);
    }

    // --- Handler ---

    [Fact]
    public async Task Handler_ValidData_ReturnsSuccessWithCourseId()
    {
        var (handler, _) = BuildHandler();
        var command = new CreateCourseCommand("Clean Architecture con .NET 8", 49.99m, "USD");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handler_ValidData_PersistsCourse()
    {
        var (handler, repo) = BuildHandler();
        var command = new CreateCourseCommand("Clean Architecture con .NET 8", 49.99m, "USD");

        var result = await handler.Handle(command, CancellationToken.None);

        var persisted = await repo.GetByIdAsync(result.Value, CancellationToken.None);
        persisted.Should().NotBeNull();
        persisted!.Title.Value.Should().Be("Clean Architecture con .NET 8");
        persisted.Price.Amount.Should().Be(49.99m);
        persisted.Price.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task Handler_TitleTooLong_ReturnsFailure()
    {
        var (handler, _) = BuildHandler();
        var longTitle = new string('A', CourseTitle.MaxLength + 1);
        var command = new CreateCourseCommand(longTitle, 49.99m, "USD");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handler_NegativePrice_ReturnsFailure()
    {
        var (handler, _) = BuildHandler();
        var command = new CreateCourseCommand("Clean Architecture con .NET 8", -1m, "USD");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handler_InvalidCurrency_ReturnsFailure()
    {
        var (handler, _) = BuildHandler();
        var command = new CreateCourseCommand("Clean Architecture con .NET 8", 49.99m, "US");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    // --- Validator ---

    [Fact]
    public void Validator_EmptyTitle_IsInvalid()
    {
        var validator = new CreateCourseCommandValidator();
        var command = new CreateCourseCommand("", 49.99m, "USD");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCourseCommand.Title));
    }

    [Fact]
    public void Validator_NegativePrice_IsInvalid()
    {
        var validator = new CreateCourseCommandValidator();
        var command = new CreateCourseCommand("Clean Architecture con .NET 8", -1m, "USD");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCourseCommand.Price));
    }

    [Fact]
    public void Validator_CurrencyNot3Chars_IsInvalid()
    {
        var validator = new CreateCourseCommandValidator();
        var command = new CreateCourseCommand("Clean Architecture con .NET 8", 49.99m, "US");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCourseCommand.Currency));
    }

    [Fact]
    public void Validator_ValidCommand_IsValid()
    {
        var validator = new CreateCourseCommandValidator();
        var command = new CreateCourseCommand("Clean Architecture con .NET 8", 49.99m, "USD");

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
