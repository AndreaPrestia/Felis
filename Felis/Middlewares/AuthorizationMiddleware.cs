using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security;
using System.Security.Claims;

namespace Felis.Middlewares;

internal class AuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthorizationMiddleware> _logger;

    public AuthorizationMiddleware(RequestDelegate next, ILogger<AuthorizationMiddleware> logger)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(logger);
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        var url = context.Request.Path.Value!;

        var method = context.Request.Method;

        var certificate = context.Connection.ClientCertificate ?? throw new SecurityException("Certificate not provided");

        var isAuthorized = certificate.Verify();

        if (!isAuthorized)
        {
            _logger.LogWarning($"Name {certificate.Subject} not authorized on resource {url} {method}");
            throw new UnauthorizedAccessException();
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, certificate.Subject),
            new Claim(ClaimTypes.Name, certificate.Subject)
        };

        var identity = new ClaimsIdentity(claims, "Certificate");
        context.User = new ClaimsPrincipal(identity);

        await _next.Invoke(context);
    }
}