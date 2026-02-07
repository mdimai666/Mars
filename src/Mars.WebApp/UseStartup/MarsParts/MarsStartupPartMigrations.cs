using Mars.Core.Extensions;
using Mars.Factories.Seeds;
using Mars.Host.Data.Contexts;
using Mars.Host.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Mars.UseStartup.MarsParts;

public static class MarsStartupPartMigrations
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

    // replace by Standart Entity framework migrations
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
        AppDbContextSeedData.SeedFirstOption(marsDbContext, services, configuration);

        UserManager<UserEntity> userManager = services.GetRequiredService<UserManager<UserEntity>>();

        SeedRoles.SeedFirstData(marsDbContext);
        SeedUsers.SeedFirstData(userManager, marsDbContext);
        await SeedPostData.SeedFirstData(marsDbContext, services, configuration);
        SeedPostCategories.SeedFirstData(marsDbContext);
    }
}

public class AppDatabaseMigrationOptions
{
    public const string SectionName = "AppDatabaseMigrationOptions";

    public bool AutoMigrate { get; set; }
}
