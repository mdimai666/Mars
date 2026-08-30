using Mars.Core.Extensions;
using Mars.Data.Contexts;
using Mars.Data.Seeding;
using Mars.Server.Models;
using Mars.Server.Seeding;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Mars.Server.Startup;

public static class MarsDbStartup
{
    public static WebApplication MarsRequireMigrate(this WebApplication app, ILogger logger, NpgsqlConnectionStringBuilder dbx)
    {
        Console.WriteLine("Database = " + dbx.Database);
        Console.WriteLine("Start migrate...");

        using var serviceScope = app.Services.CreateScope();
        using var marsDbContext = serviceScope.ServiceProvider.GetService<MarsDbContext>();
        //MarsDbContext.Database.EnsureCreated(); не требуется. создает схему без истории миграций.

        var migrations = marsDbContext.Database.GetPendingMigrations();
        if (migrations.Count() > 0)
        {
            Console.WriteLine("[Migrations] MarsDbContext: begin migrate...\n\t" + migrations.JoinStr(";\n\t"));
            marsDbContext.Database.Migrate();
            Console.WriteLine("[Migrations] MarsDbContext: complete.");
        }
        else
        {
            Console.WriteLine("no migrations");
        }

        Console.WriteLine("Migrate complete.");

        return app;
    }

    public static IServiceProvider MarsAutoMigrateCheck(this IServiceProvider services, IConfiguration configuration, ILogger logger, out bool migrated)
    {
        migrated = false;
        var migrateOptions = configuration.GetSection(AppDatabaseMigrationOptions.SectionName).Get<AppDatabaseMigrationOptions>();

        if (migrateOptions.AutoMigrate)
        {
            using var serviceScope = services.CreateScope();
            using var marsDbContext = serviceScope.ServiceProvider.GetRequiredService<MarsDbContext>();
            migrated = MigrateAsync(marsDbContext, logger).ConfigureAwait(false).GetAwaiter().GetResult();
        }
        return services;
    }

    public static async Task<bool> MigrateAsync(MarsDbContext marsDbContext, ILogger logger)
    {
        var migrations = marsDbContext.Database.GetPendingMigrations();
        if (migrations.Count() > 0)
        {
            logger.LogWarning("[Migrations] MarsDbContext: begin migrate...\n\t" + migrations.JoinStr(";\n\t"));
            await marsDbContext.Database.MigrateAsync();
            logger.LogWarning("[Migrations] MarsDbContext: complete.");
            return true;
        }
        return false;
    }

    // replace by Standard Entity framework migrations
    public static IServiceProvider SeedData(this IServiceProvider services, IConfiguration configuration, ILogger logger, bool migrated)
    {
        using var serviceScope = services.CreateScope();
        using var marsDbContext = serviceScope.ServiceProvider.GetRequiredService<MarsDbContext>();
        if (migrated)
        {
            //
        }
        SeedDataAsync(marsDbContext, serviceScope.ServiceProvider, configuration, logger).ConfigureAwait(false).GetAwaiter().GetResult();
        marsDbContext.ChangeTracker.Clear();
        return services;
    }

    static async Task SeedDataAsync(MarsDbContext marsDbContext, IServiceProvider services, IConfiguration configuration, ILogger logger)
    {
        services.GetRequiredService<ISeedFirstOptionHandler>().Seed(configuration);

        var handlers = services.GetServices<ISeedDataHandler>().OrderBy(h => h.Order);
        foreach (var handler in handlers)
            await handler.SeedAsync(marsDbContext, services, configuration);
    }
}
