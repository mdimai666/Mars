using AppFront.Shared.Hub;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace AppAdmin.Startups;

internal static class StartupWebSockets
{
    internal static void ConfigureWebSockets(this WebAssemblyHostBuilder builder, string backendUrl)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl($"{backendUrl}/_ws/ws", HttpTransportType.WebSockets | HttpTransportType.LongPolling)
            .ConfigureLogging(logging =>
            {
                //logging.SetMinimumLevel(LogLevel.Information);
                //logging.AddConsole();
            })
            .WithAutomaticReconnect()
            .Build();

        builder.Services.AddScoped<HubConnection>(sp => connection);
        builder.Services.AddScoped<ClientHub>();

        _ = connection.StartAsync();

        connection.Reconnecting += (e) =>
        {
            Q.Root.Emit(nameof(HubConnectionState), connection.State);
            return Task.CompletedTask;
        };
        connection.Closed += (e) =>
        {
            Q.Root.Emit(nameof(HubConnectionState), connection.State);
            return Task.CompletedTask;
        };
        connection.Reconnected += (e) =>
        {
            Q.Root.Emit(nameof(HubConnectionState), connection.State);
            return Task.CompletedTask;
        };
    }
}
