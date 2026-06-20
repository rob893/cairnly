
using System;
using Microsoft.AspNetCore.Builder;
using Cairnly.API.Middleware;

namespace Cairnly.API.ApplicationStartup.ApplicationBuilderExtensions;

public static class MiddlewareApplicationBuilderExtensions
{
    public static IApplicationBuilder UseCorrelationIdMiddleware(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseMiddleware<CorrelationIdMiddleware>();

        return app;
    }

    public static IApplicationBuilder UseGlobalExceptionHandlerMiddleware(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseExceptionHandler(builder => builder.UseMiddleware<GlobalExceptionHandlerMiddleware>());

        return app;
    }
}