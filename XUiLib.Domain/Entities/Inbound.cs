namespace XUiLib.Domain.Entities;

public class Inbound
{
    public string Protocol { get; set; } = null!;
    public int Port { get; set; }
    public string StreamSettings { get; set; } = null!;
    public string Settings { get; set; } = null!;
}