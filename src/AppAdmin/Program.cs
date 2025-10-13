using System.Globalization;
using AppAdmin.Components;
using AppFront.Main.Extensions;
using AppFront.Main.OptionEditForms;
using AppFront.Shared.Features;
using AppFront.Shared.Hub;
using AppFront.Shared.Interfaces;
using AppFront.Shared.OptionEditForms;
using Mars.Datasource.Front;
using Mars.Nodes.Core;
using Mars.Nodes.WebApp.Front.Forms;
using Mars.Nodes.Workspace;
using Mars.Options.Front;
using Mars.Plugin.Front;
using Mars.SemanticKernel.Front;
using MarsCodeEditor2;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Toolbelt.Blazor.Extensions.DependencyInjection;

namespace AppAdmin;

/*
 Для быстрой разработки через dotnet watch запускайте Dev/DevAdmin.DevServer
 */

public class Program
{
#if DEBUG
    public static readonly bool Dev = true;
#else
    public static readonly bool Dev = false;
#endif
    public static bool IsPrerender = false; //set on the server "_Host.cshtml"

    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

#if DEBUG
        builder.Logging.SetMinimumLevel(LogLevel.Trace);
        builder.Logging.AddFilter("System", LogLevel.Warning);
        builder.Logging.AddFilter("Microsoft", LogLevel.Error);
#endif

        string? backendUrl = builder.Configuration["BackendUrl"];
        if (string.IsNullOrEmpty(backendUrl))
        {
            backendUrl = builder.HostEnvironment.BaseAddress.Replace("/dev", "", StringComparison.OrdinalIgnoreCase).TrimEnd('/');

            if (backendUrl == "http://localhost:5185")
            {
                backendUrl = "http://localhost:5003";
            }

            Q.BackendUrl = backendUrl;
        }

        //LANG
        var defaultCulture = new CultureInfo("ru");
        var cultureInfo = defaultCulture;
        CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
        CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
        Thread.CurrentThread.CurrentCulture = cultureInfo;
        Thread.CurrentThread.CurrentUICulture = cultureInfo;
        //END LANG

        builder.Services.AddAppFrontMain(builder.Configuration, typeof(Program));

        Q.WorkDir = "C:\\Users\\D\\Documents\\VisualStudio\\2025\\Mars\\src\\";
        Q.SetupHostingInfo(new BackendHostingInfo { Backend = new Uri(Q.BackendUrl) });

        builder.Services.AddHotKeys2();

        NodeFormsLocator.RegisterAssembly(typeof(RenderPageNodeForm).Assembly);
        builder.Services.AddNodeWorkspace();
        builder.Services.DatasourceWorspace();
        builder.Services.AddSemanticKernelFront();

        NodesLocator.RefreshDict();
        NodeFormsLocator.RefreshDict();
        CodeEditor2.ToolbarComponents.Add(typeof(CodeEditorExtraToolbar));
        ContentWrapper.GeneralSectionActions = typeof(Shared.GeneralSectionActions);
        OptionsFormsLocator.RegisterAssembly(typeof(ApiOptionEditForm).Assembly);
        OptionsFormsLocator.RegisterAssembly(typeof(SmtpSettingsEditForm).Assembly);
        OptionsFormsLocator.RefreshDict();

        //string? version = Assembly.GetExecutingAssembly().
        //    GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.
        //    InformationalVersion;

        //Console.WriteLine($"InformationalVersion={version}");

        if (!Q.IsPrerender)
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
        await builder.AddRemotePluginAssemblies(Q.BackendUrl);
        var app = builder.Build();

        SmartSaveExtensions.Setup(app.Services.GetRequiredService<IMessageService>());
        app.UseRemotePluginAssemblies();

        await app.RunAsync();
    }
}

/*
* AUTH - https://docs.microsoft.com/ru-ru/aspnet/core/blazor/security/?view=aspnetcore-5.0
*
*/
