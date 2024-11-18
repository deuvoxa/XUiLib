using Microsoft.Extensions.DependencyInjection;
using XUiLib.Application.Services;
using XUiLib.Domain.Interfaces;
using XUiLib.Infrastructure.Configurations;
using XUiLib.Infrastructure.Http;

namespace XUiLib.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddVlessLib(this IServiceCollection services, Action<VlessConfiguration> configure)
    {
        var configuration = new VlessConfiguration();
        configure(configuration);

        services.AddSingleton(configuration);
        services.AddSingleton<IAuthTokenProvider, AuthTokenProvider>();

        services.AddHttpClient("VlessHttpClient", client =>
        {
            client.BaseAddress = new Uri(configuration.BaseUrl);
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        });

        services.AddSingleton<IAuthService>(provider =>
        {
            var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("VlessHttpClient");
            var tokenProvider = provider.GetRequiredService<IAuthTokenProvider>();
            var config = provider.GetRequiredService<VlessConfiguration>();
            return new AuthService(client, tokenProvider, config);
        });

        services.AddSingleton<IInboundService>(provider =>
        {
            var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("VlessHttpClient");
            var tokenProvider = provider.GetRequiredService<IAuthTokenProvider>();
            var authService = provider.GetRequiredService<IAuthService>();
            return new InboundService(client, tokenProvider, authService);
        });

        return services;
    }
}