namespace XUiLib.Domain.Entities;

public class ClientStat
{
    public string Id { get; set; } = string.Empty;
    public string InboundId { get; set; } = string.Empty;
    public bool Enable { get; set; }
    public string Email { get; set; } = string.Empty;
    public long Up { get; set; }
    public long Down { get; set; }
    public long Total { get; set; }
    public long ExpiryTime { get; set; }
}