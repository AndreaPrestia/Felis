using System.Data;
using System.Security;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Felis.Router.Middlewares;

public class ErrorMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorMiddleware> _logger;

    public ErrorMiddleware(RequestDelegate next, ILogger<ErrorMiddleware> logger)
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

        try
        {
            _logger.LogDebug($"Processing request {url} {method}");

            await _next.Invoke(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);

            var status = ex switch
            {
                ArgumentException => (StatusCodes.Status400BadRequest, "Bad Request"),
                InvalidOperationException => (StatusCodes.Status400BadRequest, "Bad Request"),
                SecurityException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
                UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Forbidden"),
                EntryPointNotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
                FileNotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
                InvalidConstraintException => (StatusCodes.Status409Conflict, "Conflict"),
                _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
            };

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = status.Item1;

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(new ProblemDetails()
                {
                    Type = $"https://httpstatuses.io/{status.Item1}",
                    Detail = ex.Message,
                    Status = status.Item1,
                    Title = status.Item2,
                    Instance = $"{url}",
                }));
        }
    }
}