using FluentAssertions;
using NexaLearn.Domain.Aggregates.Students;
using NexaLearn.Domain.ValueObjects;

namespace NexaLearn.Domain.Tests.Aggregates;

public class StudentTests
{
    private static Email ValidEmail() => Email.Create("student@example.com").Value;

    [Fact]
    public void Student_Create_ValidData_Succeeds()
    {
        var result = Student.Create(Guid.NewGuid(), ValidEmail(), "Alejandro Martín");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Student_Create_NullName_ReturnsFailure()
    {
        var result = Student.Create(Guid.NewGuid(), ValidEmail(), null!);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Student_Create_EmptyName_ReturnsFailure()
    {
        var result = Student.Create(Guid.NewGuid(), ValidEmail(), string.Empty);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Student_Create_WhitespaceName_ReturnsFailure()
    {
        var result = Student.Create(Guid.NewGuid(), ValidEmail(), "   ");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Student_Create_TrimsName()
    {
        var result = Student.Create(Guid.NewGuid(), ValidEmail(), "  Alejandro  ");

        result.Value.Name.Should().Be("Alejandro");
    }

    [Fact]
    public void Student_Email_IsValueObject()
    {
        var email = ValidEmail();
        var result = Student.Create(Guid.NewGuid(), email, "Alejandro");

        result.Value.Email.Should().Be(email);
        result.Value.Email.Should().BeOfType<Email>();
    }

    [Fact]
    public void Student_Create_ExposesCorrectId()
    {
        var id = Guid.NewGuid();
        var result = Student.Create(id, ValidEmail(), "Alejandro");

        result.Value.Id.Should().Be(id);
    }

    [Fact]
    public void Student_Create_DomainEvents_InitiallyEmpty()
    {
        var result = Student.Create(Guid.NewGuid(), ValidEmail(), "Alejandro");

        result.Value.DomainEvents.Should().BeEmpty();
    }
}
