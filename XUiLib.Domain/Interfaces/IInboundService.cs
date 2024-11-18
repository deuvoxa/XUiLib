using XUiLib.Domain.Entities;

namespace XUiLib.Domain.Interfaces;

public interface IInboundService
{
    Task<IEnumerable<Inbound>> GetInboundsAsync();
    string GenerateConfig(Client client, Inbound inbound, string address);
}