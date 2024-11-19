using XUiLib.Domain.Entities;

namespace XUiLib.Domain.Interfaces;

public interface IVlessServer
{
    Task AuthenticateAsync();
    Task<IEnumerable<Inbound>> GetInboundsAsync();
    string GenerateConfig(Client client, Inbound inbound, string address);

    Task<Client> AddClientAsync(int inboundId, string email);
    Task UpdateClientAsync(int inboundId, Client client);
    Task UpdateInboundAsync(int inboundId);
}