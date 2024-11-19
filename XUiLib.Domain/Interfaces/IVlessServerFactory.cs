namespace XUiLib.Domain.Interfaces;

public interface IVlessServerFactory
{
    IVlessServer CreateServer(string baseUrl, string username, string password);
}