using MediatR;

namespace NetAPI.Application.Commands;

public record DeleteProductCommand(Guid Id) : IRequest;
