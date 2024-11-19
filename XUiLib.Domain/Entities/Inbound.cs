namespace XUiLib.Domain.Entities;

public class Inbound
{
    public int Id { get; set; }
    public string Protocol { get; set; } = string.Empty;
    public int Port { get; set; }
    public bool Enable { get; set; }
    public long Up { get; set; }
    public long Down { get; set; }
    public long Total { get; set; }
    public List<ClientStat> ClientStats { get; set; } = [];
    public List<Client> Clients { get; set; } = [];
    public string StreamSettings { get; set; } = string.Empty;
    public string Settings { get; set; } = string.Empty;
}