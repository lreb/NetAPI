using FluentAssertions;
using NetAPI.Domain.Exceptions;
using NetAPI.Domain.ValueObjects;

namespace NetAPI.UnitTests.Domain;

public class MoneyTests
{
    [Fact]
    public void Constructor_WithValidValues_ShouldSucceed()
    {
        var money = new Money(100m, "usd");
        money.Amount.Should().Be(100m);
        money.Currency.Should().Be("USD"); // normalized to uppercase
    }

    [Fact]
    public void Constructor_WithNegativeAmount_ShouldThrowDomainException()
    {
        var act = () => new Money(-1m, "USD");
        act.Should().Throw<DomainException>().WithMessage("*negative*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData(null)]
    public void Constructor_WithInvalidCurrency_ShouldThrowDomainException(string? currency)
    {
        var act = () => new Money(10m, currency!);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Equality_SameAmountAndCurrency_ShouldBeEqual()
    {
        var m1 = new Money(50m, "EUR");
        var m2 = new Money(50m, "EUR");
        m1.Should().Be(m2);
    }

    [Fact]
    public void Equality_DifferentCurrency_ShouldNotBeEqual()
    {
        var m1 = new Money(50m, "USD");
        var m2 = new Money(50m, "EUR");
        m1.Should().NotBe(m2);
    }
}
