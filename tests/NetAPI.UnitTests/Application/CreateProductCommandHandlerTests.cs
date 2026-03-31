using FluentAssertions;
using Mapster;
using Moq;
using NetAPI.Application.Commands;
using NetAPI.Application.DTOs;
using NetAPI.Domain.Entities;
using NetAPI.Domain.Interfaces;
using NetAPI.Domain.ValueObjects;

namespace NetAPI.UnitTests.Application;

public class CreateProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CreateProductCommandHandler _handler;

    public CreateProductCommandHandlerTests()
    {
        // Register Mapster mapping so Adapt<ProductDto>() works in handler
        TypeAdapterConfig.GlobalSettings.NewConfig<Product, ProductDto>()
            .Map(dest => dest.Price, src => src.Price.Amount)
            .Map(dest => dest.Currency, src => src.Price.Currency);

        _handler = new CreateProductCommandHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateProductAndReturnDto()
    {
        // Arrange
        var command = new CreateProductCommand("Keyboard", "Mechanical keyboard", 79.99m, "USD", 100);

        _repoMock
            .Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product p, CancellationToken _) => p);

        _uowMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Keyboard");
        result.Price.Should().Be(79.99m);
        result.Currency.Should().Be("USD");
        result.StockQuantity.Should().Be(100);

        _repoMock.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidCurrency_ShouldThrowDomainException()
    {
        var command = new CreateProductCommand("X", "desc", 10m, "US", 1); // "US" is invalid — must be 3 chars
        var act = async () => await _handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<NetAPI.Domain.Exceptions.DomainException>();
    }
}
