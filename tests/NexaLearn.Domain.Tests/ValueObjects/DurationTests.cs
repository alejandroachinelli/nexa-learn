using FluentAssertions;
using NexaLearn.Domain.ValueObjects;

namespace NexaLearn.Domain.Tests.ValueObjects;

public class DurationTests
{
    [Fact]
    public void Duration_PositiveMinutes_CreatesSuccessfully()
    {
        var result = Duration.Create(90);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Duration_ZeroMinutes_ReturnsFailure()
    {
        var result = Duration.Create(0);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Duration_NegativeMinutes_ReturnsFailure()
    {
        var result = Duration.Create(-1);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Duration_Minutes_ExposesCorrectValue()
    {
        var result = Duration.Create(90);

        result.Value.Minutes.Should().Be(90);
    }

    [Fact]
    public void Duration_Hours_IsCorrectlyCalculated()
    {
        var result = Duration.Create(90);

        result.Value.Hours.Should().BeApproximately(1.5, precision: 0.001);
    }

    [Fact]
    public void Duration_OneMinute_IsValidBoundary()
    {
        var result = Duration.Create(1);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Duration_SameMinutes_AreEqual()
    {
        var a = Duration.Create(60).Value;
        var b = Duration.Create(60).Value;

        a.Should().Be(b);
    }

    [Fact]
    public void Duration_DifferentMinutes_AreNotEqual()
    {
        var a = Duration.Create(60).Value;
        var b = Duration.Create(90).Value;

        a.Should().NotBe(b);
    }
}
