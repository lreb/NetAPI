using FluentAssertions;
using NetAPI.Domain.Entities;
using NetAPI.Domain.Exceptions;
using NetAPI.Domain.ValueObjects;

namespace NetAPI.UnitTests.Domain;

public class ProductTests
{
    [Fact]
    public void Create_WithValidArguments_ShouldSucceed()
    {
        // Arrange
        var price = new Money(99.99m, "USD");

        // Act
        var product = Product.Create("Laptop", "A laptop", price, 10);

        // Assert
        product.Name.Should().Be("Laptop");
        product.Price.Amount.Should().Be(99.99m);
        product.Price.Currency.Should().Be("USD");
        product.StockQuantity.Should().Be(10);
        product.IsActive.Should().BeTrue();
        product.IsDeleted.Should().BeFalse();
        product.DomainEvents.Should().HaveCount(1);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyName_ShouldThrowArgumentException(string? name)
    {
        var price = new Money(10m, "USD");
        var act = () => Product.Create(name!, "desc", price, 0);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNegativeStock_ShouldThrowDomainException()
    {
        var price = new Money(10m, "USD");
        var act = () => Product.Create("Name", "desc", price, -1);
        act.Should().Throw<DomainException>().WithMessage("*negative*");
    }

    [Fact]
    public void ReduceStock_WithValidQuantity_ShouldReduceStock()
    {
        var product = Product.Create("P", "d", new Money(1m, "USD"), 50);
        product.ClearDomainEvents();

        product.ReduceStock(10);

        product.StockQuantity.Should().Be(40);
    }

    [Fact]
    public void ReduceStock_WhenInsufficientStock_ShouldThrowDomainException()
    {
        var product = Product.Create("P", "d", new Money(1m, "USD"), 5);
        var act = () => product.ReduceStock(10);
        act.Should().Throw<DomainException>().WithMessage("*Insufficient*");
    }

    [Fact]
    public void UpdatePrice_ShouldRaisePriceChangedEvent()
    {
        var product = Product.Create("P", "d", new Money(10m, "USD"), 5);
        product.ClearDomainEvents();

        product.UpdatePrice(new Money(25m, "EUR"));

        product.Price.Amount.Should().Be(25m);
        product.Price.Currency.Should().Be("EUR");
        product.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        var product = Product.Create("P", "d", new Money(1m, "USD"), 5);
        product.Deactivate();
        product.IsActive.Should().BeFalse();
    }
}
