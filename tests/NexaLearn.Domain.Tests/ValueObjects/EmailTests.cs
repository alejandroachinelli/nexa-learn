using FluentAssertions;
using NexaLearn.Domain.ValueObjects;

namespace NexaLearn.Domain.Tests.ValueObjects;

public class EmailTests
{
    [Fact]
    public void Email_ValidAddress_CreatesSuccessfully()
    {
        var result = Email.Create("user@example.com");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Email_NullValue_ReturnsFailure()
    {
        var result = Email.Create(null!);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Email_EmptyValue_ReturnsFailure()
    {
        var result = Email.Create(string.Empty);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Email_WithoutAtSign_ReturnsFailure()
    {
        var result = Email.Create("userexample.com");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Email_WithoutDomain_ReturnsFailure()
    {
        var result = Email.Create("user@");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Email_WithoutDotAfterAt_ReturnsFailure()
    {
        var result = Email.Create("user@examplecom");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Email_NormalizedToLowercase()
    {
        var result = Email.Create("User@Example.COM");

        result.Value.Value.Should().Be("user@example.com");
    }

    [Fact]
    public void Email_SameValue_AreEqual()
    {
        var a = Email.Create("user@example.com").Value;
        var b = Email.Create("user@example.com").Value;

        a.Should().Be(b);
    }

    [Fact]
    public void Email_DifferentValues_AreNotEqual()
    {
        var a = Email.Create("a@example.com").Value;
        var b = Email.Create("b@example.com").Value;

        a.Should().NotBe(b);
    }

    [Fact]
    public void Email_CaseInsensitive_SameAfterNormalization()
    {
        var lower = Email.Create("user@example.com").Value;
        var upper = Email.Create("USER@EXAMPLE.COM").Value;

        lower.Should().Be(upper);
    }
}
