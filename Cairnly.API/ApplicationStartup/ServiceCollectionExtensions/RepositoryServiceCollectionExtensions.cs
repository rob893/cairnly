using System;
using Cairnly.API.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Cairnly.API.ApplicationStartup.ServiceCollectionExtensions;

/// <summary>
/// Extension methods for registering repository services.
/// </summary>
public static class RepositoryServiceCollectionExtensions
{
    /// <summary>
    /// Adds repository services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddRepositoryServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<INoteRepository, NoteRepository>();

        return services;
    }
}