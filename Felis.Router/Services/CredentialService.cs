using Felis.Router.Entities;

namespace Felis.Router.Services;

public sealed class CredentialService
{
    private readonly CredentialEntity _credential;

    public CredentialService(string username, string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(username);
        ArgumentException.ThrowIfNullOrEmpty(password);

        _credential = new CredentialEntity()
        {
            Username = username,
            Password = password
        };
    }

    public bool IsValid(string username, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        return _credential.Username.Equals(username) && _credential.Password.Equals(password);
    }
}