using System.Text;
using System.Text.Json;
using System.Web;
using XUiLib.Domain.Entities;
using XUiLib.Domain.Interfaces;

namespace XUiLib.Application.Services;

public class InboundService(HttpClient httpClient, IAuthTokenProvider tokenProvider, IAuthService authService) : IInboundService
{
    public async Task<IEnumerable<Inbound>> GetInboundsAsync()
    {
        if (!tokenProvider.IsTokenValid)
        {
            await authService.AuthenticateAsync();
        }

        var response = await httpClient.GetAsync("/panel/api/inbounds/list");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var jsonElement = JsonDocument.Parse(json).RootElement;

        return jsonElement.GetProperty("obj").EnumerateArray()
            .Select(element => new Inbound
            {
                Protocol = element.GetProperty("protocol").GetString() ?? throw new InvalidOperationException(),
                Port = element.GetProperty("port").GetInt32(),
                StreamSettings = element.GetProperty("streamSettings").GetString() ?? throw new InvalidOperationException(),
                Settings = element.GetProperty("settings").GetString() ?? throw new InvalidOperationException()
            }).ToList();
    }

    public string GenerateConfig(Client client, Inbound inbound, string address)
    {
        var streamSettings = JsonDocument.Parse(inbound.StreamSettings).RootElement;
        var realitySettings = streamSettings.GetProperty("realitySettings");
        var settings = JsonDocument.Parse(inbound.Settings).RootElement;

        var publicKey = realitySettings.GetProperty("settings").GetProperty("publicKey").GetString();
        var fingerprint = realitySettings.GetProperty("settings").GetProperty("fingerprint").GetString();
        var serverName = realitySettings.GetProperty("serverNames")[0].GetString();
        var shortId = realitySettings.GetProperty("shortIds")[0].GetString();
        var spiderX = realitySettings.GetProperty("settings").GetProperty("spiderX").GetString();

        var result =
            $"{inbound.Protocol}://{client.Id}@{address}:{inbound.Port}?type={streamSettings.GetProperty("network").GetString()}" +
            $"&security={streamSettings.GetProperty("security").GetString()}" +
            $"&pbk={HttpUtility.UrlEncode(publicKey)}&fp={fingerprint}&sni={HttpUtility.UrlEncode(serverName)}" +
            $"&sid={shortId}&spx={HttpUtility.UrlEncode(spiderX)}#{client.Email}";

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(result));
    }
}