namespace XUiLib.Domain.Interfaces;

public interface IAuthTokenProvider
{
    string? AuthToken { get; }
    bool IsTokenValid { get; }
    void SetAuthToken(string token, DateTime expiration);
    void InvalidateToken();
}