using MediatR;
using NetAPI.Application.DTOs;

namespace NetAPI.Application.Queries;

public record GetProductByIdQuery(Guid Id) : IRequest<ProductDto?>;

public record GetAllProductsQuery(int Page = 1, int PageSize = 20) : IRequest<PaginatedResult<ProductDto>>;
