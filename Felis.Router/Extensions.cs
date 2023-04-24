using Felis.Router.Hubs;
using Felis.Router.Interfaces;
using Felis.Router.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Felis.Router;

public static class Extensions
{
    public static void AddFelisRouter(this WebApplicationBuilder builder)
    {
        var hubConnection = new HubConnectionBuilder().Build();
    
        builder.Services.AddSingleton(hubConnection);
        builder.Services.AddSingleton<IFelisRouterHub, FelisRouterHub>();
        builder.Services.AddSingleton<IFelisRouterStorage, FelisRouterStorage>();
    }
}