using FluentAssertions;
using NexaLearn.Application.Students.Commands;
using NexaLearn.Application.Tests.Common.InMemory;
using NexaLearn.Domain.Common;

namespace NexaLearn.Application.Tests.Students;

public class RegisterStudentTests
{
    // --- Helpers ---

    private static (RegisterStudentCommandHandler handler, InMemoryStudentRepository repo) BuildHandler()
    {
        var repo = new InMemoryStudentRepository();
        var uow = new InMemoryUnitOfWork();
        var handler = new RegisterStudentCommandHandler(repo, uow);
        return (handler, repo);
    }

    // --- Handler ---

    [Fact]
    public async Task Handler_ValidData_ReturnsSuccessWithStudentId()
    {
        var (handler, _) = BuildHandler();
        var command = new RegisterStudentCommand("Alejandro Martin", "alejandro@example.com");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handler_ValidData_PersistsStudent()
    {
        var (handler, repo) = BuildHandler();
        var command = new RegisterStudentCommand("Alejandro Martin", "alejandro@example.com");

        var result = await handler.Handle(command, CancellationToken.None);

        var persisted = await repo.GetByIdAsync(result.Value, CancellationToken.None);
        persisted.Should().NotBeNull();
        persisted!.Name.Should().Be("Alejandro Martin");
        persisted.Email.Value.Should().Be("alejandro@example.com");
    }

    [Fact]
    public async Task Handler_InvalidEmailFormat_ReturnsFailure()
    {
        var (handler, _) = BuildHandler();
        var command = new RegisterStudentCommand("Alejandro Martin", "not-an-email");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handler_DuplicateEmail_ReturnsFailure()
    {
        var (handler, _) = BuildHandler();
        var command = new RegisterStudentCommand("Alejandro Martin", "alejandro@example.com");

        await handler.Handle(command, CancellationToken.None);
        var duplicate = await handler.Handle(command, CancellationToken.None);

        duplicate.IsFailure.Should().BeTrue();
    }

    // --- Validator ---

    [Fact]
    public void Validator_EmptyName_IsInvalid()
    {
        var validator = new RegisterStudentCommandValidator();
        var command = new RegisterStudentCommand("", "alejandro@example.com");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterStudentCommand.Name));
    }

    [Fact]
    public void Validator_WhitespaceName_IsInvalid()
    {
        var validator = new RegisterStudentCommandValidator();
        var command = new RegisterStudentCommand("   ", "alejandro@example.com");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterStudentCommand.Name));
    }

    [Fact]
    public void Validator_EmptyEmail_IsInvalid()
    {
        var validator = new RegisterStudentCommandValidator();
        var command = new RegisterStudentCommand("Alejandro Martin", "");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterStudentCommand.Email));
    }

    [Fact]
    public void Validator_EmailWithoutAt_IsInvalid()
    {
        var validator = new RegisterStudentCommandValidator();
        var command = new RegisterStudentCommand("Alejandro Martin", "alejandroexample.com");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterStudentCommand.Email));
    }

    [Fact]
    public void Validator_ValidCommand_IsValid()
    {
        var validator = new RegisterStudentCommandValidator();
        var command = new RegisterStudentCommand("Alejandro Martin", "alejandro@example.com");

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
