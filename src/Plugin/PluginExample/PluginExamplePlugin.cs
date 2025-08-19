using Mars.Host.Shared.Services;
using Mars.Plugin.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PluginExample;
using PluginExample.Data;
using PluginExample.Data.Seeds;
using PluginExample.Options;

[assembly: WebApplicationPlugin(typeof(PluginExamplePlugin))]

namespace PluginExample;

public class PluginExamplePlugin : WebApplicationPlugin, IPluginDatabaseMigrator
{
    public const string PluginName = "PluginExample";
    public const string PluginNameFullName = "PackageName.PluginExample";

    public override void ConfigureWebApplicationBuilder(WebApplicationBuilder builder, PluginSettings settings)
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;

        builder.Services.AddTransient<MyPluginDbContext>(sp =>
        {
            return MyPluginDbContext.CreateInstance(connectionString);
        });

    }

    public override void ConfigureWebApplication(WebApplication app, PluginSettings settings)
    {
        var logger = MarsLogger.GetStaticLogger<PluginExamplePlugin>();

        logger.LogWarning("> Example1Plugin - Work!!!!");

        var op = app.Services.GetRequiredService<IOptionService>();

        op.RegisterOption<PluginExampleOption1>(appendToInitialSiteData: true);
        op.SaveOption(new PluginExampleOption1 { Value = "200" });
        op.SetConstOption(new PluginConstOption2() { Value = "222" }, appendToInitialSiteData: true);
    }

    public async Task ApplyMigrations(IServiceProvider rootServices, IConfiguration configuration, PluginSettings settings)
    {
        using var serviceScope = rootServices.CreateScope();
        // not work - return invalid DbContextOptions. variant from BaseClass
        //var ef = serviceScope.ServiceProvider.GetRequiredService<MyPluginDbContext>();

        var conn = configuration.GetConnectionString("DefaultConnection")!;
        using var ef = MyPluginDbContext.CreateInstance(conn);

        var pendingMigrations = (await ef.Database.GetPendingMigrationsAsync()).ToList();
        var migrations = ef.Database.GetMigrations();
        var appliedMigrations = await ef.Database.GetAppliedMigrationsAsync();

        if (!migrations.Any())
            throw new Exception("Migrations not exist or DbContext configure invalid!");

        if (pendingMigrations.Count() > 0)
        {
            Console.WriteLine("Start PLUGIN migrate...");
            ef.Database.Migrate();
            Console.WriteLine("Migrate PLUGIN complete.");
        }

        PluginNewsSeed.SeedFirstData(ef, serviceScope.ServiceProvider, configuration, settings.ContentRootPath).Wait();
    }

}
