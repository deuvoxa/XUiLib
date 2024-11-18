using XUiLib.Domain.Interfaces;

namespace XUiLib.Infrastructure.Configurations;

public class VlessConfiguration : IVlessConfiguration
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}