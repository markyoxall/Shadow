using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace Shadow.FunkyGibbon;

public sealed class ExceptionHandlingMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            await next(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Log exception with context information and a structured scope
            var invocationId = context.InvocationId?.ToString() ?? "(unknown)";
            using var _ = _logger.BeginScope(new { InvocationId = invocationId });
            _logger.LogError(ex, "Unhandled exception in function invocation {InvocationId}", invocationId);

            // Re-throw so Functions runtime receives the exception as well
            throw;
        }
    }
}
