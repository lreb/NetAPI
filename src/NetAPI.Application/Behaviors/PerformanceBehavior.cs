using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace NetAPI.Application.Behaviors;

/// <summary>
/// Warns when a request exceeds the performance threshold (default 500ms).
/// </summary>
public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int WarningThresholdMilliseconds = 500;
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger) =>
        _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var response = await next(cancellationToken);
        sw.Stop();

        if (sw.ElapsedMilliseconds > WarningThresholdMilliseconds)
        {
            _logger.LogWarning(
                "[Performance] Slow request detected: {RequestName} took {ElapsedMs}ms. Request: {@Request}",
                typeof(TRequest).Name, sw.ElapsedMilliseconds, request);
        }

        return response;
    }
}
