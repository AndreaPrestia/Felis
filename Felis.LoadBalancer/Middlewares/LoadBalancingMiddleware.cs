using System.Data;
using System.Security;
using System.Text.Json;
using Felis.LoadBalancer.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Felis.LoadBalancer.Middlewares;

internal class LoadBalancingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly LoadBalancingService _loadBalancingService;
    private readonly ILogger<LoadBalancingMiddleware> _logger;
    private readonly HttpClient _httpClient;

    public LoadBalancingMiddleware(RequestDelegate next, LoadBalancingService loadBalancingService,
        ILogger<LoadBalancingMiddleware> logger, HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(loadBalancingService);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(httpClient);
        _next = next;
        _loadBalancingService = loadBalancingService;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task Invoke(HttpContext context)
    {
        var url = context.Request.Path.Value!;

        var method = context.Request.Method;

        try
        {
            _logger.LogInformation($"Processing request {url} {method}");

            var server = _loadBalancingService.GetNextRouterEndpoint();

            if (string.IsNullOrWhiteSpace(server))
            {
                throw new InvalidOperationException(
                    "No server configured in load balancing. No request will be processed.");
            }

            _logger.LogInformation($"Forwarding {url} {method} to {server}");

            await ForwardRequest(context, server);
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

    private async Task ForwardRequest(HttpContext context, string destinationServer)
    {
        var requestMessage = new HttpRequestMessage();
        requestMessage.Method = new HttpMethod(context.Request.Method);
        requestMessage.RequestUri = new System.Uri($"{destinationServer}/{context.Request.Path}");

        // Copy headers from the original request
        foreach (var header in context.Request.Headers)
        {
            requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }

        // Copy content from the original request if necessary
        if (context.Request.ContentLength is > 0)
        {
            requestMessage.Content = new StreamContent(context.Request.Body);
        }

        // Send the request and get the response
        var responseMessage = await _httpClient.SendAsync(requestMessage);

        // Copy the response back to the original context
        context.Response.StatusCode = (int)responseMessage.StatusCode;
        foreach (var header in responseMessage.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        // Copy the response content
        await responseMessage.Content.CopyToAsync(context.Response.Body);
    }
}