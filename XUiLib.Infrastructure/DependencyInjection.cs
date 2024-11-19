using Microsoft.Extensions.DependencyInjection;
using XUiLib.Application.Services;
using XUiLib.Domain.Interfaces;
using XUiLib.Infrastructure.Configurations;
using XUiLib.Infrastructure.Factories;
using XUiLib.Infrastructure.Http;

namespace XUiLib.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddXUiLib(this IServiceCollection services)
    {
        services.AddSingleton<IVlessServerFactory, VlessServerFactory>();
        services.AddHttpClient();
        return services;
    }
}