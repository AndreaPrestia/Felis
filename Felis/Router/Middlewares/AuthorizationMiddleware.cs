using System.Security;
using System.Text;
using Felis.Router.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Felis.Router.Middlewares;

internal class AuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorMiddleware> _logger;
    private readonly CredentialService _credentialService;
    
    public AuthorizationMiddleware(RequestDelegate next, ILogger<ErrorMiddleware> logger, CredentialService credentialService)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(credentialService);
        _next = next;
        _logger = logger;
        _credentialService = credentialService;
    }

    public async Task Invoke(HttpContext context)
    {
        var url = context.Request.Path.Value!;

        var method = context.Request.Method;

        var authorization = context.Request.Headers["Authorization"].ToString();

        if (string.IsNullOrWhiteSpace(authorization))
        {
            throw new SecurityException("Authorization not provided");
        }

        var encodedCredentials = authorization.Split(" ").LastOrDefault();

        if (string.IsNullOrWhiteSpace(encodedCredentials))
        {
            throw new SecurityException("Credentials not provided");
        }

        var decodedContent = Encoding.Default.GetString(Convert.FromBase64String(encodedCredentials));

        var splitAuthorization = decodedContent.Split(':');
        
        var username = splitAuthorization[0];

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new SecurityException("Username not provided.");
        }

        var password = splitAuthorization[1];

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new SecurityException("Password not provided.");
        }

        var isAuthorized = _credentialService.IsValid(username, password);

        if (!isAuthorized)
        {
            _logger.LogWarning($"Username {username} with password {password} not authorized on resource {url} {method}");
            throw new UnauthorizedAccessException();
        }
        
        await _next.Invoke(context);
    }
}