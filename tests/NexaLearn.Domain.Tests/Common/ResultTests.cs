using FluentAssertions;
using NexaLearn.Domain.Common;

namespace NexaLearn.Domain.Tests.Common;

public class ResultTests
{
    // --- Result (non-generic) ---

    [Fact]
    public void Result_Success_IsSuccessTrue()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Result_Success_IsFailureFalse()
    {
        var result = Result.Success();

        result.IsFailure.Should().BeFalse();
    }

    [Fact]
    public void Result_Failure_IsFailureTrue()
    {
        var result = Result.Failure("algo salió mal");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Result_Failure_IsSuccessFalse()
    {
        var result = Result.Failure("algo salió mal");

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Result_Failure_HasCorrectErrorMessage()
    {
        var result = Result.Failure("email inválido");

        result.Error.Should().Be("email inválido");
    }

    [Fact]
    public void Result_Success_ErrorIsEmpty()
    {
        var result = Result.Success();

        result.Error.Should().BeEmpty();
    }

    // --- Result<T> ---

    [Fact]
    public void ResultT_Success_IsSuccessTrue()
    {
        var result = Result<int>.Success(42);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ResultT_Success_ExposesValue()
    {
        var result = Result<string>.Success("nexa");

        result.Value.Should().Be("nexa");
    }

    [Fact]
    public void ResultT_Success_ErrorIsEmpty()
    {
        var result = Result<int>.Success(1);

        result.Error.Should().BeEmpty();
    }

    [Fact]
    public void ResultT_Failure_IsFailureTrue()
    {
        var result = Result<int>.Failure("error");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ResultT_Failure_HasCorrectErrorMessage()
    {
        var result = Result<string>.Failure("curso no encontrado");

        result.Error.Should().Be("curso no encontrado");
    }

    [Fact]
    public void ResultT_Failure_AccessingValueThrowsInvalidOperationException()
    {
        var result = Result<int>.Failure("error");

        var act = () => { _ = result.Value; };

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ResultT_Failure_DoesNotRequireValue()
    {
        var act = () => Result<string>.Failure("error sin valor");

        act.Should().NotThrow();
    }
}
