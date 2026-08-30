using Mars.Excel.Abstractions;
using Mars.Excel.Host.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Excel.Host;

public static class MainExcel
{
    public static IServiceCollection AddMarsExcel(this IServiceCollection services)
    {
        services.AddScoped<IExcelService, ExcelService>();

        return services;
    }
}
