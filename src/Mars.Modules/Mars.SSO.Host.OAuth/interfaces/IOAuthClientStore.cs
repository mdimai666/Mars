using Mars.SSO.Host.OAuth.Models;

namespace Mars.SSO.Host.OAuth.interfaces;

public interface IOAuthClientStore
{
    OAuthClient? FindClientById(string clientId);
}
