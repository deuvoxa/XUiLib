using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using XUiLib.Domain.Interfaces;

namespace XUiLib.Application.Services;

public class AuthService(HttpClient httpClient, IAuthTokenProvider tokenProvider, IVlessConfiguration configuration)
    : IAuthService
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
            throw new InvalidOperationException("Failed to retrieve or parse auth token.");
        }
        
        tokenProvider.SetAuthToken(token, expiration);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
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