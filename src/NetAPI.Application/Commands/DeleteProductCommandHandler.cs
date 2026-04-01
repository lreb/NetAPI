using MediatR;
using NetAPI.Domain.Entities;
using NetAPI.Domain.Exceptions;
using NetAPI.Domain.Interfaces;

namespace NetAPI.Application.Commands;

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProductCommandHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.Id);

        product.Deactivate();
        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
