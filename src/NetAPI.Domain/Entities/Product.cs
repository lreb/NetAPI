using NetAPI.Domain.Common;
using NetAPI.Domain.Exceptions;
using NetAPI.Domain.Events;
using NetAPI.Domain.ValueObjects;

namespace NetAPI.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = string.Empty;
    public Money Price { get; private set; } = default!;
    public int StockQuantity { get; private set; }
    public bool IsActive { get; private set; }

    private Product() { }

    public static Product Create(string name, string description, Money price, int initialStock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(price);

        if (initialStock < 0)
            throw new DomainException("Initial stock cannot be negative.");

        var product = new Product
        {
            Name = name,
            Description = description,
            Price = price,
            StockQuantity = initialStock,
            IsActive = true
        };

        product.AddDomainEvent(new ProductCreatedEvent(product.Id, name));
        return product;
    }

    public void UpdatePrice(Money newPrice)
    {
        ArgumentNullException.ThrowIfNull(newPrice);
        Price = newPrice;
        AddDomainEvent(new ProductPriceChangedEvent(Id, newPrice.Amount, newPrice.Currency));
    }

    public void ReduceStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity to reduce must be positive.");

        if (StockQuantity < quantity)
            throw new DomainException($"Insufficient stock. Available: {StockQuantity}, Requested: {quantity}");

        StockQuantity -= quantity;
    }

    public void Deactivate() => IsActive = false;
}
