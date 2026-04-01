using MediatR;
using NetAPI.Application.DTOs;

namespace NetAPI.Application.Commands;

public record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    string Currency,
    int InitialStock
) : IRequest<ProductDto>;
