using System.Security.Cryptography;

namespace Felis.Core.Models;

public record Service(string? Name, string? Host, bool IsPublic);

