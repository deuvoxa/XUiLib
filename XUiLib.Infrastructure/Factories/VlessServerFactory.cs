using XUiLib.Application.Services;
using XUiLib.Domain.Interfaces;
using XUiLib.Infrastructure.Configurations;
using XUiLib.Infrastructure.Http;

namespace XUiLib.Infrastructure.Factories;

public class VlessServerFactory(IHttpClientFactory httpClientFactory) : IVlessServerFactory
{
    public IVlessServer CreateServer(string baseUrl, string username, string password)
    {
        var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(baseUrl);
        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        var config = new VlessConfiguration()
        {
            BaseUrl = baseUrl,
            Username = username,
            Password = password
        };

        var tokenProvider = new AuthTokenProvider();
        return new VlessServer(client, tokenProvider, config);
    }
}