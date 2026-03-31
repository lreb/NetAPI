using Mapster;
using MediatR;
using NetAPI.Application.DTOs;
using NetAPI.Domain.Interfaces;

namespace NetAPI.Application.Queries;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    private readonly IProductRepository _productRepository;

    public GetProductByIdQueryHandler(IProductRepository productRepository) =>
        _productRepository = productRepository;

    public async Task<ProductDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.Id, cancellationToken);
        return product?.Adapt<ProductDto>();
    }
}

public class GetAllProductsQueryHandler : IRequestHandler<GetAllProductsQuery, PaginatedResult<ProductDto>>
{
    private readonly IProductRepository _productRepository;

    public GetAllProductsQueryHandler(IProductRepository productRepository) =>
        _productRepository = productRepository;

    public async Task<PaginatedResult<ProductDto>> Handle(GetAllProductsQuery request, CancellationToken cancellationToken)
    {
        var allProducts = await _productRepository.GetActiveProductsAsync(cancellationToken);
        var totalCount = allProducts.Count;

        var paged = allProducts
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => p.Adapt<ProductDto>())
            .ToList();

        return new PaginatedResult<ProductDto>(paged, totalCount, request.Page, request.PageSize);
    }
}
