using XUiLib.Domain.Interfaces;

namespace XUiLib.Infrastructure.Http;

public class AuthTokenProvider : IAuthTokenProvider
{
    private DateTime? _expiration;

    public string? AuthToken { get; private set; }

    public bool IsTokenValid => AuthToken != null && _expiration.HasValue && _expiration > DateTime.UtcNow;

    public void SetAuthToken(string token, DateTime expiration)
    {
        AuthToken = token;
        _expiration = expiration;
    }

    public void InvalidateToken()
    {
        AuthToken = null;
        _expiration = null;
    }
}