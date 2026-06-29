using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Cairnly.API.ApplicationStartup.ApplicationBuilderExtensions;
using Cairnly.API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Cairnly.API.Tests.ApplicationStartup.ApplicationBuilderExtensions;

/// <summary>
/// Tests for API security application-builder extensions and middleware.
/// </summary>
public sealed class SecurityApplicationBuilderExtensionsTests
{
    [Fact]
    public void CorsRequestHeaderAllowlist_ContainsOnlySupportedCredentialedHeaders()
    {
        Assert.Equal(
            ["Authorization", "Content-Type", "X-CSRF-Token", "X-Correlation-Id"],
            CorsApplicationBuilderExtensions.AllowedRequestHeaders);
    }

    [Fact]
    public async Task SecurityHeadersMiddleware_AddsClickjackingHeaders()
    {
        var responseFeature = new CapturingResponseFeature();
        var context = new DefaultHttpContext();
        context.Features.Set<IHttpResponseFeature>(responseFeature);

        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);
        await responseFeature.FireOnStartingAsync();

        Assert.Equal("DENY", context.Response.Headers.XFrameOptions);
        Assert.Equal("frame-ancestors 'none'", context.Response.Headers.ContentSecurityPolicy);
    }

    private sealed class CapturingResponseFeature : IHttpResponseFeature
    {
        private readonly List<(Func<object, Task> Callback, object State)> callbacks = [];

        public int StatusCode { get; set; } = StatusCodes.Status200OK;

        public string? ReasonPhrase { get; set; }

        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();

        public Stream Body { get; set; } = Stream.Null;

        public bool HasStarted { get; private set; }

        public void OnStarting(Func<object, Task> callback, object state) => this.callbacks.Add((callback, state));

        public void OnCompleted(Func<object, Task> callback, object state)
        {
        }

        public async Task FireOnStartingAsync()
        {
            this.HasStarted = true;
            foreach (var (callback, state) in this.callbacks)
            {
                await callback(state);
            }
        }
    }
}