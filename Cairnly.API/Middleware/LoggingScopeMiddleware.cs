using System;
using System.Threading.Tasks;
using Cairnly.API.Services.Core;
using Cairnly.API.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Cairnly.API.Middleware;

/// <summary>
/// This middleware adds the correlation id passed in the request into the response.
/// </summary>
public sealed class LoggingScopeMiddleware
{
    private readonly RequestDelegate next;

    private readonly ILogger<LoggingScopeMiddleware> logger;

    public LoggingScopeMiddleware(RequestDelegate next, ILogger<LoggingScopeMiddleware> logger)
    {
        this.next = next ?? throw new ArgumentNullException(nameof(next));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context, ICorrelationIdService correlationIdService)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(correlationIdService);

        var scope = RequestLoggingScope.Build(context, correlationIdService.CorrelationId);

        using (this.logger.BeginScope(scope))
        {
            await this.next(context);
        }
    }
}