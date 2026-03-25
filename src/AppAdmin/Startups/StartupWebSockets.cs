using AppFront.Shared.Hub;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace AppAdmin.Startups;

internal static class StartupWebSockets
{
    static readonly TimeSpan[] reconnectDelays = [
        TimeSpan.Zero,
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(2),
        TimeSpan.FromMinutes(5)
    ];

    internal static void ConfigureWebSockets(this WebAssemblyHostBuilder builder, string backendUrl)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl($"{backendUrl}/_ws/admin", HttpTransportType.WebSockets | HttpTransportType.LongPolling)
            .ConfigureLogging(logging =>
            {
                //logging.SetMinimumLevel(LogLevel.Information);
                //logging.AddConsole();
            })
            .WithAutomaticReconnect(reconnectDelays)
            .Build();

        connection.KeepAliveInterval = TimeSpan.FromSeconds(15);
        connection.ServerTimeout = TimeSpan.FromSeconds(30);

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
