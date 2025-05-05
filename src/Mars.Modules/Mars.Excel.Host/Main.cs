using Mars.Excel.Host.Services;
using Mars.Host.Shared.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Excel.Host;

public static class Main
{
    public static IServiceCollection AddMarsExcel(this IServiceCollection services)
    {
        services.AddScoped<IExcelService, ExcelService>();

        return services;
    }

    public static IApplicationBuilder UseMarsExcel(this WebApplication app)
    {

        return app;
    }
}
