
using System;
using Cairnly.API.Middleware;
using Microsoft.AspNetCore.Builder;

namespace Cairnly.API.ApplicationStartup.ApplicationBuilderExtensions;

/// <summary>
/// Provides middleware pipeline configuration helpers for the API.
/// </summary>
public static class MiddlewareApplicationBuilderExtensions
{
    /// <summary>
    /// Adds correlation-id propagation to the application pipeline.
    /// </summary>
    /// <param name="app">The application builder to configure.</param>
    /// <returns>The configured application builder.</returns>
    public static IApplicationBuilder UseCorrelationIdMiddleware(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseMiddleware<CorrelationIdMiddleware>();

        return app;
    }

    /// <summary>
    /// Adds the global exception handler to the application pipeline.
    /// </summary>
    /// <param name="app">The application builder to configure.</param>
    /// <returns>The configured application builder.</returns>
    public static IApplicationBuilder UseGlobalExceptionHandlerMiddleware(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseExceptionHandler(builder => builder.UseMiddleware<GlobalExceptionHandlerMiddleware>());

        return app;
    }

    /// <summary>
    /// Adds browser security response headers to the application pipeline.
    /// </summary>
    /// <param name="app">The application builder to configure.</param>
    /// <returns>The configured application builder.</returns>
    public static IApplicationBuilder UseSecurityHeadersMiddleware(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseMiddleware<SecurityHeadersMiddleware>();

        return app;
    }
}