using System;
using Cairnly.API.Constants;
using Cairnly.API.Models.Settings;
using Cairnly.API.Services.Email;
using Cairnly.API.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cairnly.API.ApplicationStartup.ServiceCollectionExtensions;

public static class EmailServiceCollectionExtensions
{
    public static IServiceCollection AddEmailServices(this IServiceCollection services, IConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(config);

        services.Configure<EmailSettings>(config.GetSection(ConfigurationKeys.Email));

        services.AddSingleton<IAcsEmailClientFactory, AcsEmailClientFactory>()
            .AddScoped<IEmailService, AcsEmailService>()
            .AddSingleton<IEmailTemplateService, EmailTemplateService>();

        return services;
    }
}