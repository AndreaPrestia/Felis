using System.Security.Cryptography;
using System.Text;

namespace Felis.Services;

internal sealed class CredentialService
{
    private readonly string _username;
    private readonly string _password;

    public CredentialService(string username, string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(username);
        ArgumentException.ThrowIfNullOrEmpty(password);

        _username = username;
        _password = GetSha256(password);
    }

    public bool IsValid(string username, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        return _username.Equals(username) && _password.Equals(GetSha256(password));
    }
    
    private static string GetSha256(string text)
    {
        var b = Encoding.Default.GetBytes(text);

        using var calculator = SHA256.Create();
        var c = calculator.ComputeHash(b);

        var stringBuilder = new StringBuilder();

        foreach (var t in c)
        {
            stringBuilder.Append($"{t:x2}");
        }

        return stringBuilder.ToString();
    }
}