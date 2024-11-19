using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using XUiLib.Domain.Entities;
using XUiLib.Domain.Interfaces;

namespace XUiLib.Application.Services;

public class VlessServer(HttpClient httpClient, IAuthTokenProvider tokenProvider, IVlessConfiguration configuration)
    : IVlessServer
{
    public async Task AuthenticateAsync()
    {
        var loginUrl = $"{configuration.BaseUrl}/login";
        var loginData = new { username = configuration.Username, password = configuration.Password };

        var response = await httpClient.PostAsJsonAsync(loginUrl, loginData);
        response.EnsureSuccessStatusCode();

        var setCookieHeader = response.Headers.GetValues("Set-Cookie").FirstOrDefault();
        if (setCookieHeader == null || !TryExtractToken(setCookieHeader, out var token, out var expiration))
        {
            throw new InvalidOperationException("Failed to retrieve or parse auth token");
        }

        tokenProvider.SetAuthToken(token, expiration);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<IEnumerable<Inbound>> GetInboundsAsync()
    {
        if (!tokenProvider.IsTokenValid)
            await AuthenticateAsync();

        var response = await httpClient.GetAsync("/panel/api/inbounds/list");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var jsonElement = JsonDocument.Parse(json).RootElement;

        var inboundsArray = jsonElement.GetProperty("obj");

        return inboundsArray.EnumerateArray()
            .Select(element =>
            {
                var inbound = new Inbound
                {
                    Id = element.GetProperty("id").GetInt32(),
                    Protocol = element.GetProperty("protocol").GetString() ?? string.Empty,
                    Port = element.GetProperty("port").GetInt32(),
                    Enable = element.GetProperty("enable").GetBoolean(),
                    Up = element.GetProperty("up").GetInt64(),
                    Down = element.GetProperty("down").GetInt64(),
                    Total = element.GetProperty("total").GetInt64(),
                    StreamSettings = element.GetProperty("streamSettings").GetString() ?? string.Empty,
                    Settings = element.GetProperty("settings").GetString() ?? string.Empty,
                    ClientStats = element.TryGetProperty("clientStats", out var clientStatsElement) &&
                                  clientStatsElement.ValueKind == JsonValueKind.Array
                        ? clientStatsElement.EnumerateArray()
                            .Select(stat => new ClientStat
                            {
                                Id = stat.GetProperty("id").GetInt32().ToString(),
                                InboundId = stat.GetProperty("inboundId").GetInt32().ToString(),
                                Enable = stat.GetProperty("enable").GetBoolean(),
                                Email = stat.GetProperty("email").GetString() ?? string.Empty,
                                Up = stat.GetProperty("up").GetInt64(),
                                Down = stat.GetProperty("down").GetInt64(),
                                Total = stat.GetProperty("total").GetInt64(),
                                ExpiryTime = stat.GetProperty("expiryTime").GetInt64(),
                            }).ToList()
                        : [],
                    Clients = JsonDocument.Parse(element.GetProperty("settings").GetString()!)
                                  .RootElement.TryGetProperty("clients", out var clientsElement) &&
                              clientsElement.ValueKind == JsonValueKind.Array
                        ? clientsElement.EnumerateArray()
                            .Select(client => new Client
                            {
                                Email = client.GetProperty("email").GetString() ?? string.Empty,
                                Id = client.GetProperty("id").GetString() ?? string.Empty,
                                Enable = client.GetProperty("enable").GetBoolean(),
                                ExpiryTime = client.GetProperty("expiryTime").GetInt64(),
                                TotalGB = client.GetProperty("totalGB").GetInt64()
                            }).ToList()
                        : []
                };

                return inbound;
            }).ToList();
    }

    public string GenerateConfig(Client client, Inbound inbound, string address)
    {
        var streamSettings = JsonDocument.Parse(inbound.StreamSettings).RootElement;
        var realitySettings = streamSettings.GetProperty("realitySettings");

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

    public async Task UpdateClientAsync(int inboundId, Client client)
    {
        if (!tokenProvider.IsTokenValid)
            await AuthenticateAsync();

        var url = $"/panel/api/inbounds/updateClient/{client.Id}";
        var clientPayload = new
        {
            id = inboundId,
            settings = JsonSerializer.Serialize(new
            {
                clients = new[]
                {
                    new
                    {
                        id = client.Id,
                        email = client.Email,
                        totalGB = client.TotalGB,
                        expiryTime =  client.ExpiryTime,
                        enable = client.Enable,
                    }
                }
            })
        };
        
        await httpClient.PostAsJsonAsync(url, clientPayload);
        await UpdateInboundAsync(inboundId);
    }
    
    public async Task<Client> AddClientAsync(int inboundId, string email)
    {
        if (!tokenProvider.IsTokenValid)
            await AuthenticateAsync();

        const string url = "/panel/api/inbounds/addClient";
        var clientId = Guid.NewGuid();
        var clientPayload = new
        {
            id = inboundId,
            settings = JsonSerializer.Serialize(new
            {
                clients = new[]
                {
                    new
                    {
                        id = clientId, email
                    }
                }
            })
        };

        var response = await httpClient.PostAsJsonAsync(url, clientPayload);
        await UpdateInboundAsync(inboundId);

        if (response.IsSuccessStatusCode)
            return new Client
            {
                Id = clientId.ToString(),
                Email = email,
            };

        var error = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException($"Ошибка добавления клиента: {error}");
    }
    
    public async Task UpdateInboundAsync(int inboundId)
    {
        if (!tokenProvider.IsTokenValid)
            await AuthenticateAsync();

        var getUrl = $"/panel/api/inbounds/get/{inboundId}";
        var getResponse = await httpClient.GetAsync(getUrl);
        
        var inboundJson = await getResponse.Content.ReadAsStringAsync();
        var inboundData = JsonDocument.Parse(inboundJson).RootElement.GetProperty("obj");
        
        var inboundPayload = new
        {
            up = inboundData.GetProperty("up").GetInt64(),
            down = inboundData.GetProperty("down").GetInt64(),
            total = inboundData.GetProperty("total").GetInt64(),
            enable = inboundData.GetProperty("enable").GetBoolean(),
            port = inboundData.GetProperty("port").GetInt32(),
            protocol = inboundData.GetProperty("protocol").GetString() ?? "vless",
            settings = inboundData.GetProperty("settings").GetString(),
            streamSettings = inboundData.GetProperty("streamSettings").GetString(),
        };

        var url = $"/panel/api/inbounds/update/{inboundId}";
        await httpClient.PostAsJsonAsync(url, inboundPayload);
    }

    private static bool TryExtractToken(string setCookieHeader, out string token, out DateTime expiration)
    {
        token = string.Empty;
        expiration = DateTime.MinValue;

        var match = Regex.Match(setCookieHeader, @"3x-ui=([^;]+);.*?Expires=([^;]+);");
        if (!match.Success) return false;
        token = match.Groups[1].Value;

        if (!DateTime.TryParse(match.Groups[2].Value, out var parsedExpiration)) return false;
        expiration = parsedExpiration.ToUniversalTime();
        return true;
    }
}