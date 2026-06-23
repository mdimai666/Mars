using Mars.Host.Models;

namespace Mars.UseStartup.MarsParts;

internal static class MarsStartupPartPostgresDistributedCache
{
    public static IServiceCollection AddPostgresDistributedCache(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDistributedPostgresCache(options =>
        {
            var settings = new PostgresDistributedCacheSettings();
            configuration.GetSection(PostgresDistributedCacheSettings.Section).Bind(settings);

            options.ConnectionString = configuration.GetConnectionString("DefaultConnection");
            options.SchemaName = settings.SchemaName;
            options.TableName = settings.TableName;
            options.CreateIfNotExists = settings.CreateIfNotExists;
            options.UseWAL = settings.UseWAL;

            if (TimeSpan.TryParse(settings.ExpiredItemsDeletionInterval, out var interval))
            {
                options.ExpiredItemsDeletionInterval = interval;
            }

            if (TimeSpan.TryParse(settings.DefaultSlidingExpiration, out var sliding))
            {
                options.DefaultSlidingExpiration = sliding;
            }

        });
        services.AddHybridCache();

        return services;
    }
}
