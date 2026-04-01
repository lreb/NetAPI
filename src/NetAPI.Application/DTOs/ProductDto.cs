namespace NetAPI.Application.DTOs;

public record ProductDto(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    int StockQuantity,
    bool IsActive,
    DateTime CreatedAt
);
