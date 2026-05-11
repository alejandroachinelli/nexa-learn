using FluentAssertions;
using NexaLearn.Domain.ValueObjects;

namespace NexaLearn.Domain.Tests.ValueObjects;

public class CourseTitleTests
{
    [Fact]
    public void CourseTitle_ValidTitle_CreatesSuccessfully()
    {
        var result = CourseTitle.Create("Clean Architecture con .NET 8");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void CourseTitle_NullValue_ReturnsFailure()
    {
        var result = CourseTitle.Create(null!);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void CourseTitle_EmptyValue_ReturnsFailure()
    {
        var result = CourseTitle.Create(string.Empty);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void CourseTitle_WhitespaceOnly_ReturnsFailure()
    {
        var result = CourseTitle.Create("   ");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void CourseTitle_ExactlyMaxLength_CreatesSuccessfully()
    {
        var title = new string('a', 200);
        var result = CourseTitle.Create(title);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void CourseTitle_ExceedsMaxLength_ReturnsFailure()
    {
        var title = new string('a', 201);
        var result = CourseTitle.Create(title);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void CourseTitle_TrimsWhitespace()
    {
        var result = CourseTitle.Create("  Clean Architecture  ");

        result.Value.Value.Should().Be("Clean Architecture");
    }

    [Fact]
    public void CourseTitle_SameValue_AreEqual()
    {
        var a = CourseTitle.Create("Domain-Driven Design").Value;
        var b = CourseTitle.Create("Domain-Driven Design").Value;

        a.Should().Be(b);
    }

    [Fact]
    public void CourseTitle_DifferentValues_AreNotEqual()
    {
        var a = CourseTitle.Create("Clean Architecture").Value;
        var b = CourseTitle.Create("Domain-Driven Design").Value;

        a.Should().NotBe(b);
    }
}
