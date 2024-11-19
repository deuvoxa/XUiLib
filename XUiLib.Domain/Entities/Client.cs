namespace XUiLib.Domain.Entities;

public class Client
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool Enable { get; set; }
    public long ExpiryTime { get; set; }
    public long TotalGB { get; set; }
}