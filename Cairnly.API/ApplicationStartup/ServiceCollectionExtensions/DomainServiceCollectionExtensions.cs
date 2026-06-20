using System;
using Cairnly.API.Services.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Cairnly.API.ApplicationStartup.ServiceCollectionExtensions;

/// <summary>
/// Extension methods for registering domain services.
/// </summary>
public static class DomainServiceCollectionExtensions
{
    /// <summary>
    /// Adds domain services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<INoteService, NoteService>();

        return services;
    }
}