using Mars.CommandLine.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.CommandLine.Scripting;

public static class MainCommandLineScripting
{
    public static WebApplication UseMarsCommandLineScripting(this WebApplication app)
    {
        app.Services.GetService<ICommandLineApi>()?.Register<FnCommandCli>();
        return app;
    }
}
