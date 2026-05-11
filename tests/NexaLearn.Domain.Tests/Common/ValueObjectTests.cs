using FluentAssertions;
using NexaLearn.Domain.Common;

namespace NexaLearn.Domain.Tests.Common;

public class ValueObjectTests
{
    // --- Clases concretas de prueba ---

    private class SingleComponentValueObject : ValueObject
    {
        public string Value { get; }

        public SingleComponentValueObject(string value) => Value = value;

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }
    }

    private class MultiComponentValueObject : ValueObject
    {
        public decimal Amount { get; }
        public string Currency { get; }

        public MultiComponentValueObject(decimal amount, string currency)
        {
            Amount = amount;
            Currency = currency;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }
    }

    // --- Igualdad estructural ---

    [Fact]
    public void ValueObject_SameComponents_AreEqual()
    {
        var a = new SingleComponentValueObject("nexa");
        var b = new SingleComponentValueObject("nexa");

        a.Should().Be(b);
    }

    [Fact]
    public void ValueObject_DifferentComponents_AreNotEqual()
    {
        var a = new SingleComponentValueObject("nexa");
        var b = new SingleComponentValueObject("learn");

        a.Should().NotBe(b);
    }

    [Fact]
    public void ValueObject_MultipleComponents_AllMustMatchForEquality()
    {
        var a = new MultiComponentValueObject(10, "USD");
        var b = new MultiComponentValueObject(10, "USD");
        var c = new MultiComponentValueObject(10, "ARS");

        a.Should().Be(b);
        a.Should().NotBe(c);
    }

    [Fact]
    public void ValueObject_SameComponents_SameHashCode()
    {
        var a = new SingleComponentValueObject("nexa");
        var b = new SingleComponentValueObject("nexa");

        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void ValueObject_DifferentComponents_DifferentHashCode()
    {
        var a = new SingleComponentValueObject("nexa");
        var b = new SingleComponentValueObject("learn");

        a.GetHashCode().Should().NotBe(b.GetHashCode());
    }

    // --- Operadores == y != ---

    [Fact]
    public void ValueObject_EqualityOperator_SameComponents_ReturnsTrue()
    {
        var a = new SingleComponentValueObject("nexa");
        var b = new SingleComponentValueObject("nexa");

        (a == b).Should().BeTrue();
    }

    [Fact]
    public void ValueObject_EqualityOperator_DifferentComponents_ReturnsFalse()
    {
        var a = new SingleComponentValueObject("nexa");
        var b = new SingleComponentValueObject("learn");

        (a == b).Should().BeFalse();
    }

    [Fact]
    public void ValueObject_InequalityOperator_DifferentComponents_ReturnsTrue()
    {
        var a = new SingleComponentValueObject("nexa");
        var b = new SingleComponentValueObject("learn");

        (a != b).Should().BeTrue();
    }

    [Fact]
    public void ValueObject_InequalityOperator_SameComponents_ReturnsFalse()
    {
        var a = new SingleComponentValueObject("nexa");
        var b = new SingleComponentValueObject("nexa");

        (a != b).Should().BeFalse();
    }

    [Fact]
    public void ValueObject_NullComparison_EqualityOperator_ReturnsFalse()
    {
        var a = new SingleComponentValueObject("nexa");

        (a == null).Should().BeFalse();
    }

    [Fact]
    public void ValueObject_BothNull_EqualityOperator_ReturnsTrue()
    {
        SingleComponentValueObject? a = null;
        SingleComponentValueObject? b = null;

        (a == b).Should().BeTrue();
    }
}
