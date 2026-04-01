using MediatR;
using Microsoft.Extensions.Logging;

namespace NetAPI.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that logs every request and its outcome.
/// </summary>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger) =>
        _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("[MediatR] Handling {RequestName}: {@Request}", requestName, request);

        try
        {
            var response = await next(cancellationToken);
            _logger.LogInformation("[MediatR] {RequestName} handled successfully.", requestName);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MediatR] {RequestName} failed: {Message}", requestName, ex.Message);
            throw;
        }
    }
}
