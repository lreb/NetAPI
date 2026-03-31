using NetAPI.Domain.Common;

namespace NetAPI.Domain.Events;

public sealed record ProductCreatedEvent(Guid ProductId, string Name) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public sealed record ProductPriceChangedEvent(Guid ProductId, decimal NewAmount, string Currency) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
