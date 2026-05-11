using FluentAssertions;
using NexaLearn.Domain.ValueObjects;

namespace NexaLearn.Domain.Tests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Money_ValidAmountAndCurrency_CreatesSuccessfully()
    {
        var result = Money.Create(10.00m, "USD");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Money_NegativeAmount_ReturnsFailure()
    {
        var result = Money.Create(-1m, "USD");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Money_ZeroAmount_CreatesSuccessfully()
    {
        var result = Money.Create(0m, "USD");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Money_NullCurrency_ReturnsFailure()
    {
        var result = Money.Create(10m, null!);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Money_EmptyCurrency_ReturnsFailure()
    {
        var result = Money.Create(10m, string.Empty);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Money_TwoCharacterCurrency_ReturnsFailure()
    {
        var result = Money.Create(10m, "US");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Money_FourCharacterCurrency_ReturnsFailure()
    {
        var result = Money.Create(10m, "USDD");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Money_CurrencyNormalizedToUppercase()
    {
        var result = Money.Create(10m, "usd");

        result.Value.Currency.Should().Be("USD");
    }

    [Fact]
    public void Money_Free_IsZeroUsd()
    {
        Money.Free.Amount.Should().Be(0m);
        Money.Free.Currency.Should().Be("USD");
    }

    [Fact]
    public void Money_Add_SameCurrency_ReturnsCorrectSum()
    {
        var a = Money.Create(10m, "USD").Value;
        var b = Money.Create(5m, "USD").Value;

        var result = a.Add(b);

        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(15m);
        result.Value.Currency.Should().Be("USD");
    }

    [Fact]
    public void Money_Add_DifferentCurrency_ReturnsFailure()
    {
        var usd = Money.Create(10m, "USD").Value;
        var ars = Money.Create(10m, "ARS").Value;

        var result = usd.Add(ars);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Money_SameAmountAndCurrency_AreEqual()
    {
        var a = Money.Create(10m, "USD").Value;
        var b = Money.Create(10m, "USD").Value;

        a.Should().Be(b);
    }

    [Fact]
    public void Money_DifferentAmount_AreNotEqual()
    {
        var a = Money.Create(10m, "USD").Value;
        var b = Money.Create(20m, "USD").Value;

        a.Should().NotBe(b);
    }

    [Fact]
    public void Money_DifferentCurrency_AreNotEqual()
    {
        var a = Money.Create(10m, "USD").Value;
        var b = Money.Create(10m, "ARS").Value;

        a.Should().NotBe(b);
    }
}
