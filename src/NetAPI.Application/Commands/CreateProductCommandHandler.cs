using MediatR;
using NetAPI.Application.DTOs;
using NetAPI.Domain.Entities;
using NetAPI.Domain.Interfaces;
using NetAPI.Domain.ValueObjects;
using Mapster;

namespace NetAPI.Application.Commands;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductCommandHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var price = new Money(request.Price, request.Currency);
        var product = Product.Create(request.Name, request.Description, price, request.InitialStock);

        await _productRepository.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return product.Adapt<ProductDto>();
    }
}
